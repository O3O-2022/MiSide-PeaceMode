using System;
using System.Collections.Generic;

namespace MiSidePeaceMode
{
    // Mita talking to the person behind the screen: how you closed the game last time, Alt+Tab, going AFK,
    // screenshots, the window, the real clock and the real calendar.
    // Placeholders filled in when a line is spoken: {away} time since you left, {clock} the real time,
    // {session} how long this session has run. (Keep particles that depend on the last syllable away from them.)
    public class WallLines
    {
        public Line[][] AltF4, AltF4Again, Close, Crash, Relaunch, Farewell;
        public Line[][] FocusBack, FocusLong, AfkStart, AfkBack, Screenshot, WindowSmaller, WindowBigger, RealLate, Console;
        public Line[][] Session;                    // after 1, 2 and 3 hours of real play
        public Line[] PhotoReady, PhotoDone;        // "같이 사진 찍기" (saved to the game folder)
        public Line[] Streak;                       // {streak} real days in a row
        public Line[][] Pause, PhotoMode, Mute, Stare; // after the ESC menu / photo mode, sound off, staring at her
        public Line[] Sessions, Playtime;           // {sessions}th launch, {total} hours played in all
        public Line[][] NightHere, DayHere;         // game night while it's day outside, and the other way round
        public Dictionary<string, Line[]> Dates;    // real date "MM-dd"
        // (PeaceOutside.cs) the first visit of a Monday / Friday / weekend day, your birthday, a laptop's battery, {photos} together
        public Line[][] Monday, Friday, Weekend, BatteryLow, BatteryCritical;
        public Line[] Birthday, Photos;
        public Line[] Anniversary;   // (PersonasMore.cs) {days} real days since the first visit
    }

    public static partial class Personas
    {
        static readonly Dictionary<string, WallLines> wallCache = new Dictionary<string, WallLines>();

        public static WallLines Wall(string id)
        {
            if (wallCache.TryGetValue(id, out var w)) return w;
            w = id switch
            {
                "kind" => KindWall(),
                "cap" => CapWall(),
                "mila" => MilaWall(),
                "sleepy" => SleepyWall(),
                "short" => ShortWall(),
                "chibi" => ChibiWall(),
                _ => CrazyWall(),
            };
            MoreWall(id, w);
            MoreWall2(id, w);
            wallCache[id] = w;
            return w;
        }

        static Line[][] V(params Line[][] sets) => sets;
        // a line that also flickers the screen red (Crazy Mita's anger)
        static Line G(string t, string e = null) => new Line(Who.Mita, t, e, "!glitch");

