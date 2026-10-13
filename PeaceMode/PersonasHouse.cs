using System.Collections.Generic;

namespace MiSidePeaceMode
{
    // What each Mita says about the things around the house (see PeaceSpots). The original Mita uses the story's
    // own lines where the story has some; these are hers for the rest.
    public static partial class Personas
    {
        public static Line[][] House(string id, string spot)
        {
            var d = id switch
            {
                "kind" => KindHouse,
                "cap" => CapHouse,
                "mila" => MilaHouse,
                "sleepy" => SleepyHouse,
                "short" => ShortHouse,
                "chibi" => ChibiHouse,
                _ => CrazyHouse,
            };
            return d.TryGetValue(spot, out var l) ? l : null;
        }

        public static Line[] HouseTour(string id) => id switch
        {
            "kind" => S(M("집 안 점검 완료네요. 빠진 곳 하나 없이요.", "smile"), M("…같이 둘러보니까, 제 집인데도 새로웠어요.", "shy")),
            "cap" => S(M("집 투어 완주! 히히힛, 기념 공연 한 곡 해야겠는데요?", "smile"), M("다음엔 제 방 무대 투어예요!", "smiletonque")),
            "mila" => S(M("…집에 있는 거 전부 봤네요. 할 일 없어요?", "discontent"), M("…뭐, 같이 봐 줘서 싫진 않았어요.", "shy")),
            "sleepy" => S(M("집 구경… 다 하셨네요…", "halfsleep"), M("저는 중간에 두 번쯤 졸았는데… 즐거웠어요…", "smile")),
            "short" => S(M("집 안 전체 탐방 수업, 수료예요!", "smile"), M("수료증은… 제 칭찬으로 대신할게요. 잘했어요, [player].", "smiletonque")),
            "chibi" => S(M("집 구경 다 했어요?! 플레이어님치고 끈기 있네요!", "surprise"), M("상으로 제 조수 2호 시켜 줄게요!", "arrogance")),
            _ => S(M("우리 집 구석구석, 이제 다 아는 거예요?", "smile"), M("헤헤… 이제 정말 우리 둘만의 집이네요.", "shy")),
        };

