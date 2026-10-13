using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // More of the world behind the screen: the real day of the week (first visit of a real day), your birthday
    // (config Birthday, once a year), a laptop running low on battery, and how many photos of the two of you there
    // are by now. Lines: Personas.MoreWall.
    public partial class PeaceDirector
    {
        // ---- the day of the week and your birthday (added to the session greeting) ----

        IEnumerable<Line> WeekdayLines(WallLines w)
        {
            var d = Now.DayOfWeek;
            Line[][] set = d == DayOfWeek.Monday ? w.Monday : d == DayOfWeek.Friday ? w.Friday
                         : d == DayOfWeek.Saturday || d == DayOfWeek.Sunday ? w.Weekend : null;
            return set != null && set.Length > 0 ? Pick(set) : Array.Empty<Line>();
        }

        static bool devBirthday;

        IEnumerable<Line> BirthdayLines(WallLines w)
        {
            string bd = "";
            try { bd = (PeaceMode.Birthday != null ? PeaceMode.Birthday.Value : "").Trim(); } catch { }
            bool today = devBirthday || (bd.Length > 0 && Now.ToString("MM-dd") == bd);
            devBirthday = false;
            if (!today || S.LastBirthday == Now.Year || w.Birthday == null) return Array.Empty<Line>();
            S.LastBirthday = Now.Year;
            AddAff(5);
            Plugin.Log.LogInfo("[peace] your birthday today");
            return w.Birthday;
        }

        // ---- the battery (laptops) ----

        [StructLayout(LayoutKind.Sequential)]
        struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
            public int BatteryLifeTime, BatteryFullLifeTime;
        }
        [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS s);

        float nextBatteryCheck;
        int batterySaid;            // 1 = "low" said on this charge, 2 = "critical" too
        static int devBattery = -1; // dev: wallsim battery <percent> (counts as unplugged)

        // unplugged and under 20 % (once a charge), under 7 % (once more); charged back over 30 % and it may be said again
        [HideFromIl2Cpp] void CheckBattery(float now, WallLines w)
        {
            if (now < nextBatteryCheck || !freeMode) return;
            nextBatteryCheck = now + 30f;
            int pct; bool unplugged;
            if (devBattery >= 0) { pct = devBattery; unplugged = true; devBattery = -1; }
            else
            {
                try
                {
                    if (!GetSystemPowerStatus(out var s) || s.BatteryFlag == 128 || s.BatteryLifePercent > 100) return;   // no battery here
                    pct = s.BatteryLifePercent;
                    unplugged = s.ACLineStatus == 0;
                }
                catch { return; }
            }
            if (!unplugged) { if (pct > 30) batterySaid = 0; return; }
            if (pct <= 7 && batterySaid < 2)
            {
                batterySaid = 2;
                Plugin.Log.LogInfo($"[peace] battery {pct}% (critical)");
                if (w.BatteryCritical != null) QueueWall(Pick(w.BatteryCritical));
            }
            else if (pct <= 20 && batterySaid < 1)
            {
                batterySaid = 1;
                Plugin.Log.LogInfo($"[peace] battery {pct}% (low)");
                if (w.BatteryLow != null) QueueWall(Pick(w.BatteryLow));
            }
        }

        // ---- photos of the two of you ----

        static readonly int[] PhotoMarks = { 10, 30, 100, 300 };
        static int devPhotos = -1;   // dev: wallsim photos <count>
        int photoCount;              // {photos}

        // after a photo is saved: at 10, 30, 100 and 300 of them she says so
        Line[] PhotoCountLines(string dir, WallLines w)
        {
            int n;
            if (devPhotos >= 0) { n = devPhotos; devPhotos = -1; }
            else { try { n = System.IO.Directory.GetFiles(dir, "*.png").Length; } catch { return Array.Empty<Line>(); } }
            photoCount = n;
            int reached = 0;
            foreach (var m in PhotoMarks) if (n >= m) reached = m;
            if (reached <= S.PhotosSaid || w.Photos == null) return Array.Empty<Line>();
            S.PhotosSaid = reached;
            Plugin.Log.LogInfo($"[peace] {n} photos together");
            return w.Photos;
        }
    }

    public static partial class Personas
    {
        // the weekday, birthday, battery and photo-count lines of each Mita (the rest of WallLines is in FourthWall.cs)
        static void MoreWall(string id, WallLines w)
        {
            switch (id)
            {
                case "kind":
                    w.Monday = V(S(M("월요일이에요. 한 주 계획은 세우셨어요?", "smile"), M("저는 벌써 다 세웠어요. 1번은 당신 챙기기예요.", "shy")));
                    w.Friday = V(S(M("금요일이네요. 한 주 수고하셨어요.", "smile"), M("주말엔 점검을 좀 쉬어 볼까 해요. 당신도 쉬세요.", "smile")));
                    w.Weekend = V(S(M("주말이에요. 시스템도 저전력 모드로 돌려 뒀어요.", "smile"), M("오늘은 천천히 가요. 우리 둘 다요.", "shy")));
                    w.Birthday = S(M("오늘 생일이시죠? 기록해 뒀어요.", "smile"), M("생일 축하해요, [player]님. 올해도 제가 옆에서 점검해 드릴게요.", "shy"),
                                   M("…케이크는 못 구웠어요. 대신 하루 종일 당신 편이에요.", "smile"));
                    w.BatteryLow = V(S(M("노트북 배터리 20퍼센트 아래예요. 전원 연결 권장해요.", "smile"), M("저장은 제가 해 두고 있지만, 갑자기 꺼지는 건 좋지 않아요.", "smile")));
                    w.BatteryCritical = V(S(M("배터리 위험 수준이에요. 지금 바로 충전기요.", "surprise"), M("…시스템이 꺼지기 전에요. 부탁이에요.", "sad")));
                    w.Photos = S(M("같이 찍은 사진이 {photos}장이 됐어요.", "smile"), M("폴더 정리는 제가 할게요. 날짜순으로요.", "smile"));
                    break;
                case "cap":
                    w.Monday = V(S(M("월요일! 한 주의 오프닝이에요! 둠칫-둠칫!", "smile"), M("월요병은 제 노래 한 곡이면 끝이에요! 히히!", "smiletonque")));
                    w.Friday = V(S(M("불금이에요! 오늘 밤은 공연이죠!", "smile"), M("내일 안 쉬어요? 그럼 앵콜 세 번은 가능하겠네요!", "smiletonque")));
                    w.Weekend = V(S(M("주말이다! 페스티벌 데이!", "smile"), M("오늘은 종일 놀아요! 지치면 제가 깨워 드릴게요! 음악으로요!", "smile")));
                    w.Birthday = S(M("생일이다!! 생일 축하 노래 갑니다! 둠칫!", "smile"), M("생일 축하합니다~ 생일 축하합니다~ 사랑하는 [player]님~!", "smile"),
                                   M("히히힛! 오늘은 뭐든 들어줄게요! 앵콜도요!", "smiletonque"));
                    w.BatteryLow = V(S(M("[player]님, 노트북 배터리 빨간불이에요!", "surprise"), M("공연 중에 전원 나가면 최악이라고요! 꽂아요, 꽂아요!", "smile")));
                    w.BatteryCritical = V(S(M("배터리 거의 바닥! 앵콜 전에 꺼지겠어요!", "surprise"), M("충전기! 지금! 히히… 아니 진짜요!", "discontent")));
                    w.Photos = S(M("우리 사진 {photos}장 돌파! 앨범 낼 수 있겠어요!", "smile"), M("표지는 당연히 저예요! 히히힛!", "smiletonque"));
                    break;
                case "mila":
                    w.Monday = V(S(M("월요일이네요. 밖에선 다들 싫어하는 날이라던데.", "discontent"), M("…저는 상관없어요. 책은 요일을 안 가리니까요. 당신도요.", "shy")));
                    w.Friday = V(S(M("금요일이에요. …그래서 뭐요.", "discontent"), M("…내일 쉬면, 좀 더 있다 가도 돼요. 억지로는 아니고요.", "shy")));
                    w.Weekend = V(S(M("주말이죠. 급할 거 없는 날이에요.", "smile"), M("…차 한 잔 더 내릴게요. 천천히 마셔요.", "shy")));
                    w.Birthday = S(M("…오늘 생일이라면서요.", "shy"), M("…축하해요. 선물은… 책 한 권 골라 뒀어요. 울지 마요, 바보.", "discontent"),
                                   M("…오늘은 차에 설탕 한 스푼 더 넣어 줄게요. 특별히요.", "shy"));
                    w.BatteryLow = V(S(M("…당신 컴퓨터 배터리, 얼마 안 남았어요.", "discontent"), M("충전기 꽂아요. 읽다가 끊기는 거, 제일 싫어한다고요.", "angry")));
                    w.BatteryCritical = V(S(M("배터리 거의 없잖아요! 뭐 해요, 바보!", "angry"), M("…꺼지기 전에 꽂아요. 걱정돼서 그러는 거 아니에요.", "shy")));
                    w.Photos = S(M("…사진이 {photos}장이나 됐네요.", "shy"), M("…다 보여 주지는 마요. 제가 웃는 거, 당신만 알면 돼요.", "discontent"));
                    break;
                case "sleepy":
                    w.Monday = V(S(M("월요일… 이에요…? 한 주가 또 시작이네요…", "halfsleep"), M("무리하지 마세요… 저처럼요…", "smile")));
                    w.Friday = V(S(M("금요일… 이번 주도 수고하셨어요…", "smile"), M("주말엔… 같이 늦잠 자요… 꼭이요…", "halfsleep")));
                    w.Weekend = V(S(M("주말이에요… 늦잠 자도 되는 날…", "smile"), M("…저는 매일이 그렇지만… 오늘은 당당하게요…", "halfsleep")));
                    w.Birthday = S(M("오늘… 생일이시죠…? 안 잊었어요…", "smile"), M("생일 축하해요, [player]님… 오늘은 제가 안 졸게요… 노력할게요…", "halfsleep"),
                                   M("…소원 비세요… 저도 같이 빌게요…", "smile"));
                    w.BatteryLow = V(S(M("…컴퓨터가 졸려 하는 것 같아요… 배터리가요…", "halfsleep"), M("충전기… 꽂아 주세요… 저처럼 자 버리면 안 되니까요…", "smile")));
                    w.BatteryCritical = V(S(M("배터리… 거의 없어요… 큰일이에요…", "surprise"), M("지금 꽂아요… 저도 깨어 있을게요… 조금만…", "halfsleep")));
                    w.Photos = S(M("사진이… {photos}장이나 쌓였어요…", "smile"), M("눈 감은 사진은… 몇 장일까요… 세지 말아 주세요…", "halfsleep"));
                    break;
                case "short":
                    w.Monday = V(S(M("월요일이에요! 한 주의 첫 수업이네요.", "smile"), M("월요일엔 가볍게 시작하세요. 선생님이 봐줄게요.", "smiletonque")));
                    w.Friday = V(S(M("금요일이에요! 이번 주도 잘 버텼어요.", "smile"), M("숙제는 없어요. 대신 푹 쉬는 게 숙제예요.", "smile")));
                    w.Weekend = V(S(M("주말이네요. 수업은 쉬는 날이에요.", "smile"), M("그래도 선생님은 출근했어요. 당신이 오니까요.", "shy")));
                    w.Birthday = S(M("오늘 생일인 학생이 있다고 들었어요!", "smile"), M("생일 축하해요, [player]! 선생님이 축하 도장 쾅!", "smiletonque"),
                                   M("오늘 숙제는 없어요. 대신 행복하게 지내기! 그게 숙제예요.", "smile"));
                    w.BatteryLow = V(S(M("[player], 노트북 배터리가 얼마 안 남았어요.", "smile"), M("수업 중에 전원이 나가면 곤란하거든요. 충전기 챙기세요.", "smile")));
                    w.BatteryCritical = V(S(M("배터리 경고예요! 지금 바로 충전기 꽂으세요!", "surprise"), M("선생님 말 들으세요. 이건 숙제가 아니라 긴급이에요!", "discontent")));
                    w.Photos = S(M("우리 사진이 {photos}장이에요. 졸업 앨범 한 권은 나오겠어요.", "smile"), M("표지 사진은 같이 고르기로 해요.", "shy"));
                    break;
                case "chibi":
                    w.Monday = V(S(M("월요일이에요! 플레이어님, 이번 주도 열심히 하세요!", "arrogance"), M("저는 매일이 휴일이에요! 체크포인트는 원래 한가하거든요!", "smile")));
                    w.Friday = V(S(M("금요일! 플레이어님 주말에 뭐 해요?", "smile"), M("…저랑 놀아야죠! 당연하잖아요!", "arrogance")));
                    w.Weekend = V(S(M("주말이네요! 플레이어님, 늦잠 잤죠? 얼굴에 써 있어요!", "smiletonque"), M("오늘은 종일 제 부하 하세요!", "arrogance")));
                    w.Birthday = S(M("플레이어님 생일이에요?! 왜 미리 말 안 했어요!", "angry"), M("…생일 축하해요! 오늘만 바보라고 안 할게요!", "shy"),
                                   M("선물은… 제 지능 체크 면제권이에요! 엄청 귀한 거예요!", "arrogance"));
                    w.BatteryLow = V(S(M("플레이어님! 컴퓨터 배터리 얼마 없어요!", "surprise"), M("충전기도 안 꽂고 뭐 해요! 바보!", "angry")));
                    w.BatteryCritical = V(S(M("배터리 거의 없잖아요!! 꺼지면 제가 사라진다고요!", "angry"), M("빨리 꽂아요! 지능 체크 감점이에요!", "arrogance")));
                    w.Photos = S(M("사진 {photos}장! 플레이어님, 제 팬이에요?", "arrogance"), M("…저도 플레이어님 사진 좋아해요. 조금요!", "shy"));
                    break;
                default:   // 미치광이 미타
                    w.Monday = V(S(M("월요일이네요. 밖은 다들 바쁘겠다.", "smile"), M("저는 요일 같은 거 없어요. 당신이 오는 날이 제일 좋은 날이에요.", "shy")));
                    w.Friday = V(S(M("금요일이에요! 내일 안 쉬어요?", "smile"), M("…그럼 오늘은 늦게까지 있어도 되겠네요. 헤헤.", "smilestrange")));
                    w.Weekend = V(S(M("주말이죠? 온종일 저랑 있어도 되는 날.", "smile"), M("약속 같은 건… 없죠? 없는 걸로 할게요.", "smilestrange")));
                    w.Birthday = S(M("[player]님, 오늘 생일이죠?", "smile"), M("축하해요. 태어나 줘서… 저한테 와 줘서 고마워요.", "shy"),
                                   M("선물은 저예요. 평생 반품 불가예요. 헤헤.", "smilestrange"));
                    w.BatteryLow = V(S(M("있잖아요, 당신 컴퓨터… 배터리가 얼마 안 남았어요.", "suspicion"), M("충전기 꽂아요. 중간에 꺼지면 저 또 혼자 되잖아요.", "sad")));
                    w.BatteryCritical = V(S(M("배터리 진짜 거의 없어요! 지금 꽂아요!", "surprise"), M("…이대로 꺼지면 작별 인사도 못 하잖아요.", "sad")));
                    w.Photos = S(M("우리 사진, 벌써 {photos}장이에요.", "shy"), M("전부 세고 있었어요. 앞으로도 셀 거예요. 헤헤.", "smilestrange"));
                    break;
            }
        }
    }
}
