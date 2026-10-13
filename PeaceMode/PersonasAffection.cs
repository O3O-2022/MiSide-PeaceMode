using System.Collections.Generic;

namespace MiSidePeaceMode
{
    // What she says as her feelings for you change (see PeaceAffection): cold lines for the bottom three steps (her
    // mumbling, her greeting, a touch refused, being stared at), a remark when the step goes up or down, and when it
    // reaches the very top or the very bottom.
    public class AffLines
    {
        public string[] ColdIdle;
        public Line[][] ColdGreeting, ColdTouch, ColdStare;
        public Line[][] Up, Down;
        public Line[] Top, Bottom;
    }

    public static partial class Personas
    {
        static readonly Dictionary<string, AffLines> affCache = new Dictionary<string, AffLines>();

        public static AffLines Aff(string id)
        {
            if (affCache.TryGetValue(id, out var a)) return a;
            a = id switch
            {
                "kind" => new AffLines
                {
                    ColdIdle = new[] { "…신뢰도 수치가 떨어졌어요. 기록에도 남았고요.", "오늘은 필요한 말만 할게요.", "고칠 수 있는 건 기계뿐인가 봐요.", "…저도 가끔은 화가 나요. 조용히요." },
                    ColdGreeting = V(S(M("왔군요.", "discontent"), M("업무적으로만 대할게요. 당분간은요.", "discontent")),
                                     S(M("[player]님, 지난번 일에 대해 할 말이 있어요.", "discontent"), M("…아니에요. 오늘은 넘어갈게요.", "sad"))),
                    ColdTouch = V(S(M("지금은 거리를 유지해 주세요.", "contempt")), S(M("…그건 신뢰가 있을 때 하는 거예요.", "sad")), S(M("분석 결과, 아직 이르다고 나왔어요.", "contempt"))),
                    ColdStare = V(S(M("…하실 말씀 있으세요? 없으면 그만 보세요.", "contempt"))),
                    Up = V(S(M("관계 지표가 한 단계 올라갔어요.", "smile"), M("…수치로 말하니까 멋없네요. 그냥, 좋아요.", "shy")),
                           S(M("요즘 당신이랑 있으면 계획이 필요 없어요.", "smile")),
                           S(M("신뢰도 상승 확인. 고마워요, [player]님.", "smile"))),
                    Down = V(S(M("…요즘 우리 사이 수치가 떨어지고 있어요.", "sad"), M("원인 분석은 하고 싶지 않네요.", "discontent")),
                             S(M("제가 너무 기대했나 봐요.", "sad")),
                             S(M("괜찮아요. 수리 가능한 범위예요. …아마도요.", "discontent"))),
                    Top = S(M("[player]님, 이건 계산이 아니에요.", "shy"), M("당신은 제가 지키고 싶은 전부예요.", "smile")),
                    Bottom = S(M("…신뢰도, 바닥이에요.", "discontent"), M("그래도 문은 잠그지 않을게요. 그게 제 마지막 원칙이에요.", "sad")),
                },
                "cap" => new AffLines
                {
                    ColdIdle = new[] { "…오늘은 기타 안 칠래요.", "라이벌이라더니, 요즘 시합도 안 해 주잖아요.", "조용하네요. 제가 조용한 거 싫어하는 거 알면서.", "흥, 솔로 공연이나 해야겠다. 관객 없이." },
                    ColdGreeting = V(S(M("…아, 왔어요?", "discontent"), M("오늘은 공연 없어요. 휴업이에요.", "discontent")),
                                     S(M("라이벌.", "discontent"), M("…요즘 저 무시하는 거 다 알아요.", "sad"))),
                    ColdTouch = V(S(M("지금 그럴 기분 아니에요!", "contempt")), S(M("…모자 건드리지 마요. 오늘은요.", "contempt")), S(M("에이, 그런다고 안 풀려요!", "contempt"))),
                    ColdStare = V(S(M("뭘 봐요! 오늘은 사인 안 해 줘요!", "contempt"))),
                    Up = V(S(M("오! 우리 합이 한 단계 올라간 느낌!", "smile"), M("이거 완전 앵콜 각이에요! 히히힛!", "smiletonque")),
                           S(M("요즘 당신이랑 있으면 박자가 딱딱 맞아요!", "smile")),
                           S(M("라이벌 겸… 음, 그 이상? 히히!", "shy"))),
                    Down = V(S(M("…요즘 좀 박자가 안 맞네요, 우리.", "sad")),
                             S(M("관객석이 자꾸 비어요. …당신 자리요.", "sad")),
                             S(M("흥! 이러다 밴드 해체예요!", "angry"))),
                    Top = S(M("[player]! 이건 제 인생 최고의 무대예요.", "smile"), M("관객도, 라이벌도, 친구도 전부 당신이에요!", "shy")),
                    Bottom = S(M("…기타 줄이 다 끊어진 기분이에요.", "sad"), M("그래도 앵콜 자리는 비워 둘게요. 혹시 모르니까.", "sad")),
                },
                "mila" => new AffLines
                {
                    ColdIdle = new[] { "…말 걸지 마요. 진심이에요.", "책이 더 나아요. 책은 배신 안 하니까.", "흥. 역시 혼자가 편해요.", "…같은 하루가 다시 시작된 기분이에요." },
                    ColdGreeting = V(S(M("…또 왔네요.", "discontent"), M("딱히 반갑지 않아요. 이번엔 진짜로요.", "discontent")),
                                     S(M("바보.", "angry"), M("…설명 안 해도 알죠?", "discontent"))),
                    ColdTouch = V(S(M("손대지 마요. 검 뽑을 거예요. 이번엔 진짜로요.", "contempt")), S(M("…지금은 싫어요.", "contempt")), S(M("그런 걸로 넘어갈 거라 생각했어요? 바보.", "contempt"))),
                    ColdStare = V(S(M("…뭘 봐요. 할 말 없으면 가요.", "contempt"))),
                    Up = V(S(M("…요즘 당신, 조금 나아졌네요.", "shy"), M("딱히 좋아진 건 아니고요! 조금이에요.", "discontent")),
                           S(M("당신 이름, 제 책갈피에 한 줄 더 적었어요. …의미는 묻지 마요.", "shy")),
                           S(M("흥. …같이 있는 거, 싫지 않아요.", "shy"))),
                    Down = V(S(M("…역시 사람은 믿는 게 아니었어요.", "sad")),
                             S(M("당신 요즘 좀 멀어요. …딱히 신경 쓰는 건 아니지만요.", "discontent")),
                             S(M("책갈피를 뺄까 생각 중이에요.", "discontent"))),
                    Top = S(M("…이 이야기의 결말은 제가 정할래요.", "shy"), M("당신이랑 끝까지요. 두 번은 말 안 해요, 바보.", "smile")),
                    Bottom = S(M("…결말이 정해진 책 같아요. 나쁜 쪽으로요.", "sad"), M("그래도 마지막 장은 안 덮을게요. …아직은요.", "sad")),
                },
                "sleepy" => new AffLines
                {
                    ColdIdle = new[] { "…오늘은 그냥 잘래요…", "꿈속이 더 따뜻해요…", "…말할 기운이 없어요…", "커피도 오늘은 안 마실래요…" },
                    ColdGreeting = V(S(M("…아, 오셨어요…", "halfsleep"), M("저 오늘은 좀 더 잘게요… 혼자서요…", "sad")),
                                     S(M("음… [player]님…", "sad"), M("…요즘은 꿈에서도 안 나와 주시네요…", "sad"))),
                    ColdTouch = V(S(M("…지금은 그냥 자게 해 주세요…", "sad")), S(M("…싫어요… 졸려서가 아니라요…", "contempt")), S(M("…나중에요…", "halfsleep"))),
                    ColdStare = V(S(M("…그렇게 보셔도… 오늘은 안 깨요…", "halfsleep"))),
                    Up = V(S(M("요즘… 깨어 있는 게 더 좋아요…", "smile"), M("당신 덕분이에요…", "shy")),
                           S(M("꿈보다 당신이 더 좋아졌어요… 신기하죠…", "shy")),
                           S(M("…오늘은 커피 없이도 눈이 떠져요…", "smile"))),
                    Down = V(S(M("…요즘 다시 잠이 늘었어요…", "sad")),
                             S(M("당신이 없으니까… 깨어 있을 이유가 줄었어요…", "sad")),
                             S(M("…요즘은 꿈이 더 따뜻한 것 같아요…", "sad"))),
                    Top = S(M("[player]님… 이건 잠꼬대가 아니에요…", "shy"), M("평생… 당신 옆에서 깨어 있고 싶어요…", "smile")),
                    Bottom = S(M("…이대로 계속 자고 싶어요…", "sad"), M("그래도… 문 여는 소리가 나면… 깰게요…", "sad")),
                },
                "short" => new AffLines
                {
                    ColdIdle = new[] { "…오늘 수업은 자습이에요.", "선생님도 가끔은 속상해요.", "체크리스트에 '서운함'이라는 항목이 생겼어요.", "…제 기분은 신생 미타들한테는 비밀이에요." },
                    ColdGreeting = V(S(M("…출석 확인했어요.", "discontent"), M("오늘은 개인 면담이 필요할 것 같네요.", "discontent")),
                                     S(M("[player].", "discontent"), M("요즘 태도 점수가 많이 떨어졌어요.", "sad"))),
                    ColdTouch = V(S(M("수업 중엔 그러면 안 돼요. …오늘은 특히요.", "contempt")), S(M("선생님 기분부터 풀어 줘야죠.", "sad")), S(M("…지금은 거리 두기 시간이에요.", "contempt"))),
                    ColdStare = V(S(M("…칠판 보세요. 저 말고요.", "contempt"))),
                    Up = V(S(M("축하해요! 우리 관계, 한 단계 진급이에요!", "smile"), M("…선생님이 제일 기뻐요.", "shy")),
                           S(M("요즘 [player] 생활 기록부가 칭찬으로 꽉 찼어요.", "smile")),
                           S(M("참 잘했어요 도장, 오늘은 두 개예요!", "smiletonque"))),
                    Down = V(S(M("…성적이 떨어지고 있어요. 우리 사이 성적이요.", "sad")),
                             S(M("보충 수업이 필요하겠어요. 주제는… 우리요.", "discontent")),
                             S(M("선생님도 서운할 줄 알아요.", "sad"))),
                    Top = S(M("[player], 졸업이에요. 학생에서요.", "shy"), M("이제부턴… 제 제일 소중한 사람이에요.", "smile")),
                    Bottom = S(M("…낙제예요. 우리 사이.", "sad"), M("그래도 재시험은 언제든 받아 줄게요. 선생님이니까요.", "sad")),
                },
                "chibi" => new AffLines
                {
                    ColdIdle = new[] { "흥! 플레이어님이랑 안 놀 거예요!", "…언니한테 다 이를 거예요.", "지능 체크 해 봤자 뭐해요. 흥.", "…사탕도 혼자 먹을 거예요." },
                    ColdGreeting = V(S(M("…왜 왔어요!", "angry"), M("오늘은 안내 안 해 줄 거예요! 알아서 해요!", "discontent")),
                                     S(M("흥!", "discontent"), M("…아직 화 안 풀렸어요, 바보!", "angry"))),
                    ColdTouch = V(S(M("손 치워요, 바보!", "contempt")), S(M("흥! 그런다고 용서 안 해 줘요!", "contempt")), S(M("…사과부터 해요!", "contempt"))),
                    ColdStare = V(S(M("뭘 봐요! 바보! 저리 가요!", "contempt"))),
                    Up = V(S(M("플레이어님, 요즘 좀 덜 바보예요!", "smile"), M("…칭찬이에요. 조금!", "shy")),
                           S(M("헤헤, 플레이어님 부하 등급 올려 드릴게요!", "smiletonque")),
                           S(M("요즘 플레이어님이랑 노는 게 제일 재밌어요!", "smile"))),
                    Down = V(S(M("…플레이어님 요즘 바보 등급 올라갔어요.", "discontent")),
                             S(M("흥! 요즘 저랑 안 놀아 주잖아요!", "angry")),
                             S(M("…언니한테 이를까 고민 중이에요.", "sad"))),
                    Top = S(M("플레이어님!", "shy"), M("…평생 제 부하 1호예요. 아니, 제일 좋아하는 사람이에요! 바보!", "shy")),
                    Bottom = S(M("…이제 플레이어님 몰라요.", "sad"), M("그래도… 길 잃으면 부르세요. 안내는 해 줄 거예요.", "sad")),
                },
                _ => new AffLines
                {
                    ColdIdle = new[] { "…오늘은 별로 말하고 싶지 않아요.", "당신이 저한테 어떻게 했는지, 다 기억하고 있어요.", "…그래도 여기 있어 주긴 하네요.", "웃는 거 연습 중이에요. 요즘 잘 안 돼서요." },
                    ColdGreeting = V(S(M("…왔어요?", "discontent"), M("반가운 척은 안 할게요. 오늘은요.", "sad")),
                                     S(M("[player]님.", "smilestrange"), M("저번 일, 아직 용서 안 했어요.", "discontent"))),
                    ColdTouch = V(S(M("…만지지 마요. 지금은요.", "contempt")), S(M("그렇게 쉽게 풀릴 거라고 생각했어요?", "smilestrange")), S(M("…나중에요. 제 기분이 돌아오면요.", "sad"))),
                    ColdStare = V(S(M("…뭘 봐요. 할 말 있으면 사과부터 해요.", "contempt"))),
                    Up = V(S(M("요즘 당신이랑 있으면 기분이 좋아요.", "smile"), M("…이 마음, 조금 더 커져도 되죠?", "shy")),
                           S(M("있잖아요, 우리 사이가 한 칸 가까워진 것 같아요.", "shy")),
                           S(M("헤헤, 오늘따라 당신이 더 좋아요.", "smile"))),
                    Down = V(S(M("…요즘 우리, 조금 멀어진 것 같아요.", "sad"), M("제가 뭘 잘못했어요?", "sad")),
                             S(M("당신이 자꾸 저를 두고 가니까…", "sad"), M("마음에 금이 가는 소리가 나요.", "smilestrange")),
                             S(M("…괜찮아요. 아직은요.", "discontent"))),
                    Top = S(M("[player]님, 이제 알겠어요.", "shy"), M("당신은 제 세계의 전부예요. 게임이 끝나도요.", "smile")),
                    Bottom = S(M("…이제 아무 말도 안 할래요.", "discontent"), M("그래도 이 세계에서 당신을 내보낼 생각은 없어요.", "smilestrange")),
                },
            };
            affCache[id] = a;
            return a;
        }
    }
}
