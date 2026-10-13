using System;
using System.Collections.Generic;
using Coffee.UIEffects;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiSidePeaceMode
{
    // Screen-space UI for peace mode (HUD, fallback prompt, choice menu, fade).
    // Pills are cloned from the game's own interaction frame ("FrameInfo": rounded, pink gradient, shadow)
    // so the menu looks like the rest of MiSide.
    public class PeaceUI
    {
        static readonly Color TextColor = Color.white;
        static readonly Color TextShadow = new Color(0.30f, 0.03f, 0.26f, 0.9f);
        static readonly Color GradA = new Color(0.93f, 0.33f, 0.66f, 0.94f);
        static readonly Color GradB = new Color(0.55f, 0.20f, 0.80f, 0.94f);
        static readonly Color GradTitleA = new Color(0.98f, 0.45f, 0.74f, 0.97f);
        static readonly Color GradTitleB = new Color(0.86f, 0.26f, 0.58f, 0.97f);

        readonly GameObject root;
        readonly GameObject pillTemplate;
        readonly Font font;
        readonly GameObject hudPill;
        readonly Text hud;
        readonly GameObject promptPill;
        readonly Text prompt;
        readonly Image fade;
        GameObject menu;
        Action<int> onChoose;
        int optionCount;

        public bool MenuOpen => menu != null;

        public PeaceUI(GameObject frameTemplate)
        {
            font = GlobalGame.fontUse;
            if (font == null)
                foreach (var t in UnityEngine.Object.FindObjectsOfType<Text>())
                    if (t.font != null) { font = t.font; break; }

            root = new GameObject("PeaceModeUI");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            if (frameTemplate != null)
            {
                // keep an inactive private copy so the game can't destroy our template
                pillTemplate = UnityEngine.Object.Instantiate(frameTemplate, root.transform, false);
                pillTemplate.name = "PillTemplate";
                pillTemplate.SetActive(false);
            }

            hudPill = MakePill(root.transform, "Hud", 26, TextAnchor.MiddleCenter, out hud);
            Place(hudPill.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-36, -28), new Vector2(620, 54));

            promptPill = MakePill(root.transform, "Prompt", 30, TextAnchor.MiddleCenter, out prompt);
            Place(promptPill.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(460, 62));
            promptPill.SetActive(false);

            var fgo = new GameObject("Fade");
            fgo.transform.SetParent(root.transform, false);
            fade = fgo.AddComponent<Image>();
            fade.color = new Color(0, 0, 0, 0);
            fade.raycastTarget = false;
            var fr = fade.rectTransform;
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
        }

        // A rounded MiSide-style frame with a text label.
        GameObject MakePill(Transform parent, string name, int size, TextAnchor anchor, out Text text)
        {
            GameObject go;
            if (pillTemplate != null)
            {
                go = UnityEngine.Object.Instantiate(pillTemplate, parent, false);
                go.name = name;
                go.SetActive(true);
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
                text = go.GetComponentInChildren<Text>(true);
                // the interaction frame fades in only while hovered in-game: make our copy fully opaque and unmasked
                foreach (var m in go.GetComponentsInChildren<Mask>(true)) UnityEngine.Object.Destroy(m);
                foreach (var cg in go.GetComponentsInChildren<CanvasGroup>(true)) cg.alpha = 1f;
                foreach (var gr in go.GetComponentsInChildren<Graphic>(true)) { var c = gr.color; c.a = 1f; gr.color = c; }
                foreach (var g in go.GetComponents<UIGradient>()) { g.color1 = GradA; g.color2 = GradB; }
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.55f, 0.18f, 0.55f, 0.9f);
                var tgo = new GameObject("Text");
                tgo.transform.SetParent(go.transform, false);
                text = tgo.AddComponent<Text>();
            }
            if (text != null)
            {
                if (text.font == null || pillTemplate == null) text.font = font;
                text.fontSize = size;
                text.alignment = anchor;
                // white with a dark plum shadow: the template's light pink text was unreadable on the pink pills
                text.color = TextColor;
                foreach (var sh in text.GetComponents<Shadow>()) { sh.effectColor = TextShadow; sh.effectDistance = new Vector2(1.6f, -1.6f); }
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false;
                var tr = text.rectTransform;
                tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.pivot = new Vector2(0.5f, 0.5f);
                tr.offsetMin = new Vector2(22, 0); tr.offsetMax = new Vector2(-22, 0);
                tr.localScale = Vector3.one;
            }
            var bg = go.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = false;
            return go;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public void SetHud(string s, bool show)
        {
            s = Loc.Display(s);
            if (hud != null && hud.text != s) { hud.text = s; FitHud(s); }
            if (hudPill.activeSelf != show) hudPill.SetActive(show);
        }

        // The pill follows its text (a name, a language can be much longer), but the clock ticking must not make it
        // breathe: the text is measured with every digit as the font's widest one, and the pill grows at once but only
        // shrinks when it is clearly too wide (another Mita, another language). It is anchored at its right edge.
        float hudWidth;
        char wideDigit;

        void FitHud(string s)
        {
            if (hud == null || string.IsNullOrEmpty(s)) return;
            try
            {
                var gen = hud.cachedTextGeneratorForLayout;
                var set = hud.GetGenerationSettings(Vector2.zero);
                float Measure(string t) => gen.GetPreferredWidth(t, set) / hud.pixelsPerUnit;
                if (wideDigit == '\0')
                {
                    wideDigit = '0';
                    float best = -1f;
                    for (char c = '0'; c <= '9'; c++) { var w = Measure(c.ToString()); if (w > best) { best = w; wideDigit = c; } }
                }
                var chars = s.ToCharArray();
                for (int i = 0; i < chars.Length; i++) if (chars[i] >= '0' && chars[i] <= '9') chars[i] = wideDigit;
                var tr = hud.rectTransform;
                float want = Mathf.Clamp(Measure(new string(chars)) - tr.offsetMax.x + tr.offsetMin.x + 12f, 260f, 1500f);
                if (want > hudWidth || want < hudWidth - 48f)
                {
                    hudWidth = want;
                    var rt = hudPill.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(hudWidth, rt.sizeDelta.y);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("[ui] hud width: " + e.Message); }
        }

        public void ShowPrompt(string s)
        {
            if (s == null) { if (promptPill.activeSelf) promptPill.SetActive(false); return; }
            if (prompt != null) prompt.text = Loc.Display(s);
            if (!promptPill.activeSelf) promptPill.SetActive(true);
        }

        // the hint line over the black screen (a scene that happens in the dark)
        public void PromptOnTop(bool on)
        {
            if (on) promptPill.transform.SetAsLastSibling();
            else fade.transform.SetAsLastSibling();
        }

        public void SetFade(float a) => fade.color = new Color(0, 0, 0, Mathf.Clamp01(a));
        public void SetTint(Color c) => fade.color = c;
        public void SetVisible(bool v) { if (root != null) root.SetActive(v); }

        public void OpenMenu(string title, IList<string> options, Action<int> choose)
        {
            CloseMenu();
            onChoose = choose;
            optionCount = options.Count;
            menu = new GameObject("PeaceMenu");
            menu.transform.SetParent(root.transform, false);
            var mrt = menu.AddComponent<RectTransform>();
            float rowH = 64, gap = 10, titleH = 76;
            float h = titleH + gap * 2 + (rowH + gap) * options.Count;
            Place(mrt, new Vector2(1, 0.5f), new Vector2(-110, 0), new Vector2(520, h));

            var titlePill = MakePill(menu.transform, "Title", 36, TextAnchor.MiddleCenter, out var tt);
            Place(titlePill.GetComponent<RectTransform>(), new Vector2(0.5f, 1), Vector2.zero, new Vector2(360, titleH));
            foreach (var g in titlePill.GetComponents<UIGradient>()) { g.color1 = GradTitleA; g.color2 = GradTitleB; }
            if (tt != null) { tt.text = "♡ " + Loc.Display(title) + " ♡"; tt.color = Color.white; }

            for (int i = 0; i < options.Count; i++)
            {
                int idx = i;
                var row = MakePill(menu.transform, "Option" + i, 29, TextAnchor.MiddleLeft, out var txt);
                Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0, -(titleH + gap * 2 + (rowH + gap) * i)), new Vector2(500, rowH));
                var img = row.GetComponent<Image>();
                if (img == null) img = row.AddComponent<Image>();
                img.raycastTarget = true;
                var btn = row.AddComponent<Button>();
                var cb = btn.colors;
                cb.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                cb.highlightedColor = Color.white;
                cb.pressedColor = new Color(1f, 0.75f, 0.9f, 1f);
                cb.selectedColor = cb.normalColor;
                btn.colors = cb;
                btn.targetGraphic = img;
                btn.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => Choose(idx))));
                if (txt != null) txt.text = Loc.RightToLeft ? $"{Loc.Display(options[i])}   {(i + 1) % 10}" : $"{(i + 1) % 10}   {options[i]}";   // the tenth is on 0
            }
        }

        public void Choose(int i)
        {
            if (menu == null || i < 0 || i >= optionCount) return;
            var cb = onChoose;
            CloseMenu();
            cb?.Invoke(i);
        }

        public void CloseMenu()
        {
            if (menu != null) UnityEngine.Object.Destroy(menu);
            menu = null;
            onChoose = null;
        }

        // ---------------------------------------------------------------- a line of text to type ("자유롭게 말하기")

        GameObject inputPanel;
        InputField input;
        Text inputHint;

        public bool InputOpen => inputPanel != null && inputPanel.activeSelf;
        public string InputText => input != null ? input.text : "";

        public void OpenInput(string hint)
        {
            if (inputPanel == null)
            {
                inputPanel = MakePill(root.transform, "ChatInput", 30, TextAnchor.MiddleLeft, out var shown);
                Place(inputPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1100, 72));
                var bg = inputPanel.GetComponent<Image>();
                if (bg != null) bg.raycastTarget = true;
                if (shown != null)
                {
                    shown.supportRichText = false;
                    shown.horizontalOverflow = HorizontalWrapMode.Wrap;
                    var ph = UnityEngine.Object.Instantiate(shown.gameObject, shown.transform.parent, false);
                    ph.name = "Hint";
                    inputHint = ph.GetComponent<Text>();
                    inputHint.color = new Color(1f, 1f, 1f, 0.55f);
                    inputHint.fontStyle = FontStyle.Italic;
                    shown.text = "";
                }
                input = inputPanel.AddComponent<InputField>();
                input.textComponent = shown;
                input.placeholder = inputHint;
                input.lineType = InputField.LineType.SingleLine;
                input.characterLimit = 120;
                input.caretWidth = 3;
                input.customCaretColor = true;
                input.caretColor = Color.white;
            }
            if (inputHint != null) inputHint.text = hint;
            input.text = "";
            inputPanel.SetActive(true);
            Input.imeCompositionMode = IMECompositionMode.On;
            FocusInput();
        }

        public void SetInputText(string t) { if (input != null) input.text = t ?? ""; }

        public void FocusInput()
        {
            if (input == null) return;
            try { input.Select(); input.ActivateInputField(); } catch { }
        }

        public void CloseInput()
        {
            if (inputPanel == null) return;
            try { input.DeactivateInputField(); } catch { }
            inputPanel.SetActive(false);
            Input.imeCompositionMode = IMECompositionMode.Auto;
        }

        public void Destroy() { if (root != null) UnityEngine.Object.Destroy(root); }
    }
}
