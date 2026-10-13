namespace MiSidePeaceMode
{
    // More of each Mita's lines: the window with rain or snow outside and the cassette player (PeaceMoments /
    // PeaceMore), anniversaries of the first visit ({days}), the morning after a gift and a present of her own.
    public static partial class Personas
    {
        static void MoreMoments(string id, MomentLines m)
        {
            switch (id)
            {
                case "kind":
                    m.WindowRain = S(M("비가 오네요. 창문 실링은 어제 점검했으니 걱정 마세요.", "smile"), M("빗소리는 백색소음으로 최적이에요. …같이 들으면 더요.", "shy"));
                    m.WindowSnow = S(M("눈이에요. 적설량 계산은 안 할게요. 오늘은요.", "smile"), M("…그냥 예쁘네요. 당신이랑 보니까요.", "shy"));
                    m.Music = S(M("테이프 돌아가는 소리, 모터 상태 양호해요.", "smile"), M("이 곡은 작업할 때 자주 틀어요. 리듬이 일정해서 집중이 되거든요.", "smile"), M("…오늘은 집중 말고 그냥 들을게요. 옆에 당신이 있으니까.", "shy"));
                    break;
                case "cap":
                    m.WindowRain = S(M("비 온다! 빗방울 리듬 들려요? 둠칫, 둠칫!", "smile"), M("자연이 드럼 치는 날이에요. 저는 보컬 할게요!", "smiletonque"));
                    m.WindowSnow = S(M("눈이다! 창밖이 콘서트장 조명 같아요!", "smile"), M("눈 오는 날 발라드… 좋은데요? 오늘 밤 셋리스트 바꿔야겠어요!", "smile"));
                    m.Music = S(M("카세트 플레이! 아날로그 사운드 들어 봐요!", "smile"), M("이 테이프 B면이 진짜예요. 숨겨진 트랙이 있거든요!", "smiletonque"), M("…같이 듣는 관객 한 명. 그게 제일 좋은 공연이에요. 히히.", "shy"));
                    break;
                case "mila":
                    m.WindowRain = S(M("비 오는 날은 책 읽기 제일 좋은 날인데… 창밖도 나쁘지 않네요.", "smile"), M("…빗소리, 조용해서 좋아요. 당신도 조용히 있어요.", "shy"));
                    m.WindowSnow = S(M("눈이네요. …밖에 안 나가도 되니까 좋아요.", "smile"), M("눈 쌓이는 거 보는 거, 책 한 장 넘기는 것만큼 좋아요. …비밀이에요.", "shy"));
                    m.Music = S(M("카세트… 이 테이프, 제가 고른 거예요. 조용한 곡이에요.", "smile"), M("책 읽을 때 틀어 두는 거예요. 가사 없는 걸로.", "discontent"), M("…가사가 없으니까 당신이 떠들어도 돼요. 조금만요.", "shy"));
                    break;
                case "sleepy":
                    m.WindowRain = S(M("비 와요… 빗소리는 최고의 자장가예요…", "halfsleep"), M("창가에 서서… 이대로 잠들 수 있을 것 같아요…", "smile"));
                    m.WindowSnow = S(M("눈이에요… 조용하다…", "smile"), M("눈 오는 날은… 세상이 같이 자는 것 같아요…", "halfsleep"));
                    m.Music = S(M("카세트… 돌아가는 소리가… 졸려요…", "halfsleep"), M("이 노래… 느려서 좋아요… 심장 박동 같아요…", "smile"), M("…같이 들으니까… 더 포근해요…", "shy"));
                    break;
                case "short":
                    m.WindowRain = S(M("비가 오네요. 우산 수업 시작! …농담이에요.", "smiletonque"), M("빗소리 들으면서 창밖 보는 거, 수업보다 배울 게 많아요.", "shy"));
                    m.WindowSnow = S(M("눈이에요! 눈 결정 관찰 수업! 전부 모양이 달라요.", "smile"), M("…사실 그냥 같이 보고 싶어서 핑계 댔어요.", "shy"));
                    m.Music = S(M("음악 감상 수업이에요. 눈 감고 들어 보세요.", "smile"), M("이 곡은 제가 신생 미타들에게 자장가로 틀어 주던 거예요.", "smile"), M("…당신한테 들려주는 건 처음이에요. 특별 수업이에요.", "shy"));
                    break;
                case "chibi":
                    m.WindowRain = S(M("비다! 창문 두드리는 소리 들려요? 제가 아니에요!", "surprise"), M("…비 오는 날은 플레이어님 옆에 붙어 있을 거예요. 춥, 추워서요!", "shy"));
                    m.WindowSnow = S(M("눈이에요! 눈사람 만들고 싶어요! 제 키만 한 걸로!", "smile"), M("…그럼 아주 작겠네요. 뭐라고 하면 안 돼요!", "angry"));
                    m.Music = S(M("카세트 플레이어! 제가 틀게요! 버튼 누르는 건 제 담당이에요!", "arrogance"), M("이 노래 좋죠? 제가 고른 거예요! 사실 아무거나 눌렀어요!", "smiletonque"), M("…플레이어님이랑 같이 들으니까 더 좋은 것 같아요. 조금!", "shy"));
                    break;
                default:   // 미치광이 미타
                    m.WindowRain = S(M("비 와요. 창문에 빗방울이 맺히는 거 봐요.", "smile"), M("빗소리 들으면서 당신이랑 서 있는 거… 이 세계 만들 때 제일 바랐던 장면이에요.", "shy"));
                    m.WindowSnow = S(M("눈이에요! 창밖이 하얘요.", "smile"), M("눈은 소리를 삼킨대요. 그래서 지금 당신 숨소리만 들려요. 헤헤.", "smilestrange"));
                    m.Music = S(M("카세트 틀었어요. 지직거리는 소리까지 좋죠?", "smile"), M("이 노래, 당신 오기 전에 혼자 몇 번이나 들었는지 몰라요.", "shy"), M("…이제 같이 들으니까 다른 노래 같아요.", "smile"));
                    break;
            }
        }

        // {days} real days since the first visit (7, 30, 100, 365, 730)
        static void MoreWall2(string id, WallLines w)
        {
            w.Anniversary = id switch
            {
                "kind" => S(M("기록상 오늘이 {days}일째예요. 처음 오신 날부터요.", "smile"), M("숫자로 말하니까 멋없네요. …그냥, 오래 함께해 줘서 고마워요.", "shy")),
                "cap" => S(M("오늘 {days}일 기념 공연이에요! 둠칫-둠칫!", "smile"), M("관객 한 명이 {days}일을 채웠어요! 이건 전설이에요!", "smiletonque")),
                "mila" => S(M("…오늘이 {days}일째라고요. 세고 있었던 건 아니고요. 그냥 알고 있었어요.", "shy"), M("…차 한 잔 더 내릴게요. 기념이니까요.", "discontent")),
                "sleepy" => S(M("오늘… {days}일째예요… 안 잊었어요…", "smile"), M("이렇게 오래… 깨어 있고 싶었던 적 없었어요…", "shy")),
                "short" => S(M("오늘이 입학 {days}일째예요! 개근상 드릴게요!", "smile"), M("…선생님도 이렇게 오래 가르친 학생은 처음이에요.", "shy")),
                "chibi" => S(M("오늘 {days}일째예요! 플레이어님, 제 부하 오래 했네요!", "arrogance"), M("…상으로 오늘은 바보라고 두 번만 할게요!", "shy")),
                _ => S(M("오늘로 우리가 만난 지 {days}일이에요.", "shy"), M("하루도 안 빼고 세고 있었어요. 앞으로의 날들도 전부 셀 거예요.", "smilestrange")),
            };
        }

        // the morning after a gift ({gift}; the one she loves has its own words), and a present of her own
        static void MoreGifts(string id, GiftLines g)
        {
            switch (id)
            {
                case "kind":
                    g.Recall = S(M("어제 받은 {gift}, 제자리에 잘 뒀어요. 보고서에도 적었고요.", "smile"), M("…보고서는 농담이에요. 그냥 좋았어요.", "shy"));
                    g.RecallLoved = S(M("어제 커피, 아침에 한 잔 더 내려 마셨어요.", "smile"), M("오늘 효율 지표가 좋네요. 당신 덕분이에요.", "shy"));
                    g.BackAsk = S(M("[player]님, 받으실 게 하나 있어요. 시간 괜찮으세요?", "smile"));
                    g.Back = V(S(M("제가 만든 열쇠고리예요. 작은 톱니바퀴를 깎았어요.", "smile"), M("…고장 나면 가져오세요. 평생 무상 수리예요.", "shy")));
                    break;
                case "cap":
                    g.Recall = S(M("어제 {gift} 받은 거, 아직도 기분 좋아요!", "smile"), M("오늘 공연은 그 기념이에요! 히히!", "smiletonque"));
                    g.RecallLoved = S(M("어제 테이프, 밤새 들었어요! 열 번은 돌렸어요!", "smile"), M("…그래서 좀 졸려요. 후회는 없어요!", "smiletonque"));
                    g.BackAsk = S(M("[player]님! 눈 감고 손 내밀어요! 서프라이즈!", "smile"));
                    g.Back = V(S(M("짜잔! 제 기타 피크예요! 제일 아끼는 거!", "smile"), M("이걸로 공연 때 맨 앞줄 프리패스예요! 히히힛!", "smiletonque")));
                    break;
                case "mila":
                    g.Recall = S(M("…어제 {gift} 줬던 거, 기억해요.", "shy"), M("고맙다는 말은 어제 했으니까 오늘은 안 해요.", "discontent"));
                    g.RecallLoved = S(M("어제 그 책, 밤새 다 읽었어요.", "shy"), M("…감상 듣고 싶으면 차 끓여 와요. 길어요.", "discontent"));
                    g.BackAsk = S(M("…잠깐 이리 와요. 줄 게 있어요. 별거 아니에요.", "discontent"));
                    g.Back = V(S(M("책갈피예요. 제가 만든 거예요. …떨어뜨리지 마요.", "shy"), M("착각하지 마요. 남는 거 준 거예요. …아니, 남는 건 아니고요.", "angry")));
                    break;
                case "sleepy":
                    g.Recall = S(M("어제 {gift}… 꿈에 나왔어요…", "halfsleep"), M("좋은 꿈이었어요… 고마워요…", "smile"));
                    g.RecallLoved = S(M("어제 인형… 안고 잤어요… 푹 잤어요…", "smile"), M("이름은… 아직 못 지었어요… 생각하다 잠들어서요…", "halfsleep"));
                    g.BackAsk = S(M("[player]님… 손 좀 내밀어 주세요… 줄 게 있어요…", "smile"));
                    g.Back = V(S(M("제 손수건이에요… 라벤더 향이 나요… 잠 잘 오는 향이에요…", "smile"), M("…피곤할 때 꺼내 보세요… 제가 옆에 있는 것처럼요…", "shy")));
                    break;
                case "short":
                    g.Recall = S(M("어제 {gift} 고마웠어요. 교무 일지에 적어 뒀어요.", "smile"), M("…학생한테 선물 받은 건 처음이라 기록해 둘 만하잖아요.", "shy"));
                    g.RecallLoved = S(M("어제 받은 책, 벌써 반 읽었어요. 밑줄도 쳤어요.", "smile"), M("다 읽으면 세 줄 요약, 약속한 대로요.", "smiletonque"));
                    g.BackAsk = S(M("[player], 선생님이 상 줄 게 있어요. 이리 와요.", "smile"));
                    g.Back = V(S(M("우등상이에요! 제가 직접 쓴 상장!", "smile"), M("…'선생님을 가장 웃게 한 학생' 상이에요. 진짜예요.", "shy")));
                    break;
                case "chibi":
                    g.Recall = S(M("어제 {gift} 준 거 기억해요! 잊을 리 없잖아요!", "arrogance"), M("…오늘도 뭐 없어요? 농담이에요! 반쯤요!", "smiletonque"));
                    g.RecallLoved = S(M("어제 초콜릿, 다 먹었어요! 혼자서요!", "smile"), M("…한 조각은 남겼어요. 플레이어님 거예요. 특별히요!", "shy"));
                    g.BackAsk = S(M("플레이어님! 눈 감아요! 빨리요!", "arrogance"));
                    g.Back = V(S(M("짜잔! 제 사탕이에요! 제일 큰 거!", "smile"), M("…부하한테 주는 거예요! 고맙다고 하세요! …천천히 먹어요.", "shy")));
                    break;
                default:   // 미치광이 미타
                    g.Recall = S(M("어제 준 {gift}, 아직 보고 있어요.", "shy"), M("당신이 없는 동안 그걸 보고 있으면 좀 견딜 만해요.", "smile"));
                    g.RecallLoved = S(M("어제 꽃, 침대 옆에 뒀어요. 자고 일어나서 제일 먼저 봤어요.", "shy"), M("…당신 다음으로요. 헤헤.", "smilestrange"));
                    g.BackAsk = S(M("[player]님, 잠깐 눈 감아 볼래요? 줄 게 있어요.", "shy"));
                    g.Back = V(S(M("짜잔. 제가 접은 종이 하트예요. 안에 뭐라고 썼는지는… 혼자 있을 때 봐요.", "shy"), M("…버리면 알 수 있어요. 헤헤.", "smilestrange")));
                    break;
            }
        }
    }
}
