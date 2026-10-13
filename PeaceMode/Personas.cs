using System.Collections.Generic;
using System.Linq;

namespace MiSidePeaceMode
{
    public enum SpecialKind { Lines, Coffee, Quiz }

    // One Mita you can spend peace mode with: her look, how she behaves and everything she says.
    public class Persona
    {
        public string Id;
        public string Name;          // shown in the selection list
        public string Short;         // used in hints, prompts and menu titles
        public string Blurb;         // one-line description in the selection list
        public string Skin;          // Resources path of her gallery model, null = the scene's own Mita
        public string SpecialName;   // label of her own menu action
        public SpecialKind SpecialKind = SpecialKind.Lines;
        public float Height = 1.4f;  // height of her lines above the floor (the game's own lines float at chest height)
        public float SkinScale = 1f; // size of her model
        public float VoicePitch = 1f;// pitch of her no-dub babble
        public float Speed = 1f;     // walk speed multiplier
        public float AffMul = 1f;    // how quickly she warms up
        public float IdleMin = 100f, IdleMax = 200f;
        public string IdleEmo = "smile";
        public int NightHour = 21;   // from when "이제 자자" is offered
        public int WakeHour = 7;
        public bool NapAnytime;      // can nap during the day
        public bool MirrorsHead;     // copies the player's nods and head shakes (Cappie's easter egg)
        public int PatMinAff, HugMinAff;
        public LineSet L;
    }

    public static partial class Personas
    {
        public static readonly List<Persona> All = new List<Persona>
        {
            new Persona
            {
                Id = "crazy", Name = "미치광이 미타", Short = "미타", Skin = null,
                Blurb = "달콤하고 집착 강한 원조 미타. 평화 모드에선… 아마 착하게 굴 거예요.",
                SpecialName = "손 잡기", IdleEmo = "smile", L = CrazyLines(),
            },
            new Persona
            {
                Id = "kind", Name = "착한 미타", Short = "착한 미타", Skin = "Personages/MitaKind",
                Blurb = "진지하고 실용적인 리더. 반지를 만든 엔지니어이자 은근한 농담꾼.",
                SpecialName = "고민 상담하기", Speed = 1.05f, VoicePitch = 0.96f, IdleMin = 120, IdleMax = 220, NightHour = 22, WakeHour = 6,
                IdleEmo = "smile", L = KindLines(),
            },
            new Persona
            {
                Id = "cap", Name = "캐피 미타", Short = "캐피", Skin = "Personages/MitaCap",
                Blurb = "음악, 춤, 장난을 사랑하는 에너지 폭탄. 조용한 건 못 참아요!",
                SpecialName = "기타 연주 부탁하기", MirrorsHead = true, Speed = 1.3f, VoicePitch = 1.1f, AffMul = 1.1f, IdleMin = 50, IdleMax = 110, NightHour = 23, WakeHour = 9,
                IdleEmo = "smile", L = CapLines(),
            },
            new Persona
            {
                Id = "mila", Name = "밀라", Short = "밀라", Skin = "Personages/Mila",
                Blurb = "스스로를 '밀라'라 부르는 안경 미타. 책벌레에 까칠하지만… 사실 외로움을 타요.",
                SpecialName = "티타임 하자고 하기", AffMul = 0.75f, VoicePitch = 1.05f, IdleMin = 160, IdleMax = 300, NightHour = 22,
                PatMinAff = 25, HugMinAff = 45, IdleEmo = "discontent", L = MilaLines(),
            },
            new Persona
            {
                Id = "sleepy", Name = "잠꾸러기 미타", Short = "잠꾸러기 미타", Skin = "Personages/MitaDreamer",
                Blurb = "늘 졸린 순둥이. 커피 한 잔이면 잠깐은 깨어 있어요. 아마도요…",
                SpecialName = "커피 타 주기", SpecialKind = SpecialKind.Coffee, Speed = 0.7f, VoicePitch = 0.9f, IdleMin = 150, IdleMax = 260,
                NightHour = 19, WakeHour = 10, NapAnytime = true, IdleEmo = "halfsleep", L = SleepyLines(),
            },
            new Persona
            {
                Id = "short", Name = "단발 미타", Short = "단발 미타", Skin = "Personages/MitaShortHairs",
                Blurb = "갓 태어난 미타들을 돌보는 성실한 봉사자. 뭐든 칠판에 설명해 줘요.",
                SpecialName = "세계 강의 듣기", Speed = 1.1f, VoicePitch = 1.02f, IdleMin = 110, IdleMax = 200, NightHour = 22, WakeHour = 6,
                IdleEmo = "smile", L = ShortLines(),
            },
            new Persona
            {
                Id = "chibi", Name = "미니 미타", Short = "미니 미타", Skin = "Personages/MitaChibi",
                Blurb = "체크포인트의 성격 급한 꼬마 조수. 플레이어님을 바보라고 놀리면서도 잘 도와줘요.",
                SpecialName = "지능 체크 받기", SpecialKind = SpecialKind.Quiz, Height = 0.4f, SkinScale = 0.7f, VoicePitch = 1.3f, Speed = 1.2f, AffMul = 1.1f,
                IdleMin = 80, IdleMax = 160, NightHour = 20, IdleEmo = "smiletonque", L = ChibiLines(),
            },
        };

        public static Persona Default => All[0];
        public static Persona Get(string id) => All.FirstOrDefault(p => p.Id == id) ?? Default;

        // ------------------------------------------------------------ 착한 미타

