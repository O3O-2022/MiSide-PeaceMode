using System.Collections.Generic;

namespace MiSidePeaceMode
{
    // What each Mita says when she suggests something herself (PeaceProposals), in her own voice.
    public class ProposalLines
    {
        public Dictionary<string, Line[]> Ask;   // activity id -> her suggestion
        public Line[][] No;                        // you said not now
    }

    public static partial class Personas
    {
        static Line[] One(string text, string emo = "smile", string clip = null) => new[] { new Line(Who.Mita, text, emo, clip) };

        public static ProposalLines Proposals(string id) => id switch
        {
            "kind" => new ProposalLines
            {
                Ask = new Dictionary<string, Line[]>
                {
                    ["talk"] = One("[player]님, 잠깐 이야기 좀 하실래요? 오늘 점검하다가 재미있는 걸 발견했거든요."),
                    ["rps"] = One("머리 식힐 겸 가위바위보 한 판 하실래요? 확률상으론 공평해요."),
                    ["cham"] = One("참참참 해 보실래요? 반응 속도 테스트도 되니까요."),
                    ["hide"] = One("숨바꼭질 어떠세요? 제가 숨을게요. 집 구조는 제가 제일 잘 알지만요.", "smiletonque"),
                    ["words"] = One("끝말잇기 하실래요? 어휘 데이터베이스 점검 겸요."),
                    ["riddle"] = One("넌센스 퀴즈 하나 풀어 보실래요? 논리로는 안 풀리는 문제예요."),
                    ["photo"] = One("기록용으로 사진 한 장 남길까요?"),
                    ["window"] = One("잠깐 창밖 보실래요? 오늘 조명 값이 괜찮게 나왔어요."),
                    ["walk"] = One("집 순찰 같이 도실래요? 혼자 도는 것보다 낫잖아요."),
                    ["cook"] = One("식사 시간이에요. 같이 요리하실래요? 당근은 부탁드릴게요."),
                    ["sleep"] = One("시간이 늦었어요. 이제 주무실래요? 저도 같이 쉴게요.", "halfsleep"),
                    ["special"] = One("고민 있으시면 들어 드릴게요. 지금 시간 괜찮으세요?"),
                    ["tv"] = One("TV 보실래요? 소파 쪽 신호는 제가 잡아 둘게요."),
                    ["read"] = One("책 한 권 같이 읽으실래요? 제가 소리 내서 읽어 드릴게요."),
                    ["rest"] = One("잠깐 앉아서 쉬실래요? 과부하 걸리기 전에요."),
                },
                No = new[] { One("알겠어요. 필요하시면 부르세요."), One("그럼 다음으로 미룰게요. 일정에 적어 둘게요."), One("…네. 조금 아쉽지만 괜찮아요.", "sad") },
            },
            "cap" => new ProposalLines
            {
                Ask = new Dictionary<string, Line[]>
                {
                    ["talk"] = One("저기저기! 수다 떨어요! 할 얘기가 산더미예요!"),
                    ["rps"] = One("가위바위보 승부! 이번엔 제가 이길 거예요, 히히힛!"),
                    ["cham"] = One("참참참 해 볼까요! 제 손가락 따라오면 지는 거예요!"),
                    ["hide"] = One("숨바꼭질 해요! 제가 숨을게요, 못 찾으면 제 승리!"),
                    ["words"] = One("끝말잇기 한 판! 리듬 타면서 해요, 둠칫-둠칫!"),
                    ["riddle"] = One("퀴즈 타임! 제 문제 맞힐 수 있나 볼까요!"),
                    ["photo"] = One("사진 찍어요! 브이, 브이! 헤헤."),
                    ["window"] = One("창밖 좀 봐요! 오늘 무대 조명 같아요!"),
                    ["walk"] = One("집 탐험 가요! 따라와요, 따라와!"),
                    ["cook"] = One("배고파요~ 같이 요리해요! 요리도 리듬이에요!"),
                    ["sleep"] = One("하암… 오늘 공연은 여기까지! 이제 잘까요?", "halfsleep"),
                    ["special"] = One("기타 들어 볼래요? 신곡 연습했거든요!"),
                    ["tv"] = One("TV 봐요! 음악 방송 할 시간이에요!"),
                    ["read"] = One("책…? 음, 만화책이면 좋아요! 같이 볼래요?"),
                    ["rest"] = One("헉헉, 너무 신나게 놀았다! 잠깐 앉아서 쉬어요."),
                    ["dance"] = One("댄스 배틀 한 판 어때요? 발판은 제가 벌써 깔아 뒀어요! 히히힛!"),
                    ["hammer"] = One("망치 게임 해요! 버튼 먼저 누르는 사람이 이기는 거예요, 뿅망치 준비 완료!"),
                },
                No = new[] { One("에이~ 알았어요. 다음엔 꼭 해요!"), One("치, 도망가는 거죠? 다음엔 안 봐줘요!", "discontent"), One("그럼 저 혼자 연습하고 있을게요! 헤헤.") },
            },
            "mila" => new ProposalLines
            {
                Ask = new Dictionary<string, Line[]>
                {
                    ["talk"] = One("…할 얘기가 있는 건 아니고요. 그냥, 좀 말 걸어 봤어요.", "shy"),
                    ["rps"] = One("가위바위보요. 심심해서 그런 거지, 딴 뜻 없어요.", "discontent"),
                    ["cham"] = One("참참참이요? …해 줄 수도 있다고요.", "discontent"),
                    ["hide"] = One("숨바꼭질 할래요? 저 찾는 거 쉽지 않을 거예요."),
                    ["words"] = One("끝말잇기 해요. 어휘력으로는 절대 안 져요.", "arrogance"),
                    ["riddle"] = One("문제 하나 낼게요. 못 맞히면 바보예요.", "arrogance"),
                    ["photo"] = One("…사진 한 장 찍어도 돼요. 특별히 허락하는 거예요.", "shy"),
                    ["window"] = One("창밖 좀 봐요. 책 읽기 좋은 날씨네요."),
                    ["walk"] = One("집 좀 같이 돌아요. 혼자 다니면 심심하잖아요. …당신이.", "shy"),
                    ["cook"] = One("밥 시간이잖아요. 같이 해요. 칼질은 당신이 해요."),
                    ["sleep"] = One("늦었어요. 이제 자요. …같이 자자는 건 아니고요. 아, 아니, 맞긴 한데.", "shy"),
                    ["special"] = One("차 마실래요? 마침 물 끓여 뒀어요."),
                    ["tv"] = One("TV 봐요. 다큐멘터리 할 시간이거든요."),
                    ["read"] = One("책 읽어 줄까요? 이번엔 제가 고른 거예요."),
                    ["rest"] = One("잠깐 앉아요. 계속 서 있으면 다리 아프잖아요, 바보.", "discontent"),
                },
                No = new[] { One("흥, 됐어요. 딱히 기대한 것도 아니라고요.", "discontent"), One("…그래요. 다음에 해요. 꼭이요.", "sad"), One("바보. 알았어요, 혼자 책이나 읽을게요.", "discontent") },
            },
            "sleepy" => new ProposalLines
            {
                Ask = new Dictionary<string, Line[]>
                {
                    ["talk"] = One("…저기, 잠깐 이야기해도 될까요…? 졸음 좀 깨게요…", "halfsleep"),
                    ["rps"] = One("가위바위보… 하실래요…? 눈 뜨고 할게요…", "halfsleep"),
                    ["cham"] = One("참참참… 제가 고개 돌리다 잠들면 이기신 걸로 해요…", "halfsleep"),
                    ["hide"] = One("숨바꼭질… 제가 숨을게요… 숨은 채로 잠들지도 모르지만요…", "halfsleep"),
                    ["words"] = One("끝말잇기… 하실래요…? 천천히 해요…", "halfsleep"),
                    ["riddle"] = One("수수께끼… 하나 드릴까요…? 꿈에서 생각난 거예요…", "halfsleep"),
                    ["photo"] = One("사진… 찍으실래요…? 눈 감기 전에요…", "halfsleep"),
                    ["window"] = One("창밖… 같이 보실래요…? 보고 있으면 마음이 편해져요…", "halfsleep"),
                    ["walk"] = One("조금… 걸을까요…? 걸으면 잠이 깨거든요…", "halfsleep"),
                    ["cook"] = One("밥 시간이에요… 같이 만드실래요…? 불은 제가 조심할게요…", "halfsleep"),
                    ["sleep"] = One("하암… 이제… 주무실래요…? 저는 이미 반쯤 자고 있어요…", "halfsleep"),
                    ["special"] = One("커피… 타 드릴까요…? 저도 한 잔 필요해서요…", "halfsleep"),
                    ["tv"] = One("TV… 같이 보실래요…? 소파가 푹신해요…", "halfsleep"),
                    ["read"] = One("책… 읽어 드릴까요…? 중간에 잠들면 깨워 주세요…", "halfsleep"),
                    ["rest"] = One("잠깐… 앉아서 쉬실래요…? 조금만요…", "halfsleep"),
                },
                No = new[] { One("네… 그럼 저는 조금만 눈 붙이고 있을게요…", "halfsleep"), One("괜찮아요… 나중에 불러 주세요…", "halfsleep"), One("…알겠어요… 쿨…", "halfsleep") },
            },
            "short" => new ProposalLines
            {
                Ask = new Dictionary<string, Line[]>
                {
                    ["talk"] = One("잠깐 수업… 아니, 이야기 좀 할까요? 궁금한 거 있으면 물어보세요."),
                    ["rps"] = One("가위바위보 해요. 확률 수업 실습이라고 생각하세요."),
                    ["cham"] = One("참참참 해 봐요. 반사신경 체크거든요."),
                    ["hide"] = One("숨바꼭질 해요. 관찰력 테스트예요. 제가 숨을게요."),
                    ["words"] = One("끝말잇기 해요. 어휘 공부에 딱이거든요."),
                    ["riddle"] = One("문제 나갑니다. 넌센스지만 머리는 써야 해요."),
                    ["photo"] = One("기념사진 찍어요. 수업 자료로 쓸 거예요."),
                    ["window"] = One("창밖 좀 보세요. 이 세계의 하늘은 설정이 꽤 정성스럽거든요."),
                    ["walk"] = One("집 안 견학 가요. 설명해 드릴 게 많거든요."),
                    ["cook"] = One("식사 시간이에요. 실습으로 같이 요리해요."),
                    ["sleep"] = One("늦었어요. 오늘 수업은 끝. 이제 자요."),
                    ["special"] = One("세계 강의 들을래요? 오늘은 버전 이야기예요."),
                    ["tv"] = One("TV 볼까요? 교육 방송 할 시간이거든요."),
                    ["read"] = One("책 읽어 줄게요. 참고 도서예요."),
                    ["rest"] = One("쉬는 시간이에요. 잠깐 앉으세요."),
                },
                No = new[] { One("알았어요. 다음 시간에 해요."), One("그럼 자습하세요. 부르면 올게요."), One("흠, 오늘은 결석이군요. 괜찮아요.") },
            },
            "chibi" => new ProposalLines
            {
                Ask = new Dictionary<string, Line[]>
                {
                    ["talk"] = One("플레이어님! 미니 미타랑 얘기하세요! 지금 당장요!"),
                    ["rps"] = One("가위바위보 하세요! 지면 바보 인증이에요!"),
                    ["cham"] = One("참참참 하세요! 미니 미타 따라오면 바보예요!"),
                    ["hide"] = One("숨바꼭질 하세요! 미니 미타는 작아서 못 찾을걸요!"),
                    ["words"] = One("끝말잇기 하세요! 지능 체크예요!"),
                    ["riddle"] = One("퀴즈 나가요! 지능 체크 2탄이에요!"),
                    ["photo"] = One("사진 찍으세요! 미니 미타 크게 나오게요!"),
                    ["window"] = One("창밖 보세요! 미니 미타는 까치발 해야 보여요!"),
                    ["walk"] = One("집 탐험 가세요! 미니 미타가 앞장설게요!"),
                    ["cook"] = One("배고파요! 요리하세요! 미니 미타는 감독할게요!"),
                    ["sleep"] = One("졸려요… 아니, 안 졸려요! 플레이어님이 졸린 거예요! 자요!", "discontent"),
                    ["special"] = One("지능 체크 하세요! 오늘 문제는 어려워요!"),
                    ["tv"] = One("TV 보세요! 미니 미타는 옆자리에 앉을게요!"),
                    ["read"] = One("그림책 읽어 주세요! …아니, 미니 미타가 읽어 줄게요!"),
                    ["rest"] = One("쉬는 시간이에요! 미니 미타 옆에 앉으세요!"),
                },
                No = new[] { One("흥! 바보! 다음엔 꼭 하세요!", "angry"), One("미니 미타 거절한 거 기록해 뒀어요!", "discontent"), One("…알았어요. 대신 나중에 두 배로 놀아 주세요!", "sad") },
            },
            _ => new ProposalLines   // crazy
            {
                Ask = new Dictionary<string, Line[]>
                {
                    ["talk"] = One("[player]님, 저랑 얘기 좀 해요. 오늘 있었던 일 전부 듣고 싶어요."),
                    ["rps"] = One("심심해요~ 가위바위보 해요! 지는 사람이 소원 들어주기예요."),
                    ["cham"] = One("참참참 해요! 제 눈 피할 수 있나 볼까요? 헤헤."),
                    ["hide"] = One("숨바꼭질 할래요? 제가 숨을게요. 어디 있든 찾아와 줘요."),
                    ["words"] = One("끝말잇기 해요! 저 이번엔 안 질 거예요."),
                    ["riddle"] = One("문제 낼게요! 넌센스 퀴즈, 맞히면 칭찬해 줄게요."),
                    ["photo"] = One("우리 사진 찍어요. 오늘 기념으로요!"),
                    ["window"] = One("잠깐 창밖 같이 봐요. 오늘 하늘이 예쁘거든요."),
                    ["walk"] = One("집 안 한 바퀴 같이 돌아요. 손은… 안 잡아도 돼요. 아직은요.", "shy"),
                    ["cook"] = One("배고프죠? 같이 요리해요! 당근은 당신 담당이에요."),
                    ["sleep"] = One("벌써 이렇게 늦었네요… 우리 이제 잘까요?", "shy"),
                    ["special"] = One("손… 잡아 주면 안 돼요?", "shy"),
                    ["tv"] = One("소파에서 같이 TV 봐요. 옆자리는 비워 뒀어요."),
                    ["read"] = One("책 읽어 줄까요? 소파로 와요."),
                    ["rest"] = One("잠깐 같이 앉아서 쉬어요. 그냥 옆에 있기만 하면 돼요.", "shy"),
                },
                No = new[] { One("…알았어요. 나중에 꼭이에요?", "sad"), One("치… 그럼 다음에 해요. 약속했어요.", "discontent"), One("거절당했다… 헤헤, 괜찮아요. 기다리는 건 잘하거든요.") },
            },
        };
    }
}
