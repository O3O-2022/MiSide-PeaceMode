using HarmonyLib;
using UnityEngine.SceneManagement;

namespace MiSidePeaceMode
{
    // Dev-only call tracing to understand the game's scene flow. Patched only when DevRunner is active.
    public static class TraceHooks
    {
        static void L(string s) => Plugin.Log.LogInfo($"[trace] {s} | LoadingLevel={GlobalGame.LoadingLevel} levelLoad={GlobalGame.levelLoad} scene={SceneManager.GetActiveScene().name}");

        public static void Apply(Harmony h)
        {
            void P(System.Type t, string m, System.Type[] args, string pre = null, string post = null)
            {
                try
                {
                    var mi = args == null ? AccessTools.Method(t, m) : AccessTools.Method(t, m, args);
                    if (mi == null) { Plugin.Log.LogWarning($"[trace] no method {t.Name}.{m}"); return; }
                    h.Patch(mi,
                        pre != null ? new HarmonyMethod(typeof(TraceHooks), pre) : null,
                        post != null ? new HarmonyMethod(typeof(TraceHooks), post) : null);
                }
                catch (System.Exception e) { Plugin.Log.LogWarning($"[trace] patch fail {t.Name}.{m}: {e.Message}"); }
            }
            P(typeof(Menu), nameof(Menu.ButtonContinue), null, nameof(PreGeneric));
            P(typeof(Menu), nameof(Menu.ButtonLoadScene), new[] { typeof(string) }, nameof(PreLoadScene));
            P(typeof(Menu), nameof(Menu.NextLevel), null, nameof(PreGeneric));
            P(typeof(SceneLoading), nameof(SceneLoading.Start), null, nameof(PreGeneric));
            P(typeof(SceneLoading), nameof(SceneLoading.LoadGo), null, nameof(PreGeneric));
            P(typeof(SceneLoading), nameof(SceneLoading.StopLoad), null, nameof(PreGeneric));
            P(typeof(World), nameof(World.Awake), null, nameof(PreGeneric));
            P(typeof(World), nameof(World.Start), null, nameof(PreGeneric));
            P(typeof(GlobalAM), nameof(GlobalAM.LoadData), new[] { typeof(string) }, null, nameof(PostLoadData));
            P(typeof(GlobalAM), nameof(GlobalAM.ExistsData), new[] { typeof(string) }, null, nameof(PostExists));
            P(typeof(SceneManager), nameof(SceneManager.LoadScene), new[] { typeof(string) }, nameof(PreSceneLoad));
            P(typeof(GlobalLanguage), nameof(GlobalLanguage.GetString), new[] { typeof(string), typeof(int) }, null, nameof(PostGetString));
            P(typeof(SceneManager), nameof(SceneManager.LoadSceneAsync), new[] { typeof(string) }, nameof(PreSceneLoad));
            P(typeof(SceneManager), nameof(SceneManager.LoadSceneAsync), new[] { typeof(string), typeof(LoadSceneMode) }, nameof(PreSceneLoad));
        }

        static void PreGeneric(System.Reflection.MethodBase __originalMethod) => L(__originalMethod.DeclaringType.Name + "." + __originalMethod.Name);
        static void PreLoadScene(string _nameScene) => L("Menu.ButtonLoadScene(" + _nameScene + ")");
        static void PreSceneLoad(string sceneName) => L("SceneManager.Load(" + sceneName + ")");
        static void PostLoadData(string path, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray __result)
            => L($"GlobalAM.LoadData({path}) -> {(__result == null ? "null" : string.Join("\\n", (string[])__result))}");
        static void PostGetString(string _name, int _string, string __result) => Plugin.Log.LogInfo($"[trace] GetString({_name}, {_string}) -> {__result}");
        static void PostExists(string path, bool __result) => L($"GlobalAM.ExistsData({path}) -> {__result}");
    }
}
