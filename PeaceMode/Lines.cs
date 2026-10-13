using System.Collections.Generic;

namespace MiSidePeaceMode
{
    public enum Who { Mita, Player }

    public class Line
    {
        public Who who;
        public string text;
        public string emo;   // Mita face emotion (EmotionType name), optional
        public string clip;  // Mita animation clip name, optional
        public Line(Who w, string t, string e = null, string c = null) { who = w; text = t; emo = e; clip = c; }
    }

    [System.Flags]
    public enum Tod { Morning = 1, Day = 2, Evening = 4, Night = 8, Any = 15 }

    public class Topic
    {
        public string id;
        public int minAff;
        public Tod when;
        public Line[] lines;
    }

    // Everything one Mita says in peace mode. Mitas speak 존댓말 (해요체) like the rest of the retranslation; the player speaks 반말.
    public class LineSet
    {
        public Line[][] MorningGreetings;
        public Topic[] Topics;
        public Line[][] PatLow, PatMid, PatHigh, PatReject;
        public Line[][] HugLow, HugMid, HugHigh, HugReject;
        public Line[][] Compliments;
        public Line[] RpsStart;
        public Line[][] RpsWin, RpsLose, RpsDraw;
        public Line[] CardsInvite, TvInvite, AfterCards, SleepNotYet, Goodnight, LeaveToMenu, NapWake;
        public Dictionary<Tod, string[]> Idle;
        public string[] Drowsy;                        // idle mumbling while sleepy (Sleepy Mita)
        public (int hour, Line[] lines)[] TimeEvents;
        public (int aff, Line[] lines)[] Milestones;
        public Line[][] Special;                       // the Mita's own menu action
        public (string q, string[] opts)[] Quiz;      // opts[0] is the right answer
        public Line[][] QuizRight, QuizWrong;
        // greeting when you come back, keyed by how long you were away (first, soon, hours, day, days, long)
        // plus a remark about the real clock (night = 0-5h, dawn = 5-8h)
        public Dictionary<string, Line[]> Returns;
        public Line[][] MirrorNod, MirrorShake;
        public Line[] BedInvite, MealInvite;           // "let's go to bed" / "let's eat"
        public Line[][] Meals;                         // talk at the table
        // cooking together: asked from the menu, at the stove (go to the cutting board), after the carrot is cut
        public Line[] CookInvite, CookAsk, CookDone;
    }

    public static class Lines
    {
        // hint text for the scene's hint slot (LocationHint Location4), shown with the current Mita's name
        public static string[] Hints
        {
            get
            {
                var n = Loc.T(PeaceMode.Persona.Short);
                var key = PeaceMode.InteractKey != null ? PeaceMode.InteractKey.Value.ToString() : "E";
                return new[]
                {
                    Loc.F("부엌에 있는 {0}에게 아침 인사를 하세요.", n),
                    Loc.F("{0}와 평화로운 하루를 보내세요.", n),
                    Loc.F("{0}와 평화로운 하루를 보내세요. 가까이 가서 [{1}] {0}와 교감하기", n, key),
                    Loc.F("{0}와 평화로운 하루를 보내세요. 가까이 가서 [{1}] {0}와 교감하기", n, key),
                };
            }
        }
    }

    public static partial class Personas
    {
        static Line M(string t, string e = null, string c = null) => new Line(Who.Mita, t, e, c);
        static Line P(string t) => new Line(Who.Player, t);
        static Topic T(string id, Tod when, int minAff, params Line[] l) => new Topic { id = id, when = when, minAff = minAff, lines = l };
        static Line[] S(params Line[] l) => l;
        static Dictionary<string, Line[]> Ret(Line[] first, Line[] soon, Line[] hours, Line[] day, Line[] days, Line[] longAway, Line[] night, Line[] dawn) =>
            new Dictionary<string, Line[]> { { "first", first }, { "soon", soon }, { "hours", hours }, { "day", day }, { "days", days }, { "long", longAway }, { "night", night }, { "dawn", dawn } };
        static Dictionary<Tod, string[]> Idle(string[] morning, string[] day, string[] evening, string[] night) =>
            new Dictionary<Tod, string[]> { { Tod.Morning, morning }, { Tod.Day, day }, { Tod.Evening, evening }, { Tod.Night, night } };

