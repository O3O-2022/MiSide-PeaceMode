using System;
using HarmonyLib;

namespace MiSidePeaceMode
{
    // The story's camera moves (the World's "look at that thing" and back) are called from borrowed setups' key events
    // (the dance pad's song pick). In our scene one of them can throw inside the game (PlayerMove.CameraTargetOther:
    // a NullReferenceException), and an exception in one call of a UnityEvent stops every call after it - the pad's
    // pick then never got to the player stepping off or to our own handler. In peace mode they run inside a try.
    [HarmonyPatch]
    public static class CallShield
    {
        static bool inside;

        static bool Shield(Action original, string what)
        {
            if (inside || !PeaceMode.Active) return true;
            inside = true;
            try { original(); }
            catch (Exception e) { Plugin.Log.LogWarning($"[shield] {what}: {e.Message.Split('\n')[0]}"); }
            finally { inside = false; }
            return false;
        }

        [HarmonyPatch(typeof(WorldPlayer), nameof(WorldPlayer.CameraLerpOtherTargetStop)), HarmonyPrefix]
        static bool LerpStop(WorldPlayer __instance) => Shield(__instance.CameraLerpOtherTargetStop, "CameraLerpOtherTargetStop");

        [HarmonyPatch(typeof(WorldPlayer), nameof(WorldPlayer.CameraFOVLerpReset)), HarmonyPrefix]
        static bool FovReset(WorldPlayer __instance) => Shield(__instance.CameraFOVLerpReset, "CameraFOVLerpReset");
    }
}
