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
            try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(sleep); } catch { }
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
            holdCam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            Plugin.Log.LogInfo($"[anim] stage view from {main.name} at {main.transform.position}");
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

        bool CanHugLikeTheEnding => hugPlayer != null && hugRelease != null && pm != null && mitaBody != null && mitaAnim != null && !posed
            && (skin == null || !skin.OwnAnimator) && Clip("Mita StartHug") != null && Clip("Mita HugIdle") != null && Clip("Mita StopHug") != null;

        // plays `lines` while you hold each other; false = not possible here (the caller just plays the lines)
        [HideFromIl2Cpp] bool StageHug(IList<Line> lines, Action done)
        {
            if (!CanHugLikeTheEnding || hugging) return false;
            hugging = true;
            var mp = mitaBody.position;
            var to = pm.transform.position - mp; to.y = 0;
            if (to.sqrMagnitude < 0.01f) to = Vector3.forward;
            // she stays where she is, turned to you (standing, her face is on the root's +Z: see DoWindow)
            PoseMitaUpright(mp, to);
            uprightFront = Vector3.zero;
            poseRot = Quaternion.LookRotation(to.normalized);
            mitaBody.rotation = poseRot;
            try { mitaLook?.Activation(false); } catch { }
            // you: where the ending puts you, in front of her
            var spot = OnFloor(mp + poseRot * hugPlayerOffset);
            var rot = Quaternion.Euler(0f, poseRot.eulerAngles.y + hugPlayerYaw, 0f);
            hugPlayer.transform.SetPositionAndRotation(spot, rot);
            hugRelease.transform.SetPositionAndRotation(spot, rot);
            try { hugPlayer.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[anim] hug (player): " + e.Message); }
            var start = Clip("Mita StartHug");
            try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(start); } catch { }
            SettleCloth();
            After(start.length - 0.05f, () =>
            {
                if (!hugging) return;
                HoldClip(Clip("Mita HugIdle"));
                Play(lines, () => EndHug(done));
            });
            Plugin.Log.LogInfo($"[anim] hug at {spot}");
            return true;
        }

        [HideFromIl2Cpp] void EndHug(Action done)
        {
            if (!hugging) { done?.Invoke(); return; }
            var stop = Clip("Mita StopHug");
            heldClip = null;
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);   // (StopHug ends in her idle, not the hug's)
            try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(stop); } catch { }
            try { hugRelease.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[anim] hug release: " + e.Message); }
            After(stop.length + 0.1f, () =>
            {
                hugging = false;
                if (posed && mitaBody != null && pm != null) ReleasePose(mitaBody.position, pm.transform.position);
                try { if (pm.animationRun) pm.AnimationStopFull(); } catch { }
                FaceHer();
                try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
                done?.Invoke();
            });
        }

        // ---- her body language: clips from the Mitas' own scenes on her lines ----
        // lines without a clip of their own now and then get one of hers (a talk gesture)
        static readonly Dictionary<string, string[]> TalkGestures = new Dictionary<string, string[]>
        {
            // (only clips that stay on her spot: Laugh, Jump, ShowHands, Yawns, Look Lamp, IdleTalk, Start Dance and
            // Mila Show Place carry where their Mita stood in her own scene, or lean her into the camera - gestureprobe)
            ["crazy"] = new[] { "Mita Start Thinks", "Mita Talk 1", "Mita Talk 2", "Mita Talk 3" },
            ["kind"] = new[] { "MitaKind Pointer", "Mita Start Thinks", "Mita Talk 1", "Mita Talk 3" },   // (Scene 6's console pointing leaves her in a T-pose)
            ["cap"] = new[] { "Mita Talk 2", "Mita Talk 4", "Mita Talk 5", "Mita Pointer" },
            ["mila"] = new[] { "Mila CheckBack", "Mita Talk 4" },
            ["sleepy"] = new[] { "Mita Talk 2", "Mita Start Thinks" },
            ["short"] = new[] { "Mita Talk 1", "Mita Talk 2", "Mita Talk 3", "Mita Talk 4", "Mita Talk 5", "Mita Pointer" },
        };

        string forcedGesture;
        float nextTalkGesture, gestureUntil;
        public static bool DevAllGestures;   // (dev: a gesture on every line, for recording them)
        int gestureToken;

        // the next line of hers (one without a clip of its own) plays this
        void NextGesture(string clip) { if (clip != null && Clip(clip) != null) forcedGesture = clip; }

        [HideFromIl2Cpp] string TakeGesture(Line l)
        {
            if (forcedGesture != null) { var f = forcedGesture; forcedGesture = null; return f; }
            if ((posed && !showcasing) || Time.time < nextTalkGesture || !TalkGestures.TryGetValue(Pn.Id, out var set)) return null;
            if (Pn.Id == "mila" && (l.emo == "angry" || l.emo == "discontent") && Clip("Mila Menace") != null && Random.value < 0.5f) { nextTalkGesture = Time.time + 6f; return "Mila Menace"; }
            if (!DevAllGestures && Random.value > 0.4f) return null;
            nextTalkGesture = Time.time + (DevAllGestures ? 2.5f : Random.Range(5f, 9f));
            var pick = set[Random.Range(0, set.Length)];
            return Clip(pick) != null ? pick : null;
        }

        // a gesture on her lines; one that loops is ended after one round (back to her idle)
        void PlayGestureClip(AnimationClip c)
        {
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
            poseRot = Quaternion.Euler(0f, yaw, 0f);
            mitaBody.rotation = poseRot;
            try { mitaLook?.Activation(false); } catch { }
            handPlayer.transform.SetPositionAndRotation(OnFloor(mp + poseRot * handPlayerOffset), Quaternion.Euler(0f, yaw + handPlayerYaw, 0f));
            try { handPlayer.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[anim] hand (player): " + e.Message); }
            try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(mc); } catch { }
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
                FaceHer();
                try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
                done?.Invoke();
            };
            After(mc.length + 0.1f, () => { animDone = true; finish(); });
            Play(lines, () => { linesDone = true; finish(); });
            Plugin.Log.LogInfo("[anim] holding hands");
            return true;
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
