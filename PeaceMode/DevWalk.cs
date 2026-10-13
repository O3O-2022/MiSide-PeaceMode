using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace MiSidePeaceMode
{
    // Dev-only: walk the player like a real player would (physics body, pushes doors, crosses the room triggers)
    // along a NavMesh path. Driven from PlayerMove.FixedUpdate so it runs after the game's own movement code.
    // Patched only when DevRunner is active.
    public static class DevWalk
    {
        static readonly Queue<Vector3> points = new Queue<Vector3>();
        static Vector3 goal;
        static float until, stuckSince, bestDist;
        public static bool Active => points.Count > 0;
        public const float Speed = 2.2f;
        const int AllAreas = -1;   // NavMesh.AllAreas (a constant the interop assembly leaves out)

        public static void Apply(Harmony h)
        {
            var mi = AccessTools.Method(typeof(PlayerMove), "FixedUpdate");
            if (mi == null) Plugin.Log.LogWarning("[dev] walk: no PlayerMove.FixedUpdate");
            else h.Patch(mi, postfix: new HarmonyMethod(typeof(DevWalk), nameof(AfterFixedUpdate)));
            // "mouse hold|release": the left button as the game reads it (for minigames driven by a held click)
            foreach (var (name, post) in new[] { ("GetMouseButton", nameof(MouseHeld)), ("GetMouseButtonDown", nameof(MouseDown)), ("GetMouseButtonUp", nameof(MouseUp)) })
            {
                try
                {
                    var m = AccessTools.Method(typeof(Input), name, new[] { typeof(int) });
                    if (m != null) h.Patch(m, postfix: new HarmonyMethod(typeof(DevWalk), post));
                }
                catch (System.Exception e) { Plugin.Log.LogWarning($"[dev] mouse patch {name}: {e.Message}"); }
            }
            // the same button asked as KeyCode.Mouse0
            foreach (var (name, post) in new[] { ("GetKey", nameof(KeyHeld)), ("GetKeyDown", nameof(KeyDown)), ("GetKeyUp", nameof(KeyUp)) })
            {
                try
                {
                    var m = AccessTools.Method(typeof(Input), name, new[] { typeof(KeyCode) });
                    if (m != null) h.Patch(m, postfix: new HarmonyMethod(typeof(DevWalk), post));
                }
                catch (System.Exception e) { Plugin.Log.LogWarning($"[dev] key patch {name}: {e.Message}"); }
            }
        }

        static void KeyHeld(KeyCode key, ref bool __result) { if (key == KeyCode.Mouse0 && Hold) __result = true; }
        static void KeyDown(KeyCode key, ref bool __result) { if (key == KeyCode.Mouse0 && Hold && Time.frameCount - holdFrame <= 1) __result = true; }
        static void KeyUp(KeyCode key, ref bool __result) { if (key == KeyCode.Mouse0 && !Hold && Time.frameCount - releaseFrame <= 1) __result = true; }

        // "mouse clicks <n> [seconds apart]": a train of separate clicks
        static int clicksLeft;
        static float clickEvery = 0.35f, nextClickToggle;
        public static void Clicks(int n, float every) { clicksLeft = n; clickEvery = every; nextClickToggle = 0f; }
        public static void TickClicks()
        {
            if (clicksLeft <= 0 || Time.realtimeSinceStartup < nextClickToggle) return;
            nextClickToggle = Time.realtimeSinceStartup + clickEvery / 2f;
            if (!Hold) SetHold(true);
            else { SetHold(false); clicksLeft--; }
        }

        public static bool Hold;
        static int holdFrame = -10, releaseFrame = -10;
        public static void SetHold(bool on)
        {
            if (on == Hold) return;
            Hold = on;
            if (on) holdFrame = Time.frameCount; else releaseFrame = Time.frameCount;
            Plugin.Log.LogInfo("[dev] mouse " + (on ? "held" : "released"));
        }
        static void MouseHeld(int button, ref bool __result) { if (button == 0 && Hold) __result = true; }
        static void MouseDown(int button, ref bool __result) { if (button == 0 && Hold && Time.frameCount - holdFrame <= 1) __result = true; }
        static void MouseUp(int button, ref bool __result) { if (button == 0 && !Hold && Time.frameCount - releaseFrame <= 1) __result = true; }

        // walk to (x, z); returns false if there is no player
        public static bool Start(Vector3 target, float timeout)
        {
            var pm = Object.FindObjectOfType<PlayerMove>();
            if (pm == null) return false;
            points.Clear();
            goal = target;
            var from = pm.transform.position;
            var path = new NavMeshPath();
            bool ok = NavMesh.SamplePosition(from, out var a, 2f, AllAreas)
                      & NavMesh.SamplePosition(target, out var b, 2f, AllAreas)
                      && NavMesh.CalculatePath(a.position, b.position, AllAreas, path)
                      && path.corners != null && path.corners.Length > 1;
            if (ok) for (int i = 1; i < path.corners.Length; i++) points.Enqueue(path.corners[i]);
            else points.Enqueue(target);
            until = Time.realtimeSinceStartup + timeout;
            stuckSince = Time.realtimeSinceStartup; bestDist = float.MaxValue;
            var sb = new System.Text.StringBuilder();
            foreach (var p in points) sb.Append($"({p.x:0.0},{p.z:0.0}) ");
            Plugin.Log.LogInfo($"[dev] walk from ({from.x:0.0},{from.z:0.0}) to ({target.x:0.0},{target.z:0.0}): {(ok ? "navmesh " + path.status : "straight")} {sb}");
            return true;
        }

        static void Stop(string why)
        {
            points.Clear();
            var pm = Object.FindObjectOfType<PlayerMove>();
            if (pm != null && pm.rb != null) pm.rb.velocity = new Vector3(0, pm.rb.velocity.y, 0);
            Plugin.Log.LogInfo($"[dev] walk {why} at {(pm != null ? pm.transform.position.ToString() : "-")} (goal {goal})");
        }

        static void AfterFixedUpdate(PlayerMove __instance)
        {
            if (points.Count == 0) return;
            var pm = __instance;
            if (pm.rb == null) { Stop("failed: no rigidbody"); return; }
            float now = Time.realtimeSinceStartup;
            if (now > until) { Stop("timed out"); return; }
            var pos = pm.transform.position;
            var next = points.Peek();
            var to = next - pos; to.y = 0;
            if (to.magnitude < 0.3f)
            {
                points.Dequeue();
                if (points.Count == 0) { Stop("arrived"); return; }
                next = points.Peek(); to = next - pos; to.y = 0;
                bestDist = float.MaxValue; stuckSince = now;
            }
            // no progress for 4 s: something (a closed door, the player being held) blocks the way
            float d = to.magnitude;
            if (d < bestDist - 0.05f) { bestDist = d; stuckSince = now; }
            else if (now - stuckSince > 4f) { Stop($"stuck (dontMove={pm.dontMove} fixHead={(pm.fixHead != null ? pm.fixHead.name : "-")} anim={pm.animationRun})"); return; }
            var dir = to.normalized;
            pm.rb.velocity = new Vector3(dir.x * Speed, pm.rb.velocity.y, dir.z * Speed);
            // look where we go, like a player would
            pm.rotX = Mathf.MoveTowardsAngle(pm.rotX, Quaternion.LookRotation(dir).eulerAngles.y, 360f * Time.fixedDeltaTime);
        }
    }
}
