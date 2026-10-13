using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // Eating together, with the table set of the story's "Scene 3 - WeTogether" (World>Quests>Quest 5 Kitchen):
    // plates, food and forks, the player sitting down on the kitchen chair (Interactive SitOnChair's animation) and
    // the "eat" key (3D HintKey - Eat: fork to the mouth behind the CanvasEat fade).
    // Mita sits down on the chair across the table with Scene 3's "Mita Eating" (see KeepSceneClips), the story's fork
    // in her hand; its bites (her animation events 4 and 5 there) are passed to the fork's Location3FoodMita.
    // Only the calls between the borrowed eating parts are kept: the story's lines (ketchup, scissors), its Mita
    // calls and the ending (plate dropped, camera noise, music) go.
    public partial class PeaceDirector
    {
        GameObject mealTemplate, mealSet;
        ObjectAnimationPlayer sitAnim;
        Interface_KeyHint_Key eatKey;
        Vector3 mealMitaSpot, mealPlayerPlate;
        bool ateThisMeal, eatingNow, mitaSeated;
        Transform mealFork;
        Location3FoodMita mealFood;

        static readonly HashSet<string> MealKeep = new HashSet<string>
        {
            "Interactive SitOnChair", "AnimaitonPlayer Eat", "AnimaitonPlayer StopEat", "CanvasEat",
            "Food", "Food PlayerEat Start", "Food PlayerEat Stop", "Particle SteamFood Player", "Particle SteamFood Mita",
            "Fork Player", "Plate Player", "AnimaitonPlayer Stand",
        };

        // from the Scene 3 import (see ImportCooking): move = Scene 3 house -> this house
        [HideFromIl2Cpp] void BorrowMealSet(Scene s, Scene here, Func<Vector3, Vector3> move, Quaternion turn)
        {
            Transform q5 = null;
            foreach (var r in s.GetRootGameObjects()) { q5 = Util.FindDeep(r.transform, "Quest 5 Kitchen"); if (q5 != null) break; }
            if (q5 == null) { Plugin.Log.LogWarning("[meal] no Quest 5 Kitchen in Scene 3"); return; }
            int dropped = SceneImport.StripOutside(q5.gameObject, s) + KeepOnly(q5.gameObject, MealKeep);
            // standing up is kept, but not what it led to (the plate carried to the sink, then dropped)
            var standT = Util.FindDeep(q5, "AnimaitonPlayer Stand");
            var standA = standT != null ? standT.GetComponent<ObjectAnimationPlayer>() : null;
            if (standA != null)
                foreach (var ev in new UnityEventBase[] { standA.eventFinish, standA.eventStartLoop, standA.eventStartAnimaiton })
                {
                    var calls = ev?.m_PersistentCalls?.m_Calls;
                    if (calls == null || calls.Count == 0) continue;
                    dropped += calls.Count; calls.Clear(); ev.DirtyPersistentCalls();
                }
            q5.gameObject.SetActive(false);
            var p = move(q5.position);
            var q = turn * q5.rotation;
            SceneImport.Take(q5, here);
            q5.SetPositionAndRotation(p, q);
            q5.SetParent(templates.transform, true);
            // (tables are set from a copy, as the cooking corner: see ImportCooking)
            q5.name = "PeaceMeal Original";
            mealTemplate = Instantiate(q5.gameObject, templates.transform);
            mealTemplate.name = "PeaceMeal Template";
            var mp = Util.FindDeep(q5, "Plate Mita");
            var pp = Util.FindDeep(q5, "Plate Player");
            var sit = Util.FindDeep(q5, "Interactive SitOnChair");
            if (mp != null && pp != null)
            {
                var across = mp.position - pp.position; across.y = 0;
                mealMitaSpot = new Vector3(mp.position.x, 0f, mp.position.z) + across.normalized * 0.45f;
                mealPlayerPlate = new Vector3(pp.position.x, 0f, pp.position.z);
            }
            Plugin.Log.LogInfo($"[meal] table set borrowed ({dropped} calls dropped): plates {(mp != null ? mp.position.ToString() : "-")} / {(pp != null ? pp.position.ToString() : "-")}, chair {(sit != null ? sit.position.ToString() : "-")}, Mita at {mealMitaSpot}");
        }

        // removes every persistent call between the borrowed objects except those aimed at the listed ones
        static int KeepOnly(GameObject root, HashSet<string> keep)
        {
            int removed = 0;
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
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
                        var go = tgt == null ? null : tgt.TryCast<GameObject>() ?? tgt.TryCast<Component>()?.gameObject;
                        if (go != null && keep.Contains(go.name)) continue;
                        calls.RemoveAt(i); removed++; changed = true;
                    }
                    if (changed) ev.DirtyPersistentCalls();
                }
            }
            return removed;
        }

        // (screen black) sit down at the table with the story's plates; false = no table set, use the old meal
        bool StartMealSet()
        {
            if (mealTemplate == null) return false;
            ClearMealSet();
            mealSet = Instantiate(mealTemplate);
            mealSet.name = "PeaceMeal";
            SceneManager.MoveGameObjectToScene(mealSet, SceneManager.GetActiveScene());
            mealSet.SetActive(true);
            // Mita's own eating (fork, food in her hand) runs on Scene 3's Mita: off
            foreach (var f in mealSet.GetComponentsInChildren<Location3FoodMita>(true)) f.enabled = false;
            var sit = Util.FindDeep(mealSet.transform, "Interactive SitOnChair");
            sitAnim = sit != null ? sit.GetComponent<ObjectAnimationPlayer>() : null;
            var oi = sit != null ? sit.GetComponent<ObjectInteractive>() : null;
            if (oi != null) oi.enabled = false;   // we sit you down ourselves
            var key = Util.FindDeep(mealSet.transform, "3D HintKey - Eat");
            eatKey = key != null ? key.GetComponent<Interface_KeyHint_Key>() : null;
            if (eatKey != null) eatKey.eventKeyDown.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnPlayerEats)));
            var canvas = Util.FindDeep(mealSet.transform, "CanvasEat");
            var bs = canvas != null ? canvas.GetComponent<BlackScreenAnimation>() : null;
            if (bs != null) bs.eventStop1.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => { eatingNow = false; Plugin.Log.LogInfo("[meal] bite done"); })));
            ateThisMeal = eatingNow = false;
            if (sitAnim == null) { Plugin.Log.LogWarning("[meal] no chair animation"); ClearMealSet(); return false; }
            try { sitAnim.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[meal] sit: " + e.Message); ClearMealSet(); return false; }
            ShowRoom("Kitchen");
            SeatMitaAtTable();
            Plugin.Log.LogInfo($"[meal] sitting down (eat key {(eatKey != null)}, Mita {(mitaSeated ? "seated" : "standing")})");
            return true;
        }

        // her chair is the one across from yours (Kitchen Chair 2); without Scene 3's eating clip she stands there
        void SeatMitaAtTable()
        {
            mitaSeated = false;
            var eatClip = Clip("Mita Eating");
            var chair = Util.FindPath("Kitchen Chair 2");
            var seat = chair != null ? new Vector3(chair.transform.position.x, 0f, chair.transform.position.z) : SeatMita;
            var plate = mealPlayerPlate != Vector3.zero ? mealPlayerPlate : SeatPlayerSide;
            if (eatClip == null || mitaAnim == null)
            {
                var spot = mealMitaSpot != Vector3.zero ? mealMitaSpot : SeatMita;
                PoseMitaUpright(spot, plate - spot);
                return;
            }
            PoseMitaUpright(seat, plate - seat);
            HoldClip(eatClip);
            try { mitaLook?.Activation(false); } catch { }
            mitaSeated = true;
            // the story's fork, in her right hand
            var fork = Util.FindDeep(mealSet.transform, "Fork Mita");
            mealFork = PutInHand(fork);
            if (mealFork != null) { fork.gameObject.SetActive(false); mealFood = mealFork.GetComponent<Location3FoodMita>(); }
            Plugin.Log.LogInfo($"[meal] Mita seated at {seat}, fork {(mealFork != null)}");
        }

        // A borrowed clip she keeps playing while posed (eating at the table, cooking at the stove). Pausing her
        // roaming queues her idle in the same frame and that start wins over ours, so KeepPose (re)starts it.
        AnimationClip heldClip;
        float nextHeldCheck;

        void HoldClip(AnimationClip c)
        {
            heldClip = c;
            nextHeldCheck = Time.time + 0.2f;
            if (c != null) SetIdle(c);
        }

        // back to her own idle right away (the borrowed clip's events stay muted only while it plays)
        void ReleaseClip()
        {
            if (heldClip == null) return;
            heldClip = null;
            var idle = (skin != null && !skin.OwnAnimator ? skin.IdleClip : null) ?? Clip("Mita Idle") ?? originalIdle;
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            try { mitaAnim?.ResetOrder(); if (idle != null) mitaAnim?.AnimationClipSimpleNext(idle); } catch { }
        }

        void KeepHeldClip()
        {
            if (heldClip == null || !posed || mitaBody == null || Time.time < nextHeldCheck) return;
            nextHeldCheck = Time.time + 0.5f;
            var an = mitaBody.GetComponent<Animator>();
            if (an == null || Playing(an, heldClip.name)) return;
            try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(heldClip); } catch (Exception e) { Plugin.Log.LogWarning("[peace] held clip: " + e.Message); }
            if (DevRunner.Active) Plugin.Log.LogInfo("[peace] Mita (re)starts " + heldClip.name);
        }

        // a copy of the story's prop (fork, spatula) in her right hand; its magnet pointed at Scene 3's Mita
        Transform PutInHand(Transform template)
        {
            var hand = mitaBody != null ? Util.FindDeep(mitaBody, "RightItem") : null;
            if (template == null || hand == null) { Plugin.Log.LogWarning($"[peace] prop for her hand: template {(template != null)} hand {(hand != null)}"); return null; }
            var t = Instantiate(template.gameObject).transform;
            t.name = template.name;
            foreach (var m in t.GetComponents<Transform_Magnet>()) m.enabled = false;
            t.SetParent(hand, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.gameObject.SetActive(true);
            return t;
        }

        bool PlayingBorrowed()
        {
            var an = mitaBody != null ? mitaBody.GetComponent<Animator>() : null;
            if (an == null) return false;
            foreach (var n in BorrowedClips) if (Playing(an, n)) return true;
            return false;
        }

        static bool Playing(Animator an, string clip)
        {
            foreach (var ci in an.GetCurrentAnimatorClipInfo(0)) if (ci.clip != null && ci.clip.name == clip) return true;
            foreach (var ci in an.GetNextAnimatorClipInfo(0)) if (ci.clip != null && ci.clip.name == clip) return true;
            return false;
        }

        // her bites, from "Mita Eating" (Scene 3's event slots 4 and 5)
        void MitaBite(int slot)
        {
            if (mealFood == null) return;
            try
            {
                var foods = mealFood.foods;
                if (slot == 4)
                {
                    if (foods != null && mealFood.indexFood >= foods.Length) mealFood.indexFood = 0;   // a plate that never runs out
                    mealFood.TakeFood();
                }
                else if (slot == 5) mealFood.EatFood();
            }
            catch (Exception e) { Plugin.Log.LogWarning("[meal] Mita bite: " + e.Message); mealFood = null; }
        }

        // the eat key shows once the lines start; pressing it is up to you
        void ShowEatKey(bool on)
        {
            if (eatKey == null) return;
            if (eatKey.gameObject.activeSelf != on) eatKey.gameObject.SetActive(on);
        }

        void OnPlayerEats()
        {
            ateThisMeal = true;
            eatingNow = true;
            Plugin.Log.LogInfo("[meal] you eat");
        }

        void ClearMealSet()
        {
            if (mealFork != null) Destroy(mealFork.gameObject);
            if (mealSet != null) Destroy(mealSet);
            mealSet = null; sitAnim = null; eatKey = null; mealFork = null; mealFood = null;
            if (mitaSeated)
            {
                mitaSeated = false;
                ReleaseClip();
                try { mitaLook?.Activation(true); } catch { }
            }
        }

        // (screen black) stand up with the story's own animation: cutting the sitting one short left the camera behind
        // the player's body. Returns false when there is nothing to wait for.
        bool StandUpFromMeal()
        {
            if (mealSet == null) return false;
            var st = Util.FindDeep(mealSet.transform, "AnimaitonPlayer Stand");
            var anim = st != null ? st.GetComponent<ObjectAnimationPlayer>() : null;
            try
            {
                if (anim != null) { anim.AnimationPlay(); return true; }
                pm?.AnimationStopFull();
            }
            catch (Exception e) { Plugin.Log.LogWarning("[meal] stand: " + e.Message); try { pm?.AnimationStopFull(); } catch { } }
            return false;
        }

        // Scene 3's clips of Mita (eating at the table, cooking) outlive the scene: kept here, and kept loaded
        static readonly string[] BorrowedClips = { "Mita Eating", "Mita ContinueEating", "Mita StopEatForIdle", "Mita StartCooking", "Mita Cooking", "Mita Cooking StopShow Carrot", "Mita Yawns" };

        void KeepSceneClips(Scene s)
        {
            int kept = 0;
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimationClip>()))
            {
                if (o == null || Array.IndexOf(BorrowedClips, o.name) < 0 || clips.ContainsKey(o.name)) continue;
                var c = o.Cast<AnimationClip>();
                c.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                clips[o.name] = c;
                kept++;
            }
            if (DevRunner.Active)
                foreach (var r in s.GetRootGameObjects())
                {
                    foreach (var a in r.GetComponentsInChildren<Animator_FunctionsOverride>(true))
                    {
                        if (a == null || a.eventAnimation == null || a.name != "MitaPerson Mita") continue;
                        for (int i = 0; i < a.eventAnimation.Count; i++) Plugin.Log.LogInfo($"[meal] Scene 3 Mita event {i}: {SceneDumper.EventString(a.eventAnimation[i])}");
                    }
                    var fork = Util.FindDeep(r.transform, "Fork Mita");
                    var fm = fork != null ? fork.GetComponent<Transform_Magnet>() : null;
                    if (fm != null)
                        Plugin.Log.LogInfo($"[meal] Fork Mita magnet: target {Util.PathOf(fm.transformTarget)} magnet {Util.PathOf(fm.transformMagnet)} local={fm.local} localMagnet={fm.localMagnet} pos={fm.position} rot={fm.rotation} active={fm.active} parent={Util.PathOf(fork.parent)} lpos={fork.localPosition} lrot={fork.localEulerAngles}");
                }
            FindAudio("Music Ambient Scene");   // (its day music: the morning track, see PeaceMusic)
            Plugin.Log.LogInfo($"[meal] kept {kept} of Scene 3's Mita clips");
        }

        // while Mita plays Scene 3's clips, the numbered events they fire mean something else in this scene (the TV
        // game's gamepad and remote): muted
        public bool MutesAnimEvent(Animator_FunctionsOverride a, int slot)
        {
            if (a == null || mitaAnim == null || a.Pointer != mitaAnim.Pointer) return false;
            bool mute = mealSet != null || eating || cooking || devMuteEvents || PlayingBorrowed();
            if (DevRunner.Active) Plugin.Log.LogInfo($"[peace] Mita animation event {slot}{(mute ? " (muted)" : "")}");
            if (mitaSeated) MitaBite(slot);
            return mute;
        }

        bool devMuteEvents;
        public void DevMuteEvents(bool on) => devMuteEvents = on;

        public void DevEatKey() { try { eatKey?.eventKeyDown?.Invoke(); } catch (Exception e) { Plugin.Log.LogWarning("[dev] eat: " + e.Message); } }
    }
}