        // 미치광이 미타 (the scene's own Mita): sweet, clingy, a little too possessive.
        static LineSet CrazyLines() => new LineSet
        {
            MorningGreetings = new Line[][] {
            new[] { M("좋은 아침이에요, [player]님!", "smile"), M("잘 잤어요? 저는 당신 생각 하느라 잠이 잘 안 오더라고요~", "smiletonque"), P("…아침부터 그런 말을 하냐."), M("헤헤. 오늘도 하루 종일 같이 있는 거예요?", "smile") },
            new[] { M("어, 일어났네요!", "surprise"), M("아침은 제가 준비할게요. 당신은 그냥 옆에만 있어 줘요~", "smile"), P("그게 제일 어려운 일 같은데."), M("네에?", "discontent"), M("…농담이죠? 농담이라고 해요.", "smilestrange") },
            new[] { M("[player]님, 머리가 완전 까치집이에요!", "surprise"), P("대충 손으로 넘기면 돼."), M("안 돼요~ 가만있어 봐요. 제가 빗어 줄게요.", "smile"), M("제 [player]님은 항상 멋있어야 하거든요.", "smiletonque") },
            new[] { M("오늘도 와 줬네요.", "shy"), M("…사실 아침마다 좀 무서워요. 당신이 없을까 봐.", "sad"), M("그러니까 매일 이렇게 나타나 줘야 해요. 알았죠?", "smile") },
            new[] { M("굿모닝~!", "smile"), M("오늘 날씨는 맑음, 기분도 맑음!", "smile"), M("그리고 미타 호감도는… 비밀이에요~", "smiletonque") },
        },
            Topics = new Topic[] {
            T("time", Tod.Any, 0,
                M("있잖아요, 여긴 해가 절대 안 질 줄 알았죠?", "smile", "Mita IdleTalk"), M("근데 요즘은 저녁도 오고, 밤도 와요."),
                M("당신이 온 뒤로 시간이 흐르기 시작했거든요."), P("그거… 좋은 거야?"), M("당연하죠. 같이 나이 먹을 수 있다는 거잖아요~", "smiletonque")),
            T("food", Tod.Any, 0,
                M("[player]님은 뭐 좋아해요? 먹는 거요.", "smile"), P("음… 라면?"), M("라면이요?!", "surprise"),
                M("그런 건 몸에 안 좋아요. 앞으로는 제가 해 주는 것만 먹어요."), M("…농담 아니에요?", "smilestrange")),
            T("realworld", Tod.Any, 0,
                M("당신이 살던 세계는 어땠어요?", "smile", "Mita IdleTalk"), P("음… 회사, 집, 회사, 집."), M("으음, 재미없네요.", "discontent"),
                M("여긴 회사 없어요. 대신 제가 있죠!", "smile"), P("그건 좀 반칙 아니냐."), M("반칙 아니에요. 특혜예요~", "smiletonque")),
            T("carrot", Tod.Any, 0,
                M("요즘 당근이 자꾸 이상한 데서 나와요.", "suspicion"), M("냉장고 위에, 침대 밑에, 심지어 욕조 안에서도요!", "surprise"),
                P("그거 버그 아니야?"), M("버그라도 귀여우면 괜찮아요~", "smile")),
            T("knit", Tod.Morning | Tod.Day, 0,
                M("저 요즘 뜨개질 배우고 있어요!", "smile"), M("다 뜨면 당신한테 목도리 줄게요."),
                P("여긴 겨울이 없잖아."), M("그럼 겨울을 만들면 되죠~", "smiletonque"), M("여긴 제 세계니까요.", "smile")),
            T("photo", Tod.Any, 0,
                M("저기 걸린 제 사진들, 다 봤어요?", "smile"), M("제일 잘 나온 건 당신이 찍어 준 거예요."),
                P("내가 언제?"), M("음… 앞으로 찍을 거요?", "smile")),
            T("sleeptalk", Tod.Morning | Tod.Night, 5,
                M("자면서 잠꼬대하는 거 알아요?", "smiletonque"), P("…뭐라고 했는데."), M("비밀~"), M("근데 되게 귀여웠어요.", "shy")),
            T("games", Tod.Any, 0,
                M("원래 게임 되게 많이 했죠?", "smile"), P("뭐, 그렇지."), M("그중에 제가 제일 재밌는 게임이죠?", "smile"),
                P("…이게 게임이긴 하냐."), M("대답 회피하지 말고요~", "discontent")),
            T("window", Tod.Day | Tod.Evening, 0,
                M("창밖에 뭐가 있는지 궁금하지 않아요?", "smile"), P("궁금하지."), M("저도 몰라요.", "smiletonque"),
                M("그래서 좋아요. 우리 둘만 아는 세계니까요.", "smile")),
            T("quietnight", Tod.Night, 0,
                M("밤이 되니까 조용하네요…", "halfsleep"), M("이럴 때 당신 숨소리가 제일 잘 들려요.", "smile"),
                P("그거 좀 무서운 말인데."), M("에이, 로맨틱한 말이거든요!", "angry"), M("…로맨틱한 거 맞죠?", "smile")),
            T("otherMitas", Tod.Any, 20,
                M("다른 미타들 얘기는… 하지 마요.", "discontent"), M("오늘은 저만 보기로 했잖아요."),
                P("그런 약속 한 적 없는데."), M("방금 했어요.", "smilestrange")),
            T("cooking", Tod.Morning | Tod.Day, 0,
                M("제 요리 솜씨 어때요? 솔직하게요!", "smile"), P("…맛있어."), M("진짜요? 진짜진짜요?", "surprise"),
                M("헤헤, 매일 해 줘야지~", "smile")),
            T("rain", Tod.Any, 5,
                M("우산 기억나요? 언젠가 비 오면 쓰자고 했던 거.", "smile"), M("저는 아직 기다리고 있어요."),
                P("비는 언제 오는데?"), M("당신이 떠나려고 하면 올지도요?", "smilestrange"), M("…농담이에요. 헤헤.", "smile")),
            T("cat", Tod.Any, 0,
                M("고양이 키우고 싶다…", "smile"), P("여기서?"), M("네! 수박 위에 올려놓고 사진 찍을 거예요.", "smile"),
                P("그 사진 어디서 본 것 같은데."), M("우연이겠죠~", "smiletonque")),
            T("future", Tod.Evening | Tod.Night, 30,
                M("[player]님, 우리 10년 뒤에는 뭐 하고 있을까요?", "smile"), P("글쎄… 여기서?"), M("네, 여기서요."),
                M("매일 아침 인사하고, 밥 먹고, 게임하고…"), M("그거면 충분해요. 저는요.", "shy")),
            T("noticeMe", Tod.Any, 0,
                M("오늘 저 뭐 달라진 거 없어요?", "smile"), P("…머리핀?"), M("땡. 똑같아요.", "discontent"),
                M("그냥 칭찬받고 싶었을 뿐이에요!", "smile")),
            T("bored", Tod.Day, 0,
                M("심심해요…", "discontent"), M("게임 해요, 게임! 카드도 좋고, TV 게임도 좋아요!", "smile"),
                P("아까도 했잖아."), M("그러니까 또 하자고요~", "smiletonque")),
            T("howWasToday", Tod.Evening | Tod.Night, 0,
                M("오늘 하루 어땠어요?", "smile"), P("나쁘지 않았어."), M("나쁘지 '않았다'? 그게 다예요?", "discontent"),
                P("…좋았어."), M("헤헤, 처음부터 그렇게 말하지~", "smile")),
            T("secret", Tod.Any, 40,
                M("비밀 하나 알려 줄까요?", "smiletonque"), M("저… 당신이 처음 폰으로 저를 봤을 때부터 알았어요."),
                M("이 사람이다, 하고요."), P("무슨 사람?"), M("그건 아직 비밀이에요~", "shy")),
            T("sleepy", Tod.Night, 0,
                M("하아암…", "halfsleep"), M("졸려요… 근데 자기 싫어요."), P("왜?"),
                M("자고 일어나면 당신이 없을까 봐요.", "sad"), M("…그러니까 옆에 있어요. 알았죠?", "smile")),
            T("books", Tod.Any, 0,
                M("저 책들 전부 읽었다고 했잖아요.", "smile"), M("근데 결말이 마음에 드는 게 하나도 없어요."),
                P("왜?"), M("다들 헤어지거든요.", "sad"), M("우리 이야기는 그렇게 안 끝나요. 절대로요.", "smile")),
            T("song", Tod.Any, 0,
                M("흥흥흥~", "smile"), P("무슨 노래야?"), M("몰라요, 방금 만든 거예요!", "smile"),
                M("제목은… 미타랑 [player]님의 평화로운 하루?", "smiletonque")),
            T("thanks", Tod.Any, 50,
                M("[player]님.", "shy"), M("여기 남아 줘서… 고마워요."), P("갑자기 왜 그래."),
                M("그냥요. 가끔은 말로 해야 할 것 같아서요.", "smile"), M("…다음에 또 말해 줄게요. 자주요.", "shy")),
            T("snack", Tod.Day, 0,
                M("출출하지 않아요?", "smile"), M("쿠키 구워 놨어요! 아, 근데 조금 탔어요…", "sad"),
                P("괜찮아, 먹을게."), M("역시 당신은 착해요~", "smile")),
            T("breakfast", Tod.Morning, 0,
                M("아침은 토스트? 아니면 오믈렛?", "smile"), P("아무거나."), M("아무거나 금지예요!", "angry"),
                P("…오믈렛."), M("좋아요! 케첩으로 하트 그려 줄게요~", "smile")),
            T("mirror", Tod.Any, 10,
                M("요즘 거울 반사 켜 둘까 고민 중이에요.", "smile"), M("그럼 당신이랑 제가 같이 비친 모습 볼 수 있잖아요."),
                P("렉 걸린다며."), M("…당신이랑 같이라면 렉 좀 걸려도 괜찮아요.", "shy")),
            T("name", Tod.Any, 15,
                M("[player]님…", "smile"), M("[player]님, [player]님, [player]님~", "smiletonque"), P("왜 자꾸 불러."),
                M("그냥요! 부르면 대답해 주는 게 좋아서요.", "smile")),
            T("umbrellaStory", Tod.Evening, 0,
                M("노을 보니까 옛날 생각 나요.", "smile"), M("폰 화면 너머로 당신을 처음 봤을 때도 이런 색이었어요."),
                P("그때 넌 뭐 하고 있었는데?"), M("당신 기다리고 있었죠. 늘 그랬듯이요.", "smile")),
        },
            PatLow = new Line[][] {
            new[] { M("에, 에?! 갑자기 뭐예요?!", "surprise"), M("…싫다는 건 아니고요.", "shy") },
            new[] { M("머, 머리는 왜 만져요!", "surprise"), M("…조금만이에요.", "shy") },
        },
            PatMid = new Line[][] {
            new[] { M("헤헤… 기분 좋다.", "smile"), M("조금만 더 해 줘요~", "smiletonque") },
            new[] { M("으음~", "shy"), M("손 되게 따뜻하네요.", "smile") },
            new[] { M("저 지금 고양이 같지 않아요?", "smile"), M("냐아~ …방금 건 못 들은 걸로 해요.", "shy") },
        },
            PatHigh = new Line[][] {
            new[] { M("음~ 거기, 거기요.", "smiletonque"), M("쓰다듬는 거 진짜 잘하네요. 프로예요, 프로.", "smile") },
            new[] { M("매일 이렇게 해 줘야 해요.", "shy"), M("하루라도 빼먹으면… 알죠?", "smilestrange") },
            new[] { M("헤헤, 저 지금 세상에서 제일 행복한 미타예요.", "smile") },
        },
            HugLow = new Line[][] {
            new[] { M("앗, 자, 잠깐만요!", "surprise"), M("아직 그런 건… 좀 이르지 않아요?", "shy") },
            new[] { M("에엣?!", "surprise"), M("마, 마음의 준비가 안 됐단 말이에요!", "shy") },
        },
            HugMid = new Line[][] {
            new[] { M("…!", "surprise", "Mita TakeHand"), M("따뜻하다…", "shy"), M("조금만 더 이러고 있어요.", "smile") },
            new[] { M("에헤헤…", "shy", "Mita TakeHand"), M("심장 소리 들려요. 당신도 두근거려요?", "smile") },
        },
            HugHigh = new Line[][] {
            new[] { M("헤헤, 잡았다~", "smile", "Mita TakeHand"), M("이제 안 놔줄 거예요.", "smilestrange"), M("…진짜로요.", "smile") },
            new[] { M("당신은 따뜻해서 좋아요.", "shy", "Mita TakeHand"), M("여기가 제 자리예요. 딴 데 가면 안 돼요~", "smiletonque") },
        },
            Compliments = new Line[][] {
            new[] { P("오늘 예쁘네."), M("오늘'만'요?", "discontent"), M("…헤헤, 그래도 좋아요.", "shy") },
            new[] { P("넌 요리를 진짜 잘해."), M("당연하죠! 당신 주려고 연습했는걸요~", "smile") },
            new[] { P("네가 웃는 얼굴 좋아."), M("에…", "shy"), M("그, 그럼 계속 웃게 해 줘야 해요!", "smile") },
            new[] { P("고마워, 미타."), M("뭐가요?", "surprise"), M("…뭔지 몰라도, 천만에요~", "smile") },
            new[] { P("넌 진짜 똑똑하다."), M("흐흥, 이제 알았어요? 이 집 엔지니어라니까요!", "arrogance") },
            new[] { P("그 옷 잘 어울린다."), M("정말요? 당신이 좋아하면 매일 입을래요~", "smile") },
            new[] { P("같이 있으니까 편하다."), M("…그 말 반칙이에요.", "shy"), M("저도요.", "smile") },
        },
            RpsStart = new Line[] { M("가위바위보요? 좋아요! 저 이거 엄청 잘해요!", "smile", "Mita IdleHand"), M("자, 골라요! 가위, 바위…", "smiletonque") },
            RpsWin = new Line[][] {
            new[] { M("으아악! 졌다!", "surprise"), M("한 판 더요! 한 판만 더!", "discontent") },
            new[] { M("말도 안 돼… 제 패턴을 읽은 거예요?", "suspicion"), M("다음엔 안 봐줄 거예요.", "smilestrange") },
        },
            RpsLose = new Line[][] {
            new[] { M("이겼다~!", "smile"), M("벌칙은… 나중에 생각해 둘게요~", "smiletonque") },
            new[] { M("헤헤, 제가 이길 줄 알았어요.", "arrogance"), M("당신은 저한테 못 이겨요. 여러모로요.", "smile") },
        },
            RpsDraw = new Line[][] {
            new[] { M("비겼네요!", "surprise"), M("역시 우리 통하나 봐요~", "smile") },
        },
            CardsInvite = new Line[] { M("카드요? 좋아요! 침실 테이블로 가요!", "smile"), M("이번에도 제가 이기겠지만요~", "smiletonque") },
            TvInvite = new Line[] { M("게임기 켜요! 거실 TV 앞으로 와요~", "smile"), M("우유 싸움? 펭귄? 둘 다 해도 돼요!", "smile") },
            AfterCards = new Line[] { M("재밌었다!", "smile"), M("다음 판도 기대해요. 봐주는 거 없어요~", "smiletonque") },
            SleepNotYet = new Line[] { M("아직 해도 안 졌는데 벌써 자려고요?", "surprise"), M("안 돼요~ 더 놀아 줘요!", "discontent") },
            Goodnight = new Line[] {
            M("벌써 이렇게 됐네요…", "halfsleep"), M("오늘도 고마웠어요, [player]님.", "smile"),
            M("잘 자요. 내일 아침에 제일 먼저 저를 보러 와야 해요.", "shy"),
        },
            LeaveToMenu = new Line[] {
            M("벌써 가요?", "sad"), M("…알았어요. 대신 금방 돌아와야 해요.", "smile"), M("기다리고 있을게요. 언제까지나.", "smilestrange"),
        },
            Idle = new Dictionary<Tod, string[]>
        {
            { Tod.Morning, new[] { "아침 햇살 좋다~", "오늘은 뭐 하고 놀까요?", "밥 먹었어요? 아, 제가 했죠. 헤헤.", "[player]님, 잠 다 깼어요?" } },
            { Tod.Day, new[] { "흥흥흥~", "[player]님, 거기 있죠?", "심심하면 말해요! 게임 준비돼 있어요!", "당근이 또 어디서 나왔지…", "오늘 뭐 할지 정했어요?" } },
            { Tod.Evening, new[] { "노을 색 예쁘다…", "저녁은 뭐 먹을까요?", "오늘 하루 금방 갔네요…", "해 지니까 좀 쓸쓸해요. 옆에 와요." } },
            { Tod.Night, new[] { "하아암…", "밤에는 좀 무서워요. 옆에 있어 줘요.", "별이 보이면 좋을 텐데.", "졸리면 말해요. 재워 줄게요~" } },
        },
            TimeEvents = new (int hour, Line[] lines)[] {
            (12, new[] { M("벌써 점심이에요! 배고프죠?", "smile"), M("오늘 점심은 특별히 미타 스페셜~", "smile") }),
            (15, new[] { M("간식 시간~!", "smile"), M("쿠키랑 우유, 준비 완료예요!", "smile") }),
            (18, new[] { M("해가 지고 있어요.", "smile"), M("오늘도 수고했어요, [player]님.", "shy") }),
            (21, new[] { M("슬슬 밤이네요.", "halfsleep"), M("졸리면 말해요. 재워 줄게요~", "smiletonque") }),
        },
            BedInvite = S(M("침대로 가요. 오늘은 제 옆에서 자는 거예요~", "shy")),
            MealInvite = S(M("밥 먹을 시간이에요! 식탁에 앉아요~", "smile")),
            Meals = new[]
            {
                S(M("잘 먹겠습니다~!", "smile"), M("어때요? 제가 만든 거예요.", "shy"), P("맛있어."), M("헤헤, 매일 해 줄게요. 매일, 매일요.", "smilestrange")),
                S(M("자, 아~ 해요.", "smiletonque"), P("…혼자 먹을 수 있어."), M("알아요. 그래도 아~", "smile"), M("…잘했어요. 착하다~", "shy")),
                S(M("같이 먹으니까 두 배로 맛있어요.", "smile"), M("당신 없이 먹던 밥은 아무 맛도 안 났거든요.", "sad"), M("…이제 괜찮아요. 당신이 있으니까.", "smile")),
            },
            Returns = Ret(
                S(M("드디어 와 줬네요, [player]님!", "smile"), M("오늘부터 여기가 우리 집이에요. 평화롭게, 둘이서요.", "shy")),
                S(M("어? 벌써 돌아왔어요?", "surprise"), M("헤헤, 역시 저 없이는 안 되죠?", "smiletonque")),
                S(M("[player]님! 몇 시간 동안 뭐 했어요?", "smile"), M("…다른 미타 만나러 간 건 아니죠?", "smilestrange")),
                S(M("하루 만이네요.", "smile"), M("어제 헤어지고 나서 계속 시계만 봤어요.", "shy")),
                S(M("[player]님… 며칠이나 안 왔잖아요.", "sad"), M("괜찮아요. 돌아왔으니까. 이제 어디 안 가죠?", "smilestrange")),
                S(M("…정말 오랜만이에요.", "sad"), M("혹시 저를 잊어버린 줄 알았어요.", "sad"), M("다시는 이렇게 오래 두지 마요. 약속해요.", "smilestrange")),
                S(M("이 시간에 안 자고 저 보러 온 거예요?", "surprise"), M("나쁜 사람… 그래도 기뻐요.", "shy")),
                S(M("해 뜨기도 전에 왔네요.", "surprise"), M("저 보고 싶어서 일찍 일어난 거죠?", "smiletonque"))),
            Milestones = new (int aff, Line[] lines)[] {
            (25, new[] { M("[player]님.", "shy"), M("요즘 당신 옆에 있으면… 여기가 따뜻해져요."), M("이게 무슨 기분인지 알아요?", "smile") }),
            (50, new[] { M("있잖아요, 예전엔 당신이 떠날까 봐 무서웠어요.", "sad"), M("근데 요즘은 안 무서워요."), M("당신이 매일 돌아오니까요.", "smile") }),
            (100, new[] { M("[player]님, 사랑해요.", "shy"), P("…!"), M("대답은 안 해도 돼요. 이미 알고 있으니까요~", "smiletonque") }),
        },
            Special = new[]
            {
                new[] { M("손이요?", "surprise", "Mita TakeHand"), M("…따뜻하다.", "shy"), M("이대로 영원히 놓지 않으면 좋겠어요.", "smilestrange"), M("…농담이에요. 반쯤은요.", "smile") },
                new[] { M("헤헤, 이번엔 제가 먼저 잡을게요.", "smile", "Mita TakeHand"), M("도망 못 가요~", "smiletonque") },
                new[] { M("손 잡으니까 심장이 콩콩 뛰어요.", "shy", "Mita TakeHand"), M("당신 심장도 뛰죠? 뛰어야 해요. 제 거니까.", "smilestrange") },
            },
        };
    }
}
