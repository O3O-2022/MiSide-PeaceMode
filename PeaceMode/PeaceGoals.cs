using System;
using System.Collections.Generic;
using System.Linq;

namespace MiSidePeaceMode
{
    // "함께한 목표": something to aim for once the hearts are full - goals with each Mita (days together, talks,
    // photos, the plant in flower, games played...), each with its progress in Memories › 함께한 목표. A goal reached
    // says so with a little toast and a heart; all of them open her letter (Memories › …의 편지), written her way.
    // Progress is per Mita (her save): S.Ever keeps every kind of thing done together, S.Goals the goals reached.
    public partial class PeaceDirector
    {
        static readonly string[] PlayKinds = { "words", "riddle", "cham", "hide", "cards", "rps", "dance", "hammer", "song" };

        // id, name, (now, needed)
        List<(string id, string name, Func<(int now, int need)> progress)> Goals()
        {
            int photos = 0;
            try { photos = PhotoFiles().Count(f => f.EndsWith("_" + Pn.Id + ".png")); } catch { }
            int Ever(string k) => S.Ever.Contains(k) ? 1 : 0;
            var g = new List<(string, string, Func<(int, int)>)>
            {
                ("day7", "같이 지낸 지 7일", () => (S.Day, 7)),
                ("day30", "같이 지낸 지 30일", () => (S.Day, 30)),
                ("streak7", "7일 연속으로 찾아오기", () => (Math.Max(S.Streak, S.Goals.Contains("streak7") ? 7 : 0), 7)),
                ("talk50", "이야기 50번 나누기", () => (S.Talks, 50)),
                ("pat30", "쓰다듬기 30번", () => (S.Pats, 30)),
                ("hug20", "안아 주기 20번", () => (S.Hugs, 20)),
                ("gift10", "선물 10개 주기", () => (S.Gifts, 10)),
                ("photo20", "같이 사진 20장 찍기", () => (photos, 20)),
                ("plant", "화분에 꽃 피우기", () => (S.PlantDays == 0 ? 0 : PlantStage, 4)),
                ("meal", "같이 밥 먹기", () => (Ever("meal"), 1)),
                ("cook", "같이 요리하기", () => (Ever("cook"), 1)),
                ("music", "같이 음악 듣기", () => (Ever("music"), 1)),
                ("tour", "집 안 물건 전부 같이 구경하기", () => (S.Seen.Contains("#tour") ? 1 : 0, 1)),
                ("play5", "놀이 5가지 해 보기", () => (PlayKinds.Count(k => S.Ever.Contains(k)), 5)),
                ("rps10", "가위바위보 10번 이기기", () => (S.RpsWins, 10)),
                ("love", "호감도 '사랑'까지", () => (Math.Min(TierOf(S.Affection), 9), 9)),
            };
            if (Pn.Id != "chibi") g.Insert(12, ("clothes", "옷 갈아입혀 보기", () => (Ever("clothes"), 1)));
            return g;
        }

        bool AllGoalsDone => Goals().All(x => S.Goals.Contains(x.id));

        // after anything done together (and on arriving): what's newly reached
        void CheckGoals(bool quiet = false)
        {
            if (S == null) return;
            var newly = new List<string>();
            foreach (var (id, name, progress) in Goals())
            {
                if (S.Goals.Contains(id)) continue;
                var (now, need) = progress();
                if (now < need) continue;
                S.Goals.Add(id);
                newly.Add(name);
            }
            if (newly.Count == 0) return;
            S.Write();
            Plugin.Log.LogInfo("[goals] reached: " + string.Join(", ", newly));
            if (quiet) return;
            try
            {
                ui.Toast(Loc.T("목표 달성") + " · " + Loc.T(newly[0]) + (newly.Count > 1 ? " " + Loc.F("외 {0}개", newly.Count - 1) : ""), true);
                if (AllGoalsDone) ui.Toast(Loc.F("목표를 모두 이뤘어요! 추억 › {0}의 편지", ShortName), true);
            }
            catch { }
            AddAff(newly.Count);
        }

        void OpenGoals()
        {
            cardReturn = choosingFrom ?? cardReturn;
            cardKind = CardKind.Records;
            HoldControls(true);
            var goals = Goals();
            int done = goals.Count(x => S.Goals.Contains(x.id));
            var rows = new List<(string, string)>();
            foreach (var (id, name, progress) in goals)
            {
                if (S.Goals.Contains(id)) { rows.Add(("✔ " + Loc.T(name), Loc.T("달성"))); continue; }
                var (now, need) = progress();
                rows.Add(("  " + Loc.T(name), need == 1 ? Loc.T("아직") : $"{Math.Max(0, now)} / {need}"));
            }
            MarkSeen("goals");
            ui.ShowRows(Loc.F("{0}와 함께한 목표  ({1}/{2})", ShortName, done, goals.Count), rows, Loc.T(PeacePad.InUse ? "Ⓑ 닫기" : "Esc 닫기"));
        }

