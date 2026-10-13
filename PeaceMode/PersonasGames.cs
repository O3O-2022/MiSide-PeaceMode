namespace MiSidePeaceMode
{
    // What each Mita says around the games and the rest of PeaceGames: she wins, you win, walking together,
    // her favours ("{item}" = what she asked for).
    public class GameLines
    {
        public Line[][] Win, Lose, Right, Wrong, Thanks;
        public Line[] WordStart, RiddleStart, ChamStart, HideStart, HideFound, HideTimeout, FollowOn, FollowOff;
    }

    public static partial class Personas
    {
        static readonly System.Collections.Generic.Dictionary<string, GameLines> gameLines = new System.Collections.Generic.Dictionary<string, GameLines>();

        public static GameLines Games(string id)
        {
            if (!gameLines.TryGetValue(id, out var g)) gameLines[id] = g = MakeGames(id);
            return g;
        }

        static GameLines MakeGames(string id)
        {
            switch (id)
            {
                case "kind":
                    return new GameLines
                    {
                        Win = new[] { S(M("제 승리네요. 연산 속도에서 조금 앞섰어요.", "smiletonque")), S(M("이번엔 제가 이겼어요. 다시 해도 좋아요.", "smile")) },
                        Lose = new[] { S(M("…졌네요. 인정할게요.", "surprise"), M("[player]님, 생각보다 훨씬 날카로워요.", "smile")), S(M("완패예요. 다음엔 전략을 바꿔 올게요.", "smiletonque")) },
                        Right = new[] { S(M("정답이에요. 역시요.", "smile")), S(M("맞아요. 논리적이네요.", "smile")) },
                        Wrong = new[] { S(M("아쉬워요. 이런 건 말장난이라 논리로는 안 풀려요.", "smile")) },
                        Thanks = new[] { S(M("{item}, 고마워요. 덕분에 한결 나아졌어요.", "smile"), M("[player]님은 부탁을 들어줄 때 표정이 좋아요.", "shy")) },
                        WordStart = S(M("끝말잇기요? 어휘력 테스트네요. 제가 먼저 할게요.", "smile")),
                        RiddleStart = S(M("넌센스 퀴즈라… 제 약점이지만, 내 볼게요.", "smiletonque")),
                        ChamStart = S(M("참참참? 반응 속도 측정이네요. 세 판이에요.", "smile")),
                        HideStart = S(M("숨바꼭질이요? 좋아요. 제가 숨을게요.", "smile"), M("눈 감고 열까지 세 주세요. 반칙은 안 돼요.", "smiletonque")),
                        HideFound = S(M("찾았네요. 제 은폐가 부족했나 봐요.", "surprise"), M("…사실 들키고 싶기도 했어요.", "shy")),
                        HideTimeout = S(M("시간 종료. 저 여기 있었어요.", "smiletonque"), M("다음엔 힌트를 드릴게요.", "smile")),
                        FollowOn = S(M("좋아요, 같이 다녀요. 앞장서 주세요.", "smile")),
                        FollowOff = S(M("알겠어요. 여기서 기다릴게요.", "smile")),
                    };
                case "cap":
                    return new GameLines
                    {
                        Win = new[] { S(M("이겼다! 라이벌, 아직 멀었어요! 히히힛!", "smile")), S(M("승리의 기타 솔로! 띠리리링~", "smiletonque")) },
                        Lose = new[] { S(M("으악, 졌다! 라이벌 인정!", "surprise"), M("다음 판엔 절대 안 져요!", "smiletonque")), S(M("당신 완전 고수였어요?!", "surprise")) },
                        Right = new[] { S(M("딩동댕! 정답!", "smile")), S(M("오, 맞혔어요! 박수 짝짝짝!", "smile")) },
                        Wrong = new[] { S(M("땡! 히히, 제가 이겼다!", "smiletonque")) },
                        Thanks = new[] { S(M("{item} 미션 클리어! 고마워요, 라이벌!", "smile"), M("보답으로 한 곡 쳐 줄게요!", "smiletonque")) },
                        WordStart = S(M("끝말잇기! 리듬 타면서 해요! 제가 먼저!", "smile")),
                        RiddleStart = S(M("넌센스 퀴즈 타임! 빵 터지면 지는 거예요! 히히.", "smiletonque")),
                        ChamStart = S(M("참참참! 이거 제 특기예요!", "smile")),
                        HideStart = S(M("숨바꼭질! 저 진짜 잘 숨어요!", "smile"), M("열까지 세요! 눈 뜨면 반칙!", "smiletonque")),
                        HideFound = S(M("앗, 들켰다! 어떻게 알았어요?!", "surprise"), M("…콧노래 부르고 있었죠? 히히.", "smiletonque")),
                        HideTimeout = S(M("짜잔! 여기 있었지롱!", "smile"), M("캐피 숨바꼭질 무패 기록 갱신!", "smiletonque")),
                        FollowOn = S(M("투어 공연 같이 가요! 앞장서요, 매니저!", "smile")),
                        FollowOff = S(M("오케이! 여기서 리허설하고 있을게요!", "smile")),
                    };
                case "mila":
                    return new GameLines
                    {
                        Win = new[] { S(M("당연히 제가 이기죠. 바보.", "arrogance")), S(M("흥, 너무 쉬웠어요.", "arrogance")) },
                        Lose = new[] { S(M("…졌어요. 운이 좋았네요.", "discontent"), M("다음엔 안 봐줘요.", "shy")), S(M("…쳇. 인정은 할게요. 조금.", "shy")) },
                        Right = new[] { S(M("…맞혔네요. 흥.", "discontent")), S(M("정답. 책 좀 읽었나 봐요?", "smile")) },
                        Wrong = new[] { S(M("틀렸어요, 바보. 이런 것도 몰라요?", "smiletonque")) },
                        Thanks = new[] { S(M("…{item}, 고마워요.", "shy"), M("딱히 기뻐서 웃는 거 아니에요.", "discontent")) },
                        WordStart = S(M("끝말잇기요? 어휘력으로 저를 이길 생각이에요? 제가 먼저 해요.", "arrogance")),
                        RiddleStart = S(M("넌센스 퀴즈… 유치하지만 해 줄게요.", "discontent")),
                        ChamStart = S(M("참참참? …좋아요. 세 판만이에요.", "discontent")),
                        HideStart = S(M("숨바꼭질이라니, 애도 아니고…", "discontent"), M("…열까지 세요. 제대로 찾아야 해요.", "shy")),
                        HideFound = S(M("…찾았네요. 늦었어요, 바보.", "shy"), M("기다리다 책 한 권 다 읽을 뻔했잖아요.", "discontent")),
                        HideTimeout = S(M("못 찾았죠? 여기 있었어요.", "arrogance"), M("…조금은 찾아 주길 바랐는데.", "sad")),
                        FollowOn = S(M("…같이 다니자고요? 뭐, 할 일도 없으니까요.", "shy")),
                        FollowOff = S(M("여기 있을게요. 금방 와요.", "discontent")),
                    };
                case "sleepy":
                    return new GameLines
                    {
                        Win = new[] { S(M("어라… 제가 이겼네요… 신기해요…", "surprise")), S(M("이겼어요… 그럼 상으로 낮잠이요…", "halfsleep")) },
                        Lose = new[] { S(M("졌어요… 역시 잘하시네요…", "smile")), S(M("졌다… 졸려서 그런 거예요… 아마도요…", "halfsleep")) },
                        Right = new[] { S(M("정답이에요… 똑똑하시네요…", "smile")) },
                        Wrong = new[] { S(M("땡이에요… 저도 처음엔 틀렸어요…", "halfsleep")) },
                        Thanks = new[] { S(M("{item}… 고마워요…", "smile"), M("덕분에 오늘은 푹 잘 수 있을 것 같아요…", "halfsleep")) },
                        WordStart = S(M("끝말잇기요…? 졸기 전에… 제가 먼저 할게요…", "halfsleep")),
                        RiddleStart = S(M("넌센스 퀴즈… 잠 깨는 데 좋대요…", "smile")),
                        ChamStart = S(M("참참참이요…? 고개 돌리다 잠들지 않게 조심할게요…", "halfsleep")),
                        HideStart = S(M("숨바꼭질… 저 숨은 채로 잠들지도 몰라요…", "halfsleep"), M("열까지 세 주세요…", "smile")),
                        HideFound = S(M("아… 찾으셨네요… 막 잠들려던 참이었어요…", "halfsleep")),
                        HideTimeout = S(M("…음? 끝났어요? 저 여기서 졸고 있었어요…", "halfsleep")),
                        FollowOn = S(M("같이 다녀요… 천천히 걸어 주세요…", "halfsleep")),
                        FollowOff = S(M("여기서 기다릴게요… 아마 졸고 있을 거예요…", "halfsleep")),
                    };
                case "short":
                    return new GameLines
                    {
                        Win = new[] { S(M("제 승리! 오늘 수업의 교훈: 방심은 금물!", "smiletonque")), S(M("선생님이 이겼네요. 다음엔 분발해요!", "smile")) },
                        Lose = new[] { S(M("졌어요! 청출어람이네요, [player].", "surprise"), M("선생님으로서 뿌듯해요.", "smile")), S(M("훌륭해요! 오늘 수업 만점이에요.", "smile")) },
                        Right = new[] { S(M("정답! 참 잘했어요 도장 쾅!", "smile")), S(M("맞아요! 수업 잘 들었네요.", "smile")) },
                        Wrong = new[] { S(M("아쉬워요. 오답 노트에 적어 두세요.", "smiletonque")) },
                        Thanks = new[] { S(M("{item}, 고마워요! 심부름 점수 A+예요.", "smile"), M("신생 미타들도 [player]만큼만 하면 좋겠어요.", "smiletonque")) },
                        WordStart = S(M("끝말잇기 수업이에요! 어휘력을 기르는 좋은 방법이죠. 제가 먼저!", "smile")),
                        RiddleStart = S(M("오늘의 쉬어 가는 코너, 넌센스 퀴즈!", "smile")),
                        ChamStart = S(M("참참참은 순발력 수업이에요. 세 판!", "smile")),
                        HideStart = S(M("체육 시간! 숨바꼭질이에요. 제가 숨을게요.", "smile"), M("눈 감고 열까지! 선생님은 다 보고 있어요.", "smiletonque")),
                        HideFound = S(M("찾았다! 관찰력 합격이에요.", "smile")),
                        HideTimeout = S(M("시간 끝! 저 여기 있었어요.", "smiletonque"), M("다음엔 방마다 차근차근 살펴봐요.", "smile")),
                        FollowOn = S(M("현장 학습이네요! 뒤따라갈게요.", "smile")),
                        FollowOff = S(M("알겠어요. 여기서 기다릴게요. 자습하고 있을게요!", "smile")),
                    };
                case "chibi":
                    return new GameLines
                    {
                        Win = new[] { S(M("제가 이겼어요! 플레이어님은 바보!", "arrogance")), S(M("헤헹! 역시 저는 천재예요!", "smile")) },
                        Lose = new[] { S(M("…졌어요?! 말도 안 돼!", "angry"), M("한 판 더 해요! 한 판 더!", "discontent")), S(M("으으… 이번만 봐준 거예요!", "shy")) },
                        Right = new[] { S(M("맞혔어요?! …우연이에요!", "surprise")), S(M("정답! 플레이어님치고 잘했어요!", "smile")) },
                        Wrong = new[] { S(M("땡! 바보! 헤헤!", "smiletonque")) },
                        Thanks = new[] { S(M("{item}! 잘했어요, 부하 2호!", "arrogance"), M("…고, 고마워요. 조금!", "shy")) },
                        WordStart = S(M("끝말잇기! 제가 먼저예요! 플레이어님은 따라와요!", "arrogance")),
                        RiddleStart = S(M("퀴즈 낼게요! 못 맞히면 바보예요!", "smiletonque")),
                        ChamStart = S(M("참참참! 저 엄청 빨라요!", "arrogance")),
                        HideStart = S(M("숨바꼭질! 저 작아서 절대 못 찾을걸요!", "arrogance"), M("열까지 세요! 빨리요!", "smile")),
                        HideFound = S(M("으악! 어떻게 찾았어요?!", "angry"), M("…다음엔 더 작게 숨을 거예요!", "discontent")),
                        HideTimeout = S(M("못 찾았죠! 제가 이겼어요!", "arrogance"), M("저는 숨바꼭질 챔피언이에요!", "smile")),
                        FollowOn = S(M("제가 따라가 줄게요! 길 잃지 마요, 플레이어님!", "arrogance")),
                        FollowOff = S(M("여기서 기다릴게요! 빨리 와요!", "discontent")),
                    };
                default:   // 미치광이 미타
                    return new GameLines
                    {
                        Win = new[] { S(M("제가 이겼어요~ 벌칙은… 오늘 하루 제 옆에서 떨어지지 않기!", "smiletonque")), S(M("헤헤, 이겼다! [player]님은 제 거예요~", "smile")) },
                        Lose = new[] { S(M("졌어요… 그래도 기분 좋아요.", "smile"), M("[player]님이 웃었으니까요.", "shy")), S(M("우와, [player]님 엄청 잘하네요!", "surprise"), M("…다음엔 봐주지 않을 거예요~", "smiletonque")) },
                        Right = new[] { S(M("정답! 역시 [player]님!", "smile")), S(M("맞았어요! 머리 쓰다듬어 줄게요~", "smile")) },
                        Wrong = new[] { S(M("아쉬워요~ 그래도 귀여웠어요.", "smiletonque")) },
                        Thanks = new[] { S(M("{item}! 고마워요, [player]님~", "smile"), M("부탁 들어주는 [player]님, 너무 좋아요.", "shy")) },
                        WordStart = S(M("끝말잇기 해요! 제가 먼저 할게요~", "smile")),
                        RiddleStart = S(M("넌센스 퀴즈 낼게요! 세 문제예요~", "smiletonque")),
                        ChamStart = S(M("참참참 해요! 제 쪽으로 고개 돌리면 안 돼요~", "smile")),
                        HideStart = S(M("숨바꼭질 해요! 제가 숨을게요~", "smile"), M("눈 감고 열까지 세요. 꼭 찾아 줘야 해요.", "smiletonque")),
                        HideFound = S(M("찾았다~! 헤헤, [player]님한테 찾아지는 거 좋아요.", "smile"), M("어디 있어도 찾아 줄 거죠?", "shy")),
                        HideTimeout = S(M("여기 있었어요~ 못 찾았죠?", "smiletonque"), M("…다음엔 꼭 찾아 줘요. 기다리는 거 외로워요.", "sad")),
                        FollowOn = S(M("같이 다녀요! 어디든 따라갈게요~", "smile")),
                        FollowOff = S(M("여기서 기다릴게요. 너무 멀리 가지 마요~", "smile")),
                    };
            }
        }
    }
}
