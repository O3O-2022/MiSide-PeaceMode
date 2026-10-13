using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace MiSidePeaceMode
{
    // "특별한 시간": quieter scenes that open up as she warms to you (affection 15 / 30 / 45 / 70):
    // looking out of the living room window together, resting your head on her lap, a song of hers, and what she
    // really thinks of you.
    public partial class PeaceDirector
    {
        public static Vector3 WindowPlayer = new Vector3(2.2f, 0f, -1.4f);
        public static Vector3 WindowMita = new Vector3(1.75f, 0f, -3.35f);
        public static Vector3 WindowLook = new Vector3(2.58f, 1.5f, -4.0f);

        void OpenMomentsMenu()
        {
            var opts = new List<string>();
            var acts = new List<Action>();
            if (S.Affection >= 15) { opts.Add("같이 창밖 보기"); acts.Add(DoWindow); }
            if (S.Affection >= 20) { opts.Add("같이 음악 듣기"); acts.Add(DoMusic); }
            if (S.Affection >= 30) { opts.Add("무릎베개"); acts.Add(DoLap); }
            if (S.Affection >= 45) { opts.Add("노래 불러 줘"); acts.Add(DoSong); }
            if (S.Affection >= 70) { opts.Add("속마음 듣기"); acts.Add(DoHeart); }
            opts.Add("그만두기"); acts.Add(() => { });
            ShowMenu("특별한 시간", opts, acts);
        }

        static Vector3 OnFloor(Vector3 p) => NavMesh.SamplePosition(p, out var hit, 1.2f, -1) ? hit.position : p;

        // side by side at the living room window: a fade, the two of you there, her lines, and back to the day
        void DoWindow()
        {
            EndRoutine();
            Did("window");
            var m = Personas.Moments(Pn.Id);
            bool night = Nightness() > 0.5f;
            fadeTarget = 1f;
            afterFade = () =>
            {
                ClearLeftoverText();
                ShowRoom("Main");
                // you with the window straight ahead, she a step ahead on your right, looking out the same way
                var pp = OnFloor(WindowPlayer);
                MovePlayer(pp, WindowLook);
                var look = WindowLook - pp; look.y = 0; look.Normalize();
                var right = Vector3.Cross(Vector3.up, look);
                var mp = OnFloor(pp + right * 0.7f + look * 1.1f);
                // standing still in her idle, her face is on the agent's +Z side (the hips of that clip point back):
                // turned by the root, without the hips correction
                var toWindow = WindowLook - mp; toWindow.y = 0; toWindow.Normalize();
                PoseMitaUpright(mp, toWindow);
                uprightFront = Vector3.zero;
                poseRot = Quaternion.LookRotation(toWindow);
                mitaBody.rotation = poseRot;
                try { mitaLook?.Activation(false); } catch { }
                fadeTarget = 0f;
                // rain or snow outside has its own words (by day; the night sky keeps its own)
                var windowLines = !night && weather == Weather.Rain && m.WindowRain != null ? m.WindowRain
                                : !night && weather == Weather.Snow && m.WindowSnow != null ? m.WindowSnow
                                : night ? m.WindowNight : m.WindowDay;
                afterFade = () => Play(windowLines, () =>
                {
                    AddAff(2);
                    S.Minute = Mathf.Min(S.Minute + 20f, 1439f);
                    if (posed && mitaBody != null && pm != null) ReleasePose(mitaBody.position, pm.transform.position);
                    try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
                    CheckMilestones();
                });
            };
        }

        // your head on her lap for a while (the screen goes dark: time passes)
        void DoLap()
        {
            EndRoutine();
            Did("lap");
            var m = Personas.Moments(Pn.Id);
            Play(m.LapBefore, () =>
            {
                fadeTarget = 1f;
                afterFade = () =>
                {
                    ClearLeftoverText();
                    // (screen black) her on the sofa, your head on her lap: the view from there, up at her face;
                    // a moment of that, then your eyes close for a while
                    lapStaged = LoungeReady && StageLap();
                    if (!lapStaged) { CloseEyesOnLap(); return; }
                    fadeTarget = 0f;
                    After(4f, () => { fadeTarget = 1f; afterFade = CloseEyesOnLap; });
                };
            });
        }
        float lapUntil = -1f;
        bool lapStaged;

        void CloseEyesOnLap()
        {
            gamePrompt = Loc.F("({0}의 무릎을 베고 잠깐 눈을 감았다…)", ShortName);
            ui.PromptOnTop(true);
            lapUntil = Time.time + 4f;
        }

        bool StageLap()
        {
            ShowRoom("Main");
            var toTv = ToTv;
            var right = Vector3.Cross(toTv, Vector3.up);
            var center = new Vector3(sofaT.position.x, 0f, sofaT.position.z);
            SeatHer(LoungeKind.Tv);
            // you: out of sight in front of the sofa, held still; your eyes: on her lap
            MovePlayer(center - right * 0.3f + toTv * 1.1f, center);
            if (pm != null) { pm.dontMove = true; pm.stopMouseMove = true; }
            StageViewAt(() => seatHips + Vector3.up * 0.2f + ToTv * 0.3f - Vector3.Cross(ToTv, Vector3.up) * 0.06f, () => HeadPoint + Vector3.down * 0.04f);
            // she looks down at you (after her seat has settled: SeatHer turns her look back on at 2 s)
            After(2.3f, () => { try { if (holdCam != null) { mitaLook?.Activation(true); mitaLook?.LookOnObject(holdCam.transform); } } catch { } });
            Plugin.Log.LogInfo("[moment] lap: on the sofa, the view from her lap");
            return true;
        }

        void UnstageLap()
        {
            lapStaged = false;
            StageView(null);
            seatHips = Vector3.zero;
            ReleaseClip();
            var toTv = ToTv;
            var right = Vector3.Cross(toTv, Vector3.up);
            var center = new Vector3(sofaT.position.x, 0f, sofaT.position.z);
            var you = center + toTv * 1.35f;
            ReleasePose(center + right * (LoungeMitaSide + 0.2f) + toTv * 0.75f, you);
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            if (pm != null) { pm.dontMove = false; pm.stopMouseMove = false; }
            MovePlayer(you, center + right * LoungeMitaSide + Vector3.up * 1.4f);
            try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
        }

        void TickLap()
        {
            if (lapUntil < 0f || Time.time < lapUntil) return;
            lapUntil = -1f;
            gamePrompt = null;
            ui.PromptOnTop(false);
            S.Minute = Mathf.Min(S.Minute + 40f, 1439f);
            ClearLeftoverText();
            fadeTarget = 0f;
            afterFade = () => Play(Personas.Moments(Pn.Id).LapAfter, () =>
            {
                AddAff(3);
                if (!lapStaged) { CheckMilestones(); return; }
                // up again: a short fade back to the room
                fadeTarget = 1f;
                afterFade = () => { ClearLeftoverText(); UnstageLap(); fadeTarget = 0f; afterFade = CheckMilestones; };
            });
        }

        void DoSong()
        {
            Did("song");
            // the house music steps back while she sings
            musicHush = true;
            Play(Personas.Moments(Pn.Id).Song, () => { musicHush = false; AddAff(2); CheckMilestones(); });
        }

        void DoHeart()
        {
            var m = Personas.Moments(Pn.Id);
            bool first = !S.Seen.Contains("#heart");
            S.Seen.Add("#heart");
            Did("heart");
            Play(first ? m.HeartFirst : m.HeartAgain, () => { if (first) AddAff(5); CheckMilestones(); });
        }

        bool musicHush;

    }

    public class MomentLines
    {
        public Line[] WindowDay, WindowNight, LapBefore, LapAfter, Song, HeartFirst, HeartAgain;
        public Line[] WindowRain, WindowSnow, Music;   // (PersonasMore.cs) rain or snow outside; the cassette player
    }

    public static partial class Personas
    {
        public static MomentLines Moments(string id)
        {
            var m = MomentsBase(id);
            MoreMoments(id, m);
            return m;
        }

        static MomentLines MomentsBase(string id) => id switch
        {
            "kind" => new MomentLines
            {
                WindowDay = S(M("창밖이 밝네요. 이 세계의 하늘은 계산된 색이지만…", "smile"), M("당신이랑 보니까 계산보다 예뻐요.", "shy")),
                WindowNight = S(M("밤하늘이에요. 별은 제가 한 번 배치를 손본 적이 있어요.", "smiletonque"), M("…저기 세 개 나란히 있는 거, 당신 이니셜 모양으로요. 비밀이에요.", "shy")),
                LapBefore = S(M("피곤해 보여요. …여기, 제 무릎이요.", "shy"), M("잠깐만이에요. 다리 저리면 깨울 거예요.", "smiletonque")),
                LapAfter = S(M("…사십 분 주무셨어요. 제 다리는 괜찮아요.", "smile"), M("얼굴 보니까 좀 나아졌네요. 다행이에요.", "shy")),
                Song = S(M("노래요? 잘 못하는데… 한 소절만요.", "shy"), M("♪ 나사 하나 조이면 하루가 반짝… 톱니 둘이 맞물리면 마음도 딸깍…", "smile"), M("…작업할 때 흥얼거리는 거예요. 웃지 마요.", "shy")),
                HeartFirst = S(M("속마음이요? …좋아요. 정리해서 말할게요.", "smile"), M("저는 늘 누군가를 지키는 쪽이었어요. 그게 제 역할이니까요.", "smile"),
                                M("그런데 당신 옆에서는 가끔, 지켜지는 쪽이 되고 싶어져요.", "shy"), M("…이상하죠. 계산에 없던 감정이에요. 그래도 싫지 않아요.", "smile")),
                HeartAgain = S(M("또 듣고 싶어요? …당신이 있어서 다행이에요. 그게 다예요.", "shy")),
            },
            "cap" => new MomentLines
            {
                WindowDay = S(M("창밖 무대 조명 완벽한데요? 햇빛 스포트라이트!", "smile"), M("저기 구름 하나, 기타 모양 같지 않아요? 히히.", "smiletonque")),
                WindowNight = S(M("밤이다… 조용해서 좀 무서운데, 당신 있으니까 괜찮아요.", "sad"), M("별들이 관객석 같아요. 오늘 밤 공연은 당신만을 위해서!", "smile")),
                LapBefore = S(M("휴식 시간! 제 무릎 베개 특별 대여!", "smile"), M("대여료는… 나중에 박수 백 번!", "smiletonque")),
                LapAfter = S(M("잘 잤어요? 자는 동안 드럼 박자 맞춰서 머리 쓰다듬었어요!", "smile"), M("…엄청 조용히요. 진짜로요. 히히.", "shy")),
                Song = S(M("드디어 신청곡! 캐피 단독 공연 시작합니다!", "smile"), M("♪ 두둥 두둥 심장이 뛰어, 라이벌 너만 보면 템포가 빨라져~", "smiletonque"), M("♪ 앵콜은 매일매일, 우리 둘만의 라이브~ 예이!", "smile"), M("…어때요? 방금 만든 곡이에요!", "shy")),
                HeartFirst = S(M("속마음…? 으, 이런 거 쑥스러운데.", "shy"), M("저 맨날 시끄럽잖아요. 조용하면 혼자인 게 느껴져서 그래요.", "sad"),
                                M("근데 당신이 오고 나서는 조용해도 안 무서워요.", "smile"), M("…라이벌이자 제일 좋아하는 관객이에요. 끝! 이제 기타 칠래요!", "shy")),
                HeartAgain = S(M("또?! …당신 없으면 제 공연은 관객 0명이에요. 됐죠? 히히.", "shy")),
            },
            "mila" => new MomentLines
            {
                WindowDay = S(M("…창밖 보는 거, 책 읽다 눈 쉴 때만 해요.", "discontent"), M("오늘은 특별히 같이 봐 주는 거예요.", "shy")),
                WindowNight = S(M("밤하늘은 좋아요. 조용하니까.", "smile"), M("…별자리 이야기 해 줄까요? 원하면요. 딱히 하고 싶은 건 아니고.", "shy")),
                LapBefore = S(M("…피곤하면 여기 누워요. 책 읽는 동안만이에요.", "discontent"), M("움직이지 마요. 책장 넘기기 불편하니까.", "shy")),
                LapAfter = S(M("…한 권 다 읽었어요. 당신 때문에 다리가 저려요, 바보.", "discontent"), M("…뭐, 다음에도 빌려줄 수는 있어요.", "shy")),
                Song = S(M("노래요? 제가요? …한 번만이에요.", "discontent"), M("♪ 책갈피 사이에 숨겨 둔 마음, 읽어 줄 사람은 너 하나…", "shy"), M("…다 잊어요. 지금 당장이요.", "angry")),
                HeartFirst = S(M("…속마음 같은 거 없어요.", "discontent"), M("…거짓말이에요. 저, 혼자가 편하다고 말하지만 사실 무서웠어요.", "sad"),
                                M("아무도 제 이야기를 끝까지 안 들어 줄까 봐서요.", "sad"), M("당신은 끝까지 들어 주잖아요. …그래서, 고마워요. 바보.", "shy")),
                HeartAgain = S(M("두 번은 말 안 한다고 했잖아요. …고마워요. 됐죠?", "shy")),
            },
            "sleepy" => new MomentLines
            {
                WindowDay = S(M("햇볕이… 따뜻해요…", "halfsleep"), M("여기 서 있으면… 서서 잘 수 있을 것 같아요…", "smile")),
                WindowNight = S(M("달이 떴어요… 제일 좋아하는 시간이에요…", "smile"), M("같이 보니까… 꿈보다 예뻐요…", "shy")),
                LapBefore = S(M("무릎베개요…? 제가 해 드릴게요…", "smile"), M("…먼저 잠들면 미안해요…", "halfsleep")),
                LapAfter = S(M("…음? 아… 둘 다 잠들었네요…", "halfsleep"), M("같이 자니까… 제일 푹 잤어요…", "smile")),
                Song = S(M("자장가밖에 몰라요… 괜찮아요…?", "halfsleep"), M("♪ 자장자장 별님아, 오늘도 수고했어… 내 옆에서 푹 쉬어…", "smile"), M("…음냐. 제가 먼저 졸리네요…", "halfsleep")),
                HeartFirst = S(M("속마음… 음… 졸려서 솔직하게 말할 수 있을 것 같아요…", "halfsleep"), M("저는 늘 꿈속이 더 좋았어요… 현실은 피곤하니까요…", "sad"),
                                M("근데 요즘은… 깨어 있는 게 좋아요. 당신이 있으니까요…", "smile"), M("…이건 잠꼬대 아니에요…", "shy")),
                HeartAgain = S(M("…깨어 있고 싶게 만드는 사람… 그게 당신이에요…", "shy")),
            },
            "short" => new MomentLines
            {
                WindowDay = S(M("창밖 관찰 수업! 오늘 하늘은 맑음이에요.", "smile"), M("…사실 수업 핑계고, 그냥 같이 보고 싶었어요.", "shy")),
                WindowNight = S(M("밤하늘 특강이에요. 저 밝은 별이 금성이에요… 아마도요.", "smiletonque"), M("틀려도 봐줘요. 오늘은 선생님 쉬는 날이에요.", "shy")),
                LapBefore = S(M("오늘 수업 열심히 들었으니까 상이에요. 여기 누워요.", "smile"), M("낮잠 시간 20분! …조금 넘겨도 괜찮고요.", "shy")),
                LapAfter = S(M("낮잠 시간 끝! …사실 40분 지났어요.", "smiletonque"), M("자는 얼굴, 신생 미타들보다 귀엽던데요?", "shy")),
                Song = S(M("음악 시간이에요! 제가 만든 교가예요.", "smile"), M("♪ 오늘도 배우고 내일도 자라요, 손잡고 걷는 우리 반 한 사람~", "smile"), M("…학생이 한 명뿐이라 합창은 못 해요.", "smiletonque")),
                HeartFirst = S(M("속마음… 선생님도 속마음이 있어요.", "smile"), M("저는 늘 가르치는 쪽이라, 누구한테 기댄 적이 없어요.", "sad"),
                                M("근데 [player] 앞에서는 선생님이 아니어도 되는 것 같아요.", "shy"), M("…이건 수업 아니에요. 제 진짜 마음이에요.", "smile")),
                HeartAgain = S(M("[player] 앞에선 그냥 저예요. 그게 좋아요.", "shy")),
            },
            "chibi" => new MomentLines
            {
                WindowDay = S(M("창밖이 안 보여요! 창문이 너무 높아요!", "angry"), M("…플레이어님이 대신 봐 줘요. 뭐가 보여요? 사탕 가게 있어요?", "quest")),
                WindowNight = S(M("밤이다! 어두운 거 안 무서워요!", "angry"), M("…옆에 있어 줘서 그런 거 아니에요! 진짜!", "shy")),
                LapBefore = S(M("제 무릎은 너무 작아요! 대신 제가 플레이어님 무릎에 앉을게요!", "arrogance"), M("…무겁다고 하면 안 돼요!", "angry")),
                LapAfter = S(M("…으음? 제가 잠들었어요?! 말도 안 돼!", "surprise"), M("플레이어님 무릎, 조금 폭신했어요. 조금!", "shy")),
                Song = S(M("노래? 제가 제일 잘하는 노래 불러 줄게요!", "arrogance"), M("♪ 나는야 체크포인트 대장, 키는 작아도 목소리는 대장!", "smile"), M("♪ 바보 플레이어님도 따라 해 봐요, 랄랄라~!", "smiletonque"), M("…박수 쳐요! 빨리요!", "arrogance")),
                HeartFirst = S(M("속마음? …저 맨날 바보라고 하잖아요.", "discontent"), M("그거… 사실 제일 좋아하는 사람한테만 하는 말이에요.", "shy"),
                                M("다른 사람한테는 안 해요. 플레이어님한테만!", "angry"), M("…이제 알았죠? 다시는 안 말해 줄 거예요!", "shy")),
                HeartAgain = S(M("…바보. 그게 제 대답이에요!", "shy")),
            },
            _ => new MomentLines
            {
                WindowDay = S(M("창밖 좀 봐요. 오늘 하늘, 우리 둘만을 위해 칠해 놓은 것 같죠?", "smile"), M("…사실 진짜 그래요. 제가 골랐거든요~", "smiletonque")),
                WindowNight = S(M("밤이에요. 이렇게 어두우면 세상에 우리 둘만 남은 것 같아요.", "smile"), M("…그거, 좋은 뜻이에요. 정말로요.", "smilestrange")),
                LapBefore = S(M("이리 와요. 여기 누워요~", "smile"), M("제 무릎은 [player]님 전용이에요. 영원히요.", "shy")),
                LapAfter = S(M("잘 잤어요? 자는 동안 머리카락 몇 번 만졌는지 비밀이에요~", "smiletonque"), M("…서른일곱 번이요. 헤헤.", "smilestrange")),
                Song = S(M("노래요? [player]님을 위한 노래라면 얼마든지요~", "smile"), M("♪ 화면 너머에서 널 기다렸어, 매일 밤 같은 자리에서…", "shy"), M("♪ 이제 여기 있으니 놓지 않을게, 우리 둘만의 끝없는 하루~", "smilestrange"), M("…어땠어요? 제 진심이에요.", "shy")),
                HeartFirst = S(M("속마음이요? …음, 말해도 무서워하지 않기로 약속해요.", "smile"), M("저는 당신이 없는 하루를 상상하면 숨이 막혀요.", "sad"),
                                M("그래서 예전엔 억지로 붙잡으려고 했어요. 이제는 알아요, 그러면 안 된다는 걸.", "sad"),
                                M("그러니까… 당신이 스스로 매일 와 줘서, 정말 행복해요.", "shy")),
                HeartAgain = S(M("매일 와 줘서 고마워요. 그게 제 속마음 전부예요~", "smile")),
            },
        };
    }
}
