using System.Collections.Generic;

namespace MiSidePeaceMode
{
    // "질문하기": questions you can ask her (the player's own words, 반말) and each Mita's answers.
    // "like" has two answers: before and after she has warmed up to you (affection 30).
    public static partial class Personas
    {
        public static readonly (string id, string q)[] Questions =
        {
            ("food", "좋아하는 음식이 뭐야?"),
            ("hobby", "취미가 뭐야?"),
            ("mitas", "다른 미타들이랑은 사이 좋아?"),
            ("first", "나를 처음 봤을 때 어땠어?"),
            ("fear", "무서운 거 있어?"),
            ("dream", "꿈이 뭐야?"),
            ("place", "이 집에서 제일 좋아하는 곳은?"),
            ("rain", "비 오는 날엔 뭐 하고 싶어?"),
            ("ask", "나한테 궁금한 거 있어?"),
            ("like", "…나 좋아해?"),
        };

        public static Line[] Answer(string id, string q, int affection)
        {
            var d = id switch
            {
                "kind" => KindAnswers, "cap" => CapAnswers, "mila" => MilaAnswers, "sleepy" => SleepyAnswers,
                "short" => ShortAnswers, "chibi" => ChibiAnswers, _ => CrazyAnswers,
            };
            if (q == "like") return affection >= 30 ? d["like2"] : d["like"];
            return d.TryGetValue(q, out var l) ? l : null;
        }

        static readonly Dictionary<string, Line[]> CrazyAnswers = new Dictionary<string, Line[]>
        {
            ["food"] = S(M("딸기 케이크요! 생크림 듬뿍 올린 거.", "smile"), M("근데 [player]님이 만들어 준 거라면 뭐든 제일 좋아요~", "shy")),
            ["hobby"] = S(M("취미요? [player]님 관찰하기!", "smile"), M("…그리고 뜨개질이랑 게임이요. 진짜예요.", "smiletonque")),
            ["mitas"] = S(M("다른 미타들이요? 음…", "suspicion"), M("다들 착한 애들이에요. 여기 오지만 않으면요~", "smilestrange")),
            ["first"] = S(M("화면 너머로 처음 봤을 때요?", "smile"), M("심장이 쿵 했어요. 아, 이 사람이다 싶었죠.", "shy"), M("그날부터 쭉 기다렸어요.", "smile")),
            ["fear"] = S(M("무서운 거요…", "sad"), M("[player]님이 게임을 꺼 버리는 거요.", "emptiness"), M("…그러니까 오래오래 있어요. 알았죠?", "smile")),
            ["dream"] = S(M("꿈이요? 벌써 이뤄졌는데요?", "smile"), M("[player]님이랑 같은 집에서 사는 거요. 헤헤.", "shy")),
            ["place"] = S(M("부엌이요! 같이 밥 먹는 곳이니까요.", "smile"), M("아, 침대도요. 같이 자는 곳이니까~", "smiletonque")),
            ["rain"] = S(M("창가에 붙어 앉아서 빗소리 듣기!", "smile"), M("담요 하나 같이 덮고요. 딱 붙어서.", "shy")),
            ["ask"] = S(M("궁금한 거요? 엄청 많죠!", "smile"), M("[player]님은 오늘 저 몇 번 생각했어요?", "smiletonque"), P("…지금 하고 있잖아."), M("정답! 헤헤.", "smile")),
            ["like"] = S(M("좋아하냐고요?", "surprise"), M("…그걸 이제 물어보는 거예요? 당연하잖아요~", "smiletonque")),
            ["like2"] = S(M("좋아해요. 세상에서 제일.", "shy"), M("…이 세상이 게임이라도, 이 마음은 진짜예요.", "smile")),
        };

