using System;
using System.Collections.Generic;
using Coffee.UIEffects;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiSidePeaceMode
{
    // one row of a menu
    public class MenuItem
    {
        public string Label;          // as written in Korean (translated when shown)
        public string Hint;           // small text on the right (already translated), or the reason it is locked
        public bool Enabled = true;
        public bool Sub;              // opens another menu (an arrow on the right)
        public bool Back;             // "← 뒤로" / "닫기"
        public bool Now;              // fits the moment (cooking, meal time, her on the sofa...): drawn with an accent
        public string Badge;          // something new inside ("NEW", a count): a small red pill
        public MenuItem(string label) { Label = label; }
    }

    // The menu panel, the status card, toasts, the photo viewer and the info card (2026-10 redesign).
    // Everything is drawn with sprites made here (a rounded rectangle, a circle), so nothing depends on the scene.
    public static class PeaceSkin
    {
        public static readonly Color Panel = new Color(0.12f, 0.05f, 0.16f, 0.88f);
        public static readonly Color PanelEdge = new Color(1f, 0.55f, 0.82f, 0.35f);
        public static readonly Color Row = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color RowNow = new Color(1f, 0.72f, 0.30f, 0.16f);
        public static readonly Color PinkA = new Color(0.96f, 0.38f, 0.68f, 1f);
        public static readonly Color PinkB = new Color(0.62f, 0.26f, 0.88f, 1f);
        public static readonly Color Pink = new Color(1f, 0.55f, 0.80f, 1f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.36f, 1f);
        public static readonly Color Soft = new Color(1f, 0.86f, 0.94f, 0.80f);
        public static readonly Color Dim = new Color(1f, 1f, 1f, 0.38f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.35f);
        public static readonly Color Badge = new Color(1f, 0.27f, 0.42f, 1f);
        // the canvas height in its own units (1080 at UI scale 1; less when the UI is drawn bigger)
        public static float ScreenH = 1080f;

        static Sprite round, circle, glow;

        // a 9-sliced rounded rectangle (white, tinted by the Image's colour)
        public static Sprite Round
        {
            get
            {
                if (round == null) round = MakeRounded(64, 22, 1.4f, false);
                return round;
            }
        }

        // a soft shadow (rounded, blurred edge)
        public static Sprite Glow
        {
            get
            {
                if (glow == null) glow = MakeRounded(96, 40, 26f, true);
                return glow;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    int n = 64;
                    var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    float r = n / 2f - 1f;
                    var px = new Color32[n * n];
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float d = Mathf.Sqrt((x + 0.5f - n / 2f) * (x + 0.5f - n / 2f) + (y + 0.5f - n / 2f) * (y + 0.5f - n / 2f));
                            byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255);
                            px[y * n + x] = new Color32(255, 255, 255, a);
                        }
                    tex.SetPixels32(px);
                    tex.Apply();
                    circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                    circle.hideFlags = HideFlags.HideAndDontSave;
                    tex.hideFlags = HideFlags.HideAndDontSave;
                }
                return circle;
            }
        }

        static Sprite MakeRounded(int n, int radius, float soft, bool shadow)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // distance outside the rounded rectangle inset by radius
                    float cx = Mathf.Clamp(x + 0.5f, radius, n - radius), cy = Mathf.Clamp(y + 0.5f, radius, n - radius);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy)) - (shadow ? radius - soft : radius);
                    float a = shadow ? Mathf.Clamp01(1f - d / soft) : Mathf.Clamp01(0.5f - d / soft);
                    if (shadow) a = a * a;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            int b = radius + 2;
            var sp = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sp.hideFlags = HideFlags.HideAndDontSave;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return sp;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        public static Image Box(Transform parent, string name, Color c, Sprite sprite = null)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Round;
            img.type = Image.Type.Sliced;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, Font font, int size, Color c, TextAnchor anchor, bool bold = false)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = c;
            t.alignment = anchor;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.18f, 0.02f, 0.16f, 0.7f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        public static void Fill(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        public static void At(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static void Gradient(Graphic g, Color a, Color b)
        {
            try
            {
                var gr = g.gameObject.AddComponent<UIGradient>();
                gr.direction = UIGradient.Direction.Horizontal;
                gr.color1 = a; gr.color2 = b;
            }
            catch { g.color = Color.Lerp(a, b, 0.5f); }
        }

        public static float Width(Text t, string s)
        {
            try
            {
                var gen = t.cachedTextGeneratorForLayout;
                var set = t.GetGenerationSettings(Vector2.zero);
                return gen.GetPreferredWidth(s, set) / t.pixelsPerUnit;
            }
            catch { return s.Length * t.fontSize * 0.6f; }
        }
    }

    public partial class PeaceUIPanel
    {
        // ---------------------------------------------------------------- the menu panel

        readonly Transform root;
        readonly Func<Font> font;
        GameObject panel;
        CanvasGroup panelGroup;
        RectTransform panelRt;
        readonly List<(Image bg, Image badge, Text num, Text label, Text hint, Text arrow, Button btn, RectTransform rt)> rows = new List<(Image, Image, Text, Text, Text, Text, Button, RectTransform)>();
        List<MenuItem> items;
        Action<int> onChoose;
        Action onBack;
        int selected = -1;
        float openT;
        Vector3 lastMouse;
        // the small movements: rows coming in one after another, the picked row leaning forward, a refused one
        // shaking its head, now and then a shine over the header (see PeaceFx)
        float openedAt, nextShine, shineAt = -1f;
        int enterDir;                 // 1 = a sub-menu (in from the right), -1 = back (from the left), 0 = fresh, 2 = the same menu again (no entrance)
        readonly List<CanvasGroup> rowGroups = new List<CanvasGroup>();
        readonly List<float> selEase = new List<float>();
        readonly List<(RectTransform rt, float at)> badgePops = new List<(RectTransform, float)>();
        int shakeRow = -1;
        float shakeAt;
        RectTransform shine;
        float panelW;
        GameObject lastGhost;
        const float RowStagger = 0.022f, RowIn = 0.2f;

        const float PanelY = -40f;   // (a little low: the game's own quest note sits at the top left)
        public bool Open => panel != null;
        public int Selected => selected;

        public PeaceUIPanel(Transform root, Func<Font> font) { this.root = root; this.font = font; }

        public void Show(string title, string crumb, List<MenuItem> list, Action<int> choose, Action back, int dir = 0)
        {
            Close(dir == 2 ? 0 : dir);
            if (dir == 2 && lastGhost != null) { PeaceFx.Kill(lastGhost); lastGhost = null; }   // (the same menu: no fading copy behind it)
            enterDir = dir;
            items = list;
            onChoose = choose;
            onBack = back;
            var f = font();
            const float W = 600f, headH = 108f, pad = 20f, footH = 50f;
            // rows get shorter when there are many, so the panel stays within the screen (1080 tall)
            float gap = list.Count > 11 ? 5f : 8f;
            float rowH = Mathf.Clamp((PeaceSkin.ScreenH - 180f - headH - pad - footH) / Mathf.Max(1, list.Count) - gap, 46f, 60f);
            float h = headH + pad * 0.5f + (rowH + gap) * list.Count - gap + pad * 0.5f + footH;

            panel = new GameObject("PeacePanel");
            panel.transform.SetParent(root, false);
            panelRt = panel.AddComponent<RectTransform>();
            PeaceSkin.At(panelRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, PanelY), new Vector2(W, h));
            panelGroup = panel.AddComponent<CanvasGroup>();
            panelGroup.alpha = dir == 2 ? 1f : 0f;
            panelW = W;

            // shadow, body, a thin pink edge
            var sh = PeaceSkin.Box(panel.transform, "Shadow", PeaceSkin.Shadow, PeaceSkin.Glow);
            PeaceSkin.Fill(sh.rectTransform, -26, -34, -26, -18);
            var body = PeaceSkin.Box(panel.transform, "Body", PeaceSkin.Panel);
            PeaceSkin.Fill(body.rectTransform);
            var edge = PeaceSkin.Box(panel.transform, "Edge", PeaceSkin.PanelEdge);
            PeaceSkin.Fill(edge.rectTransform, -1.5f, -1.5f, -1.5f, -1.5f);
            edge.transform.SetAsFirstSibling(); sh.transform.SetAsFirstSibling();

            // header: a gradient band with the crumb and the title
            var band = PeaceSkin.Box(panel.transform, "Header", Color.white);
            PeaceSkin.At(band.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(W, headH));
            PeaceSkin.Gradient(band, PeaceSkin.PinkA, PeaceSkin.PinkB);
            var crumbT = PeaceSkin.Label(band.transform, "Crumb", f, 20, new Color(1, 1, 1, 0.82f), TextAnchor.UpperLeft);
            PeaceSkin.Fill(crumbT.rectTransform, 28, 0, 28, 16);
            crumbT.text = Loc.Display(crumb ?? "");
            var titleT = PeaceSkin.Label(band.transform, "Title", f, 38, Color.white, TextAnchor.LowerLeft, true);
            PeaceSkin.Fill(titleT.rectTransform, 28, 14, 28, 0);
            titleT.text = Loc.Display(title ?? "");
            // a soft shine that crosses the header now and then
            band.gameObject.AddComponent<RectMask2D>();
            var gl = PeaceSkin.Box(band.transform, "Shine", new Color(1, 1, 1, 0f), PeaceSkin.Glow);
            shine = gl.rectTransform;
            PeaceSkin.At(shine, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, 0), new Vector2(90, headH * 1.9f));
            shine.localRotation = Quaternion.Euler(0, 0, -18f);
            shineAt = -1f;
            nextShine = Time.unscaledTime + (dir == 2 ? 6f : 0.45f);

            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                int idx = i;
                float y = -(headH + pad * 0.5f + (rowH + gap) * i);
                var bg = PeaceSkin.Box(panel.transform, "Row" + i, it.Now ? PeaceSkin.RowNow : PeaceSkin.Row);
                var rt = bg.rectTransform;
                PeaceSkin.At(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(pad, y), new Vector2(W - pad * 2, rowH));
                bg.raycastTarget = true;
                var rg = bg.gameObject.AddComponent<CanvasGroup>();
                rg.alpha = dir == 2 ? 1f : 0f;
                rowGroups.Add(rg);
                selEase.Add(0f);
                var btn = bg.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => { selected = idx; Pick(idx); })));

                var badge = PeaceSkin.Box(bg.transform, "Badge", new Color(1, 1, 1, 0.14f), PeaceSkin.Circle);
                badge.type = Image.Type.Simple;
                PeaceSkin.At(badge.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(36, 36));
                var num = PeaceSkin.Label(badge.transform, "Num", f, 20, Color.white, TextAnchor.MiddleCenter, true);
                PeaceSkin.Fill(num.rectTransform);
                num.text = it.Back ? "Esc" : i < 9 ? (i + 1).ToString() : i == 9 ? "0" : "";
                if (it.Back) num.fontSize = 13;

                var label = PeaceSkin.Label(bg.transform, "Label", f, 28, it.Enabled ? Color.white : PeaceSkin.Dim, TextAnchor.MiddleLeft);
                PeaceSkin.Fill(label.rectTransform, 64, 0, 40, 0);
                string text = Loc.T(it.Label);
                if (it.Back) text = "← " + text;
                label.text = Loc.Display(text);

                float right = it.Sub ? 40 : 18;
                if (!string.IsNullOrEmpty(it.Badge))
                {
                    var bt = Loc.Display(it.Badge);
                    float bw = 22f + bt.Length * 11f;
                    var pill = PeaceSkin.Box(bg.transform, "Badge", PeaceSkin.Badge, PeaceSkin.Round);
                    PeaceSkin.At(pill.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-right, 0), new Vector2(bw, 26));
                    var bl = PeaceSkin.Label(pill.transform, "T", f, 15, Color.white, TextAnchor.MiddleCenter, true);
                    PeaceSkin.Fill(bl.rectTransform);
                    bl.text = bt;
                    right += bw + 8f;
                    if (dir != 2) badgePops.Add((pill.rectTransform, Time.unscaledTime + i * RowStagger + RowIn + 0.05f));
                }
                var hint = PeaceSkin.Label(bg.transform, "Hint", f, 20, it.Enabled ? (it.Now ? PeaceSkin.Gold : PeaceSkin.Soft) : PeaceSkin.Dim, TextAnchor.MiddleRight);
                PeaceSkin.Fill(hint.rectTransform, 64, 0, right, 0);
                hint.text = Loc.Display(it.Hint ?? "");

                var arrow = PeaceSkin.Label(bg.transform, "Arrow", f, 18, it.Enabled ? new Color(1, 1, 1, 0.7f) : PeaceSkin.Dim, TextAnchor.MiddleRight);
                PeaceSkin.Fill(arrow.rectTransform, 0, 0, 16, 0);
                arrow.text = it.Sub ? "▶" : "";
                rows.Add((bg, badge, num, label, hint, arrow, btn, rt));
            }

            var foot = PeaceSkin.Label(panel.transform, "Footer", f, 19, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter);
            PeaceSkin.At(foot.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(W - 40, footH - 12));
            foot.text = Loc.Display(Loc.T(PeacePad.InUse ? "↕ 선택   ·   Ⓐ 확인   ·   Ⓑ 뒤로" : "↑↓ 선택   ·   Enter 확인   ·   Esc 뒤로"));

            selected = -1;
            for (int i = 0; i < list.Count; i++) if (list[i].Enabled && !list[i].Back) { selected = i; break; }
            openT = dir == 2 ? 1f : 0f;
            openedAt = Time.unscaledTime;
            if (dir == 2) for (int i = 0; i < selEase.Count; i++) selEase[i] = i == selected ? 1f : 0f;
            lastMouse = Input.mousePosition;
            Paint();
        }

        void Paint()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                bool sel = i == selected;
                var it = items[i];
                var g = r.bg.GetComponent<UIGradient>();
                if (sel && it.Enabled)
                {
                    if (g == null) PeaceSkin.Gradient(r.bg, PeaceSkin.PinkA, PeaceSkin.PinkB);
                    else g.enabled = true;
                    r.bg.color = Color.white;
                    r.badge.color = Color.white;
                    r.num.color = PeaceSkin.PinkA;
                    r.hint.color = Color.white;
                }
                else
                {
                    if (g != null) g.enabled = false;
                    r.bg.color = sel ? new Color(1, 1, 1, 0.12f) : it.Now ? PeaceSkin.RowNow : PeaceSkin.Row;
                    r.badge.color = it.Now && it.Enabled ? PeaceSkin.Gold : new Color(1, 1, 1, it.Enabled ? 0.14f : 0.08f);
                    r.num.color = it.Now && it.Enabled ? new Color(0.25f, 0.1f, 0.05f, 1f) : (it.Enabled ? Color.white : PeaceSkin.Dim);
                    r.hint.color = it.Enabled ? (it.Now ? PeaceSkin.Gold : PeaceSkin.Soft) : PeaceSkin.Dim;
                }
            }
        }

        public void Move(int d)
        {
            if (items == null || items.Count == 0) return;
            int n = items.Count, i = selected;
            for (int k = 0; k < n; k++)
            {
                i = (i + d + n) % n;
                if (items[i].Enabled) { selected = i; break; }
            }
            PeaceSfx.Play(PeaceSfx.Move, 0.14f, UnityEngine.Random.Range(0.97f, 1.04f));
            Paint();
        }

        // the shown number (1..9, 0 for the tenth) -> the row
        public void PickNumber(int i) { if (items != null && i >= 0 && i < items.Count && !items[i].Back) Pick(i); }
        public void PickSelected() { if (selected >= 0) Pick(selected); }
        public void PickAny(int i) => Pick(i);
        public void Select(int i)
        {
            if (items == null || i < 0 || i >= items.Count || !items[i].Enabled) return;
            selected = i;
            if (enterDir == 2) for (int k = 0; k < selEase.Count; k++) selEase[k] = k == i ? 1f : 0f;   // (the same menu again: no glide)
            Paint();
        }

        public event Action<string> Refused;

        void Pick(int i)
        {
            if (items == null || i < 0 || i >= items.Count) return;
            var it = items[i];
            if (!it.Enabled) { shakeRow = i; shakeAt = Time.unscaledTime; PeaceSfx.Play(PeaceSfx.Refuse, 0.26f, 0.9f); Refused?.Invoke(it.Hint); return; }
            if (it.Back) { Back(); return; }
            PeaceSfx.Play(PeaceSfx.Pick, 0.3f);
            var cb = onChoose;
            Close();
            cb?.Invoke(i);
        }

        public void Back()
        {
            if (panel != null) PeaceSfx.Play(PeaceSfx.Back, 0.24f);
            var b = onBack;
            Close(-1);
            b?.Invoke();
        }

        // closing: the panel fades away drifting the way the menus go (deeper: to the left; back: to the right)
        public void Close() => Close(0);
        void Close(int dir)
        {
            if (panel != null) lastGhost = PeaceFx.FadeAway(panel, 0.16f, new Vector2(dir < 0 ? 26f : -26f, 0f));
            panel = null; panelGroup = null; shine = null;
            rows.Clear(); rowGroups.Clear(); selEase.Clear(); badgePops.Clear();
            shakeRow = -1;
            items = null; onChoose = null; onBack = null;
        }

        public void Tick(float dt)
        {
            if (panel == null) return;
            float now = Time.unscaledTime;
            if (openT < 1f)
            {
                openT = Mathf.Min(1f, openT + Time.unscaledDeltaTime / 0.2f);
                float e = PeaceFx.Out(openT);
                panelGroup.alpha = e;
                float from = enterDir == 1 ? 34f : enterDir == -1 ? -34f : -28f;
                panelRt.anchoredPosition = new Vector2(110 + from * (1 - e), PanelY);
            }
            // rows: in one after another; the picked one eases forward (its words a little to the right, a touch bigger)
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                float p = enterDir == 2 ? 1f : (now - openedAt - i * RowStagger) / RowIn;
                float e = PeaceFx.Out(p);
                rowGroups[i].alpha = e;
                float target = i == selected && items[i].Enabled ? 1f : 0f;
                selEase[i] = Mathf.MoveTowards(selEase[i], target, Time.unscaledDeltaTime / 0.12f);
                float s = PeaceFx.Smooth(selEase[i]);
                float x = 20f + (1f - e) * (enterDir == -1 ? -14f : 14f);
                if (i == shakeRow)
                {
                    float q = (now - shakeAt) / 0.32f;
                    if (q >= 1f) shakeRow = -1;
                    else x += Mathf.Sin(q * Mathf.PI * 6f) * 7f * (1f - q);
                }
                r.rt.anchoredPosition = new Vector2(x, r.rt.anchoredPosition.y);
                r.rt.localScale = Vector3.one * (1f + 0.012f * s);
                var lr = r.label.rectTransform;
                lr.offsetMin = new Vector2(64f + 6f * s, lr.offsetMin.y);
                // (a sub-menu's arrow leans the way it goes while the row is picked)
                if (r.arrow.text.Length > 0)
                {
                    var ar = r.arrow.rectTransform;
                    float nudge = s * (2f + 2f * Mathf.Sin(now * 5f));
                    ar.offsetMax = new Vector2(-16f + nudge, ar.offsetMax.y);
                    ar.offsetMin = new Vector2(nudge, ar.offsetMin.y);
                }
            }
            // a new thing's badge: a little pop once its row is in
            for (int i = badgePops.Count - 1; i >= 0; i--)
                if (now >= badgePops[i].at) { PeaceFx.Pulse(badgePops[i].rt, 0.22f, 0.42f); badgePops.RemoveAt(i); }
            // the shine: across the header in 1 s, every 7 s or so (and once as the menu comes in)
            if (shine != null)
            {
                if (shineAt < 0f && now >= nextShine) shineAt = now;
                if (shineAt >= 0f)
                {
                    float q = (now - shineAt) / 1.0f;
                    if (q >= 1f) { shineAt = -1f; nextShine = now + UnityEngine.Random.Range(6f, 9f); shine.GetComponent<Image>().color = new Color(1, 1, 1, 0f); }
                    else
                    {
                        shine.anchoredPosition = new Vector2(Mathf.Lerp(-80f, panelW + 80f, PeaceFx.Smooth(q)), 0f);
                        shine.GetComponent<Image>().color = new Color(1, 1, 1, 0.16f * Mathf.Sin(q * Mathf.PI));
                    }
                }
            }
            // the mouse picks the row under it once it moves
            var m = Input.mousePosition;
            if ((m - lastMouse).sqrMagnitude > 4f)
            {
                lastMouse = m;
                for (int i = 0; i < rows.Count; i++)
                    if (items[i].Enabled && RectTransformUtility.RectangleContainsScreenPoint(rows[i].rt, m, null))
                    {
                        if (selected != i) { selected = i; PeaceSfx.Play(PeaceSfx.Move, 0.1f, UnityEngine.Random.Range(0.97f, 1.04f)); Paint(); }
                        break;
                    }
            }
        }
    }
}