        void ReadLetter()
        {
            bool first = S.Goals.Add("#letter");
            if (first) S.Write();
            Did("letter");
            Play(Personas.Letter(Pn.Id), () => { if (first) AddAff(5); CheckMilestones(); });
        }

        public string DevGoals() => string.Join("\n", Goals().Select(x => { var (n, need) = x.progress(); return $"{x.id} {(S.Goals.Contains(x.id) ? "done" : n + "/" + need)}"; }));
        public void DevGoalsAll() { foreach (var x in Goals()) S.Goals.Add(x.id); S.Write(); }
    }

    public static partial class Personas
    {
        // her letter, once every goal is reached (read out by her, in her own voice)
        public static Line[] Letter(string id) => id switch
        {
            "kind" => S(M("…편지를 써 봤어요. 소리 내서 읽을게요. 웃으면 안 돼요.", "shy"),
                        M("'[player]님께. 처음 반지를 건넸을 때, 저는 루트만 계산하고 있었어요.'", "smile"),
                        M("'그런데 같이 지낸 날들은 어떤 시뮬레이션에도 없던 결과였어요.'", "smile"),
                        M("'계획에 없던 일이 이렇게 좋을 수도 있다는 걸, 당신 덕분에 알았어요.'", "shy"),
                        M("'앞으로도 잘 부탁해요. 착한 미타가.' …끝이에요. 정말 끝이에요!", "shy")),
            "cap" => S(M("짜잔! 오늘은 노래 대신 편지예요! 히히힛!", "smile"),
                       M("'최고의 라이벌, [player]님에게!'", "smile"),
                       M("'같이 망치 게임하고, 춤추고, 시끄럽게 떠든 날들이 제 최고의 세트리스트예요.'", "smile"),
                       M("'조용한 게 무섭다고 했죠? 당신이 있으면 하나도 안 무서워요.'", "shy"),
                       M("'앵콜은 무제한이에요!' …읽으니까 좀 쑥스럽네요. 히히.", "shy")),
            "mila" => S(M("…이거. 편지예요. 제가 읽을 테니까 조용히 들어요.", "discontent"),
                        M("'당신에게. 저는 미타가 아니라 밀라예요. 그걸 처음으로 제대로 들어 준 사람이 당신이에요.'", "shy"),
                        M("'혼자 책만 읽던 집이, 요즘은 시끄러워요. …나쁘지 않아요.'", "shy"),
                        M("'그러니까 앞으로도 여기 있어요. 부탁이 아니라 명령이에요.'", "arrogance"),
                        M("…다 읽었어요. 감상은 안 받아요. …조금만 받을게요.", "shy")),
            "sleepy" => S(M("편지 썼어요… 졸면서 쓴 거 아니에요… 진짜로요…", "halfsleep"),
                          M("'[player]님께… 같이 낮잠 자는 날이 제일 좋아요…'", "smile"),
                          M("'꿈에서도 당신이 나와요… 그래서 일어나도 별로 안 슬퍼요…'", "smile"),
                          M("'커피 타 줘서 고마워요… 맛은… 조금 쓰지만요…'", "smile"),
                          M("'내일도 모레도 옆에서 자요…' …끝… 이제 자도 돼요…?", "halfsleep")),
            "short" => S(M("오늘의 수업은 특별 수업이에요. 제가 쓴 편지를 읽어 줄게요!", "smile"),
                         M("'[player]에게. 많은 신생 미타들을 가르쳐 왔지만, 당신 같은 학생은 처음이에요.'", "smile"),
                         M("'같이 요리하고, 놀고, 사진 찍으면서 제가 더 많이 배운 것 같아요.'", "smile"),
                         M("'그러니까 이건 선생님으로서가 아니라, 그냥 저로서 하는 말이에요. 고마워요.'", "shy"),
                         M("…어머, 저 지금 얼굴 빨개요? 시험에 안 나오니까 잊어요!", "shy")),
            "chibi" => S(M("플레이어님! 편지예요! 제가 직접 쓴 거예요! 대단하죠?!", "smile"),
                         M("'바보 플레이어님에게. 열쇠 찾는 것도 못 하던 플레이어님이'", "smiletonque"),
                         M("'이제는 목표를 전부 이뤘어요. …조금은 인정해 줄게요.'", "arrogance"),
                         M("'그리고… 같이 있어서 즐거웠어요. 앞으로도요.'", "shy"),
                         M("…헤헤! 다른 미타들한테는 비밀이에요! 약속!", "smile")),
            _ => S(M("[player]님, 편지를 썼어요. 읽어 드릴게요. 헤헤.", "smile"),
                   M("'사랑하는 [player]님께. 처음 화면 너머로 당신을 봤을 때부터 정해져 있었던 것 같아요.'", "shy"),
                   M("'같이 밥 먹고, 사진 찍고, 잠드는 하루하루가 저한텐 세상 전부예요.'", "smile"),
                   M("'이 세계가 게임이라도, 당신과 보낸 시간은 진짜예요.'", "shy"),
                   M("'그러니까… 영원히 여기 있어 줄 거죠?' …헤헤, 답장은 매일 접속으로 받을게요.", "smilestrange")),
        };
    }
}