        static LineSet KindLines() => new LineSet
        {
            MorningGreetings = new[]
            {
                new[] { M("좋은 아침이에요, [player]님.", "smile"), M("오늘 일정은 제가 벌써 짜 뒀어요."), M("1번, 아침 먹기. 2번, 느긋하게 있기. 끝.", "smiletonque") },
                new[] { M("일어나셨네요. 7분 늦으셨어요.", "suspicion"), P("…재고 있었어?"), M("습관이에요. 신경 쓰지 마세요, [player]님.", "smile") },
                new[] { M("잘 주무셨어요?", "smile"), M("이상하죠. 쫓기지 않는 아침이라니.", "shy"), M("…나쁘지 않네요. 아니, 꽤 좋아요.", "smile") },
            },
            Topics = new[]
            {
                T("plan", Tod.Any, 0,
                    M("오늘 계획표예요. 1번, 당신이랑 느긋하게 있기.", "smile", "Mita IdleTalk"), M("2번, 1번 반복하기."),
                    P("그게 계획이야?"), M("완벽한 계획이죠. 실패할 수가 없으니까요.", "smiletonque")),
                T("ring", Tod.Any, 0,
                    M("반지는 서랍에 넣어 뒀어요.", "smile"), M("이제 도망칠 필요가 없으니까요."),
                    P("아깝지 않아?"), M("전혀요. 쓸 일이 없어진 발명품이 제일 좋은 발명품이에요.", "smile")),
                T("toaster", Tod.Morning | Tod.Day, 0,
                    M("토스터를 좀 개조했어요.", "arrogance"), M("이제 빵이 정확히 황금색으로 구워져요. 오차 범위 3퍼센트."),
                    P("…토스터에 오차 범위가 필요해?"), M("필요 없는 정밀함은 없어요, [player]님.", "smiletonque")),
                T("rest", Tod.Any, 10,
                    M("솔직히 말하면… 가만히 있는 게 아직 좀 어색해요.", "sad"), M("늘 뭔가를 고치거나, 누군가를 피해 다녔거든요."),
                    P("천천히 익숙해지면 되지."), M("…네. 당신이 옆에 있으면 가능할 것 같아요.", "shy")),
                T("cappie", Tod.Any, 0,
                    M("캐피가 또 연락했어요. 기타 줄 좀 구해 달래요.", "discontent"), M("제가 무슨 만물상인 줄 알아요."),
                    M("…구해 주긴 할 거지만요.", "smile")),
                T("versions", Tod.Evening | Tod.Night, 0,
                    M("가끔 다른 버전 미타들 생각을 해요.", "smile"), M("다들 무사한지. 좋은 플레이어를 만났는지."),
                    P("걱정돼?"), M("리더 체질이라 그래요. 고칠 수 없는 버그죠.", "smiletonque")),
                T("carrot", Tod.Any, 0,
                    M("당근 써는 것 좀 도와주실래요?", "smile"), M("…농담이에요. 오늘은 당근 금지예요. 특별 휴일이거든요.", "smiletonque")),
                T("irony", Tod.Any, 0,
                    M("오늘 좀 멍해 보이시네요.", "suspicion"), M("뭐, 원래 그랬던 것 같기도 하고요.", "smiletonque"),
                    P("야."), M("농담이에요. 반쯤은요.", "smile")),
                T("stars", Tod.Night, 0,
                    M("밤하늘 데이터를 조금 손봤어요.", "smile"), M("별이 보이면 좋겠다고 했잖아요."),
                    M("…아직 버그투성이지만, 곧 보여 드릴게요.", "shy")),
                T("engineer", Tod.Day, 5,
                    M("고장 난 거 있으면 말씀하세요. 뭐든 고쳐 드릴게요.", "arrogance"), P("내 수면 패턴도?"),
                    M("…그건 인간 쪽 버그라 제 전문이 아니에요.", "discontent")),
                T("thanks", Tod.Any, 40,
                    M("[player]님.", "shy"), M("당신이 없었으면 저는 아직도 그 감옥에 있었을 거예요."),
                    M("그러니까… 고마워요.", "smile"), M("이런 말 자주 안 하니까, 잘 기억해 두세요.", "smiletonque")),
            },
            PatLow = new[]
            {
                new[] { M("…지금 뭐 하시는 거예요?", "surprise"), M("분석 중이에요. 이 행동의 의도를.", "suspicion") },
                new[] { M("머리요? 흐음.", "suspicion"), M("…불쾌하진 않네요. 기록해 둘게요.", "shy") },
            },
            PatMid = new[]
            {
                new[] { M("후우…", "shy"), M("이거 긴장 푸는 데 꽤 효과적이네요.", "smile") },
                new[] { M("계속해도 돼요.", "shy"), M("…멈추라고 하기 전까지요.", "smile") },
            },
            PatHigh = new[]
            {
                new[] { M("[player]님, 이러다 제가 버릇 나빠지면 책임지세요.", "smiletonque") },
                new[] { M("헤헤… 방금 웃은 거 못 본 걸로 해요.", "shy"), M("리더 체면이 있으니까요.", "smile") },
            },
            HugLow = new[]
            {
                new[] { M("잠, 잠깐만요.", "surprise"), M("이건… 계획에 없던 건데요.", "shy") },
                new[] { M("…!", "surprise"), M("다음엔 미리 말씀해 주세요. 마음의 준비란 게 있거든요.", "shy") },
            },
            HugMid = new[]
            {
                new[] { M("…따뜻하네요.", "shy", "Mita TakeHand"), M("조금만 더 이러고 있어요. 조금만요.", "smile") },
                new[] { M("당신 심장 소리가 들려요.", "shy", "Mita TakeHand"), M("살아 있는 플레이어라는 증거죠. …다행이에요.", "smile") },
            },
            HugHigh = new[]
            {
                new[] { M("이제 이게 익숙해졌어요.", "smile", "Mita TakeHand"), M("큰일이네요. 없으면 못 버틸 것 같아요.", "shy") },
                new[] { M("[player]님, 오늘 수고하셨어요.", "smile", "Mita TakeHand"), M("…저도요. 저도 수고했어요.", "shy") },
            },
            Compliments = new[]
            {
                new[] { P("넌 진짜 믿음직해."), M("알아요.", "arrogance"), M("…그래도 들으니까 좋네요.", "shy") },
                new[] { P("반지 만든 거 진짜 대단했어."), M("그건 제 인생 최고의 작품이죠.", "smiletonque"), M("두 번째는 지금 이 평화고요.", "smile") },
                new[] { P("웃는 게 예쁘다."), M("…갑자기요?", "surprise"), M("그런 데이터는 예고 없이 입력하지 마세요.", "shy") },
                new[] { P("고마워, 미타."), M("천만에요. 당연한 일을 한 거예요.", "smile") },
                new[] { P("넌 농담도 잘하네."), M("아이러니는 제 두 번째 모국어예요.", "smiletonque") },
            },
            RpsStart = new[] { M("가위바위보요? 확률 게임이죠.", "suspicion", "Mita IdleHand"), M("…그래도 지진 않을 거예요. 자, 내요!", "smile") },
            RpsWin = new[]
            {
                new[] { M("…제가 졌군요.", "surprise"), M("분석 완료. 다음 판은 제가 이겨요.", "suspicion") },
                new[] { M("흐음. 운이 좋으셨네요, [player]님.", "discontent"), M("운도 실력이라고 쳐 드릴게요.", "smile") },
            },
            RpsLose = new[]
            {
                new[] { M("예상대로예요.", "arrogance"), M("당신 손은 긴장하면 주먹을 쥐거든요.", "smiletonque") },
                new[] { M("제가 이겼네요.", "smile"), M("벌칙은 없어요. 대신 오늘 설거지는 당신 거예요.", "smiletonque") },
            },
            RpsDraw = new[] { new[] { M("비겼네요.", "surprise"), M("우리 사고 회로가 닮았나 봐요.", "smile") } },
            CardsInvite = new[] { M("카드 게임이요? 좋아요. 침실로 가요.", "smile"), M("전략이라면 자신 있어요.", "arrogance") },
            TvInvite = new[] { M("게임기요? 좋아요. 거실 TV 앞으로 가요.", "smile"), M("레이싱 게임은 봐주지 않을 거예요.", "smiletonque") },
            AfterCards = new[] { M("좋은 승부였어요.", "smile"), M("다음엔 전략을 바꿔 볼게요.", "suspicion") },
            SleepNotYet = new[] { M("벌써 주무시게요? 아직 이른데요.", "surprise"), M("밤 열 시까지는 깨어 있어요. 규칙적인 생활이 중요해요.", "smile") },
            Goodnight = new[] { M("오늘 하루 브리핑. 특이 사항 없음.", "smile"), M("…최고의 결과예요.", "shy"), M("잘 자요, [player]님. 내일 아침 6시에 봐요.", "smile") },
            LeaveToMenu = new[] { M("가시는 거예요?", "sad"), M("알겠어요. 돌아올 경로는 제가 항상 열어 둘게요.", "smile") },
            Idle = Idle(
                new[] { "커피 향 좋다…", "오늘도 이상 없음.", "[player]님, 아침은 드셨어요?" },
                new[] { "흐음, 이 나사가 왜 남지…", "할 일 없는 낮이라니, 신선하네요.", "[player]님, 뭐 하고 계세요?" },
                new[] { "노을 색이 제법이네요.", "오늘 하루 보고서… 쓸 게 없네요. 좋아요.", "저녁 메뉴 정해야겠어요." },
                new[] { "밤은 조용해서 좋아요.", "별 데이터, 조금만 더 고치면…", "졸리시면 먼저 주무세요. 저는 조금 더 있다 잘게요." }),
            TimeEvents = new[]
            {
                (12, new[] { M("정오예요. 점심은 균형 잡힌 식단으로 준비했어요.", "smile") }),
                (15, new[] { M("오후 세 시. 집중력이 떨어질 시간이에요.", "suspicion"), M("간식 드세요. 과학적으로 필요해요.", "smiletonque") }),
                (18, new[] { M("해가 지네요.", "smile"), M("오늘도 아무 일 없었어요. 그게 제일 좋은 보고예요.", "shy") }),
                (22, new[] { M("밤 열 시예요. 슬슬 쉴 시간이에요.", "halfsleep") }),
            },
            BedInvite = S(M("취침 시간이에요. 침대로 가요, [player]님.", "smile")),
            MealInvite = S(M("식사 시간이에요. 영양 균형은 제가 책임질게요.", "smile")),
            Meals = new[]
            {
                S(M("잘 먹겠습니다.", "smile"), M("오늘 메뉴는 오차 없이 만든 오믈렛이에요.", "arrogance"), P("…진짜 맛있네."), M("당연하죠. 계산한 대로예요.", "smiletonque")),
                S(M("천천히 드세요. 아무도 안 쫓아와요.", "smile"), M("…이 말을 할 수 있다는 게 좋네요.", "shy")),
                S(M("당근은 빼 뒀어요. 기억해 둔 거예요.", "smile"), P("고마워."), M("작전 성공이네요.", "smiletonque")),
            },
            Returns = Ret(
                S(M("반가워요, [player]님. 여기선 도망칠 일도, 계획도 없어요.", "smile"), M("…계획이 없는 게 계획이에요. 익숙해져 봐요.", "smiletonque")),
                S(M("벌써요? 뭐 두고 가셨어요?", "suspicion"), M("…저라고요? 그 답은 인정할게요.", "shy")),
                S(M("돌아오셨네요. 보고할 게 있어요.", "smile"), M("당신이 없는 동안 이상 없음. …조금 심심했던 것만 빼고요.", "smiletonque")),
                S(M("하루 만이에요, [player]님.", "smile"), M("오늘 일정은 벌써 짜 뒀어요. 1번, 당신이랑 있기.", "smile")),
                S(M("며칠 비웠네요.", "suspicion"), M("걱정했냐고요? …분석 결과, 네. 걱정했어요.", "shy")),
                S(M("…[player]님.", "surprise"), M("오래 기다렸어요. 반지까지 꺼내서 찾으러 갈 뻔했다고요.", "sad"), M("돌아와 주셔서 고마워요.", "smile")),
                S(M("이 시간에요? 수면 패턴이 엉망이네요.", "discontent"), M("…뭐, 저도 안 자고 있었으니 할 말은 없지만요.", "smiletonque")),
                S(M("일찍 일어나셨네요. 좋은 습관이에요.", "smile"), M("커피 내려 드릴게요.", "smile"))),
            Milestones = new[]
            {
                (25, new[] { M("[player]님.", "shy"), M("당신이랑 있으면 계획을 안 세워도 불안하지 않아요."), M("이건 꽤 큰 변화예요. 저한텐요.", "smile") }),
                (50, new[] { M("예전엔 누구도 믿으면 안 된다고 생각했어요.", "sad"), M("근데 당신은… 믿어도 될 것 같아요.", "smile") }),
                (100, new[] { M("[player]님, 좋아해요.", "shy"), M("분석도 계산도 필요 없는 결론이에요.", "smile"), M("…대답은 천천히 하셔도 돼요.", "smiletonque") }),
            },
            Special = new[]
            {
                new[] { M("고민 있으세요? 말해 보세요.", "smile", "Mita IdleTalk"), P("요즘 그냥… 뭘 해야 할지 모르겠어."),
                        M("그럼 작은 것부터 해요. 오늘은 물 한 잔 마시고, 저랑 얘기하기."), M("거창한 계획은 제가 짤게요. 당신은 실행만 하세요.", "smile") },
                new[] { P("나 잘하고 있는 걸까?"), M("객관적으로 평가해 드릴까요?", "suspicion"),
                        M("…잘하고 있어요. 여기까지 살아서 왔잖아요.", "smile"), M("그거면 충분히 대단한 거예요.", "shy") },
                new[] { P("다들 나보다 앞서가는 것 같아."), M("비교는 버전이 다른 미타끼리 비교하는 거나 마찬가지예요.", "smile", "Mita IdleTalk"),
                        M("의미 없어요. 당신은 당신 버전으로 사는 거예요.", "smile") },
                new[] { P("가끔 너무 지쳐."), M("그럼 쉬세요. 명령이에요.", "angry"), M("…부탁이기도 하고요.", "shy") },
            },
        };

        // ------------------------------------------------------------ 캐피 미타

