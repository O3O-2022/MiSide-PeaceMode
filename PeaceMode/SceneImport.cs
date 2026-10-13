using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MiSidePeaceMode
{
    // Borrows objects from another story scene (Scene 3's cooking corner, ...): the scene is loaded additively and its
    // World root is switched off from inside World.Awake (the first thing that wakes up in a story scene; its other root
    // components only have Start), so nothing of its story ever starts. On the next Tick a callback moves what it needs
    // into the peace scene and the rest is unloaded again.
    // (SceneManager.sceneLoaded is stripped from this build, hence the Awake hook.)
    [HarmonyPatch]
    public static class SceneImport
    {
        static string loading;
        static Action<Scene> onLoaded;
        static bool awoke;
        static float giveUpAt;
        public static bool Busy => loading != null;

        public static bool Load(string scene, Action<Scene> loaded)
        {
            if (loading != null) return false;
            loading = scene;
            onLoaded = loaded;
            awoke = false;
            giveUpAt = Time.realtimeSinceStartup + 120f;
            Plugin.Log.LogInfo("[import] loading " + scene);
            try { SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive); }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[import] LoadSceneAsync(mode): " + e.Message + ", trying LoadSceneParameters");
                try { SceneManager.LoadSceneAsync(scene, new LoadSceneParameters(LoadSceneMode.Additive)); }
                catch (Exception e2) { Plugin.Log.LogError("[import] cannot load " + scene + ": " + e2.Message); loading = null; onLoaded = null; return false; }
            }
            return true;
        }

        // called every frame by the director
        public static void Tick()
        {
            if (loading == null) return;
            Scene s = default;
            bool found = false;
            for (int i = 0; i < SceneManager.sceneCount && !found; i++) { s = SceneManager.GetSceneAt(i); found = s.name == loading; }
            if (!awoke || !found || !s.isLoaded)
            {
                if (Time.realtimeSinceStartup > giveUpAt) { Plugin.Log.LogWarning("[import] gave up waiting for " + loading); loading = null; onLoaded = null; }
                return;
            }
            foreach (var r in s.GetRootGameObjects()) r.SetActive(false);
            var cb = onLoaded;
            var name = loading;
            loading = null; onLoaded = null;
            try { cb?.Invoke(s); } catch (Exception e) { Plugin.Log.LogError("[import] " + e); }
            try { SceneManager.UnloadSceneAsync(s); }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[import] UnloadSceneAsync(scene): " + e.Message);
                try { SceneManager.UnloadSceneAsync(name); } catch (Exception e2) { Plugin.Log.LogError("[import] cannot unload " + name + ": " + e2.Message); }
            }
            Plugin.Log.LogInfo("[import] " + name + " borrowed and unloaded");
        }

        // Moves a borrowed object (keeping its world position) into the given scene, as a root object.
        public static GameObject Take(Transform t, Scene into)
        {
            if (t == null) return null;
            t.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(t.gameObject, into);
            return t.gameObject;
        }

        // Drops every persistent event call of the borrowed objects that points into the rest of their old scene
        // (the story's quests, its World): those objects are about to be unloaded. Call before the move.
        public static int StripOutside(GameObject kept, Scene from)
        {
            int removed = 0;
            foreach (var comp in kept.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                var type = SceneDumper.FindType(comp.GetIl2CppType().Name);
                if (type == null) continue;
                object wrapper;
                try { wrapper = Activator.CreateInstance(type, new object[] { comp.Pointer }); } catch { continue; }
                foreach (var p in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (!typeof(UnityEventBase).IsAssignableFrom(p.PropertyType)) continue;
                    UnityEventBase ev;
                    try { ev = p.GetValue(wrapper) as UnityEventBase; } catch { continue; }
                    var calls = ev?.m_PersistentCalls?.m_Calls;
                    if (calls == null) continue;
                    bool changed = false;
                    for (int i = calls.Count - 1; i >= 0; i--)
                    {
                        var tgt = calls[i].m_Target;
                        if (tgt == null) continue;
                        var go = tgt.TryCast<GameObject>() ?? tgt.TryCast<Component>()?.gameObject;
                        if (go == null || go.scene != from || go.transform.IsChildOf(kept.transform)) continue;
                        Plugin.Log.LogInfo($"[import] dropped {comp.name}.{p.Name} -> {go.name}.{calls[i].m_MethodName}");
                        calls.RemoveAt(i); removed++; changed = true;
                    }
                    if (changed) ev.DirtyPersistentCalls();
                }
            }
            return removed;
        }

        // Every persistent event a component has: its UnityEvents, lists of them (an animation player's per-clip events)
        // and the timed points of a Time_Events.
        public static System.Collections.Generic.List<(string name, UnityEventBase ev)> Events(Component comp)
        {
            var list = new System.Collections.Generic.List<(string, UnityEventBase)>();
            var type = SceneDumper.FindType(comp.GetIl2CppType().Name);
            if (type == null) return list;
            object wrapper;
            try { wrapper = Activator.CreateInstance(type, new object[] { comp.Pointer }); } catch { return list; }
            foreach (var p in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                object v;
                try { v = p.GetValue(wrapper); } catch { continue; }
                if (v is UnityEventBase ev) list.Add((p.Name, ev));
                else if (v is Il2CppSystem.Collections.Generic.List<UnityEvent> evs) { for (int i = 0; i < evs.Count; i++) list.Add(($"{p.Name}[{i}]", evs[i])); }
                else if (v is Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<TimePoint> tps)
                {
                    for (int i = 0; i < tps.Length; i++) if (tps[i] != null) list.Add(($"{p.Name}[{i}]@{tps[i].time:0.##}", tps[i]._event));
                }
            }
            return list;
        }

        // Like StripOutside, but a call into the rest of the old scene can be pointed at one of ours instead (her animator
        // instead of the story Mita's, our World...): map returns the new target, or null to drop the call.
        public static int Retarget(GameObject kept, Scene from, Func<UnityEngine.Object, UnityEngine.Object> map, System.Text.StringBuilder log)
        {
            int moved = 0;
            foreach (var comp in kept.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                foreach (var (name, ev) in Events(comp))
                {
                    var calls = ev?.m_PersistentCalls?.m_Calls;
                    if (calls == null || calls.Count == 0) continue;
                    bool changed = false;
                    for (int i = calls.Count - 1; i >= 0; i--)
                    {
                        var c = calls[i];
                        var tgt = c.m_Target;
                        var go = tgt == null ? null : tgt.TryCast<GameObject>() ?? tgt.TryCast<Component>()?.gameObject;
                        string what = $"{comp.name}({comp.GetIl2CppType().Name}).{name}: {(tgt != null ? tgt.name + "(" + tgt.GetIl2CppType().Name + ")" : "null")}.{c.m_MethodName}";
                        if (go == null || go.scene != from || go.transform.IsChildOf(kept.transform)) { log?.AppendLine("   " + what); continue; }
                        var nt = map(tgt);
                        if (nt != null) { c.m_Target = nt; log?.AppendLine("   " + what + "  -> ours"); moved++; }
                        else { calls.RemoveAt(i); log?.AppendLine("   " + what + "  -> dropped"); }
                        changed = true;
                    }
                    if (changed) ev.DirtyPersistentCalls();
                }
            }
            return moved;
        }

        // The borrowed scene's World wakes first: switch the whole scene off right there and skip its setup.
        [HarmonyPatch(typeof(World), nameof(World.Awake)), HarmonyPrefix]
        static bool WorldAwake(World __instance)
        {
            if (loading == null || __instance.gameObject.scene.name != loading) return true;
            __instance.transform.root.gameObject.SetActive(false);
            awoke = true;
            Plugin.Log.LogInfo("[import] " + loading + " woke up: switched off before its story starts");
            return false;
        }
    }
}