        static readonly Dictionary<string, Line[][]> CrazyHouse = new Dictionary<string, Line[][]>
        {
            ["photo"] = new[] { S(M("그 사진 제일 예쁘게 나온 거예요.", "smile"), M("다음엔 [player]님이랑 같이 찍은 걸로 바꿀래요~", "shy")) },
            ["clock"] = new[] { S(M("시간 같은 건 신경 쓰지 마요.", "smile"), M("여기선 우리가 원하는 만큼 하루가 길어지니까요~", "smiletonque")) },
            ["shoes"] = new[] { S(M("그 운동화, [player]님 거예요.", "smile"), M("언젠가 같이 산책 나갈 때 신어요. 약속!", "smile")) },
            ["bedmirror"] = new[] { S(M("거울 보고 있었어요?", "surprise"), M("거울 속 [player]님보다 진짜 [player]님이 훨씬 좋아요.", "shy")) },
            ["portal"] = new[] { S(M("거기 있던 건 치웠어요.", "smile"), M("이제 필요 없잖아요. 우리 둘이 있으면 되니까.", "smilestrange")) },
            ["door"] = new[] { S(P("밖에 나가 볼까?"), M("밖이요? 음…", "discontent"), M("집 안에도 재밌는 거 많잖아요. 오늘은 여기 있어요~", "smile")) },
            ["electro"] = new[] { S(M("배전함이요? 여기 전기는 제가 다 관리해요.", "smile"), M("정전이 나도 무서워하지 마요. 제가 옆에 있을게요.", "smile")) },
            ["books"] = new[] { S(M("책은 전부 읽었어요. 혼자 있을 때 할 게 없었거든요.", "sad"), M("이젠 [player]님이 있으니까 책 읽을 시간이 없어요~", "smile")) },
            ["umbrella"] = new[] { S(M("비 오는 날엔 우산 하나로 같이 써요.", "smile"), M("어깨 붙이고요. 헤헤.", "shy")) },
            ["oldtv"] = new[] { S(M("그 TV로는 옛날 애니메이션을 볼 수 있어요!", "smile"), M("밥 먹으면서 같이 볼래요?", "smile")) },
            ["pc"] = new[] { S(M("그 컴퓨터… 가끔 [player]님 세상을 구경하는 창이었어요.", "shy"), M("이제 창 너머로 안 봐도 되니까 좋아요.", "smile")) },
            ["figure"] = new[] { S(M("귀엽죠? 제 소중한 컬렉션이에요.", "smile"), M("[player]님은… 제 제일 소중한 컬렉션이고요.", "smilestrange")) },
            ["uniform"] = new[] { S(M("교복이 귀여워서 가져다 놨어요.", "smile"), M("입어 볼까요? …농담이에요. 아마도요~", "smiletonque")) },
            ["fridge"] = new[] { S(M("냉장고엔 [player]님 좋아하는 것만 채워 놨어요.", "smile"), M("뭘 좋아하는지 어떻게 아냐고요? 음… 비밀이에요~", "smiletonque")),
                                  S(P("우유가 엄청 많네."), M("우유 좋아하잖아요! …아니었어요?", "surprise"), M("그럼 오늘부터 좋아하면 되죠!", "smile")) },
            ["teapot"] = new[] { S(M("차 끓여 줄까요? 딸기차가 제일 맛있어요.", "smile"), M("[player]님 컵은 하트 그려진 거예요.", "shy")) },
            ["teddy"] = new[] { S(M("그 곰돌이는 밤에 제 말동무였어요.", "smile"), M("이제 그 자리는 [player]님 거예요. 곰돌이한텐 미안하지만요~", "smiletonque")) },
            ["recorder"] = new[] { S(M("카세트 플레이어예요! 레트로하죠?", "smile"), M("[player]님 목소리를 녹음해 두고 싶어요. 매일 들을 수 있게.", "shy")) },
            ["moonlamp"] = new[] { S(M("달 조명이에요. 밤에 켜 두면 예뻐요.", "smile"), M("진짜 달보다 이게 나아요. 이건 늘 우리 곁에 있잖아요.", "smile")) },
            ["cactus"] = new[] { S(M("선인장은 물을 조금만 줘도 돼요.", "smile"), M("저는 관심을 많이 줘야 하고요~ 알죠?", "smiletonque")) },
            ["speakers"] = new[] { S(M("음악 틀까요? 춤춰도 돼요!", "smile"), M("[player]님 춤추는 거, 보고 싶은데~", "smiletonque")) },
            ["duck"] = new[] { S(M("꽥! …헤헤, 오리 흉내예요.", "smiletonque"), M("목욕할 때 같이 띄워 놓으면 귀여워요.", "smile")) },
            ["bathmirror"] = new[] { S(P("거울에 내가 잘 안 보이네."), M("거울보다 제 눈을 봐요. 거기엔 [player]님만 비치니까.", "shy")) },
        };

