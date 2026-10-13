using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Configuration;

namespace MiSidePeaceMode
{
    public class PeaceSave
    {
        public int Day = 1;
        public float Minute = 7 * 60;   // minute of the day, 0..1440
        public int Affection;
        public int LastTier;            // the step of affection she last remarked on (0 = not yet)
        public string Weather;          // today's weather (PeaceWeather) and the day it was drawn for
        public int WeatherDay;
        public float AffCarry;          // fractional affection for Mitas who warm up slowly
        public int Talks, Pats, Hugs, Compliments, RpsWins, RpsGames;
        public HashSet<int> Milestones = new HashSet<int>();
        public HashSet<string> Seen = new HashSet<string>();        // things in the house looked at with her ("#tour" once all were)
        public HashSet<string> SeenToday = new HashSet<string>();   // ... on day SeenDay (a little affection the first time each day)
        public int SeenDay;
        public string Outfit;   // what the original Mita wears ("FIIdClSchool:1"), empty = her own clothes
        public DateTime? LastSeen;       // real time of the last save (updated while playing and on leaving)
        public string LastMeal;          // day + b/l/d of the last meal together
        // how the last session ended: menu (left to the main menu), quit (said goodbye), altf4, close (window X);
        // Running stays true while playing, so a session that never recorded an exit was killed or crashed
        public bool Running;
        public string ExitKind = "";
        public int AltF4Count;
        public string LastDateEvent;     // real date (yyyy-MM-dd) of the last calendar greeting
        public string LastRealDate;      // real date of the last session, for the day streak
        public int Streak, RealDays;
        public int Sessions, PlayHoursSaid;
        public float PlayedSec;          // real seconds spent in peace mode, all sessions
        public int GiftDay, Gifts;       // gifts (PeaceGifts): the game day of GiftsToday, how many in all
        public HashSet<string> GiftsToday = new HashSet<string>();
        public int LastBirthday;         // the year her birthday wishes were last said
        public int PhotosSaid;           // the photo count she last remarked on (10, 30, 100, 300)
        public string FirstDate;         // real date (yyyy-MM-dd) of the first visit, for anniversaries
        public int LastGiftDay;          // the game day of the last gift (0 = already mentioned), what it was, loved or not
        public string LastGift;
        public bool LastGiftLoved;
        public int PresentDay;           // the game day she last gave you something
        public const int Version = 2;    // written as "version="; readers ignore keys they don't know
        // set by the dev runner once it has put the player's saves back: nothing may write after that
        public static bool Frozen;

        public string Id = "crazy";
        // the original Mita keeps the first save file name so existing progress carries over
        string PathFile => System.IO.Path.Combine(BepInEx.Paths.ConfigPath, Id == "crazy" ? "MiSidePeaceMode.save" : $"MiSidePeaceMode_{Id}.save");

        public static PeaceSave Load(string id = "crazy")
        {
            var s = new PeaceSave { Id = id };
            var PathFile = s.PathFile;
            try
            {
                if (!File.Exists(PathFile)) return s;
                foreach (var raw in File.ReadAllLines(PathFile))
                {
                    var kv = raw.Split('=', 2);
                    if (kv.Length != 2) continue;
                    var v = kv[1].Trim();
                    switch (kv[0].Trim())
                    {
                        case "day": s.Day = int.Parse(v); break;
                        case "minute": s.Minute = float.Parse(v, CultureInfo.InvariantCulture); break;
                        case "affection": s.Affection = int.Parse(v); break;
                        case "affCarry": s.AffCarry = float.Parse(v, CultureInfo.InvariantCulture); break;
                        case "talks": s.Talks = int.Parse(v); break;
                        case "pats": s.Pats = int.Parse(v); break;
                        case "hugs": s.Hugs = int.Parse(v); break;
                        case "compliments": s.Compliments = int.Parse(v); break;
                        case "rpsWins": s.RpsWins = int.Parse(v); break;
                        case "rpsGames": s.RpsGames = int.Parse(v); break;
                        case "lastMeal": s.LastMeal = v; break;
                        case "running": s.Running = v == "1"; break;
                        case "exit": s.ExitKind = v; break;
                        case "altF4": s.AltF4Count = int.Parse(v); break;
                        case "lastDate": s.LastDateEvent = v; break;
                        case "lastRealDate": s.LastRealDate = v; break;
                        case "streak": s.Streak = int.Parse(v); break;
                        case "realDays": s.RealDays = int.Parse(v); break;
                        case "sessions": s.Sessions = int.Parse(v); break;
                        case "playHoursSaid": s.PlayHoursSaid = int.Parse(v); break;
                        case "playedSec": s.PlayedSec = float.Parse(v, CultureInfo.InvariantCulture); break;
                        case "lastSeen":
                            if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ls)) s.LastSeen = ls;
                            break;
                        case "seen": foreach (var x in v.Split(',', StringSplitOptions.RemoveEmptyEntries)) s.Seen.Add(x); break;
                        case "seenToday": foreach (var x in v.Split(',', StringSplitOptions.RemoveEmptyEntries)) s.SeenToday.Add(x); break;
                        case "seenDay": s.SeenDay = int.Parse(v); break;
                        case "outfit": s.Outfit = v; break;
                        case "lastTier": s.LastTier = int.Parse(v); break;
                        case "weather": s.Weather = v; break;
                        case "weatherDay": s.WeatherDay = int.Parse(v); break;
                        case "giftDay": s.GiftDay = int.Parse(v); break;
                        case "gifts": s.Gifts = int.Parse(v); break;
                        case "giftsToday": foreach (var x in v.Split(',', StringSplitOptions.RemoveEmptyEntries)) s.GiftsToday.Add(x); break;
                        case "lastBirthday": s.LastBirthday = int.Parse(v); break;
                        case "photosSaid": s.PhotosSaid = int.Parse(v); break;
                        case "firstDate": s.FirstDate = v; break;
                        case "lastGiftDay": s.LastGiftDay = int.Parse(v); break;
                        case "lastGift": s.LastGift = v; break;
                        case "lastGiftLoved": s.LastGiftLoved = v == "1"; break;
                        case "presentDay": s.PresentDay = int.Parse(v); break;
                        case "milestones":
                            foreach (var m in v.Split(',', StringSplitOptions.RemoveEmptyEntries)) s.Milestones.Add(int.Parse(m));
                            break;
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("[peace] save load failed: " + e.Message); }
            return s;
        }

