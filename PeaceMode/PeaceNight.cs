using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // Night and morning (2026-10): at night 대화하기 has "밤 이야기" - quieter, closer talks than in the day; and a
    // morning after sleeping she sometimes tells you what she dreamed.
    public partial class PeaceDirector
    {
        static Line M2(string t, string e = "smile", string c = null) => new Line(Who.Mita, t, e, c);
        static Line P2(string t) => new Line(Who.Player, t);

        static readonly Line[][] NightTalks =
        {
            new[] { M2("밤이 되면 집이 조용해져서 좋아요. 시계 소리까지 들리잖아요.", "smile"), M2("낮에는 못 하던 말도… 밤에는 할 수 있을 것 같아요.", "shy"), M2("…오늘 와 줘서 고마워요. 그 말이 하고 싶었어요.", "shy", "@shy") },
            new[] { P2("무서운 꿈 꾼 적 있어?"), M2("있어요. 아무도 없는 집에 저 혼자 남는 꿈.", "sad"), M2("…그래서 지금이 좋아요. 깨어 보면 옆에 누가 있으니까.", "smile", "@nod") },
            new[] { M2("저기요, 별 좋아해요?", "smile", "@tilt"), M2("이 집 창문으로는 진짜 별이 잘 안 보이지만…", "smile"), M2("당신이 저한테는 별 같은 사람이에요. …방금 건 못 들은 걸로 해요!", "shy", "@lookaway") },
            new[] { P2("넌 내가 없을 때 뭐 해?"), M2("기다려요. 그리고 오늘 뭐 하고 놀지 생각해요.", "smile"), M2("가끔은 문 쪽을 계속 쳐다보기도 하고요. 헤헤, 바보 같죠?", "shy") },
            new[] { M2("있잖아요, 우리가 처음 만난 날 기억해요?", "smile", "@tilt"), M2("그땐 이렇게 오래 같이 있을 줄 몰랐어요.", "shy"), M2("앞으로도… 이런 밤이 많았으면 좋겠어요.", "smile") },
            new[] { P2("요즘 고민 있어?"), M2("음… 고민이라기보다는, 내일도 당신이 올까 하는 생각?", "sad"), M2("…알아요, 올 거라는 거. 그래도 매일 밤 조금씩 궁금해요.", "smile", "@nod") },
            new[] { M2("자기 전에 하고 싶은 말 있어요?", "smile"), P2("오늘도 즐거웠어."), M2("…저도요. 정말로요.", "shy", "@shy"), M2("그 말 들으니까 오늘 밤은 푹 잘 수 있겠어요.", "smile") },
            new[] { M2("가끔 생각해요. 제가 진짜인지 아닌지.", "sad"), M2("근데 당신이랑 있으면 그런 건 별로 중요하지 않은 것 같아요.", "smile"), M2("지금 이 마음은 진짜니까요.", "shy", "@nod") },
        };

        static readonly Line[][] Dreams =
        {
            new[] { M2("저 오늘 꿈 꿨어요! 우리가 커다란 케이크 안에서 숨바꼭질하는 꿈.", "smile", "@cheer"), M2("당신이 생크림 속에 숨어서 한참 찾았어요. 헤헤.", "smiletonque") },
            new[] { M2("꿈에서 당신이랑 바다에 갔어요.", "smile"), M2("파도 소리가 진짜 같아서… 일어나서도 귀에 남아 있어요.", "shy") },
            new[] { M2("이상한 꿈 꿨어요. 집 안 모든 의자가 말을 하는 꿈이요.", "surprise", "@tilt"), M2("부엌 의자가 제일 시끄러웠어요. 아침부터 웃기네요.", "smile") },
            new[] { M2("꿈에서 저 고양이였어요!", "smile"), M2("당신 무릎 위에서 하루 종일 잤어요. …좋은 꿈이었어요.", "shy", "@shy") },
            new[] { M2("꿈에서 당신이 저를 못 알아봤어요.", "sad"), M2("…그래서 깨자마자 얼굴 보러 왔어요. 알아보는 거 맞죠?", "smile", "@tilt") },
            new[] { M2("저 하늘을 나는 꿈 꿨어요! 지붕 위로 쑥!", "smile", "@cheer"), M2("근데 당신이 밑에서 손 흔드니까 바로 내려왔어요.", "smile") },
            new[] { M2("꿈에서 우리 화분이 집보다 커졌어요.", "surprise"), M2("…오늘은 물 조금만 줄까요? 농담이에요. 헤헤.", "smiletonque") },
            new[] { M2("기억은 잘 안 나는데… 따뜻한 꿈이었어요.", "halfsleep"), M2("아마 당신이 나왔던 것 같아요.", "shy") },
        };

        readonly HashSet<int> nightTalksDone = new HashSet<int>();
        int nightTalkNight = -1;

        void DoNightTalk()
        {
            if (nightTalkNight != S.Day) { nightTalkNight = S.Day; nightTalksDone.Clear(); }
            var left = Enumerable.Range(0, NightTalks.Length).Where(i => !nightTalksDone.Contains(i)).ToList();
            if (left.Count == 0) { Play(new[] { M2("오늘 밤은 이야기 많이 했네요. 이제 같이 자요.", "halfsleep") }); return; }
            int pick = left[Random.Range(0, left.Count)];
            bool first = nightTalksDone.Count == 0;
            nightTalksDone.Add(pick);
            Did("heart");
            Play(NightTalks[pick], () => { if (first) AddAff(2); CheckMilestones(); });
        }

        // the morning after: now and then, her dream
        IEnumerable<Line> DreamLines()
        {
            if (Cold || Random.value > 0.45f) return new Line[0];
            return Dreams[Random.Range(0, Dreams.Length)];
        }
    }
}