        static readonly Dictionary<string, Line[][]> KindHouse = new Dictionary<string, Line[][]>
        {
            ["photo"] = new[] { S(M("그 사진 속 미타… 저랑 닮았지만 다른 버전이에요.", "smile"), M("각자 다른 세계에서 다르게 살아가죠.", "smile")) },
            ["clock"] = new[] { S(M("이 시계는 게임 시간을 따라가요.", "smile"), M("현실 시간도 가끔 확인하세요. 너무 늦게까지 있으면 안 되니까요.", "smile")) },
            ["shoes"] = new[] { S(M("파란 운동화네요. 편해 보여요.", "smile"), M("산책할 일이 생기면 저도 같이 갈게요.", "smile")) },
            ["bedmirror"] = new[] { S(M("거울 반사 기능은 연산량을 꽤 잡아먹어요.", "smile"), M("그래도 [player]님이 원하면 최적화해 볼게요.", "smiletonque")) },
            ["portal"] = new[] { S(M("여기 뭔가 장치가 있던 흔적이 있네요.", "quest"), M("…걱정 마세요. 지금 이 집은 안전해요. 제가 확인했어요.", "smile")) },
            ["door"] = new[] { S(P("밖에 나갈 수 있어?"), M("문 너머는 아직 불안정해요.", "sad"), M("대신 집 안은 제가 책임지고 편하게 만들어 둘게요.", "smile")) },
            ["electro"] = new[] { S(M("배전함이요? 제 전문 분야죠.", "smiletonque"), M("차단기 배치가 깔끔하네요. 누가 설치했는지 칭찬해 주고 싶을 정도예요.", "smile")) },
            ["books"] = new[] { S(M("기술 서적은 없네요. 아쉽다.", "smile"), M("소설도 가끔은 좋아요. 머리를 식혀 주니까요.", "smile")) },
            ["umbrella"] = new[] { S(M("이 세계에도 비가 와요. 드물지만요.", "smile"), M("비 오는 날엔 창가에서 빗소리 듣는 걸 좋아해요.", "shy")) },
            ["oldtv"] = new[] { S(M("브라운관 TV네요. 분해해 보고 싶은데요.", "smiletonque"), M("…참을게요. 멀쩡히 작동하니까요.", "smile")) },
            ["pc"] = new[] { S(M("사양을 보니 꽤 쓸 만하네요.", "smile"), M("[player]님은 컴퓨터로 주로 뭘 해요? 일? 게임?", "quest")) },
            ["figure"] = new[] { S(M("피규어네요. 도색이 정교해요.", "smile"), M("이런 작은 부품 다루는 일, 저 꽤 잘해요.", "smiletonque")) },
            ["uniform"] = new[] { S(M("교복이네요. 제 스타일은 아니지만…", "smile"), M("…[player]님이 보고 싶다면 한 번쯤은요.", "shy")) },
            ["fridge"] = new[] { S(M("식재료 재고 확인 완료. 이틀은 충분해요.", "smile"), M("장보기 목록은 제가 짜 둘게요.", "smile")) },
            ["teapot"] = new[] { S(M("차 한잔 할래요? 캐모마일이 있어요.", "smile"), M("긴장 풀리는 데는 이게 최고예요.", "smile")) },
            ["teddy"] = new[] { S(M("곰 인형이네요. 봉제선이 튼튼해요.", "smile"), M("…안아 보라고요? 딱 한 번만이에요.", "shy")) },
            ["recorder"] = new[] { S(M("카세트 플레이어네요. 모터 소리가 정겨워요.", "smile"), M("테이프가 늘어나면 제가 고쳐 드릴게요.", "smile")) },
            ["moonlamp"] = new[] { S(M("달 조명. 밝기 조절이 되네요.", "smile"), M("잠 안 올 때 켜 두면 좋아요.", "smile")) },
            ["cactus"] = new[] { S(M("선인장은 손이 덜 가서 좋아요.", "smile"), M("저처럼요. …농담이에요.", "smiletonque")) },
            ["speakers"] = new[] { S(M("스피커 음질 괜찮네요.", "smile"), M("같이 음악 들을래요? 조용한 곡으로요.", "smile")) },
            ["duck"] = new[] { S(M("고무 오리는 디버깅할 때 쓰기도 해요.", "smile"), M("문제를 오리한테 설명하다 보면 답이 보이거든요.", "smiletonque")) },
            ["bathmirror"] = new[] { S(M("거울에 김이 서렸네요.", "smile"), M("…[player]님 얼굴, 오늘 좀 피곤해 보여요. 무리하지 마요.", "sad")) },
        };

