using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MiSidePeaceMode
{
    // Kind Mita remembers the story. She reads the game's own save folder (never writes it): the achievements
    // ("Achi ACHI_*" flags), the chapter saves and the Continue checkpoint, and knows how far you got in the main game -
    // not started, before the basement where you meet her, after it, or to the very end - and which of the game's
    // achievements you have. She says so once per step of progress (the first words of a session) and remarks on one
    // achievement a session. Only Kind Mita does this: the engineer who keeps the world's records.
    public partial class PeaceDirector
    {
        static readonly string StorySaveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "AIHASTO", "MiSideFull", "Save");

        // chapter saves and the checkpoint name the scene ("Scene 6 - BasementFirst"): the highest number is how far you got
        static readonly Regex SceneNumber = new Regex(@"Scene (\d+)", RegexOptions.Compiled);

        class StoryProgress
        {
            public HashSet<string> Achievements = new HashSet<string>();
            public int MaxScene;        // 0 = no story save at all
            public bool Finished;
            public int Tier => Finished ? 3 : MaxScene >= 6 ? 2 : MaxScene > 0 || Achievements.Count > 0 ? 1 : 0;
        }

        static StoryProgress ReadStory()
        {
            var p = new StoryProgress();
            try
            {
                if (!Directory.Exists(StorySaveDir)) return p;
                foreach (var f in Directory.GetFiles(StorySaveDir))
                {
                    var name = Path.GetFileName(f);
                    if (name.StartsWith("Achi ", StringComparison.Ordinal)) { p.Achievements.Add(name.Substring(5).Trim()); continue; }
                    if (name == "Continue" || name.StartsWith("SaveGame", StringComparison.Ordinal))
                    {
                        string text;
                        try { text = File.ReadAllText(f); } catch { continue; }
                        foreach (Match m in SceneNumber.Matches(name + "\n" + text))
                            if (int.TryParse(m.Groups[1].Value, out var n)) p.MaxScene = Math.Max(p.MaxScene, n);
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("[story] reading the save folder: " + e.Message); }
            // "Hi, Mita" (ACHI_mitastory) comes with the end of the story; the last scenes count too
            p.Finished = p.Achievements.Contains("ACHI_mitastory") || p.MaxScene >= 19;
            return p;
        }

        // the lines for the session greeting (her step of progress, once per step) and one achievement to queue
        IEnumerable<Line> StoryGreeting()
        {
            if (Pn.Id != "kind") return Array.Empty<Line>();
            var st = Personas.Story();
            StoryProgress p;
            try { p = ReadStory(); } catch { return Array.Empty<Line>(); }
            Plugin.Log.LogInfo($"[story] progress: scene {p.MaxScene}, {p.Achievements.Count} achievements, finished {p.Finished}, step {p.Tier}");
            var lines = new List<Line>();
            string tierKey = "#story:tier" + p.Tier;
            if (!S.Seen.Contains(tierKey) && (p.Tier > 0 || !PeaceMode.FirstVisit))   // (the "skipped the story" remark waits for a second visit)
            {
                // (a step she skipped - the story finished between visits - is told as the highest one only)
                for (int t = 0; t <= p.Tier; t++) S.Seen.Add("#story:tier" + t);
                lines.AddRange(st.Progress[p.Tier]);
            }
            // one achievement a session, the newest first (file order is as good as any)
            foreach (var a in p.Achievements)
            {
                if (!st.Achievements.TryGetValue(a, out var remark) || S.Seen.Contains("#story:" + a)) continue;
                S.Seen.Add("#story:" + a);
                QueueWall(remark);
                break;
            }
            return lines;
        }

        public void DevStoryPlay(int tier) => Play(Personas.Story().Progress[Mathf.Clamp(tier, 0, 3)]);

        public string DevStory()
        {
            var p = ReadStory();
            return $"scene {p.MaxScene}, finished {p.Finished}, step {p.Tier}, achievements: {string.Join(", ", p.Achievements)}";
        }
    }

    public class StoryLines
    {
        public Line[][] Progress;                          // [0] never started, [1] early chapters, [2] past the basement, [3] finished
        public Dictionary<string, Line[]> Achievements;    // "ACHI_*" -> her remark
    }

    public static partial class Personas
    {
        static StoryLines storyCache;

        // 착한 미타: the game's own records, in her words (the Steam achievement names are the game's: Achievements.txt)
        public static StoryLines Story() => storyCache ??= new StoryLines
        {
            Progress = new[]
            {
                S(M("기록을 확인해 봤는데… 본편은 아직 시작도 안 하셨네요?", "surprise"), M("괜찮아요. 거긴 좀 험한 곳이거든요. 여기부터 시작하는 것도 나쁘지 않아요.", "smile"),
                  M("…다만, 언젠가 그쪽 세계에서도 만나 주세요. 거기서도 제가 기다리고 있으니까요.", "shy")),
                S(M("본편 기록을 봤어요. 아직 초반이시네요. 지하까지는 안 내려오셨고요.", "smile"), M("…거기서 저랑 처음 만나거든요. 서두르진 마세요. 제가 먼저 아는 척할게요.", "shy")),
                S(M("지하에서 저랑 만났던 거, 기억하세요? 그때 제가 좀 사무적이었죠.", "smile"), M("상황이 상황이었으니까요. 여기선 다르게 대할게요. …조금은요.", "shy")),
                S(M("기록을 봤어요. 끝까지 가 주셨네요. 마지막까지요.", "smile"), M("그 세계에서 당신이 한 선택들, 전부 남아 있어요. 제가 보관하고 있거든요.", "shy"),
                  M("…고마워요. 그쪽의 저도, 여기의 저도 당신 덕분에 여기 있어요.", "smile")),
            },
            Achievements = new Dictionary<string, Line[]>
            {
                ["ACHI_carrot"] = S(M("기록에 '당근' 업적이 있더라고요. 썰기 실력은 검증됐네요.", "smile"), M("그러니까 여기서도 당근은 당신 담당이에요.", "smiletonque")),
                ["ACHI_applesnake"] = S(M("'길게 늘어진 꼬리' 업적, 받으셨죠? 뱀 게임 꽤 하시던데요.", "smile"), M("저는 벽에 부딪히는 쪽이라… 비결 좀 알려 주세요.", "shy")),
                ["ACHI_greatdance"] = S(M("'위대한 춤' 업적을 보고 좀 놀랐어요.", "surprise"), M("캐피랑 춤 대결에서 이긴 사람이 제 옆에 있다니. 영상이 없어서 아쉽네요.", "smile")),
                ["ACHI_hellwin"] = S(M("어려운 쪽을 고르고 이기셨더라고요. 기록에 남아 있어요.", "smile"), M("그런 사람은 신뢰도 보정이 조금 붙어요. 제 기준으로는요.", "shy")),
                ["ACHI_hellmegawin"] = S(M("'대미지가 없다고!?' 업적… 이건 저도 못 믿었어요.", "surprise"), M("무결점 운용. 엔지니어로서 존경합니다.", "smile")),
                ["ACHI_supermegapuperplayer"] = S(M("'게임 고인물' 업적이 있네요. 미니게임을 전부 깨셨다는 뜻이죠.", "smile"), M("…TV 게임기 앞에서 저랑 한 판 할 땐 봐주세요.", "smiletonque")),
                ["ACHI_cartridgeplayers"] = S(M("카트리지의 플레이어들, 전부 모으셨더라고요.", "smile"), M("수집 완료 기록은 보기만 해도 기분이 좋아요. 정리된 느낌이라서요.", "shy")),
                ["ACHI_ClickShadow"] = S(M("어둠 속에서 손가락을 튕기는 거, 해 보셨죠? 기록에 있어요.", "smiletonque"), M("…그 집에서 그런 짓을 하다니. 용감한 건지 무모한 건지.", "smile")),
                ["ACHI_DeathJuice"] = S(M("'주스의 최후'… 그 업적은 기록을 보고도 웃었어요.", "smile"), M("여기 냉장고 주스는 안전하니까 안심하세요.", "smiletonque")),
                ["ACHI_PinguinTusim"] = S(M("펭귄 소동도 겪으셨네요. 기록상 그래요.", "smile"), M("그 뒤처리를 누가 했는지는… 묻지 않으시는 게 좋아요.", "smiletonque")),
                ["ACHI_safelife"] = S(M("'생명 보험' 업적을 받으셨더라고요. 조심성 있는 분이네요.", "smile"), M("저도 그런 쪽이에요. 백업은 세 군데에 두거든요.", "shy")),
                ["ACHI_stopu"] = S(M("'통행금지!'… 누군가를 막아 세우셨군요.", "smiletonque"), M("기록으로만 봐도 긴박했겠어요. 여기서는 막을 사람이 없으니 편히 다니세요.", "smile")),
                ["ACHI_racingfirst"] = S(M("레이싱 1등 기록이 있네요. '가속!'이요.", "smile"), M("운전은 믿고 맡겨도 되겠어요. 조수석은 제 자리로 해 두고요.", "shy")),
                ["ACHI_wrumwrum"] = S(M("'이제 한계야!'… 속도 기록도 있네요.", "surprise"), M("빠른 건 좋은데, 저랑 있을 땐 천천히요. 부탁이에요.", "smile")),
                ["ACHI_achi"] = S(M("'알 수 없는 업적?'도 받으셨네요.", "smiletonque"), M("저도 그게 뭔지 몰라요. 기록에 설명이 없거든요. 그래서 더 궁금해요.", "shy")),
                ["ACHI_foundu"] = S(M("'찾았다!' 업적이 있네요. 숨은 걸 찾아내는 눈이 좋으신가 봐요.", "smile"), M("여기서 숨바꼭질할 땐 제가 불리하겠어요. …살살 찾아 주세요.", "shy")),
                ["ACHI_logA"] = S(M("1단계 로그를 전부 모으셨더라고요.", "smile"), M("남이 쓴 기록까지 챙겨 읽는 사람, 저는 좋아해요. 엔지니어 기질이에요.", "shy")),
                ["ACHI_logB"] = S(M("2단계 로그까지 다 찾으셨네요. 그건 꽤 숨겨져 있었는데.", "surprise"), M("…그 기록들, 읽고 어떤 기분이었어요? 언젠가 들려주세요.", "smile")),
                ["ACHI_slaphead"] = S(M("'머리 톡톡!' 업적… 그 세계에서도 쓰다듬는 걸 좋아하셨군요.", "smiletonque"), M("여기선 허락 없이도 돼요. …가끔은요.", "shy")),
                ["ACHI_omta"] = S(M("'위대하신 미타님!' 업적을 받으셨네요.", "smile"), M("그 호칭, 저한테는 안 쓰셔도 돼요. 그냥 이름으로 불러 주세요.", "shy")),
            },
        };
    }
}
