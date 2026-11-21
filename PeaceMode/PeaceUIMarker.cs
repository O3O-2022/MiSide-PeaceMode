using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiSidePeaceMode
{
    // "Where is she?": a small pill with her name and distance. Over her head when she is on screen but behind a wall,
    // at the edge of the screen with an arrow towards her when she is off it. (Placed and shown by PeaceDirector.TickMarker.)
    public class PeaceMarker
    {
        readonly RectTransform rt;
        readonly CanvasGroup group;
        readonly Text text;
        readonly RectTransform arrowRt;
        readonly Text arrow;
        float alpha, target;
        Vector2 arrowBase, arrowDir;

        public PeaceMarker(Transform root, Func<Font> font)
        {
            rt = PeaceSkin.Rect(root, "MitaMarker");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(150, 40);
            group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var bg = PeaceSkin.Box(rt, "Bg", PeaceSkin.Panel, PeaceSkin.Round);
            PeaceSkin.Fill(bg.rectTransform);
            var edge = PeaceSkin.Box(rt, "Edge", PeaceSkin.PanelEdge, PeaceSkin.Round);
            PeaceSkin.Fill(edge.rectTransform, -1.5f, -1.5f, -1.5f, -1.5f);
            edge.transform.SetAsFirstSibling();
            text = PeaceSkin.Label(rt, "Text", font(), 20, Color.white, TextAnchor.MiddleCenter, true);
            PeaceSkin.Fill(text.rectTransform, 12, 0, 12, 0);
            arrowRt = PeaceSkin.Rect(rt, "Arrow");
            arrowRt.anchorMin = arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRt.sizeDelta = new Vector2(30, 30);
            arrow = arrowRt.gameObject.AddComponent<Text>();
            arrow.font = font();
            arrow.fontSize = 22;
            arrow.alignment = TextAnchor.MiddleCenter;
            arrow.color = PeaceSkin.Pink;
            arrow.raycastTarget = false;
            arrow.text = "▲";
            rt.gameObject.SetActive(false);
        }

        // pos: the pill's centre on the canvas (from the bottom left); dir: towards her on screen (zero = no arrow)
        public void Set(Vector2 pos, Vector2 dir, string label, Font font)
        {
            if (font != null && text.font != font) text.font = font;
            string s = Loc.Display(label);
            if (text.text != s)
            {
                text.text = s;
                rt.sizeDelta = new Vector2(Mathf.Max(110f, PeaceSkin.Width(text, s) + 34f), 40);
            }
            rt.anchoredPosition = pos;
            bool showArrow = dir.sqrMagnitude > 0.0001f;
            arrow.enabled = showArrow;
            if (showArrow)
            {
                var d = dir.normalized;
                // just outside the pill, on the side facing her
                arrowBase = new Vector2(d.x * (rt.sizeDelta.x * 0.5f + 14f), d.y * (rt.sizeDelta.y * 0.5f + 14f));
                arrowDir = d;
                arrowRt.anchoredPosition = arrowBase;
                arrowRt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            }
            target = 1f;
        }

        public void Hide() => target = 0f;

        public void Tick(float dt)
        {
            alpha = Mathf.MoveTowards(alpha, target, dt / 0.25f);
            group.alpha = alpha * 0.92f;
            rt.localScale = Vector3.one * (0.9f + 0.1f * PeaceFx.Back(alpha));
            if (arrow.enabled) arrowRt.anchoredPosition = arrowBase + arrowDir * (2.5f * Mathf.Sin(Time.unscaledTime * 4f));
            bool on = alpha > 0.001f;
            if (rt.gameObject.activeSelf != on) rt.gameObject.SetActive(on);
        }
    }
}