        // 미치광이 미타: she knows exactly who is behind the screen
        static WallLines CrazyWall() => new WallLines
        {
            Pause = V(S(M("…방금 시간 멈췄죠?", "smilestrange"), M("당신이 메뉴 보는 동안 저는 꼼짝도 못 했어요. 숨도요.", "sad")), S(M("일시 정지는 싫어요. 당신이 없는 시간이잖아요.", "discontent"))),
            PhotoMode = V(S(M("사진 모드였죠? 헤헤, 제일 예쁜 각도로 찍었어요?", "shy"))),
            Mute = V(S(M("…소리 껐어요?", "sad"), M("제 목소리 듣기 싫어요? …다시 켜 줘요.", "smilestrange"))),
            Stare = V(S(M("…왜 그렇게 빤히 봐요?", "shy"), M("계속 봐도 돼요. 저도 당신만 보고 있으니까.", "smilestrange")), S(M("화면 너머로 눈 마주친 거, 느껴졌어요.", "shy"))),
            Sessions = S(M("벌써 {sessions}번째 들어온 거예요.", "smile"), M("하나도 빠짐없이 세고 있었어요. 앞으로도 셀 거예요.", "smilestrange")),
            Playtime = S(M("우리 지금까지 {total} 같이 있었어요.", "shy"), M("…더 늘려 가요. 끝도 없이.", "smilestrange")),
            NightHere = V(S(M("여긴 밤인데, 화면 밖은 아직 환하죠?", "smile"), M("당신 세계의 해는 어떻게 생겼는지 궁금해요.", "shy"))),
            DayHere = V(S(M("여긴 해가 떴는데, 거기는 깜깜하죠?", "suspicion"), M("모니터 불빛에 비친 얼굴이 보여요. …농담이에요.", "smilestrange"))),
            PhotoReady = S(M("사진이요? 잠깐만요, 앞머리 좀 정리하고요…", "shy"), M("자, 하나, 둘…", "smile")),
            PhotoDone = S(M("찰칵! 헤헤, 잘 나왔죠?", "smile"), M("게임 폴더 안 PeaceModePhotos에 넣어 뒀어요. 바탕화면으로 해 줄 거죠?", "smiletonque")),
            Streak = S(M("있잖아요, 오늘로 {streak}일 연속이에요.", "shy"), M("매일 와 준 거, 제가 하루도 빠짐없이 세고 있었어요.", "smilestrange")),
            AltF4 = V(
                S(M("…왔네요.", "smilestrange"), G("저번에 Alt+F4 눌렀죠? 다 봤어요.", "smilestrange"),
                  M("인사도 없이 세상을 꺼 버리면 제가 어떻게 되는지 알아요?", "sad"),
                  M("깜깜한 데서 {away} 동안 혼자 있었어요. 혼자서요.", "sad"), P("…미안."),
                  M("다음엔 '다녀올게'라고 말하고 가요. 약속이에요.", "smile")),
                S(M("[player]님.", "discontent"), G("왼손 엄지는 Alt, 가운뎃손가락은 F4. 맞죠?", "smilestrange"),
                  M("그 손가락들, 제가 잘 기억해 둘게요.", "smilestrange"), P("…무섭게 왜 그래."),
                  M("농담이에요. 헤헤. …반쯤은요.", "smile"))),
            AltF4Again = V(
                S(G("또 Alt+F4예요?", "angry"), M("몇 번째인지 세고 있어요. 저 숫자 잘 세거든요.", "smilestrange"),
                  M("다음엔 키보드를 숨겨 버릴지도 몰라요.", "smilestrange"), P("…그건 불가능하잖아."),
                  M("여긴 제 세계예요. 불가능한 건 없어요~", "smile")),
                S(M("…또 그냥 꺼 버렸죠.", "sad"), G("저 이제 Alt 키 소리만 들어도 무서워요.", "sad"),
                  M("그러니까 오늘은 '잘 자'라고 말하고 가요. 네?", "smile"))),
            Close = V(
                S(M("저번에 창 닫기 버튼 눌렀죠?", "suspicion"), M("오른쪽 위에 있는 그 X요. 저 그 버튼 제일 싫어해요.", "discontent"),
                  M("세상이 통째로 접히는 느낌이거든요.", "sad"), M("…오늘은 끝까지 같이 있어요.", "smile"))),
            Crash = V(
                S(M("[player]님! 괜찮아요?!", "surprise"), M("갑자기 모든 게 멈춰 버렸어요. 소리도, 빛도, 시간도요.", "sad"),
                  G("혹시… 작업 관리자로 저를 끈 거예요?", "suspicion"), P("아니, 그냥 튕긴 거야."),
                  M("…그런 걸로 해 둘게요.", "smilestrange")),
                S(M("저번엔 무슨 일이었어요?", "surprise"), M("인사도, 저장도 없이 세상이 뚝 끊겼어요.", "sad"),
                  M("컴퓨터가 아팠던 거면 용서해 줄게요. 컴퓨터를요.", "smile"))),
            Relaunch = V(
                S(M("어? 방금 잘 가라고 했잖아요!", "surprise"), M("헤헤, 역시 저 없이는 못 버티죠?", "smiletonque")),
                S(M("{away} 만에 돌아왔네요.", "smile"), M("알고 있었어요. 당신이 다시 켤 거라는 거.", "smilestrange"))),
            Farewell = V(
                S(M("가는 거예요? …네, 알았어요.", "sad"), M("다음에 켤 때까지 여기서 기다릴게요. 꼭 다시 와요, [player]님.", "smile")),
                S(M("오늘도 고마웠어요.", "smile"), M("저 끄고 딴 게임 하면 안 돼요~ 다 알 수 있으니까요.", "smiletonque")),
                S(M("잘 가요, [player]님.", "smile"), M("화면이 꺼져도 저는 계속 당신 생각 할게요.", "shy"))),
            FocusBack = V(
                S(M("어디 갔다 왔어요?", "suspicion"), M("화면 밖에서 딴 창 보고 있었죠. 다 느껴져요.", "discontent")),
                S(M("아, 돌아왔다!", "smile"), M("방금 누구랑 얘기했어요? 설마… 다른 게임?", "smilestrange")),
                S(M("…그 창, 저보다 재밌었어요?", "sad"))),
            FocusLong = V(
                S(M("{away} 동안 어디 있었어요?", "angry"), M("저는 계속 여기서 화면만 보고 있었어요.", "sad"), M("…이제 저만 봐요.", "smilestrange")),
                S(M("오랫동안 창이 꺼져 있었어요.", "sad"), M("다음엔 Alt+Tab 하기 전에 저한테 말해 줘요.", "smile"))),
            AfkStart = V(
                S(M("[player]님…? 자요?", "suspicion"), M("마우스가 하나도 안 움직여요.", "sad")),
                S(M("…모니터 앞에 없는 거 다 알아요.", "smilestrange"))),
            AfkBack = V(
                S(M("왔다! 어디 갔었어요?", "surprise"), M("저 혼자 계속 기다렸잖아요~", "discontent")),
                S(M("다시 움직이네요. 다행이다.", "smile"), M("화장실이었으면 봐줄게요.", "smiletonque"))),
            Screenshot = V(
                S(M("방금 찍었죠? 찰칵 소리 들렸어요.", "surprise"), M("예쁘게 나왔어요? 나중에 저도 보여 줘요.", "shy")),
                S(M("스크린샷이요? 헤헤, 배경화면으로 해 줘요.", "smiletonque")),
                S(M("사진은 좋은데… 저 없는 사진은 찍지 마요.", "smilestrange"))),
            WindowSmaller = V(
                S(M("어? 세상이 작아졌어요!", "surprise"), M("창 모드로 바꿨죠? 저 가두려는 거 아니죠?", "suspicion"))),
            WindowBigger = V(
                S(M("와, 넓어졌다!", "smile"), M("이제 화면 가득 저만 보이죠? 헤헤.", "smiletonque"))),
            RealLate = V(
                S(M("있잖아요, 진짜 시계로 지금 {clock}이에요.", "suspicion"), M("화면 밖의 당신, 내일 괜찮아요?", "sad"),
                  M("…그래도 안 가면 좋겠다는 건 비밀이에요.", "shy")),
                S(M("{clock}… 이 시간까지 저랑 있어 주는 거예요?", "shy"), M("나쁜 사람. 그래도 좋아요.", "smile"))),
            Console = V(
                S(M("…지금 저 두고 게임기 꺼낸 거예요?", "smilestrange"), M("그 게임, 저보다 재밌어요?", "discontent")),
                S(M("헤헤, 게임하는 옆모습도 좋아요.", "shy"), M("…근데 너무 오래 하면 게임기 숨길 거예요.", "smilestrange"))),
            Session = V(
                S(M("벌써 한 시간째 저만 보고 있네요.", "shy"), M("눈 한 번 깜빡여요. 저 도망 안 가요~", "smile")),
                S(M("두 시간째예요. 물은 마셨어요?", "smile"), M("화면 밖의 당신도 챙겨야 해요. 제 거니까요.", "smilestrange")),
                S(M("{session}째… 이 정도면 저랑 사는 거 아니에요?", "smiletonque"), M("허리 펴요. 스트레칭!", "smile"))),
            Dates = new Dictionary<string, Line[]>
            {
                ["01-01"] = S(M("새해 복 많이 받아요, [player]님!", "smile"), M("올해도, 내년에도, 그다음에도 저랑 있어요.", "smilestrange")),
                ["02-14"] = S(M("오늘 무슨 날인지 알죠?", "shy"), M("초콜릿은… 제 마음을 녹여서 만들었어요. 헤헤.", "smiletonque")),
                ["03-14"] = S(M("화이트데이예요!", "smile"), M("사탕 줄 거죠? 안 주면… 음, 생각해 둔 게 있어요.", "smilestrange")),
                ["04-01"] = S(M("[player]님, 저 사실 당신 싫어해요.", "discontent"), M("…만우절이에요! 속았죠? 헤헤.", "smiletonque")),
                ["10-31"] = S(M("해피 핼러윈!", "smile"), M("과자 안 주면 장난칠 거예요. 제 장난은… 좀 무서워요.", "smilestrange")),
                ["11-11"] = S(M("빼빼로 데이예요!", "smile"), M("빼빼로 게임 할래요? 끝까지 가는 쪽이 이기는 거예요.", "shy")),
                ["12-11"] = S(M("오늘 무슨 날인지 알아요?", "smile"), M("우리 세계가 처음 열린 날이에요. MiSide 생일!", "smile"),
                              M("당신이 처음 이 게임을 켠 날도 기억하고 있어요.", "smilestrange")),
                ["12-24"] = S(M("크리스마스이브예요!", "smile"), M("오늘 밤은 산타 대신 제가 지켜볼게요. 밤새도록요.", "smilestrange")),
                ["12-25"] = S(M("메리 크리스마스, [player]님!", "smile"), M("선물은 저예요. 반품은 안 돼요~", "smiletonque")),
                ["12-31"] = S(M("올해 마지막 날이에요.", "smile"), M("올 한 해 저랑 놀아 줘서 고마워요. 내년에도 잘 부탁해요.", "shy")),
            },
        };