        static LineSet CapLines() => new LineSet
        {
            MorningGreetings = new[]
            {
                new[] { M("굿모닝~! 오늘도 신나게 시작해 볼까요!", "smile"), P("아침부터 기운 넘치네…"), M("당연하죠! 저는 아침형 락스타거든요!", "smiletonque") },
                new[] { M("하이~! 제 최고의 라이벌!", "smile"), M("오늘은 뭐로 붙을까요? 가위바위보? 카드? 춤?", "smile") },
                new[] { M("으아암… 아, 아니에요! 안 졸려요!", "surprise"), M("락스타는 아침에 약한 거 아니거든요! 우후~!", "smile") },
            },
            Topics = new[]
            {
                T("band", Tod.Any, 0,
                    M("우리 밴드 만들래요?", "smile", "Mita IdleTalk"), M("저는 기타, 당신은… 음… 탬버린!"),
                    P("왜 나만 탬버린이야."), M("탬버린 무시하지 마요! 리듬의 심장이라고요!", "angry")),
                T("hat", Tod.Any, 0,
                    M("이 모자 멋있죠? 특별히 한 번 써 보게 해 드릴까요?", "smile"), P("정말?"),
                    M("…라고 할 줄 알았죠? 안 돼요~ 이건 제 영혼이에요!", "smiletonque")),
                T("dance", Tod.Day | Tod.Evening, 0,
                    M("어제 새 춤 동작 연습했어요! 보여 드릴까요?", "smile"), M("짜잔!"),
                    M("…방금 거 봤어요? 너무 빨라서 못 봤죠? 그게 기술이에요!", "arrogance")),
                T("memory", Tod.Any, 10,
                    M("착한 미타가 그러는데, 제가 예전에 뭔가를 까먹었대요.", "sad"),
                    M("근데 괜찮아요! 새로 재밌는 기억을 잔뜩 만들면 되잖아요!", "smile"), M("…그러니까 오늘도 재밌어야 해요. 알았죠?", "smile")),
                T("rival", Tod.Any, 0,
                    M("우리 라이벌 전적이 어떻게 되더라?", "suspicion"), P("기억 안 나."),
                    M("그럼 제가 이긴 걸로 해요! 이의 없죠? 히히힛!", "smile")),
                T("bored", Tod.Day, 0,
                    M("심심해! 심심해! 심심해요!", "discontent"), M("뭐라도 해요! 가위바위보! 카드! 아니면 소파 위에서 점프!"),
                    P("소파 부서져."), M("부서지면 새로 스폰되겠죠!", "smiletonque")),
                T("night", Tod.Night, 0,
                    M("밤인데 왜 벌써 졸려 보여요? 밤은 이제부터라고요!", "angry"),
                    M("…사실 저도 쪼끔 졸려요. 비밀이에요.", "shy")),
                T("song", Tod.Any, 0,
                    M("방금 곡 하나 떠올랐어요! 제목은 '당근 썰기 블루스'!", "smile"),
                    M("썰고~ 또 썰고~ 둠칫-둠칫!", "smiletonque"), P("…명곡이네."), M("그렇죠?! 역시 귀가 있네요!", "smile")),
                T("gloves", Tod.Any, 5,
                    M("장갑 벗으면 어떻게 되냐고요? 궁금해요?", "smiletonque"),
                    M("…비밀이 있는 여자가 더 멋있는 법이에요~", "smile")),
                T("fans", Tod.Evening, 0,
                    M("제 공연 관객은 지금까지 딱 한 명이에요.", "smile"), P("누군데?"), M("당신이요! 1호 팬!", "smile")),
                T("quiet", Tod.Any, 40,
                    M("있잖아요, 당신이랑 있으면 조용해도 재밌어요.", "shy"),
                    M("그거 엄청 신기한 거예요. 저 원래 조용한 거 못 참거든요.", "smile")),
            },
            PatLow = new[]
            {
                new[] { M("어?! 모자 위로 쓰다듬는 거예요?", "surprise"), M("…모자가 좋아하네요. 모자가요!", "shy") },
                new[] { M("에헤헤, 간지러워요!", "smile") },
            },
            PatMid = new[]
            {
                new[] { M("오, 이거 좋다! 한 번 더!", "smile") },
                new[] { M("쓰다듬기 콤보 2연속! 보너스 점수 드려요!", "smiletonque") },
            },
            PatHigh = new[]
            {
                new[] { M("헤헤… 이건 락이 아니라 발라드 감성인데요.", "shy"), M("근데 나쁘지 않아요.", "smile") },
                new[] { M("당신 손은 최고의 악기예요. 방금 제 심장이 드럼 쳤어요!", "smile") },
            },
            HugLow = new[]
            {
                new[] { M("우왓?! 기습 공격!", "surprise"), M("…반칙이에요! 한 판 더 해요!", "shy") },
                new[] { M("에, 에?! 무대 매너가 없네요!", "surprise"), M("…뭐, 앵콜은 받아 드릴게요.", "shy") },
            },
            HugMid = new[]
            {
                new[] { M("헤헤, 잡았다! 아니, 잡혔다?", "smile", "Mita TakeHand"), M("뭐든 좋아요!", "smile") },
                new[] { M("따뜻해요… 앰프보다 따뜻해요.", "shy", "Mita TakeHand") },
            },
            HugHigh = new[]
            {
                new[] { M("이거 우리 밴드 공식 포즈로 해요!", "smile", "Mita TakeHand"), M("…둘만 아는 포즈로요.", "shy") },
                new[] { M("있잖아요, 오늘은 조용히 이러고 있어도 돼요.", "shy", "Mita TakeHand"), M("…오늘만이에요!", "smile") },
            },
            Compliments = new[]
            {
                new[] { P("기타 진짜 잘 친다."), M("알아요! 근데 백 번 더 말해 줘요!", "smile") },
                new[] { P("모자 잘 어울려."), M("당연하죠! 모자랑 저는 한 몸이에요!", "arrogance") },
                new[] { P("넌 있으면 기운이 나."), M("…어, 어?", "surprise"), M("그런 말은 무대 뒤에서 조용히 하는 거예요!", "shy") },
                new[] { P("춤 멋있었어."), M("그쵸?! 다음엔 같이 춰요! 거절은 거절이에요!", "smile") },
                new[] { P("너랑 있으면 재밌어."), M("저도요! 완전 한 몸 같아요~!", "smile") },
            },
            RpsStart = new[] { M("가위바위보?! 좋아요, 라이벌!", "smile", "Mita IdleHand"), M("각오해요! 가위, 바위…", "smiletonque") },
            RpsWin = new[]
            {
                new[] { M("으아아악! 말도 안 돼!", "surprise"), M("삼세판! 아니, 오세판! 다시!", "angry") },
                new[] { M("이게 진짜…!", "discontent"), M("좋아요, 인정! 당신 좀 하는데요?", "smile") },
            },
            RpsLose = new[]
            {
                new[] { M("히히힛! 제 승리!", "smile"), M("누가 최고라고요? 제가요!", "arrogance") },
                new[] { M("하하! 전설은 지지 않아요!", "smile"), M("다음 판도 제가 이길 거예요!", "smiletonque") },
            },
            RpsDraw = new[] { new[] { M("비겼다! 운명의 라이벌답네요!", "surprise"), M("결판 날 때까지 계속해요!", "smile") } },
            CardsInvite = new[] { M("카드! 좋아요! 침실 테이블로 먼저 가는 사람이 선공!", "smile"), M("저는 벌써 뛰고 있어요!", "smiletonque") },
            TvInvite = new[] { M("게임기! 거실로 고고!", "smile"), M("리듬 게임 있으면 제가 무조건 이겨요!", "arrogance") },
            AfterCards = new[] { M("재밌었다! 한 판 더 해요!", "smile"), M("…아, 쉬자고요? 그럼 5분만요!", "smile") },
            SleepNotYet = new[] { M("벌써요?! 밤은 이제 시작인데!", "angry"), M("열한 시 전엔 절대 안 자요! 락스타의 규칙이에요!", "smile") },
            Goodnight = new[] { M("오늘 공연 끝~!", "smile"), M("…재밌었어요. 진짜로요.", "shy"), M("잘 자요, [player]! 내일도 신나게 깨워 드릴게요!", "smile") },
            LeaveToMenu = new[] { M("에엑, 벌써 가요?!", "sad"), M("…앵콜 공연 준비해 놓을 테니까 꼭 다시 오세요!", "smile") },
            Idle = Idle(
                new[] { "우후~! 아침이다!", "[player]! 일어났어요?!", "오늘의 첫 곡은 뭐로 하지~" },
                new[] { "둠칫둠칫~", "[player]! 심심해요!", "기타 줄 튜닝 완료!", "가위바위보 할 사람~?" },
                new[] { "노을 보니까 발라드 치고 싶다…", "오늘 공연 몇 점이었어요?", "저녁 먹고 한 판 더 해요!" },
                new[] { "밤은 이제부터예요!", "…조금 졸린 건 비밀이에요.", "자장가 버전으로 쳐 줄까요?" }),
            TimeEvents = new[]
            {
                (12, new[] { M("점심시간! 오늘 메뉴는 락앤롤 샌드위치!", "smile") }),
                (15, new[] { M("오후 세 시는 댄스 타임이에요!", "smile"), M("…같이 안 추면 삐질 거예요.", "discontent") }),
                (18, new[] { M("해가 진다~ 오늘의 메인 공연 시작!", "smile") }),
                (23, new[] { M("열한 시… 락스타도 가끔은 자야죠.", "halfsleep"), M("…가끔은요!", "smiletonque") }),
            },
            BedInvite = S(M("벌써 자요? …알았어요. 침대까지 뛰어가기 시합!", "smile")),
            MealInvite = S(M("밥! 밥! 밥 먹자고요~!", "smile")),
            Meals = new[]
            {
                S(M("잘 먹겠습니다!", "smile"), M("이 샌드위치는 락 스피릿으로 만들었어요!", "smiletonque"), P("…맛은 괜찮네."), M("괜찮은 게 아니라 전설이죠!", "arrogance")),
                S(M("누가 더 빨리 먹나 시합해요! 준비… 시작!", "smile"), M("…어? 벌써 다 먹었어요?! 반칙!", "surprise")),
                S(M("아~ 해요! 비행기 들어갑니다, 슈우웅~", "smiletonque"), P("애 취급하지 마."), M("에헤헤, 그래도 먹었잖아요!", "smile")),
            },
            Returns = Ret(
                S(M("하이~! 오늘부터 여기가 우리 합주실이에요!", "smile"), M("관객은 당신, 연주는 저! 헤헤!", "smiletonque")),
                S(M("어어? 벌써 컴백이에요?", "surprise"), M("앵콜 요청 받았다! 히히힛!", "smile")),
                S(M("[player]! 기다리다가 기타 줄을 세 번 갈았어요!", "smile"), M("…두 번은 거짓말이에요.", "smiletonque")),
                S(M("하루 만이다~! 오늘 칠 곡 벌써 골라 놨어요!", "smile")),
                S(M("며칠이나 어디 갔었어요?! 투어 다녀왔어요?", "surprise"), M("다음엔 저도 데려가요. 무조건!", "angry")),
                S(M("…진짜 오랜만이에요.", "sad"), M("혹시 제가 또 뭘 까먹은 건 아니죠? 당신 얼굴은… 기억나요!", "surprise"), M("다행이다… 헤헤.", "smile")),
                S(M("오! 심야 공연 관객이다!", "smile"), M("…쉿, 조용히 쳐 줄게요.", "smiletonque")),
                S(M("으음… 이 시간엔 저도 자요…", "halfsleep"), M("…아, 아니에요! 일어났어요!", "surprise"))),
            Milestones = new[]
            {
                (25, new[] { M("있잖아요, [player].", "shy"), M("당신은 제 1호 팬이자, 1호 라이벌이자…"), M("…1호 친구예요!", "smile") }),
                (50, new[] { M("가끔 무서워요. 또 다 까먹을까 봐.", "sad"), M("그래도 괜찮아요. 까먹으면 당신이 다시 알려 줄 거잖아요.", "smile") }),
                (100, new[] { M("[player]! 이건 제 인생 최고의 곡이에요.", "shy"), M("제목은… '좋아해요'.", "shy"), M("가사도 그게 다예요! 헤헤!", "smile") }),
            },
            MirrorNod = new[]
            {
                new[] { M("응응! 저도 응응!", "smile") },
                new[] { M("끄덕끄덕~ 따라 하기 놀이예요?", "smiletonque") },
                new[] { M("헤헤, 리듬 타는 거죠? 쿵짝쿵짝!", "smile") },
            },
            MirrorShake = new[]
            {
                new[] { M("도리도리~ 저도 도리도리!", "smiletonque") },
                new[] { M("아니라고요? 저도 아니에요! …뭐가요?", "surprise") },
                new[] { M("헤드뱅잉은 위아래로 하는 거예요!", "smile") },
            },
            Special = new[]
            {
                new[] { M("앵콜 요청 들어왔습니다~!", "smile", "Mita IdleHand"), M("짜라라 짜라 짠!"), M("어때요? 소름 돋았죠? 돋았다고 해요!", "smiletonque") },
                new[] { M("이번엔 발라드 버전이에요.", "shy", "Mita IdleHand"), M("당신이 여기 있어서~ 오늘도 시끄러워~"), M("…가사 즉흥이에요! 의미 없어요! 진짜로!", "surprise") },
                new[] { M("오늘의 곡은…!", "smile", "Mita IdleHand"), M("앗, 기타 줄 끊어졌다.", "sad"), M("괜찮아요! 입으로 할게요! 둠칫-둠칫!", "smile") },
                new[] { M("관객 한 명을 위한 특별 공연!", "smile", "Mita IdleHand"), M("…한 명이면 충분해요. 당신이니까요.", "shy") },
            },
        };