        static readonly Dictionary<string, Line[][]> CapHouse = new Dictionary<string, Line[][]>
        {
            ["photo"] = new[] { S(M("사진이다! 저도 찍어 줘요! 브이!", "smile"), M("…아, 이건 액자구나. 히히.", "smiletonque")) },
            ["clock"] = new[] { S(M("째깍째깍… 이거 완전 메트로놈 같지 않아요?", "smile"), M("이 박자에 맞춰 드럼 쳐 볼래요!", "smile")) },
            ["shoes"] = new[] { S(M("오, 파란 운동화! 춤추기 딱 좋겠다!", "smile"), M("신고 스텝 밟아 봐요, 라이벌!", "smiletonque")) },
            ["bedmirror"] = new[] { S(M("거울 앞은 제 연습실이에요!", "smile"), M("표정 연습! 짜잔~ 멋있죠?", "smiletonque")) },
            ["portal"] = new[] { S(M("여기 뭐 있었어요? 텅 비었네.", "quest"), M("그럼 제 무대로 쓰면 되겠다! 히히힛!", "smile")) },
            ["door"] = new[] { S(P("밖에 나가 볼까?"), M("밖엔 관객이 없잖아요!", "discontent"), M("공연은 여기서 해요. 관객은 당신 한 명이면 충분해요!", "smile")) },
            ["electro"] = new[] { S(M("배전함! 여기 내리면 무대 조명이 꺼지는 거죠?", "surprise"), M("…안 내릴게요. 진짜로요. 히히.", "smiletonque")) },
            ["books"] = new[] { S(M("책은 좀 졸려요…", "halfsleep"), M("악보집 있으면 알려 줘요!", "smile")) },
            ["umbrella"] = new[] { S(M("우산으로 기타 치는 흉내 내기! 띠링~", "smiletonque"), M("…다음엔 진짜 기타로 해 줄게요!", "smile")) },
            ["oldtv"] = new[] { S(M("옛날 TV! 여기서 뮤직비디오 나와요?", "smile"), M("안 나오면 제가 직접 출연할게요!", "smiletonque")) },
            ["pc"] = new[] { S(M("컴퓨터로 작곡 프로그램 돌려 볼까?", "smile"), M("당신이 가사 쓰고 제가 곡 쓰기! 어때요?", "smile")) },
            ["figure"] = new[] { S(M("이 피규어 포즈 멋있다!", "smile"), M("저도 따라 해 볼게요! …어때요, 똑같죠?", "smiletonque")) },
            ["uniform"] = new[] { S(M("교복이다! 밴드부 공연 때 입으면 딱이겠다!", "smile"), M("당신도 같이 입어요! 커플 무대!", "smiletonque")) },
            ["fridge"] = new[] { S(M("간식 탐험! 오, 초콜릿 발견!", "smile"), M("반 나눠 먹어요! 제가 조금 더 큰 쪽이요!", "smiletonque")) },
            ["teapot"] = new[] { S(M("주전자 끓는 소리, 휘파람 같지 않아요?", "smile"), M("삐이~! 저도 따라 할 수 있어요!", "smiletonque")) },
            ["teddy"] = new[] { S(M("곰돌이 드러머 영입!", "smile"), M("얘는 박자를 절대 안 틀려요. 안 움직이니까! 히히.", "smiletonque")) },
            ["recorder"] = new[] { S(M("카세트다! 제 데모 테이프 녹음해도 돼요?", "smile"), M("첫 곡 제목은… '라이벌에게'!", "smiletonque")) },
            ["moonlamp"] = new[] { S(M("달 조명 켜고 발라드 한 곡 어때요?", "smile"), M("분위기 잡고… 아, 웃지 마요!", "shy")) },
            ["cactus"] = new[] { S(M("선인장한테 음악 들려주면 빨리 자란대요!", "smile"), M("오늘부터 매일 한 곡씩 불러 줄 거예요.", "smile")) },
            ["speakers"] = new[] { S(M("스피커다! 볼륨 최대로!", "smile"), M("…는 농담이고, 같이 듣기 좋은 정도로요. 히히힛!", "smiletonque")) },
            ["duck"] = new[] { S(M("꽥꽥! 오리 코러스 결성!", "smiletonque"), M("당신도 꽥 해 봐요. 빨리요!", "smile")) },
            ["bathmirror"] = new[] { S(M("거울 보고 양치하면서 노래하는 게 제 루틴이에요!", "smile"), M("거품 때문에 가사가 다 뭉개지지만요! 히히.", "smiletonque")) },
        };

