using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiSidePeaceMode
{
    // "옷 색 바꾸기" for the Mitas who are their own models (kind, cap, mila, sleepy, short): the game's costumes are
    // made for the original Mita, but these models keep their clothes on a texture of their own ("Cloth" /
    // "Clothes", nothing but clothes on it), so a recoloured copy of that texture gives them new outfits. The
    // clothes' main colour is turned to the chosen one (colours far from it stay as they are); black,
    // white and skin-coloured buttons barely move as they have little colour. (The chibi is one texture, skin and
    // all, so she keeps hers.)
    public partial class PeaceDirector
    {
        // key, menu name, target hue (0-1; <0 = no colour)
        static readonly (string key, string name, float hue)[] ClothColors =
        {
            ("blue", "하늘색 옷", 0.57f),
            ("pink", "분홍색 옷", 0.93f),
            ("mint", "민트색 옷", 0.44f),
            ("yellow", "노란색 옷", 0.13f),
            ("mono", "흑백 옷", -1f),
        };

        bool CanRecolor => skin != null && Pn.Id != "chibi" && ClothTextures().Count > 0;

        // her clothes' textures as the model came (by material, on her copy)
        readonly Dictionary<IntPtr, Texture> clothOriginal = new Dictionary<IntPtr, Texture>();
        readonly Dictionary<string, Texture2D> recolored = new Dictionary<string, Texture2D>();
        string colorNow = "";

        List<Material> ClothTextures()
        {
            var list = new List<Material>();
            if (skin == null) return list;
            foreach (var r in skin.Renderers)
            {
                if (r == null) continue;
                foreach (var m in r.materials)
                {
                    if (m == null) continue;
                    var n = m.name;
                    if (!(n.StartsWith("Cloth") || n.StartsWith("Clothes"))) continue;
                    if (!clothOriginal.ContainsKey(m.Pointer)) clothOriginal[m.Pointer] = m.mainTexture;
                    list.Add(m);
                }
            }
            return list;
        }

        bool Recolor(string key)
        {
            var mats = ClothTextures();
            if (mats.Count == 0) return false;
            int ci = Array.FindIndex(ClothColors, c => c.key == key);
            foreach (var m in mats)
            {
                var orig = clothOriginal.TryGetValue(m.Pointer, out var o) ? o : m.mainTexture;
                if (orig == null) continue;
                if (ci < 0) { m.mainTexture = orig; continue; }
                var cacheKey = Pn.Id + "|" + orig.name + "|" + key;
                if (!recolored.TryGetValue(cacheKey, out var tex) || tex == null)
                {
                    float t0 = Time.realtimeSinceStartup;
                    tex = RecolorTexture(orig, ClothColors[ci].hue);
                    recolored[cacheKey] = tex;
                    Plugin.Log.LogInfo($"[clothes] {Pn.Id} {orig.name} -> {key} in {Time.realtimeSinceStartup - t0:0.00}s");
                }
                if (tex != null) m.mainTexture = tex;
            }
            colorNow = ci < 0 ? "" : key;
            S.Outfit = ci < 0 ? "" : "color:" + key;
            return true;
        }

        // a readable copy (the game's textures aren't readable: through a render texture), with the hue turned
        static Texture2D RecolorTexture(Texture src, float targetHue)
        {
            int w = src.width, h = src.height;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Texture2D tex;
            try
            {
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            }
            finally { RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); }
            var px = tex.GetPixels32();
            int n = px.Length;
            var buf = new Color32[n];
            for (int i = 0; i < n; i++) buf[i] = px[i];
            float delta = 0f, main = 0f;
            if (targetHue >= 0f)
            {
                // the clothes' main hue: the strongest colour bin, weighted by how colourful each pixel is
                var bins = new float[36];
                for (int i = 0; i < n; i += 7)
                {
                    ToHsv(buf[i], out var hh, out var ss, out var vv);
                    if (ss < 0.25f || vv < 0.2f) continue;
                    bins[Math.Min(35, (int)(hh * 36))] += ss * vv;
                }
                int best = 0;
                for (int b = 1; b < 36; b++) if (bins[b] > bins[best]) best = b;
                main = (best + 0.5f) / 36f;
                delta = targetHue - main;
            }
            // (plain C# all the way: Unity's Color.RGBToHSV / Mathf go through the IL2CPP bridge, 3 s for a 2048 texture)
            for (int i = 0; i < n; i++)
            {
                var c = buf[i];
                ToHsv(c, out var hh, out var ss, out var vv);
                if (targetHue >= 0f)
                {
                    // only the main colour (and what is near it) turns: the rest of the outfit - a blue skirt under a
                    // red sweater - keeps its own colour (a full turn made that skirt brown)
                    float d = Math.Abs(hh - main); d = Math.Min(d, 1f - d);
                    float t = Math.Clamp((d - 0.07f) / 0.08f, 0f, 1f);
                    float wgt = 1f - t * t * (3f - 2f * t);
                    hh += delta * wgt; hh -= (float)Math.Floor(hh);
                }
                else { ss *= 0.1f; vv *= 0.85f; }
                buf[i] = FromHsv(hh, ss, vv, c.a);
            }
            tex.SetPixels32(buf);
            tex.Apply(true);
            tex.name = src.name + " (peace)";
            UnityEngine.Object.DontDestroyOnLoad(tex);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return tex;
        }

        static void ToHsv(Color32 c, out float h, out float s, out float v)
        {
            float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
            float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
            v = max;
            s = max > 0f ? d / max : 0f;
            if (d <= 0f) { h = 0f; return; }
            if (max == r) h = (g - b) / d + (g < b ? 6f : 0f);
            else if (max == g) h = (b - r) / d + 2f;
            else h = (r - g) / d + 4f;
            h /= 6f;
        }

        static Color32 FromHsv(float h, float s, float v, byte a)
        {
            float r, g, b;
            if (s <= 0f) r = g = b = v;
            else
            {
                float hh = h * 6f;
                int i = (int)Math.Floor(hh) % 6;
                float f = hh - (float)Math.Floor(hh), p = v * (1f - s), q = v * (1f - s * f), t = v * (1f - s * (1f - f));
                switch (i)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    default: r = v; g = p; b = q; break;
                }
            }
            return new Color32((byte)(r * 255f + 0.5f), (byte)(g * 255f + 0.5f), (byte)(b * 255f + 0.5f), a);
        }

        void OpenColorMenu()
        {
            var opts = new List<string>();
            var acts = new List<Action>();
            if (colorNow != "") { opts.Add("원래 옷"); acts.Add(() => ChangeColor("")); }
            foreach (var c in ClothColors)
                if (c.key != colorNow) { var k = c.key; opts.Add(c.name); acts.Add(() => ChangeColor(k)); }
            opts.Add("그만두기"); acts.Add(() => { });
            ShowMenu("옷 갈아입기", opts, acts);
        }

        void ChangeColor(string key)
        {
            Did("clothes");
            fadeTarget = 1f;
            afterFade = () =>
            {
                try { Recolor(key); } catch (Exception e) { Plugin.Log.LogError("[clothes] recolor: " + e); }
                fadeTarget = 0f;
                afterFade = () => Play(Pick(Personas.ColorLines(Pn.Id, key)), () => { AddAff(1); CheckMilestones(); });
            };
        }

        void ColorAsSaved()
        {
            if (!CanRecolor || string.IsNullOrEmpty(S.Outfit) || !S.Outfit.StartsWith("color:")) return;
            try { Recolor(S.Outfit.Substring(6)); } catch (Exception e) { Plugin.Log.LogWarning("[clothes] saved colour: " + e.Message); }
        }

        public void DevColor(string key) { try { Recolor(key == "off" ? "" : key); } catch (Exception e) { Plugin.Log.LogError("[clothes] " + e); } }
    }

    public static partial class Personas
    {
        public static Line[][] ColorLines(string id, string key)
        {
            bool back = key == "";
            switch (id)
            {
                case "kind":
                    return back ? new[] { S(M("역시 원래 옷이 제일 편해요.", "smile"), M("익숙한 게 제일 안전하거든요.", "smile")) }
                                : new[] { S(M("자, 어때요? 색만 바꿔 봤어요.", "smile"), M("…생각보다 잘 어울리죠? 맞죠?", "shy")),
                                          S(M("옷 색 변경 완료!", "smile"), M("가끔은 이런 변화도 나쁘지 않네요.", "smile")) };
                case "cap":
                    return back ? new[] { S(M("원래 무대 의상으로 복귀! 히히힛!", "smile")) }
                                : new[] { S(M("짜잔! 새 무대 의상이에요!", "smile"), M("오늘 공연은 이 색으로 간다! 히히힛!", "smiletonque")),
                                          S(M("오호, 이 색 완전 록스타 같지 않아요?", "smile"), M("박수! 박수 쳐 줘요!", "smile")) };
                case "mila":
                    return back ? new[] { S(M("원래 옷으로 돌아왔어요. …이게 제일 나아요.", "discontent")) }
                                : new[] { S(M("…뭘 그렇게 봐요. 옷 색 좀 바꾼 거예요.", "shy"), M("이상하면 이상하다고 말해요. …안 이상하죠?", "discontent")),
                                          S(M("새 옷이에요. 감상은 짧게 부탁해요.", "arrogance"), M("…칭찬이면 길어도 돼요.", "shy")) };
                case "sleepy":
                    return back ? new[] { S(M("원래 옷… 이게 제일 포근해요…", "halfsleep")) }
                                : new[] { S(M("옷 색이 바뀌었어요… 잠옷 같기도 하고…", "halfsleep"), M("이대로 자도 되겠어요…", "smile")),
                                          S(M("음… 새 옷이에요…", "halfsleep"), M("어울려요…? 그럼 됐어요… 쿨…", "smile")) };
                case "short":
                    return back ? new[] { S(M("원래 옷으로 돌아왔어요! 역시 이게 제일 저답죠.", "smile")) }
                                : new[] { S(M("어머, 색 하나 바꿨을 뿐인데 기분이 확 달라지네요!", "smile"), M("신생 미타들한테도 보여 줘야겠어요.", "smile")),
                                          S(M("새 옷이에요! 당연히 잘 어울리죠?", "smile"), M("…솔직하게 말해 줘도 돼요.", "shy")) };
                default:
                    return new[] { S(M("어때요? 잘 어울려요?", "smile")) };
            }
        }
    }
}
