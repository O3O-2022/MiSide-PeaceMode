using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // Animations lent by the other story scenes, for Mita and for the player: Sleepy Mita's sleep (Scene 17), the
    // ending's hug (Scene 15), ... Each scene is loaded additively once per session (switched off before its story
    // starts, see SceneImport), the clips we want are kept loaded, and the scene goes away again.
    public partial class PeaceDirector
    {
        // scene, whose (null = every Mita), the clips kept from it
        static readonly (string scene, string only, string[] clips)[] AnimSources =
        {
            // Sleepy Mita: sleeping in bed, dozing, stretching, reacting to a game won or lost
            ("Scene 17 - Dreamer", null, new[]
            {
                "Mita Sleep Idle 1", "Mita Sleep Idle 2", "Mita Sleep Idle 3", "Mita Sleep WakeUp", "Mita Start Sleep",
                "Mita Stay StartSleep", "Mita Stay IdleSleep", "Mita Stay StopSleep", "Mita SitIdle HalfSleep", "Mita SitIdle Start HalfSleep",
                "Mita Warmup", "Mita Count", "Mita Look Lamp", "Mita Player Win", "Mita Player Lose",
                "Mita Start Sit", "Mita StartSit", "Mita SitIdle", "Mita Sit Idle",
            }),
            // the ending: the hug; tea
            ("Scene 15 - BasementAndDeath", null, new[]
            {
                "Mita StartHug", "Mita HugIdle", "Mita StopHug", "Player StartHug", "Player HugIdle", "Player StopHug",
                "Mita DrinkTea", "Mita ShowItemsStart", "Mita ShowItemsIdle",
                "Mita SitStart", "Mita Sit Normal", "Mita Sit NormalStart", "Mita SitHoldMe Idle", "Mita SitTakeMe", "Mita Sit Cheal",
            }),
            // the backrooms with Cappie and Kind Mita: laughing, jumping, waving goodbye, dancing, thinking, holding hands
            ("Scene 7 - Backrooms", null, new[]
            {
                "Mita Laugh", "Mita Jump", "Mita ByeBye", "Mita StartShowHands", "Mita ShowHands Show", "Mita ShowHands Idle",
                "Mita Start Dance", "Mita Dance Idle", "Mita Stop Dance", "Mita Start Thinks", "Mita Thinks", "Mita Stop Thinks",
                "MitaKind Pointer", "Mita HoldHand", "Player HoldHand", "Player Take", "MitaD StayIdle",
            }),
            // Short-haired Mita's world: her lecture gestures; hiding (the mannequins' game)
            ("Scene 10 - ManekenWorld", null, new[]
            {
                "Mita StartHide 1", "Mita StartHide 2", "Mita StartHide 3", "Mita Hide 1", "Mita Hide 2", "Mita Hide 3",
                "Mita Talk 1", "Mita Talk 2", "Mita Talk 3", "Mita Talk 4", "Mita Talk 5", "Mita Pointer",
            }),
            // Mila's flat
            ("Scene 19 - Glasses", "mila", new[] { "Mila Show Place", "Mila CheckBack", "Mila Menace", "Mila Show Idle" }),
        };

        readonly Queue<(string scene, string only, string[] clips)> animQueue = new Queue<(string, string, string[])>();
        bool animQueued;
        public static HashSet<int> DevScanBefore;      // clip ids already loaded when a dev animscan started

        // after the house is set up (and Scene 3 has been borrowed): one scene at a time
        void TickAnimImports()
        {
            if (!animQueued)
            {
                animQueued = true;
                foreach (var s in AnimSources) if (s.only == null || s.only == Pn.Id) animQueue.Enqueue(s);
            }
            if (animQueue.Count == 0 || SceneImport.Busy) return;
            var next = animQueue.Dequeue();
            if (!SceneImport.Load(next.scene, s => KeepClips(s, next.clips))) animQueue.Enqueue(next);
        }

        [HideFromIl2Cpp] void KeepClips(Scene s, string[] wanted)
        {
            int kept = 0;
            var had = new HashSet<string>(clips.Keys);
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimationClip>()))
            {
                if (o == null || Array.IndexOf(wanted, o.name) < 0 || had.Contains(o.name)) continue;
                var c = o.Cast<AnimationClip>();
                // two of a name in one scene (two Mitas there): the longer one, the whole gesture
                if (clips.TryGetValue(o.name, out var other) && other.length >= c.length) continue;
                c.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                if (!clips.ContainsKey(o.name)) kept++;
                clips[o.name] = c;
            }
            if (s.name == "Scene 15 - BasementAndDeath") { RememberHugPlace(s); KeepHugPlayers(s); }
            if (s.name == "Scene 17 - Dreamer") RememberSleepPlace(s);
            if (s.name == "Scene 7 - Backrooms")
            {
                KeepBackroomsPlayers(s); KeepCappieIdle();
                try { KeepDanceGame(s); } catch (Exception e) { Plugin.Log.LogError("[dance] " + e); }
                try { KeepHammerGame(s); } catch (Exception e) { Plugin.Log.LogError("[hammer] " + e); }
            }
            Plugin.Log.LogInfo($"[anim] kept {kept} of {wanted.Length} clips from {s.name}");
            if (DevRunner.Active) File.WriteAllText(Path.Combine(BepInEx.Paths.ConfigPath, "anim_" + s.name.Split(' ')[1] + ".txt"), AnimScan(s), Encoding.UTF8);
        }

        // ---- Cappie stands the way she does when you meet her (Scene 7, "spend time with Cappie"): her override
        // controller "Mita Cap Over" gives her own idle; the character gallery's model bounces about far too much ----
        static AnimationClip cappieIdle;

        void KeepCappieIdle()
        {
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimatorOverrideController>()))
            {
                if (o == null || o.name != "Mita Cap Over") continue;
                var oc = o.Cast<AnimatorOverrideController>();
                var baseCtrl = oc.runtimeAnimatorController;
                if (baseCtrl == null) continue;
                foreach (var c in baseCtrl.animationClips)
                {
                    if (c == null || c.name != IdleSlot) continue;
                    var idle = oc[c] ?? c;
                    idle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    cappieIdle = idle;
                    Plugin.Log.LogInfo($"[anim] Cappie's story idle: {idle.name} {idle.length:F2}s ({(idle == c ? "the base one" : "her own")})");
                    break;
                }
                if (cappieIdle != null) break;
            }
            UseCappieIdle();
        }

        void UseCappieIdle()
        {
            if (cappieIdle == null || Pn.Id != "cap" || skin == null || skin.OwnAnimator) return;
            skin.IdleClip = cappieIdle;
            if (!posed && heldClip == null && !hugging) { SetIdle(cappieIdle); ReIdle(); }
        }

        // ---- the hug's layout in Scene 15: where the player's animation stands relative to Mita ----
        Vector3 hugPlayerOffset = new Vector3(0f, 0f, 0.55f);   // in Mita's frame
        float hugPlayerYaw = 180f;

        void RememberHugPlace(Scene s)
        {
            Transform mitaAt = null, playerAt = null;
            foreach (var r in s.GetRootGameObjects())
            {
                mitaAt ??= Util.FindDeep(r.transform, "Position MitaForHug");
                playerAt ??= Util.FindDeep(r.transform, "AnimationPlayer Hug");
            }
            if (mitaAt == null || playerAt == null) { Plugin.Log.LogWarning($"[anim] hug layout: mita {mitaAt != null} player {playerAt != null}"); return; }
            hugPlayerOffset = Quaternion.Inverse(mitaAt.rotation) * (playerAt.position - mitaAt.position);
            hugPlayerYaw = Mathf.DeltaAngle(mitaAt.eulerAngles.y, playerAt.eulerAngles.y);
            Plugin.Log.LogInfo($"[anim] hug layout: player at {hugPlayerOffset} yaw {hugPlayerYaw:0} from Mita");
        }

        // ---- Sleepy Mita's bed (Scene 17 is the same house): where her body lies, relative to the house ----
        Vector3 sleepLocalPos;
        Quaternion sleepLocalRot;
        bool sleepPlaceKnown;

        // the bedroom of the house prefab (both scenes have it; the houses themselves sit in different places)
        static Transform HouseAnchor(Scene s)
        {
            foreach (var r in s.GetRootGameObjects())
            {
                var h = Util.FindDeep(r.transform, "HouseGame Tamagotchi");
                var t = h != null ? Util.FindDeep(h, "Bedroom") : null;
                if (t != null) return t;
            }
            return null;
        }

        void RememberSleepPlace(Scene s)
        {
            Transform dream = null;
            foreach (var r in s.GetRootGameObjects()) dream ??= Util.FindDeep(r.transform, "Mita Dream");
            var house = HouseAnchor(s);
            if (dream == null || house == null) { Plugin.Log.LogWarning($"[anim] sleep place: mita {dream != null} house {house != null}"); return; }
            sleepLocalPos = house.InverseTransformPoint(dream.position);
            sleepLocalRot = Quaternion.Inverse(house.rotation) * dream.rotation;
            sleepPlaceKnown = true;
            var clocks = Util.FindDeep(house, "BedroomSleepClocks");
            var ours = HouseAnchor(SceneManager.GetActiveScene());
            var ourClocks = ours != null ? Util.FindDeep(ours, "BedroomSleepClocks") : null;
            Plugin.Log.LogInfo($"[anim] sleep place: {sleepLocalPos} {sleepLocalRot.eulerAngles} in the house; clocks there {(clocks != null ? house.InverseTransformPoint(clocks.position).ToString() : "-")}, here {(ourClocks != null ? ours.InverseTransformPoint(ourClocks.position).ToString() : "-")}");
        }

        // her in bed: Sleepy Mita's own sleep (Scene 17's clip and place); without it, the old way (her idle laid down).
        // Her sleeping positions all lie turned to the room side of the bed: alone she is on that side (you see her face
        // from the bedside); with you she moves over to the wall side, facing the half where you lie.
        static readonly Vector3 WallSide = new Vector3(0f, 0f, -0.45f);   // across the bed (world: the bed's short side is Z)

        void LieInBed(bool withYou = false)
        {
            var sleep = Clip("Mita Sleep Idle 2");   // (Scene 17's second position: on her side, turned to the room)
            var house = HouseAnchor(SceneManager.GetActiveScene());
            if (!sleepPlaceKnown || sleep == null || house == null || mitaBody == null || mitaAnim == null || (skin != null && skin.OwnAnimator))
            {
                SetIdle(null);
                PoseMita(new Vector3(BedCenter.x, BedTop + 0.12f, BedCenter.z), Vector3.up, BedHeadDir);
                return;
            }
            HoldAddon(true);
            try { mita?.AiShraplyStop(); } catch { }
            if (mita.nma != null) { try { mita.nma.isStopped = true; } catch { } mita.nma.enabled = false; }
            posePos = house.TransformPoint(sleepLocalPos) + (withYou ? WallSide : Vector3.zero);
            poseRot = house.rotation * sleepLocalRot;
            posed = true; lying = false; uprightFront = Vector3.zero;
            mitaBody.SetPositionAndRotation(posePos, poseRot);
            HoldClip(sleep);
            // (a frame later: HoldAddon's own next clip in this frame left hers waiting ~2 s - standing in the bed
            // view, the camera inside her, then dropped into bed in four frames)
            After(0.001f, () => { if (heldClip == sleep) PlayClipSure(sleep, () => heldClip == sleep, () => { if (fadeHoldUntil > Time.time + 0.15f) fadeHoldUntil = Time.time + 0.15f; }); });
            Plugin.Log.LogInfo($"[anim] asleep like Sleepy Mita at {posePos} yaw {poseRot.eulerAngles.y:0}");
        }

        // out of bed again: her own idle back before she stands up
        void UpFromBed() { SleepFace(false); ReleaseClip(); }

        // asleep: eyes closed and kept closed (her look, its blinking and a line's face would open them again)
        bool sleepFace;
        float nextSleepFace;

        void SleepFace(bool on)
        {
            sleepFace = on;
            try
            {
                if (on) { mita?.FaceEmotion("sleep"); mitaLook?.ActivationBlink(false); mitaLook?.Activation(false); }
                else mitaLook?.ActivationBlink(true);
            }
            catch { }
        }

        public void DevBedView(bool on) => BedCamera(on);

        void KeepSleepFace()
        {
            if (!sleepFace || busy || queue.Count > 0 || Time.time < nextSleepFace) return;   // (awake while she talks)
            nextSleepFace = Time.time + 1.5f;
            try { mita?.FaceEmotion("sleep"); } catch { }
        }

        // you in bed too, on the room side, facing her: the view from your pillow (see BedCamera); your body waits at
        // the bedside behind it (curled up with her sleeping clip, your own arms would be in front of your eyes)
        bool playerInBed;

        bool PlayerLieDown()
        {
            if (!posed || heldClip == null || mitaBody == null) return false;
            playerInBed = true;
            BedCamera(true);
            Plugin.Log.LogInfo("[anim] you lie down facing her");
            return true;
        }

        // true = you were in bed (the caller puts you back at the bedside)
        bool PlayerGetUp()
        {
            if (!playerInBed) return false;
            playerInBed = false;
            BedCamera(false);
            return true;
        }

        // Lying curled up, your own arms are in front of your eyes: the view comes from your pillow instead, a little
        // in front of your face, looking at hers (a camera drawn over the game's while you are in bed)
        Camera bedCam;
        public static float BedCamAcross = 0.47f, BedCamUp = 0.36f;   // (dev: bedcam <across> <up>)
        int bedCamLogged;

        void BedCamera(bool on)
        {
            if (!on) { if (bedCam != null) UnityEngine.Object.Destroy(bedCam.gameObject); bedCam = null; return; }
            if (bedCam != null) return;
            var go = new GameObject("PeaceBedCamera");
            bedCam = go.AddComponent<Camera>();
            bedCam.depth = 40;
            bedCam.nearClipPlane = 0.03f;
            bedCam.fieldOfView = 55f;
            var main = Camera.main;
            if (main != null) { bedCam.cullingMask = main.cullingMask; bedCam.clearFlags = main.clearFlags; bedCam.backgroundColor = main.backgroundColor; }
            bedCam.cullingMask |= LineLayers();
            TickBedCamera();
        }

        void TickBedCamera()
        {
            if (bedCam == null || mitaBody == null) return;
            // from her eye bones: in front of her face whatever way the clip turns her head, your head tilted like hers
            if (!FaceFrame(out var eyes, out var fwd, out var up)) return;
            // (her hands are curled up in front of her face: seen from a little above them, as from a pillow next to hers)
            var across = fwd; across.y = 0;
            across = across.sqrMagnitude > 0.01f ? across.normalized : -WallSide.normalized;
            var at = eyes + across * BedCamAcross + Vector3.up * BedCamUp;
            if (bedCamLogged++ == 30) Plugin.Log.LogInfo($"[anim] bed view: her face {fwd} (eyes {eyes}), you at {at}");
            bedCam.transform.position = at;
            bedCam.transform.rotation = Quaternion.LookRotation(eyes - at, Vector3.up);
        }

        // ---- holding hands: the backrooms' player animation walks your head on and looks ahead (you walked hand in
        // hand there); here you look at her instead - a view camera on your animated head, turned to her face ----
        Camera holdCam, holdSource;
        Func<Vector3> viewTarget, viewFrom;   // (viewFrom: somewhere else than your eyes - lying on her lap)

        void HoldHandsView(bool on) => StageView(on ? () => HeadPoint + Vector3.down * 0.18f : (Func<Vector3>)null);

        // a view camera on your animated head (the game's own camera), turned to what the scene is about
        [HideFromIl2Cpp] void StageViewAt(Func<Vector3> from, Func<Vector3> target)
        {
            StageView(target);
            viewFrom = from;
            if (holdCam != null && from != null) holdCam.transform.SetPositionAndRotation(from(), Quaternion.LookRotation(target() - from(), Vector3.up));
        }

        [HideFromIl2Cpp] void StageView(Func<Vector3> target)
        {
            viewTarget = target;
            viewFrom = null;
            if (target == null) { if (holdCam != null) UnityEngine.Object.Destroy(holdCam.gameObject); holdCam = null; holdSource = null; return; }
            if (holdCam != null) return;
            // (the camera the game renders your eyes with: the animation moves it, not the player's own transforms)
            var main = Camera.main;
            if (main == null)
                foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>())
                    if (c != null && c.isActiveAndEnabled && c.targetTexture == null && (main == null || c.depth > main.depth)) main = c;
            if (main == null) return;
            holdSource = main;
            var go = new GameObject("PeaceHoldHandsCamera");
            holdCam = go.AddComponent<Camera>();
            holdCam.depth = 40;
            holdCam.nearClipPlane = 0.03f;
            holdCam.fieldOfView = main != null ? main.fieldOfView : 60f;
            if (main != null) { holdCam.cullingMask = main.cullingMask; holdCam.clearFlags = main.clearFlags; holdCam.backgroundColor = main.backgroundColor; }
            holdCam.cullingMask |= LineLayers();   // (her lines: another camera of the game's draws them, under this one)
            holdCam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            Plugin.Log.LogInfo($"[anim] stage view from {main.name} at {main.transform.position}");
        }

        // the layers her (and your) 3D lines are on: a camera of our own has to draw them too
        int LineLayers()
        {
            int m = 0;
            foreach (var t in new[] { tmplMita, tmplPlayer })
                if (t != null) foreach (var c in t.GetComponentsInChildren<Transform>(true)) m |= 1 << c.gameObject.layer;
            return m;
        }

        // (LateUpdate: after the animation has moved your head)
        void TickHoldHandsView()
        {
            if (holdCam == null || holdSource == null || mitaBody == null || viewTarget == null) return;
            var from = viewFrom != null ? viewFrom() : holdSource.transform.position;
            var want = Quaternion.LookRotation(viewTarget() - from, Vector3.up);
            holdCam.transform.position = from;
            holdCam.transform.rotation = Quaternion.Slerp(holdCam.transform.rotation, want, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        // Staged scenes put her in place in one frame; her hair and skirt (MagicaCloth) then fly up as if she'd been
        // flung there. Start their simulation over from where she now stands, again a moment later as the clip settles.
        void SettleCloth()
        {
            ResetCloth();
            After(0.05f, ResetCloth);
            After(0.2f, ResetCloth);
        }

        void ResetCloth()
        {
            if (mitaBody == null) return;
            var top = mitaBody.parent != null ? mitaBody.parent : mitaBody;
            foreach (var c in top.GetComponentsInChildren<MagicaCloth.BaseCloth>(true))
                try { if (c != null && c.isActiveAndEnabled) c.ResetCloth(); } catch (Exception e) { Plugin.Log.LogWarning("[anim] cloth reset: " + e.Message); }
        }

        // ---- a tiny scheduler for staged scenes ----
        readonly List<(float at, Action a)> timers = new List<(float, Action)>();
        [HideFromIl2Cpp] void After(float seconds, Action a) => timers.Add((Time.time + seconds, a));

        // ---- whole clips through her idle slot ----
        // The game's "next clip" (AnimationClipSimpleNext) leaves its state at the exit time set for the clip that state
        // was made for: a longer clip is cut off part way (StartHug at 45%, HoldHand at a quarter - she sprang back to
        // standing). Her idle state plays whatever clip is in its slot, whole and looping, so a clip of ours goes in the
        // slot and that state is started again from its first frame.
        int idleStateHash;
        string idleClipName;

        void RememberIdleState() { }   // (learned in PlayInSlot instead: see there)

        void PlayInSlot(AnimationClip c)
        {
            if (c == null) return;
            try { mitaAnim.ResetOrder(); } catch { }
            SetIdle(c);
            // The state that plays the slot is found by watching for the clip itself (the state where "Mita Idle"
            // happens to play may be the next-clip one); once known, it is jumped to straight away.
            float t0 = Time.time;
            void Tick()
            {
                try
                {
                    var an = mitaBody != null ? mitaBody.GetComponent<Animator>() : null;
                    if (an == null) return;
                    bool Has(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<AnimatorClipInfo> infos)
                    {
                        foreach (var ci in infos) if (ci.clip != null && ci.clip.name == c.name) return true;
                        return false;
                    }
                    int hash = 0;
                    if (Has(an.GetCurrentAnimatorClipInfo(0))) hash = an.GetCurrentAnimatorStateInfo(0).fullPathHash;
                    else if (an.IsInTransition(0) && Has(an.GetNextAnimatorClipInfo(0))) hash = an.GetNextAnimatorStateInfo(0).fullPathHash;
                    else if (idleStateHash != 0 && Time.time - t0 > 0.02f) hash = idleStateHash;
                    if (hash != 0)
                    {
                        an.Play(hash, 0, 0f);
                        if (hash != idleStateHash && Has(an.GetCurrentAnimatorClipInfo(0)) || idleStateHash == 0) idleStateHash = hash;
                        return;
                    }
                    if (Time.time - t0 < 0.6f) { After(0.001f, Tick); return; }
                    Plugin.Log.LogWarning("[anim] slot: " + c.name + " never came in");
                }
                catch (Exception e) { Plugin.Log.LogWarning("[anim] slot restart: " + e.Message); }
            }
            After(0.001f, Tick);
        }

        // her clip, made sure of: asked for as the next clip (it blends in), then watched. It counts as on once she has
        // been in it or blending to it for 0.3 s straight (a blend into it was sometimes cut off again a few frames later by
        // the idle HoldAddon asks for - and StartHug blends in long, so "not blending any more" never came). Not in it
        // within 0.4 s, or dropped out of it: asked again; the second time forced (AnimationClipSimple cuts straight to it).
        [HideFromIl2Cpp] void PlayClipSure(AnimationClip clip, Func<bool> still, Action started = null)
        {
            if (clip == null || mitaAnim == null) return;
            int tries = 0;
            float t0 = Time.time, onSince = -1f;
            void Ask() { try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(clip); } catch { } }
            bool On()
            {
                var an = mitaBody != null ? mitaBody.GetComponent<Animator>() : null;
                if (an == null) return true;
                try
                {
                    foreach (var ci in an.GetCurrentAnimatorClipInfo(0)) if (ci.clip != null && ci.clip.name == clip.name && ci.weight > 0.3f) return true;
                    if (an.IsInTransition(0)) foreach (var ci in an.GetNextAnimatorClipInfo(0)) if (ci.clip != null && ci.clip.name == clip.name) return true;
                }
                catch (Exception e) { Plugin.Log.LogWarning($"[anim] {clip.name} check: {e.Message}"); return true; }
                return false;
            }
            void Again(string why)
            {
                t0 = Time.time; onSince = -1f;
                if (++tries == 1) { Plugin.Log.LogInfo($"[anim] {clip.name} {why}: asked again"); Ask(); }
                else { Plugin.Log.LogInfo($"[anim] {clip.name} {why} again: forced"); try { mitaAnim.AnimationClipSimple(clip); } catch { } started?.Invoke(); tries = 99; }
            }
            void Tick()
            {
                if (tries == 99) return;
                if (still != null && !still()) return;
                if (On())
                {
                    if (onSince < 0f) onSince = Time.time;
                    if (Time.time - onSince >= 0.3f) { if (tries > 0) Plugin.Log.LogInfo($"[anim] {clip.name} on (try {tries})"); started?.Invoke(); return; }
                }
                else if (onSince >= 0f) Again("dropped out");
                else if (Time.time - t0 > 0.4f) Again("didn't come in");
                if (tries != 99) After(0.001f, Tick);
            }
            // first let the idle HoldAddon has just asked for settle in (asked while that blend runs, ours was cut off by
            // it a few frames later): up to 0.3 s, until she is in one state, not blending
            float w0 = Time.time;
            void Wait()
            {
                if (still != null && !still()) return;
                bool blending = false;
                try { var an = mitaBody != null ? mitaBody.GetComponent<Animator>() : null; blending = an != null && an.IsInTransition(0); } catch { }
                if (blending && Time.time - w0 < 0.3f) { After(0.001f, Wait); return; }
                t0 = Time.time;
                Ask();
                After(0.001f, Tick);
            }
            After(0.001f, Wait);
        }

        // `then` the frame her clip `clip` is about to end (or has ended), checked every frame; `fallback` s at most
        [HideFromIl2Cpp] void AtClipEnd(string clip, float fallback, Action then)
        {
            float t0 = Time.time;
            bool seen = false;
            void Tick()
            {
                var an = mitaBody != null ? mitaBody.GetComponent<Animator>() : null;
                bool fire = Time.time - t0 > fallback;
                try
                {
                    if (an != null && !fire)
                    {
                        // which clip is the state she is in, which one she is blending to (the heaviest of each)
                        string Top(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<AnimatorClipInfo> infos)
                        {
                            string n = null; float w = -1f;
                            foreach (var ci in infos) if (ci.clip != null && ci.weight > w) { w = ci.weight; n = ci.clip.name; }
                            return n;
                        }
                        bool inTr = an.IsInTransition(0);
                        var cur = Top(an.GetCurrentAnimatorClipInfo(0));
                        var nxt = inTr ? Top(an.GetNextAnimatorClipInfo(0)) : null;
                        if (cur == clip)
                        {
                            seen = true;
                            var st = an.GetCurrentAnimatorStateInfo(0);
                            // leaving it (blending to something else), or its last 1.5 frames
                            if (inTr && nxt != clip) fire = true;
                            else if (!inTr && st.normalizedTime >= 1f - 0.05f / Mathf.Max(0.1f, st.length)) fire = true;
                        }
                        else if (nxt == clip) seen = true;   // (blending into it)
                        else if (seen) fire = true;          // gone already
                    }
                }
                catch { }
                if (fire) { then(); return; }
                After(0.001f, Tick);
            }
            After(0.001f, Tick);
        }

        void TickTimers()
        {
            TickBedCamera();
            KeepSleepFace();
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                if (Time.time < timers[i].at) continue;
                var a = timers[i].a;
                timers.RemoveAt(i);
                try { a(); } catch (Exception e) { Plugin.Log.LogError("[anim] timer: " + e); }
            }
        }

        // ---- the ending's hug (Scene 15): she steps into your arms, you hold her while she talks, and let go ----
        ObjectAnimationPlayer hugPlayer, hugRelease;
        bool hugging;

        void KeepHugPlayers(Scene s)
        {
            var here = SceneManager.GetActiveScene();
            foreach (var r in s.GetRootGameObjects())
            {
                var h = Util.FindDeep(r.transform, "AnimationPlayer Hug");
                var i = h != null && h.parent != null ? h.parent.Find("AnimationPlayer Idle") : null;
                if (h != null && hugPlayer == null && h.GetComponent<ObjectAnimationPlayer>() != null)
                {
                    SceneImport.StripOutside(h.gameObject, s);
                    hugPlayer = SceneImport.Take(h, here).GetComponent<ObjectAnimationPlayer>();
                }
                if (i != null && hugRelease == null && i.GetComponent<ObjectAnimationPlayer>() != null)
                {
                    SceneImport.StripOutside(i.gameObject, s);
                    hugRelease = SceneImport.Take(i, here).GetComponent<ObjectAnimationPlayer>();
                    hugRelease.animationLoop = null;   // (the ending keeps you standing there: here you get your controls back)
                }
            }
            Plugin.Log.LogInfo($"[anim] hug players kept: {hugPlayer != null} {hugRelease != null}");
        }

        // ---- a shared animation hands your view back without a jump ----
        // Your eyes sit a little off your feet; the animation moves your eyes, and when it stops they go back to your
        // body, wherever that stands. So: your eyes' offset from your feet is noted while you stand normally, and when
        // the animation lets go, your body is put where those eyes are, looking the way they looked.
        Vector3 headOffsetLocal = new Vector3(0.07f, 0f, 0.07f);

        void RememberHeadOffset()
        {
            try
            {
                if (pm == null || pm.animationRun || pm.mainCamera == null) return;
                var d = pm.mainCamera.position - pm.transform.position; d.y = 0;
                if (d.magnitude < 0.5f) headOffsetLocal = Quaternion.Inverse(pm.transform.rotation) * d;
            }
            catch { }
        }

        [HideFromIl2Cpp] void KeepViewWhereItIs(Action stop)
        {
            var cam = pm.mainCamera;
            if (cam == null) { stop(); return; }
            var eye = cam.position;
            var fwd = cam.forward;
            stop();
            var flat = new Vector3(fwd.x, 0f, fwd.z);
            float yaw = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat).eulerAngles.y : pm.transform.eulerAngles.y;
            var feet = new Vector3(eye.x, pm.transform.position.y, eye.z) - Quaternion.Euler(0f, yaw, 0f) * headOffsetLocal;
            feet = OnFloor(feet);
            try { pm.TeleportPlayer(feet, yaw); } catch (Exception e) { Plugin.Log.LogWarning("[anim] view keep: " + e.Message); }
            pm.rotX = yaw;
            pm.rotY = -Mathf.Asin(Mathf.Clamp(fwd.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        // `offset` (in her frame) from `at`, turned as little as possible from `facing` so that it lands on open floor;
        // `turned` = her rotation for it
        static Vector3 ClearFacing(Vector3 at, Quaternion facing, Vector3 offset, out Quaternion turned)
        {
            foreach (var a in new[] { 0f, 25f, -25f, 50f, -50f, 75f, -75f, 105f, -105f, 135f, -135f, 180f })
            {
                var r = Quaternion.Euler(0f, a, 0f) * facing;
                var want = at + r * offset;
                if (UnityEngine.AI.NavMesh.SamplePosition(want, out var hit, 0.12f, -1) && Mathf.Abs(hit.position.y - want.y) < 0.25f
                    && !UnityEngine.AI.NavMesh.Raycast(at, hit.position, out _, -1))
                {
                    turned = r;
                    return hit.position;
                }
            }
            turned = facing;
            return OnFloor(at + facing * offset);
        }

        // where you stand for the hug, on top of where the ending puts you (her local x right / z towards you)
        public static Vector3 HugShift = Vector3.zero;

        bool CanHugLikeTheEnding => hugPlayer != null && hugRelease != null && pm != null && mitaBody != null && mitaAnim != null && !posed
            && (skin == null || !skin.OwnAnimator) && Clip("Mita StartHug") != null && Clip("Mita HugIdle") != null && Clip("Mita StopHug") != null;

        // plays `lines` while you hold each other; false = not possible here (the caller just plays the lines)
        [HideFromIl2Cpp] bool StageHug(IList<Line> lines, Action done)
        {
            if (!CanHugLikeTheEnding || hugging) return false;
            if (activity != MitaActivity.Free) EndRoutine();
            hugging = true;
            FoldCollar(true);
            var mp = mitaBody.position;
            var to = pm.transform.position - mp; to.y = 0;
            if (to.sqrMagnitude < 0.01f) to = Vector3.forward;
            // she stays where she is, turned to you (standing, her face is on the root's +Z: see DoWindow)
            PoseMitaUpright(mp, to);
            uprightFront = Vector3.zero;
            // you: where the ending puts you, in front of her - on open floor: facing a wall or the counter that spot
            // was snapped to the nearest walkable point, half a metre too close, and the view went through her body
            RememberHeadOffset();
            var spot = ClearFacing(mp, Quaternion.LookRotation(to.normalized), hugPlayerOffset + HugShift, out poseRot);
            mitaBody.rotation = poseRot;
            try { mitaLook?.Activation(false); } catch { }
            var rot = Quaternion.Euler(0f, poseRot.eulerAngles.y + hugPlayerYaw, 0f);
            hugPlayer.transform.SetPositionAndRotation(spot, rot);
            hugRelease.transform.SetPositionAndRotation(spot, rot);
            try { hugPlayer.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[anim] hug (player): " + e.Message); }
            // StartHug plays as her "next" clip; the hold (HugIdle, through her idle slot) comes in the very frame StartHug
            // reaches its end - watched, not timed: on a timer StartHug ran out first and she stood apart for half a
            // second before springing back into your arms. (A slot swap restarts the idle state and cancels a "next"
            // clip, so it never happens while StartHug plays; StopHug goes through the slot for the same reason.)
            // StartHug a frame later: PoseMitaUpright (via HoldAddon) has just asked for her idle as the next clip, and two
            // "next" clips in one frame cut the second one off part way (StartHug at 45% - she sprang back to standing).
            // The hold then follows as the next clip too, and sits in her idle slot for whenever that state comes back.
            var start = Clip("Mita StartHug");
            EndMotion();
            SettleCloth();
            After(0.001f, () =>
            {
                if (!hugging) return;
                // (watched: StartHug sometimes came in late - behind her idle - and a timer from asking for it cut it
                // off in the middle, flashing her into your face and back, then StartHug played again; its end is
                // watched only once it is surely on)
                PlayClipSure(start, () => hugging, () => AtClipEnd(start.name, start.length + 1.5f, () =>
                {
                    if (!hugging) return;
                    var hold = Clip("Mita HugIdle");
                    HoldClip(hold);
                    try { mitaAnim.AnimationClipSimpleNext(hold); } catch { }
                    Play(lines, () => EndHug(done));
                }));
            });
            Plugin.Log.LogInfo($"[anim] hug at {spot}");
            return true;
        }

        [HideFromIl2Cpp] void EndHug(Action done)
        {
            if (!hugging) { done?.Invoke(); return; }
            var stop = Clip("Mita StopHug");
            heldClip = null;
            FoldCollar(false);
            // letting go as the next clip (the slot keeps the hold for now: swapped first, the swap cut StopHug off in
            // the frame it began); her own idle goes back in the slot under StopHug's last frame, which ends in that pose
            try { mitaAnim.AnimationClipSimpleNext(stop); } catch { }
            try { hugRelease.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[anim] hug release: " + e.Message); }
            After(stop.length - 0.05f, () => { if (hugging) SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null); });
            After(stop.length + 0.1f, () => FinishHug(done));
        }

        [HideFromIl2Cpp] void FinishHug(Action done)
        {
            {
                hugging = false;
                // your view, which the release animation carried, stays where it is: you are put there before the
                // animation lets go of it (stopped first, the view jumped back to where you had stood - 70 cm in a frame)
                try { if (pm != null && pm.animationRun) KeepViewWhereItIs(() => pm.AnimationStopFull()); } catch { }
                if (posed && mitaBody != null && pm != null) ReleasePose(mitaBody.position, pm.transform.position);
                // (no FaceHer: the release animation ends looking at her, and turning you again was a jump of its own)
                try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
                done?.Invoke();
            }
        }

        // ---- her body language: clips from the Mitas' own scenes on her lines ----
        // lines without a clip of their own now and then get one of hers (a talk gesture)
        static readonly Dictionary<string, string[]> TalkGestures = new Dictionary<string, string[]>
        {
            // (only clips that stay on her spot: Laugh, Jump, ShowHands, Yawns, Look Lamp, IdleTalk, Start Dance and
            // Mila Show Place carry where their Mita stood in her own scene, or lean her into the camera - gestureprobe)
            // (2026-10: Talk 1-3 hold an arm out at nothing or hunch over pointing up: our own motions instead, see PeaceMotion;
            // 2026-11: Talk 5 too - its arm stayed out at nothing for seconds, still out when her lines had ended)
            ["crazy"] = new[] { "Mita Start Thinks", "@explain", "@tilt", "@nod" },
            ["kind"] = new[] { "MitaKind Pointer", "Mita Start Thinks", "@explain", "@nod" },   // (Scene 6's console pointing leaves her in a T-pose)
            ["cap"] = new[] { "Mita Talk 4", "Mita Pointer", "@cheer", "@explain" },
            ["mila"] = new[] { "Mila CheckBack", "Mita Talk 4", "@tilt" },
            ["sleepy"] = new[] { "Mita Start Thinks", "@nod", "@tilt" },
            ["short"] = new[] { "Mita Talk 4", "Mita Pointer", "@explain", "@nod" },
        };

        string forcedGesture;
        float nextTalkGesture, gestureUntil;
        public static bool DevAllGestures;   // (dev: a gesture on every line, for recording them)
        int gestureToken;

        // the next line of hers (one without a clip of its own) plays this
        void NextGesture(string clip) { if (clip != null && (clip[0] == '@' ? Motions.All.ContainsKey(clip) : Clip(clip) != null)) forcedGesture = clip; }

        // what the line says: a question tilts her head, yes nods, no shakes it, shy looks away
        [HideFromIl2Cpp] static string GestureForLine(Line l)
        {
            var t = (l.text ?? "").Trim();
            if (l.emo == "shy") return Random.value < 0.5f ? "@shy" : "@lookaway";
            if (t.StartsWith("아니") || t.StartsWith("싫어") || t.StartsWith("안 돼")) return "@shake";
            if (t.StartsWith("네") || t.StartsWith("응") || t.StartsWith("좋아요") || t.StartsWith("맞아요") || t.StartsWith("그래요") || t.StartsWith("물론")) return "@nod";
            if (t.EndsWith("?")) return "@tilt";
            return null;
        }

        [HideFromIl2Cpp] string TakeGesture(Line l)
        {
            if (forcedGesture != null) { var f = forcedGesture; forcedGesture = null; return f; }
            if ((posed && !showcasing) || Time.time < nextTalkGesture || !TalkGestures.TryGetValue(Pn.Id, out var set)) return null;
            // (Mila Menace is gone: its first frame stands her elsewhere - a one-frame lurch sideways as it began)
            if (!DevAllGestures && Random.value > 0.4f) return null;
            nextTalkGesture = Time.time + (DevAllGestures ? 2.5f : Random.Range(5f, 9f));
            var fit = GestureForLine(l);
            if (fit == null && Mood > 85f && Random.value < 0.3f) fit = "@cheer";   // (in high spirits)
            if (fit != null && Random.value < 0.7f) return fit;
            var pick = set[Random.Range(0, set.Length)];
            return pick[0] == '@' || Clip(pick) != null ? pick : null;
        }

        // a gesture on her lines; one that loops is ended after one round (back to her idle)
        void PlayGestureClip(AnimationClip c)
        {
            EndMotion();   // (one of ours would fight it)
            try { mitaAnim.AnimationClipSimpleNext(c); } catch { return; }
            Plugin.Log.LogInfo($"[anim] gesture {c.name}");
            gestureUntil = Time.time + Mathf.Min(c.length, 8f);
            StartPlant();
            int token = ++gestureToken;
            if (c.isLooping) After(Mathf.Min(c.length, 8f), () => { if (token == gestureToken) ReIdle(); });
        }

        // Many of the borrowed clips carry where their Mita stood in her own scene: played here they slide her half a
        // metre or more off her spot (and snap back at the end). For as long as a gesture plays her hips are kept where
        // they were when it started (sideways only - bows and jumps stay), so only the body language comes through.
        Transform plantHips;
        Vector3 plantWorld, plantRoot;
        bool planting;

        void StartPlant()
        {
            if (mitaBody == null) return;
            if (plantHips == null || !plantHips.IsChildOf(mitaBody)) plantHips = Util.FindDeep(mitaBody, "Hips");
            if (plantHips == null) return;
            // (a gesture right after another keeps the first one's spot)
            if (!planting) { plantWorld = plantHips.position; plantRoot = mitaBody.position; }
            planting = true;
        }

        // LateUpdate, after the animator and KeepPose (which puts a posed Mita's root back every frame). A free one is
        // put back where she stood first, or the agent would take our correction as a move of hers and it would add up.
        // Kept on until her hips are back on the spot by themselves (the blend back to her idle takes a moment); a clip
        // that leaves her somewhere else has that made her real position instead of snapping her there.
        void KeepPlant()
        {
            if (!planting) return;
            bool walkingOff = !posed && mita != null && mita.nma != null && mita.nma.enabled && mita.nma.velocity.sqrMagnitude > 0.01f;
            if (plantHips == null || mitaBody == null || hugging || lying || walkingOff) { planting = false; return; }
            if (!posed) mitaBody.position = new Vector3(plantRoot.x, mitaBody.position.y, plantRoot.z);
            var slide = plantHips.position - plantWorld;
            slide.y = 0;
            bool over = Time.time > gestureUntil;
            if (over && slide.sqrMagnitude < 0.0004f) { planting = false; return; }
            mitaBody.position -= slide;
            if (over && Time.time > gestureUntil + 3f)
            {
                if (posed) posePos -= slide;
                else if (mita.nma != null && mita.nma.enabled) try { mita.nma.Warp(mitaBody.position); } catch { }
                planting = false;
            }
        }

        void ReIdle()
        {
            if (heldClip != null || (posed && !showcasing) || mitaAnim == null) return;
            var idle = (skin != null && !skin.OwnAnimator ? skin.IdleClip : null) ?? Clip("Mita Idle") ?? originalIdle;
            try { if (idle != null) mitaAnim.AnimationClipSimpleNext(idle); } catch { }
        }

        // her own action, with the body to go with it
        string SpecialGesture() => Pn.Id switch
        {
            "kind" => "Mita Thinks",           // (advice: she thinks it over)
            "cap" => null,                    // (her dance start walks her into you)
            "mila" => null,                   // ("Mita DrinkTea" is a sitting clip)
            "sleepy" => "Mita Warmup",        // (the coffee wakes her up: a big stretch)
            "short" => "Mita Pointer",
            _ => null,
        };

        string ByeGesture() => Pn.Id == "kind" ? "MitaKind Pointer" : "Mita ByeBye";

        // ---- dev: how far each gesture carries her (a clip that walks her off her spot shows up here) ----
        string[] probeList;
        int probeIndex;
        float probeUntil, probeMax;
        string probeName;
        Vector3 probeStart;
        Transform probeHips;

        public void DevGestureProbe(string csv)
        {
            probeList = string.IsNullOrWhiteSpace(csv)
                ? TalkGestures.Values.SelectMany(v => v)
                    .Concat(new[] { "Mita ByeBye", "Mita Thinks", "Mita Start Dance", "Mita DrinkTea", "Mita Warmup", "Mita Player Win", "Mita Player Lose", "Mila Menace", "Mita TakeHand", "Mita IdleHand" })
                    .Distinct().ToArray()
                : csv.Split(',').Select(x => x.Trim()).ToArray();
            probeIndex = 0; probeUntil = 0f; probeName = null;
            probeHips = mitaBody != null ? Util.FindDeep(mitaBody, "Hips") : null;
        }

        // (LateUpdate: after the animator has posed her)
        void TickGestureProbe()
        {
            if (probeList == null || probeHips == null) return;
            if (probeName != null)
            {
                var d = probeHips.position - probeStart; d.y = 0;
                probeMax = Mathf.Max(probeMax, d.magnitude);
                if (Time.time < probeUntil) return;
                Plugin.Log.LogInfo($"[dev] gesture probe {probeName}: hips moved up to {probeMax:F2} m");
                probeName = null;
                probeUntil = Time.time + 1f;   // (back in her idle first)
                return;
            }
            if (Time.time < probeUntil) return;
            if (probeIndex >= probeList.Length) { probeList = null; Plugin.Log.LogInfo("[dev] gesture probe done"); return; }
            var c = Clip(probeList[probeIndex++]);
            if (c == null) return;
            probeName = c.name; probeStart = probeHips.position; probeMax = 0f;
            PlayGestureClip(c);
            probeUntil = Time.time + Mathf.Min(c.length, 8f) + 0.2f;
        }

        // ---- hide and seek: crouched in her hiding place (the mannequins' hiding, Scene 10) ----
        void HideCrouch()
        {
            int i = Random.Range(1, 4);
            var start = Clip("Mita StartHide " + i);
            var idle = Clip("Mita Hide " + i);
            if (start == null || idle == null || mitaAnim == null) return;
            try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(start); } catch { }
            After(start.length - 0.05f, () => { if (hideStage != 0) HoldClip(idle); });
        }

        // ---- the backrooms' animation players: holding hands (Kind Mita and you), taking something ----
        ObjectAnimationPlayer handPlayer, takePlayer;
        Vector3 handPlayerOffset = new Vector3(0.62f, 0f, 0.32f);
        float handPlayerYaw = 180f;

        void KeepBackroomsPlayers(Scene s)
        {
            var here = SceneManager.GetActiveScene();
            Transform mitaAt = null, hand = null, take = null;
            foreach (var r in s.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                {
                    if (mitaAt == null && t.name == "Position HandHold") mitaAt = t;
                    var oap = t.GetComponent<ObjectAnimationPlayer>();
                    if (oap == null || oap.animationStart == null) continue;
                    if (hand == null && oap.animationStart.name == "Player HoldHand") hand = t;
                    if (take == null && oap.animationStart.name == "Player Take" && oap.animationStart.length < 4f && oap.animationLoop == null) take = t;
                }
            if (mitaAt != null && hand != null)
            {
                handPlayerOffset = Quaternion.Inverse(mitaAt.rotation) * (hand.position - mitaAt.position);
                handPlayerYaw = Mathf.DeltaAngle(mitaAt.eulerAngles.y, hand.eulerAngles.y);
            }
            if (hand != null) { SceneImport.StripOutside(hand.gameObject, s); handPlayer = SceneImport.Take(hand, here).GetComponent<ObjectAnimationPlayer>(); }
            if (take != null) { SceneImport.StripOutside(take.gameObject, s); takePlayer = SceneImport.Take(take, here).GetComponent<ObjectAnimationPlayer>(); }
            Plugin.Log.LogInfo($"[anim] backrooms players: hand {handPlayer != null} (you at {handPlayerOffset} yaw {handPlayerYaw:0}), take {takePlayer != null}");
        }

        // her hand in yours (Kind Mita's "give me your hand" in the backrooms), while `lines` play
        [HideFromIl2Cpp] bool StageHoldHands(IList<Line> lines, Action done)
        {
            var mc = Clip("Mita HoldHand");
            if (mc == null || handPlayer == null || pm == null || mitaBody == null || mitaAnim == null || posed || hugging || (skin != null && skin.OwnAnimator)) return false;
            hugging = true;
            var mp = mitaBody.position;
            var to = pm.transform.position - mp; to.y = 0;
            if (to.sqrMagnitude < 0.01f) to = Vector3.forward;
            // she turns so that you, standing where the backrooms put you, stay about where you are
            var yaw = Quaternion.LookRotation(to.normalized).eulerAngles.y - Mathf.Atan2(handPlayerOffset.x, handPlayerOffset.z) * Mathf.Rad2Deg;
            PoseMitaUpright(mp, to);
            uprightFront = Vector3.zero;
            var handSpot = ClearFacing(mp, Quaternion.Euler(0f, yaw, 0f), handPlayerOffset, out poseRot);   // (open floor: see StageHug)
            yaw = poseRot.eulerAngles.y;
            mitaBody.rotation = poseRot;
            try { mitaLook?.Activation(false); } catch { }
            handPlayer.transform.SetPositionAndRotation(handSpot, Quaternion.Euler(0f, yaw + handPlayerYaw, 0f));
            try { handPlayer.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[anim] hand (player): " + e.Message); }
            // (a frame later: see StageHug - PoseMitaUpright's own next clip in the same frame cut this one off at a quarter)
            SettleCloth();
            HoldHandsView(true);
            bool linesDone = false, animDone = false;
            Action finish = () =>
            {
                if (!linesDone || !animDone) return;
                HoldHandsView(false);
                hugging = false;
                if (posed && mitaBody != null && pm != null) ReleasePose(mitaBody.position, pm.transform.position);
                try { if (pm.animationRun) pm.AnimationStopFull(); } catch { }
                StayFacingYou(0.9f);
                done?.Invoke();
            };
            // (its end watched only once it is surely on: see StageHug)
            After(0.001f, () => { if (hugging) PlayClipSure(mc, () => hugging, () => AtClipEnd(mc.name, mc.length + 1f, () => { animDone = true; finish(); })); });
            Play(lines, () => { linesDone = true; finish(); });
            Plugin.Log.LogInfo("[anim] holding hands");
            return true;
        }

        // the end of something done together (holding hands): you a step back if the clip left you on top of her, both
        // facing each other, and she stays a moment instead of wandering straight off (her roaming had her turn away)
        void StayFacingYou(float minDist)
        {
            if (pm == null || mitaBody == null) return;
            var d = pm.transform.position - mitaBody.position; d.y = 0;
            if (d.magnitude < minDist)
            {
                var dir = d.sqrMagnitude > 0.0001f ? d.normalized : -mitaBody.forward;
                var at = ClearSpot(mitaBody.position + dir * (minDist + 0.15f));
                MovePlayer(at, mitaBody.position);
                Plugin.Log.LogInfo($"[peace] a step back: {d.magnitude:0.00} m -> {(at - mitaBody.position).magnitude:0.00} m");
            }
            else FaceHer();
            try
            {
                if (!attentiveHold) { attentiveHold = true; HoldAddon(true); }
                attentiveUntil = Time.time + 8f;
                mitaLook?.Activation(true);
                mitaLook?.LookOnPlayerAndRotate();
            }
            catch { }
        }

        // after a shared animation your body ends up turned wherever the clip left it: back to looking at her
        void FaceHer()
        {
            if (pm == null || mitaBody == null) return;
            var p = pm.transform.position;
            if ((mitaBody.position - p).sqrMagnitude > 0.04f) MovePlayer(p, mitaBody.position);
        }

        // you reach out and take something (the thing she asked for): the backrooms' "take", where you stand
        void PlayerTake()
        {
            if (takePlayer == null || pm == null) return;
            try
            {
                takePlayer.transform.SetPositionAndRotation(pm.transform.position, Quaternion.Euler(0f, pm.transform.eulerAngles.y, 0f));
                takePlayer.AnimationPlay();
            }
            catch (Exception e) { Plugin.Log.LogWarning("[anim] take: " + e.Message); }
        }

        // ---- dev: everything about animations in a borrowed scene ----
        public static string AnimScan(Scene s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("### " + s.name);
            sb.AppendLine("## clips (new with this scene)");
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimationClip>()))
            {
                if (o == null || (DevScanBefore != null && DevScanBefore.Contains(o.GetInstanceID()))) continue;
                var c = o.Cast<AnimationClip>();
                sb.AppendLine($"  {c.name}  len={c.length:0.00} loop={c.isLooping} human={c.humanMotion} events={c.events?.Length ?? 0}");
            }
            sb.AppendLine("## animation players / positions");
            foreach (var r in s.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                {
                    var n = t.name;
                    var oap = t.GetComponent<ObjectAnimationPlayer>();
                    if (oap != null)
                        sb.AppendLine($"  OAP {Util.PathOf(t)} pos={t.position} rot={t.eulerAngles} start={oap.animationStart?.name} loop={oap.animationLoop?.name} stop={oap.animationStop?.name} hips={oap.stopPositionHips}");
                    else if (n.Contains("Position") || n.Contains("Hug") || n.Contains("Sleep") || n.Contains("Bed") || n.Contains("Sofa") || n.Contains("Chair") || n.Contains("Sit"))
                        sb.AppendLine($"  POS {Util.PathOf(t)} pos={t.position} rot={t.eulerAngles}");
                    var an = t.GetComponent<Animator>();
                    if (an != null && an.runtimeAnimatorController != null)
                        sb.AppendLine($"  ANIMATOR {Util.PathOf(t)} ctrl={an.runtimeAnimatorController.name} clips={string.Join(", ", an.runtimeAnimatorController.animationClips.Select(c => c?.name).Distinct())}");
                }
            sb.AppendLine("## events that play animations");
            foreach (var r in s.GetRootGameObjects())
                foreach (var comp in r.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) continue;
                    var type = SceneDumper.FindType(comp.GetIl2CppType().Name);
                    if (type == null) continue;
                    object w;
                    try { w = Activator.CreateInstance(type, new object[] { comp.Pointer }); } catch { continue; }
                    foreach (var p in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                    {
                        if (!typeof(UnityEventBase).IsAssignableFrom(p.PropertyType)) continue;
                        string ev;
                        try { ev = SceneDumper.EventString(p.GetValue(w) as UnityEventBase); } catch { continue; }
                        if (ev == null || !(ev.Contains("Animation") || ev.Contains("Clip") || ev.Contains("Play"))) continue;
                        sb.AppendLine($"  {Util.PathOf(comp.transform)} [{type.Name}.{p.Name}] {ev}");
                    }
                }
            return sb.ToString();
        }
    }
}