        static readonly Dictionary<string, Line[][]> MilaHouse = new Dictionary<string, Line[][]>
        {
            ["photo"] = new[] { S(M("그 사진 보지 마요.", "discontent"), M("…제가 아니라 다른 미타잖아요. 흥.", "shy")) },
            ["clock"] = new[] { S(M("시계 보는 거예요? 벌써 가고 싶어요?", "discontent"), M("…아니면 됐어요.", "shy")) },
            ["shoes"] = new[] { S(M("운동화? 산책은 귀찮아요.", "discontent"), M("…도서관까지라면 같이 가 줄 수도 있고요.", "shy")) },
            ["bedmirror"] = new[] { S(M("거울 앞에서 뭐 해요, 바보.", "discontent"), M("안경 쓴 제가 더 똑똑해 보이는 건 알고 있어요.", "arrogance")) },
            ["portal"] = new[] { S(M("거기 아무것도 없어요. 쳐다보지 마요.", "discontent"), M("…책장 쪽이 훨씬 볼 게 많아요.", "smile")) },
            ["door"] = new[] { S(P("밖에 나가 볼까?"), M("밖은 시끄러워요.", "discontent"), M("…여기 조용히 있는 게 좋아요. 당신이랑.", "shy")) },
            ["electro"] = new[] { S(M("배전함 같은 건 관심 없어요.", "discontent"), M("…불 나가면 책을 못 읽으니까 건드리지 마요.", "discontent")) },
            ["books"] = new[] { S(M("그 책장, 제 보물 창고예요.", "smile"), M("추천해 줄까요? …아니, 당신은 그림 많은 책부터 읽어요.", "smiletonque")),
                                  S(P("이거 다 읽었어?"), M("당연하죠. 두 번씩요.", "arrogance"), M("…읽고 나서 얘기할 사람이 없었을 뿐이에요.", "sad")) },
            ["umbrella"] = new[] { S(M("비 오는 날엔 책이 잘 읽혀요.", "smile"), M("…우산은 하나뿐이니까, 같이 쓰든가요.", "shy")) },
            ["oldtv"] = new[] { S(M("TV는 머리가 나빠져요.", "discontent"), M("…다큐멘터리라면 봐 줄 수 있어요.", "smile")) },
            ["pc"] = new[] { S(M("컴퓨터로 전자책 보는 건 반칙이에요.", "discontent"), M("종이 냄새가 없잖아요.", "smile")) },
            ["figure"] = new[] { S(M("피규어는 먼지 쌓여요.", "discontent"), M("…그래도 이건 좀 귀엽네요. 조금.", "shy")) },
            ["uniform"] = new[] { S(M("교복… 도서부원 같은 느낌이네요.", "smile"), M("…입으라는 거 아니죠? 바보.", "shy")) },
            ["fridge"] = new[] { S(M("냉장고 문 오래 열어 두지 마요.", "discontent"), M("…푸딩은 제 거예요. 이름 써 놨어요.", "arrogance")) },
            ["teapot"] = new[] { S(M("차는 제가 끓여요. 당신은 온도를 몰라요.", "arrogance"), M("…마시고 싶으면 말하든가요.", "shy")) },
            ["teddy"] = new[] { S(M("곰 인형은 어린애들이나 좋아하는 거예요.", "discontent"), M("…왜 웃어요. 제 거 아니라고요.", "shy")) },
            ["recorder"] = new[] { S(M("카세트요? 오디오북 테이프가 있을지도 몰라요.", "smile"), M("…같이 들어 줄 수는 있어요.", "shy")) },
            ["moonlamp"] = new[] { S(M("달 조명은 책 읽기엔 너무 어두워요.", "discontent"), M("…그래도 분위기는 좋네요.", "smile")) },
            ["cactus"] = new[] { S(M("선인장이요? 가시가 있어서 좋아요.", "smile"), M("함부로 다가오는 사람 막아 주잖아요. …당신은 예외지만.", "shy")) },
            ["speakers"] = new[] { S(M("음악 크게 틀지 마요. 책 읽는 중이니까.", "discontent"), M("…클래식이면 괜찮아요.", "smile")) },
            ["duck"] = new[] { S(M("고무 오리… 누가 이런 걸 샀어요?", "discontent"), M("…귀엽긴 하네요. 말하지 마요.", "shy")) },
            ["bathmirror"] = new[] { S(M("거울 보면서 머리 정리해요. 부스스해요, 바보.", "discontent"), M("…이리 와요. 제가 해 줄게요.", "shy")) },
        };

