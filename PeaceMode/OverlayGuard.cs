using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiSidePeaceMode
{
    // The game's full-screen loading overlay (SceneLoading_Preloading, kept across scenes) normally removes itself
    // once a scene is ready. After leaving peace mode it could stay up over the menu and the next peace scene.
    // If it is still visible a few seconds after a scene started, dismiss it.
    public static class OverlayGuard
    {
        public static bool ReturningFromPeace;   // set when peace mode sends the player back to the menu
        static int sceneHandle;
        static float sceneStart;
        static bool handled;

        public static void Tick(bool peaceScene)
        {
            var sc = SceneManager.GetActiveScene();
            if (sc.name == "SceneLoading") return;
            if (sc.handle != sceneHandle) { sceneHandle = sc.handle; sceneStart = Time.unscaledTime; handled = false; return; }
            if (handled || Time.unscaledTime - sceneStart < 15f) return;
            handled = true;
            if (!peaceScene && !ReturningFromPeace) return;
            ReturningFromPeace = false;
            foreach (var p in UnityEngine.Object.FindObjectsOfType<SceneLoading_Preloading>())
            {
                if (p == null) continue;
                var cv = p.GetComponent<Canvas>();
                if (cv != null && !cv.enabled) continue;
                Plugin.Log.LogWarning($"[peace] loading overlay still up in {sc.name}, dismissing it");
                try { p.LoadingReady(0.3f); } catch (Exception e) { Plugin.Log.LogWarning("[peace] LoadingReady failed: " + e.Message); }
                UnityEngine.Object.Destroy(p.gameObject, 1.5f);
            }
        }
    }
}
