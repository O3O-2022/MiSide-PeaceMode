using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // Her eyes on you. While she is free and you are around, the story's own look (Character_Look, which the free
    // roaming leaves switched off) keeps her head and eyes on you, and she turns to face you when you walk round her.
    // When you have been off in another part of the house for a while, she now and then comes over to where you are.
    // Looked in the face for long, she gets shy as in the story (MitaLifeEmotion): the shy face and a glance away; keep
    // staring and she says something about it (FourthWall's Stare lines).
    public partial class PeaceDirector
    {
        float nextAttention, nextTurn, farSince = -1f, nextVisit = -1f, shyUntil, nextShy, attentiveUntil;
        bool attentionOn, shyNow, visiting, attentiveHold;
        GameObject lookAwayTarget;

        // (everything that stages her itself - scenes, games, walks, cooking, bed - steers her look itself)
        bool AttentionFree => freeMode && !posed && !hugging && !walking && activity == MitaActivity.Free && hideStage == 0
                              && !cardGame && chamStage == 0 && mitaBody != null && mita != null && pm != null && pm.mainCamera != null && mitaLook != null;

        void TickAttention()
        {
            if (!AttentionFree) { attentionOn = false; shyNow = false; farSince = -1f; attentiveHold = false; return; }
            if (shyNow && Time.time > shyUntil) EndShy();
            if (Time.time < nextAttention) return;
            nextAttention = Time.time + 0.3f;

            var me = pm.transform.position;
            var d = me - mitaBody.position; d.y = 0;
            float dist = d.magnitude;
            // (cold, she only looks at you when you are right in front of her)
            bool near = dist < (Cold ? 2.5f : 6f) && CanSee(HeadPoint, pm.mainCamera.position);

            if (near && !shyNow)
            {
                if (!attentionOn || mitaLook.lookObject == null)
                {
                    try { mitaLook.Activation(true); mitaLook.LookOnPlayer(); } catch { }
                    attentionOn = true;
                }
                // you went round her: she turns to you (not while she is walking somewhere)
                bool still = mita.nma == null || !mita.nma.enabled || mita.nma.velocity.magnitude < 0.05f;
                var face = FaceForward; face.y = 0;
                if (still && !following && dist > 0.6f && Time.time > nextTurn && Vector3.Angle(face, d) > 55f)
                {
                    nextTurn = Time.time + 2.5f;
                    try { mitaLook.RotateOnTargetExact(pm.transform); } catch { }
                }
            }
            else if (!near && attentionOn && !busy)
            {
                attentionOn = false;
                try { mitaLook.LookRandom(); } catch { }
            }

            // you came up to her and look at her: she stops her wandering and stays with you (until you look away a while)
            var toHer = HeadPoint - pm.mainCamera.position;
            bool lookedAt = !Cold && near && dist < 3.5f && Vector3.Angle(pm.mainCamera.forward, toHer) < 25f;
            if (lookedAt)
            {
                attentiveUntil = Time.time + 8f;
                if (!attentiveHold && !following)
                {
                    attentiveHold = true;
                    HoldAddon(true);
                    try { mita.AiShraplyStop(); mitaLook.LookOnPlayerAndRotate(); } catch { }
                }
            }
            else if (attentiveHold && Time.time > attentiveUntil && !following && !busy)
            {
                attentiveHold = false;
                HoldAddon(false);
            }

            // you have been elsewhere for a while: she comes to see what you are doing
            if (visiting)
            {
                if (!following) visiting = false;
                else if (!followWalking && dist < 2.3f) EndVisit();
                return;
            }
            if (dist < 7f || busy || following || Cold || DevCalm) { farSince = -1f; return; }
            if (farSince < 0f) farSince = Time.time;
            if (nextVisit < 0f) nextVisit = Time.time + (Warm ? Random.Range(30f, 70f) : Random.Range(60f, 120f));
            if (Time.time - farSince > 45f && Time.time > nextVisit && request == null)
            {
                nextVisit = Time.time + (Warm ? Random.Range(90f, 180f) : Random.Range(150f, 300f));
                visiting = true;
                following = true;   // (the follow walk, until she is by your side: see TickFollow)
                followWalking = false;
                nextFollow = 0f;
                Plugin.Log.LogInfo($"[attention] she comes over ({dist:0.0} m away for {Time.time - farSince:0} s)");
            }
        }

        void EndVisit()
        {
            visiting = false;
            following = false;
            farSince = -1f;
            try { mita.AiShraplyStop(); mitaLook.Activation(true); mitaLook.LookOnPlayerAndRotate(); } catch { }
            // she stays by you a while before wandering off again
            After(25f, () => { if (!following && !posed && !walking && activity == MitaActivity.Free) HoldAddon(false); });
            // and often she came with something in mind
            if (Random.value < 0.6f) After(1.2f, () => { if (!busy && !MenuOpen && freeMode && activity == MitaActivity.Free) TryPropose(true); });
        }

        // nothing between your eyes and her head (walls, doors); her own body and yours don't count
        bool CanSee(Vector3 head, Vector3 eye)
        {
            if (!Physics.Linecast(eye, head, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            var t = hit.transform;
            return t != null && (t.IsChildOf(mita.transform) || (mitaBody != null && t.IsChildOf(mitaBody)) || t.IsChildOf(pm.transform));
        }

        // stared at in the face: the shy face, a glance down and to the side, then her eyes come back to you
        void BeShy()
        {
            if (!AttentionFree || shyNow) return;
            shyNow = true;
            shyUntil = Time.time + Random.Range(2.2f, 3.2f);
            nextShy = Time.time + Random.Range(9f, 14f);
            if (Cold) Contempt(true);   // (cold: a look of contempt, not shy)
            else try { mita.FaceEmotion("shy"); } catch { }
            if (lookAwayTarget == null) lookAwayTarget = new GameObject("PeaceLookAway");
            var toYou = pm.mainCamera.position - HeadPoint; toYou.y = 0;
            var side = Vector3.Cross(Vector3.up, toYou.normalized) * (Random.value < 0.5f ? -1f : 1f);
            // (a glance: down and a little aside - far enough and her whole body would turn away)
            lookAwayTarget.transform.position = HeadPoint + side * 0.55f + toYou.normalized * 1.0f + Vector3.down * 0.6f;
            try { mitaLook.LookOnObject(lookAwayTarget.transform); } catch { }
            Plugin.Log.LogInfo("[attention] shy");
        }

        void EndShy()
        {
            shyNow = false;
            attentionOn = false;   // (her eyes come back on the next look check)
            Contempt(false);
            try { mita.FaceEmotion(Pn.IdleEmo ?? "smile"); } catch { }
        }
    }
}
