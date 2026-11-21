using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiSidePeaceMode
{
    // The UI's small movements (2026-10): eases, a scale "bump" on something that changed, and panels that fade away
    // instead of vanishing. All short (0.1-0.5 s) and small (a few pixels, a few percent): alive, not busy.
    public static class PeaceFx
    {
        public static float Out(float x) { x = Mathf.Clamp01(x); float k = 1f - x; return 1f - k * k * k; }
        public static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        // a gentle overshoot (about 4 %) at the end
        public static float Back(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.1f, c3 = c1 + 1f;
            float k = x - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }

        class Ghost { public GameObject go; public CanvasGroup cg; public RectTransform rt; public Vector2 from, delta; public float t0, dur, scale0, scale1; }
        class Bump { public RectTransform rt; public float t0, dur, amp; }

        static readonly List<Ghost> ghosts = new List<Ghost>();
        static readonly List<Bump> bumps = new List<Bump>();

        // something closing: it fades (and drifts / shrinks a little) for dur, then goes
        public static GameObject FadeAway(GameObject go, float dur, Vector2 drift, float shrink = 0f)
        {
            if (go == null) return null;
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            var rt = go.GetComponent<RectTransform>();
            go.name += " (closing)";
            ghosts.Add(new Ghost { go = go, cg = cg, rt = rt, from = rt != null ? rt.anchoredPosition : Vector2.zero, delta = drift, t0 = Time.unscaledTime, dur = dur,
                                   scale0 = go.transform.localScale.x, scale1 = go.transform.localScale.x * (1f - shrink) });
            return go;
        }

        public static void Kill(GameObject go)
        {
            if (go == null) return;
            for (int i = ghosts.Count - 1; i >= 0; i--) if (ghosts[i].go == go) ghosts.RemoveAt(i);
            UnityEngine.Object.Destroy(go);
        }

        // a quick swell and settle of its scale (a number that changed, a heart that went up)
        public static void Pulse(RectTransform rt, float amp = 0.12f, float dur = 0.4f)
        {
            if (rt == null) return;
            bumps.RemoveAll(b => b.rt == rt);
            bumps.Add(new Bump { rt = rt, t0 = Time.unscaledTime, dur = dur, amp = amp });
        }

        public static void Tick()
        {
            float now = Time.unscaledTime;
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                var g = ghosts[i];
                float p = (now - g.t0) / g.dur;
                if (g.go == null || p >= 1f) { if (g.go != null) UnityEngine.Object.Destroy(g.go); ghosts.RemoveAt(i); continue; }
                float e = Out(p);
                g.cg.alpha = Mathf.Min(g.cg.alpha, 1f - e);
                if (g.rt != null) g.rt.anchoredPosition = g.from + g.delta * e;
                g.go.transform.localScale = Vector3.one * Mathf.Lerp(g.scale0, g.scale1, e);
            }
            for (int i = bumps.Count - 1; i >= 0; i--)
            {
                var b = bumps[i];
                float p = (now - b.t0) / b.dur;
                if (b.rt == null || p >= 1f) { if (b.rt != null) b.rt.localScale = Vector3.one; bumps.RemoveAt(i); continue; }
                b.rt.localScale = Vector3.one * (1f + b.amp * Mathf.Sin(p * Mathf.PI) * (1f - p * 0.35f));
            }
        }

        public static void Clear()
        {
            foreach (var g in ghosts) if (g.go != null) UnityEngine.Object.Destroy(g.go);
            ghosts.Clear();
            bumps.Clear();
        }
    }
}
