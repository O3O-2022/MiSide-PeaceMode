using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace MiSidePeaceMode
{
    // Our own animations (2026-10). The borrowed story clips were made for their own scenes: the "Talk" ones point at
    // things that aren't here (Talk 1-3, IdleTalk hold an arm out at nothing), TakeHand / IdleHand walk her off her spot,
    // Yawns and Look Lamp start sitting. A Unity clip can't be made at runtime for this rig (generic, SetCurve is editor
    // only), so these are keyframes on her bones, laid over whatever the animator gives (her idle) after it ran:
    //   Turn: a bone turned about an axis of her body (x = her right, y = up, z = where she faces), in degrees;
    //   Aim:  a bone pointed along a direction of her body (an arm: from its joint towards the next joint).
    // A motion fades in and out over her own pose, so it starts and ends wherever she is. Names start with '@' and go
    // wherever a clip name goes (a line's clip, a gesture).
    public class Motion
    {
        public string Name;
        public float Length, FadeIn = 0.25f, FadeOut = 0.35f;
        public bool Hold;                 // a pose: stays (at its last keys) until something else plays or EndMotion
        public readonly List<MotionTrack> Tracks = new List<MotionTrack>();
    }

    public class MotionTrack
    {
        public string Bone, Toward;       // Toward: the next joint (Aim)
        public bool Aim;
        public bool Straight;             // a finger joint in line with the one before it (opened out)
        public string Palm;               // Spread: turned about the palm's normal ("R"/"L" hand), Angles in degrees
        public Vector3 Axis;              // Turn: her body axis
        public readonly List<(float t, float a)> Angles = new List<(float, float)>();
        public readonly List<(float t, Vector3 d)> Dirs = new List<(float, Vector3)>();

        // eased between keys (each key a gentle stop: what a hand or a head does)
        public float AngleAt(float t)
        {
            if (Angles.Count == 0) return 0f;
            if (t <= Angles[0].t) return Angles[0].a;
            for (int i = 1; i < Angles.Count; i++)
                if (t <= Angles[i].t)
                {
                    var (t0, a0) = Angles[i - 1]; var (t1, a1) = Angles[i];
                    return Mathf.Lerp(a0, a1, Ease((t - t0) / Mathf.Max(0.0001f, t1 - t0)));
                }
            return Angles[Angles.Count - 1].a;
        }

        public Vector3 DirAt(float t)
        {
            if (Dirs.Count == 0) return Vector3.forward;
            if (t <= Dirs[0].t) return Dirs[0].d;
            for (int i = 1; i < Dirs.Count; i++)
                if (t <= Dirs[i].t)
                {
                    var (t0, d0) = Dirs[i - 1]; var (t1, d1) = Dirs[i];
                    return Vector3.Slerp(d0, d1, Ease((t - t0) / Mathf.Max(0.0001f, t1 - t0)));
                }
            return Dirs[Dirs.Count - 1].d;
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    }

    public static class Motions
    {
        static readonly Vector3 R = Vector3.right, U = Vector3.up, F = Vector3.forward;

        static MotionTrack Turn(Motion m, string bone, Vector3 axis, params float[] tv)
        {
            var tr = new MotionTrack { Bone = bone, Axis = axis };
            for (int i = 0; i + 1 < tv.Length; i += 2) tr.Angles.Add((tv[i], tv[i + 1]));
            m.Tracks.Add(tr);
            return tr;
        }

        static MotionTrack Aim(Motion m, string bone, string toward, params (float t, Vector3 d)[] keys)
        {
            var tr = new MotionTrack { Bone = bone, Toward = toward, Aim = true };
            foreach (var k in keys) tr.Dirs.Add((k.t, k.d.normalized));
            m.Tracks.Add(tr);
            return tr;
        }

        static void Straighten(Motion m, params string[] bones) { foreach (var b in bones) m.Tracks.Add(new MotionTrack { Bone = b, Straight = true }); }

        static void Spread(Motion m, string bone, string side, float deg)
        {
            var tr = new MotionTrack { Bone = bone, Palm = side };
            tr.Angles.Add((0f, deg));
            m.Tracks.Add(tr);
        }

        // a back-and-forth: n swings of amp degrees, each half of period long, from t0
        static float[] Swing(float t0, int n, float period, float amp, float rest = 0f)
        {
            var l = new List<float> { t0, rest };
            for (int i = 0; i < n; i++) { l.Add(t0 + period * (i + 0.5f)); l.Add(rest + (i % 2 == 0 ? amp : -amp)); }
            l.Add(t0 + period * (n + 0.5f)); l.Add(rest);
            return l.ToArray();
        }

        // her right arm (the one nearer your left), up to the shoulder, the forearm up: a hand by her face
        static readonly Vector3 ArmUpR = new Vector3(0.75f, -0.05f, 0.35f), ForearmUpR = new Vector3(0.12f, 1f, 0.22f);

        public static readonly Dictionary<string, Motion> All = Build();

        static Dictionary<string, Motion> Build()
        {
            var d = new Dictionary<string, Motion>();
            void Add(Motion m) => d[m.Name] = m;

            // yes: two small nods
            var nod = new Motion { Name = "@nod", Length = 1.0f, FadeIn = 0.1f, FadeOut = 0.2f };
            Turn(nod, "Head", R, 0f, 0f, 0.18f, 13f, 0.36f, 2f, 0.56f, 11f, 0.8f, 0f);
            Turn(nod, "Neck1", R, 0f, 0f, 0.2f, 5f, 0.4f, 0f, 0.6f, 4f, 0.82f, 0f);
            Add(nod);

            // no: a small shake of the head
            var shake = new Motion { Name = "@shake", Length = 1.2f, FadeIn = 0.1f, FadeOut = 0.2f };
            Turn(shake, "Head", U, Swing(0.05f, 4, 0.22f, 13f));
            Turn(shake, "Head", R, 0f, 0f, 0.3f, 5f, 1.0f, 5f, 1.15f, 0f);
            Add(shake);

            // a question: her head on one side
            var tilt = new Motion { Name = "@tilt", Length = 1.9f, FadeIn = 0.3f, FadeOut = 0.45f };
            Turn(tilt, "Head", F, 0f, 0f, 0.35f, 14f, 1.5f, 12f);
            Turn(tilt, "Neck1", F, 0f, 0f, 0.35f, 5f, 1.5f, 4f);
            Turn(tilt, "Head", U, 0f, 0f, 0.35f, -6f, 1.5f, -5f);
            Add(tilt);

            // shy: head down and away, a little sway of her shoulders (her hands stay together in front)
            var shy = new Motion { Name = "@shy", Length = 2.8f, FadeIn = 0.4f, FadeOut = 0.5f };
            Turn(shy, "Head", R, 0f, 0f, 0.4f, 14f, 2.3f, 12f);
            Turn(shy, "Head", U, 0f, 0f, 0.5f, 16f, 1.4f, 10f, 2.3f, 14f);
            Turn(shy, "Head", F, 0f, 0f, 0.5f, -7f, 2.3f, -6f);
            Turn(shy, "Chest", U, Swing(0.3f, 3, 0.8f, 5f));
            Turn(shy, "Spine", R, 0f, 0f, 0.5f, 4f, 2.3f, 4f);
            Add(shy);

            // a glance away (caught looking at you)
            var away = new Motion { Name = "@lookaway", Length = 1.6f, FadeIn = 0.15f, FadeOut = 0.4f };
            Turn(away, "Head", U, 0f, 0f, 0.25f, -28f, 1.1f, -24f);
            Turn(away, "Head", R, 0f, 0f, 0.25f, 9f, 1.1f, 8f);
            Turn(away, "Neck1", U, 0f, 0f, 0.3f, -8f, 1.1f, -7f);
            Add(away);

            // hello: a hand up by her face, waving
            var wave = new Motion { Name = "@wave", Length = 2.2f, FadeIn = 0.3f, FadeOut = 0.4f };
            Aim(wave, "Right arm", "Right elbow", (0f, ArmUpR), (2.2f, ArmUpR));
            Aim(wave, "Right elbow", "Right wrist", (0f, ForearmUpR), (2.2f, ForearmUpR));
            Turn(wave, "Right elbow", F, Swing(0.35f, 5, 0.3f, 16f));
            Turn(wave, "Head", F, 0f, 0f, 0.4f, -8f, 1.8f, -8f);
            Add(wave);

            // explaining: an open hand towards you, a little beat with the words (instead of an arm held out at nothing)
            var explain = new Motion { Name = "@explain", Length = 2.4f, FadeIn = 0.35f, FadeOut = 0.45f };
            Aim(explain, "Right arm", "Right elbow", (0f, new Vector3(0.35f, -0.85f, 0.35f)), (2.4f, new Vector3(0.35f, -0.85f, 0.35f)));
            Aim(explain, "Right elbow", "Right wrist",
                (0f, new Vector3(0.2f, -0.05f, 1f)), (0.6f, new Vector3(0.25f, 0.18f, 1f)), (1.0f, new Vector3(0.2f, -0.05f, 1f)),
                (1.5f, new Vector3(0.3f, 0.15f, 1f)), (2.4f, new Vector3(0.25f, 0f, 1f)));
            Turn(explain, "Head", F, 0f, 0f, 0.5f, 6f, 1.6f, -4f, 2.2f, 0f);
            Turn(explain, "Head", R, 0f, 0f, 0.6f, 5f, 1.0f, 0f, 1.5f, 4f, 2.0f, 0f);
            Add(explain);

            // yay: both hands up in little fists, a happy wiggle
            var cheer = new Motion { Name = "@cheer", Length = 1.8f, FadeIn = 0.2f, FadeOut = 0.4f };
            Aim(cheer, "Right arm", "Right elbow", (0f, new Vector3(0.55f, -0.6f, 0.45f)), (1.8f, new Vector3(0.55f, -0.6f, 0.45f)));
            Aim(cheer, "Right elbow", "Right wrist", (0f, new Vector3(-0.05f, 1f, 0.45f)), (1.8f, new Vector3(-0.05f, 1f, 0.45f)));
            Aim(cheer, "Left arm", "Left elbow", (0f, new Vector3(-0.55f, -0.6f, 0.45f)), (1.8f, new Vector3(-0.55f, -0.6f, 0.45f)));
            Aim(cheer, "Left elbow", "Left wrist", (0f, new Vector3(0.05f, 1f, 0.45f)), (1.8f, new Vector3(0.05f, 1f, 0.45f)));
            Turn(cheer, "Chest", F, Swing(0.2f, 4, 0.28f, 5f));
            Turn(cheer, "Head", F, Swing(0.2f, 4, 0.28f, -7f));
            Add(cheer);

            // ---- poses for the camera (held until the photo is taken) ----
            Motion Pose(string name) { var m = new Motion { Name = name, Length = 1f, FadeIn = 0.4f, FadeOut = 0.3f, Hold = true }; Add(m); return m; }

            var hi = Pose("@pose_hi");
            Aim(hi, "Right arm", "Right elbow", (0f, ArmUpR));
            Aim(hi, "Right elbow", "Right wrist", (0f, ForearmUpR));
            Turn(hi, "Head", F, 0f, -8f);

            // a V by her face: the hand up near her cheek, two fingers out
            var v = Pose("@pose_v");
            Aim(v, "Right arm", "Right elbow", (0f, new Vector3(0.5f, -0.55f, 0.5f)));
            Aim(v, "Right elbow", "Right wrist", (0f, new Vector3(-0.25f, 1f, 0.3f)));
            Straighten(v, "IndexFinger1_R", "IndexFinger2_R", "IndexFinger3_R", "MiddleFinger1_R", "MiddleFinger2_R", "MiddleFinger3_R");
            Spread(v, "IndexFinger1_R", "R", 11f);
            Spread(v, "MiddleFinger1_R", "R", -7f);
            Turn(v, "Head", F, 0f, -10f);
            Turn(v, "Head", U, 0f, 6f);

            var hands = Pose("@pose_cheer");
            Aim(hands, "Right arm", "Right elbow", (0f, new Vector3(0.55f, -0.6f, 0.45f)));
            Aim(hands, "Right elbow", "Right wrist", (0f, new Vector3(-0.05f, 1f, 0.45f)));
            Aim(hands, "Left arm", "Left elbow", (0f, new Vector3(-0.55f, -0.6f, 0.45f)));
            Aim(hands, "Left elbow", "Left wrist", (0f, new Vector3(0.05f, 1f, 0.45f)));
            Turn(hands, "Head", F, 0f, 6f);

            var both = Pose("@pose_both");
            Aim(both, "Right arm", "Right elbow", (0f, ArmUpR));
            Aim(both, "Right elbow", "Right wrist", (0f, ForearmUpR));
            Aim(both, "Left arm", "Left elbow", (0f, new Vector3(-ArmUpR.x, ArmUpR.y, ArmUpR.z)));
            Aim(both, "Left elbow", "Left wrist", (0f, new Vector3(-ForearmUpR.x, ForearmUpR.y, ForearmUpR.z)));

            var chin = Pose("@pose_chin");
            Aim(chin, "Right arm", "Right elbow", (0f, new Vector3(0.25f, -0.8f, 0.4f)));
            Aim(chin, "Right elbow", "Right wrist", (0f, new Vector3(-0.45f, 0.8f, 0.4f)));
            Turn(chin, "Head", F, 0f, -9f);
            Turn(chin, "Head", R, 0f, -4f);

            var ptilt = Pose("@pose_tilt");
            Turn(ptilt, "Head", F, 0f, 15f);
            Turn(ptilt, "Neck1", F, 0f, 5f);

            var pshy = Pose("@pose_shy");
            Turn(pshy, "Head", R, 0f, 13f);
            Turn(pshy, "Head", U, 0f, 12f);
            Turn(pshy, "Head", F, 0f, -6f);
            Turn(pshy, "Spine", R, 0f, 4f);

            return d;
        }
    }

    public partial class PeaceDirector
    {
        Motion motion;
        float motionStart;
        bool motionLookOff;   // her look at you (Character_Look) turned off while one of ours moves her head: it sets the head after us
        readonly Dictionary<string, Transform> motionBones = new Dictionary<string, Transform>();
        Transform motionBonesOf;

        bool CanMove => mitaBody != null && !hugging && !lying && !(posed && !showcasing) && (skin == null || !skin.OwnAnimator)
                        && activity != MitaActivity.Sleeping && activity != MitaActivity.OnSofa;

        // a clip name for her line or gesture: one of ours ('@'), or a borrowed one we'd rather not show here
        string MotionFor(string clip)
        {
            if (clip == null) return null;
            switch (clip)
            {
                case "Mita IdleTalk": case "Mita Talk 1": case "Mita Talk 3": return "@explain";   // (an arm held out at nothing / hunched pointing up)
                case "Mita Talk 2": return "@tilt";                                                   // (a hand behind her head, the other pointing down)
                case "Mita TakeHand": return heldClip == null ? "@shy" : clip;                         // (walks her into you unless she is holding your hand)
                case "Mita IdleHand": return "@cheer";                                                 // (walks off her spot)
                default: return clip;
            }
        }

        bool devKeepPose;   // (a pose started by the dev command stays without the photo menu: stills)
        public void DevMotion(string name)
        {
            if (name == "off") { devKeepPose = false; EndMotion(); return; }
            if (!PlayMotion(name)) Plugin.Log.LogWarning("[dev] motion " + name + ": not played (unknown, or she can't now)");
            else devKeepPose = motion != null && motion.Hold;
        }

        [HideFromIl2Cpp] bool PlayMotion(string name)
        {
            if (name == null || !Motions.All.TryGetValue(name, out var m) || !CanMove) return false;
            // over her plain idle (a gesture clip still playing would fight it)
            gestureToken++;
            ReIdle();
            EndMotion();
            motion = m;
            motionStart = Time.time;
            if (m.Tracks.Exists(tr => tr.Bone == "Head" || tr.Bone == "Neck1" || tr.Bone == "Chest" || tr.Bone == "Spine"))
                try { mitaLook?.Activation(false); motionLookOff = true; } catch { }
            gestureUntil = Time.time + m.Length;
            Plugin.Log.LogInfo($"[anim] motion {m.Name}");
            return true;
        }

        void EndMotion()
        {
            motion = null;
            if (!motionLookOff) return;
            motionLookOff = false;
            try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
        }

        Transform MotionBone(string n)
        {
            if (motionBonesOf != mitaBody) { motionBones.Clear(); motionBonesOf = mitaBody; }
            if (!motionBones.TryGetValue(n, out var t) || t == null) motionBones[n] = t = Util.FindDeep(mitaBody, n);
            return t;
        }

        // LateUpdate: after the animator (and her look at you), before her skin copies the bones
        void TickMotion()
        {
            if (motion == null) return;
            float t = Time.time - motionStart;
            if ((!motion.Hold && t >= motion.Length) || !CanMove) { EndMotion(); return; }
            float w = motion.Hold ? Mathf.Clamp01(t / motion.FadeIn) : Mathf.Min(Mathf.Clamp01(t / motion.FadeIn), Mathf.Clamp01((motion.Length - t) / motion.FadeOut));
            w = w * w * (3f - 2f * w);
            var body = mitaBody.rotation;
            foreach (var tr in motion.Tracks)
            {
                var b = MotionBone(tr.Bone);
                if (b == null) continue;
                if (tr.Straight)
                {
                    // in line with the bone before it (its parent's direction, from the parent's joint to this one)
                    var next = b.childCount > 0 ? b.GetChild(0) : null;
                    var par = b.parent;
                    if (next == null || par == null) continue;
                    var cur = next.position - b.position;
                    var want = b.position - par.position;
                    if (cur.sqrMagnitude < 1e-8f || want.sqrMagnitude < 1e-8f) continue;
                    b.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, want), w) * b.rotation;
                }
                else if (tr.Palm != null)
                {
                    // fanned out (or in) about the palm's normal
                    var wr = MotionBone(tr.Palm == "R" ? "Right wrist" : "Left wrist");
                    var a = MotionBone(tr.Palm == "R" ? "IndexFinger1_R" : "IndexFinger1_L");
                    var l = MotionBone(tr.Palm == "R" ? "LittleFinger1_R" : "LittleFinger1_L");
                    if (wr == null || a == null || l == null) continue;
                    var n = Vector3.Cross(a.position - wr.position, l.position - wr.position);
                    if (n.sqrMagnitude < 1e-10f) continue;
                    b.rotation = Quaternion.AngleAxis(tr.AngleAt(t) * w, n.normalized) * b.rotation;
                }
                else if (tr.Aim)
                {
                    var next = MotionBone(tr.Toward);
                    if (next == null) continue;
                    var cur = next.position - b.position;
                    if (cur.sqrMagnitude < 1e-6f) continue;
                    var want = body * tr.DirAt(t);
                    b.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, want), w) * b.rotation;
                }
                else b.rotation = Quaternion.AngleAxis(tr.AngleAt(t) * w, body * tr.Axis) * b.rotation;
            }
        }
    }
}