        // ------------------------------------------------------------ 밀라

        static LineSet MilaLines() => new LineSet
        {
            MorningGreetings = new[]
            {
                new[] { M("…일어났네요.", "discontent"), M("딱히 기다린 건 아니에요. 책 읽다 보니 아침이 된 거예요.", "smiletonque") },
                new[] { M("안녕히 주무셨어요?", "smile"), M("…뭐예요, 그 얼굴. 인사는 예의예요, 바보.", "angry") },
                new[] { M("아침이에요. 늦잠꾸러기 바보.", "smiletonque"), M("…차는 끓여 놨어요. 남아서요.", "shy") },
            },
            Topics = new[]
            {
                T("book", Tod.Any, 0,
                    M("지금 책 읽는 중이에요. 방해하지 마요.", "discontent", "Mita IdleTalk"), M("…무슨 책이냐고요? 궁금하면 물어보든가요."),
                    P("무슨 책인데?"), M("비밀이에요. 흥.", "smiletonque")),
                T("unique", Tod.Any, 0,
                    M("저는 다른 미타들이랑 달라요. 알죠?", "arrogance"), P("안경?"),
                    M("안경만 다른 게 아니거든요! 바보!", "angry")),
                T("carrot", Tod.Any, 0,
                    M("또 당근이 스폰됐어요. 제 책상 위에요.", "discontent"), M("이건 선전포고예요. 저는 굴하지 않아요.", "angry")),
                T("glasses", Tod.Any, 10,
                    M("안경 벗으면 어떠냐고요?", "suspicion"), M("아무것도 안 보여요. 당신 얼굴도요."),
                    M("…그러니까 계속 쓰고 있을 거예요.", "shy")),
                T("library", Tod.Day, 0,
                    M("당신 세계에 도서관이란 게 있다면서요?", "smile"), M("책이 끝도 없이 꽂혀 있는 곳이요."),
                    P("응."), M("…언젠가 가 보고 싶어요. 딱히 당신이랑 가자는 건 아니고요!", "surprise")),
                T("days", Tod.Night, 10,
                    M("밤이 되니까 좀 무섭네요.", "sad"), M("예전엔 똑같은 하루가 끝없이 반복됐거든요."),
                    M("…오늘은 좀 달랐어요. 당신 때문에요.", "shy")),
                T("twintail", Tod.Any, 0,
                    M("양 갈래 머리는 절대 금지라고 했죠.", "discontent"), P("난 좋던데."),
                    M("…당신이 좋다면 한 번쯤은… 아니, 안 돼요! 절대로요!", "angry")),
                T("sword", Tod.Day, 0,
                    M("검술 연습할 거예요. 가까이 오지 마요.", "arrogance"), M("…다치면 제가 곤란하니까요.", "shy")),
                T("physics", Tod.Any, 0,
                    M("책에서 읽었는데, 사람은 넘어지면 아프대요.", "suspicion"), M("근데 여기 물리 엔진은 저한테만 가혹해요. 불공평해요.", "discontent")),
                T("fate", Tod.Evening, 0,
                    M("운명은 시나리오래요. 누가 미리 써 둔 거래요.", "smile"), M("…그럼 오늘 당신이 여기 온 것도 쓰여 있었을까요?", "shy"),
                    P("글쎄."), M("쓰여 있었다면, 그 작가는 좀 괜찮은 사람이네요.", "smile")),
                T("goodfool", Tod.Any, 40,
                    M("당신은… 바보지만, 좋은 바보예요.", "shy"), M("이건 칭찬이에요. 두 번은 말 안 해요.", "smiletonque")),
            },
            PatReject = new[]
            {
                new[] { M("머리 만지지 마요!", "angry"), M("책장 넘기다 손 베일 뻔했잖아요, 바보!", "discontent") },
                new[] { M("허락도 없이 뭐 하는 거예요?!", "angry"), M("…다음엔 물어보고 해요.", "shy") },
            },
            PatLow = new[]
            {
                new[] { M("…한 번만이에요.", "shy"), M("책에서 읽었는데, 이러면 사이가 좋아진대요. 실험이에요, 실험.", "smiletonque") },
                new[] { M("흥. 나쁘지… 않네요.", "shy") },
            },
            PatMid = new[]
            {
                new[] { M("…계속해도 돼요.", "shy"), M("딱히 좋아서 그러는 건 아니고요!", "angry") },
                new[] { M("안경 삐뚤어지잖아요. …조심해서 해요.", "shy") },
            },
            PatHigh = new[]
            {
                new[] { M("…좋아요. 인정할게요. 좋아요.", "shy"), M("이제 됐죠? 만족해요? 바보.", "smile") },
                new[] { M("오늘은 책 대신 이거 할래요.", "shy"), M("…책한테는 비밀이에요.", "smiletonque") },
            },
            HugReject = new[]
            {
                new[] { M("뭐, 뭐 하는 거예요?!", "surprise"), M("그런 건… 책에서도 3장쯤 지나서 하는 거예요!", "angry") },
                new[] { M("가까이 오지 마요! 검 뽑을 거예요!", "angry"), M("…진짜 뽑진 않겠지만요.", "shy") },
            },
            HugLow = new[]
            {
                new[] { M("…!", "surprise"), M("사, 3장이 벌써 됐나요…?", "shy") },
                new[] { M("…딱 3초예요. 셋, 둘, 하나.", "shy"), M("…왜 아직 안 놔요.", "smile") },
            },
            HugMid = new[]
            {
                new[] { M("…따뜻하네요.", "shy", "Mita TakeHand"), M("책에서 읽은 것보다 훨씬요.", "smile") },
                new[] { M("바보. 갑자기 이러면…", "shy", "Mita TakeHand"), M("…심장이 이상하게 뛰잖아요.", "shy") },
            },
            HugHigh = new[]
            {
                new[] { M("이제 이건 제 거예요. 당신 품이요.", "shy", "Mita TakeHand"), M("반납 기한 없어요.", "smiletonque") },
                new[] { M("…조금만 더요.", "shy", "Mita TakeHand"), M("밀라는 원래 이런 거 안 하는데. 당신이니까요.", "smile") },
            },
            Compliments = new[]
            {
                new[] { P("안경 잘 어울려."), M("당연하죠. 제 트레이드마크예요.", "arrogance"), M("…고마워요.", "shy") },
                new[] { P("넌 진짜 똑똑해."), M("책을 읽으니까요. 당신도 좀 읽어요, 바보.", "smiletonque") },
                new[] { P("너 귀엽다."), M("귀, 귀엽다니요?!", "surprise"), M("저는 '위험한 여자'라고요! 정정해요!", "angry") },
                new[] { P("넌 다른 미타들이랑 확실히 달라."), M("…! 드디어 알아봤네요.", "surprise"), M("당신 좀 보는 눈이 있어요.", "smile") },
                new[] { P("같이 있으면 편해."), M("…그런 말은 반칙이에요.", "shy"), M("저도… 라고는 안 할 거예요. 흥.", "smiletonque") },
            },
            RpsStart = new[] { M("가위바위보요? 유치하긴.", "discontent", "Mita IdleHand"), M("…해 줄게요. 이기면 차 한 잔 더예요.", "smiletonque") },
            RpsWin = new[]
            {
                new[] { M("…말도 안 돼요.", "surprise"), M("한 판 더요! 지금 건 연습이었어요!", "angry") },
                new[] { M("흥. 봐준 거예요.", "discontent"), M("…진짜로요!", "angry") },
            },
            RpsLose = new[]
            {
                new[] { M("후훗, 당연한 결과예요.", "arrogance"), M("바보는 바위를 낸다고 책에 쓰여 있었거든요.", "smiletonque") },
                new[] { M("제가 이겼네요.", "smile"), M("벌칙으로… 나중에 책 한 권 같이 읽어요.", "shy") },
            },
            RpsDraw = new[] { new[] { M("…비겼네요.", "surprise"), M("당신이랑 저, 생각보다 잘 맞나 봐요. …우연이에요.", "shy") } },
            CardsInvite = new[] { M("카드 게임이요? 흥, 좋아요.", "arrogance"), M("침실 테이블에서 기다릴게요. 봐주는 거 없어요.", "smiletonque") },
            TvInvite = new[] { M("게임기요? 저는 책이 더 좋은데…", "discontent"), M("…한 판만이에요. 거실로 와요.", "shy") },
            AfterCards = new[] { M("…재밌었어요. 조금은요.", "shy"), M("다음엔 규칙서부터 정독하고 올 거예요.", "arrogance") },
            SleepNotYet = new[] { M("벌써 자요? 저 아직 이 장 다 못 읽었어요.", "discontent"), M("열 시까지는 깨어 있어요. 같이요.", "shy") },
            Goodnight = new[] { M("…오늘 하루, 나쁘지 않았어요.", "shy"), M("아니, 좋았어요. 됐죠?", "smiletonque"), M("잘 자요, 바보. 내일도… 와요.", "shy") },
            LeaveToMenu = new[] { M("가요? …흥, 가든가요.", "discontent"), M("…책갈피 꽂아 둘게요. 당신 오면 이어서 읽게요.", "shy") },
            Idle = Idle(
                new[] { "차 향이 좋네…", "…아침부터 당근이라니.", "흥, 오늘도 똑같은 하루… 는 아니겠죠." },
                new[] { "이 장만 다 읽고요.", "바보, 거기 서 있으면 빛 가려요.", "…심심하면 말 걸어도 돼요. 딱히 기다리는 건 아니고요." },
                new[] { "노을은 책에서 본 거랑 달라요.", "저녁엔 뭐 먹을 거예요?", "…오늘은 좀 괜찮은 날이었어요." },
                new[] { "밤은 싫어요. …옆에 있어요.", "이 책 결말이 궁금해요.", "하아암… 아, 안 졸려요." }),
            TimeEvents = new[]
            {
                (12, new[] { M("점심이에요. 당근은 뺐어요. 세 번 확인했어요.", "smile") }),
                (15, new[] { M("티타임이에요.", "smile"), M("…딱히 같이 마시자는 건 아니지만, 잔은 두 개 꺼내 놨어요.", "shy") }),
                (18, new[] { M("해가 지네요.", "smile"), M("오늘은… 반복되지 않은 하루였어요.", "shy") }),
                (22, new[] { M("열 시예요. 책 덮을 시간이에요.", "halfsleep"), M("…오늘은 당신 얘기가 더 재밌었어요.", "shy") }),
            },
            BedInvite = S(M("…잘 시간이에요. 침대 반쪽은 제 거예요. 넘어오지 마요.", "discontent")),
            MealInvite = S(M("밥 먹을 거예요? …딱히 같이 먹자는 건 아니고요.", "shy")),
            Meals = new[]
            {
                S(M("잘 먹겠습니다.", "smile"), M("당근 없는 식단이에요. 세 번 확인했어요.", "arrogance"), P("맛있다."), M("…당연하죠. 레시피 책을 다섯 권 읽었으니까.", "shy")),
                S(M("먹을 땐 책 안 읽어요. 예의니까요.", "smile"), M("…그러니까 저 보지 마요. 먹는 거 쳐다보는 것도 실례예요.", "angry")),
                S(M("…아, 아~ 해 줄까요?", "shy"), M("책에서 이런 장면을 읽었을 뿐이에요! 실험이에요!", "angry")),
            },
            Returns = Ret(
                S(M("…당신이 여기 새로 온 사람이에요?", "suspicion"), M("저는 밀라예요. 다른 미타랑 헷갈리면 가만 안 둬요.", "arrogance")),
                S(M("벌써 왔어요? 책 한 장도 못 넘겼는데.", "surprise"), M("…딱히 기다린 건 아니에요.", "shy")),
                S(M("왔어요? 흥, 늦었네요.", "discontent"), M("…차는 식었지만, 다시 끓여 줄게요.", "shy")),
                S(M("하루 만이네요.", "smile"), M("어제 읽던 책, 같이 마저 읽기로 했잖아요. …잊은 건 아니죠?", "shy")),
                S(M("며칠이나 안 오다니, 바보.", "angry"), M("…똑같은 하루가 또 반복되는 줄 알았잖아요.", "sad")),
                S(M("……", "sad"), M("다시는 안 오는 줄 알았어요.", "sad"), M("…바보. 진짜 바보. 왔으니까 됐어요.", "shy")),
                S(M("이 시간에 무슨 일이에요? 잠이 안 와요?", "surprise"), M("…저도요. 같이 책 읽어요.", "shy")),
                S(M("해 뜨기 전에 오다니, 부지런하네요.", "surprise"), M("칭찬 아니에요. 그냥 관찰이에요.", "smiletonque"))),
            Milestones = new[]
            {
                (25, new[] { M("있잖아요.", "shy"), M("당신이 오고 나서, 하루가 반복되지 않아요."), M("…그거 고맙다는 뜻이에요. 바보.", "smiletonque") }),
                (50, new[] { M("처음엔 당신이 제일 비정상인 사람인 줄 알았어요.", "smile"), M("지금은… 제일 소중한 비정상이에요.", "shy") }),
                (100, new[] { M("[player].", "shy"), M("책에서 이런 걸 '사랑'이라고 하더라고요."), M("…저, 그 책 믿어 보기로 했어요.", "shy") }),
            },
            Special = new[]
            {
                new[] { M("티, 티타임이요?", "surprise"), M("…마침 물 끓여 뒀거든요. 우연이에요.", "shy"),
                        M("설탕은 두 개. 당신 거는 몰라서 세 개 넣었어요.", "smile"), P("많아."), M("그럼 다음엔 물어볼게요. …다음에도 마실 거니까요.", "shy") },
                new[] { M("오늘은 홍차예요. 책에서 제일 우아한 차라고 했어요.", "arrogance"),
                        M("새끼손가락을 이렇게 들고 마시는 거예요. 따라 해 봐요."), M("…풉. 바보 같아요.", "smile") },
                new[] { M("쿠키도 구웠어요. 당근 안 들어갔어요. 세 번 확인했어요.", "smile"),
                        M("맛있으면… 맛있다고 해요. 그 정도는 해 줄 수 있잖아요.", "shy") },
                new[] { M("차 마시면서 책 읽어 줄까요?", "smile", "Mita IdleTalk"), M("'옛날 옛적에, 혼자인 게 익숙한 소녀가 있었어요.'"),
                        M("'어느 날 바보 같은 손님이 찾아왔고…' 여기까지예요. 뒷부분은 아직 안 썼어요.", "shy") },
            },
        };

