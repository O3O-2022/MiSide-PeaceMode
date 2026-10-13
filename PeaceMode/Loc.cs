using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MiSidePeaceMode
{
    // Languages. Everything the mod says is written in Korean in the code; lang/<code>.json (embedded in the DLL)
    // maps each Korean text to its translation. T() translates a fixed text, F() a template ("{0}와 교감하기") before
    // filling it in. Missing entries fall back to Korean. The language follows the game's (config: Language = auto,
    // every language the game has) unless one is set; one we have no table for falls back to English.
    public static class Loc
    {
        public static string Lang = "ko";
        static Dictionary<string, string> table;

        public static bool Korean => Lang == "ko";
        public static bool RightToLeft => Lang == "ar" || Lang == "arz" || Lang == "fa";

        // what the screen gets: Arabic and Persian joined and in visual order, like the game's own files (see Rtl)
        public static string Display(string s) => RightToLeft ? Rtl.Shape(s) : s;

        // the game's language names (its Data/Languages folders) -> our codes
        static readonly Dictionary<string, string> GameLanguages = new Dictionary<string, string>
        {
            ["korean"] = "ko", ["english"] = "en", ["japanese"] = "ja", ["chinesesimplified"] = "zh", ["chinesetraditional"] = "zht",
            ["russian"] = "ru", ["pre-revolutionaryrussian"] = "ruold", ["ukrainian"] = "uk", ["belarusian"] = "be", ["bulgarian"] = "bg",
            ["kazakh"] = "kk", ["serbian (latin)"] = "sr", ["croatian"] = "hr", ["czech"] = "cs", ["slovak"] = "sk", ["polish"] = "pl",
            ["hungarian"] = "hu", ["romanian"] = "ro", ["german"] = "de", ["french"] = "fr", ["italian"] = "it",
            ["spanish (spain)"] = "es", ["spanish (latinamerica)"] = "esla", ["portugues portugal"] = "pt", ["português-brasil"] = "ptbr",
            ["swedish"] = "sv", ["turkish"] = "tr", ["indonesia"] = "id", ["filipino"] = "fil", ["vietnamese"] = "vi", ["thai"] = "th",
            ["arabic"] = "ar", ["arabic (egyptian)"] = "arz", ["persian"] = "fa",
        };

        // which of the game's fonts a language needs (OptionsGame.font*)
        static string Script(string code) => code switch
        {
            "ja" or "zh" or "zht" => "cjk", "ko" => "ko", "th" => "th", "ar" or "arz" or "fa" => "arabic",
            "ru" or "ruold" or "uk" or "be" or "bg" or "kk" => "cyrillic", _ => "latin",
        };

        // Japanese and Chinese need the game's CJK font: its speech bubbles are built letter by letter from
        // GlobalGame.fontUse, which follows the game's own language (a Korean font has no kana). While peace mode runs
        // in one of them with the game in another language, that font is swapped in (and put back afterwards).
        static UnityEngine.Font savedFont;
        public static UnityEngine.Font SpeechFont { get; private set; }   // non-null while swapped in

        public static void UseFont(bool on)
        {
            if (!on)
            {
                if (savedFont != null) GlobalGame.fontUse = savedFont;
                savedFont = null;
                SpeechFont = null;
                return;
            }
            if (savedFont != null) return;
            string game;
            try { game = GlobalGame.Language; } catch { game = null; }
            var need = Script(Lang);
            if (need == Script(FromGame(game))) return;   // (the game's own font already has these letters)
            OptionsGame opt = null;
            foreach (var o in UnityEngine.Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<OptionsGame>()))
                if (o != null) { opt = o.Cast<OptionsGame>(); break; }
            var font = opt == null ? null : need switch
            {
                "cjk" => opt.fontJapaneseChinese, "ko" => opt.fontKorean, "th" => opt.fontThai, "arabic" => opt.fontArabic,
                "cyrillic" => opt.fontRussian, _ => opt.fontOther,
            };
            if (font == null) { Plugin.Log.LogWarning($"[lang] no {need} font found"); return; }
            savedFont = GlobalGame.fontUse;
            GlobalGame.fontUse = font;
            SpeechFont = font;
            Plugin.Log.LogInfo($"[lang] speech font: {font.name} (was {(savedFont != null ? savedFont.name : "-")})");
        }

        // the game's own language setting (GlobalGame.Language) -> ours
        static string FromGame(string g)
        {
            g = (g ?? "").Trim().ToLowerInvariant();
            if (g.Length == 0) return "ko";
            if (GameLanguages.TryGetValue(g, out var code)) return code;
            if (g.Contains("korea") || g == "ko" || g.Contains("한국")) return "ko";
            if (g.Contains("japan") || g == "ja" || g == "jp") return "ja";
            if (g.Contains("tradition")) return "zht";
            if (g.Contains("chin") || g == "zh" || g == "cn") return "zh";
            return "en";
        }

        public static void Init(string setting)
        {
            string game = null;
            try { game = GlobalGame.Language; } catch { }
            var want = string.IsNullOrWhiteSpace(setting) || setting.Trim().ToLowerInvariant() == "auto" ? FromGame(game) : setting.Trim().ToLowerInvariant();
            if (want == Lang && (table != null || want == "ko")) return;
            Lang = want;
            table = null;
            english = null;
            if (Lang != "ko")
            {
                try
                {
                    table = LoadTable(Lang);
                    if (table == null) { Plugin.Log.LogWarning($"[lang] no {Lang} table: English"); Lang = "en"; table = LoadTable("en"); }
                    // (what a language hasn't translated - the AI chat's character sheets - comes out in English, not Korean)
                    if (Lang != "en") english = LoadTable("en");
                }
                catch (Exception e) { Plugin.Log.LogWarning("[lang] " + e.Message); table = null; english = null; Lang = "ko"; }
            }
            Plugin.Log.LogInfo($"[lang] {Lang} (game: '{game}', setting: '{setting}', {table?.Count ?? 0} texts)");
        }

        static Dictionary<string, string> english;

        static Dictionary<string, string> LoadTable(string code)
        {
            using var s = typeof(Loc).Assembly.GetManifestResourceStream($"MiSidePeaceMode.lang.{code}.json");
            if (s == null) return null;
            using var r = new StreamReader(s);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(r.ReadToEnd());
        }

        public static string T(string ko)
        {
            if (ko == null || table == null) return ko;
            if (table.TryGetValue(ko, out var t) && !string.IsNullOrEmpty(t)) return t;
            return english != null && english.TryGetValue(ko, out var e) && !string.IsNullOrEmpty(e) ? e : ko;
        }

        // an object's name as a label ("teddy bear" -> "Teddy bear"); Korean, Japanese and Chinese have no case
        public static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        public static string F(string koTemplate, params object[] args)
        {
            var t = T(koTemplate);
            try { return string.Format(t, args); } catch { return string.Format(koTemplate, args); }
        }
    }

    // the speech bubbles pick their font by the game's language when they start: ours instead (see Loc.UseFont)
    [HarmonyLib.HarmonyPatch]
    static class SpeechFontPatch
    {
        [HarmonyLib.HarmonyPatch(typeof(Dialogue_3DText), nameof(Dialogue_3DText.Start)), HarmonyLib.HarmonyPostfix]
        static void AfterStart(Dialogue_3DText __instance) { if (Loc.SpeechFont != null && __instance != null) __instance.font = Loc.SpeechFont; }

        [HarmonyLib.HarmonyPatch(typeof(Dialogue_3DText), nameof(Dialogue_3DText.SetStyleFont)), HarmonyLib.HarmonyPostfix]
        static void AfterStyle(Dialogue_3DText __instance) { if (Loc.SpeechFont != null && __instance != null) __instance.font = Loc.SpeechFont; }
    }
}
