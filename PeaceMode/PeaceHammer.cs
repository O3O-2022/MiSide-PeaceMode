using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // Cappie's hammer game (Scene 7, the kitchen table): a big button between you, a timer counting down; when the
    // system says go, press it before she does - whoever's slower gets bopped with her toy hammer. Three hearts each.
    // Borrowed like the dance pad (PeaceDance), but set by the kitchen table: her kitchen there is ours turned half
    // way round, so the setup goes from her table's frame to ours. Her toy hammer (on her own model in the story) and
    // the chair she pulls out come along. Her clips' numbered events (take the hammer, put it down, press the button)
    // arrive through an Events_Animator we give our Mita. Her lines are the story's own (LocationDialogue Location7).
    [HarmonyPatch]
    public static class MitaClipEventHook
    {
        // our Mita's Events_Animator (see EnsureMitaEvents): her borrowed clips' numbered events come here
        [HarmonyPatch(typeof(Events_Animator), nameof(Events_Animator.EventAnim)), HarmonyPrefix]
        static bool MitaEventAnim(Events_Animator __instance, int _index)
        {
            if (__instance == null || __instance.Pointer != PeaceDirector.mitaEventsPtr) return true;
            try { PeaceDirector.Instance?.OnMitaClipEvent(_index); } catch (Exception e) { Plugin.Log.LogWarning("[hammer] clip event: " + e.Message); }
            return false;
        }
    }

    public partial class PeaceDirector
    {
        GameObject hammerTemplate;       // the borrowed setup, off; a fresh copy per game
        GameObject hammerNow;
        Location7_HammerButton hb;
        int hammerStage;                 // 0 none, 1 sitting down / her intro, 2 a round, 3 talk after a hit, 4 over
        GameObject[] hammerYouWon, hammerSheWon;
        Transform hammerToy, hammerChairOurs;
        Vector3 hammerChairAt;
        bool hammerExplained;
        Events_Animator mitaEvents;      // ours, on her animator: the story Mitas' clips call EventAnim(n)
        public static IntPtr mitaEventsPtr;

        bool HammerReady => hammerTemplate != null && Pn.Id == "cap" && KitchenTable() != null;

        Transform KitchenTable()
        {
            var kitchen = Util.FindPath("HouseGame Tamagotchi>House>Kitchen");
            return kitchen != null ? kitchen.transform.Find("Kitchen Table") : null;
        }

        void KeepHammerGame(Scene s)
        {
            Location7_HammerButton game = null;
            foreach (var r in s.GetRootGameObjects())
            {
                foreach (var g in r.GetComponentsInChildren<Location7_HammerButton>(true)) { game = g; break; }
                if (game != null) break;
            }
            if (game == null) { Plugin.Log.LogWarning("[hammer] no hammer game in " + s.name); return; }
            var group = game.transform;
            var button = Util.FindDeep(group, "Button");
            Transform Nearest(string name)
            {
                Transform best = null; float d = float.MaxValue;
                foreach (var r in s.GetRootGameObjects())
                    foreach (var t in r.GetComponentsInChildren<Transform>(true))
                        if (t != null && t.name == name && t.GetComponent<MeshRenderer>() != null)
                        {
                            float dd = (t.position - (button != null ? button.position : group.position)).sqrMagnitude;
                            if (dd < d) { d = dd; best = t; }
                        }
                return best;
            }
            var table = Nearest("Kitchen Table");
            var chair = Nearest("Kitchen Chair 2");
            Transform toy = null;
            foreach (var r in s.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform_Magnet>(true))
                    if (t != null && t.name == "HammerToy") { toy = t.transform; break; }
            if (table == null || KitchenTable() == null) { Plugin.Log.LogWarning($"[hammer] her table {table != null}, ours {KitchenTable() != null}"); return; }

            // her toy hammer and the chair she pulls out: into the setup, so the calls on them stay
            if (toy != null) toy.SetParent(group, true);
            if (chair != null) chair.SetParent(group, true);
            var log = new StringBuilder();
            log.AppendLine($"hammer game, her table {table.position} {table.eulerAngles.y:0}, ours {KitchenTable().position} {KitchenTable().eulerAngles.y:0}, toy {toy != null}, chair {chair != null}");
            int moved = SceneImport.Retarget(group.gameObject, s, MapHammerTarget, log);

            group.gameObject.SetActive(false);
            hammerTemplate = new GameObject("PeaceHammerTemplate");
            SceneManager.MoveGameObjectToScene(hammerTemplate, KitchenTable().gameObject.scene);
            hammerTemplate.SetActive(false);
            hammerTemplate.transform.SetPositionAndRotation(table.position, table.rotation);
            group.SetParent(hammerTemplate.transform, true);
            group.gameObject.SetActive(true);
            Plugin.Log.LogInfo($"[hammer] Cappie's hammer game borrowed ({moved} calls now ours)");
            if (DevRunner.Active) File.WriteAllText(Path.Combine(BepInEx.Paths.ConfigPath, "hammer_events.txt"), log.ToString(), Encoding.UTF8);
        }

        UnityEngine.Object MapHammerTarget(UnityEngine.Object tgt)
        {
            var comp = tgt.TryCast<Component>();
            if (comp == null) return null;
            switch (comp.GetIl2CppType().Name)
            {
                case "Animator_FunctionsOverride": return mitaAnim;
                case "Character_Look": return mitaLook;
                case "MitaPerson": case "Achievement_function": case "Light_LightingColor": return null;
                // (her hips' sound: the chair scrape, "look, a chair")
                case "AudioSource": if (comp.gameObject.name == "Hips" && mitaBody != null) { var h = Util.FindDeep(mitaBody, "Hips"); return h != null ? h.GetComponent<AudioSource>() : null; } return null;
            }
            if (comp.gameObject.name == "World" && world != null) return world.gameObject.GetComponent(comp.GetIl2CppType());
            return null;
        }

        // her clips' numbered events: the story Cappie's Events_Animator (10 hammer to the table, 11 hammer to her
        // hand, 12 her press on the button, 23 look at you); she has no such component of her own
        void EnsureMitaEvents()
        {
            if (mitaEvents != null || mitaAnim == null) return;
            var go = mitaAnim.gameObject;
            mitaEvents = go.GetComponent<Events_Animator>() ?? go.AddComponent<Events_Animator>();
            if (mitaEvents.events == null) mitaEvents.events = new Il2CppReferenceArray<Events_AnimatorEvent>(0);
            mitaEventsPtr = mitaEvents.Pointer;
        }

        public void OnMitaClipEvent(int n)
        {
            if (DevRunner.Active && hammerStage != 0) Plugin.Log.LogInfo($"[peace] Mita clip event {n} (hammer {hammerStage})");
            if (hammerNow == null || hammerToy == null) return;
            var magnet = hammerToy.GetComponent<Transform_Magnet>();
            switch (n)
            {
                case 10: { var rest = HammerFind("HammerToy Table"); if (rest != null) try { magnet?.ReMagnetAndParent(rest); } catch { } break; }
                case 11: { var hand = mitaBody != null ? Util.FindDeep(mitaBody, "RightItem") : null; if (hand != null) try { magnet?.ReMagnetAndParent(hand); } catch { } break; }
                case 12: try { hb?.ButtonDownMita(); } catch (Exception e) { Plugin.Log.LogWarning("[hammer] her press: " + e.Message); } break;
                case 23: try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { } break;
            }
        }

        Transform HammerFind(string name) => hammerNow != null ? Util.FindDeep(hammerNow.transform, name) : null;
        const string HammerFile = "LocationDialogue Location7";

        List<Line> HammerLines(params (bool mita, int idx, string emo)[] ls)
        {
            var list = new List<Line>();
            foreach (var (m, idx, emo) in ls) { var t = LineText(HammerFile, idx); if (t != null) list.Add(new Line(m ? Who.Mita : Who.Player, t, m ? emo : null)); }
            return list;
        }

        bool BuildHammer()
        {
            var ours = KitchenTable();
            var pivot = new GameObject("PeaceHammer");
            SceneManager.MoveGameObjectToScene(pivot, ours.gameObject.scene);
            pivot.SetActive(false);
            pivot.transform.SetPositionAndRotation(hammerTemplate.transform.position, hammerTemplate.transform.rotation);
            var src = hammerTemplate.transform.GetChild(0);
            var copy = UnityEngine.Object.Instantiate(src.gameObject, pivot.transform);
            copy.transform.localPosition = src.localPosition;
            copy.transform.localRotation = src.localRotation;
            pivot.transform.SetPositionAndRotation(ours.position, ours.rotation);
            hammerNow = pivot;
            hb = copy.GetComponent<Location7_HammerButton>();
            if (hb == null) { UnityEngine.Object.Destroy(pivot); hammerNow = null; return false; }

            // the story's lines go (we say them), the game's own "who won" spots get stand-ins we watch
            foreach (var d in copy.GetComponentsInChildren<Dialogue_3DText>(true)) if (d != null) UnityEngine.Object.Destroy(d.gameObject);
            var holder = new GameObject("PeaceHammerCues");
            holder.transform.SetParent(pivot.transform, false);
            GameObject[] Cues(string n)
            {
                var a = new GameObject[3];
                for (int i = 0; i < 3; i++) { a[i] = new GameObject(n + " " + i); a[i].transform.SetParent(holder.transform, false); a[i].SetActive(false); }
                return a;
            }
            hammerYouWon = Cues("you won"); hammerSheWon = Cues("she won");
            hb.playerDialogueWin = new Il2CppReferenceArray<GameObject>(hammerYouWon);
            hb.mitaDialogueWin = new Il2CppReferenceArray<GameObject>(hammerSheWon);

            hammerToy = HammerFind("HammerToy");
            // her chair: ours, the one nearest her seat, pulled back a little from the table (the story's own pulled-out
            // chair is animated in its old scene's space and lands far away: it goes)
            var theirs = HammerFind("Kitchen Chair 2");
            if (theirs != null) UnityEngine.Object.Destroy(theirs.gameObject);
            var herSeat = HammerFind("Mita Position Sit");
            hammerChairOurs = null;
            var kitchen = ours.parent;
            if (herSeat != null && kitchen != null)
            {
                float best = 1.5f;
                for (int i = 0; i < kitchen.childCount; i++)
                {
                    var c = kitchen.GetChild(i);
                    if (!c.name.StartsWith("Kitchen Chair")) continue;
                    float d = (c.position - herSeat.position).magnitude;
                    if (d < best) { best = d; hammerChairOurs = c; }
                }
                if (hammerChairOurs != null)
                {
                    hammerChairAt = hammerChairOurs.position;
                    var away = hammerChairOurs.position - ours.position; away.y = 0;
                    hammerChairOurs.position += away.normalized * 0.22f;
                }
            }
            EnsureMitaEvents();
            pivot.SetActive(true);
            Plugin.Log.LogInfo($"[hammer] set up at our table: button {HammerFind("Button")?.position}, her seat {HammerFind("Mita Position Sit")?.position}, toy {hammerToy != null}, her chair {hammerChairOurs?.name}");
            return true;
        }

        // ---- the game ----

        void StartHammer()
        {
            if (!HammerReady) { Plugin.Log.LogWarning("[hammer] not ready"); return; }
            Did("hammer");
            KeepAmbient();
            EndRoutine();
            StopFollow(false);
            hammerStage = 1;
            fadeTarget = 1f;
            afterFade = () =>
            {
                ClearLeftoverText();
                ShowRoom("Kitchen");
                if (!BuildHammer()) { EndHammerNow(); fadeTarget = 0f; return; }
                // you: sat down at the table (the story's own sit); her: on her seat, taking the hammer and sitting
                var seat = HammerFind("Mita Position Sit");
                if (seat != null)
                {
                    PoseMitaUpright(seat.position, seat.forward);
                    try { mitaLook?.Activation(false); } catch { }
                }
                // (the sit keeps the way you were facing: turned to her side of the table first)
                if (pm != null && seat != null) MovePlayer(pm.transform.position, seat.position);
                // (your sit is played from its anchor, which came over turned away from her: it faces her seat now)
                var sitAnchor = HammerFind("AnimationPlayer Sit");
                if (sitAnchor != null && seat != null)
                {
                    var to = seat.position - sitAnchor.position; to.y = 0;
                    var was = sitAnchor.forward; was.y = 0;
                    Plugin.Log.LogInfo($"[hammer] your sit anchor at {sitAnchor.position} faced {Vector3.SignedAngle(to, was, Vector3.up):0}° off her seat");
                    if (to.sqrMagnitude > 0.01f && DevHammerFaceHer) sitAnchor.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
                }
                try { HammerFind("AnimationPlayer Sit")?.GetComponent<ObjectAnimationPlayer>()?.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[hammer] sit: " + e.Message); }
                try { HammerFind("AnimationMita Sit")?.GetComponent<Time_Events>()?.YieldRestart(); } catch (Exception e) { Plugin.Log.LogWarning("[hammer] her sit: " + e.Message); }
                fadeTarget = 0f;
                // (her sit clip carries her hips off her feet's spot, the way the dance clips do: once she's down, her
                // hips go over the chair she pulled out)
                After(4f, () =>
                {
                    if (hammerStage != 1 || !posed || poseHips == null || hammerChairOurs == null) return;
                    var d = hammerChairOurs.position - poseHips.position; d.y = 0;
                    Plugin.Log.LogInfo($"[hammer] her hips {poseHips.position}, the chair {hammerChairOurs.position}: {d.magnitude:0.00} m apart");
                    if (d.magnitude > 0.08f && d.magnitude < 1.2f) posePos += d;
                });
                After(4.5f, HammerIntro);
            };
        }

        void HammerIntro()
        {
            if (hammerStage != 1) return;
            try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
            var lines = HammerLines((true, 363, "smile"), (true, 364, "smile"));
            lines.AddRange(HammerLines((true, 387, "smile")));
            Play(lines, () =>
            {
                try { HammerFind("AnimationMita ShowButton")?.GetComponent<Time_Events>()?.YieldRestart(); } catch { }
                if (!hammerExplained)
                {
                    hammerExplained = true;
                    // how it goes (the first time)
                    var how = HammerLines((true, 392, null), (true, 393, "smile"), (true, 394, null), (true, 395, null), (true, 396, null),
                                          (true, 397, null), (true, 398, null), (true, 399, "smile"), (false, 401, null), (true, 403, "smile"), (true, 404, "smile"));
                    After(1.5f, () => Play(how, HammerFirstRound));
                }
                else After(1.5f, HammerFirstRound);
            });
        }

        void HammerFirstRound()
        {
            if (hammerStage != 1) return;
            try { HammerFind("AnimationMita OnButton")?.GetComponent<Time_Events>()?.YieldRestart(); } catch { }
            var anims = HammerFind("Анимации игры");
            if (anims != null) anims.gameObject.SetActive(true);
            HammerRound();
        }

        void HammerRound()
        {
            hammerStage = 2;
            hammerAutoPressed = false;
            try { mitaLook?.Activation(false); } catch { }
            try { hb.PlayGame(); } catch (Exception e) { Plugin.Log.LogWarning("[hammer] round: " + e.Message); }
            Plugin.Log.LogInfo($"[hammer] a round: you {hb.hpPlayer}, her {hb.hpMita}");
        }

        void TickHammer()
        {
            if (hammerStage == 0 || hammerNow == null) return;
            // (the game switches a cue on and waits while it stays on - the story's lines playing: off once ours are said)
            if (hammerStage == 2)
                for (int i = 0; i < 3; i++)
                {
                    if (hammerYouWon[i].activeSelf) { HammerAfterHit(true, i, hammerYouWon[i]); break; }
                    if (hammerSheWon[i].activeSelf) { HammerAfterHit(false, i, hammerSheWon[i]); break; }
                }
            if (DevHammerAuto && hammerStage == 2 && hb.imgCanButton != null && hb.imgCanButton.gameObject.activeInHierarchy && !hammerAutoPressed)
            {
                hammerAutoPressed = true;
                After(Random.Range(0.1f, 0.5f), () => { try { hb.KeyButtonPlayer(); } catch { } });
            }
            if (!hb.canButtonDown && hammerStage != 2) hammerAutoPressed = false;
            // (you won: soft or hard - the auto test goes soft)
            if (DevHammerAuto && Time.time > hammerAutoKickAt)
            {
                var soft = HammerFind("3D HintKey Soft");
                if (soft != null && soft.gameObject.activeInHierarchy)
                {
                    hammerAutoKickAt = Time.time + 6f;
                    After(1.2f, () => { try { soft.GetComponent<Interface_KeyHint_Key>()?.eventKeyDown?.Invoke(); } catch { } });
                }
            }
        }

        float hammerAutoKickAt;

        public static bool DevHammerAuto;
        public static bool DevHammerFaceHer = true;   // dev: `hammer face off` keeps the sit anchor as it came
        bool hammerAutoPressed;

        // after a bop: what the story says there (by who won and how many times), then the next round or the end
        void HammerAfterHit(bool youWon, int i, GameObject cue)
        {
            hammerStage = 3;
            Plugin.Log.LogInfo($"[hammer] {(youWon ? "you" : "she")} won round: you {hb.hpPlayer}, her {hb.hpMita} (#{i})");
            List<Line> lines = youWon
                ? (i == 0 ? HammerLines((true, 427, "surprise"), (true, 428, "smile"), (true, 429, "discontent"))
                  : i == 1 ? HammerLines((true, 432, "surprise"), (false, 433, null), (true, 434, "smile"), (true, 435, "sad"))
                  : HammerLines((false, 438, null), (true, 439, "shy"), (true, 440, "smile"), (true, 441, "smile")))
                : (i == 0 ? HammerLines((false, 407, null), (false, 408, null), (true, 409, "surprise"), (false, 410, null), (true, 411, "smile"))
                  : i == 1 ? HammerLines((false, 414, null), (false, 415, null), (true, 416, "smile"), (true, 417, "smile"))
                  : HammerLines((false, 420, null), (true, 421, "smile"), (true, 422, "smile"), (true, 423, "smile"), (true, 424, "smile")));
            AddAff(youWon ? 1 : 2);
            bool last = i >= 2;
            try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
            After(1.4f, () => Play(lines, () => { cue.SetActive(false); if (last) EndHammer(); else HammerRound(); }));
        }

        void EndHammer()
        {
            hammerStage = 4;
            var lines = HammerLines((false, 444, null), (true, 445, "smile"), (true, 446, "smile"));
            Play(lines, () =>
            {
                var end = HammerFind("Анимации конечные");
                if (end != null) end.gameObject.SetActive(true);
                try { HammerFind("AnimationPlayer StandUp")?.GetComponent<ObjectAnimationPlayer>()?.AnimationPlay(); } catch { }
                try { HammerFind("AnimationMita Standup")?.GetComponent<Time_Events>()?.YieldRestart(); } catch { }
                After(2.5f, () =>
                {
                    fadeTarget = 1f;
                    afterFade = () => { ClearLeftoverText(); EndHammerNow(); fadeTarget = 0f; };
                });
            });
        }

        // (screen black) everything back
        void EndHammerNow()
        {
            if (hammerNow != null) UnityEngine.Object.Destroy(hammerNow);
            hammerNow = null; hb = null; hammerToy = null;
            RestoreAmbient();
            if (hammerChairOurs != null) hammerChairOurs.position = hammerChairAt;
            hammerChairOurs = null;
            if (posed && mitaBody != null && pm != null)
            {
                var at = mitaBody.position;
                if (NavMesh.SamplePosition(at, out var hit, 1.5f, -1)) at = hit.position;
                ReleasePose(at, pm.transform.position);
            }
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            try { mitaLook?.Activation(true); } catch { }
            hammerStage = 0;
            Plugin.Log.LogInfo("[hammer] over");
        }

        public void DevHammer(string what)
        {
            var sp = what.Split(' ');
            switch (sp[0])
            {
                case "start": StartHammer(); break;
                case "auto": DevHammerAuto = sp.Length < 2 || sp[1] != "off"; break;
                case "face": DevHammerFaceHer = sp.Length < 2 || sp[1] != "off"; break;
                case "press": try { hb?.KeyButtonPlayer(); } catch (Exception e) { Plugin.Log.LogWarning("[hammer] " + e.Message); } break;
                case "state":
                    if (hb == null) { Plugin.Log.LogInfo($"[hammer] no game (template {hammerTemplate != null})"); break; }
                    Plugin.Log.LogInfo($"[hammer] stage {hammerStage} you {hb.hpPlayer} her {hb.hpMita} can {hb.canButtonDown} timer {hb.timeTimer}/{hb.timeTimerMili:0.0} reaction {hb.reactionTimeMita:0.00} " +
                                       $"want {hb.playerWantButtonDown}/{hb.playerWantButtonDownBed} started {hb.timeStartPlay:0.0} youWin {hb.timePlayerWin:0.0} herWin {hb.timeMitaWin:0.0} dlg {hb.playerDialogueActive}/{hb.MitaDialogueActive}");
                    break;
                case "lines":   // hammer lines <from> <to>: the story's lines there, to check the numbering
                    for (int i = int.Parse(sp[1]); i <= int.Parse(sp[2]); i++) Plugin.Log.LogInfo($"[hammer] line {i}: {LineText(HammerFile, i)}");
                    break;
            }
        }
    }
}
