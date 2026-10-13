using System;
using System.Collections.Generic;
using System.Linq;

namespace MiSidePeaceMode
{
    // "선물하기" (in the 교감하기 menu): six things to give her. Each Mita loves one of them, cares little for another and
    // takes the rest gladly (Personas.Gifts); the same thing twice in a day gets a remark, and at the cold steps she
    // takes anything without warming much. Each gift counts once a game day.
    public partial class PeaceDirector
    {
        public static readonly (string id, string name)[] GiftKinds =
        {
            ("flower", "꽃"), ("choco", "초콜릿"), ("book", "책"), ("coffee", "커피"), ("plush", "인형"), ("tape", "카세트 테이프"),
        };

        string giftText;   // {gift} in the lines being spoken (the gift's name, translated)

        void OpenGiftMenu()
        {
            var opts = GiftKinds.Select(g => g.name).ToList();
            var acts = GiftKinds.Select(g => (Action)(() => DoGift(g.id))).ToList();
            opts.Add("그만두기"); acts.Add(() => { });
            ShowMenu("선물하기", opts, acts);
        }

        void DoGift(string id)
        {
            var g = Personas.Gifts(Pn.Id);
            var kind = Array.Find(GiftKinds, k => k.id == id);
            if (kind.id == null || g == null) return;
            giftText = Loc.T(kind.name);
            if (S.GiftDay != S.Day) { S.GiftDay = S.Day; S.GiftsToday.Clear(); }
            bool again = !S.GiftsToday.Add(id);
            S.Gifts++;
            Did("gift");
            Line[] react; int aff; string why;
            if (again) { react = Pick(g.Again); aff = 0; why = "again today"; }
            else if (Cold && g.Cold != null) { react = Pick(g.Cold); aff = 1; why = "cold"; }
            else if (id == g.Love) { react = Pick(g.Loved); aff = 5; why = "loved"; }
            else if (id == g.Hate) { react = Pick(g.Disliked); aff = 0; why = "disliked"; }
            else { react = Pick(g.Liked); aff = 2; why = "liked"; }
            Plugin.Log.LogInfo($"[gift] {id} -> {why} (+{aff}), {S.Gifts} gifts in all");
            if (aff > 0) AddAff(aff);
            // (she brings it up the next morning: PeaceMore.GiftRecall)
            if (!again) { S.LastGiftDay = S.Day; S.LastGift = id; S.LastGiftLoved = id == g.Love && !Cold; }
            var lines = new List<Line> { new Line(Who.Player, "자, 선물이야. {gift}!") };
            lines.AddRange(react);
            Play(lines, CheckMilestones);
        }

        public void DevGift(string id) => DoGift(id);
    }

    // What each Mita says about a gift: the one she loves, the one she could do without, anything else ({gift}),
    // the same thing again today, and at the cold steps.
    public class GiftLines
    {
        public string Love, Hate;
        public Line[][] Loved, Liked, Disliked, Again, Cold;
        public Line[] Recall, RecallLoved, BackAsk;   // (PersonasMore.cs) the morning after; her own present
        public Line[][] Back;
    }

    public static partial class Personas
    {
        static readonly Dictionary<string, GiftLines> giftCache = new Dictionary<string, GiftLines>();