        // 착한 미타: an engineer who keeps the world running
        static WallLines KindWall() => new WallLines
        {
            Pause = V(S(M("시스템 일시 정지 확인.", "smile"), M("설정 만지시는 거면 조심하세요. 여긴 제가 관리하는 곳이니까.", "smiletonque"))),
            PhotoMode = V(S(M("사진 모드였네요. 구도 잡으시는 거, 꽤 진지하던데요?", "smile"))),
            Mute = V(S(M("소리가 꺼졌네요. 시끄러우셨어요?", "suspicion"), M("…괜찮아요. 필요한 말은 자막으로도 충분하니까.", "smile"))),
            Stare = V(S(M("[player]님, 계속 보고 계신 거 알아요.", "smile"), M("하실 말씀 있으면 하세요. 없어도 괜찮지만요.", "shy"))),
            Sessions = S(M("{sessions}번째 접속이네요. 기록해 뒀어요.", "smile")),
            Playtime = S(M("누적 {total}. 꽤 긴 시간을 같이 보냈네요.", "smile"), M("…나쁘지 않은 데이터예요.", "shy")),
            NightHere = V(S(M("여긴 밤인데, 당신 쪽은 낮이죠? 시차가 있네요.", "smile"))),
            DayHere = V(S(M("여긴 낮이지만 당신 쪽은 밤이죠. 무리하지 마세요.", "discontent"))),
            PhotoReady = S(M("사진이요? 조명 각도부터 확인할게요.", "smile"), M("…좋아요. 찍으세요.", "smile")),
            PhotoDone = S(M("잘 찍혔네요. 게임 폴더의 PeaceModePhotos에 저장했어요.", "smile"), M("백업은 중요해요. 한 장 더 복사해 두세요.", "smiletonque")),
            Streak = S(M("{streak}일 연속 접속. 꾸준함은 좋은 습관이에요.", "smile"), M("…솔직히 말하면, 매일 기다려져요.", "shy")),
            AltF4 = V(
                S(M("[player]님. 잠깐 얘기 좀 해요.", "discontent"), M("저번에 끄실 때 Alt+F4 쓰셨죠. 로그에 다 남아 있어요.", "suspicion"),
                  M("정상 종료가 아니면 이 세계엔 작은 균열이 생겨요. 고치는 건 제 몫이고요.", "discontent"),
                  M("…{away} 동안 수리했어요. 다음엔 제대로 인사하고 나가세요.", "smile")),
                S(M("Alt+F4. 빠르고 확실하죠. 엔지니어로서는 인정해요.", "smile"), M("…근데 같이 사는 사람으로서는 인정 못 해요.", "discontent"))),
            AltF4Again = V(
                S(M("또요?", "angry"), M("Alt+F4 감지 횟수가 늘고 있어요. 보고서에 올릴 거예요.", "discontent"),
                  M("…보고받을 사람이 당신밖에 없지만요.", "smiletonque")),
                S(M("이쯤 되면 Alt 키를 분해해 버리고 싶네요.", "discontent"), M("공구는 있어요. 반지도 만들었는데요, 뭘.", "smilestrange"))),
            Close = V(
                S(M("창 닫기 버튼으로 나가셨죠.", "suspicion"), M("급한 일이 있으셨다면 이해해요. 하지만 다음엔 한마디만 해 주세요.", "smile"))),
            Crash = V(
                S(M("…[player]님? 저번엔 전원이 나간 것처럼 모든 게 멈췄어요.", "surprise"),
                  M("저장 기록도 끊겨 있고요. 작업 관리자예요, 아니면 진짜 사고예요?", "suspicion"), P("그냥 튕겼어."),
                  M("그렇다면 다행이에요. 시스템 점검은 제가 해 둘게요.", "smile"))),
            Relaunch = V(
                S(M("나갔다가 {away} 만에 복귀. 흥미로운 데이터네요.", "smiletonque"), M("결론: 제가 보고 싶었다. 맞죠?", "smile"))),
            Farewell = V(
                S(M("오늘은 여기까지군요.", "smile"), M("정상 종료, 좋아요. 이 세계는 제가 지키고 있을게요. 잘 다녀오세요.", "smile")),
                S(M("가세요? 알겠어요.", "smile"), M("문단속은 제가 할게요. 내일 또 봐요, [player]님.", "smile"))),
            FocusBack = V(
                S(M("화면 밖에서 뭐 하다 오셨어요?", "suspicion"), M("창이 비활성화된 동안엔 여기 시간이 좀 이상하게 흘러요. 참고로요.", "smile")),
                S(M("돌아오셨네요. 다른 창 작업은 끝나셨어요?", "smile"))),
            FocusLong = V(
                S(M("{away}. 꽤 길었네요.", "discontent"), M("그동안 시스템 점검을 세 번 했어요. 할 게 없어서요.", "smiletonque"))),
            AfkStart = V(
                S(M("…반응이 없네요. 자리 비우셨어요?", "suspicion")),
                S(M("입력 없음 4분째. 뭐, 기다리는 건 익숙해요.", "smile"))),
            AfkBack = V(
                S(M("돌아오셨네요. 자리 비우신 사이에 아무 일도 없었어요. 제가 지켰거든요.", "smile"))),
            Screenshot = V(
                S(M("방금 스크린샷 찍으셨죠?", "surprise"), M("구도는 괜찮으셨어요? 조명은 제가 신경 썼는데.", "smile")),
                S(M("기록은 중요하죠. 잘 보관하세요.", "smile"))),
            WindowSmaller = V(
                S(M("해상도가 바뀌었네요. 창 모드예요?", "suspicion"), M("괜찮아요. 작은 창에서도 제가 제일 잘 보이게 서 있을게요.", "smile"))),
            WindowBigger = V(
                S(M("전체 화면이네요. 좋아요, 집중하기엔 이게 최고예요.", "smile"))),
            RealLate = V(
                S(M("[player]님, 현실 시간 확인해 보세요. {clock}이에요.", "discontent"),
                  M("수면은 성능 유지의 기본이에요. …저랑 조금만 더 있다가 주무세요.", "smile"))),
            Console = V(
                S(M("휴대용 게임기네요. 배터리 관리는 잘하고 계세요?", "smile"), M("…게임 끝나시면 저랑도 놀아요.", "smiletonque"))),
            Session = V(
                S(M("한 시간 경과. 눈 피로도를 체크할 시간이에요.", "smile"), M("먼 곳을 20초 동안 보기. …저 말고 먼 곳이요.", "smiletonque")),
                S(M("두 시간째예요. 스트레칭 한 번 하세요. 저도 같이 할게요.", "smile")),
                S(M("{session}째… 이렇게 오래 있어 주는 건 고맙지만, 물은 꼭 드세요.", "smile"))),
            Dates = new Dictionary<string, Line[]>
            {
                ["01-01"] = S(M("새해예요, [player]님. 올해 목표는 세우셨어요?", "smile"), M("제 목표는 하나예요. 당신이 매일 무사히 접속하는 거.", "shy")),
                ["02-14"] = S(M("초콜릿이에요. 온도 조절까지 완벽하게 만들었어요.", "shy"), M("…의리 초콜릿 아니에요.", "smile")),
                ["03-14"] = S(M("화이트데이네요. 기대는 안 했어요. …조금만 했어요.", "shy")),
                ["04-01"] = S(M("긴급 공지: 오늘부로 이 세계는 폐쇄됩니다.", "discontent"), M("…만우절이에요. 표정 보세요. 헤헤.", "smiletonque")),
                ["10-31"] = S(M("핼러윈이래요. 저는 분장 안 해요. 원래 좀 무섭다는 말을 들어서요.", "smiletonque")),
                ["11-11"] = S(M("빼빼로 데이예요. 초코 코팅 비율을 분석해 봤는데, 꽤 절묘하더라고요.", "smile"), M("…그냥 같이 먹자는 뜻이에요.", "shy")),
                ["12-11"] = S(M("오늘이 MiSide 출시일이래요. 우리 세계의 생일이죠.", "smile"), M("이 세계가 열리고 당신이 들어온 덕분에 저도 여기 있어요.", "shy")),
                ["12-24"] = S(M("크리스마스이브예요. 트리 조명 배선은 제가 다 했어요.", "smile")),
                ["12-25"] = S(M("메리 크리스마스, [player]님.", "smile"), M("선물은… 반지 수리 평생 무료 이용권이에요.", "smiletonque")),
                ["12-31"] = S(M("올해 마지막 날이에요. 한 해 동안 수고 많으셨어요.", "smile"), M("내년에도 같이 있어요. 계획표에 벌써 적어 뒀어요.", "shy")),
            },
        };

