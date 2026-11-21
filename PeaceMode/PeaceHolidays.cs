using System;
using System.Collections.Generic;

namespace MiSidePeaceMode
{
    // Seollal and Chuseok (lunar dates, so a table) on top of the fixed calendar days in FourthWall: each Mita says
    // something her way on the first visit of the day (Korean only - the holidays are Korea's).
    public static partial class Personas
    {
        static readonly Dictionary<string, string> LunarDays = new Dictionary<string, string>
        {
            ["2026-02-17"] = "seollal", ["2027-02-07"] = "seollal", ["2028-01-27"] = "seollal", ["2029-02-13"] = "seollal", ["2030-02-03"] = "seollal",
            ["2026-09-25"] = "chuseok", ["2027-09-15"] = "chuseok", ["2028-10-03"] = "chuseok", ["2029-09-22"] = "chuseok", ["2030-09-12"] = "chuseok",
        };

        // the key into WallLines.Dates for this real day: a lunar holiday (Korean), else "MM-dd"
        public static string DateKey(DateTime now)
        {
            if (Loc.Korean && LunarDays.TryGetValue(now.ToString("yyyy-MM-dd"), out var k)) return k;
            return now.ToString("MM-dd");
        }

        static void HolidayWall(string id, WallLines w)
        {
            if (w.Dates == null) return;
            (Line[] seol, Line[] chu) = id switch
            {
                "kind" => (S(M("설날이에요! 새해 복 많이 받으세요, [player]님.", "smile"), M("떡국은 제가 끓였어요. 계량은 완벽하게 했거든요!", "smile")),
                           S(M("추석이에요. 송편 빚기, 같이 해요!", "smile"), M("모양이 조금 달라도 괜찮아요. 맛은 제가 보장할게요.", "smile"))),
                "cap" => (S(M("설날이다! 새해 복 많이 받아요! 히히힛!", "smile"), M("세배하면 제가 기타 한 곡 쳐 줄게요!", "smiletonque")),
                          S(M("추석이에요! 보름달 보면서 합주해요!", "smile"), M("송편은 무조건 깨 송편이죠! 히히!", "smile"))),
                "mila" => (S(M("…설날이네요. 새해 복 많이 받아요.", "shy"), M("세뱃돈은 없어요. 대신 책 한 권 골라 줄게요.", "discontent")),
                           S(M("추석이에요. 보름달이 밝아서 책 읽기 좋겠어요.", "smile"), M("…송편, 하나만 나눠 줘요.", "shy"))),
                "sleepy" => (S(M("설날이에요… 새해 복 많이 받으세요…", "halfsleep"), M("떡국 먹으면 한 살 더… 그럼 더 자도 되겠죠…", "smile")),
                             S(M("추석이에요… 보름달은 최고의 무드등이에요…", "halfsleep"), M("배부르게 먹고 같이 자요…", "smile"))),
                "short" => (S(M("설날이에요! 오늘의 수업은 세배 예절이에요!", "smile"), M("자, 따라 해 봐요. 새해 복 많이 받으세요!", "smile")),
                            S(M("추석이에요! 송편 빚기 실습 시간이에요!", "smile"), M("예쁘게 빚으면 예쁜 딸 낳는대요. …아, 이건 시험에 안 나와요!", "shy"))),
                "chibi" => (S(M("설날이에요! 세뱃돈 주세요, 플레이어님!", "smile"), M("안 주면… 바보라고 백 번 부를 거예요!", "smiletonque")),
                            S(M("추석이에요! 송편 제가 제일 많이 먹을 거예요!", "smile"), M("플레이어님 건 하나만 남겨 줄게요. 헤헤!", "smiletonque"))),
                _ => (S(M("설날이에요! 새해 복 많이 받아요, [player]님!", "smile"), M("한복 입은 저, 상상해 봤어요? 헤헤.", "shy")),
                      S(M("추석이에요! 보름달한테 소원 빌었어요.", "smile"), M("무슨 소원인지는… 당신이 제일 잘 알 거예요.", "smilestrange"))),
            };
            w.Dates["seollal"] = seol;
            w.Dates["chuseok"] = chu;
        }
    }
}