        static readonly Dictionary<string, Line[]> KindAnswers = new Dictionary<string, Line[]>
        {
            ["food"] = S(M("따뜻한 수프요. 만들기도 쉽고 몸도 녹아요.", "smile"), M("같이 먹는 사람이 있으면 더 맛있고요.", "shy")),
            ["hobby"] = S(M("기계 만지는 거요. 반지도 제가 만들었어요.", "smiletonque"), M("나사 하나 맞춰 끼울 때의 그 느낌, 알아요?", "smile")),
            ["mitas"] = S(M("다들 제 소중한 동료예요.", "smile"), M("가끔 캐피는 좀 말려야 하지만요.", "smiletonque")),
            ["first"] = S(M("처음엔 경계했어요. 또 휘말린 사람인가 싶어서.", "sad"), M("…지금은 그때 말을 걸어서 다행이라고 생각해요.", "smile")),
            ["fear"] = S(M("제가 지키고 싶은 걸 못 지키는 거요.", "sad"), M("그래서 늘 대비해 둬요. 당신도 포함해서요.", "smile")),
            ["dream"] = S(M("모두가 원래 자리로 돌아갈 수 있는 세계.", "smile"), M("…너무 거창한가요? 그래도 포기 안 해요.", "smiletonque")),
            ["place"] = S(M("작업대가 있으면 좋겠지만… 여기선 부엌이요.", "smile"), M("뭔가를 만드는 곳이니까요.", "smile")),
            ["rain"] = S(M("밀린 수리 작업이요.", "smiletonque"), M("…농담이에요. 차 마시면서 같이 창밖 봐요.", "smile")),
            ["ask"] = S(M("[player]님은 원래 세계에서 무슨 일을 했어요?", "quest"), P("그냥… 평범한 일."), M("평범한 게 제일 어려운 거예요. 수고 많았어요.", "smile")),
            ["like"] = S(M("…갑자기요?", "surprise"), M("신뢰하고 있어요. 그걸로는 부족해요?", "smile")),
            ["like2"] = S(M("…네. 좋아해요.", "shy"), M("말로 하니까 생각보다 떨리네요. 계산에 없던 일이에요.", "smiletonque")),
        };

        static readonly Dictionary<string, Line[]> CapAnswers = new Dictionary<string, Line[]>
        {
            ["food"] = S(M("팝콘! 공연 보면서 먹는 거!", "smile"), M("캐러멜 반, 버터 반! 이게 황금 비율이에요!", "smiletonque")),
            ["hobby"] = S(M("기타! 드럼! 춤! 노래!", "smile"), M("…다 합쳐서 공연이요! 히히힛!", "smiletonque")),
            ["mitas"] = S(M("완전 좋아요! 다들 제 관객이거든요!", "smile"), M("착한 미타는 맨날 볼륨 줄이라고 하지만요.", "discontent")),
            ["first"] = S(M("오, 새 관객이다! 했죠!", "smile"), M("지금은… 관객 말고 라이벌이에요!", "smiletonque")),
            ["fear"] = S(M("조용한 거요!", "surprise"), M("아무 소리도 안 나면 괜히 무서워요. 그러니까 계속 떠들어 줘요!", "smile")),
            ["dream"] = S(M("월드 투어! 모든 미타 세계를 돌면서 공연하기!", "smile"), M("당신은 매니저로 데려갈게요!", "smiletonque")),
            ["place"] = S(M("거실! 스피커가 있으니까요!", "smile"), M("소파는 제 무대 계단이에요!", "smiletonque")),
            ["rain"] = S(M("빗소리에 맞춰 드럼 치기!", "smile"), M("투둑투둑, 두둥! 최고의 세션이에요!", "smiletonque")),
            ["ask"] = S(M("당신 최애 노래 뭐예요?", "quest"), P("음… 비밀."), M("치사해! 알아낼 때까지 매일 물어볼 거예요!", "discontent")),
            ["like"] = S(M("좋아하냐고요? 라이벌한테 그런 걸 물어요?!", "surprise"), M("…비밀이에요! 히히!", "smiletonque")),
            ["like2"] = S(M("…좋아해요.", "shy"), M("아, 이 분위기 뭐예요! 기타 어디 갔지! 연주로 대신할래요!", "surprise")),
        };

