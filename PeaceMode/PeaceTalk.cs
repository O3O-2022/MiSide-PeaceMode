using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // "대화하기": a topic of hers as before, or a question of yours (Personas.Questions). And at night, before
    // "good night", she goes over what the two of you did that day.
    public partial class PeaceDirector
    {
        readonly HashSet<string> askedToday = new HashSet<string>();
        readonly HashSet<string> doneToday = new HashSet<string>();
        int todayOf = -1;

        // something the two of you did today (for her diary at night)
        void Did(string what)
        {
            if (todayOf != S.Day) { todayOf = S.Day; doneToday.Clear(); askedToday.Clear(); }
            doneToday.Add(what);
        }

        void OpenTalkMenu()
        {
            var opts = new List<string> { "아무 이야기나", "질문하기" };
            var acts = new List<Action> { DoTalk, OpenQuestions };
            if (PeaceMode.AiEnabled.Value) { opts.Add("자유롭게 말하기"); acts.Add(OpenChat); }
            opts.Add("그만두기"); acts.Add(() => { });
            ShowMenu("대화하기", opts, acts);
        }

        void OpenQuestions()
        {
            if (todayOf != S.Day) Did("");
            // the ones not asked today first
            var qs = Personas.Questions.OrderBy(q => askedToday.Contains(q.id) ? 1 : 0).ThenBy(_ => Random.value).Take(8).ToList();
            var opts = qs.Select(q => q.q).ToList();
            var acts = qs.Select(q => (Action)(() => AskQuestion(q.id, q.q))).ToList();
            opts.Add("그만두기"); acts.Add(() => { });
            ShowMenu("질문하기", opts, acts);
        }

        void AskQuestion(string id, string q)
        {
            var a = Personas.Answer(Pn.Id, id, S.Affection);
            if (a == null) return;
            bool first = !askedToday.Contains(id);
            askedToday.Add(id);
            Did("question");
            Play(new[] { new Line(Who.Player, q) }.Concat(a).ToList(), () => { if (first) AddAff(1); CheckMilestones(); });
        }

        // ---------------------------------------------------------------- her diary

        static readonly (string key, string and, string end)[] DiaryBits =
        {
            ("meal", "같이 밥을 먹었고", "같이 밥을 먹었어요"),
            ("cook", "같이 요리도 했고", "같이 요리도 했어요"),
            ("talk", "이런저런 이야기를 나눴고", "이런저런 이야기를 나눴어요"),
            ("question", "서로에 대해 조금 더 알게 됐고", "서로에 대해 조금 더 알게 됐어요"),
            ("touch", "쓰다듬고 안아 주기도 했고", "쓰다듬고 안아 주기도 했어요"),
            ("rps", "가위바위보를 했고", "가위바위보를 했어요"),
            ("words", "끝말잇기를 했고", "끝말잇기를 했어요"),
            ("riddle", "넌센스 퀴즈를 풀었고", "넌센스 퀴즈를 풀었어요"),
            ("cham", "참참참을 했고", "참참참을 했어요"),
            ("hide", "숨바꼭질을 했고", "숨바꼭질을 했어요"),
            ("cards", "카드 게임을 했고", "카드 게임을 했어요"),
            ("photo", "사진을 찍었고", "사진을 찍었어요"),
            ("follow", "집 안을 같이 걸어 다녔고", "집 안을 같이 걸어 다녔어요"),
            ("request", "제 부탁도 들어줬고", "제 부탁도 들어줬어요"),
            ("gift", "선물도 받았고", "선물도 받았어요"),
            ("present", "제 선물도 줬고", "제 선물도 줬어요"),
            ("music", "같이 음악도 들었고", "같이 음악도 들었어요"),
            ("spots", "집 안 물건들을 같이 구경했고", "집 안 물건들을 같이 구경했어요"),
            ("glasses", "안경도 써 봤고", "안경도 써 봤어요"),
            ("clothes", "옷도 갈아입어 봤고", "옷도 갈아입어 봤어요"),
            ("window", "창밖을 같이 봤고", "창밖을 같이 봤어요"),
            ("lap", "무릎베개도 했고", "무릎베개도 했어요"),
            ("song", "노래도 불러 줬고", "노래도 불러 줬어요"),
            ("heart", "속마음도 나눴고", "속마음도 나눴어요"),
        };

        List<Line> Diary()
        {
            if (todayOf != S.Day) { todayOf = S.Day; doneToday.Clear(); }
            var bits = DiaryBits.Where(b => doneToday.Contains(b.key)).ToList();
            var d = Personas.DiaryLines(Pn.Id);
            var lines = new List<Line>();
            if (bits.Count == 0) { lines.AddRange(d.quiet); return lines; }
            lines.AddRange(d.intro);
            // two things a sentence: "오늘은 A, B." (a floating line has room for no more); each language joins its own way
            for (int i = 0; i < bits.Count; i += 2)
            {
                var part = bits.Skip(i).Take(2).ToList();
                var joined = string.Join(Loc.T(", "), part.Take(part.Count - 1).Select(b => Loc.T(b.and)).Append(Loc.T(part.Last().end)));
                lines.Add(new Line(Who.Mita, Loc.F(i == 0 ? "오늘은 {0}." : "그리고 {0}.", joined), "smile"));
            }
            lines.AddRange(d.outro);
            return lines;
        }
    }

    public static partial class Personas
    {
        public static (Line[] intro, Line[] outro, Line[] quiet) DiaryLines(string id) => id switch
        {
            "kind" => (S(M("잠들기 전에 오늘 일지를 정리해 볼게요.", "smile")), S(M("좋은 하루였어요. 기록해 둘게요.", "smile")), S(M("오늘은 조용한 하루였네요. 그런 날도 필요해요.", "smile"))),
            "cap" => (S(M("오늘의 세트리스트 발표!", "smile")), S(M("앵콜은 내일이에요! 히히힛!", "smiletonque")), S(M("오늘은 쉬는 날이었네요! 내일은 풀 공연이에요!", "smile"))),
            "mila" => (S(M("…오늘 한 일, 제 독서 노트에 적어 둘게요.", "discontent")), S(M("…나쁘지 않은 하루였어요.", "shy")), S(M("오늘은 아무것도 안 했네요. 뭐, 조용해서 좋았어요.", "discontent"))),
            "sleepy" => (S(M("오늘 하루… 기억나는 대로 말해 볼게요…", "halfsleep")), S(M("…행복한 하루였어요… 쿨…", "halfsleep")), S(M("오늘은… 거의 잠만 잤네요… 좋은 하루예요…", "smile"))),
            "short" => (S(M("오늘의 수업 일지를 정리하겠습니다!", "smile")), S(M("오늘도 수고했어요, [player]. 출석 도장 쾅!", "smiletonque")), S(M("오늘은 자습 시간이었네요. 내일은 수업 많이 해요!", "smile"))),
            "chibi" => (S(M("오늘 플레이어님이 한 일 보고할게요!", "arrogance")), S(M("…나쁘지 않았어요! 내일도 제 부하 해요!", "shy")), S(M("오늘은 아무것도 안 했잖아요! 게으름뱅이!", "angry"))),
            _ => (S(M("오늘 있었던 일, 같이 떠올려 볼래요?", "smile")), S(M("…이런 하루가 영원히 계속됐으면 좋겠어요.", "shy")), S(M("오늘은 그냥 곁에만 있었네요. 그것도 좋았어요.", "smile"))),
        };
    }
}