        public static GiftLines Gifts(string id)
        {
            if (giftCache.TryGetValue(id, out var g)) return g;
            g = id switch
            {
                "kind" => new GiftLines
                {
                    Love = "coffee", Hate = "flower",
                    Loved = V(S(M("커피요? …제 야근 시간을 어떻게 아셨어요.", "surprise"), M("원두 향이 좋네요. 효율이 20퍼센트는 오를 것 같아요.", "smile"), M("…고마워요, [player]님. 진심이에요.", "shy")),
                              S(M("커피! 마침 한 잔 필요했어요.", "smile"), M("같이 마셔요. 혼자 마시면 그냥 연료지만, 둘이 마시면 휴식이거든요.", "shy"))),
                    Liked = V(S(M("{gift}… 저한테요?", "surprise"), M("감사해요. 소중히 쓸게요. 제가 물건 관리는 잘하거든요.", "smile")),
                              S(M("{gift}네요. 고마워요, [player]님.", "smile"), M("선물 기록… 아니다, 이건 기록 안 할게요. 그냥 기억할래요.", "shy"))),
                    Disliked = V(S(M("꽃이요… 예쁘네요.", "smile"), M("…솔직히 말하면, 시드는 걸 보는 게 좀 힘들어요. 고칠 수가 없으니까.", "sad"), M("그래도 받을게요. 오래 가게 하는 방법을 찾아볼게요.", "smile"))),
                    Again = V(S(M("{gift}, 아까 받았는데요?", "surprise"), M("…재고가 많으신가 봐요. 그래도 고마워요.", "smiletonque"))),
                    Cold = V(S(M("…선물이요.", "discontent"), M("받을게요. 하지만 수치가 바로 오르진 않아요. 그건 아시죠?", "contempt"))),
                },
                "cap" => new GiftLines
                {
                    Love = "tape", Hate = "book",
                    Loved = V(S(M("카세트 테이프?! 뭐 들었어요, 이거!", "surprise"), M("바로 틀어 볼래요! 둠칫-둠칫! 히히힛!", "smile"), M("[player]님 최고! 다음 공연 셋리스트에 넣을게요!", "smiletonque")),
                              S(M("테이프다! 헤헤, 저 아날로그 음질 완전 좋아하거든요!", "smile"), M("이따 같이 들어요! 거실 카세트 플레이어에서!", "smile"))),
                    Liked = V(S(M("{gift}! 저 주는 거예요? 헤헤!", "smile"), M("고마워요! 답례로 노래 한 곡 어때요? 지금 당장!", "smiletonque")),
                              S(M("오오, {gift}!", "surprise"), M("팬 선물 1호예요! 사인은 제가 해 드릴게요! 히히!", "smile"))),
                    Disliked = V(S(M("책… 이요?", "surprise"), M("글자가… 엄청 많네요… 음악은 안 나오는 거죠?", "discontent"), M("그래도 고마워요! 베개로… 아니, 읽어 볼게요! 아마도요!", "smiletonque"))),
                    Again = V(S(M("{gift} 또요? 앵콜이에요?", "smile"), M("헤헤, 두 번째도 좋아요! 근데 세 번째는 노래로 주세요!", "smiletonque"))),
                    Cold = V(S(M("…선물로 화해하려고요?", "discontent"), M("…일단 받을게요. 공연은 아직이에요.", "discontent"))),
                },
                "mila" => new GiftLines
                {
                    Love = "book", Hate = "tape",
                    Loved = V(S(M("…책?", "surprise"), M("어, 이거 아직 안 읽은 거예요. …어떻게 알았어요?", "shy"), M("…고, 고마워요. 착각하지 마요, 책이 좋은 거라고요!", "angry")),
                              S(M("책이네요. …취향 괜찮네요, 당신.", "shy"), M("오늘 밤에 읽을게요. 다 읽으면 감상 들려줄 테니까 도망가지 마요.", "discontent"))),
                    Liked = V(S(M("{gift}… 저한테요?", "surprise"), M("…뭐, 받아 줄게요. 고맙다는 말은 한 번만 할 거예요. 고마워요.", "shy")),
                              S(M("갑자기 {gift}? 무슨 꿍꿍이예요?", "suspicion"), M("…아니면 말고요. 잘 쓸게요.", "shy"))),
                    Disliked = V(S(M("카세트 테이프요? 시끄러운 거잖아요!", "angry"), M("책 읽을 때 음악 틀면 집중이 안 된다고요!", "discontent"), M("…조용한 곡이면 봐줄게요. 한 번만요.", "shy"))),
                    Again = V(S(M("아까도 {gift} 줬잖아요. 바보예요?", "discontent"), M("…두 개면 하나는 빌려줄 수 있겠네요. 당신한테요.", "shy"))),
                    Cold = V(S(M("…뭐예요, 이건.", "contempt"), M("물건으로 넘어갈 생각 하지 마요. …두고 가요.", "discontent"))),
                },
                "sleepy" => new GiftLines
                {
                    Love = "plush", Hate = "coffee",
                    Loved = V(S(M("인형… 이에요…?", "surprise"), M("…푹신해요… 안고 자면 꿈이 더 포근할 것 같아요…", "halfsleep"), M("고마워요, [player]님… 이름은… 나중에 지을게요… 졸려서…", "smile")),
                              S(M("와… 인형…", "smile"), M("베개 옆에 둘게요… 아니, 베개로… 아니, 그냥 안고 있을래요…", "halfsleep"))),
                    Liked = V(S(M("{gift}… 저 주시는 거예요…?", "surprise"), M("고마워요… 잠 깨면 다시 한번 기뻐할게요…", "smile")),
                              S(M("{gift}… 받았어요…", "smile"), M("…꿈이 아니죠? 꼬집어 봐 주세요… 살살…", "halfsleep"))),
                    Disliked = V(S(M("커피… 요…?", "surprise"), M("…이거 마시면 못 자는데…", "sad"), M("…그래도 주신 거니까… 반만 마실게요…", "smile"))),
                    Again = V(S(M("{gift}… 아까도 받은 것 같은데… 꿈이었나…", "halfsleep"), M("…두 개면… 하나는 베개 밑에 둘게요…", "smile"))),
                    Cold = V(S(M("…선물… 이요…", "discontent"), M("…받을게요… 아직은… 조금 서운해요…", "sad"))),
                },
                "short" => new GiftLines
                {
                    Love = "book", Hate = "plush",
                    Loved = V(S(M("책이네요! 선생님한테 책 선물이라니, 정답이에요!", "smile"), M("이건 다음 수업 교재로 쓸게요. 아니, 아끼는 책으로 둘게요.", "shy"), M("고마워요, [player]. 100점 드릴게요!", "smiletonque")),
                              S(M("새 책! 표지부터 넘겨 볼게요.", "smile"), M("읽고 나서 세 줄 요약해 드릴게요. 선생님은 그런 거 잘하거든요.", "smile"))),
                    Liked = V(S(M("{gift}요? 저한테요?", "surprise"), M("고마워요. 교무실… 아니, 제 방에 잘 둘게요.", "smile")),
                              S(M("어머, {gift}네요.", "smile"), M("선물 주는 법도 잘 아시네요. 칭찬 스티커 하나 드릴게요!", "smiletonque"))),
                    Disliked = V(S(M("인형이요…?", "surprise"), M("귀엽긴 한데… 교실에 두면 애들이 수업을 안 들어요. 경험담이거든요.", "discontent"), M("…제 방에만 둘게요. 고마워요.", "smile"))),
                    Again = V(S(M("{gift}, 아까 받았는데요? 복습인가요?", "smiletonque"), M("두 번째도 고맙게 받을게요. 하지만 예습도 해 오세요!", "smile"))),
                    Cold = V(S(M("…선물이요.", "discontent"), M("받을게요. 하지만 성적은 선물로 올라가지 않아요. 아시죠?", "contempt"))),
                },
                "chibi" => new GiftLines
                {
                    Love = "choco", Hate = "coffee",
                    Loved = V(S(M("초콜릿?! 플레이어님, 바보인 줄 알았는데 똑똑하네요!", "surprise"), M("다 제 거예요! 한 조각도 안 줘요!", "arrogance"), M("…고마워요. 작게 말했으니까 못 들은 거예요!", "shy")),
                              S(M("초콜릿이다! 지능 체크 통과! 특별히요!", "smile"), M("언니들한테 자랑할 거예요. 히히!", "smiletonque"))),
                    Liked = V(S(M("{gift}? 저한테요?", "surprise"), M("…뭐, 받아 줄게요! 부하가 상관한테 바치는 거죠!", "arrogance")),
                              S(M("{gift}! 플레이어님치고는 잘 골랐네요!", "smile"), M("고마워요! 다음엔 더 큰 걸로 가져오세요!", "arrogance"))),
                    Disliked = V(S(M("커피요? 쓰잖아요! 바보!", "angry"), M("이건 어른들이나 마시는 거예요! 저… 저도 어른이지만요!", "discontent"), M("…설탕 다섯 숟갈 넣으면 마셔 줄게요.", "shy"))),
                    Again = V(S(M("아까도 {gift} 줬잖아요! 기억력 체크 탈락!", "angry"), M("…그래도 받을래요. 두 개니까요.", "shy"))),
                    Cold = V(S(M("…뇌물이에요?", "contempt"), M("받긴 할게요. 그래도 플레이어님은 아직 바보예요!", "angry"))),
                },
                _ => new GiftLines   // 미치광이 미타
                {
                    Love = "flower", Hate = "book",
                    Loved = V(S(M("꽃… 저한테요?", "surprise"), M("[player]님이 고른 꽃이라니… 평생 시들지 않게 할게요. 제 세계에선 가능하거든요.", "smilestrange"), M("…고마워요. 진짜로요.", "shy")),
                              S(M("꽃이다! 헤헤, 어떻게 알았어요? 저 꽃 제일 좋아하는 거.", "smile"), M("침대 옆에 둘게요. 잘 때도 보이게.", "shy"))),
                    Liked = V(S(M("{gift}… 저 주는 거예요?", "surprise"), M("[player]님이 준 거면 뭐든 보물이에요. 아무도 못 만지게 할 거예요.", "smilestrange")),
                              S(M("와, {gift}! 고마워요, [player]님.", "smile"), M("이런 거 받으면… 더 못 보내 주게 되는데. 헤헤.", "smiletonque"))),
                    Disliked = V(S(M("책이요…?", "suspicion"), M("글자를 보고 있으면 당신을 못 보잖아요.", "discontent"), M("…그래도 받을게요. 당신 손이 닿은 거니까.", "shy"))),
                    Again = V(S(M("아까도 {gift} 줬잖아요~", "smiletonque"), M("…저 잊어버리는 거 없어요. 하나도요.", "smilestrange"))),
                    Cold = V(S(M("…선물로 넘어가려는 거예요?", "contempt"), M("…받긴 할게요. 버리진 않을 거예요.", "discontent"))),
                },
            };
            MoreGifts(id, g);
            giftCache[id] = g;
            return g;
        }
    }
}