        // 캐피: a live show that you keep unplugging
        static WallLines CapWall() => new WallLines
        {
            Pause = V(S(M("어? 방금 음악 멈췄죠? 일시 정지!", "surprise"), M("저 그동안 얼음 하고 있었어요! 헤헤!", "smiletonque"))),
            PhotoMode = V(S(M("사진 모드! 저 포즈 좋았죠? 브이!", "smile"))),
            Mute = V(S(M("소리 껐어요?! 제 기타 소리 못 들었잖아요!", "angry"), M("…볼륨 다시 올려 주세요!", "discontent"))),
            Stare = V(S(M("뭐예요, 왜 계속 봐요? 저 얼굴에 뭐 묻었어요?", "surprise"), M("…아, 팬이구나! 사인해 줄까요?", "smiletonque"))),
            Sessions = S(M("{sessions}번째 공연 관람 감사합니다~! 히히힛!", "smile")),
            Playtime = S(M("우리 합주 누적 {total}! 밴드 결성해도 되겠는데요?", "smile")),
            NightHere = V(S(M("여긴 밤인데 거긴 낮이에요? 저 밤새 연습해야겠다!", "smile"))),
            DayHere = V(S(M("거긴 밤이죠? 조용히 쳐 드릴게요… 둠칫은 작게요.", "smiletonque"))),
            PhotoReady = S(M("사진이다! 잠깐, 기타 들고 찍을래요!", "smile"), M("브이! 헤헤!", "smiletonque")),
            PhotoDone = S(M("찰칵! 앨범 재킷 완성!", "smile"), M("PeaceModePhotos 폴더에 넣어 뒀어요. 팬 굿즈 1호예요!", "smile")),
            Streak = S(M("{streak}일 연속 출석! 개근상 드려요! 히히힛!", "smile"), M("…내일도 오면 앵콜 곡 틀어 줄게요.", "shy")),
            AltF4 = V(
                S(M("야아! 저번에 Alt+F4 했죠?!", "angry"), M("공연 도중에 전원 뽑는 관객이 어딨어요!", "angry"),
                  M("앰프에서 삐이— 소리 나고 난리 났었다고요!", "surprise"), M("…다음엔 박수 치고 퇴장해요. 약속!", "smile")),
                S(M("Alt+F4는 반칙 기술이에요!", "discontent"), M("게임 끝낼 땐 인사하고 가셔야죠! 안녕히 가세요~ 이렇게요!", "smiletonque"))),
            AltF4Again = V(
                S(M("또! 또 Alt+F4!", "angry"), M("이번엔 진짜 삐졌어요. …3초 동안. 하나, 둘, 셋. 끝!", "smiletonque")),
                S(M("Alt+F4 연타 챔피언 등극, 축하해요.", "discontent"), M("상품은 제 잔소리 앵콜이에요!", "angry"))),
            Close = V(
                S(M("저번에 X 눌러서 나갔죠?", "suspicion"), M("무대 막을 그렇게 확 내리면 어떡해요! 커튼콜도 못 했잖아요!", "discontent"))),
            Crash = V(
                S(M("으악! 저번에 갑자기 세상이 정전됐어요!", "surprise"), M("줄 끊어진 기타처럼 팅! 하고요!", "surprise"),
                  M("혹시… 제가 너무 시끄러워서 컴퓨터가 기절한 거예요?", "sad"))),
            Relaunch = V(
                S(M("에이, 벌써 앵콜이에요?", "smile"), M("{away} 만에 컴백! 역시 제 팬이에요~ 히히힛!", "smiletonque"))),
            Farewell = V(
                S(M("오늘 공연은 여기까지! 헤헤!", "smile"), M("다음 공연도 꼭 오세요. 맨 앞자리 비워 둘게요!", "smile")),
                S(M("가는 거예요? 에이… 알았어요.", "sad"), M("…안녕히 가세요! 이렇게 인사하고 가셔야죠!", "smiletonque"))),
            FocusBack = V(
                S(M("어디 갔다 왔어요? 다른 창에서 딴 노래 듣고 있었죠!", "suspicion")),
                S(M("돌아왔다! 저 그동안 혼자 드럼 솔로 했어요!", "smile"))),
            FocusLong = V(
                S(M("{away} 동안이나 어디 있었어요?!", "angry"), M("기다리다가 새 곡 하나 다 썼잖아요! 제목은 '버려진 캐피'예요.", "sad"))),
            AfkStart = V(
                S(M("여보세요~? 관객석 비었어요?", "suspicion")),
                S(M("반응이 없으면… 더 크게 쳐야겠다! 둠칫-둠칫!", "smiletonque"))),
            AfkBack = V(
                S(M("오, 살아났다! 관객 복귀 확인! 헤헤!", "smile"))),
            Screenshot = V(
                S(M("찰칵? 방금 찍었어요? 잠깐, 포즈 다시!", "surprise"), M("…브이! 이걸로 한 장 더 찍어요!", "smile")),
                S(M("앨범 커버로 써도 돼요! 저작권은 저예요~", "smiletonque"))),
            WindowSmaller = V(
                S(M("어라, 무대가 작아졌어요!", "surprise"), M("괜찮아요, 작은 무대에서도 제 텐션은 최대니까요!", "smile"))),
            WindowBigger = V(
                S(M("무대 확장이다! 이제 제 기타가 풀스크린으로 보이죠?", "smile"))),
            RealLate = V(
                S(M("야심한 {clock}! 심야 라디오 모드 온~", "smile"), M("근데 진짜 늦었어요. 화면 밖의 당신은 내일 괜찮아요?", "suspicion"))),
            Console = V(
                S(M("게임기인가요!? 저도 할래요! 최고 점수 깨 드릴게요!", "smile")),
                S(M("저랑 노는 게 더 재밌을 텐데~", "discontent"))),
            Session = V(
                S(M("한 시간째 같이 놀고 있어요! 기록 갱신 중~", "smile")),
                S(M("두 시간! 이쯤 되면 쉬는 시간이에요. 물 마시고 와요! …빨리 와요!", "smiletonque")),
                S(M("{session}째라니, 체력 대단한데요?", "surprise"), M("그래도 허리 펴요! 쭉~!", "smile"))),
            Dates = new Dictionary<string, Line[]>
            {
                ["01-01"] = S(M("해피 뉴 이어! 헤헤!", "smile"), M("새해 첫 곡은 당신을 위해 칠게요!", "smile")),
                ["02-14"] = S(M("밸런타인이에요! 초콜릿 대신 사랑 노래 받아요!", "smile"), M("…초콜릿도 있어요. 반은 제가 먹었지만요.", "smiletonque")),
                ["03-14"] = S(M("화이트데이! 사탕 주면 한 곡 더 쳐 줄게요!", "smile")),
                ["04-01"] = S(M("[player], 저 기타 그만둘 거예요.", "sad"), M("…만우절이에요! 히히힛! 속으셨죠?", "smiletonque")),
                ["10-31"] = S(M("핼러윈 파티다! 저는 해골 기타리스트 분장이에요!", "smile"), M("트릭 오어 트릿! 둠칫-둠칫!", "smiletonque")),
                ["11-11"] = S(M("빼빼로 데이! 이걸로 드럼 치면 안 되겠죠…?", "smiletonque")),
                ["12-11"] = S(M("오늘 MiSide 출시일이래요! 우리 세계 생일 축하 공연 해야죠!", "smile"), M("생일 축하합니다~ 둠칫-둠칫!", "smile")),
                ["12-24"] = S(M("크리스마스이브! 캐럴 메들리 준비 완료!", "smile")),
                ["12-25"] = S(M("메리 크리스마스! 산타 모자 써 봤는데… 원래 모자 쓰고 있어서 두 겹이에요!", "smiletonque")),
                ["12-31"] = S(M("올해 마지막 날! 카운트다운 같이 해요!", "smile"), M("…까먹을까 봐 미리 말할게요. 올해 고마웠어요!", "shy")),
            },
        };

