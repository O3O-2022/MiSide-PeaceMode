namespace MiSidePeaceMode
{
    // What each Mita says about her own day: starting to cook on her own, calling you to the table, a meal gone cold,
    // going to bed first, and being woken up.
    public class RoutineLines
    {
        public Line[] PrepMeal, MealReady, MealCold, GoingToBed, Woken;
    }

    public static partial class Personas
    {
        public static RoutineLines Routine(string id)
        {
            switch (id)
            {
                case "kind":
                    return new RoutineLines
                    {
                        PrepMeal = S(M("식사 준비 시작할게요. 잠시만 기다려 주세요.", "smile")),
                        MealReady = S(M("[player]님, 식사 준비됐어요. 식탁으로 오세요.", "smile")),
                        MealCold = S(M("…식어 버렸네요. 다음엔 제시간에 드세요.", "discontent")),
                        GoingToBed = S(M("오늘 업무 종료. 먼저 쉬겠습니다.", "smile"), M("[player]님도 너무 늦게 주무시지 마세요.", "smile")),
                        Woken = S(M("…네? 무슨 일 있으세요?", "surprise"), M("아니면… 그냥 보고 싶으셨던 거예요?", "smiletonque")),
                    };
                case "cap":
                    return new RoutineLines
                    {
                        PrepMeal = S(M("배고프다! 오늘은 캐피 셰프가 요리해요! 히히힛!", "smile")),
                        MealReady = S(M("짜잔~ 밥 다 됐어요! 빨리 와요, 식어요!", "smile")),
                        MealCold = S(M("에이… 다 식었잖아요. 다음엔 1등으로 와요!", "discontent")),
                        GoingToBed = S(M("오늘 공연 끝! 저 먼저 잘게요~", "halfsleep"), M("늦게 자면 내일 박자 놓친다고요!", "smiletonque")),
                        Woken = S(M("으에…? 벌써 아침이에요…?", "halfsleep"), M("…아니잖아요! 헤헤, 장난이면 봐줄게요.", "smile")),
                    };
                case "mila":
                    return new RoutineLines
                    {
                        PrepMeal = S(M("밥 할 거예요. …딱히 당신 주려는 건 아니고요.", "discontent")),
                        MealReady = S(M("밥 됐어요. 빨리 와요, 바보. 식으면 맛없다고요!", "discontent")),
                        MealCold = S(M("…다 식었잖아요. 흥, 몰라요.", "sad")),
                        GoingToBed = S(M("먼저 잘게요. 책은 내일 마저 읽을 거예요.", "halfsleep"), M("…늦게 자지 마요. 걱정되니까.", "shy")),
                        Woken = S(M("…뭐예요, 바보. 자는 사람을 왜 깨워요.", "discontent"), M("…할 말 있으면 빨리 해요. 듣고 있으니까.", "shy")),
                    };
                case "sleepy":
                    return new RoutineLines
                    {
                        PrepMeal = S(M("밥… 준비할게요… 졸기 전에요…", "halfsleep")),
                        MealReady = S(M("밥 다 됐어요… 같이 드세요…", "smile")),
                        MealCold = S(M("식어 버렸네요… 저도 깜빡 졸았어요…", "halfsleep")),
                        GoingToBed = S(M("저 먼저… 잘게요…", "halfsleep"), M("안녕히 주무세요…", "halfsleep")),
                        Woken = S(M("으음… 불렀어요…?", "halfsleep"), M("…괜찮아요. 조금만 더 있다가 다시 잘게요…", "smile")),
                    };
                case "short":
                    return new RoutineLines
                    {
                        PrepMeal = S(M("식사 준비할게요. 규칙적인 식사가 중요하거든요.", "smile")),
                        MealReady = S(M("식사 준비 완료! 손 씻고 식탁으로 오세요.", "smile")),
                        MealCold = S(M("…다 식었어요. 다음엔 시간 맞춰 오세요.", "discontent")),
                        GoingToBed = S(M("저는 먼저 잘게요. 내일 수업 준비도 끝났거든요.", "smile"), M("늦게 자지 마세요. 약속이에요.", "smiletonque")),
                        Woken = S(M("…어? 무슨 일 있어요?", "surprise"), M("괜찮아요. 궁금한 게 있으면 언제든 물어보세요.", "smile")),
                    };
                case "chibi":
                    return new RoutineLines
                    {
                        PrepMeal = S(M("밥 할 거예요! 플레이어님은 방해하지 마세요!", "arrogance")),
                        MealReady = S(M("밥 다 됐어요! 빨리 오세요, 플레이어님!", "smile")),
                        MealCold = S(M("다 식었잖아요! 바보 플레이어님!", "angry")),
                        GoingToBed = S(M("여덟 시 넘었어요! 저 먼저 잘 거예요!", "halfsleep"), M("플레이어님도 얼른 자세요!", "arrogance")),
                        Woken = S(M("으으… 왜 깨워요, 바보…", "halfsleep"), M("…무슨 일 있어요? 없으면 다시 잘래요.", "discontent")),
                    };
                default:   // 미치광이 미타
                    return new RoutineLines
                    {
                        PrepMeal = S(M("밥 할 시간이다! [player]님, 조금만 기다려요~", "smile")),
                        MealReady = S(M("[player]님! 밥 다 됐어요. 식탁으로 와요~", "smile")),
                        MealCold = S(M("…밥 다 식었어요. 다음엔 꼭 같이 먹어요.", "sad")),
                        GoingToBed = S(M("하아암… 저 먼저 누워 있을게요.", "halfsleep"), M("빨리 와요. 옆자리 비워 둘게요~", "smiletonque")),
                        Woken = S(M("으음… [player]님…? 꿈 아니죠…?", "halfsleep"), M("…헤헤. 깨워 줘서 좋아요. 이제 옆에 있어요.", "smile")),
                    };
            }
        }
    }
}