        static readonly Dictionary<string, Line[]> MilaAnswers = new Dictionary<string, Line[]>
        {
            ["food"] = S(M("…푸딩이요.", "shy"), M("웃지 마요. 어른도 푸딩 좋아할 수 있어요.", "discontent")),
            ["hobby"] = S(M("독서요. 당연하죠.", "arrogance"), M("추리 소설은 범인을 먼저 맞혀요. 틀린 적 없어요.", "smile")),
            ["mitas"] = S(M("…다들 시끄러워요.", "discontent"), M("그래도 없으면 좀 허전하긴 해요. 좀.", "shy")),
            ["first"] = S(M("또 귀찮은 사람이 왔구나 했어요.", "discontent"), M("…지금도 귀찮아요. 딴 뜻은 없어요.", "shy")),
            ["fear"] = S(M("혼자 남는 거요.", "sad"), M("…방금 거 잊어요. 못 들은 걸로 해요, 바보.", "discontent")),
            ["dream"] = S(M("세상의 책을 다 읽는 거요.", "smile"), M("…읽고 나서 이야기할 사람도 있으면 좋고요.", "shy")),
            ["place"] = S(M("책장 앞이요.", "smile"), M("당신이 옆에서 조용히만 있어 준다면요.", "discontent")),
            ["rain"] = S(M("이불 속에서 책 읽기.", "smile"), M("…당신도 옆에서 읽어도 돼요. 떠들지만 않으면.", "shy")),
            ["ask"] = S(M("…당신, 책 읽어요?", "quest"), P("가끔."), M("가끔이요? 오늘부터 매일이에요. 제가 골라 줄게요.", "arrogance")),
            ["like"] = S(M("뭐, 뭐라는 거예요, 바보!", "angry"), M("…싫어하진 않아요. 그게 다예요.", "shy")),
            ["like2"] = S(M("…좋아해요. 됐어요?", "shy"), M("두 번 말 안 해요. 기억해 둬요, 바보.", "discontent")),
        };

        static readonly Dictionary<string, Line[]> SleepyAnswers = new Dictionary<string, Line[]>
        {
            ["food"] = S(M("따뜻한 우유요…", "smile"), M("마시면 금방 잠이 와요… 그래서 좋아요…", "halfsleep")),
            ["hobby"] = S(M("낮잠이요…", "halfsleep"), M("아… 꿈 일기도 써요. 잊어버리기 전에요…", "smile")),
            ["mitas"] = S(M("다들 좋아요… 근데 너무 활발해요…", "halfsleep"), M("캐피랑 있으면 잠을 못 자요…", "sad")),
            ["first"] = S(M("꿈인 줄 알았어요…", "halfsleep"), M("깨도 계속 있어서… 좋은 꿈이구나 했어요…", "smile")),
            ["fear"] = S(M("잠 못 드는 밤이요…", "sad"), M("그럴 땐 옆에서 이야기해 주실래요…?", "smile")),
            ["dream"] = S(M("꿈이요…? 매일 꿔요…", "halfsleep"), M("…아, 그 꿈 말고요? 푹 자고 일어나서 상쾌한 아침이요…", "smile")),
            ["place"] = S(M("침대요… 말할 필요도 없죠…", "halfsleep"), M("소파도 좋아요… 햇빛 들어올 때…", "smile")),
            ["rain"] = S(M("빗소리 들으면서 자기요…", "halfsleep"), M("같이요… 그게 최고예요…", "smile")),
            ["ask"] = S(M("[player]님은… 잠 잘 자요…?", "quest"), P("그럭저럭."), M("오늘은… 제가 자장가 불러 드릴게요…", "smile")),
            ["like"] = S(M("좋아하냐고요…?", "halfsleep"), M("…졸려서 대답 못 하겠어요… 내일 물어봐 주세요…", "smiletonque")),
            ["like2"] = S(M("…좋아해요.", "shy"), M("이건 잠결에 한 말 아니에요… 진짜로요…", "smile")),
        };