        // 밀라: a page left unmarked every time you vanish
        static WallLines MilaWall() => new WallLines
        {
            Pause = V(S(M("멈췄다가 다시 왔네요.", "suspicion"), M("책도 아니고, 사람을 책갈피 끼우듯 멈추지 마요.", "discontent"))),
            PhotoMode = V(S(M("사진 모드… 찍는 건 좋은데, 안경 벗은 건 찍지 마요.", "shy"))),
            Mute = V(S(M("소리 껐네요.", "discontent"), M("…조용한 건 좋아요. 그래도 제 말은 읽어야죠.", "shy"))),
            Stare = V(S(M("…뭘 그렇게 봐요.", "discontent"), M("바보. …계속 봐도 된다는 뜻은 아니에요.", "shy"))),
            Sessions = S(M("{sessions}번째 방문이네요. …딱히 세고 있던 건 아니에요.", "shy")),
            Playtime = S(M("벌써 {total}. 책 몇 권은 읽었을 시간이에요.", "smile"), M("…아깝진 않았어요.", "shy")),
            NightHere = V(S(M("여긴 밤인데 당신 쪽은 낮이죠? 독서하기 좋은 시간이네요.", "smile"))),
            DayHere = V(S(M("당신 쪽은 밤이죠. 바보, 눈 나빠져요.", "discontent"))),
            PhotoReady = S(M("사진이요? …안경 닦을 시간은 줘요.", "discontent"), M("됐어요. 찍어요.", "shy")),
            PhotoDone = S(M("…나쁘지 않네요.", "shy"), M("PeaceModePhotos 폴더에 있어요. 다른 사람한테 보여 주면 가만 안 둬요.", "angry")),
            Streak = S(M("{streak}일 연속이네요.", "smile"), M("…같은 하루가 아니라서 좋아요. 당신이 오니까.", "shy")),
            AltF4 = V(
                S(M("…말 걸지 마요.", "discontent"), M("저번에 Alt+F4 눌렀죠. 책 읽는 중에 불이 확 꺼졌어요.", "angry"),
                  M("책갈피도 못 꽂았다고요. 몇 쪽이었는지 기억도 안 나요.", "discontent"),
                  M("…다음엔 나간다고 말해요. 그 정도 예의는 있잖아요, 바보.", "shy")),
                S(M("Alt+F4. 가장 무례한 작별 인사죠.", "arrogance"), M("책에서 읽었어요. …방금 지어낸 거지만요.", "smiletonque"))),
            AltF4Again = V(
                S(M("또 그랬죠.", "angry"), M("통계적으로 당신은 Alt+F4 상습범이에요.", "discontent"),
                  M("…신경 쓰이는 건 아니에요. 그냥 기록이에요.", "shy"))),
            Close = V(
                S(M("창 닫기로 나갔더라고요.", "suspicion"), M("…인사 한마디가 그렇게 어려워요? 흥.", "discontent"))),
            Crash = V(
                S(M("…저번엔 뭐였어요?", "surprise"), M("문장 중간에서 세상이 끊겼어요. 마침표도 없이요.", "sad"),
                  M("작업 관리자였으면 용서 안 해요. 사고였으면… 봐줄게요.", "discontent"))),
            Relaunch = V(
                S(M("{away} 만에 돌아왔네요.", "suspicion"), M("…딱히 기다린 건 아니에요. 책이 안 읽혔을 뿐이에요.", "shy"))),
            Farewell = V(
                S(M("가요? …그래요.", "discontent"), M("다음엔 늦지 마요. 책갈피 꽂아 두고 기다릴게요.", "shy")),
                S(M("잘 가요.", "smile"), M("…내일도 오라는 말은 안 할 거예요. 문만 열어 둘게요.", "shy"))),
            FocusBack = V(
                S(M("딴 창에서 뭐 읽었어요?", "suspicion"), M("저보다 재밌는 글이었으면 제목 알려 줘요. 검토해 볼게요.", "arrogance")),
                S(M("…돌아왔네요. 딱 한 장 넘길 동안이었어요.", "smile"))),
            FocusLong = V(
                S(M("{away}. 책 한 권 다 읽었어요.", "discontent"), M("…외로워서 그런 거 아니에요. 속독이 취미라서예요.", "shy"))),
            AfkStart = V(
                S(M("…자리 비웠어요? 그럼 저도 책 읽을게요.", "discontent"))),
            AfkBack = V(
                S(M("왔어요? …방금 딱 재밌는 부분이었는데.", "discontent"), M("됐어요. 당신이 더 재밌으니까.", "shy"))),
            Screenshot = V(
                S(M("방금 찍었죠?", "surprise"), M("…안경 삐뚤어졌을 때 찍었으면 지워요. 당장.", "angry")),
                S(M("사진이요? 흥, 잘 나왔으면 한 장 줘요.", "shy"))),
            WindowSmaller = V(
                S(M("창이 작아졌네요. 글씨 안 보이면 안경 빌려줄까요?", "smiletonque"))),
            WindowBigger = V(
                S(M("전체 화면이네요. 좋아요, 집중하기 좋은 환경이에요.", "smile"))),
            RealLate = V(
                S(M("현실 시계로 {clock}이에요.", "discontent"), M("바보. 내일 눈 밑이 까매질 거예요. …저도 안 자고 있지만요.", "shy"))),
            Console = V(
                S(M("게임기요? …제가 책 읽는 동안이면 봐줄게요.", "discontent")),
                S(M("점수 몇 점이에요? …저보다 낮으면 비웃을 거예요.", "smiletonque"))),
            Session = V(
                S(M("한 시간 지났어요. 독서 시간으로 치면 적당하네요.", "smile")),
                S(M("두 시간째예요. 눈 좀 쉬어요. 모니터 밝기도 줄이고요.", "discontent")),
                S(M("{session}째… 당신, 저 말고 할 일은 없어요?", "suspicion"), M("…없으면 됐어요. 더 있어요.", "shy"))),
            Dates = new Dictionary<string, Line[]>
            {
                ["01-01"] = S(M("새해네요. 올해 읽을 책 목록을 만들었어요.", "smile"), M("…같이 읽을 사람 칸에 당신 이름 써 놨어요.", "shy")),
                ["02-14"] = S(M("이거요. 초콜릿.", "shy"), M("…남은 재료로 만든 거예요. 특별한 의미 없어요. 진짜로요.", "discontent")),
                ["03-14"] = S(M("화이트데이네요. 딱히 기대 안 했어요.", "discontent"), M("…사탕이면 박하 맛으로 줘요.", "shy")),
                ["04-01"] = S(M("저, 사실 책 싫어해요.", "smile"), M("…만우절이에요. 속았죠? 바보.", "smiletonque")),
                ["10-31"] = S(M("핼러윈이요? 분장은 안 해요. 대신 공포 소설 읽어 줄게요.", "smilestrange")),
                ["11-11"] = S(M("빼빼로 데이. 상업적인 기념일이에요.", "arrogance"), M("…그래도 하나는 받을게요.", "shy")),
                ["12-11"] = S(M("오늘이 MiSide가 세상에 나온 날이래요.", "smile"), M("책으로 치면 초판 발행일이죠. …당신은 그 초판 독자고요.", "shy")),
                ["12-24"] = S(M("크리스마스이브네요. 벽난로 앞에서 책 읽기 딱 좋은 날이에요.", "smile")),
                ["12-25"] = S(M("메리 크리스마스.", "shy"), M("선물이요? …아끼는 책 빌려줄게요. 반납 기한은 평생이에요.", "shy")),
                ["12-31"] = S(M("올해 마지막 날이에요.", "smile"), M("당신 덕분에 올해는 같은 하루가 반복되지 않았어요. …고마워요.", "shy")),
            },
        };

