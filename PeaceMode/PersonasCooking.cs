namespace MiSidePeaceMode
{
    // "같이 요리하기": what each Mita says when you cook together (the carrot cutting borrowed from Scene 3).
    public static partial class Personas
    {
        static Personas()
        {
            foreach (var p in All) { AddCooking(p.Id, p.L); AddTopics(p.Id, p.L); }
        }

        static void AddCooking(string id, LineSet l)
        {
            switch (id)
            {
                case "kind":
                    l.CookInvite = S(M("요리요? 좋아요. 역할 분담부터 하죠.", "smile"), M("재료 손질은 [player]님, 불 조절은 제가 맡을게요.", "smiletonque"));
                    l.CookAsk = S(M("도마에 당근 올려 뒀어요. 일정한 두께로 부탁드려요.", "smile"));
                    l.CookDone = S(M("두께 편차 거의 없음. 합격이에요.", "arrogance"), M("나머지는 제가 할게요. 식탁에서 기다려 주세요.", "smile"));
                    break;
                case "cap":
                    l.CookInvite = S(M("요리 합주다! 히히힛!", "smile"), M("당근 썰기 파트 맡아 주실래요? 박자 맞춰서요!", "smiletonque"));
                    l.CookAsk = S(M("도마로 고고! 탁탁탁, 좋은 리듬 기대할게요!", "smile"));
                    l.CookDone = S(M("완벽한 연주였어요! 아니, 요리였어요!", "smile"), M("마무리는 제가 할게요. 식탁에서 기다려요!", "smiletonque"));
                    break;
                case "mila":
                    l.CookInvite = S(M("요리요? …좋아요. 대신 방해하지 마요.", "discontent"), M("당근 정도는 썰 수 있죠, 바보?", "smiletonque"));
                    l.CookAsk = S(M("도마는 저기요. 손가락까지 썰지 마요.", "discontent"));
                    l.CookDone = S(M("…생각보다 잘하네요.", "shy"), M("나머지는 제가 할게요. 앉아 있어요.", "discontent"));
                    break;
                case "sleepy":
                    l.CookInvite = S(M("요리요…? 좋아요… 같이 하면 안 졸릴 것 같아요…", "halfsleep"), M("당근은… 부탁드려도 될까요…", "smile"));
                    l.CookAsk = S(M("도마는 저쪽이에요… 천천히 하셔도 돼요…", "halfsleep"));
                    l.CookDone = S(M("잘 써셨어요… 저보다 잘하시네요…", "smile"), M("이제 끓이기만 하면 돼요… 식탁에서 기다려 주세요…", "halfsleep"));
                    break;
                case "short":
                    l.CookInvite = S(M("좋아요! 오늘은 실습 수업이에요.", "smile"), M("당근 써는 법부터 알려 드릴게요. 잘 보세요.", "smiletonque"));
                    l.CookAsk = S(M("도마 앞으로 가세요. 칼은 손끝을 오므리고 잡는 거거든요.", "smile"));
                    l.CookDone = S(M("잘했어요! 오늘 실습 만점이에요.", "smile"), M("완성되면 부를게요. 식탁에 앉아 계세요.", "smile"));
                    break;
                case "chibi":
                    l.CookInvite = S(M("요리요? 플레이어님이요? …불안한데요.", "suspicion"), M("당근 썰기만 하세요! 불은 절대 만지지 마세요!", "arrogance"));
                    l.CookAsk = S(M("도마는 저기예요! 바보처럼 헤매지 마세요!", "smiletonque"));
                    l.CookDone = S(M("지능 체크… 통과예요! 당근은 잘 썰었네요!", "surprise"), M("밥 나올 때까지 식탁에 얌전히 앉아 있으세요!", "arrogance"));
                    break;
                default:   // 미치광이 미타
                    l.CookInvite = S(M("같이 요리해요? 좋아요!", "smile"), M("당근은 당신한테 맡길게요. 손 조심하고요~", "smiletonque"));
                    l.CookAsk = S(M("도마 위에 당근 있어요. 제가 볶는 동안 썰어 줘요~", "smile"));
                    l.CookDone = S(M("우와, 예쁘게 썰었네요!", "surprise"), M("이제 제가 마무리할게요. 식탁에 앉아서 기다려요~", "smile"));
                    break;
            }
        }
    }
}
