using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiSidePeaceMode
{
    // The status card (top right): her name and the day, the time and the weather, the hearts and the step's name.
    public class PeaceHudCard
    {
        readonly GameObject go;
        readonly RectTransform rt;
        readonly Text name, day, time, hearts;
        readonly Image accent;
        readonly CanvasGroup cg;
        float width = 360f;
        float shown, shownTarget, heartFlashAt = -10f;

        public PeaceHudCard(Transform root, Font f)
        {
            rt = PeaceSkin.Rect(root, "StatusCard");
            go = rt.gameObject;
            PeaceSkin.At(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -26), new Vector2(width, 124));
            cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0f;
            var sh = PeaceSkin.Box(go.transform, "Shadow", PeaceSkin.Shadow, PeaceSkin.Glow);
            PeaceSkin.Fill(sh.rectTransform, -20, -26, -20, -14);
            var body = PeaceSkin.Box(go.transform, "Body", PeaceSkin.Panel);
            PeaceSkin.Fill(body.rectTransform);
            accent = PeaceSkin.Box(go.transform, "Accent", Color.white);
            PeaceSkin.At(accent.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 12), new Vector2(7, 100));
            accent.rectTransform.anchorMax = new Vector2(0, 1);
            accent.rectTransform.offsetMin = new Vector2(10, 12); accent.rectTransform.offsetMax = new Vector2(17, -12);
            PeaceSkin.Gradient(accent, PeaceSkin.PinkA, PeaceSkin.PinkB);

            name = PeaceSkin.Label(go.transform, "Name", f, 30, Color.white, TextAnchor.UpperLeft, true);
            PeaceSkin.Fill(name.rectTransform, 32, 0, 18, 12);
            day = PeaceSkin.Label(go.transform, "Day", f, 20, PeaceSkin.Soft, TextAnchor.UpperRight);
            PeaceSkin.Fill(day.rectTransform, 32, 0, 20, 18);
            time = PeaceSkin.Label(go.transform, "Time", f, 22, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleLeft);
            PeaceSkin.Fill(time.rectTransform, 32, 30, 18, 46);
            hearts = PeaceSkin.Label(go.transform, "Hearts", f, 22, PeaceSkin.Pink, TextAnchor.LowerLeft);
            hearts.rectTransform.pivot = new Vector2(0f, 0f);   // (a pulse swells from its corner, not from the card's middle)
            PeaceSkin.Fill(hearts.rectTransform, 32, 12, 18, 0);
            day.rectTransform.pivot = new Vector2(1f, 1f);
            PeaceSkin.Fill(day.rectTransform, 32, 0, 20, 18);
        }

        public void Set(string nameText, string dayText, string timeText, string heartText, Font f)
        {
            foreach (var t in new[] { name, day, time, hearts }) if (f != null && t.font != f) t.font = f;
            if (name.text != nameText) name.text = Loc.Display(nameText);
            if (day.text != Loc.Display(dayText)) { bool was = day.text.Length > 0; day.text = Loc.Display(dayText); if (was) PeaceFx.Pulse(day.rectTransform, 0.18f, 0.5f); }
            if (time.text != timeText) time.text = Loc.Display(timeText);
            if (hearts.text != Loc.Display(heartText))
            {
                // affection moved a step (or a heart): the hearts swell and flash gold for a moment
                bool was = hearts.text.Length > 0;
                hearts.text = Loc.Display(heartText);
                if (was) { PeaceFx.Pulse(hearts.rectTransform, 0.16f, 0.5f); heartFlashAt = Time.unscaledTime; }
            }
            // grows at once, shrinks only when clearly too wide (the clock must not make it breathe)
            float want = Mathf.Max(PeaceSkin.Width(name, nameText) + PeaceSkin.Width(day, dayText) + 40f, PeaceSkin.Width(time, timeText.Replace('1', '0').Replace('7', '0')),
                                   PeaceSkin.Width(hearts, heartText)) + 56f;
            want = Mathf.Clamp(want, 300f, 900f);
            if (want > width || want < width - 40f) { width = want; rt.sizeDelta = new Vector2(width, rt.sizeDelta.y); }
        }

        // in and out with a short fade (and a little drop from above as it comes)
        public void Show(bool on)
        {
            shownTarget = on ? 1f : 0f;
            if (on && !go.activeSelf) go.SetActive(true);
        }

        public void Tick()
        {
            if (!go.activeSelf) return;
            shown = Mathf.MoveTowards(shown, shownTarget, Time.unscaledDeltaTime / 0.25f);
            float e = PeaceFx.Out(shown);
            cg.alpha = e;
            rt.anchoredPosition = new Vector2(-32f, -26f + 14f * (1f - e));
            if (shown <= 0f && shownTarget <= 0f) go.SetActive(false);
            float q = (Time.unscaledTime - heartFlashAt) / 0.7f;
            hearts.color = q < 1f ? Color.Lerp(PeaceSkin.Gold, PeaceSkin.Pink, PeaceFx.Smooth(q)) : PeaceSkin.Pink;
        }
    }

    // A short note under the status card ("♥ +1", "사진을 저장했어요") that fades away.
    public class PeaceToasts
    {
        readonly Transform root;
        readonly Func<Font> font;
        readonly List<(GameObject go, CanvasGroup cg, float born)> live = new List<(GameObject, CanvasGroup, float)>();

        public PeaceToasts(Transform root, Func<Font> font) { this.root = root; this.font = font; }

        public void Show(string text, bool gold = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            // the same note again (a setting changed twice): the old one goes, the new one shows
            for (int i = live.Count - 1; i >= 0; i--)
                if (live[i].go != null && live[i].go.name == "Toast " + text) { UnityEngine.Object.Destroy(live[i].go); live.RemoveAt(i); }
            var rt = PeaceSkin.Rect(root, "Toast " + text);
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            var bg = PeaceSkin.Box(rt, "Bg", PeaceSkin.Panel);
            PeaceSkin.Fill(bg.rectTransform);
            var t = PeaceSkin.Label(rt, "Text", font(), 24, gold ? PeaceSkin.Gold : PeaceSkin.Pink, TextAnchor.MiddleCenter, true);
            PeaceSkin.Fill(t.rectTransform, 18, 0, 18, 0);
            t.text = Loc.Display(text);
            float w = Mathf.Clamp(PeaceSkin.Width(t, text) + 44f, 120f, 900f);
            PeaceSkin.At(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -166), new Vector2(w, 46));
            live.Add((rt.gameObject, cg, Time.unscaledTime));
            PeaceSfx.Play(PeaceSfx.Pop, 0.22f, gold ? 1.12f : 1f);
            while (live.Count > 3) { UnityEngine.Object.Destroy(live[0].go); live.RemoveAt(0); }
        }

        public void Tick()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var (go, cg, born) = live[i];
                float age = Time.unscaledTime - born;
                if (go == null || age > 2.6f) { if (go != null) UnityEngine.Object.Destroy(go); live.RemoveAt(i); continue; }
                cg.alpha = age < 0.2f ? age / 0.2f : age > 2.1f ? (2.6f - age) / 0.5f : 1f;
                go.transform.localScale = Vector3.one * (age < 0.32f ? 0.86f + 0.14f * PeaceFx.Back(age / 0.32f) : 1f);
                // the newest at the top, older ones pushed down
                int order = live.Count - 1 - i;
                var rt = go.GetComponent<RectTransform>();
                float y = -166 - order * 54 + Mathf.Min(0f, (age - 0.2f) * 0f);
                rt.anchoredPosition = new Vector2(-32 - (age < 0.2f ? (0.2f - age) * 120f : 0f), y);
            }
        }

        public void Clear() { foreach (var l in live) if (l.go != null) UnityEngine.Object.Destroy(l.go); live.Clear(); }
    }

    // A big card in the middle: the photo album (a picture, a caption, ◀ ▶) or a list of label · value rows.
    public class PeaceCard
    {
        readonly Transform root;
        readonly Func<Font> font;
        GameObject go;
        CanvasGroup cg;
        RawImage pic;
        Text caption, counter;
        float openT, picAt = -10f;
        int picDir;
        readonly List<(CanvasGroup cg, RectTransform rt)> rowFx = new List<(CanvasGroup, RectTransform)>();

        public bool Open => go != null;

        public PeaceCard(Transform root, Func<Font> font) { this.root = root; this.font = font; }

        GameObject Frame(string title, Vector2 size, out Transform body)
        {
            Close();
            var f = font();
            var rt = PeaceSkin.Rect(root, "PeaceCard");
            go = rt.gameObject;
            PeaceSkin.At(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), size);
            cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            var sh = PeaceSkin.Box(rt, "Shadow", PeaceSkin.Shadow, PeaceSkin.Glow);
            PeaceSkin.Fill(sh.rectTransform, -30, -40, -30, -20);
            var edge = PeaceSkin.Box(rt, "Edge", PeaceSkin.PanelEdge);
            PeaceSkin.Fill(edge.rectTransform, -2, -2, -2, -2);
            var bg = PeaceSkin.Box(rt, "Body", new Color(0.10f, 0.04f, 0.14f, 0.94f));
            PeaceSkin.Fill(bg.rectTransform);
            var band = PeaceSkin.Box(rt, "Header", Color.white);
            PeaceSkin.At(band.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(size.x, 76));
            PeaceSkin.Gradient(band, PeaceSkin.PinkA, PeaceSkin.PinkB);
            var tt = PeaceSkin.Label(band.transform, "Title", f, 34, Color.white, TextAnchor.MiddleLeft, true);
            PeaceSkin.Fill(tt.rectTransform, 30, 0, 30, 0);
            tt.text = Loc.Display(title);
            var b = PeaceSkin.Rect(rt, "Content");
            PeaceSkin.Fill(b, 30, 64, 30, 96);
            body = b;
            var foot = PeaceSkin.Label(rt, "Footer", f, 20, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter);
            PeaceSkin.At(foot.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(size.x - 60, 36));
            foot.name = "FooterText";
            openT = 0f;
            openedAt = Time.unscaledTime;
            return go;
        }

        // (smaller when the UI is drawn bigger, so it still fits the screen)
        static float PhotoH => Mathf.Min(820f, PeaceSkin.ScreenH - 60f);
        static float PhotoW => 1180f * PhotoH / 820f;

        public void ShowPhoto(string title, Texture2D tex, string captionText, string counterText, string footer, int dir = 0)
        {
            // a page turned: the new photo slides in from the side it came from
            if (go != null && pic != null && dir != 0) { picAt = Time.unscaledTime; picDir = dir; if (counter != null) PeaceFx.Pulse(counter.rectTransform, 0.15f, 0.35f); }
            if (go == null || pic == null)
            {
                Frame(title, new Vector2(PhotoW, PhotoH), out var body);
                var frame = PeaceSkin.Box(body, "PhotoFrame", new Color(1, 1, 1, 0.92f));
                PeaceSkin.Fill(frame.rectTransform, 0, 84, 0, 0);   // (two lines under it: her remark, who and when)
                var holder = PeaceSkin.Rect(frame.transform, "Holder");
                PeaceSkin.Fill(holder, 10, 10, 10, 10);
                pic = holder.gameObject.AddComponent<RawImage>();
                pic.raycastTarget = false;
                caption = PeaceSkin.Label(body, "Caption", font(), 23, Color.white, TextAnchor.LowerLeft);
                caption.lineSpacing = 1.15f;
                PeaceSkin.Fill(caption.rectTransform, 4, 0, 160, 0);
                counter = PeaceSkin.Label(body, "Counter", font(), 24, PeaceSkin.Pink, TextAnchor.LowerRight, true);
                PeaceSkin.Fill(counter.rectTransform, 160, 0, 4, 0);
                SetFooter(footer);
            }
            pic.texture = tex;
            // fit the picture into the frame without stretching it
            if (tex != null)
            {
                var holder = pic.rectTransform;
                float fw = PhotoW - 60 - 20, fh = PhotoH - 96 - 64 - 84 - 20;
                float a = (float)tex.width / Mathf.Max(1, tex.height);
                float w = fw, h = fw / a;
                if (h > fh) { h = fh; w = fh * a; }
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
                holder.pivot = new Vector2(0.5f, 0.5f);
                holder.anchoredPosition = Vector2.zero;
                holder.sizeDelta = new Vector2(w, h);
            }
            caption.text = Loc.Display(captionText ?? "");
            counter.text = Loc.Display(counterText ?? "");
        }

        // a page of a book (her diary): cream paper with ruled lines, the date line at the top, her words, the page number
        Text pageHead, pageBody;
        RectTransform pageRt;
        public void ShowPage(string title, string head, string body, string counterText, string footer, int dir = 0)
        {
            if (go != null && pageBody != null && dir != 0)
            {
                picAt = Time.unscaledTime; picDir = dir;
                if (counter != null) PeaceFx.Pulse(counter.rectTransform, 0.15f, 0.35f);
            }
            if (go == null || pageBody == null)
            {
                pic = null;
                var f = font();
                Frame(title, new Vector2(900, Mathf.Min(PeaceSkin.ScreenH - 80f, 760)), out var b);
                var paper = PeaceSkin.Box(b, "Paper", new Color(0.99f, 0.96f, 0.90f, 1f));
                PeaceSkin.Fill(paper.rectTransform, 0, 40, 0, 0);
                pageRt = paper.rectTransform;
                paper.gameObject.AddComponent<RectMask2D>();   // (the rules end with the paper)
                // ruled lines under each line of her writing, and a red margin, like a notebook
                for (int i = 0; i < 16; i++)
                {
                    var line = PeaceSkin.Rect(paper.transform, "Rule");
                    var img = line.gameObject.AddComponent<Image>();
                    img.color = new Color(0.55f, 0.68f, 0.85f, 0.35f);
                    img.raycastTarget = false;
                    line.anchorMin = new Vector2(0, 1); line.anchorMax = new Vector2(1, 1);
                    float y = 106f + i * 42.2f;   // (the body's line height: 26 px at line spacing 1.22)
                    line.offsetMin = new Vector2(20, -y - 1f); line.offsetMax = new Vector2(-20, -y + 1f);
                }
                var margin = PeaceSkin.Rect(paper.transform, "Margin");
                var mi = margin.gameObject.AddComponent<Image>();
                mi.color = new Color(0.9f, 0.45f, 0.5f, 0.45f);
                mi.raycastTarget = false;
                margin.anchorMin = new Vector2(0, 0); margin.anchorMax = new Vector2(0, 1);
                margin.offsetMin = new Vector2(70, 12); margin.offsetMax = new Vector2(72, -12);
                var ink = new Color(0.30f, 0.20f, 0.34f, 1f);
                pageHead = PeaceSkin.Label(paper.transform, "Head", f, 24, new Color(0.62f, 0.32f, 0.5f, 1f), TextAnchor.UpperLeft, true);
                PeaceSkin.Fill(pageHead.rectTransform, 90, 0, 30, 22);
                pageHead.GetComponent<Shadow>().enabled = false;
                pageBody = PeaceSkin.Label(paper.transform, "Body", f, 26, ink, TextAnchor.UpperLeft);
                pageBody.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageBody.lineSpacing = 1.22f;
                pageBody.fontStyle = FontStyle.Italic;
                PeaceSkin.Fill(pageBody.rectTransform, 90, 20, 40, 66);
                pageBody.GetComponent<Shadow>().enabled = false;
                counter = PeaceSkin.Label(b, "Counter", f, 22, PeaceSkin.Pink, TextAnchor.LowerRight, true);
                PeaceSkin.Fill(counter.rectTransform, 160, 0, 4, 0);
            }
            pageHead.text = Loc.Display(head ?? "");
            pageBody.text = Loc.Display(body ?? "");
            counter.text = Loc.Display(counterText ?? "");
            SetFooter(footer);
        }

        public void ShowRows(string title, IList<(string label, string value)> list, string footer)
        {
            pic = null;
            var f = font();
            float rowH = 46f;
            float h = 76 + 64 + 96 + Mathf.Max(1, list.Count) * rowH;
            Frame(title, new Vector2(760, Mathf.Min(PeaceSkin.ScreenH - 80f, h)), out var body);
            for (int i = 0; i < list.Count; i++)
            {
                var row = PeaceSkin.Box(body, "Row" + i, i % 2 == 0 ? new Color(1, 1, 1, 0.05f) : new Color(1, 1, 1, 0f));
                var rt = row.rectTransform;
                var rcg = row.gameObject.AddComponent<CanvasGroup>();
                rcg.alpha = 0f;
                rowFx.Add((rcg, rt));
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -i * rowH); rt.sizeDelta = new Vector2(0, rowH - 4);
                var l = PeaceSkin.Label(row.transform, "L", f, 24, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleLeft);
                PeaceSkin.Fill(l.rectTransform, 16, 0, 16, 0);
                l.text = Loc.Display(list[i].label);
                var v = PeaceSkin.Label(row.transform, "V", f, 24, PeaceSkin.Pink, TextAnchor.MiddleRight, true);
                PeaceSkin.Fill(v.rectTransform, 16, 0, 16, 0);
                v.text = Loc.Display(list[i].value);
            }
            SetFooter(footer);
        }

        void SetFooter(string footer)
        {
            var ft = go != null ? go.transform.Find("FooterText") : null;
            var t = ft != null ? ft.GetComponent<Text>() : null;
            if (t != null) t.text = Loc.Display(footer ?? "");
        }

        public void Tick(float dt)
        {
            if (go == null) return;
            float now = Time.unscaledTime;
            if (openT < 1f)
            {
                openT = Mathf.Min(1f, openT + Time.unscaledDeltaTime / 0.24f);
                cg.alpha = PeaceFx.Out(openT);
                go.transform.localScale = Vector3.one * (0.94f + 0.06f * PeaceFx.Back(openT));
            }
            // the records: row after row
            for (int i = 0; i < rowFx.Count; i++)
            {
                var (rcg, rrt) = rowFx[i];
                if (rcg == null) continue;
                float e = PeaceFx.Out((now - (openedAt + 0.06f + i * 0.03f)) / 0.2f);
                rcg.alpha = e;
                rrt.anchoredPosition = new Vector2(14f * (1f - e), rrt.anchoredPosition.y);
            }
            if (pageRt != null && pageBody != null)
            {
                // a page turned: the words slide in from that side
                float q = (now - picAt) / 0.22f;
                float e = q >= 1f ? 1f : PeaceFx.Out(q);
                var br = pageBody.rectTransform;
                br.anchoredPosition = new Vector2(picDir * 40f * (1f - e), br.anchoredPosition.y);
                pageBody.color = new Color(pageBody.color.r, pageBody.color.g, pageBody.color.b, e);
            }
            if (pic != null)
            {
                float q = (now - picAt) / 0.22f;
                float e = q >= 1f ? 1f : PeaceFx.Out(q);
                var prt = pic.rectTransform;
                prt.anchoredPosition = new Vector2(picDir * 46f * (1f - e), 0f);
                pic.color = new Color(1, 1, 1, e);
            }
        }

        float openedAt;

        // closing: a quick fade and a little shrink
        public void Close()
        {
            if (go != null) PeaceFx.FadeAway(go, 0.15f, new Vector2(0f, -8f), 0.04f);
            go = null; pic = null; caption = null; counter = null;
            pageHead = null; pageBody = null; pageRt = null;
            rowFx.Clear();
        }
    }
}