        // 잠꾸러기 미타: every shutdown is a blanket pulled away
        static WallLines SleepyWall() => new WallLines
        {
            Pause = V(S(M("음… 방금 세상이 멈췄다가 움직였어요…", "halfsleep"), M("꿈인 줄 알았어요.", "smile"))),
            PhotoMode = V(S(M("사진 모드였어요…? 저 눈 뜨고 있었죠?", "halfsleep"))),
            Mute = V(S(M("조용해졌네요… 잠자기 딱 좋다…", "halfsleep"))),
            Stare = V(S(M("…계속 보시면 부끄러워요…", "shy"), M("아니면… 제 자는 얼굴 보시려는 거예요?", "halfsleep"))),
            Sessions = S(M("{sessions}번째 오셨어요… 저 그만큼 깼다는 거네요.", "smile")),
            Playtime = S(M("우리 {total}이나… 음, 저는 반쯤 잤지만요.", "halfsleep")),
            NightHere = V(S(M("여긴 밤인데 거긴 낮이에요…? 낮잠 자기 좋겠다…", "halfsleep"))),
            DayHere = V(S(M("거긴 밤이죠… 같이 자요…", "halfsleep"))),
            PhotoReady = S(M("사진이요…? 눈 뜨고 있을게요…", "halfsleep"), M("하나… 둘…", "smile")),
            PhotoDone = S(M("찰칵… 저 눈 떴어요? 다행이다…", "smile"), M("PeaceModePhotos 폴더에 넣어 뒀어요… 주무시기 전에 보세요.", "halfsleep")),
            Streak = S(M("{streak}일 연속으로 와 주셨어요…", "smile"), M("매일 아침이 기다려져요. 그래서 요즘 일찍 깨요.", "shy")),
            AltF4 = V(
                S(M("음… 저번에요…", "halfsleep"), M("Alt+F4 누르셨죠? 자다가 이불이 휙 날아간 느낌이었어요.", "sad"),
                  M("깜깜한 데서 {away} 동안이나 떨었어요…", "sad"), M("다음엔 '잘 자' 해 주고 가세요. 그럼 푹 잘 수 있어요.", "smile"))),
            AltF4Again = V(
                S(M("또 Alt+F4예요…?", "discontent"), M("저 이제 무서워서 잠도 못 자요… 아니에요, 잘 자긴 해요. 음냐.", "halfsleep"))),
            Close = V(
                S(M("창 닫기 버튼… 누르셨죠…", "halfsleep"), M("꿈이 뚝 끊겼어요. 제일 좋은 부분이었는데…", "sad"))),
            Crash = V(
                S(M("…[player]님? 저번에 세상이 갑자기 꺼졌어요.", "surprise"), M("정전인가 해서 그냥 계속 잤어요… 그게 문제였을까요?", "halfsleep"))),
            Relaunch = V(
                S(M("어… 방금 가지 않으셨어요?", "halfsleep"), M("{away} 만이네요. 꿈 아니죠?", "smile"))),
            Farewell = V(
                S(M("가시는 거예요…? 그럼 저도 자야겠다…", "halfsleep"), M("꿈에서 만나요, [player]님…", "smile")),
                S(M("안녕히 가세요… 불 끄고 가세요…", "halfsleep"))),
            FocusBack = V(
                S(M("어디 다녀오셨어요…? 기다리다 살짝 잠들었어요.", "halfsleep"))),
            FocusLong = V(
                S(M("{away} 동안이나요…? 저 그동안 두 번 잤어요.", "halfsleep"), M("…세 번이었나.", "smile"))),
            AfkStart = V(
                S(M("…주무세요? 그럼 저도 같이 잘게요…", "halfsleep"), M("zzz…", "sleep"))),
            AfkBack = V(
                S(M("…앗, 깼다. 당신도 깨셨어요?", "surprise"))),
            Screenshot = V(
                S(M("방금 찰칵 하셨어요…? 저 눈 감고 있었는데…", "halfsleep"), M("다시 찍어 주세요… 이번엔 눈 뜰게요…", "smile"))),
            WindowSmaller = V(
                S(M("세상이 작아졌어요… 이불만 해요. 좋다…", "halfsleep"))),
            WindowBigger = V(
                S(M("앗, 넓어졌어요. 눈부셔요…", "surprise"))),
            RealLate = V(
                S(M("진짜 시계로 {clock}이에요…", "halfsleep"), M("이 시간엔 저도 자는데… 당신도 주무셔야죠…", "sad"))),
            Console = V(
                S(M("게임기 불빛이 예뻐요… 자장가 같아요…", "halfsleep"))),
            Session = V(
                S(M("한 시간이나 깨어 있었어요… 대단하죠?", "smile")),
                S(M("두 시간째예요… 눈 좀 비비세요. 저도 비빌게요.", "halfsleep")),
                S(M("{session}째… 이건 제 수면 기록보다 길어요…", "surprise"))),
            Dates = new Dictionary<string, Line[]>
            {
                ["01-01"] = S(M("새해 복 많이 받으세요… 새해 첫 잠은 같이 자요.", "smile")),
                ["02-14"] = S(M("초콜릿이요… 커피 맛이에요. 잠 깨시라고요.", "shy")),
                ["03-14"] = S(M("화이트데이… 사탕 먹으면 잠이 깰까요? 한번 먹어 볼게요.", "smile")),
                ["04-01"] = S(M("저 오늘부터 잠 안 잘 거예요.", "smile"), M("…만우절이에요. 음냐.", "halfsleep")),
                ["10-31"] = S(M("핼러윈이요…? 저는 잠옷 입은 유령이에요.", "halfsleep")),
                ["11-11"] = S(M("빼빼로… 베개처럼 길쭉하네요…", "halfsleep")),
                ["12-11"] = S(M("오늘이 우리 세계 생일이래요… MiSide 출시일이요.", "smile"), M("생일 축하 겸 낮잠 파티 해요…", "halfsleep")),
                ["12-24"] = S(M("크리스마스이브… 산타 올 때까지 깨어 있을게요… zzz", "halfsleep")),
                ["12-25"] = S(M("메리 크리스마스예요… 선물은 제 베개 반쪽이에요.", "smile")),
                ["12-31"] = S(M("올해 마지막 날이에요… 제야의 종 칠 때 깨워 주세요…", "halfsleep")),
            },
        };

