using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using EPOOutline;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // Things around the house you can look at with Mita: the scene's own ones (the photo, the clock, the shoes...,
    // their clicks taken over), Scene 3's (the books, the umbrella, the school uniform...: their outlined label is a
    // copy of the photo's) and a few more of the house's things. She comes over and talks about each one in her own
    // way; the original Mita says the story's own lines where the story has them (see PeaceHouse).
    public partial class PeaceDirector
    {
        class Spot
        {
            public string id, path, label, story;
            public int scene;          // the story's conversation: 3 or 4 (0 = none)
            public bool native;        // one of this scene's own interactives (path under Addon)
            public GameObject go;
        }

        static readonly Spot[] SpotList =
        {
            new Spot { id = "photo", path = "Interactive PhotoMita", native = true, story = "PhotoMita", scene = 4 },
            new Spot { id = "clock", path = "Interactive WallClock", native = true, story = "WallClock", scene = 4 },
            new Spot { id = "shoes", path = "Interactive BlueShoes", native = true, story = "BlueShoes", scene = 4 },
            new Spot { id = "bedmirror", path = "Interactive MirrorBedroom", native = true, story = "BedroomMirror", scene = 4 },
            new Spot { id = "portal", path = "Interactive Portal", native = true, story = "Portal", scene = 4 },
            new Spot { id = "door", path = "Interactive DoorExit", native = true, story = "DoorError", scene = 4 },
            new Spot { id = "electro", path = "Main>Electrical panel", label = "배전함", story = "Electro", scene = 3 },
            new Spot { id = "books", path = "Main>LivingRoomBookScaff", label = "책장", story = "Book", scene = 3 },
            new Spot { id = "umbrella", path = "Main>UmbrellaBucket", label = "우산꽂이", story = "Umbr", scene = 3 },
            new Spot { id = "oldtv", path = "Kitchen>KitchenTv", label = "작은 TV", story = "OldTelevision", scene = 3 },
            new Spot { id = "pc", path = "Bedroom>PC", label = "컴퓨터", story = "PC", scene = 3 },
            new Spot { id = "figure", path = "Bedroom>ChibiAnita", label = "피규어", story = "Anita", scene = 3 },
            new Spot { id = "uniform", path = "Bedroom>SchoolForm", label = "교복", story = "AddCloth", scene = 3 },
            new Spot { id = "fridge", path = "Kitchen>Fridge", label = "냉장고" },
            new Spot { id = "teapot", path = "Kitchen>Kitchen Teapot", label = "주전자" },
            new Spot { id = "teddy", path = "Bedroom>Teddy", label = "곰 인형" },
            new Spot { id = "recorder", path = "Bedroom>Recorder", label = "카세트 플레이어" },
            new Spot { id = "moonlamp", path = "Bedroom>MoonLamp", label = "달 조명" },
            new Spot { id = "cactus", path = "Bedroom>Cactus", label = "선인장" },
            new Spot { id = "speakers", path = "Main>Loudspeakers", label = "스피커" },
            new Spot { id = "duck", path = "Toilet>BathDuck", label = "고무 오리" },
            new Spot { id = "bathmirror", path = "Toilet>MirrorToilet", label = "거울" },
        };

        readonly List<Spot> spots = new List<Spot>();
        bool spotsOn = true;

        void SetupSpots()
        {
            var photo = Util.FindPath("Addon>Interactive PhotoMita");
            int made = 0, taken = 0;
            foreach (var sp in SpotList)
            {
                try
                {
                    if (sp.native)
                    {
                        var go = Util.FindPath("Addon>" + sp.path);
                        var oi = go != null ? go.GetComponent<ObjectInteractive>() : null;
                        if (oi == null) { Plugin.Log.LogWarning("[spot] no " + sp.path); continue; }
                        var s = sp;
                        oi.eventClick = new UnityEvent();
                        oi.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => OnSpot(s))));
                        sp.go = go;
                        spots.Add(sp);
                        taken++;
                        continue;
                    }
                    if (photo == null) continue;
                    var target = Util.FindPath("HouseGame Tamagotchi>House>" + sp.path);
                    if (target == null) { Plugin.Log.LogWarning("[spot] no " + sp.path); continue; }
                    // the thing's size from its meshes (its room may be switched off: no renderer bounds then)
                    var rends = target.GetComponentsInChildren<MeshRenderer>(true);
                    bool any = false;
                    var b = new Bounds();
                    foreach (var r in rends)
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null) continue;
                        var mb = mf.sharedMesh.bounds;
                        for (int c = 0; c < 8; c++)
                        {
                            var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                            var w = r.transform.TransformPoint(corner);
                            if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
                        }
                    }
                    if (!any) { Plugin.Log.LogWarning("[spot] no mesh on " + sp.path); continue; }
                    var room = target.transform.parent;
                    var clone = Instantiate(photo, room);
                    clone.name = "Interactive " + sp.id + " (Peace)";
                    var t = clone.transform;
                    var pls = photo.transform.lossyScale;
                    var rls = room.lossyScale;
                    t.localScale = new Vector3(pls.x / Mathf.Max(Mathf.Abs(rls.x), 0.0001f), pls.y / Mathf.Max(Mathf.Abs(rls.y), 0.0001f), pls.z / Mathf.Max(Mathf.Abs(rls.z), 0.0001f));
                    t.rotation = Quaternion.identity;
                    t.position = b.center;
                    var box = clone.GetComponent<BoxCollider>();
                    if (box != null)
                    {
                        var ws = t.lossyScale;
                        var size = b.size + Vector3.one * 0.06f;
                        box.center = Vector3.zero;
                        box.size = new Vector3(size.x / Mathf.Max(Mathf.Abs(ws.x), 0.0001f), size.y / Mathf.Max(Mathf.Abs(ws.y), 0.0001f), size.z / Mathf.Max(Mathf.Abs(ws.z), 0.0001f));
                    }
                    var coi = clone.GetComponent<ObjectInteractive>();
                    if (coi != null)
                    {
                        var s = sp;
                        coi.eventClick = new UnityEvent();
                        coi.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => OnSpot(s))));
                        coi.destroyObjectInteractive = false; coi.deactiveObject = false; coi.destroyComponent = false;
                    }
                    var info = clone.GetComponentInChildren<ObjectInteractive_CaseInfo>(true);
                    if (info != null) info.indexString = TextHook.Register(Loc.Cap(Loc.T(sp.label)));
                    var outl = clone.GetComponent<Outlinable>();
                    if (outl != null)
                    {
                        outl.OutlineTargets.Clear();
                        foreach (var r in rends) if (r != null) outl.OutlineTargets.Add(new OutlineTarget(r, 0));
                    }
                    clone.SetActive(true);
                    sp.go = clone;
                    spots.Add(sp);
                    made++;
                }
                catch (Exception e) { Plugin.Log.LogWarning($"[spot] {sp.id}: {e.Message}"); }
            }
            Plugin.Log.LogInfo($"[spot] house things: {made} added, {taken} of the scene's own taken over");
        }

        // ours go quiet while something else is going on (the scene's own ones are switched by the roaming addon)
        void SetSpots(bool on)
        {
            if (on == spotsOn) return;
            spotsOn = on;
            foreach (var sp in spots) if (!sp.native && sp.go != null && sp.go.activeSelf != on) sp.go.SetActive(on);
        }

        [HideFromIl2Cpp] void OnSpot(Spot sp)
        {
            if (!freeMode || busy || MenuOpen || fadeTarget > 0 || cardGame) return;
            Plugin.Log.LogInfo("[spot] " + sp.id);
            if (activity == MitaActivity.Sleeping)
            {
                Play(new[] { new Line(Who.Player, "…미타가 깨지 않게 조용히 보자.") }, null, true);
                return;
            }
            if (RequestSpot(sp)) return;   // what she asked you for
            Did("spots");
            var lines = SpotLines(sp);
            if (lines == null || lines.Count == 0) return;
            bool firstToday = S.SeenDay != S.Day || !S.SeenToday.Contains(sp.id);
            Play(lines, () =>
            {
                if (S.SeenDay != S.Day) { S.SeenDay = S.Day; S.SeenToday.Clear(); }
                if (firstToday) { S.SeenToday.Add(sp.id); AddAff(1); }
                S.Seen.Add(sp.id);
                CheckTour();
                CheckMilestones();
            });
        }

        // the original Mita says the story's lines when the story has some; the others (and she, elsewhere) their own
        List<Line> SpotLines(Spot sp)
        {
            if (Pn.Id == "crazy" && sp.scene != 0)
            {
                var src = sp.scene == 3 ? storyTopics3 : storyTopics4;
                if (src.TryGetValue(sp.story, out var story) && story.Count > 1) return story;
            }
            var own = Personas.House(Pn.Id, sp.id);
            if (own != null && own.Length > 0) return Pick(own).ToList();
            return null;
        }

        // every thing in the house seen once: a line of hers, and a little affection
        void CheckTour()
        {
            if (S.Seen.Contains("#tour")) return;
            foreach (var sp in spots) if (!S.Seen.Contains(sp.id)) return;
            S.Seen.Add("#tour");
            AddAff(5);
            var t = Personas.HouseTour(Pn.Id);
            if (t != null) Play(t);
        }

        public void DevSpot(string id)
        {
            foreach (var sp in spots) if (sp.id == id) { OnSpot(sp); return; }
            Plugin.Log.LogWarning("[spot] no " + id);
        }
        public string DevSpots() => string.Join(", ", spots.Select(s => $"{s.id}{(s.go != null && s.go.activeInHierarchy ? "" : "(off)")}@{(s.go != null ? s.go.transform.position.ToString("F1") : "-")}"));
    }
}
