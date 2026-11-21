using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MiSidePeaceMode
{
    // Photo mode (2026-10): before the picture you choose how she poses (our held motions, a V by her face...),
    // her face and a frame; then 3-2-1, the shutter, a white flash. The frame is a canvas of its own drawn over the
    // game while ScreenCapture takes the picture (the rest of our UI is hidden for it), so it is in the photo.
    public class PeacePhotoOverlay
    {
        readonly GameObject root;
        readonly RectTransform frameRoot;
        readonly Text count;
        readonly Image flash;
        readonly Func<Font> font;

        public PeacePhotoOverlay(Func<Font> font)
        {
            this.font = font;
            root = new GameObject("PeacePhotoOverlay");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            frameRoot = PeaceSkin.Rect(root.transform, "Frame");
            PeaceSkin.Fill(frameRoot);
            count = PeaceSkin.Label(root.transform, "Count", font(), 220, Color.white, TextAnchor.MiddleCenter, true);
            PeaceSkin.Fill(count.rectTransform);
            var sh = count.GetComponent<Shadow>();
            if (sh != null) { sh.effectColor = new Color(0.6f, 0.1f, 0.4f, 0.8f); sh.effectDistance = new Vector2(5, -5); }
            count.text = "";
            var fgo = PeaceSkin.Rect(root.transform, "Flash");
            PeaceSkin.Fill(fgo);
            flash = fgo.gameObject.AddComponent<Image>();
            flash.color = new Color(1, 1, 1, 0);
            flash.raycastTarget = false;
        }

        public void Destroy() { if (root != null) UnityEngine.Object.Destroy(root); }

        public void Count(string s, float scale)
        {
            count.font = font();
            count.text = s;
            count.rectTransform.localScale = Vector3.one * scale;
            count.color = new Color(1, 1, 1, Mathf.Clamp01(1.6f - scale));
        }

        public void Flash(float a) => flash.color = new Color(1, 1, 1, Mathf.Clamp01(a));

        public void ClearFrame()
        {
            for (int i = frameRoot.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(frameRoot.GetChild(i).gameObject);
        }

        Image Box(string name, Color c, float l, float b, float r, float t, bool anchorsTo = true)
        {
            var img = PeaceSkin.Box(frameRoot, name, c);
            img.sprite = null;
            img.type = Image.Type.Simple;
            PeaceSkin.Fill(img.rectTransform, l, b, r, t);
            return img;
        }

        Image Edge(string name, Color c, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Sprite sprite = null)
        {
            var img = PeaceSkin.Box(frameRoot, name, c, sprite);
            if (sprite == null) { img.sprite = null; img.type = Image.Type.Simple; }
            var rt = img.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            return img;
        }

        Text Write(string s, int size, Color c, TextAnchor a, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool bold = false, FontStyle style = FontStyle.Normal)
        {
            var t = PeaceSkin.Label(frameRoot, "Text", font(), size, c, a, bold);
            if (style != FontStyle.Normal) t.fontStyle = style;
            var rt = t.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            t.text = Loc.Display(s);
            var sh = t.GetComponent<Shadow>();
            if (sh != null) sh.enabled = c.r + c.g + c.b > 1.5f;
            return t;
        }

        // the frames (laid out on the whole screen, any aspect)
        public void SetFrame(int kind, string who, DateTime when)
        {
            ClearFrame();
            var f0 = Vector2.zero; var f1 = Vector2.one;
            switch (kind)
            {
                case 1:   // a polaroid: white all round, a wide strip at the bottom with who and when, as if written by hand
                {
                    var white = new Color(0.98f, 0.97f, 0.95f, 1f);
                    Edge("L", white, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(38, 0));
                    Edge("R", white, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-38, 0), Vector2.zero);
                    Edge("T", white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -38), Vector2.zero);
                    Edge("B", white, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 150));
                    var ink = new Color(0.32f, 0.22f, 0.36f, 1f);
                    Write(Loc.F("{0}와 함께", who) + "  ♥", 46, ink, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(70, 20), new Vector2(-70, 140), false, FontStyle.Italic);
                    Write(when.ToString("yyyy. M. d."), 34, new Color(0.45f, 0.38f, 0.5f, 1f), TextAnchor.MiddleRight, new Vector2(0, 0), new Vector2(1, 0), new Vector2(70, 20), new Vector2(-70, 140), false, FontStyle.Italic);
                    break;
                }
                case 2:   // film: black bands with sprocket holes, a frame number
                {
                    var black = new Color(0.06f, 0.05f, 0.06f, 1f);
                    Edge("T", black, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -96), Vector2.zero);
                    Edge("B", black, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 96));
                    var hole = new Color(0.92f, 0.9f, 0.86f, 1f);
                    for (int i = 0; i < 40; i++)
                    {
                        float x = 30 + i * 64;
                        Edge("H", hole, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -62), new Vector2(x + 34, -36), PeaceSkin.Round);
                        Edge("H", hole, new Vector2(0, 0), new Vector2(0, 0), new Vector2(x, 36), new Vector2(x + 34, 62), PeaceSkin.Round);
                    }
                    var amber = new Color(1f, 0.62f, 0.2f, 0.95f);
                    Write("PEACE 400", 22, amber, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 0), new Vector2(-40, 30), true);
                    Write($"{when:MMdd}  ▶ 24", 22, amber, TextAnchor.MiddleRight, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 0), new Vector2(-40, 30), true);
                    break;
                }
                case 3:   // hearts: a soft pink glow in from the edges, little hearts along them
                {
                    var pink = new Color(1f, 0.55f, 0.78f, 0.28f);
                    Edge("GL", pink, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-260, -260), new Vector2(170, 260), PeaceSkin.Glow);
                    Edge("GR", pink, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-170, -260), new Vector2(260, 260), PeaceSkin.Glow);
                    Edge("GT", pink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-260, -170), new Vector2(260, 260), PeaceSkin.Glow);
                    Edge("GB", pink, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-260, -260), new Vector2(260, 170), PeaceSkin.Glow);
                    var rnd = new System.Random(when.DayOfYear * 7 + when.Hour);
                    for (int i = 0; i < 26; i++)
                    {
                        // round the edges: x along top/bottom, or y along the sides
                        bool side = i % 2 == 0;
                        float u = (float)rnd.NextDouble(), inset = 30 + (float)rnd.NextDouble() * 60;
                        Vector2 a = side ? new Vector2(rnd.Next(2), u) : new Vector2(u, rnd.Next(2));
                        Vector2 off = new Vector2(a.x == 0 ? inset : a.x == 1 ? -inset : 0, a.y == 0 ? inset : a.y == 1 ? -inset : 0);
                        var c = rnd.Next(3) == 0 ? new Color(1, 1, 1, 0.85f) : new Color(1f, 0.38f, 0.66f, 0.9f);
                        int size = 30 + rnd.Next(36);
                        var t = Write("♥", size, c, TextAnchor.MiddleCenter, a, a, off - new Vector2(40, 40), off + new Vector2(40, 40), true);
                        t.rectTransform.localRotation = Quaternion.Euler(0, 0, rnd.Next(-25, 26));
                    }
                    break;
                }
                case 4:   // the old camera's date stamp, orange, bottom right
                {
                    var stamp = Write($"'{when:yy  M  d}", 46, new Color(1f, 0.55f, 0.15f, 0.95f), TextAnchor.LowerRight, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 36), new Vector2(-60, 120), true);
                    var sh = stamp.GetComponent<Shadow>();
                    if (sh != null) { sh.enabled = true; sh.effectColor = new Color(1f, 0.3f, 0.05f, 0.45f); sh.effectDistance = new Vector2(0, 0); }
                    stamp.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.35f, 0.1f, 0.35f);
                    break;
                }
            }
        }
    }

    public partial class PeaceDirector
    {
        static readonly (string name, string motion)[] PhotoPoses =
        {
            ("자연스럽게", null), ("브이", "@pose_v"), ("손 흔들기", "@pose_hi"), ("양손 인사", "@pose_both"),
            ("만세", "@pose_cheer"), ("생각하는 척", "@pose_chin"), ("고개 갸웃", "@pose_tilt"), ("수줍게", "@pose_shy"),
        };
        static readonly (string name, string emo)[] PhotoFaces =
        {
            ("웃음", "smile"), ("수줍음", "shy"), ("메롱", "smiletonque"), ("놀람", "surprise"), ("새침", "discontent"),
        };
        static readonly string[] PhotoFrames = { "없음", "폴라로이드", "필름", "하트", "날짜 스탬프" };

        int photoPose = 1, photoFace, photoFrame = 1;
        PeacePhotoOverlay photoOverlay;
        float photoCountStart;

        void DoPhoto() => OpenPhotoMenu();

        void OpenPhotoMenu()
        {
            Did("photo");
            PreviewPhotoPose();
            var items = new List<MenuItem>
            {
                new MenuItem("포즈") { Hint = Loc.T(PhotoPoses[photoPose].name) },
                new MenuItem("표정") { Hint = Loc.T(PhotoFaces[photoFace].name) },
                new MenuItem("프레임") { Hint = Loc.T(PhotoFrames[photoFrame]) },
                new MenuItem("찰칵!") { Now = true, Hint = Loc.T("3초 뒤에 찍어요") },
                new MenuItem("그만두기") { Back = true },
            };
            var acts = new List<Action>
            {
                () => { photoPose = (photoPose + 1) % PhotoPoses.Length; RefreshPhotoMenu(); },
                () => { photoFace = (photoFace + 1) % PhotoFaces.Length; RefreshPhotoMenu(); },
                () => { photoFrame = (photoFrame + 1) % PhotoFrames.Length; RefreshPhotoMenu(); },
                StartPhotoCountdown,
                () => { },
            };
            ShowItems("같이 사진 찍기", items, acts, OpenPhotoMenu);
        }

        void RefreshPhotoMenu()
        {
            choosingFrom = null;
            reopening = refreshing = true;
            try { OpenPhotoMenu(); } finally { reopening = refreshing = false; }
        }

        // she takes the pose (and the face) while you choose
        void PreviewPhotoPose()
        {
            try
            {
                FaceYouForMenu();
                var m = PhotoPoses[photoPose].motion;
                if (m == null) EndMotion(); else if (motion == null || motion.Name != m) PlayMotion(m);
                mita?.FaceEmotion(PhotoFaces[photoFace].emo);
            }
            catch (Exception e) { Plugin.Log.LogWarning("[photo] pose: " + e.Message); }
        }

        void StartPhotoCountdown()
        {
            Plugin.Log.LogInfo($"[photo] {PhotoPoses[photoPose].name} / {PhotoFaces[photoFace].emo} / frame {photoFrame}");
            HoldControls(true);
            PreviewPhotoPose();
            photoOverlay ??= new PeacePhotoOverlay(() => ui.Font);
            photoOverlay.ClearFrame();
            photoOverlay.Flash(0f);
            ui.SetVisible(false);
            photoStage = 1;
            photoCountStart = Time.realtimeSinceStartup;
            photoAt = photoCountStart + 3.0f;
            lastCountShown = -1;
        }

        int lastCountShown = -1;

        void TickPhoto()
        {
            if (photoStage == 0) return;
            float now = Time.realtimeSinceStartup;
            if (photoStage == 1)
            {
                // 3 · 2 · 1 (each number pops and fades), her face kept in the middle
                AimAtMita();
                HoldControls(true);
                try { mita?.FaceEmotion(PhotoFaces[photoFace].emo); } catch { }
                // (still talking: the count waits until her words are gone, or they'd be in the picture)
                if ((busy || queue.Count > 0) && now - photoCountStart < 12f) { photoAt = now + 3.0f; lastCountShown = -1; photoOverlay.Count("", 1f); return; }
                float left = photoAt - now;
                int n = Mathf.CeilToInt(left);
                if (n != lastCountShown && n >= 1) { lastCountShown = n; PeaceSfx.Play(PeaceSfx.Move, 0.32f, n == 1 ? 1.25f : 1.1f); }
                float frac = left - Mathf.Floor(left);
                if (n >= 1) photoOverlay.Count(n.ToString(), 0.7f + 0.6f * (1f - frac) * 0.6f + 0.3f * frac);
                if (now < photoAt) return;
                photoOverlay.Count("", 1f);
                ClearLeftoverText();
                photoOverlay.SetFrame(photoFrame, Loc.T(Pn.Short), DateTime.Now);
                var dir = Path.Combine(Path.GetDirectoryName(BepInEx.Paths.GameRootPath + "/"), "PeaceModePhotos");
                Directory.CreateDirectory(dir);
                photoPath = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Pn.Id}.png");
                ScreenCapture.CaptureScreenshot(photoPath);
                photoStage = 2;
                photoAt = now + 0.12f;
                return;
            }
            if (photoStage == 2)
            {
                if (now < photoAt) return;
                // the shutter: a white flash after the picture is in
                photoOverlay.ClearFrame();
                PeaceSfx.Play("Flash", 0.5f);
                photoStage = 3;
                photoAt = now;
                return;
            }
            float f = (now - photoAt) / 0.45f;
            photoOverlay.Flash(0.85f * (1f - PeaceFx.Out(f)));
            if (f < 1f) return;
            photoOverlay.Flash(0f);
            ui.SetVisible(true);
            photoStage = 0;
            EndMotion();
            HoldControls(false);
            Plugin.Log.LogInfo("[peace] photo saved: " + photoPath);
            AddAff(1);
            ui.Toast(Loc.T("사진을 앨범에 넣었어요"));
            var wall = Personas.Wall(Pn.Id);
            Play((wall.PhotoDone ?? new Line[0]).Concat(PhotoCountLines(Path.GetDirectoryName(photoPath), wall)).ToList());
        }

        public void DevPhoto(string arg)
        {
            // photo <pose index> <face index> <frame index>: straight to the countdown
            var sp = (arg ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length > 0) photoPose = int.Parse(sp[0]) % PhotoPoses.Length;
            if (sp.Length > 1) photoFace = int.Parse(sp[1]) % PhotoFaces.Length;
            if (sp.Length > 2) photoFrame = int.Parse(sp[2]) % PhotoFrames.Length;
            StartPhotoCountdown();
        }
    }
}