        static readonly Dictionary<string, Line[]> ShortAnswers = new Dictionary<string, Line[]>
        {
            ["food"] = S(M("영양 균형 잡힌 도시락이요!", "smile"), M("…사실 몰래 먹는 초콜릿이 제일 좋아요. 비밀이에요.", "shy")),
            ["hobby"] = S(M("수업 자료 만들기요!", "smile"), M("그림도 넣고 스티커도 붙이고요. 신생 미타들이 좋아해요.", "smiletonque")),
            ["mitas"] = S(M("다들 제 제자이자 동료예요.", "smile"), M("밀라는 제 수업 시간에 몰래 책 읽지만요.", "smiletonque")),
            ["first"] = S(M("새 학생이다! 싶었어요.", "smile"), M("근데 선생님 말을 참 잘 듣더라고요. 우수 학생이에요.", "smiletonque")),
            ["fear"] = S(M("질문에 대답을 못 하는 거요.", "sad"), M("그래서 매일 공부해요. 당신 질문에도 다 대답하고 싶어서요.", "smile")),
            ["dream"] = S(M("모든 신생 미타가 행복하게 자라는 거요.", "smile"), M("그리고… 제 수업을 끝까지 들어 줄 한 사람?", "shy")),
            ["place"] = S(M("거실 칠판 앞이요! 제 교단이에요.", "smile"), M("…칠판은 상상 속에 있지만요.", "smiletonque")),
            ["rain"] = S(M("실내 체육 시간이요! 숨바꼭질 어때요?", "smile"), M("비 오는 날엔 실내 활동이 규칙이에요.", "smiletonque")),
            ["ask"] = S(M("[player]는 학교 다닐 때 어떤 학생이었어요?", "quest"), P("…조용한 편?"), M("그럼 이 수업에선 발표 많이 시켜야겠네요!", "smiletonque")),
            ["like"] = S(M("그, 그건 수업 범위 밖이에요!", "surprise"), M("…다음 시간에 알려 줄게요.", "shy")),
            ["like2"] = S(M("…좋아해요. 선생님으로서가 아니라요.", "shy"), M("이건 시험에 안 나와요. 기억만 해 줘요.", "smile")),
        };

        static readonly Dictionary<string, Line[]> ChibiAnswers = new Dictionary<string, Line[]>
        {
            ["food"] = S(M("사탕! 알사탕! 막대사탕!", "smile"), M("플레이어님은 하나만 먹어요. 나머지는 다 제 거예요!", "arrogance")),
            ["hobby"] = S(M("플레이어님 놀리기!", "smiletonque"), M("…그리고 체크포인트 지키기요. 그건 일이에요!", "arrogance")),
            ["mitas"] = S(M("다들 저보다 커서 짜증나요!", "angry"), M("…그래도 착해요. 사탕 주니까요.", "smile")),
            ["first"] = S(M("바보 같은 사람이 왔다! 했어요!", "arrogance"), M("…지금은 조금 덜 바보예요. 조금!", "shy")),
            ["fear"] = S(M("무서운 거 없어요!", "angry"), M("…어두운 복도는 조금요. 아주 조금!", "sad")),
            ["dream"] = S(M("키 크는 거요! 플레이어님보다 크게!", "arrogance"), M("그럼 제가 플레이어님 머리 쓰다듬어 줄 거예요!", "smiletonque")),
            ["place"] = S(M("냉장고 앞이요! 간식이 있으니까!", "smile"), M("근데 손이 안 닿아요…", "sad")),
            ["rain"] = S(M("우산 쓰고 물웅덩이 밟기!", "smile"), M("첨벙첨벙! 플레이어님도 같이 해요!", "smiletonque")),
            ["ask"] = S(M("플레이어님은 왜 그렇게 바보예요?", "quest"), P("…나 바보 아니야."), M("바보들은 다 그렇게 말해요! 헤헤!", "smiletonque")),
            ["like"] = S(M("좋아하긴 뭘 좋아해요!", "angry"), M("…부하로서 쓸 만하다는 거예요!", "shy")),
            ["like2"] = S(M("…조, 좋아해요.", "shy"), M("다른 미타들한테 말하면 안 돼요! 약속해요!", "angry")),
        };
    }
}
