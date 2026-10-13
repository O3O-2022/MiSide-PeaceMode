using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiSidePeaceMode
{
    // Things around the house: the story's own clickable objects (Scene 4's and Scene 3's "Act Addon>Interactives",
    // what Mita says about the books, the mirror, the old TV...). Their conversations are read out of the story's
    // dialogue objects (a chain of Dialogue_3DText through nextText) as plain lines, so any Mita can say them.
    public partial class PeaceDirector
    {
        // topic ("Book", "Mirror", ...) -> the story's conversation about it
        readonly Dictionary<string, List<Line>> storyTopics3 = new Dictionary<string, List<Line>>();
        readonly Dictionary<string, List<Line>> storyTopics4 = new Dictionary<string, List<Line>>();

        // the language file the story's lines of a scene come from ("LocationDialogue Location4" ...)
        static string DialogueFile(int scene) => "LocationDialogue Location" + scene;

        static string LineText(string file, int index)
        {
            try
            {
                var t = GlobalLanguage.GetString(file, index - 1);
                return string.IsNullOrEmpty(t) || t == "???" ? null : t;   // ("???": an empty line there)
            }
            catch { return null; }
        }

        // every "Dialogue X" under the story's Mita and player dialogue roots, followed from its first line
        static Dictionary<string, List<Line>> ReadTopics(Transform mitaRoot, Transform playerRoot, string file, StringBuilder log)
        {
            var result = new Dictionary<string, List<Line>>();
            var groups = new Dictionary<string, List<Transform>>();
            foreach (var root in new[] { mitaRoot, playerRoot })
            {
                if (root == null) continue;
                for (int i = 0; i < root.childCount; i++)
                {
                    var g = root.GetChild(i);
                    var name = g.name.StartsWith("Dialogue ") ? g.name.Substring(9) : g.name;
                    if (!groups.TryGetValue(name, out var l)) groups[name] = l = new List<Transform>();
                    l.Add(g);
                }
            }
            foreach (var kv in groups)
            {
                var all = new List<Dialogue_3DText>();
                foreach (var g in kv.Value) foreach (var d in g.GetComponentsInChildren<Dialogue_3DText>(true)) if (d != null) all.Add(d);
                if (all.Count == 0) continue;
                var mine = new HashSet<IntPtr>();
                foreach (var d in all) mine.Add(d.gameObject.Pointer);
                // the first line: one nothing points at (by nextText or by switching it on), preferring the active one
                var pointed = new HashSet<IntPtr>();
                foreach (var d in all)
                {
                    if (d.nextText != null) pointed.Add(d.nextText.Pointer);
                    foreach (var go in SwitchedOn(d)) pointed.Add(go.Pointer);
                }
                Dialogue_3DText start = null;
                foreach (var d in all) if (!pointed.Contains(d.gameObject.Pointer) && d.gameObject.activeSelf) { start = d; break; }
                if (start == null) foreach (var d in all) if (!pointed.Contains(d.gameObject.Pointer)) { start = d; break; }
                if (start == null) continue;
                var lines = new List<Line>();
                var seen = new HashSet<IntPtr>();
                string pendingEmo = null;
                var d0 = start;
                while (d0 != null && seen.Add(d0.gameObject.Pointer) && lines.Count < 40)
                {
                    var text = LineText(file, d0.indexString);
                    var on = d0.gameObject.name;
                    // who says it: the theme the game draws it in (the Player theme is yours, every other one is hers);
                    // the object's name and its speaker only when the theme is unknown
                    string theme = "";
                    try { theme = d0.themeDialogue.ToString(); } catch { }
                    bool mita = theme.Length > 0 ? theme != "Player"
                              : on.StartsWith("DPlayer") || on.StartsWith("Player") ? false : on.StartsWith("DMita") || on.StartsWith("Mita") || d0.speaker != null;
                    string emo = d0.emotionStart != EmotionType.none ? d0.emotionStart.ToString() : pendingEmo;
                    pendingEmo = FaceIn(d0.eventFinishPrint) ?? FaceIn(d0.eventFinish);
                    if (text != null) lines.Add(new Line(mita ? Who.Mita : Who.Player, text, mita ? emo : null));
                    log?.AppendLine($"   {(mita ? "M" : "P")} #{d0.indexString} {d0.gameObject.name} theme={theme} speaker={(d0.speaker != null ? "y" : "-")} emo={emo}: {text}  | {SceneDumper.EventString(d0.eventFinish)}");
                    Dialogue_3DText next = d0.nextText != null ? d0.nextText.GetComponent<Dialogue_3DText>() : null;
                    if (next == null)
                        foreach (var go in SwitchedOn(d0))
                        {
                            var nd = go.GetComponent<Dialogue_3DText>();
                            if (nd != null && mine.Contains(go.Pointer)) { next = nd; break; }
                        }
                    d0 = next;
                }
                if (lines.Count > 0) result[kv.Key] = lines;
                log?.AppendLine($"== {kv.Key}: {lines.Count} lines (of {all.Count})");
            }
            return result;
        }

        // the GameObjects a line's end switches on (the story's way of going on to the next line after an action)
        static IEnumerable<GameObject> SwitchedOn(Dialogue_3DText d)
        {
            foreach (var ev in new[] { d.eventFinish, d.eventFinishPrint })
            {
                var calls = ev?.m_PersistentCalls?.m_Calls;
                if (calls == null) continue;
                foreach (var c in calls)
                {
                    if (c.m_MethodName == "SetActive" && c.m_Arguments != null && c.m_Arguments.m_BoolArgument && c.m_Target != null)
                    {
                        var go = c.m_Target.TryCast<GameObject>();
                        if (go != null) yield return go;
                    }
                    // (she walks somewhere first: the line to go on with, once she is there)
                    else if (c.m_MethodName == "TakeEventWhenReadyWalk" && c.m_Arguments != null && c.m_Arguments.m_ObjectArgument != null)
                    {
                        var o = c.m_Arguments.m_ObjectArgument;
                        var go = o.TryCast<GameObject>() ?? o.TryCast<Component>()?.gameObject;
                        if (go != null) yield return go;
                    }
                }
            }
        }

        static string FaceIn(UnityEngine.Events.UnityEventBase ev)
        {
            var calls = ev?.m_PersistentCalls?.m_Calls;
            if (calls == null) return null;
            foreach (var c in calls)
                if ((c.m_MethodName == "FaceEmotion" || c.m_MethodName == "FaceEmotionFast") && c.m_Arguments != null && !string.IsNullOrEmpty(c.m_Arguments.m_StringArgument))
                    return c.m_Arguments.m_StringArgument.ToLowerInvariant();
            return null;
        }

        static Transform FindUnder(Scene s, string path)
        {
            var parts = path.Split('>');
            foreach (var r in s.GetRootGameObjects())
            {
                var t = Util.FindDeep(r.transform, parts[0]);
                for (int i = 1; t != null && i < parts.Length; i++) t = t.Find(parts[i]);
                if (t != null) return t;
            }
            return null;
        }

        // Scene 3's conversations, read while it is loaded (see ImportCooking)
        StringBuilder houseLog3;
        // Scene 3's clickable things, carried into this house (name, where, collider size)
        readonly List<(string name, Vector3 pos, Quaternion rot, Vector3 size, Vector3 center)> spots3 = new List<(string, Vector3, Quaternion, Vector3, Vector3)>();

        void ReadScene3Topics(Scene s, Scene here)
        {
            var dl = FindUnder(s, "Act Addon>Dialogues");
            var mitaRoot = dl != null ? dl.Find("Dialogue Mita") : null;
            var playerRoot = dl != null ? dl.Find("Dialogue Player") : null;
            houseLog3 = DevRunner.Active ? new StringBuilder() : null;
            foreach (var kv in ReadTopics(mitaRoot, playerRoot, DialogueFile(3), houseLog3)) storyTopics3[kv.Key] = kv.Value;
            {
                var inter = FindUnder(s, "Act Addon>Interactives");
                // (by the houses themselves: Scene 3's kitchen table stands turned)
                if (!Anchor(s, here, "HouseGame Tamagotchi", out var move, out var turn)) { move = p => p; turn = Quaternion.identity; }
                if (inter != null)
                    foreach (var oi in inter.GetComponentsInChildren<ObjectInteractive>(true))
                    {
                        var box = oi.GetComponent<BoxCollider>();
                        var t = oi.transform;
                        spots3.Add((oi.name, move(t.position), turn * t.rotation, box != null ? Vector3.Scale(box.size, t.lossyScale) : Vector3.one * 0.4f, box != null ? box.center : Vector3.zero));
                        houseLog3?.AppendLine($"interactive {oi.name} at {t.position} -> {move(t.position)} click: {SceneDumper.EventString(oi.eventClick)}");
                    }
            }
            Plugin.Log.LogInfo($"[house] Scene 3 conversations: {storyTopics3.Count}");
            BorrowGlasses(s, here);
        }

        void ReadScene4Topics()
        {
            // (the free-time conversations' roots: where the wall clock's is)
            var mitaRoot = Util.FindPath("Dialogue Mita>Dialogue WallClock")?.transform.parent;
            var playerRoot = Util.FindPath("Dialogue Player>Dialogue WallClock")?.transform.parent;
            var log = DevRunner.Active ? new StringBuilder() : null;
            foreach (var kv in ReadTopics(mitaRoot, playerRoot, DialogueFile(4), log)) storyTopics4[kv.Key] = kv.Value;
            if (log != null)
            {
                var inter = Util.FindPath("Addon>Interactives")?.transform;
                if (inter != null)
                    foreach (var oi in inter.GetComponentsInChildren<ObjectInteractive>(true))
                    {
                        var ci = oi.GetComponentInChildren<ObjectInteractive_CaseInfo>(true);
                        log.AppendLine($"interactive {oi.name} at {oi.transform.position} label #{(ci != null ? ci.indexString : -1)} click: {SceneDumper.EventString(oi.eventClick)}");
                    }
                try { System.IO.File.WriteAllText(System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "house4.txt"), $"roots {Util.PathOf(mitaRoot)} / {Util.PathOf(playerRoot)}\n" + log, Encoding.UTF8); } catch { }
            }
            Plugin.Log.LogInfo($"[house] Scene 4 conversations: {storyTopics4.Count}");
        }

        public void DevHouseDump()
        {
            ReadScene4Topics();
            // which of Scene 3's things this house has too: the nearest mesh to each
            var here = SceneManager.GetActiveScene();
            foreach (var sp in spots3)
            {
                string best = "-"; float bd = 9f;
                foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<MeshRenderer>()))
                {
                    var mr = o.Cast<MeshRenderer>();
                    if (mr == null || mr.gameObject.scene != here) continue;
                    var b = mr.bounds;
                    if (b.extents.magnitude > 1.5f) continue;   // (walls, floors, the whole-house shadow)
                    float d = Vector3.Distance(b.ClosestPoint(sp.pos), sp.pos);
                    if (d < bd) { bd = d; best = Util.PathOf(mr.transform); }
                }
                houseLog3?.AppendLine($"spot {sp.name} at {sp.pos}: nearest {best} ({bd:0.00} m)");
            }
            if (houseLog3 != null)
                try { System.IO.File.WriteAllText(System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "house3.txt"), houseLog3.ToString(), Encoding.UTF8); } catch { }
        }
    }
}