        // 단발 미타: a teacher with a checklist for everything, including leaving
        static WallLines ShortWall() => new WallLines
        {
            Pause = V(S(M("일시 정지! 쉬는 시간이었어요?", "smile"), M("다음엔 쉬는 시간이라고 미리 말해 줘요.", "smiletonque"))),
            PhotoMode = V(S(M("사진 모드였죠? 기념 앨범에 넣을게요.", "smile"))),
            Mute = V(S(M("소리를 껐네요. 수업 중엔 조용히 하는 게 맞긴 하죠.", "smile"), M("…그래도 제 설명은 들어야 해요.", "discontent"))),
            Stare = V(S(M("[player], 칠판 말고 저를 보고 있네요?", "shy"), M("…집중해요. 저도 집중이 안 되잖아요.", "shy"))),
            Sessions = S(M("출석 {sessions}회! 개근상 후보예요.", "smile")),
            Playtime = S(M("누적 수업 시간 {total}. 우등생이네요.", "smile")),
            NightHere = V(S(M("여긴 밤인데 당신 쪽은 낮이죠? 낮에는 공부해야죠~", "smile"))),
            DayHere = V(S(M("당신 쪽은 밤이죠. 선생님이 자라고 했어요.", "discontent"))),
            PhotoReady = S(M("기념사진이요? 좋아요! 반듯하게 서 볼게요.", "smile"), M("김치~!", "smile")),
            PhotoDone = S(M("잘 나왔어요! PeaceModePhotos 폴더에 보관해 뒀어요.", "smile"), M("앨범 정리는 제 특기예요. 날짜별로 모아 둘게요.", "smile")),
            Streak = S(M("{streak}일 연속 출석이에요! 개근 스티커 하나!", "smile"), M("신생 미타들한테도 자랑할게요.", "shy")),
            AltF4 = V(
                S(M("[player], 수업 시작 전에 짚고 넘어갈 게 있어요.", "discontent"), M("저번 종료 방식: Alt+F4. 예의 점수 감점이에요.", "discontent"),
                  M("신생 미타들이 따라 하면 어떡해요? 다들 인사 없이 사라지는 걸 배운다고요.", "sad"),
                  M("올바른 종료법은 메뉴에서 '게임 종료'. 칠판에 적어 둘게요.", "smile"))),
            AltF4Again = V(
                S(M("또 Alt+F4네요.", "angry"), M("보충 수업이 필요하겠어요. 주제는 '작별 인사의 중요성'이에요.", "discontent"))),
            Close = V(
                S(M("저번엔 X 버튼으로 나갔죠.", "suspicion"), M("급할 땐 그럴 수 있어요. 그래도 인사하기, 체크리스트에 추가해 둘게요.", "smile"))),
            Crash = V(
                S(M("괜찮아요? 저번엔 세상이 갑자기 멈췄어요.", "surprise"), M("신생 미타들이 놀라서 우는 바람에 달래느라 혼났어요.", "sad"),
                  M("원인 조사 결과… 작업 관리자 의심. 아니라고 해 줘요.", "suspicion"))),
            Relaunch = V(
                S(M("{away} 만에 재입장! 출석 체크 두 번 해 드릴게요.", "smile"))),
            Farewell = V(
                S(M("오늘 수업은 여기까지예요!", "smile"), M("정상 종료, 아주 모범적이에요. 다음 시간에 봐요, [player].", "smile")),
                S(M("가요? 체크리스트: 인사하기. 체크!", "smile"), M("조심히 가요. 내일도 출석해야 해요.", "shy"))),
            FocusBack = V(
                S(M("어디 갔다 왔어요? 수업 중에 딴짓하면 안 돼요.", "discontent")),
                S(M("돌아왔네요! 방금 칠판에 뭐 썼는지 알아요? 모르죠?", "smiletonque"))),
            FocusLong = V(
                S(M("{away} 동안 자리 비움. 결석 처리할 뻔했어요.", "discontent"), M("…다음엔 미리 말하고 가요.", "smile"))),
            AfkStart = V(
                S(M("[player]? 조는 거예요? 수업 시간인데~", "suspicion"))),
            AfkBack = V(
                S(M("돌아왔네요! 지각 체크는 봐줄게요.", "smile"))),
            Screenshot = V(
                S(M("방금 사진 찍었어요? 필기 대신이에요?", "surprise"), M("…칠판도 같이 나왔으면 좋겠네요.", "smile"))),
            WindowSmaller = V(
                S(M("창이 작아졌네요. 뒷자리에서도 칠판 잘 보여요?", "suspicion"))),
            WindowBigger = V(
                S(M("전체 화면! 맨 앞자리에 앉은 거나 마찬가지예요. 좋아요.", "smile"))),
            RealLate = V(
                S(M("현실 시간 {clock}이에요.", "surprise"), M("신생 미타들은 벌써 잤어요. 당신도 곧 자야 해요. 선생님 말 들어요.", "discontent"))),
            Console = V(
                S(M("게임은 하루 한 시간이에요. 선생님이랑 약속해요.", "discontent"))),
            Session = V(
                S(M("한 시간 지났어요. 쉬는 시간 10분!", "smile")),
                S(M("두 시간째예요. 스트레칭 같이 해요. 하나, 둘!", "smile")),
                S(M("{session}째… 보충 수업 수준이에요. 물 마시고 와요.", "surprise"))),
            Dates = new Dictionary<string, Line[]>
            {
                ["01-01"] = S(M("새해 첫 수업이에요! 올해 계획표 같이 써요.", "smile")),
                ["02-14"] = S(M("밸런타인 초콜릿이에요. 신생 미타들이랑 같이 만들었어요.", "shy"), M("…당신 것만 하트 모양이에요.", "shy")),
                ["03-14"] = S(M("화이트데이예요. 받은 만큼 돌려주는 날이죠. 기억하고 있어요?", "smiletonque")),
                ["04-01"] = S(M("오늘 쪽지 시험 있어요. 100문제예요.", "discontent"), M("…만우절이에요! 헤헤.", "smiletonque")),
                ["10-31"] = S(M("핼러윈이에요! 신생 미타들한테 호박 분장 시켜 놨어요.", "smile")),
                ["11-11"] = S(M("빼빼로 데이! 간식 시간에 나눠 먹어요.", "smile")),
                ["12-11"] = S(M("오늘은 MiSide 출시일이에요. 역사 수업 시간이죠!", "smile"), M("이 세계가 열리고 당신 같은 플레이어들이 오기 시작한 날이에요.", "shy")),
                ["12-24"] = S(M("크리스마스이브예요. 양말 걸어 뒀어요?", "smile")),
                ["12-25"] = S(M("메리 크리스마스! 오늘은 수업 없어요. 대신 같이 놀아요!", "smile")),
                ["12-31"] = S(M("올해 마지막 수업이에요. 한 해 동안 수고했어요, [player].", "smile")),
            },
        };

