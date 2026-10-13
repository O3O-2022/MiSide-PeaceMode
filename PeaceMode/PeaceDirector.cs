using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using EPOOutline;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Random = UnityEngine.Random;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    [HarmonyPatch]
    public static class DirectorHook
    {
        [HarmonyPatch(typeof(World), nameof(World.Start)), HarmonyPostfix]
        static void WorldStart()
        {
            if (!PeaceMode.Active) return;
            if (SceneManager.GetActiveScene().name != PeaceMode.Scene) { PeaceMode.Active = false; Loc.UseFont(false); return; }
            if (PeaceDirector.Instance != null) return;
            new GameObject("PeaceDirector").AddComponent<PeaceDirector>();
        }

        // Mita's numbered animation events: the Scene 3 clips she borrows (eating, cooking) fire slots that in this
        // scene pick up and drop the TV game's gamepad and remote
        [HarmonyPatch(typeof(Animator_FunctionsOverride), nameof(Animator_FunctionsOverride.NewEvent)), HarmonyPrefix]
        static bool MitaAnimEvent(Animator_FunctionsOverride __instance, int x)
        {
            var d = PeaceDirector.Instance;
            return d == null || !d.MutesAnimEvent(__instance, x);
        }
    }

    // Runs peace mode inside "Scene 4 - StartSecret": keeps the story's horror beats switched off,
    // drives time of day, and gives the player a menu of things to do with Mita.
    public partial class PeaceDirector : MonoBehaviour
    {
        public PeaceDirector(IntPtr ptr) : base(ptr) { }

        public static PeaceDirector Instance;
        static PeaceSave S => PeaceMode.Save;
        static Persona Pn => PeaceMode.Persona;
        static LineSet L => PeaceMode.Persona.L;
        static string ShortName => Loc.T(PeaceMode.Persona.Short);   // her name in the language of the moment

        PeaceUI ui;
        World world;
        MitaPerson mita;
        Transform mitaBody;
        Animator_FunctionsOverride mitaAnim;
        Character_Look mitaLook;
        Location34_Communication addon;
        PlayerMove pm;
        GameController gc;
        Light sun;
        float sunInt0;
        Color sunCol0;
        Transform dlgMitaParent, dlgPlayerParent;
        GameObject tmplMita, tmplPlayer;
        readonly List<GameObject> storyOff = new List<GameObject>();
        readonly Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        int initFrames;
        bool ready, freeMode, greeted;
        float guardTimer, saveTimer, idleNext;
        int lastEventHour = -1;
        readonly Queue<string> recentTopics = new Queue<string>();
        float lastPat = -100, lastHug = -100, lastCompliment = -100;

        // dialogue runner
        readonly Queue<Line> queue = new Queue<Line>();
        Action queueDone;
        GameObject current;
        bool currentIsMita;
        GameObject mitaSpeaker;
        float currentDeadline;
        bool busy, lightSeq;
        float quietUntil;

        // lighting
        Color ambBase, ambSet;
        float ambIntBase, ambIntSet;
        bool ambInit;

        // fade
        float fadeVal, fadeTarget;
        Action afterFade;
        float fadeHoldUntil;   // (a fade can wait a moment before it starts)
        bool inBedTalk;        // (the two of you in bed, before she falls asleep)

        bool controlsHeld;

        GameObject templates;          // inactive holder for our private template copies
        GameObject mitaInteractive;    // game-native "look at Mita + click" interaction
        ObjectInteractive mitaOI;
        // the dub language, swapped out for the whole of peace mode: our own lines have no recordings and are spoken in
        // the game's "no dub" babble, and the story's own lines we keep (the card game, the TV console) now sound the same
        // instead of suddenly speaking in the dub's language. Put back on leaving.
        string savedVoice;
        bool voiceSwapped;
        Outlinable mitaOutline;
        MitaSkin skin;                 // another Mita version's model worn over the scene's Mita

        void Start()
        {
            Instance = this;
            // Application.onBeforeRender is stripped from this build: sync the skin at the end of each frame instead,
            // after every LateUpdate (so after Mita's look-at IK has turned her head)
            StartCoroutine(EndOfFrameLoop().WrapToIl2Cpp());
            if (S == null || S.Id != Pn.Id) PeaceMode.Save = PeaceSave.Load(Pn.Id);
            // until this session records how it ended, the next launch will treat it as killed/crashed
            S.Running = true;
            S.ExitKind = "";
            S.Sessions++;
            S.Write();
            sessionStart = lastInput = Time.realtimeSinceStartup;
            Plugin.Log.LogInfo("[peace] director started");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // scene unloaded without the app closing: back to the main menu (our menu or the pause menu)
            if (S != null && !appQuitting) { S.Running = false; if (string.IsNullOrEmpty(S.ExitKind)) S.ExitKind = "menu"; }
            // also when leaving through the pause menu's own "main menu" button, not just our goodbye
            if (!appQuitting) OverlayGuard.ReturningFromPeace = true;
            S?.Write();
            ui?.Destroy();
            if (controlsHeld) HoldControls(false);
            RestoreVoice();
        }

        // ---------------------------------------------------------------- setup

        void Init()
        {
            ready = true;
            MuteDub();
            Loc.Init(PeaceMode.Language.Value);
            Loc.UseFont(true);
            world = FindObjectOfType<World>();
            mita = FindObjectOfType<MitaPerson>();
            mitaBody = mita != null ? (mita.nma != null ? mita.nma.transform : mita.transform) : null;
            mitaAnim = mitaBody != null ? mitaBody.GetComponent<Animator_FunctionsOverride>() : null;
            mitaLook = mitaBody != null ? mitaBody.GetComponentInChildren<Character_Look>(true) : null;
            addon = FindObjectOfType<Location34_Communication>();
            pm = FindObjectOfType<PlayerMove>();
            gc = FindObjectOfType<GameController>();
            var sunGo = Util.FindPath("World>Sun");
            sun = sunGo != null ? sunGo.GetComponent<Light>() : null;
            if (sun != null) { sunInt0 = sun.intensity; sunCol0 = sun.color; }

            dlgMitaParent = Util.FindPath("Quest 1>Dialogue Mita")?.transform;
            dlgPlayerParent = Util.FindPath("Quest 1>Dialogue Player")?.transform;
            // private copies: the story dialogue objects get destroyed/disabled as the scene runs
            templates = new GameObject("PeaceTemplates");
            templates.SetActive(false);
            var srcMita = NativeMitaLine(dlgMitaParent) ?? Util.FindPath("Dialogue Mita>Dialogue WallClock>DMita 4") ?? FirstDialogue(dlgMitaParent);
            // (the story keeps her lines and yours under one root: the player's template must be one in the Player theme,
            // or your lines would come out in her colour, from her mouth)
            var srcPlayer = NativePlayerLine(dlgPlayerParent) ?? NativePlayerLine(null) ?? FirstDialogue(dlgPlayerParent);
            tmplMita = srcMita != null ? Instantiate(srcMita, templates.transform) : null;
            tmplPlayer = srcPlayer != null ? Instantiate(srcPlayer, templates.transform) : null;
            var pdt = tmplPlayer != null ? tmplPlayer.GetComponent<Dialogue_3DText>() : null;
            Plugin.Log.LogInfo($"[peace] player line template {srcPlayer?.name} theme={(pdt != null ? pdt.themeDialogue.ToString() : "-")} speaker={(pdt != null && pdt.speaker != null ? pdt.speaker.name : "-")}");
            var md = tmplMita != null ? tmplMita.GetComponent<Dialogue_3DText>() : null;
            if (md != null)
            {
                mitaSpeaker = md.speaker;
                // each Mita gets her own pitch of the no-dub babble (it plays from Mita's head audio source)
                var voiceSrc = md.sounds != null ? md.sounds.audioSource : null;
                if (voiceSrc != null) voiceSrc.pitch = Pn.VoicePitch;
                Plugin.Log.LogInfo($"[peace] mita line template {srcMita?.name} speaker={(md.speaker != null ? md.speaker.name : "-")} sounds={(md.sounds != null ? md.sounds.name : "-")}");
            }

            var cardsT = Util.FindDeepAll("Interactive Cards >>");
            var frame = cardsT != null ? cardsT.Find("Canvas/FrameInfo") : null;
            ui = new PeaceUI(frame != null ? frame.gameObject : null);
            SetupMitaInteractive();
            ApplySkin(Pn.Skin);
            try { if (mita != null && mita.nma != null) mita.nma.speed *= Pn.Speed; } catch { }

            foreach (var n in new[] { "World>Quest 2 OpenScarf", "World>Quest 3 ChibiHouse", "World>Time MitaLook On Scarf",
                                      "Dialogue Mita>Dialogue Hello", "Dialogue Mita>Dialogue Loading",
                                      "Dialogue Player>Dialogue Hello", "Dialogue Player>Dialogue Loading", "Quest 1>Loading" })
            {
                var g = Util.FindPath(n);
                if (g != null) storyOff.Add(g);
            }

            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimationClip>()))
            {
                if (o == null) continue;
                var n = o.name;
                if (n != null && n.StartsWith("Mita") && !clips.ContainsKey(n)) clips[n] = o.Cast<AnimationClip>();
            }

            int stripped = StripHorror() + StripAnimationEvents();
            RewireKitchen();
            HookCardTable();
            RememberStove();
            try { SetupAtmosphere(); } catch (Exception e) { Plugin.Log.LogError("[night] setup: " + e); }
            try { SetupMusic(); } catch (Exception e) { Plugin.Log.LogError("[music] setup: " + e); }
            try { ReadScene4Topics(); } catch (Exception e) { Plugin.Log.LogError("[house] Scene 4: " + e); }
            try { SetupSpots(); } catch (Exception e) { Plugin.Log.LogError("[spot] setup: " + e); }
            DressAsSaved();
            importAt = Time.time + 2f;

            Plugin.Log.LogInfo($"[peace] init: world={world != null} mita={mitaBody != null} anim={mitaAnim != null} look={mitaLook != null} addon={addon != null} " +
                               $"player={pm != null} gc={gc != null} sun={sun != null} tmplMita={tmplMita != null} tmplPlayer={tmplPlayer != null} " +
                               $"storyOff={storyOff.Count} clips={clips.Count} stripped={stripped}");
        }

        // Clone an existing interactive (photo on the wall) onto Mita: outline + label + click, all native.
        void SetupMitaInteractive()
        {
            try
            {
                var photo = Util.FindPath("Addon>Interactive PhotoMita");
                if (photo == null || mitaBody == null) { Plugin.Log.LogWarning("[peace] no interactive template for Mita"); return; }
                mitaInteractive = Instantiate(photo, mitaBody);
                mitaInteractive.name = "Interactive Mita (Peace)";
                // keep the same world scale as the wall photo so its label canvas stays the same size
                var t = mitaInteractive.transform;
                var pls = photo.transform.lossyScale;
                var mls = mitaBody.lossyScale;
                t.localScale = new Vector3(pls.x / Mathf.Max(mls.x, 0.0001f), pls.y / Mathf.Max(mls.y, 0.0001f), pls.z / Mathf.Max(mls.z, 0.0001f));
                t.localRotation = Quaternion.identity;
                t.position = mitaBody.position + Vector3.up * 0.9f;
                var box = mitaInteractive.GetComponent<BoxCollider>();
                if (box != null)
                {
                    var w = t.lossyScale;
                    box.center = Vector3.zero;
                    box.size = new Vector3(0.8f / Mathf.Max(Mathf.Abs(w.x), 0.0001f), 1.75f / Mathf.Max(Mathf.Abs(w.y), 0.0001f), 0.8f / Mathf.Max(Mathf.Abs(w.z), 0.0001f));
                }
                mitaOI = mitaInteractive.GetComponent<ObjectInteractive>();
                if (mitaOI != null)
                {
                    mitaOI.eventClick = new UnityEvent();
                    mitaOI.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnMitaClicked)));
                    mitaOI.destroyObjectInteractive = false;
                    mitaOI.deactiveObject = false;
                    mitaOI.destroyComponent = false;
                }
                var info = mitaInteractive.GetComponentInChildren<ObjectInteractive_CaseInfo>(true);
                if (info != null) info.indexString = TextHook.Register(Loc.F("{0}와 교감하기", ShortName));
                var outl = mitaInteractive.GetComponent<Outlinable>();
                mitaOutline = outl;
                RefreshOutline();
                mitaInteractive.SetActive(false);   // enabled once free mode starts
                Plugin.Log.LogInfo($"[peace] Mita interactive ready (oi={mitaOI != null} info={info != null} outline={outl != null})");
            }
            catch (Exception e) { Plugin.Log.LogError("[peace] Mita interactive failed: " + e); mitaInteractive = null; }
        }

        void RefreshOutline()
        {
            if (mitaOutline == null || mitaBody == null) return;
            mitaOutline.OutlineTargets.Clear();
            if (skin != null)
                foreach (var r in skin.Renderers) { if (r != null) mitaOutline.OutlineTargets.Add(new OutlineTarget(r, 0)); }
            else
                foreach (var smr in mitaBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    mitaOutline.OutlineTargets.Add(new OutlineTarget(smr, 0));
        }

        const string IdleSlot = "MitaIdle 1";   // idle clip slot of Mita's animator override
        AnimationClip originalIdle;

        void ApplySkin(string path)
        {
            skin?.Remove();
            skin = null;
            if (!string.IsNullOrEmpty(path) && mitaBody != null)
            {
                try { skin = MitaSkin.Apply(mitaBody, path, t => mitaInteractive != null && t == mitaInteractive.transform, Pn.SkinScale); }
                catch (Exception e) { Plugin.Log.LogError("[peace] skin failed: " + e); skin = null; }
            }
            if (Pn.Id == "cap" && cappieIdle != null && skin != null && !skin.OwnAnimator) skin.IdleClip = cappieIdle;
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            RefreshOutline();
        }

        // each Mita stands the way she does in the character gallery: swap the idle clip of the original Mita's animator
        void SetIdle(AnimationClip clip)
        {
            if (mitaAnim == null || mitaAnim.animOver == null) return;
            try
            {
                if (originalIdle == null)
                {
                    var baseCtrl = mitaAnim.animOver.runtimeAnimatorController;
                    if (baseCtrl != null)
                        foreach (var c in baseCtrl.animationClips)
                            if (c != null && c.name == IdleSlot) { originalIdle = mitaAnim.animOver[c] ?? c; break; }
                }
                // default = the free-roam idle ("Mita Idle", upright, hands together); the controller's own
                // "MitaIdle 1" is the story's leaning-forward pose
                var use = clip ?? Clip("Mita Idle") ?? originalIdle;
                if (use == null) return;
                mitaAnim.ReAnimationClip(IdleSlot, use);
                Plugin.Log.LogInfo($"[peace] idle -> {use.name}");
            }
            catch (Exception e) { Plugin.Log.LogWarning("[peace] idle swap failed: " + e.Message); }
        }

        System.Collections.IEnumerator EndOfFrameLoop()
        {
            var wait = new WaitForEndOfFrame();
            while (true)
            {
                yield return wait;
                SyncSkin();
            }
        }

        // end of frame: after LateUpdate and Mita's look-at IK. Also called from LateUpdate, for frames that aren't rendered.
        void SyncSkin()
        {
            if (gameSkin != null) { try { gameSkin.Sync(); } catch (Exception e) { Plugin.Log.LogError("[peace] table skin sync: " + e); gameSkin = null; } }
            if (skin == null) return;
            try { skin.Sync(); } catch (Exception e) { Plugin.Log.LogError("[peace] skin sync: " + e); skin = null; }
        }

        void OnMitaClicked()
        {
            if (!freeMode || busy || MenuOpen || fadeTarget > 0 || cardGame) return;
            OpenMainMenu();
        }

        bool interactiveOn;
        int interactiveNotes;

        void SetMitaInteractive(bool on)
        {
            if (mitaInteractive == null) return;
            // something in the scene (the roaming addon, an animation event) may switch Mita's interactive on behind our back
            if (mitaInteractive.activeSelf != interactiveOn && interactiveNotes < 6)
            {
                interactiveNotes++;
                Plugin.Log.LogWarning($"[peace] Mita interactive was switched {(mitaInteractive.activeSelf ? "on" : "off")} by the scene (free={freeMode} busy={busy} menu={MenuOpen} fade={fadeTarget})");
            }
            if (mitaInteractive.activeSelf != on) mitaInteractive.SetActive(on);
            interactiveOn = on;
        }

        void MuteDub()
        {
            try
            {
                if (!voiceSwapped) { savedVoice = GlobalGame.LanguageVoice; voiceSwapped = true; Plugin.Log.LogInfo($"[peace] dub language \"{savedVoice}\" set aside for peace mode"); }
                if (!string.IsNullOrEmpty(GlobalGame.LanguageVoice)) GlobalGame.LanguageVoice = "";
            }
            catch { }
        }

        void RestoreVoice()
        {
            if (!voiceSwapped) return;
            GlobalGame.LanguageVoice = savedVoice;
            voiceSwapped = false;
        }

        // a line Mita really says in this scene: speaker set, Mita theme, the regular "Speak" voice sounds
        static GameObject NativeMitaLine(Transform parent)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<Dialogue_3DText>()))
            {
                var d = o.Cast<Dialogue_3DText>();
                if (d == null || d.gameObject.scene != scene) continue;
                if (d.speaker != null && d.themeDialogue.ToString() == "Mita" && d.sounds != null && d.sounds.name.EndsWith(" Speak"))
                    return d.gameObject;
            }
            return null;
        }

        // at a black screen: remove what is left of earlier lines (the native text keeps fading out after its line ends)
        void ClearLeftoverText()
        {
            // a finished line moves itself under World and fades out there for a few seconds
            int n = 0, glyphs = 0;
            foreach (var d in FindObjectsOfType<Dialogue_3DText>())
                if (d != null && d.gameObject != current && d.gameObject.name == "PeaceLine") { Destroy(d.gameObject); n++; }
            // a line that ended by itself is already gone, but its letters (Dialogue_Symbol) still fly apart on their own
            foreach (var s in FindObjectsOfType<Dialogue_Symbol>())
            {
                if (s == null) continue;
                var owner = s.scrd3;
                if (owner != null && owner.gameObject == current) continue;
                Destroy(s.gameObject); glyphs++;
            }
            if (n > 0 || glyphs > 0) Plugin.Log.LogInfo($"[peace] cleared {n} leftover lines, {glyphs} letters");
        }

        // a line of yours in the story (the Player theme, no speaker): under the parent, or anywhere in the scene
        static GameObject NativePlayerLine(Transform parent)
        {
            if (parent != null)
            {
                foreach (var d in parent.GetComponentsInChildren<Dialogue_3DText>(true))
                    if (d != null && d.themeDialogue.ToString() == "Player") return d.gameObject;
                return null;
            }
            var scene = SceneManager.GetActiveScene();
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<Dialogue_3DText>()))
            {
                var d = o.Cast<Dialogue_3DText>();
                if (d != null && d.gameObject.scene == scene && d.themeDialogue.ToString() == "Player") return d.gameObject;
            }
            return null;
        }

        static GameObject FirstDialogue(Transform parent)
        {
            if (parent == null) return null;
            foreach (var d in parent.GetComponentsInChildren<Dialogue_3DText>(true)) return d.gameObject;
            return null;
        }

        static readonly HashSet<string> HorrorTargets = new HashSet<string>
        {
            "Quest 2 OpenScarf", "Quest 3 ChibiHouse", "Time MitaLook On Scarf", "Audio Echo", "AnimationMita Final (Click)",
        };

        // Removes every persistent event call in the scene that would push the story toward the wardrobe scare.
        int StripHorror()
        {
            if (world == null) return 0;
            int removed = 0;
            foreach (var comp in world.GetComponentsInChildren<Component>(true))
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
                    if (calls == null || calls.Count == 0) continue;
                    bool hadQuest2 = false, hadScarfLook = false, changed = false;
                    for (int i = calls.Count - 1; i >= 0; i--)
                    {
                        var c = calls[i];
                        var tn = c.m_Target != null ? c.m_Target.name : "";
                        bool bad = HorrorTargets.Contains(tn)
                                   || (c.m_MethodName == "HintLocationChange" && c.m_Arguments != null && c.m_Arguments.m_IntArgument == 3)
                                   || (tn == "Music 2" && c.m_MethodName == "Pitch");
                        if (!bad) continue;
                        if (tn == "Quest 2 OpenScarf") hadQuest2 = true;
                        if (tn == "Time MitaLook On Scarf") hadScarfLook = true;
                        calls.RemoveAt(i);
                        removed++;
                        changed = true;
                    }
                    if (!changed) continue;
                    ev.DirtyPersistentCalls();
                    Plugin.Log.LogInfo($"[peace] stripped horror calls from {comp.name}.{p.Name}");
                    var ue = ev.TryCast<UnityEvent>();
                    // the stand-up animation at the very end of the card game (it used to open the wardrobe quest)
                    if (hadQuest2)
                    {
                        ue?.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnCardGameFinished)));
                        var anim = comp.TryCast<ObjectAnimationPlayer>();
                        if (anim != null) cardStandUp = anim;
                    }
                    // the card game's end (Location4TableCardGame.eventStopGame): the story let you stand up only after
                    // Mita heard knocking in the wardrobe, so without that scene you stayed seated for good
                    if (hadScarfLook) ue?.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnCardGameStopped)));
                }
            }
            return removed;
        }

        // Mita's animation clips call numbered event slots (Animator_FunctionsOverride.eventAnimation).
        // Slot 0 is the chapter-ending finger click (black screen, destroys the story objects, freezes the player) and
        // other slots start story beats: drop every call that isn't harmless look/prop/sound handling.
        // (slot 0 also switches the music off and Mita's own GameObject: the sitting and cooking clips fire it, which
        // is what made her vanish when they played)
        static readonly string[] AnimEventBlock = { "CanvasBlackScreen", "ClickMita", "PlayerDontMove", "SharplyColor", "LineColorFast", "MusicBed", "Quest 2", "Quest 3", "Destroy",
                                                    "ClickFinger", "Music 1" };

        int StripAnimationEvents()
        {
            if (mitaAnim == null || mitaAnim.eventAnimation == null) return 0;
            int removed = 0;
            for (int i = 0; i < mitaAnim.eventAnimation.Count; i++)
            {
                var ev = mitaAnim.eventAnimation[i];
                var calls = ev?.m_PersistentCalls?.m_Calls;
                if (calls == null) continue;
                bool changed = false;
                for (int k = calls.Count - 1; k >= 0; k--)
                {
                    var c = calls[k];
                    var tn = c.m_Target != null ? c.m_Target.name : "";
                    var m = c.m_MethodName ?? "";
                    bool bad = tn == "Mita" && m == "SetActive";
                    foreach (var w in AnimEventBlock) if (tn.Contains(w) || m.Contains(w)) { bad = true; break; }
                    if (!bad) continue;
                    calls.RemoveAt(k); removed++; changed = true;
                }
                if (changed) { ev.DirtyPersistentCalls(); Plugin.Log.LogInfo($"[peace] stripped story calls from Mita animation event {i}"); }
            }
            return removed;
        }

        void RewireKitchen()
        {
            var go = Util.FindPath("Quest 1>Trigger Entry Kitchen");
            var trig = go != null ? go.GetComponent<Trigger_Event>() : null;
            if (trig == null) { Plugin.Log.LogWarning("[peace] kitchen trigger not found, starting free mode directly"); StartFreeMode(); return; }
            var calls = trig.eventEnter.m_PersistentCalls?.m_Calls;
            if (calls != null)
                for (int i = calls.Count - 1; i >= 0; i--)
                {
                    var m = calls[i].m_MethodName;
                    if (m == "YieldRestart" || m == "Lock" || m == "HintLocationHide") calls.RemoveAt(i);
                }
            trig.eventEnter.DirtyPersistentCalls();
            trig.eventEnter.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnKitchenEnter)));
            world?.HintLocationChange(0);
        }

        // ---------------------------------------------------------------- dev hooks (DevRunner)

        public void DevOpenMenu() { if (!freeMode) StartFreeMode(); OpenMainMenu(); }
        // dev (recordings): nothing of her own - no idle lines, suggestions, favours, visits, sofa trips, hourly events
        public static bool DevCalm;
        public void DevChoose(int i) => Choose(i);
        public void DevGreet() => OnKitchenEnter();
        public bool DevReady => ready;
        public bool DevImportsDone => ready && animQueued && animQueue.Count == 0 && !SceneImport.Busy;
        // wallsim before the scene exists (a script sets the clock or the birthday ahead of `peace`): the static parts
        public static void DevWallSimStatic(string what, string arg)
        {
            if (what == "clock" && DateTime.TryParse(arg, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t))
                devClockShift = t - DateTime.Now;
            else if (what == "birthday") devBirthday = true;
            Plugin.Log.LogInfo($"[dev] wallsim {what} {arg} before the scene (clock {Now:MM-dd HH:mm})");
        }

        // wallsim <what> [arg]: fake the conditions of a fourth-wall event to test its reaction
        public void DevWallSim(string what, string arg)
        {
            float now = Time.realtimeSinceStartup;
            float.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x);
            switch (what)
            {
                case "clock":   // clock yyyy-MM-ddTHH:mm
                    if (DateTime.TryParse(arg, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t))
                        devClockShift = t - DateTime.Now;
                    break;
                case "focus": lastFocusLine = -999f; FocusReturned(x); break;
                case "afk": DevRunner.KeepAwake = false; lastInput = now - 241f; break;
                case "input": devInputPulse = true; break;
                case "shot": devShotPulse = true; lastShot = -999f; break;
                case "window": screenKnown = true; lastFull = Screen.fullScreen; lastSW = x > 0 ? Screen.width / 2 : Screen.width * 2; lastSH = Screen.height; lastWindowLine = -999f; break;
                case "session": sessionStart = now - x * 3600f - 1f; sessionMark = Mathf.Max(0, (int)x - 1); break;
                case "late": lateSaid = false; sessionStart = Mathf.Min(sessionStart, now - 1300f); break;
                case "console": lastConsoleLine = -999f; devConsole = true; break;
                case "pause": wasPaused = true; pauseHadPhoto = arg == "photo"; pausedSince = now - 10f; lastPauseLine = -999f; break;
                case "mute": lastVolume = 1f; devMute = true; lastMuteLine = -999f; break;
                case "stare": lastStareLine = -999f; stareTime = 7.5f; break;
                case "playtime": S.PlayedSec = x * 3600f + 1f; S.PlayHoursSaid = 0; break;
                case "mismatch": mismatchSaid = false; sessionStart = Mathf.Min(sessionStart, now - 601f); break;
                case "battery": devBattery = (int)x; batterySaid = 0; nextBatteryCheck = 0f; nextSlowCheck = 0f; break;   // battery <percent>
                case "photos": devPhotos = (int)x; S.PhotosSaid = 0; break;                                                 // photos <count> (before the next photo)
                case "birthday": devBirthday = true; S.LastBirthday = 0; break;                                             // (before the kitchen greeting)
            }
            Plugin.Log.LogInfo($"[dev] wallsim {what} {arg} (clock {Now:MM-dd HH:mm})");
        }
        public string DevSymbols()
        {
            var sb = new System.Text.StringBuilder();
            var d = tmplMita != null ? tmplMita.GetComponent<Dialogue_3DText>() : null;
            var ex = d != null && d.exampleSymbol != null ? d.exampleSymbol.name : "?";
            sb.AppendLine("exampleSymbol=" + ex + " parentOfTemplateSymbol=" + (d != null && d.exampleSymbol != null && d.exampleSymbol.transform.parent != null ? d.exampleSymbol.transform.parent.name : "-"));
            var counts = new Dictionary<string, int>();
            foreach (var t in FindObjectsOfType<Transform>())
            {
                if (!t.name.StartsWith(ex)) continue;
                string p = t.parent != null ? t.parent.name + (t.parent.parent != null ? " < " + t.parent.parent.name : "") : "(root)";
                counts[p] = counts.TryGetValue(p, out var c) ? c + 1 : 1;
            }
            foreach (var kv in counts) sb.AppendLine($"{kv.Value} x under {kv.Key}");
            foreach (var dd in FindObjectsOfType<Dialogue_3DText>()) sb.AppendLine($"live dialogue {dd.name} parent={(dd.transform.parent != null ? dd.transform.parent.name : "-")} symbols={(dd.symbolObjects != null ? dd.symbolObjects.Count : -1)}");
            return sb.ToString();
        }
        // where every Mita and her skeleton really are (the posed body vs what gets rendered)
        public string DevMitaDiag()
        {
            var sb = new System.Text.StringBuilder();
            string P(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + ">" + s; } return s; }
            foreach (var m in Resources.FindObjectsOfTypeAll(Il2CppType.Of<MitaPerson>()))
            {
                var mp = m.Cast<MitaPerson>();
                if (mp == null || !mp.gameObject.scene.IsValid()) continue;
                sb.AppendLine($"MitaPerson {P(mp.transform)} active={mp.gameObject.activeInHierarchy} pos={mp.transform.position} nma={(mp.nma != null ? P(mp.nma.transform) + " en=" + mp.nma.enabled : "-")} isOurs={mp == mita}");
            }
            sb.AppendLine($"mitaBody {(mitaBody != null ? P(mitaBody) + " pos " + mitaBody.position + " rot " + mitaBody.eulerAngles : "-")} posed={posed} lying={lying}");
            if (poseHips != null) sb.AppendLine($"poseHips {P(poseHips)} {poseHips.position}");
            if (poseHead != null) sb.AppendLine($"poseHead {P(poseHead)} {poseHead.position}");
            if (mita != null)
                foreach (var r in mita.transform.root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    sb.AppendLine($"SMR {P(r.transform)} on={r.enabled && r.gameObject.activeInHierarchy} root={(r.rootBone != null ? P(r.rootBone) + " " + r.rootBone.position : "-")} bounds={r.bounds.center}");
            foreach (var a in mita != null ? mita.transform.root.GetComponentsInChildren<Animator>(true) : new Animator[0])
                sb.AppendLine($"Animator {P(a.transform)} en={a.enabled} rootMotion={a.applyRootMotion}");
            return sb.ToString();
        }
        public void DevSleep() => DoSleep();
        // nothing playing: no lines, no fade, no staged scene (dev scripts wait for this)
        public bool DevIdle => ready && freeMode && !busy && queue.Count == 0 && fadeTarget <= 0f && fadeVal <= 0.001f && afterFade == null && (!posed || showcasing) && photoStage == 0 && pendingWall.Count == 0;
        // recording: Mita stands at (mx, mz) facing you, you stand at (px, pz) looking at her
        bool showcasing;
        public void DevPitch(float pitch)
        {
            if (pm == null) return;
            pm.rotY = pitch;
            Plugin.Log.LogInfo($"[dev] pitch -> {pitch} (rotX {pm.rotX})");
        }
        public void DevShowcase(Vector3 m, Vector3 p, string room)
        {
            if (!freeMode) StartFreeMode();
            ShowRoom(room);
            PoseMitaUpright(m, p - m);
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            MovePlayer(p, m);
            showcasing = true;
            try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
        }
        public string DevLookDump()
        {
            if (mitaLook == null) return "no look";
            var l = mitaLook;
            string T(Transform t) => t == null ? "-" : t.name + " " + t.position;
            return $"active={l.active} bodyIK={l.activeBodyIK} lookObject={T(l.lookObject)} bodyYOffset={l.bodyYOffset} need={l.needBodyYOffset} was={l.wasBodyYOffset} " +
                   $"head={(l.head != null)} spine={(l.spine != null)} ikMain={(l.ikMain != null)} player={(pm != null ? pm.transform.position.ToString() : "-")} cam={(pm != null && pm.mainCamera != null ? pm.mainCamera.position.ToString() : "-")}";
        }
        public string DevAnimNow()
        {
            var an = mitaBody != null ? mitaBody.GetComponent<Animator>() : null;
            if (an == null) return "no animator";
            var sb = new System.Text.StringBuilder();
            for (int layer = 0; layer < an.layerCount; layer++)
            {
                var st = an.GetCurrentAnimatorStateInfo(layer);
                sb.Append($"L{layer} w={an.GetLayerWeight(layer):0.0} hash={st.shortNameHash} t={st.normalizedTime:0.00}: ");
                foreach (var ci in an.GetCurrentAnimatorClipInfo(layer)) sb.Append($"{(ci.clip != null ? ci.clip.name : "-")}({ci.weight:0.0}) ");
                sb.Append(" | ");
            }
            return sb.ToString();
        }
        public void DevReIdle()
        {
            var idle = (skin != null && !skin.OwnAnimator ? skin.IdleClip : null) ?? Clip("Mita Idle") ?? originalIdle;
            SetIdle(idle);
            try { mitaAnim?.AnimationClipSimpleNext(idle); } catch (Exception e) { Plugin.Log.LogWarning("[dev] reidle: " + e.Message); }
        }
        public void DevSyncSkin() => SyncSkin();
        // captures render inside DevRunner.LateUpdate, possibly before ours: apply this frame's pose fixes first
        public void DevPrepareCapture() { if (!ready) return; KeepPose(); SyncSkin(); }
        public void DevMitaLook(bool on) { try { mitaLook?.Activation(on); if (on) mitaLook?.LookOnPlayer(); } catch { } }
        public void DevUnshowcase()
        {
            showcasing = false;
            if (mitaBody != null && pm != null) ReleasePose(mitaBody.position, pm.transform.position);
        }
        public void DevAct(string what)
        {
            // (the staged ones - the hug, holding hands - place you both themselves)
            if (showcasing && (what == "hug" || what == "special")) DevUnshowcase();
            switch (what)
            {
                case "talk": DoTalk(); break;
                case "pat": DoPat(); break;
                case "hug": DoHug(); break;
                case "compliment": DoCompliment(); break;
                case "special": DoSpecial(); break;
                case "photo": DoPhoto(); break;
                case "quit": DoQuitGame(); break;
                case "morning": Play(MorningLines()); break;
                default: Plugin.Log.LogWarning("[dev] act ?" + what); break;
            }
        }
        // play one of the fourth-wall sets by its field name (AltF4, Crash, FocusLong, Screenshot, ...)
        public void DevWall(string field, string away)
        {
            var w = Personas.Wall(Pn.Id);
            var f = typeof(WallLines).GetField(field);
            if (f == null) { Plugin.Log.LogWarning("[dev] wall ?" + field); return; }
            if (!string.IsNullOrEmpty(away)) awayText = away;
            var v = f.GetValue(w);
            if (v is Line[][] sets && sets.Length > 0) Play(Pick(sets));
            else if (v is Line[] one) Play(one);
        }
        public void DevClip(string mode, string name)
        {
            var c = name == "gallery" ? skin?.GalleryIdle : Clip(name);   // (gallery: the skin model's own idle)
            if (c == null || mitaAnim == null) { Plugin.Log.LogWarning("[dev] no clip " + name); return; }
            if (mode == "simple") mitaAnim.AnimationClipSimple(c);
            else if (mode == "frame") mitaAnim.AnimationClipSimpleFrame(c);
            else if (mode == "idle") SetIdle(c);
            else if (mode == "hold") { HoldClip(c); mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(c); }
            else mitaAnim.AnimationClipSimpleNext(c);
            var hips = Util.FindDeep(mitaBody, "Hips");
            Plugin.Log.LogInfo($"[dev] clip {mode} {name}: root {mitaBody.position} hips {(hips != null ? hips.position.ToString() : "-")} rootMotion={(mitaAnim.anim != null && mitaAnim.anim.applyRootMotion)}");
        }
        public void DevWhere()
        {
            var hips = Util.FindDeep(mitaBody, "Hips");
            Plugin.Log.LogInfo($"[dev] mita root {mitaBody.position} rot {mitaBody.eulerAngles} hips {(hips != null ? hips.position.ToString() : "-")} state={(mitaAnim.anim != null ? mitaAnim.anim.GetCurrentAnimatorClipInfo(0).Length.ToString() : "-")}");
        }
        public void DevPoseSeat() => PoseMitaUpright(SeatMita, SeatPlayerSide - SeatMita);
        public void DevMeal() { S.LastMeal = null; DoMeal(); }
        public void DevSetTime(float minute) { S.Minute = minute; lastEventHour = (int)(minute / 60); }
        public void DevSkin(string path) => ApplySkin(path == "none" ? null : path);
        public void DevLookAtPlayer()
        {
            try { if (pm != null) mitaLook?.RotateOnTargetExact(pm.transform); mitaLook?.LookOnPlayer(); }
            catch (Exception e) { Plugin.Log.LogWarning("[dev] look: " + e.Message); }
        }
        // switch Mita without reloading the scene (dev/recording only)
        public void DevPersona(string id)
        {
            // (a test switching Mitas keeps its clock, and the last one's cooking or sleep doesn't carry over)
            if (activity != MitaActivity.Free) EndRoutine();
            ReleaseClip();
            var minute = S.Minute;
            PeaceMode.Persona = Personas.Get(id);
            PeaceMode.Save = PeaceSave.Load(Pn.Id);
            S.Minute = minute;
            ApplySkin(Pn.Skin);
            var md = tmplMita != null ? tmplMita.GetComponent<Dialogue_3DText>() : null;
            if (md != null && md.sounds != null && md.sounds.audioSource != null) md.sounds.audioSource.pitch = Pn.VoicePitch;
            if (mitaInteractive != null)
            {
                var info = mitaInteractive.GetComponentInChildren<ObjectInteractive_CaseInfo>(true);
                if (info != null) info.indexString = TextHook.Register(Loc.F("{0}와 교감하기", ShortName));
            }
            Plugin.Log.LogInfo("[dev] persona -> " + Pn.Id);
        }
        public void DevEmotion(string e) { try { mita?.FaceEmotion(e); } catch (Exception ex) { Plugin.Log.LogWarning("[dev] emotion " + e + ": " + ex.Message); } }
        public Transform MitaBody => mitaBody;
        // what a camera should look at to see her face (the skin's own head for models with their own rig)
        public Vector3 HeadPoint
        {
            get
            {
                var sk = TableMitaUp ? gameSkin : skin;
                if (sk != null && sk.OwnAnimator && sk.Root != null)
                {
                    var h = Util.FindDeep(sk.Root.transform, "Head") ?? Util.FindDeep(sk.Root.transform, "Neck");
                    if (h != null) return h.position;
                }
                var body = SpeakerBody;
                var head = body != null ? Util.FindDeep(body, "Head") : null;
                if (head != null && head.GetComponent<Renderer>() == null) return head.position;
                return body != null ? body.position + Vector3.up * 1.45f : Vector3.zero;
            }
        }

        // ---------------------------------------------------------------- flow

        void OnKitchenEnter()
        {
            if (greeted) return;
            greeted = true;
            // she turns round from the stove to greet you (BeginInteraction), then the free day starts
            Play(SessionGreeting(), StartFreeMode);
        }

        // how the last session ended decides the first words: Alt+F4, the window's X, a crash, or coming right back
        List<Line> SessionGreeting()
        {
            var w = Personas.Wall(Pn.Id);
            var lines = new List<Line>();
            string exit = PeaceMode.PreviousExit ?? "";
            var away = PeaceMode.PreviousVisit.HasValue ? Now - PeaceMode.PreviousVisit.Value : TimeSpan.Zero;
            if (away < TimeSpan.Zero) away = TimeSpan.Zero;
            awayText = Personas.SpanText(away);
            Line[][] react = null;
            if (!PeaceMode.FirstVisit)
            {
                if (exit == "altf4") react = S.AltF4Count > 1 && w.AltF4Again != null ? w.AltF4Again : w.AltF4;
                else if (exit == "close") react = w.Close;
                else if (exit == "crash") react = w.Crash;
                else if (exit == "quit" && away.TotalMinutes < 3) react = w.Relaunch;
            }
            Plugin.Log.LogInfo($"[peace] previous exit: {(exit == "" ? "-" : exit)} (alt+f4 x{S.AltF4Count}), away {awayText}{(react != null ? ", reacting" : "")}");
            // what hurt her since last time
            if (!PeaceMode.FirstVisit)
            {
                if (exit == "altf4") LoseAff(4, "Alt+F4");
                else if (exit == "close") LoseAff(2, "closed the window");
                if (away.TotalDays >= 2) LoseAff(Math.Min((int)away.TotalDays - 1, 6), "days away");
            }
            if (react != null) lines.AddRange(Pick(react));
            else lines.AddRange(Cold ? MorningLines() : (IList<Line>)ReturnGreeting() ?? MorningLines());   // (cold: no warm welcome back)
            // real calendar days, once per date
            var today = Now.ToString("yyyy-MM-dd");
            if (w.Dates != null && S.LastDateEvent != today && w.Dates.TryGetValue(Now.ToString("MM-dd"), out var d))
            {
                S.LastDateEvent = today;
                lines.AddRange(d);
            }
            // real days in a row
            if (S.LastRealDate != today)
            {
                bool yesterday = DateTime.TryParse(S.LastRealDate, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var last) && (Now.Date - last.Date).TotalDays == 1;
                S.Streak = yesterday ? S.Streak + 1 : 1;
                S.LastRealDate = today;
                S.RealDays++;
                if (w.Streak != null && Array.IndexOf(StreakMarks, S.Streak) >= 0) lines.AddRange(w.Streak);
                Plugin.Log.LogInfo($"[peace] real-day streak {S.Streak} ({S.RealDays} days in all)");
                // the first visit of a Monday, a Friday or a weekend day
                if (!PeaceMode.FirstVisit) lines.AddRange(WeekdayLines(w));
            }
            // your birthday (config), once a year; anniversaries of the first visit; Kind Mita's memory of the story
            lines.AddRange(BirthdayLines(w));
            lines.AddRange(AnniversaryLines(w));
            lines.AddRange(StoryGreeting());
            // every so many launches
            Plugin.Log.LogInfo($"[peace] session #{S.Sessions}, {S.PlayedSec / 3600f:0.0} h played in all");
            if (w.Sessions != null && Array.IndexOf(SessionMarks, S.Sessions) >= 0) lines.AddRange(w.Sessions);
            return lines;
        }

        // ---------------------------------------------------------------- fourth wall

        // the real clock (dev can move it to test dates and late nights)
        static TimeSpan devClockShift;
        static DateTime Now => DateTime.Now + devClockShift;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
        static bool KeyHeldNow(int vk) { try { return (GetAsyncKeyState(vk) & 0x8000) != 0; } catch { return false; } }

        bool devInputPulse, devShotPulse, devConsole, devMute;

        static readonly int[] StreakMarks = { 3, 7, 14, 30, 50, 100, 200, 365 };
        static readonly int[] SessionMarks = { 10, 30, 50, 100, 200, 500 };
        static readonly int[] PlayHourMarks = { 1, 5, 10, 24, 50, 100, 200 };

        // pause / photo mode / sound / staring / real time vs game time
        bool wasPaused, pauseHadPhoto, mismatchSaid;
        float pausedSince, lastPauseLine = -999f, lastMuteLine = -999f, lastStareLine = -999f, stareTime, nextSlowCheck;
        float lastVolume = -1f;

        [HideFromIl2Cpp] void TrackMore(float now, WallLines w)
        {
            // total real time in peace mode, and hour milestones
            if (freeMode) S.PlayedSec += Mathf.Clamp(Time.unscaledDeltaTime, 0f, 1f);
            int hours = (int)(S.PlayedSec / 3600f);
            int reached = 0;
            foreach (var mark in PlayHourMarks) if (hours >= mark) reached = mark;
            if (reached > S.PlayHoursSaid) { S.PlayHoursSaid = reached; if (freeMode) QueueWall(w.Playtime); }

            // the ESC menu / photo mode: she notices time stopped
            bool paused = false, photo = false;
            try { if (gc != null) { paused = gc.isPaused; photo = gc.photomode; } } catch { }
            if (paused || photo)
            {
                if (!wasPaused) { wasPaused = true; pausedSince = now; pauseHadPhoto = false; }
                if (photo) pauseHadPhoto = true;
            }
            else if (wasPaused)
            {
                wasPaused = false;
                if (freeMode && now - pausedSince > 4f && now - lastPauseLine > 180f)
                {
                    lastPauseLine = now;
                    QueueWall(Pick(pauseHadPhoto ? w.PhotoMode : w.Pause));
                }
            }

            // staring at her face from close by, for 7 seconds, while she isn't talking
            if (freeMode && !busy && !MenuOpen && pm != null && pm.mainCamera != null && mitaBody != null)
            {
                var to = HeadPoint - pm.mainCamera.position;
                bool looking = to.magnitude < 3.5f && Vector3.Angle(pm.mainCamera.forward, to) < 8f;
                // (a recording's frames take far longer than they show: its own frame time there)
                stareTime = looking ? stareTime + (Time.captureFramerate > 0 ? Time.deltaTime : Time.unscaledDeltaTime) : 0f;
                if (stareTime > (Warm ? 1.8f : 2.5f) && Time.time > nextShy) BeShy();   // (the story's shyness: a glance away first)
                if (stareTime > 7f && now - lastStareLine > 300f)
                {
                    lastStareLine = now; stareTime = 0f;
                    QueueWall(Pick(Cold && A.ColdStare != null ? A.ColdStare : w.Stare));
                }
            }
            else stareTime = 0f;

            if (now < nextSlowCheck) return;
            nextSlowCheck = now + 1f;

            // sound turned off
            float vol = 1f;
            try { vol = GlobalGame.VolumeGame; } catch { }
            if (devMute) { vol = 0f; devMute = false; }
            if (lastVolume >= 0f && lastVolume > 0.01f && vol <= 0.01f && freeMode && now - lastMuteLine > 300f)
            {
                lastMuteLine = now;
                QueueWall(Pick(w.Mute));
            }
            lastVolume = vol;

            // a laptop running out of battery
            CheckBattery(now, w);

            // game time vs the real clock, once a session
            if (freeMode && !mismatchSaid && now - sessionStart > 600f)
            {
                int h = Now.Hour;
                var tod = CurrentTod();
                if (tod == Tod.Night && h >= 9 && h < 18) { mismatchSaid = true; QueueWall(Pick(w.NightHere)); }
                else if ((tod == Tod.Day || tod == Tod.Morning) && (h >= 23 || h < 5)) { mismatchSaid = true; QueueWall(Pick(w.DayHere)); }
            }
        }

        // "같이 사진 찍기": she poses, our UI hides for one frame, the screen is saved next to the game
        int photoStage;
        float photoAt;
        string photoPath;

        void DoPhoto()
        {
            Did("photo");
            var w = Personas.Wall(Pn.Id);
            Play(w.PhotoReady ?? new Line[0], () =>
            {
                photoStage = 1; photoAt = Time.realtimeSinceStartup + 0.6f;
                // she poses for the camera
                try { if (!posed) mitaLook?.LookOnPlayerAndRotate(); else mitaLook?.LookOnPlayer(); mita?.FaceEmotion("smile"); } catch { }
            });
        }

        // a photo "together" should have her in it: turn the view to her face if she is around
        void AimAtMita()
        {
            if (pm == null || pm.mainCamera == null || mitaBody == null) return;
            var to = HeadPoint - pm.mainCamera.position;
            float flat = new Vector2(to.x, to.z).magnitude;
            if (flat > 6f || flat < 0.05f) return;
            pm.rotX = Quaternion.LookRotation(new Vector3(to.x, 0f, to.z)).eulerAngles.y;
            // a little below her eyes, so her face sits in the upper middle of the frame
            pm.rotY = -Mathf.Atan2(to.y - 0.12f, flat) * Mathf.Rad2Deg;
        }

        void TickPhoto()
        {
            if (photoStage == 1 && Time.realtimeSinceStartup < photoAt) { AimAtMita(); return; }
            if (photoStage == 0 || Time.realtimeSinceStartup < photoAt) return;
            if (photoStage == 1)
            {
                // wait for the last line's letters to clear, then shoot without our HUD
                ClearLeftoverText();
                ui.SetVisible(false);
                var dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(BepInEx.Paths.GameRootPath + "/"), "PeaceModePhotos");
                System.IO.Directory.CreateDirectory(dir);
                photoPath = System.IO.Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Pn.Id}.png");
                ScreenCapture.CaptureScreenshot(photoPath);
                photoStage = 2;
                photoAt = Time.realtimeSinceStartup + 0.25f;
                return;
            }
            ui.SetVisible(true);
            photoStage = 0;
            Plugin.Log.LogInfo("[peace] photo saved: " + photoPath);
            AddAff(1);
            var wall = Personas.Wall(Pn.Id);
            Play((wall.PhotoDone ?? new Line[0]).Concat(PhotoCountLines(System.IO.Path.GetDirectoryName(photoPath), wall)).ToList());
        }

        float lastTongue = -999f, weatherErrorAt;
        float sessionStart, lastInput, lastAltHeld = -10f, lastF4 = -10f, unfocusedAt = -1f;
        float lastShot = -999f, lastFocusLine = -999f, lastWindowLine = -999f, consoleSince = -1f, lastConsoleLine = -999f, nextConsoleCheck;
        float glitchUntil;
        bool appQuitting, farewellQuit, afk, lateSaid, screenKnown, lastFull, glitching, mouseAxisOk = true;
        int lastSW, lastSH, sessionMark;
        string awayText = "";
        // events wait here until she is free to talk (several can happen at once: back from Alt+Tab and a screenshot)
        readonly Queue<(Line[] lines, string away)> pendingWall = new Queue<(Line[], string)>();
        GameObject consoleHint;
        bool consoleLogged, consoleSearchLogged;

        [HideFromIl2Cpp] void QueueWall(Line[] lines, string away = null)
        {
            if (lines == null || lines.Length == 0) return;
            // (dev recordings run on frame time, so real-time events - a long stare, AFK - pile up; they are scripted instead)
            if (DevCalm) return;
            if (pendingWall.Count >= 3) pendingWall.Dequeue();
            pendingWall.Enqueue((lines, away));
            Plugin.Log.LogInfo($"[peace] fourth wall queued ({pendingWall.Count}): {lines[0].text}");
        }

        // the name Mita calls you: config, else the one typed in chapter 1 this session, else "플레이어"
        public static string DevPlayerName;   // (dev: a name for recordings, not saved; "" = the plain "플레이어")
        static string PlayerName()
        {
            if (DevPlayerName == "") return Loc.T("플레이어");
            var n = DevPlayerName ?? (PeaceMode.PlayerName != null ? PeaceMode.PlayerName.Value : null);
            if (string.IsNullOrWhiteSpace(n)) { try { n = GlobalGame.namePlayer; } catch { n = null; } }
            // the game keeps "???" until a name is typed in chapter 1
            return string.IsNullOrWhiteSpace(n) || n.Trim().Trim('?').Length == 0 ? Loc.T("플레이어") : n.Trim();
        }

        // lines are written for a name without a final consonant ("[player]는"); fix the particle for names that have one
        static readonly (string plain, string batchim)[] Particles =
            { ("는", "은"), ("를", "을"), ("가", "이"), ("와", "과"), ("랑", "이랑"), ("야", "아"), ("나", "이나"), ("라고", "이라고"), ("예요", "이에요"), ("네요", "이네요"), ("요", "이요") };

        static string WithName(string t) => WithToken(t, "[player]", PlayerName());

        // a token ([player], {gift}) replaced by a word, with the particle after it fixed for the word's last syllable
        static string WithToken(string t, string token, string word)
        {
            if (string.IsNullOrEmpty(word) || t.IndexOf(token, StringComparison.Ordinal) < 0) return t;
            char last = word[word.Length - 1];
            bool hangul = last >= 0xAC00 && last <= 0xD7A3;
            bool batchim = hangul && (last - 0xAC00) % 28 != 0;
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (true)
            {
                int k = t.IndexOf(token, i, StringComparison.Ordinal);
                if (k < 0) { sb.Append(t, i, t.Length - i); break; }
                sb.Append(t, i, k - i).Append(word);
                i = k + token.Length;
                if (!batchim) continue;
                foreach (var (plain, bat) in Particles)
                    if (string.CompareOrdinal(t, i, plain, 0, plain.Length) == 0) { sb.Append(bat); i += plain.Length; break; }
            }
            return sb.ToString();
        }

        // {away} {clock} {session} {gift} {photos} [player]
        string Fill(string t)
        {
            if (t == null) return t;
            t = WithName(t);
            if (t.IndexOf('{') < 0) return t;
            if (giftText != null) t = WithToken(t, "{gift}", giftText);
            return t.Replace("{away}", awayText)
                    .Replace("{clock}", Personas.ClockText(Now))
                    .Replace("{streak}", S != null ? S.Streak.ToString() : "1")
                    .Replace("{sessions}", S != null ? S.Sessions.ToString() : "1")
                    .Replace("{photos}", photoCount.ToString())
                    .Replace("{days}", anniversaryDays.ToString())
                    .Replace("{total}", Loc.F("{0}시간", S != null ? (int)(S.PlayedSec / 3600f) : 1))
                    .Replace("{session}", Personas.SpanText(TimeSpan.FromSeconds(Time.realtimeSinceStartup - sessionStart)));
        }

        void TrackWall()
        {
            float now = Time.realtimeSinceStartup;
            var w = Personas.Wall(Pn.Id);
            // Alt / F4 as the game sees them, plus the physical keys while our window has focus
            bool focused = Application.isFocused;
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr) || (focused && KeyHeldNow(0x12))) lastAltHeld = now;
            if (Input.GetKey(KeyCode.F4) || (focused && KeyHeldNow(0x73))) lastF4 = now;

            // away from the keyboard: 4 minutes without a key or a mouse move
            bool input = Input.anyKey || devInputPulse;
            devInputPulse = false;
            if (!input && mouseAxisOk)
            {
                try { input = Mathf.Abs(Input.GetAxisRaw("Mouse X")) + Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f; }
                catch { mouseAxisOk = false; }
            }
            if (!freeMode || !Application.isFocused || DevRunner.ScriptAwake || danceStage != 0 || hammerStage != 0) lastInput = now;   // (her turn on the pad: you're watching)
            else if (input)
            {
                if (afk) { afk = false; QueueWall(Pick(w.AfkBack)); }
                lastInput = now;
            }
            else if (!afk && now - lastInput > 240f) { afk = true; QueueWall(Pick(w.AfkStart)); }

            // screenshot keys (Steam's F12, Print Screen)
            bool shotKey = Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.Print) || Input.GetKeyDown(KeyCode.SysReq) || devShotPulse;
            devShotPulse = false;
            if (shotKey && now - lastShot > 90f)
            {
                lastShot = now;
                if (freeMode) QueueWall(Pick(w.Screenshot));
            }

            // the window got smaller or bigger (windowed / fullscreen / resized)
            if (Application.isFocused)
            {
                int sw = Screen.width, sh = Screen.height;
                bool full = Screen.fullScreen;
                if (!screenKnown) { screenKnown = true; lastSW = sw; lastSH = sh; lastFull = full; }
                else if (full != lastFull || Math.Abs(sw * sh - lastSW * lastSH) > 40000)
                {
                    bool smaller = full != lastFull ? !full : sw * sh < lastSW * lastSH;
                    lastSW = sw; lastSH = sh; lastFull = full;
                    if (freeMode && now - lastWindowLine > 30f) { lastWindowLine = now; QueueWall(Pick(smaller ? w.WindowSmaller : w.WindowBigger)); }
                }
            }

            // real play time: 1, 2, 3 hours
            if (freeMode && w.Session != null && sessionMark < w.Session.Length && now - sessionStart >= 3600f * (sessionMark + 1))
            {
                sessionMark++;
                QueueWall(w.Session[sessionMark - 1]);
            }

            // the real clock says it's the middle of the night
            int hour = Now.Hour;
            if (freeMode && !lateSaid && hour >= 1 && hour < 5 && now - sessionStart > 1200f) { lateSaid = true; QueueWall(Pick(w.RealLate)); }

            // the player got the handheld console out (the game's own idle Tetris)
            if (now >= nextConsoleCheck)
            {
                nextConsoleCheck = now + 1f;
                if (consoleHint == null) consoleHint = FindHintKey("Key RemoveTetris");
                if (consoleHint == null && !consoleSearchLogged) { consoleSearchLogged = true; Plugin.Log.LogWarning("[peace] handheld console hint not found"); }
                bool playing = devConsole || (consoleHint != null && consoleHint.activeInHierarchy);
                if (playing != consoleLogged) { consoleLogged = playing; Plugin.Log.LogInfo("[peace] handheld console " + (playing ? "out" : "put away")); }
                if (!playing) consoleSince = -1f;
                else if (consoleSince < 0) consoleSince = now;
                else if (freeMode && now - consoleSince > 6f && now - lastConsoleLine > 300f && PlayerNearMita(9f, 360f))
                {
                    lastConsoleLine = now;
                    QueueWall(Pick(w.Console));
                }
            }

            // Crazy Mita's red flicker
            if (glitchUntil > now && fadeVal <= 0.001f)
            {
                glitching = true;
                // stutter: two frames of red for every clear one
                bool on = ((int)((glitchUntil - now) / 0.06f)) % 3 != 0;
                ui.SetTint(on ? new Color(0.5f, 0f, 0.06f, Random.Range(0.25f, 0.55f)) : Color.clear);
            }
            else if (glitching) { glitching = false; ui.SetFade(fadeVal); }
        }

        static GameObject FindHintKey(string name)
        {
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<Interface_KeyHint_Key>()))
            {
                var k = o.TryCast<Interface_KeyHint_Key>();
                if (k != null && k.gameObject.name == name && k.gameObject.scene.IsValid()) return k.gameObject;
            }
            return null;
        }

        void OnApplicationFocus(bool focus)
        {
            float now = Time.realtimeSinceStartup;
            if (!focus) { if (unfocusedAt < 0) unfocusedAt = now; return; }
            if (unfocusedAt < 0) return;
            float away = now - unfocusedAt;
            unfocusedAt = -1f;
            FocusReturned(away);
        }

        void FocusReturned(float away)
        {
            float now = Time.realtimeSinceStartup;
            lastInput = now;
            if (!ready || !freeMode || appQuitting || away < 30f || now - lastFocusLine < 120f) return;
            lastFocusLine = now;
            var w = Personas.Wall(Pn.Id);
            QueueWall(Pick(away > 300f ? w.FocusLong : w.FocusBack), Personas.SpanText(TimeSpan.FromSeconds(away)));
        }

        // Alt+F4 / the window's X / our own goodbye: remembered for the next launch
        void OnApplicationQuit()
        {
            appQuitting = true;
            RestoreVoice();   // (the game may save its options as it closes: with the player's own dub language)
            if (S == null) return;
            float now = Time.realtimeSinceStartup;
            // Alt+F4: Alt/F4 seen by the game in the last 2 s, or still physically held while the window closes
            bool altF4 = now - lastAltHeld < 2f || now - lastF4 < 2f || KeyHeldNow(0x12) || KeyHeldNow(0x73);
            S.ExitKind = farewellQuit ? "quit" : altF4 ? "altf4" : "close";
            if (S.ExitKind == "altf4") S.AltF4Count++;
            S.Running = false;
            Plugin.Log.LogInfo("[peace] game closing: " + S.ExitKind);
            S.Write();
        }

        void DoQuitGame()
        {
            NextGesture(ByeGesture());
            Play(Pick(Personas.Wall(Pn.Id).Farewell), () =>
            {
                fadeTarget = 1f;
                afterFade = () =>
                {
                    farewellQuit = true;
                    S.ExitKind = "quit";
                    S.Running = false;
                    S.Write();
                    Plugin.Log.LogInfo("[peace] said goodbye, quitting the game");
                    Application.Quit();
                };
            });
        }

        void OpenLeaveMenu()
        {
            ShowMenu("오늘은 여기까지", new List<string> { "메인 메뉴로 돌아가기", "게임 종료", "조금 더 있을래" },
                new List<Action> { DoLeave, DoQuitGame, () => { } });
        }

        // first greeting of a session: depends on how long you were away and on the real clock
        List<Line> ReturnGreeting()
        {
            var r = L.Returns;
            if (r == null) return null;
            string key;
            if (PeaceMode.FirstVisit) key = "first";
            else if (PeaceMode.PreviousVisit == null) key = "day";
            else
            {
                var away = Now - PeaceMode.PreviousVisit.Value;
                if (away.TotalMinutes < 10) key = "soon";
                else if (away.TotalHours < 8) key = "hours";
                else if (away.TotalHours < 36) key = "day";
                else if (away.TotalDays < 7) key = "days";
                else key = "long";
            }
            if (!r.TryGetValue(key, out var main) || main == null) return null;
            var lines = new List<Line>(main);
            int hour = Now.Hour;
            string clock = hour < 5 ? "night" : hour < 8 ? "dawn" : null;
            if (key != "first" && clock != null && r.TryGetValue(clock, out var extra) && extra != null) lines.AddRange(extra);
            Plugin.Log.LogInfo($"[peace] return greeting: {key}{(clock != null ? "+" + clock : "")} (last seen {PeaceMode.PreviousVisit})");
            return lines;
        }

        // Cappie's easter egg: shake the view up and down and she nods along, left and right and she shakes her head
        readonly List<(float t, float yaw, float pitch)> lookHist = new List<(float, float, float)>();
        float lastNod = -10f, lastNodLine = -60f;

        void CheckHeadGesture()
        {
            if (!Pn.MirrorsHead || pm == null || mitaLook == null) return;
            if (busy || MenuOpen || fadeTarget > 0 || !PlayerNearMita(5f, 30f)) { lookHist.Clear(); return; }
            float now = Time.time;
            lookHist.Add((now, pm.rotX, pm.rotY));
            lookHist.RemoveAll(h => now - h.t > 1.2f);
            if (now - lastNod < 1.2f || lookHist.Count < 6) return;
            int nods = Reversals(false), shakes = Reversals(true);
            bool yes = nods >= 2 && nods > shakes;
            bool no = !yes && shakes >= 2;
            if (!yes && !no) return;
            lastNod = now;
            lookHist.Clear();
            Plugin.Log.LogInfo($"[peace] {Pn.Id} mirrors a head {(yes ? "nod" : "shake")} (reversals {nods}/{shakes})");
            try { mitaLook.LookOnPlayer(); mitaLook.Nod(yes); } catch (Exception e) { Plugin.Log.LogWarning("[peace] nod: " + e.Message); }
            var set = yes ? L.MirrorNod : L.MirrorShake;
            if (set != null && now - lastNodLine > 25f && Random.value < 0.4f)
            {
                lastNodLine = now;
                Play(Pick(set), null, true);
            }
        }

        // direction changes bigger than 4° in the recent look history (yaw is unwrapped)
        int Reversals(bool yaw)
        {
            int count = 0, dir = 0;
            float prev = yaw ? lookHist[0].yaw : lookHist[0].pitch, v = 0f, extreme = 0f;
            const float th = 4f;
            for (int i = 1; i < lookHist.Count; i++)
            {
                float cur = yaw ? lookHist[i].yaw : lookHist[i].pitch;
                v += yaw ? Mathf.DeltaAngle(prev, cur) : cur - prev;
                prev = cur;
                if (dir == 0) { if (v - extreme > th) { dir = 1; extreme = v; } else if (extreme - v > th) { dir = -1; extreme = v; } }
                else if (dir == 1) { if (v > extreme) extreme = v; else if (extreme - v > th) { dir = -1; extreme = v; count++; } }
                else { if (v < extreme) extreme = v; else if (v - extreme > th) { dir = 1; extreme = v; count++; } }
            }
            return count;
        }

        void StartFreeMode()
        {
            if (freeMode) return;
            freeMode = true;
            if (addon != null)
            {
                addon.StartAddon();
                addon.ActivationAddon(false);
                addon.InteractiveActive(false);
            }
            world?.HintLocationChange(2);
            idleNext = Time.time + Random.Range(60f, 120f);
            Plugin.Log.LogInfo("[peace] free mode started");
        }

        // ---------------------------------------------------------------- card game
        // Clicking the card table (Interactive Cards >>) stops the roaming addon, sits you down and brings Mita to the table.
        // The game's own end (eventStopGame) is followed here by her AfterCards lines, then the story's stand-up animation.

        // During the game the scene's Mita is switched off and a second Mita ("Game Card>MitaGame", the original model)
        // sits at the table; the table's interactive is gone after one game.
        ObjectAnimationPlayer cardStandUp;
        bool cardGame;
        float cardEndAt = -1f;
        GameObject mitaGameGo, cardsTemplate;
        MitaPerson mitaGamePerson;
        Transform mitaGameBody, cardsParent;
        Vector3 cardsLocalPos, cardsLocalScale;
        Quaternion cardsLocalRot;
        MitaSkin gameSkin;
        bool gameSkinTried;

        void HookCardTable()
        {
            var cards = Util.FindDeepAll("Interactive Cards >>");
            var oi = cards != null ? cards.GetComponent<ObjectInteractive>() : null;
            if (oi == null) { Plugin.Log.LogWarning("[peace] no card table interactive"); return; }
            oi.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnCardGameStart)));
            // a spare copy to put back after a game
            if (cardsTemplate == null)
            {
                cardsParent = cards.parent;
                cardsLocalPos = cards.localPosition; cardsLocalRot = cards.localRotation; cardsLocalScale = cards.localScale;
                cardsTemplate = Instantiate(cards.gameObject, templates.transform);
                cardsTemplate.name = "Interactive Cards >>";
            }
            var mg = Util.FindPath("Game Card>MitaGame");
            if (mg != null)
            {
                mitaGameGo = mg;
                mitaGamePerson = mg.GetComponent<MitaPerson>();
                mitaGameBody = mitaGamePerson != null && mitaGamePerson.nma != null ? mitaGamePerson.nma.transform : mg.transform;
            }
            Plugin.Log.LogInfo($"[peace] card table hooked (table Mita {(mitaGameGo != null)})");
        }

        bool TableMitaUp => cardGame && mitaGameGo != null && mitaGameGo.activeInHierarchy;
        // who is talking right now: the Mita at the card table during a game, else the scene's Mita
        Transform SpeakerBody => TableMitaUp ? mitaGameBody : mitaBody;
        MitaPerson SpeakerPerson => TableMitaUp && mitaGamePerson != null ? mitaGamePerson : mita;

        void OnCardGameStart()
        {
            Plugin.Log.LogInfo("[peace] card game starts");
            cardGame = true;
            ui.CloseMenu();
            // the click already stopped the roaming addon: keep our talks from starting it again until you stand up
            addonHeld = true;
        }

        // the table's Mita wears the chosen Mita's look too (she appears a moment after the click)
        void TickTableSkin()
        {
            if (!TableMitaUp || gameSkinTried || string.IsNullOrEmpty(Pn.Skin)) return;
            gameSkinTried = true;
            try { gameSkin = MitaSkin.Apply(mitaGameBody, Pn.Skin, null, Pn.SkinScale); }
            catch (Exception e) { Plugin.Log.LogError("[peace] table skin failed: " + e); gameSkin = null; }
            if (gameSkin != null) Plugin.Log.LogInfo("[peace] table Mita dressed as " + Pn.Id);
        }

        void OnCardGameStopped()
        {
            Plugin.Log.LogInfo("[peace] card game over");
            cardEndAt = Time.time + 2f;   // let the "game over" sitting pose settle first
        }

        void TickCardGame()
        {
            if (cardEndAt < 0 || Time.time < cardEndAt || busy) return;
            cardEndAt = -1f;
            Play(L.AfterCards ?? new Line[0], () =>
            {
                if (cardStandUp != null)
                {
                    try { cardStandUp.AnimationPlay(); return; }
                    catch (Exception e) { Plugin.Log.LogWarning("[peace] stand up: " + e.Message); }
                }
                try { pm?.AnimationStopFull(); } catch { }
                OnCardGameFinished();
            });
        }

        void OnCardGameFinished()
        {
            Plugin.Log.LogInfo("[peace] card game finished");
            Did("cards");
            // the table's Mita leaves, the real one is back, standing by the table (the story switched them in its next scene)
            var standNear = mitaGameBody != null ? mitaGameBody.position : (pm != null ? pm.transform.position : Vector3.zero);
            if (gameSkin != null) { gameSkin.Remove(); gameSkin = null; }
            gameSkinTried = false;
            if (mitaGameGo != null && mitaGameGo.activeSelf) mitaGameGo.SetActive(false);
            cardGame = false;
            if (mita != null && !mita.gameObject.activeSelf) mita.gameObject.SetActive(true);
            if (mitaBody != null)
            {
                var spot = UnityEngine.AI.NavMesh.SamplePosition(standNear, out var hit, 2f, -1) ? hit.position : standNear;
                var face = (pm != null ? pm.transform.position : spot + Vector3.forward) - spot; face.y = 0;
                // her face is on the agent's -Z side (see ReleasePose)
                mitaBody.SetPositionAndRotation(spot, Quaternion.LookRotation(face.sqrMagnitude > 0.001f ? -face : Vector3.back));
                if (mita.nma != null) { mita.nma.enabled = true; try { mita.nma.Warp(spot); mita.nma.isStopped = false; } catch { } }
                SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            }
            addonHeld = false;
            if (addon != null) { addon.StartAddon(); addon.ActivationAddon(false); addon.InteractiveActive(false); }
            // the table again, for the next game
            var cards = Util.FindPath("Addon>Interactive Cards >>");
            if (cards == null && cardsTemplate != null && cardsParent != null)
            {
                cards = Instantiate(cardsTemplate, cardsParent);
                cards.name = "Interactive Cards >>";
                cards.transform.localPosition = cardsLocalPos; cards.transform.localRotation = cardsLocalRot; cards.transform.localScale = cardsLocalScale;
                var oi = cards.GetComponent<ObjectInteractive>();
                oi?.eventClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnCardGameStart)));
                Plugin.Log.LogInfo("[peace] card table put back");
            }
            if (cards != null && !cards.activeSelf) cards.SetActive(true);
            AddAff(2);
            CheckMilestones();
        }

        // ---------------------------------------------------------------- update

        void Update()
        {
            if (!ready)
            {
                if (++initFrames >= 3) { try { Init(); } catch (Exception e) { Plugin.Log.LogError("[peace] init failed: " + e); } }
                return;
            }
            MuteDub();   // (kept off: the game sets it again when its options are touched)
            try { Tick(); } catch (Exception e) { Plugin.Log.LogError("[peace] tick: " + e); }
            try { OverlayGuard.Tick(true); } catch (Exception e) { Plugin.Log.LogError("[peace] overlay guard: " + e); }
        }

        void Tick()
        {
            SceneImport.Tick();
            // listening to her (lines, a staged scene) is not being away: no handheld console coming out meanwhile
            if (gc != null && (busy || queue.Count > 0 || fadeTarget > 0f || hugging)) gc.timeAFK = 0f;
            float dt = Time.deltaTime;
            S.Minute += dt * PeaceMode.TimeScale.Value;
            if (S.Minute >= 1440f) { S.Minute -= 1440f; S.Day++; lastEventHour = -1; }

            guardTimer -= dt;
            if (guardTimer <= 0)
            {
                guardTimer = 0.5f;
                foreach (var g in storyOff) if (g != null && g.activeSelf) g.SetActive(false);
            }

            UpdateDialogue();
            UpdateFade(dt);

            ChatClient.Drain();
            if (chatOpen) TickChat();   // typing: none of the menu keys
            else if (MenuOpen)
            {
                HoldControls(true);
                for (int i = 0; i < 9; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { Choose(i); break; }
                // a tenth option (at meal times) is on 0
                if (MenuOpen && (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))) Choose(9);
                if (MenuOpen && (Input.GetKeyDown(PeaceMode.InteractKey.Value) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
                    CloseMenus();
                if (!MenuOpen && !pendingMenu) HoldControls(false);
            }
            else if (controlsHeld && !pendingMenu) HoldControls(false);
            pendingMenu = false;

            bool canInteract = freeMode && !busy && !MenuOpen && fadeTarget <= 0 && !cardGame && !GameRunning;
            SetMitaInteractive(canInteract);
            SetSpots(canInteract);
            bool near = canInteract && PlayerNearMita(2.2f, 35f);
            ui.ShowPrompt(gamePrompt ?? (near && mitaInteractive == null ? "[" + PeaceMode.InteractKey.Value + "] " + Loc.F("{0}와 교감하기", ShortName) : null));
            if (near && Input.GetKeyDown(PeaceMode.InteractKey.Value)) OpenMainMenu();

            TrackWall();
            try { TrackMore(Time.realtimeSinceStartup, Personas.Wall(Pn.Id)); } catch (Exception e) { Plugin.Log.LogError("[peace] fourth wall: " + e); }
            TickPhoto();
            TickCardGame();
            TickTableSkin();
            TickCooking();
            TickEarlyImport();
            if (importAt < 0f && !cookingLoading) TickAnimImports();   // after Scene 3
            TickTimers();
            TickAttention();
            // (the weather is scenery: a failure there must not stop the rest of her day)
            try { TickWeather(); } catch (Exception e) { if (Time.time > weatherErrorAt) { weatherErrorAt = Time.time + 60f; Plugin.Log.LogError("[weather] " + e); } }
            TickMeal();
            TickStandUp();
            TickMusic();
            TickChoices();
            TickGames();
            TickDance();
            TickHammer();
            // the player inside one of the story's own animations: at the TV, at the card table, cutting the carrot...
            bool playerInScene = pm != null && pm.animationRun;
            if (freeMode && !cardGame && !playerInScene) CheckHeadGesture();
            // nothing new starts while the screen is still dark, during a story minigame or right after she finished talking
            bool calm = freeMode && !busy && !MenuOpen && fadeTarget <= 0 && fadeVal <= 0.001f && afterFade == null
                        && photoStage == 0 && !cardGame && !playerInScene && !GameRunning && Time.time >= quietUntil;
            TickRoutine(calm && !DevCalm);
            if (busy) calm = false;   // she has just started talking about her plans
            // asleep (or on her way to bed) she has nothing to say
            bool awake = activity != MitaActivity.Sleeping && activity != MitaActivity.ToBed;
            if (calm && awake && !DevCalm)
            {
                if (pendingWall.Count > 0 && (!posed || showcasing || activity == MitaActivity.Cooking))
                {
                    var (p, away) = pendingWall.Dequeue();
                    if (away != null) awayText = away;
                    Play(p, null, true);
                }
                CheckTimeEvents();
                if (!busy) CheckTierChange();
                if (!busy && Time.time > idleNext)
                {
                    idleNext = Time.time + Random.Range(Pn.IdleMin, Pn.IdleMax);
                    if (PlayerNearMita(7f, 360f) && !TryRequest() && !TryPropose() && !TryWeatherTalk())
                    {
                        bool drowsy = Pn.SpecialKind == SpecialKind.Coffee && L.Drowsy != null && Time.time > awakeUntil && Random.value < 0.6f;
                        bool cold = !drowsy && Cold && A.ColdIdle != null;
                        var arr = drowsy ? L.Drowsy : cold ? A.ColdIdle : L.Idle[CurrentTod()];
                        Play(new[] { new Line(Who.Mita, arr[Random.Range(0, arr.Length)], drowsy ? "halfsleep" : cold ? (Tier <= 2 ? "contempt" : "discontent") : Pn.IdleEmo) }, null, true);
                    }
                }
            }

            if (PeaceMode.ShowHud.Value)
                ui.SetHud(Loc.F("평화 모드  ·  {0}  ·  {1}일차  ·  {2} {3:00}:{4:00}  ·  호감도 {5}", ShortName, S.Day, Loc.T(TodName()), (int)(S.Minute / 60), (int)(S.Minute % 60), TierText(S.Affection)), true);
            else ui.SetHud("", false);

            saveTimer -= dt;
            if (saveTimer <= 0) { saveTimer = 20f; S.Write(); }   // often, so a crash still leaves a close "last seen" 
        }

        void LateUpdate()
        {
            if (!ready) return;
            KeepPose();
            KeepPlant();
            TickHoldHandsView();
            TickLounge();
            // (after the game's own Update has placed the text from your idle eyes)
            if (current != null && !currentIsMita) { try { if (current.activeSelf) PlacePlayerLine(current); } catch { } }
            if (current != null) { try { FixLineLayout(); } catch (Exception e) { if (!layoutWarned) { layoutWarned = true; Plugin.Log.LogWarning("[peace] line layout: " + e.Message); } } }
            TickGestureProbe();
            ApplyFaceOverlay();
            SyncSkin();
            PlaceQuestIcon();
            try { ApplyLighting(); ApplyAtmosphere(); } catch (Exception e) { Plugin.Log.LogError("[peace] lighting: " + e); }
        }

        bool pendingMenu;

        bool PlayerNearMita(float dist, float angle)
        {
            if (pm == null || mitaBody == null) return false;
            var to = mitaBody.position - pm.transform.position;
            to.y = 0;
            if (to.magnitude > dist) return false;
            if (angle >= 360f) return true;
            var cam = pm.mainCamera != null ? pm.mainCamera : pm.transform;
            var fwd = cam.forward; fwd.y = 0;
            return Vector3.Angle(fwd, to) < angle;
        }

        void HoldControls(bool on)
        {
            controlsHeld = on;
            if (pm != null) { pm.dontMove = on; pm.stopMouseMove = on; }
            // (standing still in a menu or typing to her is not being away: no handheld console coming out)
            if (on && gc != null) gc.timeAFK = 0f;
            if (gc != null) gc.showCursor = on;
            Cursor.visible = on;
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // ---------------------------------------------------------------- time of day

        Tod CurrentTod()
        {
            float h = S.Minute / 60f;
            if (h < 6f) return Tod.Night;
            if (h < 11f) return Tod.Morning;
            if (h < 17f) return Tod.Day;
            if (h < 21f) return Tod.Evening;
            return Tod.Night;
        }

        string TodName() => CurrentTod() switch { Tod.Morning => "아침", Tod.Day => "낮", Tod.Evening => "저녁", _ => "밤" };

        bool IsNight() => S.Minute >= Pn.NightHour * 60 || S.Minute < 5 * 60;

        void CheckTimeEvents()
        {
            int hour = (int)(S.Minute / 60);
            if (hour == lastEventHour) return;
            foreach (var ev in L.TimeEvents)
            {
                if (ev.hour != hour) continue;
                lastEventHour = hour;
                // (not the lunch line while lunch is on the stove, on the table or already eaten)
                if (MealTime() && (cooking || mealSet != null || S.LastMeal == MealKey() || activity != MitaActivity.Free)) return;
                Play(ev.lines);
                return;
            }
        }

        // a borrowed story game can colour the room's ambient light (the dance battle's red) and ApplyLighting would take that
        // as the room's own from then on: the room's own is kept while the game runs and comes back after it
        Color? keptAmbient;
        void KeepAmbient() { if (!keptAmbient.HasValue && ambInit) keptAmbient = ambBase; }
        void RestoreAmbient()
        {
            if (!keptAmbient.HasValue) return;
            ambBase = keptAmbient.Value;
            keptAmbient = null;
            ambSet = RenderSettings.ambientLight;   // (so the game's colour isn't taken as the base once more)
            ApplyLighting();
        }

        void ApplyLighting()
        {
            float h = S.Minute / 60f;
            var sl = SL;   // (the season: day length, the sun's colour and strength - PeaceWeather)
            float f;
            if (h < sl.dawn0) f = 0.2f;
            else if (h < sl.dawn1 + 1f) f = Mathf.Lerp(0.2f, 1f, (h - sl.dawn0) / (sl.dawn1 + 1f - sl.dawn0));
            else if (h < sl.dusk0) f = 1f;
            else if (h < sl.dusk1) f = Mathf.Lerp(1f, 0.2f, (h - sl.dusk0) / (sl.dusk1 - sl.dusk0));
            else f = 0.2f;

            Color night = new Color(0.5f, 0.55f, 0.9f), dawn = new Color(1f, 0.82f, 0.68f), noon = sl.sunTint, dusk = new Color(1f, 0.62f, 0.45f);
            Color tint;
            if (h < sl.dawn0) tint = night;
            else if (h < sl.dawn1) tint = Color.Lerp(night, dawn, (h - sl.dawn0) / (sl.dawn1 - sl.dawn0));
            else if (h < sl.dawn1 + 3f) tint = Color.Lerp(dawn, noon, (h - sl.dawn1) / 3f);
            else if (h < sl.dusk0 - 1.5f) tint = noon;
            else if (h < sl.dusk0 + 1.5f) tint = Color.Lerp(noon, dusk, (h - sl.dusk0 + 1.5f) / 3f);
            else if (h < sl.dusk1 + 0.5f) tint = Color.Lerp(dusk, night, (h - sl.dusk0 - 1.5f) / Mathf.Max(0.5f, sl.dusk1 + 0.5f - sl.dusk0 - 1.5f));
            else tint = night;
            // clouds and rain: a weaker, greyer sun
            var grey = new Color(0.78f, 0.82f, 0.9f);
            tint = Color.Lerp(tint, new Color(tint.grayscale, tint.grayscale, tint.grayscale) * grey, gloom);

            if (sun != null) { sun.intensity = sunInt0 * f * sl.sun * (1f - gloom * 0.6f); sun.color = sunCol0 * tint; }

            var curAmb = RenderSettings.ambientLight;
            var curInt = RenderSettings.ambientIntensity;
            if (!ambInit || curAmb != ambSet) ambBase = curAmb;
            if (!ambInit || Mathf.Abs(curInt - ambIntSet) > 0.0001f) ambIntBase = curInt;
            ambInit = true;
            var mult = Color.Lerp(tint * 0.55f, Color.Lerp(Color.white, sl.sunTint, 0.5f), f);   // (the room takes on the season's light)
            ambSet = new Color(ambBase.r * mult.r, ambBase.g * mult.g, ambBase.b * mult.b, ambBase.a);
            ambIntSet = ambIntBase * Mathf.Lerp(0.6f, 1f, f) * Mathf.Lerp(0.92f, 1.05f, (sl.sun - 0.75f) / 0.4f) * (1f - gloom * 0.2f);
            RenderSettings.ambientLight = ambSet;
            RenderSettings.ambientIntensity = ambIntSet;
        }

        // ---------------------------------------------------------------- menus

        void OpenMainMenu()
        {
            if (activity == MitaActivity.Sleeping) { OpenSleepingMenu(); return; }
            if (RequestBack()) return;   // back from her favour: that comes first
            var opts = new List<string> { "대화하기", "교감하기", "같이 놀자", following ? "여기서 기다려" : "같이 다니자" };
            var acts = new List<Action> { OpenTalkMenu, OpenTouchMenu, OpenPlayMenu, () => { if (following) StopFollow(true); else StartFollow(); } };
            if (S.Affection >= 15) { opts.Add("특별한 시간"); acts.Add(OpenMomentsMenu); }
            if (!string.IsNullOrEmpty(Pn.SpecialName)) { opts.Add(Pn.SpecialName); acts.Add(DoSpecial); }
            if (cooking) { opts.Add("요리 그만하기"); acts.Add(StopCooking); }
            else if (MealTime() && S.LastMeal != MealKey())
            {
                // once she has finished cooking on her own, it's only eating left
                if (activity != MitaActivity.MealReady) { opts.Add("같이 요리하기"); acts.Add(DoCook); }
                opts.Add("같이 밥 먹기"); acts.Add(DoMeal);
            }
            if (OnSofaAlone) { opts.Insert(0, "옆에 앉기"); acts.Insert(0, JoinHerOnSofa); }
            if (IsNight()) { opts.Add("이제 자자"); acts.Add(DoSleep); }
            else if (Pn.NapAnytime) { opts.Add("같이 낮잠 자기"); acts.Add(DoSleep); }
            opts.Add("오늘은 여기까지…"); acts.Add(OpenLeaveMenu);
            opts.Add("닫기"); acts.Add(() => { });
            ShowMenu(Pn.Short, opts, acts);
        }

        List<string> lastMenuOpts;

        [HideFromIl2Cpp] void ShowMenu(string title, List<string> opts, List<Action> acts)
        {
            lastMenuOpts = opts;
            pendingMenu = true;
            HoldControls(true);
            mitaLook?.LookOnPlayer();
            // the story's own choice list once Scene 3 has lent it (a few seconds into the day), our menu before that
            Action<int> choose = i => { pendingMenu = false; acts[i](); };
            // (shown translated; the options themselves stay as written, for the checks on them)
            if (!OpenChoices(opts, choose)) ui.OpenMenu(Loc.T(title), opts.Select(Loc.T).ToList(), choose);
        }

        void OpenTouchMenu()
        {
            var opts = new List<string> { "쓰다듬기", "안아주기", "칭찬하기" };
            var acts = new List<Action> { DoPat, DoHug, DoCompliment };
            if (glassesTemplate != null && Pn.Id != "mila" && Pn.Id != "chibi") { opts.Add(GlassesWorn ? "안경 벗기" : "안경 써 줘"); acts.Add(DoGlasses); }
            if (Pn.Id == "crazy" && skin == null) { opts.Add("옷 갈아입기"); acts.Add(OpenClothesMenu); }   // (the others are their own models)
            opts.Add("선물하기"); acts.Add(OpenGiftMenu);
            opts.Add("그만두기"); acts.Add(() => { });
            ShowMenu("교감하기", opts, acts);
        }

        void OpenPlayMenu()
        {
            var opts = new List<string> { "가위바위보", "끝말잇기", "넌센스 퀴즈", "참참참", "숨바꼭질", "카드 게임", "TV 게임", "같이 사진 찍기", "그만두기" };
            var acts = new List<Action>
                {
                    () => Play(L.RpsStart, () => ShowMenu("가위바위보", new List<string> { "가위", "바위", "보" },
                        new List<Action> { () => Rps(0), () => Rps(1), () => Rps(2) })),
                    StartWordChain,
                    StartRiddles,
                    StartCham,
                    StartHide,
                    () => Play(L.CardsInvite),
                    () => Play(L.TvInvite),
                    DoPhoto,
                    () => { },
                };
            // Cappie's dance pad (her game in the story)
            if (HammerReady) { opts.Insert(0, "망치 게임"); acts.Insert(0, StartHammer); }
            if (DanceReady) { opts.Insert(0, "댄스 배틀"); acts.Insert(0, StartDance); }
            ShowMenu("같이 놀자", opts, acts);
        }

        void DoTalk()
        {
            Did("talk");
            var tod = CurrentTod();
            var pool = L.Topics.Where(t => (t.when & tod) != 0 && t.minAff <= S.Affection && !recentTopics.Contains(t.id)).ToList();
            if (pool.Count == 0) pool = L.Topics.Where(t => (t.when & tod) != 0 && t.minAff <= S.Affection).ToList();
            var topic = pool[Random.Range(0, pool.Count)];
            recentTopics.Enqueue(topic.id);
            while (recentTopics.Count > 8) recentTopics.Dequeue();
            S.Talks++;
            AddAff(1);
            Play(topic.lines, CheckMilestones);
        }

        void DoPat()
        {
            Did("touch");
            S.Pats++;
            if (Cold && A.ColdTouch != null) { Play(Pick(A.ColdTouch)); return; }
            if (S.Affection < Pn.PatMinAff && L.PatReject != null) { Play(Pick(L.PatReject)); return; }
            if (Time.time - lastPat > 45f) AddAff(2);
            lastPat = Time.time;
            var set = S.Affection < 15 ? L.PatLow : S.Affection < 50 ? L.PatMid : L.PatHigh;
            Play(Pick(set), CheckMilestones);
        }

        void DoHug()
        {
            Did("touch");
            S.Hugs++;
            if (Cold && A.ColdTouch != null) { Play(Pick(A.ColdTouch)); return; }
            if (S.Affection < Pn.HugMinAff && L.HugReject != null) { Play(Pick(L.HugReject)); return; }
            Line[][] set;
            if (S.Affection < 20) { set = L.HugLow; if (Time.time - lastHug > 60f) AddAff(1); }
            else { set = S.Affection < 50 ? L.HugMid : L.HugHigh; if (Time.time - lastHug > 60f) AddAff(3); }
            lastHug = Time.time;
            var lines = Pick(set);
            if (!StageHug(lines, CheckMilestones)) Play(lines, CheckMilestones);
        }

        void DoCompliment()
        {
            Did("touch");
            S.Compliments++;
            if (Time.time - lastCompliment > 45f) AddAff(2);
            lastCompliment = Time.time;
            Play(Pick(L.Compliments), CheckMilestones);
        }

        void Rps(int player)
        {
            Did("rps");
            int m = Random.Range(0, 3);
            string[] names = { "가위", "바위", "보" };
            var intro = new List<Line> { new Line(Who.Player, Loc.T(names[player]) + "!"), new Line(Who.Mita, Loc.T(names[m]) + "!", "smileteeth", "Mita IdleHand") };
            int res = (player - m + 3) % 3; // 1 = player wins, 2 = mita wins, 0 = draw
            S.RpsGames++;
            Line[] outcome;
            if (res == 0) outcome = Pick(L.RpsDraw);
            else if (res == 1) { S.RpsWins++; outcome = Pick(L.RpsWin); }
            else outcome = Pick(L.RpsLose);
            // (Sleepy Mita's reactions to the switch game: you won / you lost)
            if (res == 1) NextGesture("Mita Player Win"); else if (res == 2) NextGesture("Mita Player Lose");
            AddAff(1);
            Play(intro.Concat(outcome).ToList(), () => ShowMenu("한 판 더?", new List<string> { "한 판 더!", "그만할래" },
                new List<Action> { () => ShowMenu("가위바위보", new List<string> { "가위", "바위", "보" }, new List<Action> { () => Rps(0), () => Rps(1), () => Rps(2) }), () => { } }));
        }

        float lastSpecial = -100f, awakeUntil;

        // the Mita's own action: plain lines, coffee (keeps Sleepy Mita awake for a while) or a quiz
        void DoSpecial()
        {
            if (Pn.SpecialKind == SpecialKind.Quiz) { DoQuiz(); return; }
            if (Pn.SpecialKind == SpecialKind.Coffee) awakeUntil = Time.time + 300f;
            if (Time.time - lastSpecial > 60f) AddAff(2);
            lastSpecial = Time.time;
            if (L.Special == null || L.Special.Length == 0) return;
            var lines = Pick(L.Special);
            if (Pn.Id == "crazy" && StageHoldHands(lines, CheckMilestones)) return;   // (her "hold hands")
            NextGesture(SpecialGesture());
            Play(lines, CheckMilestones);
        }

        void DoQuiz()
        {
            if (L.Quiz == null || L.Quiz.Length == 0) return;
            var qz = Pick(L.Quiz);
            var order = Enumerable.Range(0, qz.opts.Length).OrderBy(_ => Random.value).ToList();
            Play(new[] { new Line(Who.Mita, qz.q, "smiletonque", "Mita IdleTalk") }, () => ShowMenu(Pn.SpecialName,
                order.Select(i => qz.opts[i]).ToList(),
                order.Select(i => (Action)(() => QuizAnswer(i == 0))).ToList()));
        }

        void QuizAnswer(bool right)
        {
            if (!right) { if (L.QuizWrong != null) Play(Pick(L.QuizWrong)); return; }
            if (Time.time - lastSpecial > 30f) AddAff(2);
            lastSpecial = Time.time;
            if (L.QuizRight != null) Play(Pick(L.QuizRight), CheckMilestones);
        }

        // ---------------------------------------------------------------- staged scenes: bed and table

        // the bedroom bed (long side along X) and the kitchen table with its four chairs
        static readonly Vector3 BedCenter = new Vector3(10.86f, 0f, -5.20f);
        static readonly Vector3 BedHeadDir = Vector3.right;
        const float BedTop = 0.56f;
        static readonly Vector3 BedsidePlayer = new Vector3(10.86f, 0f, -3.75f);
        static readonly Vector3 SeatMita = new Vector3(8.73f, 0f, 3.64f);
        static readonly Vector3 SeatPlayerSide = new Vector3(8.68f, 0f, 1.85f);

        bool posed;
        Vector3 posePos;
        Quaternion poseRot;

        // Put Mita somewhere with her face towards `front` and her head towards `up` (lying down: front = up, up = along the bed).
        // Works from her (animated) pelvis and head, so it doesn't matter which way her skeleton is built.
        void PoseMita(Vector3 hipsTarget, Vector3 front, Vector3 up)
        {
            if (mitaBody == null) return;
            var hips = Util.FindDeep(mitaBody, "Hips");
            var head = Util.FindDeep(mitaBody, "Head");
            if (hips == null || head == null) return;
            HoldAddon(true);
            if (mita.nma != null) { try { mita.nma.isStopped = true; } catch { } mita.nma.enabled = false; }
            var inv = Quaternion.Inverse(mitaBody.rotation);
            var localSpine = inv * (head.position - hips.position).normalized;
            var localFront = Vector3.ProjectOnPlane(inv * hips.forward, localSpine).normalized;
            var rot = Quaternion.LookRotation(front, up) * Quaternion.Inverse(Quaternion.LookRotation(localFront, localSpine));
            var hipsLocal = inv * (hips.position - mitaBody.position);
            posePos = hipsTarget - rot * hipsLocal;
            poseRot = rot;
            uprightFront = Vector3.zero; posed = true;
            lying = true; lieHips = hipsTarget; lieFront = front; lieUp = up; poseHips = hips; poseHead = head;
            mitaBody.SetPositionAndRotation(posePos, poseRot);
            poseLog = 6;
            Plugin.Log.LogInfo($"[peace] pose: body {mitaBody.name} -> {posePos} rot {poseRot.eulerAngles}; hips now {hips.position} (target {hipsTarget})");
        }

        // upright poses (sitting): root stays on the floor at `floorPos`, only turned so her pelvis faces `front`
        void PoseMitaUpright(Vector3 floorPos, Vector3 front)
        {
            if (mitaBody == null) return;
            var hips = Util.FindDeep(mitaBody, "Hips");
            HoldAddon(true);
            try { mita?.AiShraplyStop(); } catch { }   // (after a walk of her own her AI keeps playing the walk's end)
            if (mita.nma != null) { try { mita.nma.isStopped = true; } catch { } mita.nma.enabled = false; }
            // her face is on the agent's -Z side (same as ReleasePose); the hips bone axis depends on the clip playing
            var f = front; f.y = 0;
            var rot = Quaternion.LookRotation(f.sqrMagnitude > 0.0001f ? -f.normalized : Vector3.back);
            posePos = floorPos; poseRot = rot; posed = true; uprightFront = f.normalized;
            lying = false; poseHips = hips;
            mitaBody.SetPositionAndRotation(posePos, poseRot);
            Plugin.Log.LogInfo($"[peace] upright pose at {floorPos} yaw {rot.eulerAngles.y:F0} hipsFwd {(hips != null ? hips.forward.ToString() : "-")}");
        }

        int poseLog;

        bool eating;
        float nextBite;

        void KeepPose()
        {
            KeepCooking();
            KeepHeldClip();
            if (eating && Time.time >= nextBite)
            {
                nextBite = Time.time + 8.2f;
                var bite = Clip("Mita Lolipop");
                if (bite != null && mitaAnim != null) try { mitaAnim.AnimationClipSimpleNext(bite); } catch { }
            }
            if (!posed || mitaBody == null) return;
            if (poseLog > 0)
            {
                poseLog--;
                var hips = Util.FindDeep(mitaBody, "Hips");
                Plugin.Log.LogInfo($"[peace] pose check: body at {mitaBody.position} rot {mitaBody.eulerAngles} hips {(hips != null ? hips.position.ToString() : "-")} parent={(mitaBody.parent != null ? mitaBody.parent.name : "-")} nma={(mita.nma != null && mita.nma.enabled)}");
            }
            mitaBody.SetPositionAndRotation(posePos, poseRot);
            // lying: whatever clip plays, keep pelvis->head along the bed and her face up, pelvis on the mattress
            if (lying && poseHips != null && poseHead != null)
            {
                var spine = poseHead.position - poseHips.position;
                var f = spine.sqrMagnitude > 0.0001f ? Vector3.ProjectOnPlane(poseHips.forward, spine.normalized) : Vector3.zero;
                if (f.sqrMagnitude > 0.0001f)
                {
                    var delta = Quaternion.LookRotation(lieFront, lieUp) * Quaternion.Inverse(Quaternion.LookRotation(f.normalized, spine.normalized));
                    if (Quaternion.Angle(delta, Quaternion.identity) > 1f) mitaBody.rotation = delta * mitaBody.rotation;
                    mitaBody.position += lieHips - poseHips.position;
                }
            }
            // some clips (the snack one) turn her pelvis around: keep her facing the way she was posed
            if (uprightFront != Vector3.zero)
            {
                var hips = poseHips;
                if (hips != null)
                {
                    var hf = hips.forward; hf.y = 0;
                    if (hf.sqrMagnitude > 0.0001f)
                    {
                        float d = Vector3.SignedAngle(hf, uprightFront, Vector3.up);
                        if (Mathf.Abs(d) > 1f) mitaBody.RotateAround(hips.position, Vector3.up, d);
                    }
                }
            }
            // sitting (the sofa): whatever clip, her hips on the cushion
            if (seatHips != Vector3.zero && poseHips != null) mitaBody.position += seatHips - poseHips.position;
        }

        // the scene's free-roam addon walks Mita between points (and turns her agent back on): off while she's posed
        bool addonHeld;
        void HoldAddon(bool hold)
        {
            if (addon == null || hold == addonHeld) return;
            addonHeld = hold;
            try
            {
                // stopping the addon drops her idle (T-pose): put her own idle back
                SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
                var idle = (skin != null && !skin.OwnAnimator ? skin.IdleClip : null) ?? Clip("Mita Idle") ?? originalIdle;
                if (hold)
                {
                    mita?.AiShraplyStop();
                    addon.StopAddon();
                    // leave whatever behaviour clip the addon was playing ("Mita Something" leaning): a simple clip
                    // returns to the idle state when it ends, so play her idle once (not over a gesture of hers: it ends in her idle too)
                    if (idle != null && mitaAnim != null && Time.time >= gestureUntil) try { mitaAnim.AnimationClipSimpleNext(idle); } catch (Exception e) { Plugin.Log.LogWarning("[peace] re-idle: " + e.Message); }
                }
                else if (freeMode) { addon.StartAddon(); addon.ActivationAddon(false); addon.InteractiveActive(false); }
            }
            catch (Exception e) { Plugin.Log.LogWarning("[peace] addon hold: " + e.Message); }
            Plugin.Log.LogInfo("[peace] roaming " + (hold ? "paused" : "resumed"));
        }

        Vector3 uprightFront, lieHips, lieFront, lieUp;
        bool lying;
        Transform poseHips, poseHead;

        void ReleasePose(Vector3 standAt, Vector3 faceTowards)
        {
            if (!posed) return;
            posed = false; lying = false;
            var f = faceTowards - standAt; f.y = 0;
            // her face is on the agent's -Z side (see stage notes)
            mitaBody.SetPositionAndRotation(standAt, Quaternion.LookRotation(f.sqrMagnitude > 0.001f ? -f : Vector3.back));
            if (mita.nma != null) { mita.nma.enabled = true; try { mita.nma.Warp(standAt); mita.nma.isStopped = false; } catch { } }
            HoldAddon(false);
            try { mitaLook?.Activation(true); } catch { }
        }

        void MovePlayer(Vector3 pos, Vector3 lookAt)
        {
            if (pm == null) return;
            var d = lookAt - pos; d.y = 0;
            float yaw = Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d : Vector3.forward).eulerAngles.y;
            try { pm.TeleportPlayer(pos, yaw); } catch (Exception e) { Plugin.Log.LogWarning("[peace] teleport: " + e.Message); }
        }

        void SetProps(bool on)
        {
            if (mitaBody == null) return;
            foreach (var n in new[] { "Cookie", "Juice" })
            {
                var t = Util.FindDeep(mitaBody, n);
                if (t != null && t.GetComponent<Renderer>() != null) t.gameObject.SetActive(on);
            }
        }

        AnimationClip Clip(string n) => clips.TryGetValue(n, out var c) ? c : null;

        // The house only keeps the room you are in switched on (House>Bedroom/Kitchen/Main/Toilet + "<room> Lighting").
        // When a scene moves you to another room, switch that room on ourselves.
        public void ShowRoom(string room)
        {
            var house = Util.FindPath("HouseGame Tamagotchi>House") ?? Util.FindPath("House>House");
            if (house == null) return;
            foreach (var n in new[] { room, room + " Lighting" })
            {
                var t = house.transform.Find(n);
                if (t != null && !t.gameObject.activeSelf) t.gameObject.SetActive(true);
            }
        }

        void DoSleep()
        {
            bool nap = !IsNight() && Pn.NapAnytime;
            if (!IsNight() && !nap) { Play(L.SleepNotYet); return; }
            var invite = L.BedInvite ?? (nap ? L.SleepNotYet : null);
            EndRoutine();
            Play((IList<Line>)invite ?? new Line[0], () => { fadeTarget = 1f; afterFade = () => SleepScene(nap); });
        }

        // (screen black) the two of you at the bed until the morning (or the end of a nap)
        void SleepScene(bool nap)
        {
            {
                {
                    // in bed: on her back, head on the pillow side; you stand at the bedside
                    try { mitaLook?.Activation(false); } catch { }
                    ClearLeftoverText();
                    ShowRoom("Bedroom");
                    LieInBed(true);
                    MovePlayer(BedsidePlayer, BedCenter);
                    PlayerLieDown();
                    inBedTalk = true;
                    fadeTarget = 0f;
                    afterFade = () => Play(nap ? L.SleepNotYet : Diary().Concat(L.Goodnight).ToList(), () =>
                    {
                        inBedTalk = false;
                        SleepFace(true);
                        // a moment with her eyes closed before the screen goes dark
                        fadeHoldUntil = Time.time + 2.2f;
                        fadeTarget = 1f;
                        afterFade = () =>
                        {
                            if (nap) S.Minute = Mathf.Min(S.Minute + 180f, 1439f);
                            else
                            {
                                if (S.Minute >= 12 * 60) S.Day++;
                                S.Minute = Pn.WakeHour * 60;
                            }
                            lastEventHour = (int)(S.Minute / 60);
                            S.Write();
                            // up at the foot end of the bedside, next to you (not inside you)
                            ClearLeftoverText();
                            if (PlayerGetUp()) MovePlayer(BedsidePlayer, BedCenter);
                            UpFromBed();
                            ReleasePose(new Vector3(9.8f, 0f, -3.65f), BedsidePlayer);
                            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
                            fadeTarget = 0f;
                            afterFade = () => Play(nap ? (IList<Line>)(L.NapWake ?? new Line[0]) : MorningLines());
                        };
                    });
                }
            }
        }

        // meals: breakfast 7-10, lunch 11:30-14, dinner 17:30-20:30, once each
        bool MealTime()
        {
            float h = S.Minute / 60f;
            return (h >= 7f && h < 10f) || (h >= 11.5f && h < 14f) || (h >= 17.5f && h < 20.5f);
        }

        string MealKey()
        {
            float h = S.Minute / 60f;
            return S.Day + (h < 10.5f ? "b" : h < 15f ? "l" : "d");
        }

        void DoMeal()
        {
            if (!MealTime() || S.LastMeal == MealKey()) return;
            S.LastMeal = MealKey();
            EndRoutine();
            // the story's table set is borrowed from Scene 3 while the screen is dark (once)
            Play((IList<Line>)L.MealInvite ?? new Line[0], () => { fadeTarget = 1f; afterFade = () => ImportCooking(MealSceneAtTable); });
        }

        // (screen is black) the two of you at the kitchen table, talking over the food, then back to the day
        void MealSceneAtTable()
        {
            // at the table: you sit down with the story's plates (Scene 3's table set) and can eat; Mita sits across
            // the table and eats too (Scene 3's clip, see SeatMitaAtTable). Without the borrowed set you both stand at
            // the table as before, and she snacks (the game's "Mita Lolipop" loop, repeated while you talk).
            ClearLeftoverText();
            ShowRoom("Kitchen");
            bool seated = StartMealSet();
            if (!seated)
            {
                PoseMitaUpright(SeatMita, SeatPlayerSide - SeatMita);
                MovePlayer(SeatPlayerSide, SeatMita);
            }
            if (!mitaSeated) { SetProps(true); eating = true; nextBite = 0f; }
            fadeTarget = 0f;
            afterFade = () =>
            {
                ShowEatKey(true);
                Play(Pick(L.Meals), () =>
                {
                    // give you a moment to take a bite if you haven't (and let a bite in progress finish)
                    mealWaiting = true;
                    mealWaitUntil = Time.time + (seated && !ateThisMeal ? 15f : 0f);
                });
            };
        }

        bool mealWaiting;
        float mealWaitUntil;

        void TickMeal()
        {
            if (!mealWaiting || eatingNow || (!ateThisMeal && Time.time < mealWaitUntil)) return;
            mealWaiting = false;
            ShowEatKey(false);
            eating = false;
            AddAff(ateThisMeal ? 4 : 3);
            fadeTarget = 1f;
            afterFade = () =>
            {
                standingUp = StandUpFromMeal();
                standUntil = Time.time + 8f;
                if (!standingUp) FinishMeal();
            };
        }

        bool standingUp;
        float standUntil;

        // (screen black) once you are on your feet again, back to the day
        void TickStandUp()
        {
            if (!standingUp || (pm != null && pm.animationRun && Time.time < standUntil)) return;
            standingUp = false;
            FinishMeal();
        }

        void FinishMeal()
        {
            Did("meal");
            bool seated = mealSet != null;   // with the story's table set you stood up by the chair; else back across the table
            bool mitaSat = mitaSeated;
            S.Minute = Mathf.Min(S.Minute + 40f, 1439f);
            ClearMealSet();
            SetProps(false);
            ClearLeftoverText();
            // (off her chair: next to it, at the end of the table)
            if (seated && mitaBody != null && pm != null)
            {
                ReleasePose(mitaSat ? SeatMita + new Vector3(0.6f, 0, 0.2f) : mitaBody.position, pm.transform.position);
                AimAtMita();   // (the story's stand-up leaves you looking down at the table)
            }
            else
            {
                ReleasePose(SeatMita + new Vector3(0.6f, 0, 0.2f), SeatPlayerSide);
                MovePlayer(SeatPlayerSide, SeatMita);
            }
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            fadeTarget = 0f;
            afterFade = CheckMilestones;
        }

        void DoLeave()
        {
            NextGesture(ByeGesture());
            Play(L.LeaveToMenu, () =>
            {
                fadeTarget = 1f;
                afterFade = () =>
                {
                    S.ExitKind = "menu";
                    S.Running = false;
                    S.Write();
                    PeaceMode.Active = false;
                    OverlayGuard.ReturningFromPeace = true;
                    // the pause menu's own "main menu" exit takes care of the loading overlay; fall back to a plain load
                    InterfaceFastMenu fast = null;
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<InterfaceFastMenu>()))
                    {
                        var f = o.Cast<InterfaceFastMenu>();
                        if (f != null && f.gameObject.scene == SceneManager.GetActiveScene()) { fast = f; break; }
                    }
                    try
                    {
                        if (fast != null) { Plugin.Log.LogInfo("[peace] leaving through the pause menu exit"); fast.ExitMainMenu(); return; }
                    }
                    catch (Exception e) { Plugin.Log.LogWarning("[peace] ExitMainMenu failed: " + e.Message); }
                    // an empty LoadingLevel only means "menu" on the very first load; later SceneLoading tries to load "" and hangs
                    GlobalGame.LoadingLevel = "SceneMenu";
                    SceneManager.LoadScene("SceneLoading");
                };
            });
        }

        void AddAff(int n)
        {
            S.AffCarry += n * Pn.AffMul;
            int whole = Mathf.FloorToInt(S.AffCarry);
            S.AffCarry -= whole;
            S.Affection = Mathf.Clamp(S.Affection + whole, AffFloor, 999);
        }

        void CheckMilestones()
        {
            foreach (var ms in L.Milestones)
            {
                if (S.Affection < ms.aff || S.Milestones.Contains(ms.aff)) continue;
                S.Milestones.Add(ms.aff);
                S.Write();
                Play(ms.lines);
                return;
            }
        }

        static T Pick<T>(IList<T> arr) => arr[Random.Range(0, arr.Count)];

        void UpdateFade(float dt)
        {
            if (Mathf.Approximately(fadeVal, fadeTarget)) return;
            if (Time.time < fadeHoldUntil) return;
            fadeVal = Mathf.MoveTowards(fadeVal, fadeTarget, dt * 0.8f);
            ui.SetFade(fadeVal);
            if (Mathf.Approximately(fadeVal, fadeTarget) && afterFade != null)
            {
                var a = afterFade; afterFade = null; a();
            }
        }

        // ---------------------------------------------------------------- dialogue runner

        [HideFromIl2Cpp] void Play(IList<Line> lines, Action done = null, bool light = false)
        {
            foreach (var l in lines) queue.Enqueue(l);
            var prev = queueDone;
            queueDone = () => { prev?.Invoke(); done?.Invoke(); };
            if (!busy)
            {
                busy = true;
                lightSeq = light;
                if (!light) BeginInteraction();
            }
            else if (!light && lightSeq) { lightSeq = false; BeginInteraction(); }
        }

        bool talkInPlace;

        void BeginInteraction()
        {
            if (freeMode && addon != null && !addonHeld)
            {
                // the addon walks her to a talk spot in front of you; when she is already close that walk ran into
                // the player and shoved them aside, so close by she just stops where she is
                talkInPlace = PlayerNearMita(2.5f, 360f);
                if (talkInPlace) addon.InteractiveActiveWithoutCheckPosition(true);
                else addon.InteractiveActive(true);
            }
            // turn to you, not just the head (the greeting used to come over her shoulder from the stove)
            if (!posed) { try { mitaLook?.LookOnPlayerAndRotate(); } catch { mitaLook?.LookOnPlayer(); } }
            else mitaLook?.LookOnPlayer();
        }

        void EndInteraction()
        {
            if (!freeMode || addon == null || addonHeld) return;
            if (talkInPlace) addon.InteractiveActiveWithoutCheckPosition(false);
            else addon.InteractiveActive(false);
        }

        void UpdateDialogue()
        {
            if (current != null)
            {
                bool alive;
                try { alive = current != null && current.activeSelf; } catch { alive = false; }
                if (alive && currentIsMita) PlaceMitaLine(current);
                else if (alive) PlacePlayerLine(current);
                if (alive && !currentChecked && Time.time > currentSpawnAt + (DevLineLayout ? 1.6f : 0.6f)) CheckShownText();
                if (alive && Time.time < currentDeadline) return;
                if (!alive) Plugin.Log.LogInfo($"[peace] line gone early t={Time.time:F1} (deadline {currentDeadline:F1})");
                if (alive) Destroy(current);
                current = null;
            }
            if (queue.Count > 0) { Spawn(queue.Dequeue()); return; }
            if (busy)
            {
                busy = false;
                if (!lightSeq) EndInteraction();
                // a short pause before anything else, and no idle mumbling right after a conversation
                quietUntil = Time.time + 3f;
                if (!lightSeq) idleNext = Mathf.Max(idleNext, Time.time + Random.Range(25f, 45f));
                var d = queueDone;
                queueDone = null;
                if (d != null) Plugin.Log.LogInfo($"[peace] lines done t={Time.time:F1}");
                d?.Invoke();
            }
        }

        string currentText;
        float currentSpawnAt;
        bool currentChecked;

        // the native line picks its text up by itself: make sure it is the one we registered
        void CheckShownText()
        {
            currentChecked = true;
            var d = current.GetComponent<Dialogue_3DText>();
            var shown = d != null ? d.textPrint : null;
            if (string.IsNullOrEmpty(shown) || string.IsNullOrEmpty(currentText)) return;
            var want = currentText.Length > 4 ? currentText.Substring(0, 4) : currentText;
            if (!shown.Contains(want)) Plugin.Log.LogWarning($"[peace] line shows \"{shown}\" instead of \"{currentText}\" (index {d.indexString})");
            if (DevRunner.Active) Plugin.Log.LogInfo($"[peace] line layout: {layoutFixed} letter moves so far (size {d.sizeSymbol:0.00000}, half width {d.xPrint:0.000})");
            if (DevRunner.Active && DevLineLayout) { try { LogLineLayout(d); } catch (Exception e) { Plugin.Log.LogWarning("[peace] layout log: " + e.Message); } }
            // (who the game thinks is talking, after its own Start has run)
            string theme = "?"; try { theme = d.themeDialogue.ToString(); } catch { }
            bool wrong = currentIsMita ? theme == PlayerTheme || theme == "Player" : theme != PlayerTheme;
            if (wrong || DevRunner.Active)
                Plugin.Log.LogInfo($"[peace] line check: {(currentIsMita ? "Mita" : "Player")} theme={theme} speaker={(d.speaker != null ? d.speaker.name : "-")} audioPrint={d.audioPrint} lookOnPlayer={d.lookOnPlayer} at {current.transform.position} \"{shown}\"{(wrong ? "  <-- WRONG THEME" : "")}");
        }

        // The game lays a line's letters out one by one as they are printed, and gets the spacing wrong whenever more than
        // one letter is printed in a frame (a slow frame, the first frame of a line, a recording running on frame time):
        // the letters end up on top of each other. Their places are put right here every frame, the way the game means
        // them: left to right by each glyph's advance in the line's font, at the line's own letter size, centred.
        Transform currentParent;
        bool layoutWarned;
        int layoutFixed;
        float layoutWorst;
        readonly List<int> layoutAdv = new List<int>();
        readonly List<float> layoutSeen = new List<float>();   // when each letter of the line first showed

        public static bool DevLayoutFix = true;   // dev: `layoutfix off` shows the game's own layout

        void FixLineLayout()
        {
            if (!DevLayoutFix) return;
            if (current == null || !current.activeSelf) return;
            var d = current.GetComponent<Dialogue_3DText>();
            if (d == null) return;
            var text = d.textPrint;
            var font = d.font;
            var syms = d.symbolObjects;
            if (string.IsNullOrEmpty(text) || font == null || syms == null || syms.Count == 0) return;
            if (d.align.ToString() != "Middle") return;
            float size = d.sizeSymbol;
            if (size <= 0f) return;
            font.RequestCharactersInTexture(text);
            layoutAdv.Clear();
            int total = 0, glyphs = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (!font.GetCharacterInfo(text[i], out var ci)) return;   // (a glyph the font can't give: leave the line as it is)
                layoutAdv.Add(ci.advance);
                total += ci.advance;
                if (text[i] != ' ') glyphs++;
            }
            if (syms.Count > glyphs) return;   // (not one letter per character: some other kind of line)
            float x = -total * size * 0.5f;
            int k = 0;
            for (int i = 0; i < text.Length && k < syms.Count; i++)
            {
                if (text[i] != ' ')
                {
                    var g = syms[k++];
                    if (g != null)
                    {
                        var t = g.transform;
                        var lp = t.localPosition;
                        // (only a letter that is really out of place - more than a third of its own width: the game's
                        // own little sway is left alone - and only while it is still in the line, not falling away)
                        // (and only in a letter's first moments: once put right it stays, and at the line's end the
                        // letters fall away one by one - that is theirs)
                        while (layoutSeen.Count < k) layoutSeen.Add(Time.time);
                        float off = Mathf.Abs(lp.x - x);
                        if (Time.time - layoutSeen[k - 1] < 0.7f && off > layoutWorst) layoutWorst = off;
                        if (Time.time - layoutSeen[k - 1] < 0.7f && off > layoutAdv[i] * size * 0.34f && Mathf.Abs(lp.y) < 0.15f && Mathf.Abs(lp.z) < 0.15f)
                        {
                            t.localPosition = new Vector3(x, lp.y, lp.z);
                            layoutFixed++;
                        }
                    }
                }
                x += layoutAdv[i] * size;
            }
        }

        public static bool DevLineLayout;

        // dev: how the game laid a line's letters out (their x in the line, the glyph each one's font has for it)
        void LogLineLayout(Dialogue_3DText d)
        {
            var sb = new System.Text.StringBuilder();
            var f = d.font;
            sb.Append($"[peace] layout: font={(f != null ? f.name : "-")} dynamic={(f != null && f.dynamic)} fontSize={(f != null ? f.fontSize : 0)} sizeSymbol={d.sizeSymbol:0.0000} xPrint={d.xPrint:0.000} indexChar={d.indexChar} sizeWidth={d.sizeWidth} scale={d.transform.lossyScale.x:0.00}");
            var ex = d.exampleSymbol;
            if (ex != null)
            {
                sb.Append($" example={ex.name} scale={ex.transform.localScale.x:0.0000}");
                try { var tm = ex.GetComponent<TextMesh>(); if (tm != null) sb.Append($" TextMesh(font={(tm.font != null ? tm.font.name : "-")} fontSize={tm.fontSize} charSize={tm.characterSize} style={tm.fontStyle} anchor={tm.anchor})"); } catch { sb.Append(" (no TextMesh type)"); }
                try { var mf = ex.GetComponent<MeshFilter>(); if (mf != null) sb.Append($" MeshFilter(mesh={(mf.sharedMesh != null ? mf.sharedMesh.name + " " + mf.sharedMesh.bounds.size.x.ToString("0.000") : "-")})"); } catch { }
                try { var tx = ex.GetComponent<UnityEngine.UI.Text>(); if (tx != null) sb.Append($" UIText(fontSize={tx.fontSize})"); } catch { }
            }
            Plugin.Log.LogInfo(sb.ToString());
            var syms = d.symbolObjects;
            string text = d.textPrint ?? "";
            if (syms == null) return;
            sb.Clear();
            sb.Append($"[peace] layout x ({syms.Count} symbols for {text.Length} chars):");
            for (int i = 0; i < syms.Count && i < 30; i++)
            {
                var g = syms[i];
                if (g == null) { sb.Append(" (null)"); continue; }
                float w = 0f;
                try { var r = g.GetComponent<Renderer>(); if (r != null) w = r.bounds.size.magnitude; } catch { }
                sb.Append($" {g.name}@{g.transform.localPosition.x:0.000}/s{g.transform.localScale.x:0.0000}/w{w:0.000}");
            }
            Plugin.Log.LogInfo(sb.ToString());
            if (f == null) return;
            sb.Clear();
            sb.Append("[peace] layout glyphs:");
            foreach (int size in new[] { 0, f.fontSize })
            {
                sb.Append($" size {size}:");
                for (int i = 0; i < text.Length && i < 16; i++)
                {
                    bool ok = f.GetCharacterInfo(text[i], out var ci, size);
                    sb.Append($" {text[i]}={(ok ? ci.advance.ToString() : "X")}");
                }
            }
            Plugin.Log.LogInfo(sb.ToString());
        }

        // the theme (colour) your own lines are drawn in; dev: `ptheme <Player|White|...>`
        // (the game's Player theme is the same pink as hers; White tells your lines apart at a glance)
        public static string PlayerTheme = "White";

        // Your line: always in front of whatever camera you are looking through (a staged scene has its own), low in
        // the view, so it reads as yours and never sits on her like one of hers (the game placed it from your idle eyes)
        void PlacePlayerLine(GameObject go)
        {
            var cam = holdCam != null ? holdCam.transform : pm != null && pm.mainCamera != null ? pm.mainCamera : null;
            if (go == null || cam == null) return;
            var fwd = cam.forward;
            var flat = new Vector3(fwd.x, 0f, fwd.z);
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
            flat.Normalize();
            var pos = cam.position + fwd * 1.0f + Vector3.down * 0.56f;   // (near the bottom of the view, under her when she is close)
            go.transform.position = pos;
            // (readable side towards you, the same way her lines turn: forward points away from the camera)
            var look = pos - cam.position;
            float h = new Vector2(look.x, look.z).magnitude;
            look.y = Mathf.Clamp(look.y, -0.47f * h, 0.47f * h);
            if (look.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        // Like the game's own Mita lines: a little in front of her at chest height, facing the player (readable side).
        void PlaceMitaLine(GameObject go)
        {
            var body = SpeakerBody;
            if (go == null || body == null) return;
            // (a staged scene looks through its own camera: her line faces that one, not your idle eyes elsewhere)
            var cam = holdCam != null ? holdCam.transform : pm != null && pm.mainCamera != null ? pm.mainCamera : null;
            // lying in bed: above her head on the pillow, not above her feet; at the card table: above her head, she sits
            var basePos = (posed && uprightFront == Vector3.zero) || TableMitaUp ? HeadPoint + Vector3.up * 0.4f : body.position + Vector3.up * Pn.Height;
            var toCam = cam != null ? cam.position - basePos : body.forward;
            toCam.y = 0;
            if (toCam.sqrMagnitude < 0.0001f) toCam = body.forward;
            toCam.Normalize();
            var pos = basePos + toCam * 0.35f;
            if (cam != null)
            {
                // right next to you (or beside you): keep the line far enough out to read instead of in your face
                var flat = pos - cam.position; flat.y = 0;
                if (flat.magnitude < 0.9f)
                {
                    var dir = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                    pos = new Vector3(cam.position.x, pos.y, cam.position.z) + dir * 0.9f;
                }
            }
            go.transform.position = pos;
            if (cam != null)
            {
                // upright: turn towards the camera, but never tip over more than ~25° (it used to lie down when close)
                var look = pos - cam.position;
                float h = new Vector2(look.x, look.z).magnitude;
                look.y = Mathf.Clamp(look.y, -0.47f * h, 0.47f * h);
                if (look.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }
        }

        [HideFromIl2Cpp] void Spawn(Line l)
        {
            var tmpl = l.who == Who.Mita ? tmplMita : tmplPlayer;
            var parent = l.who == Who.Mita ? dlgMitaParent : dlgPlayerParent;
            if (tmpl == null || parent == null)
            {
                Plugin.Log.LogWarning("[peace] no dialogue template for " + l.who + ": " + l.text);
                return;
            }
            var go = Instantiate(tmpl, parent);
            go.name = "PeaceLine";
            var d = go.GetComponent<Dialogue_3DText>();
            if (Loc.SpeechFont != null) d.font = Loc.SpeechFont;
            var text = Fill(Loc.T(l.text));
            d.indexString = TextHook.Register(text);
            d.nextText = null;
            d.eventFinish = new UnityEvent();
            d.eventFinishPrint = new UnityEvent();
            d.eventDontVoice = new UnityEvent();
            // no recorded voice exists for our lines: play them like the "no dub" mode (babble sounds)
            d.dontVoice = false;
            d.audioVoice = null;
            d.audioPrint = l.who != Who.Mita;   // native Mita lines leave this off; her voice comes from the speaker's sounds
            if (l.who == Who.Mita)
            {
                d.lookOnPlayer = true;
                if (d.speaker == null && mitaSpeaker != null) d.speaker = mitaSpeaker;
                // (never in your colour, whatever object the template came from)
                try { if (d.themeDialogue.ToString() == "Player") d.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita; } catch { }
            }
            else
            {
                // your line: your colour, no speaker of hers, not placed at her head
                try { if (Enum.TryParse<Dialogue_3DText.Dialogue3DTheme>(PlayerTheme, out var pth)) d.themeDialogue = pth; } catch { }
                d.speaker = null;
                d.lookOnPlayer = false;
            }
            MuteDub();
            if (l.who == Who.Mita && SpeakerPerson != null)
            {
                // in bed she talks with sleepy, half-closed eyes
                var emoName = sleepFace || inBedTalk ? "halfsleep" : l.emo;
                // contempt is our own face over her discontent one (PeaceFace); any other emotion ends it
                // the tongue-out face was on a lot of lines and doesn't suit her every time: only now and then
                if (emoName == "smiletonque")
                {
                    if (Time.time - lastTongue < 240f || Random.value > 0.3f) emoName = "smile";
                    else lastTongue = Time.time;
                }
                bool scorn = emoName == "contempt";
                if (scorn) emoName = "discontent";
                if (!string.IsNullOrEmpty(l.emo)) Contempt(scorn);
                if (!string.IsNullOrEmpty(emoName) && Enum.TryParse<EmotionType>(emoName, out var emo))
                {
                    d.emotionStart = emo;
                    d.emotionFinish = EmotionType.none;
                    try { SpeakerPerson.FaceEmotion(emoName); } catch { }
                }
                if (l.clip == "!glitch") glitchUntil = Time.realtimeSinceStartup + 1.6f;
                else if (mitaAnim != null && !TableMitaUp && heldClip == null)
                {
                    // her line's own clip, or now and then one of her gestures (see TakeGesture)
                    var cn = !string.IsNullOrEmpty(l.clip) ? l.clip : TakeGesture(l);
                    if (cn != null && clips.TryGetValue(cn, out var clip)) PlayGestureClip(clip);
                }
                PlaceMitaLine(go);
            }
            // (the line's glyphs into the font's texture before the game asks for them: it measures each letter as it prints
            // it, and a glyph the texture doesn't hold yet - most Hangul syllables, the first time - measures as nothing)
            if (DevLayoutFix)
            {
                try { var shown = Loc.Display(text); d.font?.RequestCharactersInTexture(shown); GlobalGame.fontUse?.RequestCharactersInTexture(shown); } catch { }
            }
            if (DevRunner.Active) { string th = "?"; try { th = d.themeDialogue.ToString(); } catch { } Plugin.Log.LogInfo($"[peace] line set: {l.who} theme={th} speaker={(d.speaker != null ? d.speaker.name : "-")} template={tmpl.name}"); }
            go.SetActive(true);
            current = go;
            currentParent = go.transform.parent;
            if (DevRunner.Active) Plugin.Log.LogInfo($"[peace] line layout: the line before had {layoutFixed} letter moves, worst offset {layoutWorst * 1000f:0.0} mm");
            layoutFixed = 0; layoutWorst = 0f; layoutSeen.Clear();
            currentText = text; currentSpawnAt = Time.time; currentChecked = false;
            currentIsMita = l.who == Who.Mita;
            currentDeadline = Time.time + 4f + text.Length * 0.16f;
            Plugin.Log.LogInfo($"[peace] say t={Time.time:F1} {l.who}: {text}");
        }
    }
}
