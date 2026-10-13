using System.Collections.Generic;
using HarmonyLib;

namespace MiSidePeaceMode
{
    // Lets us feed our own lines into the game's native dialogue objects:
    // a Dialogue_3DText whose indexString is >= Base reads its text from Custom.
    [HarmonyPatch]
    public static class TextHook
    {
        public const int Base = 900000;
        static readonly Dictionary<int, string> Custom = new Dictionary<int, string>();
        static int next = Base;

        public static int Register(string text)
        {
            if (next > Base + 50000) { next = Base; Custom.Clear(); }
            Custom[next] = text;
            return next++;
        }

        // Same substitutions the game applies to its own lines ([player] -> name, ...).
        static string Rules(string t)
        {
            try { return DialogueGlobal.TextRule(t); }
            catch { return t.Replace("[player]", GlobalGame.namePlayer ?? ""); }
        }

        // Prefix: the original would index past the end of the language file for our ids.
        // The game's text objects (Dialogue_3DText, ObjectInteractive_CaseInfo, DialogueChanger) ask for line indexString - 1
        // (the story's first line, indexString 1, is looked up as 0): id N arrives here as N - 1.
        [HarmonyPatch(typeof(GlobalLanguage), nameof(GlobalLanguage.GetString)), HarmonyPrefix]
        static bool GetString(string _name, int _string, ref string __result)
        {
            if (_string >= Base - 1 && Custom.TryGetValue(_string + 1, out var t)) { __result = Loc.Display(Rules(t)); return false; }
            if (PeaceMode.Active && _name != null && _name.StartsWith("LocationHint Location4"))
            {
                __result = Loc.Display(Lines.Hints[System.Math.Clamp(_string, 0, Lines.Hints.Length - 1)]);
                return false;
            }
            return true;
        }
    }
}