        static readonly Dictionary<string, Line[][]> SleepyHouse = new Dictionary<string, Line[][]>
        {
            ["photo"] = new[] { S(M("그 사진… 눈 뜨고 찍힌 몇 안 되는 사진이에요…", "halfsleep"), M("귀하죠…?", "smile")) },
            ["clock"] = new[] { S(M("벌써 이 시간이에요…?", "halfsleep"), M("시간이 가는 게 느껴지면… 졸려요…", "halfsleep")) },
            ["shoes"] = new[] { S(M("운동화요…? 걷는 건 좋아요… 천천히라면요…", "halfsleep"), M("중간에 벤치가 있으면 더 좋고요…", "smile")) },
            ["bedmirror"] = new[] { S(M("거울 속 제가 졸고 있어요…", "halfsleep"), M("…아, 저도 졸고 있었네요…", "surprise")) },
            ["portal"] = new[] { S(M("거기… 뭐가 있었던 것 같은데…", "halfsleep"), M("꿈이었나 봐요… 다음 꿈에서 찾아볼게요…", "smile")) },
            ["door"] = new[] { S(P("밖에 나가 볼까?"), M("밖은… 햇빛이 눈부셔요…", "halfsleep"), M("오늘은 집에서 뒹굴어요…", "smile")) },
            ["electro"] = new[] { S(M("배전함이요…? 전기가 나가면… 푹 잘 수 있겠네요…", "halfsleep"), M("…농담이에요. 아마도요…", "smile")) },
            ["books"] = new[] { S(M("책은… 세 쪽이면 잠들어요…", "halfsleep"), M("최고의 수면제예요…", "smile")) },
            ["umbrella"] = new[] { S(M("빗소리 들으면서 자는 게 제일 좋아요…", "smile"), M("비 오는 날 같이 낮잠 자요…", "halfsleep")) },
            ["oldtv"] = new[] { S(M("그 TV… 틀어 놓고 자기 좋아요…", "halfsleep"), M("지지직 소리가 자장가 같거든요…", "smile")) },
            ["pc"] = new[] { S(M("컴퓨터 화면은… 눈이 아파요…", "halfsleep"), M("대신 자장가 영상 틀 때는 써요…", "smile")) },
            ["figure"] = new[] { S(M("피규어는… 안 자도 돼서 부러워요…", "halfsleep"), M("…아니, 안 자는 건 싫어요…", "sad")) },
            ["uniform"] = new[] { S(M("교복이다… 수업 시간에 졸던 기억이…", "halfsleep"), M("…저 학생이었던 적 없는데… 이상하네요…", "surprise")) },
            ["fridge"] = new[] { S(M("냉장고엔… 커피 우유가 있어요…", "smile"), M("저 깨어 있게 해 주는 생명줄이에요…", "halfsleep")) },
            ["teapot"] = new[] { S(M("주전자… 커피 물 끓일까요…?", "halfsleep"), M("아니면 따뜻한 우유… 그건 더 졸리지만요…", "smile")) },
            ["teddy"] = new[] { S(M("곰돌이… 제 베개 친구예요…", "smile"), M("같이 누우면 폭신해요… 한번 해 볼래요…?", "halfsleep")) },
            ["recorder"] = new[] { S(M("카세트엔… 빗소리 테이프가 있어요…", "halfsleep"), M("틀면 오 분 안에 잠들어요… 보장해요…", "smile")) },
            ["moonlamp"] = new[] { S(M("달 조명… 제일 좋아하는 물건이에요…", "smile"), M("이거 켜면… 벌써 밤 같아서… 졸려요…", "halfsleep")) },
            ["cactus"] = new[] { S(M("선인장은 물을 자주 안 줘도 돼요…", "smile"), M("저처럼 게으른 사람한테 딱이에요…", "halfsleep")) },
            ["speakers"] = new[] { S(M("스피커로… 자장가 틀어 줄까요…?", "halfsleep"), M("같이 들으면 금방 잠들 거예요…", "smile")) },
            ["duck"] = new[] { S(M("오리… 욕조에서 둥둥…", "halfsleep"), M("따뜻한 물에 들어가면… 저도 둥둥 잠들어요…", "smile")) },
            ["bathmirror"] = new[] { S(M("거울 속 제 머리… 까치집이에요…", "halfsleep"), M("…빗어 주실래요…?", "shy")) },
        };