        public void Write()
        {
            if (Frozen) return;
            try
            {
                LastSeen = DateTime.Now;
                File.WriteAllLines(PathFile, new[]
                {
                    "version=" + Version,
                    "lastSeen=" + LastSeen.Value.ToString("o", CultureInfo.InvariantCulture),
                    "lastMeal=" + (LastMeal ?? ""),
                    "running=" + (Running ? "1" : "0"),
                    "exit=" + (ExitKind ?? ""),
                    "altF4=" + AltF4Count,
                    "lastDate=" + (LastDateEvent ?? ""),
                    "lastRealDate=" + (LastRealDate ?? ""),
                    "streak=" + Streak, "realDays=" + RealDays,
                    "sessions=" + Sessions, "playHoursSaid=" + PlayHoursSaid, "playedSec=" + PlayedSec.ToString(CultureInfo.InvariantCulture),
                    "day=" + Day,
                    "minute=" + Minute.ToString(CultureInfo.InvariantCulture),
                    "affection=" + Affection,
                    "affCarry=" + AffCarry.ToString(CultureInfo.InvariantCulture),
                    "talks=" + Talks, "pats=" + Pats, "hugs=" + Hugs, "compliments=" + Compliments,
                    "rpsWins=" + RpsWins, "rpsGames=" + RpsGames,
                    "milestones=" + string.Join(",", Milestones),
                    "seen=" + string.Join(",", Seen), "seenToday=" + string.Join(",", SeenToday), "seenDay=" + SeenDay,
                    "outfit=" + (Outfit ?? ""),
                    "lastTier=" + LastTier,
                    "weather=" + (Weather ?? ""), "weatherDay=" + WeatherDay,
                    "giftDay=" + GiftDay, "gifts=" + Gifts, "giftsToday=" + string.Join(",", GiftsToday),
                    "lastBirthday=" + LastBirthday, "photosSaid=" + PhotosSaid,
                    "firstDate=" + (FirstDate ?? ""), "lastGiftDay=" + LastGiftDay, "lastGift=" + (LastGift ?? ""),
                    "lastGiftLoved=" + (LastGiftLoved ? "1" : "0"), "presentDay=" + PresentDay,
                });
            }
            catch (Exception e) { Plugin.Log.LogWarning("[peace] save write failed: " + e.Message); }
        }
    }

    public static class PeaceMode
    {
        public const string Scene = "Scene 4 - StartSecret";
        public static bool Active;
        public static bool Launching;
        public static PeaceSave Save;
        public static Persona Persona = Personas.Default;
        public static DateTime? PreviousVisit;   // LastSeen as it was when this session started
        public static string PreviousExit = "";  // how the previous session ended (see PeaceSave.ExitKind; crash = never recorded)
        public static bool FirstVisit;

