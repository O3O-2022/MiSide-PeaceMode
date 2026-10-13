using HarmonyLib;

namespace MiSidePeaceMode
{
    // Keeps peace mode (and dev runs) from touching the story "Continue" save.
    [HarmonyPatch]
    public static class SaveGuard
    {
        static bool Blocked(string path) =>
            (PeaceMode.Active || DevRunner.Active) && path != null && path.Replace('\\', '/').Contains("/Save/") && !path.Contains("Achi ");

        [HarmonyPatch(typeof(GlobalAM), nameof(GlobalAM.SaveData)), HarmonyPrefix]
        static bool SaveData(string path)
        {
            if (!Blocked(path)) return true;
            Plugin.Log.LogInfo($"[save-guard] blocked SaveData {path}");
            return false;
        }

        [HarmonyPatch(typeof(GlobalAM), nameof(GlobalAM.SaveFile)), HarmonyPrefix]
        static bool SaveFile(string path)
        {
            if (!Blocked(path)) return true;
            Plugin.Log.LogInfo($"[save-guard] blocked SaveFile {path}");
            return false;
        }

        // The menu shows its own "평화 모드" button only when a "PeaceMode" save flag exists: pretend it always does.
        [HarmonyPatch(typeof(GlobalAM), nameof(GlobalAM.ExistsData)), HarmonyPostfix]
        static void ExistsData(string path, ref bool __result)
        {
            if (path == "PeaceMode") __result = true;
            else if (path == "Continue" && PeaceMode.Launching) __result = true;
        }

        // Peace mode is started through the menu's own Continue flow; while launching, hand it our scene instead of the save.
        [HarmonyPatch(typeof(GlobalAM), nameof(GlobalAM.LoadData)), HarmonyPrefix]
        static bool LoadData(string path, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray __result)
        {
            if (path != "Continue" || !PeaceMode.Launching) return true;
            PeaceMode.Launching = false;
            __result = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(new[] { PeaceMode.Scene, "0", "0", "0" });
            Plugin.Log.LogInfo("[peace] served peace scene to Continue flow");
            return false;
        }

        // The scenes' own checkpoint saves (a story scene's World: SilentSave(n) writes "Continue" with its checkpoint -
        // Scene 4's did that during peace mode, turning the story's checkpoint 0 into 4)
        [HarmonyPatch(typeof(Scene_Load), nameof(Scene_Load.SilentSave)), HarmonyPrefix]
        static bool SilentSave(int _levelSave)
        {
            if (!PeaceMode.Active && !DevRunner.Active) return true;
            Plugin.Log.LogInfo($"[save-guard] blocked Scene_Load.SilentSave({_levelSave})");
            return false;
        }

        [HarmonyPatch(typeof(Scene_Load), nameof(Scene_Load.SaveGame)), HarmonyPrefix]
        static bool SceneSaveGame()
        {
            if (!PeaceMode.Active && !DevRunner.Active) return true;
            Plugin.Log.LogInfo("[save-guard] blocked Scene_Load.SaveGame");
            return false;
        }

        [HarmonyPatch(typeof(GlobalAM), nameof(GlobalAM.DeleteData)), HarmonyPrefix]
        static bool DeleteData(string path)
        {
            if (!Blocked(path)) return true;
            Plugin.Log.LogInfo($"[save-guard] blocked DeleteData {path}");
            return false;
        }
    }
}