        static readonly Dictionary<string, Line[][]> ShortHouse = new Dictionary<string, Line[][]>
        {
            ["photo"] = new[] { S(M("사진 속 미타, 표정 관리가 훌륭하죠.", "smile"), M("신생 미타들한테 교재로 보여 줘야겠어요.", "smiletonque")) },
            ["clock"] = new[] { S(M("시간 관리 수업 시간이네요!", "smile"), M("[player], 오늘 할 일은 다 했어요? 체크해 봐요.", "smiletonque")) },
            ["shoes"] = new[] { S(M("운동화 끈은 이중 매듭으로 묶는 거예요.", "smile"), M("안 풀리는 비법, 나중에 알려 드릴게요.", "smile")) },
            ["bedmirror"] = new[] { S(M("거울 앞에서 자세 체크! 등 펴세요.", "smile"), M("좋아요. 바른 자세 점수 합격!", "smiletonque")) },
            ["portal"] = new[] { S(M("여기 있던 장치는 수업 범위 밖이에요.", "smile"), M("궁금해도 오늘은 넘어가요. 시험에 안 나와요!", "smiletonque")) },
            ["door"] = new[] { S(P("밖에 나가 볼까?"), M("현장 학습은 아직 계획에 없어요.", "smile"), M("오늘은 집 안 탐방 수업으로 대신할게요!", "smile")) },
            ["electro"] = new[] { S(M("배전함 안전 수칙! 젖은 손으로 만지지 않기.", "smile"), M("신생 미타들이 제일 먼저 배우는 거예요.", "smile")) },
            ["books"] = new[] { S(M("여기 책장은 제 수업 자료실이에요.", "smile"), M("추천 도서 목록 드릴까요? 열두 권밖에 안 돼요!", "smiletonque")) },
            ["umbrella"] = new[] { S(M("우산은 쓰고 나서 펴서 말려야 해요.", "smile"), M("안 그러면 곰팡이 나요. 오늘의 생활 팁!", "smiletonque")) },
            ["oldtv"] = new[] { S(M("그 TV로 교육 방송 틀어 줘요.", "smile"), M("…애니메이션도 교육이에요. 감정 교육이요!", "smiletonque")) },
            ["pc"] = new[] { S(M("컴퓨터로 신생 미타 출석부 관리해요.", "smile"), M("[player] 이름도 올려 놨어요. 우수 학생으로요.", "smiletonque")) },
            ["figure"] = new[] { S(M("이 피규어, 신생 미타들이 무서워해서 여기로 옮겼어요.", "smile"), M("저는 귀여운데 말이죠.", "smile")) },
            ["uniform"] = new[] { S(M("교복이네요! 선생님인 제가 입으면 좀 이상하죠?", "shy"), M("…한 번쯤은 괜찮을까요?", "smiletonque")) },
            ["fridge"] = new[] { S(M("냉장고 정리 규칙: 유통기한 짧은 건 앞으로!", "smile"), M("잘 지켜지고 있네요. 칭찬해요.", "smile")) },
            ["teapot"] = new[] { S(M("쉬는 시간엔 차 한잔이죠.", "smile"), M("[player], 설탕은 한 스푼이면 충분해요.", "smiletonque")) },
            ["teddy"] = new[] { S(M("곰 인형은 신생 미타 낮잠 시간용이에요.", "smile"), M("…가끔 제가 안고 자는 건 비밀이에요.", "shy")) },
            ["recorder"] = new[] { S(M("카세트에 제 강의를 녹음해 뒀어요.", "smile"), M("복습용이에요. 한번 들어 볼래요?", "smiletonque")) },
            ["moonlamp"] = new[] { S(M("달의 위상 변화, 알아요?", "smile"), M("이 조명으로 설명해 드릴게요. 오늘 밤 특강이에요!", "smiletonque")) },
            ["cactus"] = new[] { S(M("선인장 관찰 일지를 쓰고 있어요.", "smile"), M("이번 주엔 1밀리미터 자랐어요!", "surprise")) },
            ["speakers"] = new[] { S(M("스피커로 체조 음악 틀까요?", "smile"), M("국민 체조! 하나, 둘!", "smiletonque")) },
            ["duck"] = new[] { S(M("고무 오리는 욕조 수위 표시용이에요.", "smile"), M("…라고 하면 좀 교육적이죠?", "smiletonque")) },
            ["bathmirror"] = new[] { S(M("양치는 3분! 거울 보면서 구석구석이요.", "smile"), M("신생 미타들도 다 지키는 규칙이에요.", "smile")) },
        };

