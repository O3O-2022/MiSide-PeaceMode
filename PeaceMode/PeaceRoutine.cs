using System;
using UnityEngine;
using UnityEngine.SceneManagement;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // Mita's own day (the official peace mode's quest icon and "she has something to do" flow):
    // the icon over her head is Scene 3's "Icon Dialogue" (the speech mark that floated over Mita when she wanted to talk),
    // borrowed with the rest of Scene 3 at the start of the day.
    public partial class PeaceDirector
    {
        GameObject questIcon;
        UI_LookOnCamera questIconLook;
        bool iconShown;
        float importAt = -1f;

        void BorrowIcon(Scene s, Scene here)
        {
            Transform icon = null;
            foreach (var r in s.GetRootGameObjects()) { icon = Util.FindDeep(r.transform, "Icon Dialogue"); if (icon != null) break; }
            if (icon == null) { Plugin.Log.LogWarning("[icon] no Icon Dialogue in Scene 3"); return; }
            SceneImport.StripOutside(icon.gameObject, s);
            icon.gameObject.SetActive(false);
            // it followed Scene 3's Mita by itself: we place it over ours
            foreach (var m in icon.GetComponents<Transform_Magnet>()) UnityEngine.Object.DestroyImmediate(m);
            SceneImport.Take(icon, here);
            questIcon = icon.gameObject;
            questIcon.name = "PeaceQuestIcon";
            questIconLook = questIcon.GetComponent<UI_LookOnCamera>();
            questIcon.SetActive(true);
            try { questIconLook?.Hide(true); } catch { }
            iconShown = false;
            Plugin.Log.LogInfo($"[icon] quest icon borrowed (look {(questIconLook != null)})");
        }

        // the speech mark over her head: she wants something (a meal, bed, ...)
        void ShowQuestIcon(bool on)
        {
            if (questIcon == null || on == iconShown) return;
            iconShown = on;
            try { questIconLook?.Hide(!on); } catch (Exception e) { Plugin.Log.LogWarning("[icon] " + e.Message); }
        }

        void PlaceQuestIcon()
        {
            if (questIcon == null || SpeakerBody == null) return;
            questIcon.transform.position = HeadPoint + Vector3.up * 0.42f;
        }

        // Scene 3 is borrowed right at the start of the day, while you are still in the bathroom,
        // so cooking, the table and the icon are ready without a wait later
        void TickEarlyImport()
        {
            if (importAt < 0f || Time.time < importAt) return;
            importAt = -1f;
            ImportCooking(null);
        }

        // ---------------------------------------------------------------- her own day
        // Meal times: she announces it, walks to the stove and cooks (icon "!"); a while later she calls you to the table,
        // and if you never come the food goes cold. Bedtime: she says good night, walks to the bed and sleeps there
        // (icon "z") until you sleep too, wake her, or her own morning comes.

        enum MitaActivity { Free, ToStove, Cooking, MealReady, ToBed, Sleeping, ToSofa, OnSofa }
        MitaActivity activity;
        float activityUntil;
        int walkId;
        GameObject walkTarget;
        RoutineLines RL => Personas.Routine(Pn.Id);

        static readonly Vector3 BedsideMita = new Vector3(9.8f, 0f, -3.9f);

        bool Bedtime()
        {
            float h = S.Minute / 60f;
            int bed = (Pn.NightHour + 1) % 24, wake = Pn.WakeHour;
            return bed <= wake ? h >= bed && h < wake : h >= bed || h < wake;
        }

        void SetIconText(string t)
        {
            if (questIcon == null) return;
            var tx = questIcon.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (tx != null) tx.text = t;
        }

        // called with the other checks while she is free and nothing else is going on
        void TickRoutine(bool calm)
        {
            float now = Time.time;
            switch (activity)
            {
                case MitaActivity.Free:
                    if (!calm) return;
                    if (MealTime() && S.LastMeal != MealKey() && mealStartedFor != MealKey() && stovePos != Vector3.zero)
                    {
                        mealStartedFor = MealKey();
                        StopFollow(false);
                        activity = MitaActivity.ToStove;
                        Play(RL.PrepMeal, () => WalkMita(stovePos, OnReachedStove), true);
                    }
                    else if (Bedtime() && sleptFor != S.Day + ":" + (S.Minute < 12 * 60 ? "m" : "e"))
                    {
                        sleptFor = S.Day + ":" + (S.Minute < 12 * 60 ? "m" : "e");
                        StopFollow(false);
                        activity = MitaActivity.ToBed;
                        Play(RL.GoingToBed, () => WalkMita(BedsideMita, OnReachedBed), true);
                    }
                    else TryGoToSofa();
                    break;
                case MitaActivity.ToStove:
                case MitaActivity.ToBed:
                case MitaActivity.ToSofa:
                {
                    if (!walking) break;   // still saying what she's going to do
                    var d = mitaBody != null ? mitaBody.position - walkTo : Vector3.zero; d.y = 0;
                    bool there = d.magnitude < 0.45f;
                    if (DevRunner.Active && now >= nextWalkLog)
                    {
                        nextWalkLog = now + 1f;
                        var nma = mita != null ? mita.nma : null;
                        Plugin.Log.LogInfo($"[day] walking: {d.magnitude:0.0} m left, at {mitaBody.position}, agent {(nma != null ? $"on={nma.enabled} path={nma.hasPath} pending={nma.pathPending} stopped={nma.isStopped} v={nma.velocity.magnitude:0.00} rem={nma.remainingDistance:0.0}" : "-")}");
                    }
                    // stuck on the way (a closed door, you in the doorway): she gets there anyway
                    if (!there && now > activityUntil) Plugin.Log.LogInfo($"[day] walk took too long ({d.magnitude:0.0} m left), placing her");
                    if (there || now > activityUntil)
                    {
                        walking = false;
                        if (activity == MitaActivity.ToStove) OnReachedStove();
                        else if (activity == MitaActivity.ToSofa) OnReachedSofa();
                        else OnReachedBed();
                    }
                    break;
                }
                case MitaActivity.Cooking:
                    if (!MealTime() || S.LastMeal == MealKey()) { EndRoutine(); break; }
                    if (now > activityUntil && calm)
                    {
                        activity = MitaActivity.MealReady;
                        SetIconText("!");
                        Play(RL.MealReady);
                    }
                    break;
                case MitaActivity.MealReady:
                    if (S.LastMeal == MealKey()) { EndRoutine(); break; }
                    if (!MealTime() && calm)
                    {
                        // you never came: the food went cold
                        S.LastMeal = mealStartedFor;
                        EndRoutine();
                        LoseAff(2, "the food went cold");
                        Play(RL.MealCold);
                    }
                    break;
                case MitaActivity.OnSofa:
                    // enough of that (or it's meal time / bedtime): she gets up
                    if (calm && (now > activityUntil || (MealTime() && S.LastMeal != MealKey()) || Bedtime())) EndRoutine();
                    break;
                case MitaActivity.Sleeping:
                    if (!Bedtime() && calm)
                    {
                        // her own morning (you stayed up): she gets up
                        EndRoutine();
                        if (PlayerNearMita(6f, 360f)) Play(MorningLines());
                    }
                    break;
            }
        }

        string mealStartedFor, sleptFor;

        Vector3 walkTo;
        bool walking;
        float nextWalkLog;

        // the story's own walk (animation, speed, turning); arrival is judged here by distance, because its finish
        // event fired straight away, before the agent had a path (she jumped to the stove / the bed)
        [HideFromIl2Cpp] void WalkMita(Vector3 to, Action arrived)
        {
            if (mita == null || mitaBody == null) { arrived(); return; }
            if (walkTarget == null) walkTarget = new GameObject("PeaceWalkTarget");
            walkTarget.transform.position = to;
            walkTo = to;
            walkId++;
            activityUntil = Time.time + 40f;
            HoldAddon(true);
            if (mita.nma != null) { mita.nma.enabled = true; try { mita.nma.isStopped = false; } catch { } }
            try { mita.AiWalkToTarget(walkTarget.transform); walking = true; Plugin.Log.LogInfo($"[day] Mita walks to {to} ({activity})"); }
            catch (Exception e) { Plugin.Log.LogWarning("[day] walk: " + e.Message); arrived(); }
        }

        void OnReachedStove()
        {
            if (activity != MitaActivity.ToStove) return;
            walkId++;
            activity = MitaActivity.Cooking;
            activityUntil = Time.time + 40f;
            if (!MitaCooksAtStove(false)) PoseMitaUpright(stovePos, stoveFront);
            nextStoveClip = 0f;
            SetIconText("!");
            ShowQuestIcon(true);
            Plugin.Log.LogInfo("[day] cooking on her own");
        }

        void OnReachedBed()
        {
            if (activity != MitaActivity.ToBed) return;
            walkId++;
            // lying down is a jump: hide it behind a short fade if you could see it
            bool seen = PlayerNearMita(7f, 70f) && fadeTarget <= 0f;
            Action lie = () =>
            {
                activity = MitaActivity.Sleeping;
                try { mitaLook?.Activation(false); } catch { }
                LieInBed();
                SleepFace(true);
                SetIconText("z");
                ShowQuestIcon(true);
                Plugin.Log.LogInfo("[day] asleep in bed");
            };
            if (!seen) { lie(); return; }
            fadeTarget = 1f;
            afterFade = () => { ClearLeftoverText(); lie(); fadeTarget = 0f; };
        }

        // back to her free day (the routine was interrupted, finished, or you took her somewhere)
        void EndRoutine()
        {
            StopFollow(false);
            var was = activity;
            walkId++;
            walking = false;
            activity = MitaActivity.Free;
            ShowQuestIcon(false);
            if (was == MitaActivity.Cooking || was == MitaActivity.MealReady) LeaveStove();
            if (was == MitaActivity.OnSofa) { UpFromSofa(); return; }
            if (was == MitaActivity.Cooking || was == MitaActivity.MealReady || was == MitaActivity.Sleeping)
            {
                if (posed && mitaBody != null && pm != null)
                {
                    var at = was == MitaActivity.Sleeping ? BedsideMita : mitaBody.position;
                    if (was == MitaActivity.Sleeping) UpFromBed();
                    ReleasePose(at, pm.transform.position);
                    SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
                    try { mitaLook?.Activation(true); } catch { }
                }
            }
            else if (was == MitaActivity.ToStove || was == MitaActivity.ToBed || was == MitaActivity.ToSofa) HoldAddon(false);
        }

        // she is asleep: sleep too, wake her, or let her be
        void OpenSleepingMenu()
        {
            ShowMenu(Pn.Short, new System.Collections.Generic.List<string> { "같이 자기", "깨우기", "그냥 두기" },
                new System.Collections.Generic.List<Action>
                {
                    () => { activity = MitaActivity.Free; ShowQuestIcon(false); fadeTarget = 1f; afterFade = () => SleepScene(false); },
                    () =>
                    {
                        // she answers from the pillow, then drifts off again
                        Play(RL.Woken, () => SleepFace(true));
                        LoseAff(1, "woken up");
                    },
                    () => { },
                });
        }

        // ---------------------------------------------------------------- dev
        // the numbered event slots of Mita's animations and what each one calls, plus the events inside given clips
        public string DevAnimEvents(string clipNames)
        {
            var sb = new System.Text.StringBuilder();
            if (mitaAnim != null && mitaAnim.eventAnimation != null)
                for (int i = 0; i < mitaAnim.eventAnimation.Count; i++)
                    sb.AppendLine($"slot {i}: {SceneDumper.EventString(mitaAnim.eventAnimation[i]) ?? "-"}");
            foreach (var n in clipNames.Split(','))
            {
                var c = Clip(n.Trim());
                if (c == null) { foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<AnimationClip>())) if (o != null && o.name == n.Trim()) { c = o.Cast<AnimationClip>(); break; } }
                if (c == null) { sb.AppendLine($"clip {n}: not loaded"); continue; }
                sb.Append($"clip {c.name} len={c.length:0.00} loop={c.isLooping} events:");
                foreach (var e in c.events) sb.Append($" [{e.time:0.00} {e.functionName}({e.intParameter},{e.stringParameter})]");
                sb.AppendLine();
            }
            return sb.ToString();
        }
        public void DevIcon(bool on) => ShowQuestIcon(on);
        public void DevRoutine(string what)
        {
            if (what == "meal") { S.LastMeal = null; mealStartedFor = null; S.Minute = 12 * 60; }
            else if (what == "bed") { sleptFor = null; S.Minute = ((Pn.NightHour + 1) % 24) * 60 + 5; }
            else if (what == "wake") { S.Minute = Pn.WakeHour * 60 + 1; }
            else if (what == "ready") activityUntil = 0f;
            Plugin.Log.LogInfo($"[dev] routine {what}: {activity} at {(int)(S.Minute / 60)}:{(int)(S.Minute % 60):00}");
        }
        public void DevMitaGo(Vector3 p)
        {
            if (addon == null) return;
            try { addon.MitaGoPoint(p); Plugin.Log.LogInfo($"[dev] Mita sent to {p}"); }
            catch (Exception e) { Plugin.Log.LogWarning("[dev] MitaGoPoint: " + e.Message); }
        }
        public void DevMitaWalkTo(Transform t)
        {
            if (addon == null || t == null) return;
            try { addon.MitaWalkToPoint(t); Plugin.Log.LogInfo($"[dev] Mita walking to {t.name} {t.position}"); }
            catch (Exception e) { Plugin.Log.LogWarning("[dev] MitaWalkToPoint: " + e.Message); }
        }
    }
}
