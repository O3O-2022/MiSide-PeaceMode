using System;
using UnityEngine;
using UnityEngine.AI;

namespace MiSidePeaceMode
{
    // Keeping her where she can stand. Reports: "she ends up in odd places" - half inside a desk after a scene,
    // stuck off the walkable floor after a staged scene put her somewhere the house's NavMesh doesn't reach, her body
    // left a step away from where her agent thinks she is after a borrowed clip. Two parts:
    //  - every spot she is put down on to stand (after a scene, by the window, at the recorder...) goes through
    //    ClearSpot: onto the NavMesh and a little away from its edges (the edges are walls and furniture);
    //  - in free roam a watchdog puts her back on the floor when her agent has lost it, and puts her body back over
    //    her feet when a clip has left it off.
    public partial class PeaceDirector
    {
        const float SpotMargin = 0.18f;   // from a NavMesh edge (the mesh is already shrunk by her radius)

        // the nearest place she can stand at p, kept clear of the mesh's edges
        // the nearest walkable point at the height of `p` (the floor she stands on): the nearest point in 3D was
        // sometimes the top of the coffee table or the sofa's seat - after the lap she stood on the table, 62 cm up
        static bool FloorPoint(Vector3 p, float search, out Vector3 at)
        {
            at = p;
            if (NavMesh.SamplePosition(p, out var hit, search, -1) && Mathf.Abs(hit.position.y - p.y) < 0.25f) { at = hit.position; return true; }
            float best = float.MaxValue;
            bool found = false;
            for (float r = 0.15f; r <= search + 0.01f; r += 0.15f)
            {
                for (int k = 0; k < 16; k++)
                {
                    float a = k * Mathf.PI / 8f;
                    var q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    if (!NavMesh.SamplePosition(q, out var h, 0.2f, -1) || Mathf.Abs(h.position.y - p.y) >= 0.25f) continue;
                    float d = (h.position - p).sqrMagnitude;
                    if (d < best) { best = d; at = h.position; found = true; }
                }
                if (found) return true;   // (the ring nearest to her wins)
            }
            return false;
        }

        static Vector3 ClearSpot(Vector3 p, float search = 1.5f)
        {
            if (!FloorPoint(p, search, out var at) && !FloorPoint(p, 4f, out at)) return p;
            if (NavMesh.FindClosestEdge(at, out var edge, -1) && edge.distance < SpotMargin)
            {
                var away = edge.normal; away.y = 0;
                if (away.sqrMagnitude < 0.0001f) { away = at - edge.position; away.y = 0; }
                if (away.sqrMagnitude > 0.0001f)
                {
                    var pushed = at + away.normalized * (SpotMargin - edge.distance + 0.02f);
                    if (NavMesh.SamplePosition(pushed, out var h2, 0.4f, -1) && Mathf.Abs(h2.position.y - at.y) < 0.25f) at = h2.position;
                }
            }
            return at;
        }

        float safetyErrorAt;
        float nextSafety, offMeshSince = -1f, hipsOffSince = -1f, agentOffSince = -1f;

        bool SafetyHands => posed || hugging || lying || cardGame || GameRunning || photoStage != 0 || cooking
                            || activity == MitaActivity.Sleeping || activity == MitaActivity.OnSofa || activity == MitaActivity.Cooking || activity == MitaActivity.MealReady
                            || (pm != null && pm.animationRun) || fadeTarget > 0f || afterFade != null || !freeMode;

        // free roam: once a second
        void TickMitaSafety()
        {
            if (!ready || mita == null || mitaBody == null || Time.time < nextSafety) return;
            nextSafety = Time.time + 1f;
            if (SafetyHands) { offMeshSince = hipsOffSince = agentOffSince = -1f; return; }
            var nma = mita.nma;
            if (nma == null) return;
            var root = mitaBody.position;

            // her agent switched off with nobody holding her (a scene that ended without giving her back);
            // not while she stands still on purpose: for you looking at her, a menu open, her talking
            // (turning the roaming back on then left her in a T-pose in front of the menu, 2026-10-23)
            bool stillOnPurpose = attentiveHold || MenuOpen || pendingMenu || busy;
            if (!nma.enabled && stillOnPurpose) { agentOffSince = -1f; return; }
            if (!nma.enabled)
            {
                if (agentOffSince < 0f) agentOffSince = Time.time;
                if (Time.time - agentOffSince > 3f)
                {
                    var at = ClearSpot(root);
                    Plugin.Log.LogWarning($"[safety] her agent was off in free roam: back on at {at} (was {root})");
                    nma.enabled = true;
                    try { nma.Warp(at); nma.isStopped = false; } catch { }
                    addonHeld = true; HoldAddon(false);
                    agentOffSince = -1f;
                }
                return;
            }
            agentOffSince = -1f;

            // off the floor she can walk on (in a wall, in furniture, out of the house)
            bool lost = false;
            try { lost = !nma.isOnNavMesh; } catch { }
            float off = 0f;
            if (NavMesh.SamplePosition(root, out var hit, 4f, -1))
            {
                var d = hit.position - root; off = new Vector2(d.x, d.z).magnitude;
                if (Mathf.Abs(d.y) > 0.35f) off = Mathf.Max(off, Mathf.Abs(d.y));
            }
            else lost = true;
            if (lost || off > 0.35f)
            {
                if (offMeshSince < 0f) offMeshSince = Time.time;
                if (Time.time - offMeshSince > 1.5f)
                {
                    var at = ClearSpot(root, 4f);
                    Plugin.Log.LogWarning($"[safety] she was off the floor ({(lost ? "agent lost it" : off.ToString("0.00") + " m")}) at {root}: put on {at}");
                    mitaBody.position = at;
                    try { nma.Warp(at); nma.isStopped = false; } catch { }
                    offMeshSince = -1f;
                }
            }
            else offMeshSince = -1f;

            // her body a step away from her feet: a borrowed clip that carried her (gestures are kept planted while
            // they play; this is for whatever got left over)
            var hips = Util.FindDeep(mitaBody, "Hips");
            if (hips != null && !planting && Time.time > gestureUntil + 2f)
            {
                var o = hips.position - root; o.y = 0;
                bool walking = false;
                try { walking = nma.velocity.sqrMagnitude > 0.04f; } catch { }
                if (o.magnitude > 0.6f && !walking)
                {
                    if (hipsOffSince < 0f) hipsOffSince = Time.time;
                    if (Time.time - hipsOffSince > 4f)
                    {
                        Plugin.Log.LogWarning($"[safety] her body was {o.magnitude:0.00} m off her feet ({DevAnimNow().Split('|')[0].Trim()}): back to her idle");
                        ReIdle();
                        hipsOffSince = -1f;
                    }
                }
                else hipsOffSince = -1f;
            }
        }
    }
}