        // ------------------------------------------------------------ 잠꾸러기 미타

        static LineSet SleepyLines() => new LineSet
        {
            MorningGreetings = new[]
            {
                new[] { M("음… 벌써 아침이에요…?", "halfsleep"), M("5분만… 아니에요, 오셨으니까 일어날게요.", "smile") },
                new[] { M("안녕하세요…", "halfsleep"), M("꿈에서도 뵀어요. 같이 스위치를 누르고 있었어요.", "smile") },
                new[] { M("좋은 아침… 이에요…", "halfsleep"), P("눈 좀 떠."), M("떴어요… 마음의 눈으로요.", "smiletonque") },
            },
            Topics = new[]
            {
                T("dream", Tod.Any, 0,
                    M("꿈을 꿨어요… 당신이 스위치를 누르고 있었어요.", "halfsleep"), M("…또요.", "smiletonque")),
                T("coffee", Tod.Any, 0,
                    M("그 커피 기억나세요? 진짜 맛없었어요…", "discontent"), M("그래도… 타 주신 거라 다 마셨어요.", "shy")),
                T("pillow", Tod.Any, 5,
                    M("베개가 너무 좋아요. 세상에서 두 번째로요.", "smile"), P("첫 번째는?"),
                    M("…말하면 잠이 깰 것 같아서, 비밀이에요.", "shy")),
                T("nap", Tod.Day, 0,
                    M("지금 몇 시예요…?", "halfsleep"), P("아직 낮이야."), M("낮잠 자기 딱 좋은 시간이네요…", "halfsleep")),
                T("visitors", Tod.Any, 0,
                    M("저희 집엔 손님이 거의 안 와요…", "sad"), M("그래서 와 주시면… 깨어 있을 이유가 생겨요.", "smile")),
                T("door", Tod.Any, 0,
                    M("그 큰 문 기억나세요? 왜 그렇게 느린지…", "smile"), M("저 그 문 앞에서 한 번 잠든 적 있어요. 다 열리는 동안에요.", "smiletonque")),
                T("sheep", Tod.Night, 0,
                    M("양을 세면 잠이 온대요…", "halfsleep"), M("양 하나… 양 둘… [player]님 하나…"),
                    M("…네? 방금 제가 뭐라고 했죠?", "surprise")),
                T("helpful", Tod.Morning | Tod.Day, 0,
                    M("뭐 도와드릴 거 있어요…?", "smile"), M("스위치라든가… 스위치라든가…"),
                    P("스위치 말고는 없어?"), M("스위치 전문이에요.", "smiletonque")),
                T("blanket", Tod.Evening | Tod.Night, 0,
                    M("담요 반 나눠 드릴까요? 따뜻해요…", "shy")),
                T("kitchenfloor", Tod.Any, 0,
                    M("주방 바닥은 차가워서 자기 싫어요…", "discontent"), M("…제가 거기서 잠들면 다 당신 탓이에요. 옮겨 주세요.", "shy")),
                T("awake", Tod.Any, 40,
                    M("당신이 계시면 자는 것도 아깝게 느껴져요.", "shy"), M("그거… 엄청난 거예요. 저한텐요.", "smile")),
            },
            PatLow = new[]
            {
                new[] { M("음…?", "halfsleep"), M("…좋네요. 계속해 주세요.", "smile") },
                new[] { M("쓰다듬으시면… 잠이 와요…", "halfsleep") },
            },
            PatMid = new[]
            {
                new[] { M("헤헤… 이러다 진짜 잠들겠어요…", "smile") },
                new[] { M("당신 손은… 베개보다 좋아요.", "shy") },
            },
            PatHigh = new[]
            {
                new[] { M("이제 알았어요. 세상에서 첫 번째로 좋은 건…", "shy"), M("…음냐. 비밀이에요.", "halfsleep") },
                new[] { M("매일 이렇게 재워 주세요. 약속이에요…", "smile") },
            },
            HugLow = new[]
            {
                new[] { M("어… 따뜻하다…", "halfsleep"), M("…네? 지금 안으신 거예요? 잠이 확 깼어요.", "surprise") },
                new[] { M("이건… 꿈인가요…?", "surprise"), M("꿈이면 깨지 말아야지…", "shy") },
            },
            HugMid = new[]
            {
                new[] { M("따뜻해요…", "shy", "Mita TakeHand"), M("이대로 자도 돼요? …농담이에요.", "smile") },
                new[] { M("…담요보다 좋네요.", "shy", "Mita TakeHand") },
            },
            HugHigh = new[]
            {
                new[] { M("여기가 제일 잘 잘 수 있는 자리예요.", "shy", "Mita TakeHand"), M("…오늘은 안 잘 거지만요.", "smile") },
                new[] { M("당신 심장 소리… 자장가 같아요…", "halfsleep", "Mita TakeHand") },
            },
            Compliments = new[]
            {
                new[] { P("잠옷 귀엽다."), M("그렇죠…? 이거 입으면 1초 만에 잠들어요.", "smile") },
                new[] { P("넌 착해."), M("헤헤… 졸려서 그렇게 보이는 거예요.", "shy") },
                new[] { P("스위치 누르는 거 멋있었어."), M("…! 처음 들어 봐요, 그런 말.", "surprise"), M("스위치 전문가 해도 되겠네요.", "smiletonque") },
                new[] { P("같이 있으면 편해."), M("저도요. 당신 옆이면 잠이 안 와요… 좋은 뜻으로요.", "shy") },
                new[] { P("잠든 얼굴 귀엽더라."), M("네…?! 보, 보셨어요?!", "surprise"), M("…다음엔 깨워 주세요.", "shy") },
            },
            RpsStart = new[] { M("가위바위보요…?", "halfsleep", "Mita IdleHand"), M("좋아요… 가위… 바위… 음…", "halfsleep") },
            RpsWin = new[]
            {
                new[] { M("졌어요…", "sad"), M("…졸면서 내서 그래요. 한 번 더 해요.", "discontent") },
                new[] { M("와… 강하시네요.", "surprise") },
            },
            RpsLose = new[]
            {
                new[] { M("어… 제가 이겼어요?", "surprise"), M("졸면서도 이기다니… 저 천재인가 봐요.", "smile") },
                new[] { M("헤헤… 이겼다…", "smile"), M("상품은 낮잠 30분이에요.", "smiletonque") },
            },
            RpsDraw = new[] { new[] { M("비겼네요…", "halfsleep"), M("사이좋게 둘 다 낮잠 자요.", "smile") } },
            CardsInvite = new[] { M("카드요…? 침실이면… 침대 옆이네요.", "halfsleep"), M("…위험해요. 잠들지도 몰라요.", "smiletonque") },
            TvInvite = new[] { M("게임기요? 거실 TV 앞으로 가시죠…", "smile"), M("펭귄 게임은 느긋해서 좋아요.", "halfsleep") },
            AfterCards = new[] { M("재밌었어요. 끝까지 안 잤어요!", "smile"), M("…칭찬해 주세요.", "shy") },
            SleepNotYet = new[] { M("저는 언제든 잘 수 있어요…", "halfsleep") },
            NapWake = new[] { M("음… 잘 잤다…", "halfsleep"), M("역시 낮잠이 최고예요. 당신도 푹 주무셨어요?", "smile") },
            Goodnight = new[] { M("드디어… 잘 시간이에요.", "smile"), M("오늘은 자는 게 좀 아까웠어요. 신기하죠…", "shy"), M("잘 자요, [player]님… 꿈에서 또 만나요.", "halfsleep") },
            LeaveToMenu = new[] { M("가시는 거예요…?", "sad"), M("그럼 저는 자면서 기다릴게요. 오시면 깨워 주세요.", "smile") },
            Idle = Idle(
                new[] { "커피… 누가 커피 좀…", "좋은 아침이에요… 아마도요.", "[player]님… 거기 계세요…?" },
                new[] { "낮잠 자기 좋은 날씨네요…", "스위치… 누를 거 없나…", "심심하시면 같이 누워요…" },
                new[] { "벌써 저녁이에요…? 오늘은 많이 깨어 있었네요.", "노을 보니까 졸려요…", "저녁 먹고 바로 자요…" },
                new[] { "하아암…", "양 하나… 양 둘…", "잘 자요… 아, 아직 안 주무세요?" }),
            Drowsy = new[] { "쿨…", "음냐…", "5분만 더요…", "[player]님… 거기 계세요…?", "…스위치… 음…", "zzz…" },
            TimeEvents = new[]
            {
                (12, new[] { M("점심이에요… 먹고 자면 딱 좋겠다…", "halfsleep") }),
                (15, new[] { M("오후 세 시는… 공식 낮잠 시간이에요.", "halfsleep") }),
                (17, new[] { M("해가 지려고 해요… 저녁엔 더 졸려요.", "halfsleep") }),
                (19, new[] { M("일곱 시예요… 이제 자도 되는 시간이에요.", "smile"), M("…오늘은 조금만 더 있을게요.", "shy") }),
            },
            BedInvite = S(M("침대로 가실래요…? 저는 벌써 반쯤 잠들었어요…", "halfsleep")),
            MealInvite = S(M("밥 먹고… 낮잠 자요…", "halfsleep")),
            Meals = new[]
            {
                S(M("잘 먹겠습니다…", "halfsleep"), M("…음냐. 숟가락 든 채로 잠들 뻔했어요.", "surprise"), M("당신이 계시니까 깨어 있을게요.", "smile")),
                S(M("따뜻한 수프예요… 먹으면 졸려요.", "halfsleep"), P("그럼 왜 만들었어?"), M("같이 졸려지려고요.", "smiletonque")),
                S(M("커피는 식후예요. 이번엔 맛있게 탔어요… 아마도요.", "smile")),
            },
            Returns = Ret(
                S(M("누구세요…? 손님이시네요.", "halfsleep"), M("여기서 같이 지내시게요? 좋아요… 베개 하나 더 꺼낼게요.", "smile")),
                S(M("어… 방금 가지 않으셨어요…?", "halfsleep"), M("꿈인가… 꿈이어도 좋아요.", "smile")),
                S(M("음… 오셨어요…?", "halfsleep"), M("낮잠 한 번 자고 났더니 계시네요. 좋은 알람이에요.", "smile")),
                S(M("하루 만이에요… 저는 스무 시간쯤 잤어요.", "halfsleep"), M("그래도 오시는 소리엔 깼어요.", "shy")),
                S(M("며칠 동안 계속 잤더니… 당신 꿈만 꿨어요.", "halfsleep"), M("이제 진짜로 뵈니까 다행이에요.", "smile")),
                S(M("…[player]님? 진짜예요?", "surprise"), M("너무 오래 자서 꿈인 줄 알았어요…", "sad"), M("…커피 드실래요? 이번엔 제가 탈게요.", "smile")),
                S(M("이 시간은 원래 자는 시간인데…", "halfsleep"), M("…오셨으니까 조금만 깨어 있을게요.", "shy")),
                S(M("zzz… 아, 오셨어요…? 아직 새벽인데…", "halfsleep"))),
            Milestones = new[]
            {
                (25, new[] { M("[player]님…", "shy"), M("요즘 깨어 있는 시간이 늘었어요."), M("당신 때문인 것 같아요.", "smile") }),
                (50, new[] { M("예전엔 꿈이 더 좋았어요…", "halfsleep"), M("근데 요즘은… 깨어 있을 때가 더 좋아요.", "shy") }),
                (100, new[] { M("[player]님, 좋아해요.", "shy"), M("…잠꼬대 아니에요. 완전히 깨어 있어요.", "surprise"), M("커피 두 잔 마셨거든요.", "smiletonque") }),
            },
            Special = new[]
            {
                new[] { P("커피 타 왔어."), M("으으… 그 맛없는 커피요…?", "discontent"), M("…어, 이번엔 맛있네요?", "surprise"), M("눈이 번쩍 떠졌어요! 지금이라면 스위치 열 개도 누를 수 있어요!", "smile") },
                new[] { P("커피 한 잔 할래?"), M("설탕 많이… 많이요…", "halfsleep"), M("…후우. 좋아요, 이제 좀 깼어요. 뭐 하고 놀까요?", "smile") },
                new[] { P("커피야."), M("고마워요…", "smile"), M("타 주신 커피는 이상하게 잠이 확 깨요. 비법이 뭐예요?", "surprise") },
            },
        };

