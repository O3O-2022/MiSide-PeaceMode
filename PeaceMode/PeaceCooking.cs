using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // "같이 요리하기", borrowed from the story's "Scene 3 - WeTogether" (the same house): its cooking corner
    // (World>Acts>Act Cooking: pan, knife, the carrot to cut, Location3CutCarrot) and the cutting board it uses are
    // moved into the peace scene once (see SceneImport). Scene 3's house stands somewhere else, so everything is
    // carried over by the offset between the two scenes' kitchen cutting boards.
    // Each session gets a fresh copy of the corner from an inactive template; after the carrot is cut the meal scene follows.
    public partial class PeaceDirector
    {
        GameObject cornerTemplate, boardTemplate, cookingAct, cookBoard, homeBoard;
        Location3CutCarrot cutCarrot;
        ObjectInteractive carrotInteractive;
        bool cookingLoading, cooking;
        Vector3 stovePos, stoveFront;     // where Mita cooks when the scene starts
        float nextStoveClip;
        // where Scene 3's Mita cooks (PositionCookingMita, carried over with the corner): her "Mita Cooking" clip
        // fries in that corner's pan from there
        Vector3 cookMitaPos;
        Quaternion cookMitaRot;
        bool cookMitaKnown;
        Transform spatula;
        GameObject spatulaTemplate;   // (a copy of its own: the corner template's one was gone after a session)

        public bool CookingReady => cornerTemplate != null;

        // remember where she stands at the stove at the start of the scene (called from Init)
        void RememberStove()
        {
            if (mitaBody == null) return;
            stovePos = mitaBody.position;
            var f = -mitaBody.forward; f.y = 0;   // her face is on the agent's -Z side
            stoveFront = f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        }

        Action importWaiters;

        // everyone who asked while the one Scene 3 load is under way gets called when it is done (or has failed)
        void ImportDone()
        {
            var w = importWaiters;
            importWaiters = null;
            w?.Invoke();
        }

        [HideFromIl2Cpp] void ImportCooking(Action done)
        {
            if (cornerTemplate != null) { done?.Invoke(); return; }
            importWaiters += done;
            if (cookingLoading) return;
            cookingLoading = true;
            var here = SceneManager.GetActiveScene();
            bool started = SceneImport.Load("Scene 3 - WeTogether", s =>
            {
                cookingLoading = false;
                KeepSceneClips(s);
                Transform act = null, board3 = null;
                foreach (var r in s.GetRootGameObjects())
                {
                    if (act == null) act = Util.FindDeep(r.transform, "Act Cooking");
                    if (board3 == null) board3 = Util.FindDeep(r.transform, "Kitchen CutBoard");
                }
                homeBoard = FindInScene(here, "Kitchen CutBoard")?.gameObject;
                if (act == null || board3 == null || homeBoard == null)
                {
                    Plugin.Log.LogWarning($"[cook] Scene 3 parts missing (corner {act != null}, board {board3 != null}, our board {homeBoard != null})");
                    ImportDone();
                    return;
                }
                // Scene 3 -> peace scene. Scene 3's house stands elsewhere: each borrowed part goes by the furniture it
                // belongs to in both houses (the stove for the cooking corner, the table for the table set). The cutting
                // boards are the last resort: they don't lie in quite the same place (the table set came out 0.8 m off).
                var h = homeBoard.transform;
                var boardTurn = h.rotation * Quaternion.Inverse(board3.rotation);
                var b3pos = board3.position;
                Func<Vector3, Vector3> boardMove = p => h.position + boardTurn * (p - b3pos);
                if (!Anchor(s, here, "Kitchen Table", out var tableMove, out var tableTurn)) { tableMove = boardMove; tableTurn = boardTurn; }
                if (!Anchor(s, here, "Kitchen Stove", out var Move, out var turn)) { Move = boardMove; turn = boardTurn; }
                BorrowMealSet(s, here, tableMove, tableTurn);
                BorrowIcon(s, here);
                BorrowChoices(s, here);
                try { ReadScene3Topics(s, here); } catch (Exception e) { Plugin.Log.LogWarning("[house] Scene 3: " + e); }
                // the story moves its board to the cooking spot when you enter the kitchen
                var tp = board3.GetComponent<Transform_Position>();
                try { tp?.SetPositionAndRotation(0); } catch (Exception e) { Plugin.Log.LogWarning("[cook] board position: " + e.Message); }
                var cookPoint = FindInScene(s, "PositionCookingMita");
                if (cookPoint != null)
                {
                    cookMitaPos = Move(cookPoint.position); cookMitaPos.y = 0f;
                    cookMitaRot = turn * cookPoint.rotation;
                    cookMitaKnown = true;
                }
                int dropped = SceneImport.StripOutside(act.gameObject, s) + SceneImport.StripOutside(board3.gameObject, s);
                act.gameObject.SetActive(false);
                board3.gameObject.SetActive(false);
                foreach (var t in new[] { act, board3 })
                {
                    var p = Move(t.position);
                    var q = turn * t.rotation;
                    SceneImport.Take(t, here);
                    t.SetPositionAndRotation(p, q);
                }
                foreach (var c in board3.GetComponents<Transform_Position>()) Destroy(c);
                var cooking = act.Find("Cooking");
                if (cooking != null) cooking.gameObject.SetActive(true);
                // (Scene 3's own "Mita StartCooking" clips are left alone: played on this scene's Mita they switched her
                // renderers off and emptied her animator)
                // inactive templates: a fresh corner and board for every session
                act.SetParent(templates.transform, true);
                board3.SetParent(templates.transform, true);
                // Sessions are copied from a copy: something still aims at the objects that came out of Scene 3 and
                // destroyed the corner's insides when the first session ended (a second one had no carrot)
                act.name = "PeaceCooking Original";
                board3.name = "PeaceCutBoard Original";
                cornerTemplate = Instantiate(act.gameObject, templates.transform);
                cornerTemplate.name = "PeaceCooking Template";
                var sp = Util.FindDeep(act, "SpatulaInRightHand");
                if (sp != null)
                {
                    spatulaTemplate = Instantiate(sp.gameObject, templates.transform);
                    spatulaTemplate.name = "PeaceSpatula Template";
                    spatulaTemplate.SetActive(false);
                }
                boardTemplate = Instantiate(board3.gameObject, templates.transform);
                boardTemplate.name = "PeaceCutBoard Template";
                var ci = Util.FindDeep(act, "Interactive CarrotCut");
                var pan = Util.FindDeep(act, "Pan");
                var stove = FindInScene(here, "Kitchen Stove");
                Plugin.Log.LogInfo($"[cook] cooking corner borrowed ({dropped} story calls dropped), carrot at {(ci != null ? ci.position.ToString() : "-")}, board at {board3.position}, pan at {(pan != null ? pan.position.ToString() : "-")}, stove {(stove != null ? stove.position.ToString() : "-")}, our board {h.position}, Mita cooks at {(cookMitaKnown ? cookMitaPos + " yaw " + cookMitaRot.eulerAngles.y.ToString("F0") : "-")} (stove spot {stovePos})");
                ImportDone();
            });
            if (!started) { cookingLoading = false; ImportDone(); }
        }

        // the same piece of furniture in both houses: from -> to
        static bool Anchor(Scene from, Scene to, string name, out Func<Vector3, Vector3> move, out Quaternion turn)
        {
            var a = FindInScene(from, name);
            var b = FindInScene(to, name);
            if (a == null || b == null) { move = null; turn = Quaternion.identity; Plugin.Log.LogWarning("[cook] no " + name + " in both houses"); return false; }
            var tt = b.rotation * Quaternion.Inverse(a.rotation);
            var ap = a.position;
            var bp = b.position;
            move = p => bp + tt * (p - ap);
            turn = tt;
            return true;
        }

        static Transform FindInScene(Scene s, string name)
        {
            foreach (var r in s.GetRootGameObjects())
            {
                var t = Util.FindDeep(r.transform, name);
                if (t != null) return t;
            }
            return null;
        }

        // ---------------------------------------------------------------- the session

        bool CanCook() => MealTime() && S.LastMeal != MealKey() && !cooking;

        void DoCook()
        {
            if (!CanCook()) return;
            S.LastMeal = MealKey();   // cooking together is this meal
            EndRoutine();
            cooking = true;
            // the screen goes dark while Scene 3 lends its kitchen corner (a few seconds) and she moves to the stove
            Play((IList<Line>)L.CookInvite ?? new Line[0], () =>
            {
                fadeTarget = 1f;
                afterFade = () => ImportCooking(() =>
                {
                    StartCookingSession();
                    if (!cooking) return;   // no corner: the meal scene has taken over
                    fadeTarget = 0f;
                    afterFade = () => Play((IList<Line>)L.CookAsk ?? new Line[0]);
                });
            });
        }

        void StartCookingSession()
        {
            if (cornerTemplate == null) { Plugin.Log.LogWarning("[cook] no cooking corner, eating right away"); cooking = false; MealSceneAtTable(); return; }
            ClearCookingProps();
            var here = SceneManager.GetActiveScene();
            cookingAct = Instantiate(cornerTemplate);
            cookingAct.name = "PeaceCooking";
            SceneManager.MoveGameObjectToScene(cookingAct, here);
            cookingAct.SetActive(true);

            cookBoard = Instantiate(boardTemplate);
            cookBoard.name = "PeaceCutBoard";
            SceneManager.MoveGameObjectToScene(cookBoard, here);
            cookBoard.SetActive(true);
            if (homeBoard != null) homeBoard.SetActive(false);
            cutCarrot = cookingAct.GetComponentInChildren<Location3CutCarrot>(true);
            var ci = Util.FindDeep(cookingAct.transform, "Interactive CarrotCut");
            carrotInteractive = ci != null ? ci.GetComponent<ObjectInteractive>() : null;
            if (carrotInteractive != null) carrotInteractive.objectInteractive = cookBoard;   // (the template's board otherwise)
            if (cutCarrot != null) cutCarrot.eventFinish.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnCarrotCut)));
            try { carrotInteractive?.Activation(true); } catch (Exception e) { Plugin.Log.LogWarning("[cook] carrot: " + e.Message); }
            // Mita at the stove, cooking
            ShowRoom("Kitchen");
            if (!MitaCooksAtStove(true) && stovePos != Vector3.zero) PoseMitaUpright(stovePos, stoveFront);
            nextStoveClip = 0f;
            carrotStarted = carrotDone = false; carrotAllFellAt = notCuttingSince = -1f;
            ClearLeftoverText();
            Plugin.Log.LogInfo($"[cook] session: carrot {(carrotInteractive != null)} cut {(cutCarrot != null)}");
        }

        // Scene 3's cooking at the stove: where that Mita stood, "Mita Cooking" (frying) with the spatula in her hand.
        // The session's own corner already has a spatula lying there: hidden while she holds hers.
        bool MitaCooksAtStove(bool session)
        {
            var c = Clip("Mita Cooking");
            if (!cookMitaKnown || c == null || mitaBody == null || spatulaTemplate == null) return false;
            PoseMitaUpright(cookMitaPos, cookMitaRot * Vector3.back);   // (her face is on the agent's -Z side)
            uprightFront = Vector3.zero;   // stand exactly as the story's Mita did
            posePos = cookMitaPos; poseRot = cookMitaRot;
            mitaBody.SetPositionAndRotation(posePos, poseRot);
            HoldClip(c);
            DropSpatula();
            spatula = PutInHand(spatulaTemplate.transform);
            if (session && spatula != null && cookingAct != null)
            {
                var own = Util.FindDeep(cookingAct.transform, "SpatulaInRightHand");
                if (own != null) own.gameObject.SetActive(false);
            }
            try { mitaLook?.Activation(false); } catch { }
            Plugin.Log.LogInfo($"[cook] Mita cooks at {cookMitaPos}, spatula {(spatula != null)}");
            return true;
        }

        void DropSpatula()
        {
            if (spatula != null) Destroy(spatula.gameObject);
            spatula = null;
        }

        // done at the stove: spatula away, her own idle
        void LeaveStove()
        {
            if (spatula == null && (heldClip == null || heldClip.name != "Mita Cooking")) return;
            DropSpatula();
            ReleaseClip();
            try { mitaLook?.Activation(true); } catch { }
        }

        // while you cut, she keeps cooking at the stove (called from KeepPose)
        void KeepCooking()
        {
            if (!cooking || !posed || heldClip != null || Time.time < nextStoveClip || mitaAnim == null) return;
            var c = Clip("Mita Something");
            if (c == null) return;
            nextStoveClip = Time.time + Mathf.Max(2f, c.length);
            try { mitaAnim.AnimationClipSimpleNext(c); } catch { }
        }

        bool carrotStarted, carrotDone;
        float carrotAllFellAt = -1f, notCuttingSince = -1f;

        // watches the cutting in case the story's own end (eventFinish) never comes: every piece cut, or the minigame gone
        void TickCooking()
        {
            if (!cooking || carrotDone || cookingAct == null) return;
            bool cutting = pm != null && pm.animationRun && carrotInteractive != null && pm.animationPosition == carrotInteractive.transform;
            if (cutting) { carrotStarted = true; notCuttingSince = -1f; }
            if (!carrotStarted) return;
            bool gone = cutCarrot == null;
            bool allFell = !gone && cutCarrot.partsCarrot != null && cutCarrot.indexCarrotFall >= cutCarrot.partsCarrot.Length;
            if (allFell && carrotAllFellAt < 0) carrotAllFellAt = Time.time;
            if (!cutting && notCuttingSince < 0) notCuttingSince = Time.time;
            bool end = gone || (allFell && Time.time - carrotAllFellAt > 1.5f) || (!cutting && Time.time - notCuttingSince > 1.5f);
            if (!end) return;
            Plugin.Log.LogInfo($"[cook] cutting over by itself (gone={gone} allFell={allFell} cutting={cutting})");
            // what the story's end does first: the player puts the knife down
            var stop = Util.FindDeep(cookingAct.transform, "AnimationStop");
            var anim = stop != null ? stop.GetComponent<ObjectAnimationPlayer>() : null;
            if (cutting) { try { if (anim != null) anim.AnimationPlay(); else pm.AnimationStopFull(); } catch { try { pm.AnimationStopFull(); } catch { } } }
            OnCarrotCut();
        }

        void OnCarrotCut()
        {
            if (carrotDone) return;
            carrotDone = true;
            Did("cook");
            Plugin.Log.LogInfo("[cook] carrot cut");
            AddAff(2);
            // the carrot is done: she turns to you, then you eat together
            Play((IList<Line>)L.CookDone ?? new Line[0], () =>
            {
                cooking = false;
                fadeTarget = 1f;
                afterFade = () => { ClearCookingProps(); MealSceneAtTable(); };
            });
        }

        void StopCooking()
        {
            if (!cooking) return;
            cooking = false;
            ClearCookingProps();
            if (posed && mitaBody != null && pm != null) ReleasePose(mitaBody.position, pm.transform.position);
        }

        void ClearCookingProps()
        {
            LeaveStove();
            if (cookingAct != null) Destroy(cookingAct);
            if (cookBoard != null) Destroy(cookBoard);
            cookingAct = null; cookBoard = null; cutCarrot = null; carrotInteractive = null;
            if (homeBoard != null && !homeBoard.activeSelf) homeBoard.SetActive(true);
        }

        // ---------------------------------------------------------------- dev

        public void DevImportCooking() => ImportCooking(null);
        public void DevCook() { S.LastMeal = null; S.Minute = 12 * 60; DoCook(); }
        public string DevCookState()
        {
            if (cutCarrot == null) return $"no session (template {cornerTemplate != null}, cooking {cooking})";
            return $"cut play={cutCarrot.play} keyDown={cutCarrot.keyDown} index={cutCarrot.indexTimeCut} fall={cutCarrot.indexCarrotFall} auto={cutCarrot.autoCut} " +
                   $"animPlayer={(cutCarrot.animPlayer != null ? cutCarrot.animPlayer.name : "-")} state='{cutCarrot.stateName}' " +
                   $"interactive={(carrotInteractive != null ? carrotInteractive.gameObject.activeInHierarchy + "/" + carrotInteractive.enabled + "/" + carrotInteractive.active : "-")}";
        }
        public void DevCut() { try { cutCarrot?.Cut(); } catch (Exception e) { Plugin.Log.LogWarning("[dev] cut: " + e.Message); } }
        public void DevCutFinish() { try { cutCarrot?.CutFinish(); } catch (Exception e) { Plugin.Log.LogWarning("[dev] cut finish: " + e.Message); } }
        public void DevCutAuto() { if (cutCarrot != null) cutCarrot.autoCut = true; }
    }
}
