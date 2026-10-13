using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // More to do with her: she walks around the house with you, asks you for small favours, and a few games
    // (word chain, riddles, hide and seek, 참참참) on top of rock-paper-scissors and the story's cards and TV.
    public partial class PeaceDirector
    {
        GameLines GL => Personas.Games(Pn.Id);
        string gamePrompt;   // a line of the running game on the HUD (instead of the usual hint)

        // ---------------------------------------------------------------- walking around together

        bool following;
        GameObject followTarget;
        float nextFollow;

        void StartFollow()
        {
            EndRoutine();
            following = true;
            nextFollow = 0f;
            Did("follow");
            Play(GL.FollowOn);
            Plugin.Log.LogInfo("[follow] on");
        }

        void StopFollow(bool say)
        {
            if (!following) return;
            following = false;
            try { mita?.AiShraplyStop(); } catch { }
            HoldAddon(false);
            if (say) Play(GL.FollowOff);
            Plugin.Log.LogInfo("[follow] off");
        }

        // she keeps a step or two behind you, through the doors too (the story's own walk on the NavMesh). A new walk
        // restarts her walking animation, so it's only given when she has stopped; the target moves with you.
        bool followWalking;
        float followIssued, followStill;

        void TickFollow()
        {
            if (!following || mita == null || mitaBody == null || pm == null) return;
            if (posed || busy || activity != MitaActivity.Free || cardGame || hideStage != 0) { followWalking = false; return; }
            if (Time.time < nextFollow) return;
            nextFollow = Time.time + 0.25f;
            var me = pm.transform.position;
            var d = me - mitaBody.position; d.y = 0;
            if (followTarget == null) followTarget = new GameObject("PeaceFollowTarget");
            var spot = me - (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward) * 1.2f;
            if (NavMesh.SamplePosition(spot, out var hit, 1.5f, -1)) spot = hit.position;
            followTarget.transform.position = spot;
            var nma = mita.nma;
            if (followWalking)
            {
                if (d.magnitude < 1.6f)
                {
                    followWalking = false;
                    try { mita.AiShraplyStop(); mitaLook?.LookOnPlayer(); } catch { }
                    return;
                }
                // stopped short (the target moved on): walk again
                bool still = nma == null || nma.velocity.magnitude < 0.05f;
                if (!still) followStill = Time.time;
                if (still && Time.time - followIssued > 1.5f && Time.time - followStill > 1f) followWalking = false;
                else return;
            }
            if (d.magnitude < 2.2f) return;
            HoldAddon(true);
            if (nma != null) { nma.enabled = true; try { nma.isStopped = false; } catch { } }
            try { mita.AiWalkToTarget(followTarget.transform); } catch (Exception e) { Plugin.Log.LogWarning("[follow] walk: " + e.Message); return; }
            followWalking = true;
            followIssued = followStill = Time.time;
            if (DevRunner.Active) Plugin.Log.LogInfo($"[follow] walking to {spot} ({d.magnitude:0.0} m from you)");
        }

        // ---------------------------------------------------------------- her small favours

        class Request { public string spot, item; public Line[] ask; public string take; }
        static readonly Request[] Requests =
        {
            new Request { spot = "fridge", item = "우유", ask = new[] { new Line(Who.Mita, "목말라요… 냉장고에서 우유 좀 가져다줄래요?", "smile") }, take = "(냉장고에서 차가운 우유를 한 병 꺼냈다.)" },
            new Request { spot = "teapot", item = "차", ask = new[] { new Line(Who.Mita, "따뜻한 차가 마시고 싶어요. 부엌 주전자로 한 잔만 부탁해도 돼요?", "smile") }, take = "(주전자로 차를 한 잔 우렸다.)" },
            new Request { spot = "books", item = "책", ask = new[] { new Line(Who.Mita, "거실 책장에서 아무 책이나 한 권 골라 줄래요? 같이 읽게요.", "smile") }, take = "(책장에서 표지가 예쁜 책을 한 권 골랐다.)" },
            new Request { spot = "teddy", item = "곰 인형", ask = new[] { new Line(Who.Mita, "침실 곰 인형 좀 데려와 줄래요? 안고 있고 싶어요.", "shy") }, take = "(곰 인형을 품에 안았다.)" },
            new Request { spot = "recorder", item = "카세트", ask = new[] { new Line(Who.Mita, "카세트 플레이어에서 테이프 좀 틀어 줄래요? 음악이 듣고 싶어요.", "smile") }, take = "(카세트의 재생 버튼을 눌렀다. 잔잔한 노래가 흘러나온다.)" },
            new Request { spot = "cactus", item = "선인장", ask = new[] { new Line(Who.Mita, "침실 선인장한테 물 좀 줄래요? 조금만요!", "smile") }, take = "(선인장에 물을 아주 조금 주었다.)" },
            new Request { spot = "moonlamp", item = "달 조명", ask = new[] { new Line(Who.Mita, "침실 달 조명 좀 켜 줄래요? 분위기 내고 싶어요.", "smile") }, take = "(달 조명을 켰다. 방이 은은해졌다.)" },
            new Request { spot = "speakers", item = "음악", ask = new[] { new Line(Who.Mita, "거실 스피커로 음악 좀 틀어 줄래요?", "smile") }, take = "(스피커를 켰다. 경쾌한 음악이 흐른다.)" },
            new Request { spot = "umbrella", item = "우산", ask = new[] { new Line(Who.Mita, "현관 우산꽂이 좀 봐 줄래요? 우산이 쓰러져 있는 것 같아요.", "quest") }, take = "(쓰러진 우산을 바로 세웠다.)" },
            new Request { spot = "oldtv", item = "TV", ask = new[] { new Line(Who.Mita, "부엌 작은 TV 좀 켜 줄래요? 만화 할 시간이에요!", "smile") }, take = "(작은 TV를 켰다. 옛날 만화가 나온다.)" },
            new Request { spot = "duck", item = "고무 오리", ask = new[] { new Line(Who.Mita, "욕실에 고무 오리 있죠? 걔 좀 데려와 줄래요? 헤헤.", "smiletonque") }, take = "(고무 오리를 집어 들었다. 꽥.)" },
        };

        Request request;
        string devRequest;
        bool requestDone;
        float requestUntil, nextRequest = -1f;

        // instead of a line to herself now and then, while she is free
        bool TryRequest()
        {
            if (request != null || activity != MitaActivity.Free || following) return false;
            if (nextRequest < 0f) nextRequest = Time.time + Random.Range(120f, 240f);
            if (Time.time < nextRequest) return false;
            nextRequest = Time.time + Random.Range(300f, 540f);
            var open = Requests.Where(r => spots.Any(s => s.id == r.spot) && (devRequest == null || r.spot == devRequest)).ToList();
            if (open.Count == 0) return false;
            request = open[Random.Range(0, open.Count)];
            requestDone = false;
            requestUntil = Time.time + 360f;
            SetIconText("?");
            ShowQuestIcon(true);
            Play(request.ask);
            Plugin.Log.LogInfo("[request] " + request.spot);
            return true;
        }

        // a thing clicked while she asked for it: you take care of it; true = that was it
        [HideFromIl2Cpp] bool RequestSpot(Spot sp)
        {
            if (request == null || requestDone || sp.id != request.spot) return false;
            requestDone = true;
            SetIconText("!");
            PlayerTake();
            Play(new[] { new Line(Who.Player, request.take) }, null, true);
            return true;
        }

        // back with her: thanks (called when you talk to her)
        bool RequestBack()
        {
            if (request == null || !requestDone) return false;
            var item = request.item;
            request = null;
            Did("request");
            ShowQuestIcon(false);
            AddAff(3);
            Play(Pick(GL.Thanks).Select(l => new Line(l.who, Loc.T(l.text).Replace("{item}", Loc.T(item)), l.emo, l.clip)).ToList(), CheckMilestones);
            return true;
        }

        void TickRequest()
        {
            if (request == null || Time.time < requestUntil) return;
            Plugin.Log.LogInfo("[request] forgotten");
            LoseAff(1, "forgot what she asked for");
            request = null;
            if (activity == MitaActivity.Free) ShowQuestIcon(false);
        }

        // ---------------------------------------------------------------- word chain (끝말잇기)

        // chains where every word starts with the last syllable of the one before
        static readonly string[][] WordChains =
        {
            new[] { "기차", "차표", "표정", "정원", "원숭이", "이불", "불꽃", "꽃병", "병아리", "리본" },
            new[] { "사과", "과자", "자전거", "거울", "울타리", "리듬" },
            new[] { "바다", "다리", "리본", "본보기", "기린", "린스", "스키", "키위", "위로" },
            new[] { "고양이", "이야기", "기타", "타조", "조개", "개구리", "리코더", "더위" },
            new[] { "하늘", "늘보", "보물", "물감", "감자", "자두", "두부", "부엉이", "이름" },
        };
        static readonly string[] WordDecoys = { "사탕", "모자", "나비", "구름", "도넛", "연필", "바나나", "토끼", "눈사람", "선물", "피아노", "별빛", "시계", "수박", "딸기", "호수", "쿠키", "우산", "안경", "미소" };

        string[] chain;
        int chainAt;

        void StartWordChain()
        {
            Did("words");
            chain = Chains[Random.Range(0, Chains.Length)];
            chainAt = 0;
            Play(GL.WordStart, () => WordTurn());
        }

        // she says chain[chainAt]; you pick chain[chainAt + 1] among decoys
        void WordTurn()
        {
            if (chainAt + 1 >= chain.Length)
            {
                // nothing left for her: you win
                AddAff(2);
                Play(new[] { new Line(Who.Mita, Loc.F("'{0}'(으)로 시작하는 말…?", Last(chain[chainAt])), "quest") }.Concat(Pick(GL.Lose)).ToList(), CheckMilestones);
                return;
            }
            var word = chain[chainAt];
            var right = chain[chainAt + 1];
            var need = Last(word);
            var opts = Decoys.Where(w => First(w) != need && w != right).OrderBy(_ => Random.value).Take(2).ToList();
            opts.Add(right);
            opts = opts.OrderBy(_ => Random.value).ToList();
            Play(new[] { new Line(Who.Mita, word + "!", "smile") }, () => ShowMenu("끝말잇기", opts,
                opts.Select(o => (Action)(() => WordAnswer(o, right))).ToList()));
        }

        void WordAnswer(string said, string right)
        {
            if (said != right)
            {
                Play(new[] { new Line(Who.Player, said + "!"), new Line(Who.Mita, Loc.F("땡! '{0}'(으)로 시작해야죠!", Last(chain[chainAt])), "smiletonque") }.Concat(Pick(GL.Win)).ToList());
                return;
            }
            chainAt += 2;
            if (chainAt >= chain.Length)
            {
                AddAff(2);
                Play(new[] { new Line(Who.Player, said + "!"), new Line(Who.Mita, Loc.F("'{0}'…? 으으, 생각이 안 나요!", Last(said)), "surprise") }.Concat(Pick(GL.Lose)).ToList(), CheckMilestones);
                return;
            }
            Play(new[] { new Line(Who.Player, said + "!") }, WordTurn);
        }


        // ---------------------------------------------------------------- riddles (넌센스 퀴즈)

        static readonly (string q, string a, string[] wrong)[] Riddles =
        {
            ("아몬드가 죽으면 뭐가 될까요?", "다이아몬드", new[] { "호두", "땅콩" }),
            ("왕이 넘어지면 뭐라고 할까요?", "킹콩", new[] { "왕창", "쿵왕" }),
            ("소가 불에 타면 뭐가 될까요?", "탄소", new[] { "불고기", "소불" }),
            ("딸기가 직장을 잃으면?", "딸기시럽", new[] { "딸기잼", "딸기우유" }),
            ("자동차를 톡 치면?", "카톡", new[] { "빵빵", "톡카" }),
            ("병아리가 제일 잘 먹는 약은?", "삐약", new[] { "알약", "물약" }),
            ("세상에서 가장 빠른 닭은?", "후다닥", new[] { "치킨", "통닭" }),
            ("개 중에서 가장 아름다운 개는?", "무지개", new[] { "진돗개", "푸들" }),
            ("도둑이 가장 싫어하는 아이스크림은?", "누가바", new[] { "쌍쌍바", "메로나" }),
            ("할아버지가 가장 좋아하는 돈은?", "할머니", new[] { "용돈", "세뱃돈" }),
            ("인천 앞바다의 반대말은?", "인천 엄마다", new[] { "인천 뒷바다", "부산 앞바다" }),
            ("오리가 얼면?", "언덕", new[] { "얼음오리", "빙수" }),
            ("바나나가 웃으면?", "바나나킥", new[] { "바나나우유", "하하나나" }),
            ("세상에서 가장 뜨거운 과일은?", "천도복숭아", new[] { "불수박", "고추" }),
            ("왕이 궁에 들어가기 싫을 때 하는 말은?", "궁시렁궁시렁", new[] { "왕왕", "싫궁" }),
        };
        readonly Queue<int> recentRiddles = new Queue<int>();
        int riddleRound, riddleScore;

        void StartRiddles()
        {
            Did("riddle");
            riddleRound = riddleScore = 0;
            Play(GL.RiddleStart, AskRiddle);
        }

        void AskRiddle()
        {
            int i;
            do i = Random.Range(0, RiddleSet.Length); while (recentRiddles.Contains(i) && recentRiddles.Count < RiddleSet.Length - 1);
            recentRiddles.Enqueue(i);
            while (recentRiddles.Count > 8) recentRiddles.Dequeue();
            var r = RiddleSet[i];
            var opts = r.wrong.Append(r.a).OrderBy(_ => Random.value).ToList();
            Play(new[] { new Line(Who.Mita, r.q, "quest") }, () => ShowMenu("수수께끼", opts,
                opts.Select(o => (Action)(() => RiddleAnswer(o, r.a))).ToList()));
        }

        void RiddleAnswer(string said, string right)
        {
            riddleRound++;
            bool ok = said == right;
            if (ok) riddleScore++;
            var lines = new List<Line> { new Line(Who.Player, said + "?") };
            lines.AddRange(ok ? Pick(GL.Right) : new[] { new Line(Who.Mita, Loc.F("땡~ 정답은 '{0}'!", right), "smiletonque") }.Concat(Pick(GL.Wrong)));
            if (riddleRound < 3) { Play(lines, AskRiddle); return; }
            lines.Add(new Line(Who.Mita, Loc.F("세 문제 중에 {0}개 맞혔어요!", riddleScore), riddleScore >= 2 ? "smile" : "smiletonque"));
            if (riddleScore >= 2) AddAff(2);
            Play(lines, CheckMilestones);
        }

        // ---------------------------------------------------------------- hide and seek (숨바꼭질)

        // places in the house to hide (snapped to the NavMesh)
        static readonly Vector3[] HideSpots =
        {
            new Vector3(13.4f, 0f, 4.6f),    // kitchen, by the fridge
            new Vector3(10.2f, 0f, 0.6f),    // kitchen door corner
            new Vector3(12.9f, 0f, -7.6f),   // bedroom, far corner
            new Vector3(9.6f, 0f, -6.9f),    // bedroom, by the bed's foot
            new Vector3(-3.8f, 0f, -3.0f),   // living room, by the bookcase
            new Vector3(-1.8f, 0f, 3.6f),    // hallway, by the door
            new Vector3(-6.2f, 0f, -4.6f),   // bathroom
        };
        int hideStage;   // 0 none, 1 counting (dark), 2 seeking
        float hideUntil;
        int devHideSpot = -1;   // dev: where the next game hides her (a HideSpots index)

        void StartHide()
        {
            Did("hide");
            EndRoutine();
            StopFollow(false);
            Play(GL.HideStart, () =>
            {
                hideStage = 1;
                fadeTarget = 1f;
                afterFade = () =>
                {
                    // hidden, facing away from where you are
                    var spot = HideSpots[devHideSpot >= 0 && devHideSpot < HideSpots.Length ? devHideSpot : Random.Range(0, HideSpots.Length)];
                    devHideSpot = -1;
                    if (NavMesh.SamplePosition(spot, out var hit, 1.5f, -1)) spot = hit.position;
                    var away = spot - (pm != null ? pm.transform.position : spot + Vector3.back); away.y = 0;
                    PoseMitaUpright(spot, away.sqrMagnitude > 0.01f ? away : Vector3.forward);
                    try { mitaLook?.Activation(false); } catch { }
                    HideCrouch();
                    Plugin.Log.LogInfo($"[hide] Mita hides at {spot}");
                    gamePrompt = Loc.T("하나… 둘… 셋… 열!");
                    hideUntil = Time.time + 4f;
                };
            });
        }

        void TickHide()
        {
            if (hideStage == 0 || afterFade != null) return;
            if (hideStage == 1)
            {
                if (Time.time < hideUntil) return;
                hideStage = 2;
                hideUntil = Time.time + 150f;
                fadeTarget = 0f;
                return;
            }
            int left = Mathf.Max(0, Mathf.CeilToInt(hideUntil - Time.time));
            gamePrompt = Loc.F("숨바꼭질 — {0}를 찾아라!  {1}:{2:00}", ShortName, left / 60, left % 60);
            if (PlayerNearMita(2.2f, 45f)) { EndHide(true); return; }
            if (Time.time > hideUntil) EndHide(false);
        }

        void EndHide(bool found)
        {
            hideStage = 0;
            gamePrompt = null;
            var at = mitaBody != null ? mitaBody.position : Vector3.zero;
            ReleaseClip();
            ReleasePose(at, pm != null ? pm.transform.position : at);
            if (found) { AddAff(3); Play(GL.HideFound, CheckMilestones); }
            else Play(GL.HideTimeout);
            Plugin.Log.LogInfo("[hide] " + (found ? "found" : "time's up"));
        }

        // ---------------------------------------------------------------- 참참참

        int chamStage, chamRound, chamDodged;
        float chamAt, chamYaw;
        bool chamLeft;
        GameObject chamTarget;

        void StartCham()
        {
            Did("cham");
            chamRound = chamDodged = 0;
            Play(GL.ChamStart, NextCham);
        }

        void NextCham()
        {
            chamStage = 1;
            chamAt = Time.time + 0.6f;
            chamLeft = Random.value < 0.5f;
            gamePrompt = Loc.T("참…");
        }

        // three beats, then she points her face left or right: turn your head the other way
        public static bool DevChamAuto;   // dev: turn the head by itself after "참!" (dodging two times in three)
        bool chamTurned;

        void TickCham()
        {
            if (DevChamAuto && chamStage == 3 && !chamTurned && Time.time > chamAt - 0.9f && pm != null)
            {
                chamTurned = true;
                bool dodge = chamRound != 1;
                pm.rotX += (chamLeft ^ !dodge ? -65f : 65f);
            }
            if (chamStage == 0 || Time.time < chamAt) return;
            switch (chamStage)
            {
                case 1: gamePrompt = Loc.T("참… 참…"); chamStage = 2; chamAt = Time.time + 0.7f; break;
                case 2:
                    gamePrompt = Loc.T("참! (고개를 돌려요!)");
                    chamStage = 3; chamAt = Time.time + 1.3f; chamTurned = false;
                    chamYaw = pm != null ? pm.rotX : 0f;
                    // her face turns to her side
                    if (chamTarget == null) chamTarget = new GameObject("PeaceChamTarget");
                    if (mitaBody != null && pm != null)
                    {
                        var toPlayer = pm.transform.position - mitaBody.position; toPlayer.y = 0;
                        var side = Vector3.Cross(Vector3.up, toPlayer.normalized) * (chamLeft ? -1f : 1f);
                        chamTarget.transform.position = mitaBody.position + toPlayer.normalized * 1.5f + side * 1.5f + Vector3.up * 1.4f;
                        try { mitaLook?.LookOnObject(chamTarget.transform); } catch { }
                    }
                    break;
                case 3:
                {
                    chamStage = 0;
                    gamePrompt = null;
                    float turned = pm != null ? Mathf.DeltaAngle(chamYaw, pm.rotX) : 0f;
                    // her left is your right: facing each other, turning your head to your left dodges her "left"
                    int you = Mathf.Abs(turned) < 20f ? 0 : (turned < 0 ? -1 : 1);   // -1 your left, 1 your right
                    int her = chamLeft ? 1 : -1;   // her left seen from you is your right
                    chamRound++;
                    bool dodged = you != 0 && you != her;
                    if (dodged) chamDodged++;
                    try { mitaLook?.LookOnPlayer(); } catch { }
                    var line = you == 0 ? new Line(Who.Mita, "고개를 안 돌렸잖아요! 제 승리!", "smiletonque")
                             : dodged ? new Line(Who.Mita, chamLeft ? "제 왼쪽이었는데… 피했네요!" : "제 오른쪽이었는데… 피했네요!", "surprise")
                             : new Line(Who.Mita, "잡았다! 같은 쪽이에요!", "smile");
                    Plugin.Log.LogInfo($"[cham] round {chamRound}: turned {turned:0} ({you}), her {her} -> {(dodged ? "dodged" : "caught")}");
                    if (chamRound < 3) { Play(new[] { line }, NextCham); break; }
                    var end = new List<Line> { line };
                    if (chamDodged >= 2) { AddAff(2); end.AddRange(Pick(GL.Lose)); } else end.AddRange(Pick(GL.Win));
                    Play(end, CheckMilestones);
                    break;
                }
            }
        }

        bool GameRunning => hideStage != 0 || chamStage != 0 || danceStage != 0 || hammerStage != 0 || lapStaged;

        void TickGames()
        {
            TickFollow();
            TickRequest();
            TickHide();
            TickCham();
            TickLap();
        }

        public void DevGame(string what)
        {
            switch (what)
            {
                case "follow": StartFollow(); break;
                case "stay": StopFollow(true); break;
                case "request": nextRequest = 0f; request = null; TryRequest(); break;
                case var r when r.StartsWith("request:"): devRequest = r.Substring(8); nextRequest = 0f; request = null; TryRequest(); devRequest = null; break;
                case "words": StartWordChain(); break;
                case "riddle": StartRiddles(); break;
                case "hide": StartHide(); break;
                case var h when h.StartsWith("hidespot "): devHideSpot = int.Parse(h.Substring(9)); break;   // hidespot <n>: the next game hides her there
                case "found": if (hideStage == 2) EndHide(true); break;
                case "cham": StartCham(); break;
                case "chamauto": DevChamAuto = true; break;
                case "diary": Play(Diary()); break;
                case "window": DoWindow(); break;
                case var w when w.StartsWith("window "):
                {
                    // window px pz mx mz lx lz: try another window spot
                    var f = w.Substring(7).Split(' ').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    WindowPlayer = new Vector3(f[0], 0f, f[1]); WindowMita = new Vector3(f[2], 0f, f[3]); WindowLook = new Vector3(f[4], 1.6f, f[5]);
                    DoWindow();
                    break;
                }
                case "lap": DoLap(); break;
                case "song": DoSong(); break;
                case "heart": DoHeart(); break;
            }
        }
        public string DevGameState() => $"follow {following} request {(request != null ? request.spot + (requestDone ? " (done)" : "") : "-")} hide {hideStage} cham {chamStage} prompt '{gamePrompt}'";
    }
}