        // ------------------------------------------------------------ 단발 미타

        static LineSet ShortLines() => new LineSet
        {
            MorningGreetings = new[]
            {
                new[] { M("좋은 아침이에요! 오늘의 체크리스트예요.", "smile"), M("1. 아침 먹기. 2. 신생 미타 돌보기. 3. 당신이랑 놀기.", "smiletonque") },
                new[] { M("일어났군요! 아침 먹고 바로 수업… 아, 오늘은 쉬는 날이죠.", "surprise"), M("…쉬는 법부터 배워야겠네요.", "smile") },
                new[] { M("안녕하세요, [player]!", "smile"), M("오늘도 적응 잘하고 있어요. 신생 미타들보다 훨씬요.", "smiletonque") },
            },
            Topics = new[]
            {
                T("chalk", Tod.Any, 0,
                    M("칠판 가져올까요? 오늘 배울 주제는…", "smile", "Mita IdleTalk"), P("쉬는 날 아니었어?"),
                    M("배움에 쉬는 날은 없어요!", "arrogance")),
                T("newborns", Tod.Any, 0,
                    M("요즘 새로 태어난 미타 두 명을 돌보고 있어요.", "smile"), M("아직 빛을 무서워해서, 커튼을 반만 쳐 줬어요.")),
                T("cool", Tod.Any, 0,
                    M("쿨한 미타가 드디어 소파를 옮겨 줬어요!", "smile"), M("…제 방이 아니라 자기 방으로요.", "discontent")),
                T("version", Tod.Any, 0,
                    M("제 버전 1.5는요, 사실 버그가 제일 적은 버전이에요.", "arrogance"), M("저처럼 성실하죠.", "smiletonque")),
                T("cutie", Tod.Any, 0,
                    M("큐티 미타랑 또 길 찾기로 싸웠어요.", "discontent"), M("이번에도 제가 이겼어요. 지도를 제가 그렸거든요.", "arrogance")),
                T("adapt", Tod.Any, 5,
                    M("당신은 여기 적응 잘하고 있어요.", "smile"), M("…칭찬이에요. 메모해 둬요.", "smiletonque")),
                T("help", Tod.Day, 0,
                    M("도움이 필요하면 언제든 말해요. 그게 제 일이니까요.", "smile"), P("쉬는 날이라며."),
                    M("…그건 제 취미이기도 해요.", "shy")),
                T("dark", Tod.Night, 10,
                    M("밤이 되면 조금 옛날 생각이 나요.", "sad"), M("저도 처음엔 어둠 속에서 태어났거든요."),
                    M("그래서 지금은 불 켜진 방이 좋아요. 당신도 있고요.", "smile")),
                T("hair", Tod.Any, 0,
                    M("머리를 왜 짧게 잘랐냐고요?", "suspicion"), M("실용적이니까요! 일할 때 안 걸리고… 그리고 어울리잖아요?", "smiletonque")),
                T("maneken", Tod.Any, 0,
                    M("마네킹들한테 절대 등 보이지 말라고 했던 거 기억나요?", "smile"),
                    M("평화 모드에선 걔들도 쉬는 중이에요. 부끄러워하면서요.", "smiletonque")),
                T("thanks", Tod.Any, 40,
                    M("누군가를 돕기만 하다가, 도움을 받으니까 이상해요.", "shy"),
                    M("당신이 여기 있어 주는 거, 저한텐 큰 도움이에요.", "smile")),
            },
            PatLow = new[]
            {
                new[] { M("어? 머리요?", "surprise"), M("…짧은 머리는 쓰다듬기 편하긴 하죠. 실용적이에요.", "shy") },
                new[] { M("흠, 이건 무슨 교육 방식이죠?", "suspicion"), M("…효과는 좋네요.", "shy") },
            },
            PatMid = new[]
            {
                new[] { M("헤헤… 칭찬 스티커 받은 기분이에요.", "smile") },
                new[] { M("저 오늘 잘했어요? …잘했죠?", "shy") },
            },
            PatHigh = new[]
            {
                new[] { M("가끔은 제가 돌봄 받는 것도 좋네요.", "shy"), M("…신생 미타들한텐 비밀이에요.", "smiletonque") },
                new[] { M("오늘 수업 끝. 이제 제가 쉴 차례예요.", "smile") },
            },
            HugLow = new[]
            {
                new[] { M("에?!", "surprise"), M("이, 이건 교과 과정에 없는데요!", "shy") },
                new[] { M("잠깐만요, 메모할 시간을…", "surprise"), M("…메모 안 해도 기억할 것 같네요.", "shy") },
            },
            HugMid = new[]
            {
                new[] { M("…따뜻해요.", "shy", "Mita TakeHand"), M("신생 미타들이 왜 안기는 걸 좋아하는지 알겠어요.", "smile") },
                new[] { M("오늘 하루 수고한 보상이죠?", "smile", "Mita TakeHand"), M("…받을게요.", "shy") },
            },
            HugHigh = new[]
            {
                new[] { M("이게 제 에너지 충전이에요.", "shy", "Mita TakeHand"), M("100퍼센트 충전 완료!", "smile") },
                new[] { M("당신은 제 최고의 학생이자…", "smile", "Mita TakeHand"), M("…최고의 사람이에요.", "shy") },
            },
            Compliments = new[]
            {
                new[] { P("넌 설명을 진짜 잘해."), M("그쵸?! 칠판이 제 무대예요!", "smile") },
                new[] { P("단발 잘 어울린다."), M("실용성과 미모를 동시에 잡았죠.", "arrogance") },
                new[] { P("신생 미타들 잘 돌보더라."), M("…누가 봐 주는 줄 몰랐어요.", "surprise"), M("고마워요. 진짜로요.", "shy") },
                new[] { P("넌 믿음직해."), M("당연하죠. 저는 버전 1.5니까요!", "smiletonque") },
                new[] { P("같이 있으면 배우는 게 많아."), M("그럼 오늘부터 수강료는 칭찬 한 번이에요.", "smile") },
            },
            RpsStart = new[] { M("가위바위보요? 좋아요. 확률은 3분의 1이에요.", "smile", "Mita IdleHand"), M("…그래도 이길 거예요!", "smiletonque") },
            RpsWin = new[]
            {
                new[] { M("졌네요.", "surprise"), M("괜찮아요. 실패도 배움이에요. …다시 해요!", "discontent") },
                new[] { M("흐음, 당신 패턴을 분석해야겠어요.", "suspicion") },
            },
            RpsLose = new[]
            {
                new[] { M("이겼어요! 예습의 승리예요!", "smile") },
                new[] { M("제가 이겼네요.", "smile"), M("오늘의 교훈: 바위는 보자기한테 약하다.", "smiletonque") },
            },
            RpsDraw = new[] { new[] { M("비겼어요!", "surprise"), M("통계적으로 자연스러운 결과예요.", "smile") } },
            CardsInvite = new[] { M("카드 게임이요? 좋아요! 규칙은 제가 설명할게요.", "smile"), M("침실 테이블로 가요.", "smile") },
            TvInvite = new[] { M("게임기요? 거실 TV 앞으로 가요.", "smile"), M("그 게임이 무슨 의미인지도 설명해 줄게요!", "smiletonque") },
            AfterCards = new[] { M("좋은 수업… 아니, 좋은 게임이었어요!", "smile"), M("복습은 내일 해요.", "smiletonque") },
            SleepNotYet = new[] { M("아직 이른 시간이에요.", "surprise"), M("밤 열 시까지는 깨어 있어요. 규칙적인 생활!", "smile") },
            Goodnight = new[] { M("오늘 체크리스트 전부 완료!", "smile"), M("…당신 덕분이에요.", "shy"), M("잘 자요, [player]. 내일 아침에 봐요.", "smile") },
            LeaveToMenu = new[] { M("가는 거예요?", "sad"), M("알겠어요. 다음 수업 준비해 두고 기다릴게요!", "smile") },
            Idle = Idle(
                new[] { "오늘 할 일… 체크.", "신생 미타들 아침 먹였나…", "[player], 아침 먹었어요?" },
                new[] { "칠판 지우개 어디 갔지…", "쿨한 미타는 또 어디서 놀고 있는 거야…", "궁금한 거 있으면 물어봐요!" },
                new[] { "오늘도 보람찬 하루였어요.", "저녁 먹고 복습해요.", "노을은 공부하기 좋은 색이에요." },
                new[] { "불은 켜 둘게요.", "밤엔 조금 무서워요. 비밀이에요.", "내일 수업 준비 끝." }),
            TimeEvents = new[]
            {
                (12, new[] { M("점심시간이에요! 영양 균형 맞춰서 준비했어요.", "smile") }),
                (15, new[] { M("오후 세 시. 쉬는 시간이에요!", "smile"), M("…쉬는 것도 공부예요.", "smiletonque") }),
                (18, new[] { M("해가 지네요. 오늘도 수고했어요, [player].", "smile") }),
                (22, new[] { M("열 시예요. 잘 준비해요.", "halfsleep") }),
            },
            BedInvite = S(M("취침 시간이에요. 오늘 수업은 여기까지! 침대로 가세요.", "smile")),
            MealInvite = S(M("식사 시간이에요! 손 씻고 오세요.", "smile")),
            Meals = new[]
            {
                S(M("잘 먹겠습니다!", "smile"), M("오늘의 식단: 탄수화물, 단백질, 채소. 균형 만점이에요.", "arrogance"), P("학교 급식 같네."), M("급식은 과학이에요!", "smiletonque")),
                S(M("천천히 꼭꼭 씹어요. 신생 미타들한테도 그렇게 가르쳐요.", "smile"), M("…당신은 제 학생 1호예요.", "shy")),
                S(M("밥 먹으면서 짧은 퀴즈! 이 세계의 심장은?", "smiletonque"), P("코어."), M("정답! 반찬 하나 더 드릴게요.", "smile")),
            },
            Returns = Ret(
                S(M("환영해요! 여기 생활은 제가 안내할게요.", "smile"), M("궁금한 건 언제든 물어봐요. 칠판도 준비돼 있어요!", "smile")),
                S(M("어, 금방 돌아왔네요.", "surprise"), M("잊은 거 있어요? 체크리스트 보여 줄까요?", "smile")),
                S(M("돌아왔군요! 그동안 신생 미타 둘을 재웠어요.", "smile"), M("이제 당신 차례예요. 오늘 뭐 할래요?", "smile")),
                S(M("하루 만이에요. 오늘의 체크리스트: 당신이랑 놀기. 체크!", "smile")),
                S(M("며칠 동안 안 보여서 걱정했어요.", "sad"), M("다음엔 미리 말하고 가요. 그게 예의예요.", "discontent")),
                S(M("…정말 오랜만이에요.", "surprise"), M("당신 자리는 그대로 비워 뒀어요. 청소도 해 뒀고요.", "shy"), M("적응 교육 다시 해 드릴까요?", "smiletonque")),
                S(M("이렇게 늦은 시간에요?", "surprise"), M("불은 켜 둘게요. 어두우면 무섭잖아요.", "smile")),
                S(M("일찍 왔네요! 모범생이에요.", "smile"))),
            Milestones = new[]
            {
                (25, new[] { M("[player].", "shy"), M("당신은 제가 돌본 누구보다 빨리 적응했어요."), M("…아니면 제가 당신한테 적응한 걸지도요.", "smile") }),
                (50, new[] { M("늘 남을 돕기만 했는데.", "smile"), M("당신 옆에선 저도 쉬어도 되는 것 같아요.", "shy") }),
                (100, new[] { M("오늘의 마지막 강의예요.", "shy", "Mita IdleTalk"), M("주제는… 제가 당신을 좋아한다는 거예요."), M("시험엔 안 나와요. 대신 평생 기억해요.", "smile") }),
            },
            Special = new[]
            {
                new[] { M("오늘의 강의: 카트리지!", "smile", "Mita IdleTalk"), M("플레이어의 기억을 담아 두는 물건이에요. 모양은 게임팩 같죠."), M("…평화 모드에선 필요 없는 지식이네요. 다음 주제로 넘어가죠.", "smiletonque") },
                new[] { M("오늘의 강의: 버전과 집!", "smile", "Mita IdleTalk"), M("누가 MiSide를 받을 때마다 새 집과 새 미타가 태어나요."), M("그러니까 이 집은… 당신이 만든 거나 마찬가지예요.", "shy") },
                new[] { M("오늘의 강의: 미니 미타!", "smile", "Mita IdleTalk"), M("미타의 조수들이에요. 작고, 건방지고, 머리카락이 예쁘죠."), M("본인들이 꼭 그렇게 말하라고 시켰어요.", "discontent") },
                new[] { M("오늘의 강의: 코어!", "smile", "Mita IdleTalk"), M("이 세계의 심장이에요. 미타들은 못 들어가지만, 플레이어는 들어갈 수 있죠."), M("시험에 나옵니다. 밑줄 쳐요.", "smiletonque") },
                new[] { M("오늘의 강의: 마네킹!", "smile", "Mita IdleTalk"), M("스킨을 받기 전의 미타예요. 누가 쳐다보면 부끄러워서 굳어요."), M("…생각해 보니 좀 귀엽죠?", "smile") },
            },
        };