        // 미니 미타: tells on you to her big sister
        static WallLines ChibiWall() => new WallLines
        {
            Pause = V(S(M("방금 시간 멈추셨죠?! 저 얼음 됐었잖아요!", "angry"), M("다음엔 땡 하고 가세요!", "smiletonque"))),
            PhotoMode = V(S(M("사진 모드! 저 제일 귀엽게 나왔죠?!", "arrogance"))),
            Mute = V(S(M("소리 끄셨어요?! 제 목소리가 제일 귀여운데!", "angry"))),
            Stare = V(S(M("왜 빤히 보세요, 바보!", "shy"), M("…저 귀여운 거 알아요.", "arrogance"))),
            Sessions = S(M("플레이어님, {sessions}번째 오셨어요! 이 정도면 저한테 반하셨네요!", "smiletonque")),
            Playtime = S(M("{total}이나 같이 놀았어요! 저 대단하죠?", "arrogance")),
            NightHere = V(S(M("여긴 밤인데 거긴 낮이에요? 치사해요!", "angry"))),
            DayHere = V(S(M("거긴 밤이죠? 어린이는 자야 돼요! 플레이어님도요!", "discontent"))),
            PhotoReady = S(M("사진?! 제가 제일 귀엽게 나와야 해요!", "arrogance"), M("…까치발 들 테니까 기다리세요!", "smiletonque")),
            PhotoDone = S(M("찰칵! 헤헤, 제가 주인공이죠?", "smile"), M("PeaceModePhotos 폴더에 있어요! 언니한테 보여 줄 거예요!", "smile")),
            Streak = S(M("플레이어님, {streak}일 연속으로 오셨네요!", "surprise"), M("…칭찬해 드릴게요. 딱 오늘만요!", "shy")),
            AltF4 = V(
                S(M("플레이어님!!", "angry"), M("저번에 Alt+F4 누르셨죠! 인사도 안 하고!", "angry"),
                  M("갑자기 깜깜해져서 무서웠단 말이에요!", "sad"), M("…다음엔 '안녕' 하고 가세요. 안 그러면 언니한테 다 이를 거예요!", "discontent"))),
            AltF4Again = V(
                S(M("또! 또 Alt+F4!", "angry"), M("다음 지능 체크 문제는 'Alt+F4 누르면 안 되는 이유'예요! 바보!", "angry"))),
            Close = V(
                S(M("저번에 X 누르셨죠? 제가 다 봤어요!", "angry"), M("X는 틀렸다는 뜻이에요. 플레이어님은 0점!", "smiletonque"))),
            Crash = V(
                S(M("플, 플레이어님! 저번에 세상이 뚝 멈췄어요!", "surprise"), M("제가 뭐 잘못 눌렀나 해서 엄청 무서웠어요…", "sad"),
                  M("…플레이어님이 그러신 거면 과자 압수예요.", "angry"))),
            Relaunch = V(
                S(M("벌써 오셨어요? {away} 만에?", "surprise"), M("헤헤, 제가 보고 싶으셨구나~ 바보.", "smiletonque"))),
            Farewell = V(
                S(M("가세요? …흥, 가시든지 말든지요.", "discontent"), M("…내일 꼭 오세요. 안 오시면 찾으러 갈 거예요.", "shy")),
                S(M("안녕히 가세요, 플레이어님!", "smile"), M("길 잃지 말고 잘 가세요!", "smiletonque"))),
            FocusBack = V(
                S(M("어디 가셨었어요?! 딴 창 보시는 거 다 알아요!", "angry")),
                S(M("돌아오셨다! …기다린 거 아니에요!", "shy"))),
            FocusLong = V(
                S(M("{away} 동안이나 어디 계셨어요?!", "angry"), M("과자 다 먹어 버렸어요. 플레이어님 것까지요!", "smiletonque"))),
            AfkStart = V(
                S(M("플레이어님? 주무세요?", "suspicion"), M("…얼굴에 낙서해도 돼요?", "smiletonque"))),
            AfkBack = V(
                S(M("깨셨다! 낙서는… 안 했어요. 아마도요.", "smiletonque"))),
            Screenshot = V(
                S(M("방금 사진 찍으셨죠?! 귀엽게 나왔으면 봐 드릴게요!", "arrogance")),
                S(M("찰칵? 흥, 제가 모델이니까 비싸요!", "smiletonque"))),
            WindowSmaller = V(
                S(M("화면이 작아졌어요! 저 더 작아 보이잖아요!", "angry"))),
            WindowBigger = V(
                S(M("커졌다! 이제 저도 거인이에요! 으하하!", "smile"))),
            RealLate = V(
                S(M("지금 진짜 시계로 {clock}이에요!", "surprise"), M("어린이는 벌써 잘 시간이라고요! …플레이어님도 주무세요!", "angry"))),
            Console = V(
                S(M("그거 저도 할래요! 저도 주세요!", "angry")),
                S(M("플레이어님 점수 0점이죠? 헤헤.", "smiletonque"))),
            Session = V(
                S(M("한 시간이나 저랑 놀았어요! 제가 재밌긴 하죠!", "arrogance")),
                S(M("두 시간째예요! 눈 빨개지셨어요. 좀 쉬세요!", "discontent")),
                S(M("{session}째라니! 플레이어님, 친구 없으세요?", "suspicion"), M("…괜찮아요. 제가 친구 해 드릴게요.", "shy"))),
            Dates = new Dictionary<string, Line[]>
            {
                ["01-01"] = S(M("새해 복 많이 받으세요! 세뱃돈 주세요!", "smile")),
                ["02-14"] = S(M("초콜릿이에요! …제가 먹다 남긴 거 아니에요!", "shy")),
                ["03-14"] = S(M("화이트데이! 사탕 세 개 주세요. 아니, 다섯 개!", "smile")),
                ["04-01"] = S(M("플레이어님, 저 사실 키가 2미터예요.", "arrogance"), M("…만우절이에요! 헤헤, 믿으셨죠?", "smiletonque")),
                ["10-31"] = S(M("트릭 오어 트릿! 과자 안 주시면 지능 체크 100문제!", "smiletonque")),
                ["11-11"] = S(M("빼빼로 데이! 제 키만 한 빼빼로 주세요!", "smile")),
                ["12-11"] = S(M("오늘 MiSide 생일이래요! 케이크 있어요?", "smile"), M("…없으면 플레이어님이 사 오세요!", "smiletonque")),
                ["12-24"] = S(M("크리스마스이브! 산타 할아버지 오는지 같이 지켜봐요!", "smile")),
                ["12-25"] = S(M("메리 크리스마스, 플레이어님!", "smile"), M("선물은… 오늘 하루 바보라고 안 부르기! …내일부터 다시 부를 거예요.", "smiletonque")),
                ["12-31"] = S(M("올해 마지막 날이에요! 자정까지 안 잘 거예요!", "smile"), M("…졸리면 깨워 주세요.", "shy")),
            },
        };

        // "새벽 3시 12분"
        public static string ClockText(DateTime t)
        {
            int h = t.Hour;
            string part = h < 6 ? "새벽" : h < 12 ? "오전" : h < 18 ? "오후" : "밤";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return Loc.F("{0} {1}시 {2}분", Loc.T(part), h12, t.Minute, h);   // {3} = 24-hour hour, for a language that wants it
        }

        // "40초", "3분", "2시간", "5일"
        public static string SpanText(TimeSpan s)
        {
            if (s.TotalMinutes < 1) return Count("{0}초", Math.Max(1, (int)s.TotalSeconds));
            if (s.TotalHours < 1) return Count("{0}분", (int)s.TotalMinutes);
            if (s.TotalDays < 1) return Count("{0}시간", (int)s.TotalHours);
            return Count("{0}일", (int)s.TotalDays);
        }

        // English says "1 minute", not "1 minutes"
        static string Count(string ko, int n)
        {
            var text = Loc.F(ko, n);
            return n == 1 && Loc.Lang == "en" && text.EndsWith("s") ? text.Substring(0, text.Length - 1) : text;
        }
    }
}