        public static ConfigEntry<float> TimeScale;      // game minutes per real second
        public static ConfigEntry<UnityEngine.KeyCode> InteractKey;
        public static ConfigEntry<bool> ShowHud;
        public static ConfigEntry<string> PlayerName;
        public static ConfigEntry<string> Language;
        // "자유롭게 말하기": a local language model server (OpenAI chat API)
        public static ConfigEntry<bool> AiEnabled;
        public static ConfigEntry<string> AiEndpoint, AiModel, AiApiKey;
        public static ConfigEntry<string> Birthday;
        public static ConfigEntry<int> AiMaxTokens, AiTimeout;
        public static ConfigEntry<float> AiTemperature;

        public static void BindConfig(ConfigFile cfg)
        {
            TimeScale = cfg.Bind("PeaceMode", "TimeScale", 1.0f, "게임 속 1분이 흐르는 데 걸리는 현실 시간의 역수 (1 = 현실 1초에 게임 1분)");
            InteractKey = cfg.Bind("PeaceMode", "InteractKey", UnityEngine.KeyCode.E, "미타와 교감하기 키 (가까이 가서 미타에게 테두리가 보일 때)");
            if (InteractKey.Value == UnityEngine.KeyCode.F) InteractKey.Value = UnityEngine.KeyCode.E;   // old default
            ShowHud = cfg.Bind("PeaceMode", "ShowHud", true, "화면 위쪽에 일차/시간/호감도 표시");
            PlayerName = cfg.Bind("PeaceMode", "PlayerName", "", "미타가 부르는 내 이름. 비워 두면 1장에서 입력한 이름, 그것도 없으면 '플레이어'");
            Language = cfg.Bind("PeaceMode", "Language", "auto", "평화 모드의 언어 / Language / 言語 / 语言: auto (게임 언어를 따름 / follows the game), ko, en, ja, zh");
            Birthday = cfg.Bind("PeaceMode", "Birthday", "", "내 생일 (MM-dd, 예: 03-14). 그날 처음 들어가면 미타가 축하해 줍니다. 비워 두면 없음 / your birthday as MM-dd");
            AiEnabled = cfg.Bind("AiChat", "Enabled", false, "대화하기 메뉴에 '자유롭게 말하기'(로컬 AI 모델과 자유 대화)를 켭니다. 아래 주소에서 AI 서버가 돌아가고 있어야 합니다.");
            AiEndpoint = cfg.Bind("AiChat", "Endpoint", "http://127.0.0.1:11434/v1/chat/completions", "OpenAI 방식 채팅 API 주소 (Ollama 기본값. llama.cpp 서버: http://127.0.0.1:8080/v1/chat/completions, LM Studio: http://127.0.0.1:1234/v1/chat/completions)");
            AiModel = cfg.Bind("AiChat", "Model", "", "서버에서 쓸 모델 이름 (예: Ollama의 모델 태그). llama.cpp처럼 하나만 띄우는 서버면 비워도 됩니다.");
            AiApiKey = cfg.Bind("AiChat", "ApiKey", "", "서버가 키를 요구할 때만. 로컬 서버는 보통 비워 둡니다.");
            AiMaxTokens = cfg.Bind("AiChat", "MaxTokens", 200, "한 번 대답의 최대 토큰 수");
            AiTemperature = cfg.Bind("AiChat", "Temperature", 0.8f, "대답의 다양성 (0 = 늘 같은 대답, 1 이상 = 엉뚱해짐)");
            AiTimeout = cfg.Bind("AiChat", "TimeoutSeconds", 90, "대답을 기다리는 최대 시간(초). 느린 PC면 늘리세요.");
        }

        public static void Launch(Menu menu, Persona persona = null)
        {
            Active = true;
            Persona = persona ?? Persona ?? Personas.Default;
            Save = PeaceSave.Load(Persona.Id);
            PreviousVisit = Save.LastSeen;
            FirstVisit = Save.LastSeen == null && Save.Day <= 1 && Save.Affection == 0 && Save.Talks == 0;
            PreviousExit = Save.Running ? "crash" : (Save.ExitKind ?? "");
            Plugin.Log.LogInfo($"[peace] launching peace mode with {Persona.Id}, day {Save.Day}, affection {Save.Affection}");
            if (menu != null)
            {
                Launching = true;
                menu.ButtonContinue();
            }
            else
            {
                GlobalGame.LoadingLevel = Scene;
                GlobalGame.levelLoad = 0;
                UnityEngine.SceneManagement.SceneManager.LoadScene("SceneLoading");
            }
        }

        // kept for the dev runner
        public static void Start(string id = null) => Launch(UnityEngine.Object.FindObjectOfType<Menu>(), id != null ? Personas.Get(id) : null);
    }
}