        static readonly Dictionary<string, Line[][]> ChibiHouse = new Dictionary<string, Line[][]>
        {
            ["photo"] = new[] { S(M("그 사진 속 미타보다 제가 더 귀여워요!", "arrogance"), M("인정하세요, 플레이어님!", "smile")) },
            ["clock"] = new[] { S(M("시계 볼 줄 알아요? 큰 바늘이 분이에요!", "arrogance"), M("…저도 방금 배웠어요.", "shy")) },
            ["shoes"] = new[] { S(M("운동화 엄청 크네요! 제 발 세 개는 들어가겠어요!", "surprise"), M("저 안에 숨어 있어도 돼요?", "smiletonque")) },
            ["bedmirror"] = new[] { S(M("거울이 너무 높아요! 안 보여요!", "angry"), M("플레이어님, 저 좀 들어 올려요!", "arrogance")) },
            ["portal"] = new[] { S(M("여기 뭐가 있었어요?", "quest"), M("몰라요! 저보다 작은 거면 제가 찾아 줄게요!", "smile")) },
            ["door"] = new[] { S(P("밖에 나가 볼까?"), M("안 돼요! 밖은 위험해요!", "angry"), M("플레이어님은 바보라서 길 잃어버릴 거예요!", "arrogance")) },
            ["electro"] = new[] { S(M("배전함 스위치 누르고 싶다…", "smiletonque"), M("…안 눌러요! 진짜예요! 플레이어님이 보고 있으니까요!", "discontent")) },
            ["books"] = new[] { S(M("책이 저보다 커요!", "surprise"), M("그림책은 없어요? 글자 많은 건 머리 아파요!", "discontent")) },
            ["umbrella"] = new[] { S(M("우산 펴면 제 텐트예요!", "smile"), M("플레이어님은 너무 커서 못 들어와요!", "arrogance")) },
            ["oldtv"] = new[] { S(M("TV 틀어 줘요! 만화 볼래요!", "smile"), M("리모컨은 제 거예요!", "arrogance")) },
            ["pc"] = new[] { S(M("컴퓨터 게임 해도 돼요?", "smile"), M("플레이어님은 옆에서 박수만 쳐요!", "arrogance")) },
            ["figure"] = new[] { S(M("이 피규어 저랑 키 똑같아요!", "surprise"), M("…제가 조금 더 커요! 조금!", "angry")) },
            ["uniform"] = new[] { S(M("교복! 저한텐 너무 커요!", "discontent"), M("치마가 드레스가 되겠어요. 그것도 귀엽겠다!", "smile")) },
            ["fridge"] = new[] { S(M("냉장고 위에 사탕 있는 거 알아요!", "smile"), M("꺼내 줘요, 플레이어님! 키 큰 거 이럴 때 쓰는 거예요!", "arrogance")) },
            ["teapot"] = new[] { S(M("주전자 뜨거워요! 만지지 마요!", "angry"), M("…저 걱정한 거 아니에요. 플레이어님이 다칠까 봐요!", "shy")) },
            ["teddy"] = new[] { S(M("곰돌이! 제 부하예요!", "smile"), M("플레이어님은 부하 2호예요!", "arrogance")) },
            ["recorder"] = new[] { S(M("이거 누르면 제 목소리 녹음돼요?", "smile"), M("\"플레이어님은 바보!\" …헤헤, 녹음 완료!", "smiletonque")) },
            ["moonlamp"] = new[] { S(M("달 조명! 이거 켜 놓고 자요!", "smile"), M("어두운 게 무서운 거 아니에요! 그냥 예뻐서요!", "angry")) },
            ["cactus"] = new[] { S(M("선인장 찔려 봤어요! 아팠어요!", "sad"), M("플레이어님은 만지지 마요. 바보니까 또 찔릴 거예요!", "arrogance")) },
            ["speakers"] = new[] { S(M("스피커에 대고 소리 지르면 커질까요?", "smile"), M("아아아! …안 커지네요. 고장인가!", "discontent")) },
            ["duck"] = new[] { S(M("오리다! 제 목욕 친구예요!", "smile"), M("꽥꽥! 플레이어님도 따라 해요!", "smiletonque")) },
            ["bathmirror"] = new[] { S(M("거울이 저 안 보여 줘요! 너무 높아요!", "angry"), M("플레이어님이 대신 보고 말해 줘요. 저 귀엽죠?", "smile")) },
        };
    }
}
