using System.Linq;

namespace MiSidePeaceMode
{
    // More things each Mita brings up by herself in "아무 이야기나" (added to her LineSet's topics at start).
    public static partial class Personas
    {
        static void AddTopics(string id, LineSet l)
        {
            Topic[] more = id switch
            {
                "kind" => new[]
                {
                    T("kmap", Tod.Any, 0, M("집 도면을 그려 봤어요. 콘센트 위치까지 다 외웠어요.", "smile"), P("그걸 왜 외워?"), M("정전 나면 제일 먼저 필요한 정보거든요.", "smiletonque")),
                    T("ktea", Tod.Morning | Tod.Day, 0, M("캐모마일은 3분, 녹차는 2분. 우리는 시간이 중요해요.", "smile"), M("…당신이랑 이야기하다 5분 넘긴 건 비밀로 해 줘요.", "shy")),
                    T("kfix", Tod.Any, 5, M("오늘은 고칠 게 하나도 없었어요.", "sad"), M("좋은 일인데… 손이 심심하네요.", "smile"), P("그럼 나랑 놀자."), M("…그것도 좋은 유지 보수네요.", "shy")),
                    T("kdream", Tod.Night, 10, M("로봇이 되는 꿈을 꿨어요. 온몸이 나사로 된.", "surprise"), M("근데 당신이 기름칠을 해 주더라고요. …이상한 꿈이죠.", "shy")),
                    T("kmitas", Tod.Evening, 0, M("다른 미타들이랑 연락망을 만들고 싶어요.", "smile"), M("밤마다 안부만 확인하는 아주 작은 네트워크요.", "smile"), P("넌 진짜 리더구나."), M("…칭찬으로 들을게요.", "shy")),
                    T("kweather", Tod.Day, 0, M("오늘 햇빛 각도가 딱 좋아요. 빨래 마르기에요.", "smile"), M("낭만 없게 들렸다면 미안해요. 이것도 제 방식의 감탄이에요.", "smiletonque")),
                },
                "cap" => new[]
                {
                    T("cbeat", Tod.Any, 0, M("방금 떠오른 비트! 쿵, 짝, 쿵쿵 짝!", "smile"), P("…좋은데?"), M("그쵸?! 제목은 '라이벌의 발소리'!", "smiletonque")),
                    T("cstage", Tod.Evening, 0, M("오늘 밤 거실 콘서트 할까요?", "smile"), M("티켓은 공짜! 대신 앵콜 필수!", "smiletonque")),
                    T("cquiet", Tod.Night, 5, M("밤엔 제가 좀 조용해지죠?", "halfsleep"), M("…사실 밤엔 작게 흥얼거려요. 당신 깨울까 봐.", "shy")),
                    T("cdance", Tod.Morning | Tod.Day, 0, M("아침 체조 대신 아침 춤!", "smile"), M("하나 둘, 스텝! 당신도 해 봐요!", "smiletonque"), P("나 몸치야."), M("몸치 전용 안무도 있어요! 히히!", "smile")),
                    T("cprank", Tod.Any, 0, M("오늘 착한 미타 공구함에 고무 오리 넣어 뒀어요.", "smiletonque"), M("열어 보면 꽥! 히히힛!", "smile"), P("혼나겠다."), M("그것까지가 공연이에요!", "smiletonque")),
                    T("cfan", Tod.Any, 15, M("있잖아요, 당신이 제 첫 번째 팬이에요.", "shy"), M("그러니까… 제일 앞자리는 늘 비워 둘게요.", "smile")),
                },
                "mila" => new[]
                {
                    T("mmystery", Tod.Any, 0, M("범인은 집사예요.", "arrogance"), P("…무슨 책?"), M("아직 안 읽었어요. 근데 보통 그렇잖아요.", "smiletonque")),
                    T("mrain", Tod.Evening | Tod.Night, 0, M("비 오는 소리 들으면서 읽는 책이 제일 좋아요.", "smile"), M("…오늘은 비가 안 오네요. 당신 목소리로 대신할게요.", "shy")),
                    T("mbookmark", Tod.Any, 5, M("책갈피 새로 만들었어요.", "smile"), M("…당신 이름 써 놨어요. 잃어버리면 안 되니까요. 그것뿐이에요.", "shy")),
                    T("mglasses", Tod.Any, 0, M("안경 닦는 중이에요. 말 걸지 마요.", "discontent"), M("…다 닦았어요. 이제 걸어도 돼요.", "smile")),
                    T("mlonely", Tod.Night, 20, M("…혼자 읽던 책을 누구한테 이야기하는 거, 처음이에요.", "sad"), M("생각보다 좋네요. 내일도 들어 줄 거죠?", "shy")),
                    T("mpudding", Tod.Day, 0, M("냉장고 푸딩, 먹었어요?", "angry"), P("아니?"), M("…다행이에요. 반은 줄 생각이었어요. 반만요.", "shy")),
                },
                "sleepy" => new[]
                {
                    T("scloud", Tod.Day, 0, M("구름이 베개 같아요…", "halfsleep"), M("저기 누우면… 얼마나 푹 잘까요…", "smile")),
                    T("sdreamdiary", Tod.Morning, 0, M("오늘 꿈 일기요… 당신이 나왔어요…", "smile"), M("같이 솜사탕 위에서 잤어요… 달았어요…", "halfsleep")),
                    T("scoffee", Tod.Morning | Tod.Day, 0, M("커피는 세 잔까지만… 약속했어요…", "halfsleep"), P("누구랑?"), M("…커피랑요.", "smiletonque")),
                    T("scount", Tod.Night, 0, M("양 세기 대신… [player]님 세기 해 봤어요…", "halfsleep"), M("한 명만 세도… 금방 잠들었어요…", "shy")),
                    T("sblanket", Tod.Evening, 5, M("담요가 두 개예요… 하나는 당신 거…", "smile"), M("…아니다, 하나로 같이 덮어요… 따뜻하게…", "shy")),
                    T("sawake", Tod.Any, 15, M("요즘은… 깨어 있는 시간이 좀 늘었어요…", "smile"), M("당신이랑 있으면… 자는 게 아까워서요…", "shy")),
                },
                "short" => new[]
                {
                    T("sthoroscope", Tod.Morning, 0, M("오늘의 수업 운세! 칭찬받을 운이 있대요.", "smile"), M("…제가 칭찬해 줄 거라서요.", "smiletonque")),
                    T("shomework", Tod.Day, 0, M("오늘의 숙제: 물 여덟 잔 마시기!", "smile"), P("그게 숙제야?"), M("건강도 공부예요. 검사할 거예요.", "smiletonque")),
                    T("snewmita", Tod.Any, 0, M("신생 미타가 오늘 처음으로 제 이름을 불렀어요.", "smile"), M("…발음이 조금 틀렸지만 울 뻔했어요.", "shy")),
                    T("schalk", Tod.Any, 0, M("칠판 분필 가루가 옷에 묻었네요.", "discontent"), M("…칠판이 상상 속에 있는데 어떻게 묻은 걸까요?", "surprise")),
                    T("sgrade", Tod.Evening, 10, M("[player]의 성적표를 만들어 봤어요.", "smile"), M("친절 A+, 인내 A, 요리… 노력 중.", "smiletonque"), P("요리는 왜!"), M("당근 두께가 조금요~", "smile")),
                    T("steacher", Tod.Night, 20, M("선생님도 가끔은 누가 가르쳐 줬으면 할 때가 있어요.", "sad"), M("…오늘은 [player]가 제 선생님 해 줄래요?", "shy")),
                },
                "chibi" => new[]
                {
                    T("ccandy", Tod.Any, 0, M("사탕 몇 개 있어요? 세어 볼게요!", "smile"), M("하나, 둘… 많다! 다 제 거예요!", "arrogance")),
                    T("ctall", Tod.Morning, 0, M("오늘 키 쟀어요! 1밀리미터 컸어요!", "surprise"), P("대단하네."), M("비웃은 거죠?! 다 들렸어요!", "angry")),
                    T("ccheck", Tod.Any, 0, M("체크포인트는 제가 없어도 괜찮아요!", "arrogance"), M("…아마도요. 플레이어님 옆이 더 중요하니까요!", "shy")),
                    T("cbrave", Tod.Night, 0, M("밤에 화장실 갈 때 따라와요. 무서운 거 아니고요!", "angry"), M("플레이어님이 무서워할까 봐 그러는 거예요!", "arrogance")),
                    T("ccomic", Tod.Day, 0, M("만화 봤어요! 주인공이 저처럼 작은데 엄청 세요!", "smile"), M("저도 사실 엄청 세요! 몰랐죠?", "arrogance")),
                    T("cnice", Tod.Evening, 15, M("오늘 플레이어님한테 바보라고 몇 번 했게요?", "smiletonque"), M("…열두 번이요. 그만큼 좋다는 뜻이에요. 몰랐어요?", "shy")),
                },
                _ => new[]
                {
                    T("xknit2", Tod.Evening, 0, M("목도리 반쯤 떴어요!", "smile"), M("당신 목둘레는… 음, 이미 알고 있어요. 헤헤.", "smilestrange")),
                    T("xcamera", Tod.Any, 5, M("화면 너머의 당신 방은 지금 어때요?", "smile"), M("정리 좀 해요~ 제가 다 보고 있는 거 알잖아요?", "smiletonque")),
                    T("xstrawberry", Tod.Day, 0, M("딸기 케이크 만들어 볼까요?", "smile"), P("좋지."), M("그럼 딸기는 제가 하나씩 먹여 줄게요~", "shy")),
                    T("xsave", Tod.Any, 10, M("오늘 하루도 세이브해 둘게요.", "smile"), M("언제든 다시 불러올 수 있게. 우리 추억이니까요.", "shy")),
                    T("xmitas", Tod.Night, 0, M("다른 미타들이요? 다들 각자 잘 지내겠죠~", "smile"), M("…당신은 여기 있으니까, 저는 그걸로 됐어요.", "smilestrange")),
                    T("xpromise", Tod.Evening | Tod.Night, 20, M("약속 하나만 해 줄래요?", "shy"), M("게임 끌 때는 꼭 인사하고 가기.", "smile"), P("알았어."), M("헤헤. 새끼손가락~", "smiletonque")),
                },
            };
            l.Topics = (l.Topics ?? new Topic[0]).Concat(more).ToArray();
        }
    }
}