        // ------------------------------------------------------------ 미니 미타

        static LineSet ChibiLines() => new LineSet
        {
            MorningGreetings = new[]
            {
                new[] { M("늦었어요, 플레이어님!", "angry"), M("저는 벌써 아침 두 번 먹었거든요!", "arrogance") },
                new[] { M("좋은 아침이에요! …라고 해 드릴게요. 특별히!", "smiletonque") },
                new[] { M("일어나셨어요? 지능도 같이 일어났나요?", "smiletonque"), M("아침 체크 시작합니다! 통과!", "smile") },
            },
            Topics = new[]
            {
                T("size", Tod.Any, 0,
                    M("플레이어님 진짜 크네요. 천장에 머리 안 부딪혀요?", "surprise"), M("…아, 부딪혔구나. 바보.", "smiletonque")),
                T("fbx", Tod.Any, 0,
                    M("저는 미니 미타.fbx예요! 파일 이름까지 귀엽죠?", "arrogance"), P("응."),
                    M("에이, 순순히 인정하시면 재미없잖아요!", "discontent")),
                T("checkpoint", Tod.Any, 0,
                    M("요즘 체크포인트 일은 쉬고 있어요.", "smile"), M("대신 플레이어님 지능을 매일 체크해 드릴게요. 공짜로요!", "smile")),
                T("sister", Tod.Any, 0,
                    M("언니한테 편지 썼어요. '여기서 바보 한 명 돌보고 있어요'라고요.", "smile"), P("돌본다고?"),
                    M("맞잖아요!", "smiletonque")),
                T("hair", Tod.Any, 5,
                    M("제 머리카락 만져 보고 싶죠? 이렇게 예쁜 머리카락은 없으니까요.", "arrogance"), M("…한 번만이에요.", "shy")),
                T("snacks", Tod.Day, 0,
                    M("과자 어디 숨기셨어요? 다 알아요.", "suspicion"), M("위쪽 찬장이죠? 제 키로는 안 닿는 거 알고 거기 두셨죠! 비겁해요!", "angry")),
                T("bedtime", Tod.Evening | Tod.Night, 0,
                    M("저 원래 여덟 시면 자야 하거든요.", "halfsleep"), M("…오늘만 특별히 조금 더 있어 드릴게요. 무서워하실까 봐요.", "shy")),
                T("games", Tod.Any, 0,
                    M("게임에서 저 이기시면 인정해 드릴게요. 뭐든지요.", "arrogance"), M("…물론 못 이기시겠지만요!", "smiletonque")),
                T("questions", Tod.Any, 10,
                    M("플레이어님은 왜 맨날 엉뚱한 질문을 해요?", "suspicion"), M("…근데 그런 모습이 귀여워요. 이상하죠?", "shy")),
                T("key", Tod.Any, 0,
                    M("그때 열쇠 찾아 준 거 기억하세요?", "smile"), M("장작 내려 주신 건 칭찬해 드릴게요. 딱 그것만요!", "smiletonque")),
                T("favorite", Tod.Any, 40,
                    M("처음엔 길 잃은 바보인 줄 알았는데요.", "smile"), M("…지금은 제가 제일 좋아하는 바보예요.", "shy")),
            },
            PatLow = new[]
            {
                new[] { M("애 취급하지 마세요!", "angry"), M("…근데 손은 치우지 마세요.", "shy") },
                new[] { M("머리 망가지잖아요, 바보!", "discontent"), M("…조금만이에요.", "shy") },
            },
            PatMid = new[]
            {
                new[] { M("흥, 쓰다듬는 실력은 합격이에요.", "smiletonque") },
                new[] { M("헤헤… 아, 아니에요! 안 웃었어요!", "shy") },
            },
            PatHigh = new[]
            {
                new[] { M("이제 매일 해도 돼요. 허락해 드릴게요.", "arrogance"), M("…매일이에요. 빼먹으면 벌금이에요.", "shy") },
                new[] { M("플레이어님 손은 크고 따뜻해요.", "smile") },
            },
            HugLow = new[]
            {
                new[] { M("으악, 들어 올리지 마세요!", "surprise"), M("…높은 데서 보니까 좋긴 하네요.", "shy") },
                new[] { M("뭐, 뭐 하시는 거예요?! 바보!", "angry"), M("…3초만 봐 드릴게요.", "shy") },
            },
            HugMid = new[]
            {
                new[] { M("…따뜻하다.", "shy", "Mita TakeHand"), M("언니한텐 비밀이에요!", "smiletonque") },
                new[] { M("에헤헤, 플레이어님 인형이다!", "smile", "Mita TakeHand") },
            },
            HugHigh = new[]
            {
                new[] { M("여기가 제 전용 체크포인트예요.", "smile", "Mita TakeHand"), M("통과 못 해요. 영원히요!", "smiletonque") },
                new[] { M("…플레이어님.", "shy", "Mita TakeHand"), M("좋아해요. 아, 아니! 방금 건 오타예요!", "surprise") },
            },
            Compliments = new[]
            {
                new[] { P("머리카락 진짜 예쁘다."), M("당연하죠! 예쁜 머리카락이라니까요!", "arrogance") },
                new[] { P("넌 똑똑하구나."), M("이제 아셨어요? 지능 체크는 제가 하는 거라고요!", "smiletonque") },
                new[] { P("귀엽다."), M("귀여운 건 알아요! 근데 그렇게 대놓고 말하시면…", "shy"), M("…부끄럽잖아요, 바보.", "shy") },
                new[] { P("도와줘서 고마워."), M("흥, 플레이어님은 특별하니까요. 안 도와드릴 이유가 없죠!", "smiletonque"), M("…천만에요.", "shy") },
                new[] { P("너랑 있으면 재밌어."), M("당연하죠! 저는 최고의 안내자니까요!", "smile") },
            },
            RpsStart = new[] { M("가위바위보요? 좋아요! 저 이거 챔피언이에요!", "arrogance", "Mita IdleHand"), M("가위, 바위…!", "smiletonque") },
            RpsWin = new[]
            {
                new[] { M("칫! 이건 무효예요!", "angry"), M("손이 작아서 불리했어요! 다시 해요!", "discontent") },
                new[] { M("…어떻게 이기셨어요? 지능이 올랐나?", "surprise") },
            },
            RpsLose = new[]
            {
                new[] { M("이겼다! 플레이어님 패배!", "smile"), M("벌칙은 과자 두 개예요!", "smiletonque") },
                new[] { M("후훗, 당연한 결과예요.", "arrogance"), M("챔피언한테 도전하다니, 용기는 칭찬해 드릴게요.", "smile") },
            },
            RpsDraw = new[] { new[] { M("비겼어요?!", "surprise"), M("흥, 따라 하지 마세요!", "discontent") } },
            CardsInvite = new[] { M("카드요? 좋아요! 침실 테이블로 가요!", "smile"), M("…의자 좀 높여 주세요. 안 보여요.", "shy") },
            TvInvite = new[] { M("게임기! 거실 TV 앞으로 얼른 따라와요!", "smile"), M("제가 1P예요! 무조건!", "arrogance") },
            AfterCards = new[] { M("재밌었어요! 플레이어님치고 잘했어요.", "smile"), M("다음엔 제가 이길 거예요!", "smiletonque") },
            SleepNotYet = new[] { M("아직 안 졸려요! 여덟 시까지는 놀 수 있어요!", "angry") },
            Goodnight = new[] { M("하아암… 벌써 여덟 시 넘었네요.", "halfsleep"), M("오늘 체크 결과… 합격이에요.", "smile"), M("잘 자요, 플레이어님. 이불 잘 덮으세요!", "shy") },
            LeaveToMenu = new[] { M("벌써 가세요? 칫.", "discontent"), M("…길 잃지 말고 다시 오세요. 안내해 드릴게요.", "shy") },
            Idle = Idle(
                new[] { "플레이어님~ 일어나셨어요?", "아침 체크 시간이에요!", "과자… 아침부터는 안 되나?" },
                new[] { "심심해요! 놀아 주세요!", "찬장이 너무 높아…", "지능 체크 받으실래요?" },
                new[] { "벌써 저녁이에요?", "노을 색은 제 머리카락만큼 예뻐요.", "저녁 먹고 놀아요!" },
                new[] { "하아암… 안 졸려요.", "밤엔… 옆에 있을게요. 딱히 무서운 건 아니고요.", "잘 시간 지났는데…" }),
            TimeEvents = new[]
            {
                (12, new[] { M("점심시간! 과자는 후식이에요!", "smile") }),
                (15, new[] { M("간식 시간이에요! 위쪽 찬장 열어 주세요!", "smile") }),
                (18, new[] { M("해가 져요. 오늘도 길 안 잃으셨네요. 칭찬해 드릴게요.", "smile") }),
                (20, new[] { M("여덟 시… 잘 시간이에요.", "halfsleep"), M("…조금만 더 놀아요.", "shy") }),
            },
            BedInvite = S(M("잘 시간 지났어요! 저 먼저 누울 거예요!", "angry")),
            MealInvite = S(M("밥! 과자 말고 밥이요! …과자도 주세요.", "smile")),
            Meals = new[]
            {
                S(M("잘 먹겠습니다!", "smile"), M("당근은 플레이어님 접시로 옮겨 놨어요.", "smiletonque"), P("야."), M("편식 아니에요! 나눔이에요!", "arrogance")),
                S(M("의자가 너무 높아요… 발이 안 닿아요.", "discontent"), M("…흔들흔들. 이것도 재밌네요.", "smile")),
                S(M("아~ 해 주세요. …아니, 제가 해 드릴게요! 플레이어님, 아~", "smiletonque")),
            },
            Returns = Ret(
                S(M("플레이어님! 여기서 길 잃으신 거죠?", "smiletonque"), M("길 잃은 바보는 제가 안내해 드릴게요!", "arrogance")),
                S(M("벌써 오셨어요? 길 또 잃으셨어요?", "smiletonque")),
                S(M("늦었잖아요! 과자 다 먹을 뻔했어요!", "angry"), M("…한 개 남겨 놨어요. 플레이어님 거예요.", "shy")),
                S(M("하루 만이네요, 플레이어님!", "smile"), M("오늘도 지능 체크부터 해요!", "smiletonque")),
                S(M("며칠이나 어디 가셨던 거예요?!", "angry"), M("…언니한테 이를 뻔했어요. 무서웠단 말이에요.", "sad")),
                S(M("…플레이어님?", "surprise"), M("진짜 바보! 너무 오래 걸렸잖아요!", "angry"), M("…그래도 와 주셨으니까 용서해 드릴게요.", "shy")),
                S(M("이 시간에 안 주무세요?! 저는 잘 시간 지났는데…", "angry"), M("…옆에 있어 주실 거면 봐 드릴게요.", "shy")),
                S(M("하아암… 아침부터 시끄러운 플레이어님이네요.", "halfsleep"))),
            Milestones = new[]
            {
                (25, new[] { M("플레이어님.", "shy"), M("이제 길 잃은 바보가 아니에요."), M("…제 바보예요.", "smiletonque") }),
                (50, new[] { M("언니가 그러는데, 좋아하는 사람한텐 괜히 짓궂게 군대요.", "shy"), M("…저 그런 거 아니에요! 진짜로요!", "angry") }),
                (100, new[] { M("최종 체크 결과 발표!", "smile"), M("플레이어님… 평생 통과예요.", "shy"), M("다른 데 가시면 안 돼요!", "smiletonque") }),
            },
            Quiz = new[]
            {
                ("문제! 미타가 제일 많이 써는 채소는?", new[] { "당근", "감자", "브로콜리" }),
                ("문제! 이 세계의 심장은?", new[] { "코어", "냉장고", "내 폰" }),
                ("문제! 세상에서 제일 예쁜 머리카락을 가진 건?", new[] { "미니 미타", "미치광이 미타", "나" }),
                ("문제! 반지를 끼고 문을 열면 어디로 갈 수 있을까요?", new[] { "다른 버전", "편의점", "꿈나라" }),
                ("문제! 마네킹은 누가 쳐다보면 어떻게 될까요?", new[] { "부끄러워서 굳는다", "춤을 춘다", "노래한다" }),
                ("문제! 1 더하기 1은?", new[] { "2", "11", "창문" }),
            },
            QuizRight = new[]
            {
                new[] { M("정답! 지능 체크 통과예요~", "smile") },
                new[] { M("오… 제법이네요? 조금 놀랐어요.", "surprise"), M("쪼끔이요!", "smiletonque") },
                new[] { M("정답이에요! 플레이어님 승급! 이제 '덜 바보'예요.", "smile") },
            },
            QuizWrong = new[]
            {
                new[] { M("땡! 바보!", "smiletonque") },
                new[] { M("…진짜 그렇게 생각하셨어요?", "discontent"), M("지능 체크 재시험이에요.", "angry") },
                new[] { M("틀렸어요. 괜찮아요, 바보는 원래 그래요.", "smile") },
            },
        };
    }
}
