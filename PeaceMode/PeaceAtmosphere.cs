using System.Collections.Generic;
using UnityEngine;

namespace MiSidePeaceMode
{
    // Day and night inside the house, the way the official (unreleased) peace mode meant it (Location21_World's
    // LocationTimeDay: window materials, lamps, sun particles per time of day). ApplyLighting still handles the sun
    // and the ambient light; this adds what made the night look like a purple-tinted day:
    //  - the windows' glow (frame emission + glass) goes to a dark blue night sky,
    //  - the sunlight through the windows (the "Sun" point lights and their light-shaft particles) fades out,
    //  - the lamps' shades light up warm and a warm point light sits at each of them.
    // Materials are per-renderer instances, so nothing of the game's shared assets changes.
    public partial class PeaceDirector
    {
        class MatColor { public Material mat; public string prop; public Color day; }
        readonly List<MatColor> windowGlow = new List<MatColor>();
        readonly List<MatColor> windowGlass = new List<MatColor>();   // half see-through: the bright backdrop shows through
        Light hallCeiling;
        readonly List<MatColor> lampShades = new List<MatColor>();
        readonly List<(Light light, float day)> sunFills = new List<(Light, float)>();
        readonly List<GameObject> sunShafts = new List<GameObject>();
        readonly List<Light> nightLights = new List<Light>();
        bool atmosphereReady;
        float lastNight = -1f;

        static readonly Color NightSky = new Color(0.035f, 0.05f, 0.12f, 1f);
        static readonly Color NightGlass = new Color(0.05f, 0.07f, 0.16f, 0.94f);
        static readonly Color LampWarm = new Color(1f, 0.72f, 0.42f, 1f);

        // lamps with a shade that should glow at night, and those that get a real light next to them
        static readonly string[] LampNames = { "Kitchen Lamp 1", "Kitchen Lamp 2", "Kitchen LittleLamp", "BigLamp", "FloorLamp", "RoomFloorLamp",
                                               "Bedroom TableLamp", "BedroomTableLamp", "BedroomFloorLamp 1", "BedroomFloorLamp 2", "MoonLamp",
                                               "LittleLamp 1", "LittleLamp 2", "Bedroom TumbLamp 1" };
        static readonly string[] LitLamps = { "Kitchen Lamp 1", "Kitchen Lamp 2", "BigLamp", "FloorLamp", "Bedroom TableLamp", "BedroomTableLamp", "MoonLamp" };

        void SetupAtmosphere()
        {
            var house = Util.FindPath("HouseGame Tamagotchi>House") ?? Util.FindPath("House>House");
            var lighting = Util.FindPath("HouseGame Tamagotchi>Lighting");
            if (house == null) { Plugin.Log.LogWarning("[night] no house"); return; }
            foreach (var r in house.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var n = r.name;
                if (n.StartsWith("Windows "))
                {
                    var mats = r.materials;   // instances
                    foreach (var m in mats)
                    {
                        if (m == null) continue;
                        if (DevRunner.Active) Plugin.Log.LogInfo($"[night] {n}: {m.name} ({m.shader?.name}) color {(m.HasProperty("_Color") ? m.GetColor("_Color").ToString() : "-")} emission {(m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString() : "-")} tex {(m.HasProperty("_MainTex") && m.mainTexture != null ? m.mainTexture.name : "-")} emap {(m.HasProperty("_EmissionMap") && m.GetTexture("_EmissionMap") != null ? m.GetTexture("_EmissionMap").name : "-")}");
                        if (m.HasProperty("_EmissionColor")) windowGlow.Add(new MatColor { mat = m, prop = "_EmissionColor", day = m.GetColor("_EmissionColor") });
                        if (m.name.Contains("Glass") && m.HasProperty("_Color")) windowGlass.Add(new MatColor { mat = m, prop = "_Color", day = m.GetColor("_Color") });
                    }
                }
                else if (System.Array.IndexOf(LampNames, n) >= 0)
                {
                    var m = r.material;
                    if (m != null && m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        lampShades.Add(new MatColor { mat = m, prop = "_EmissionColor", day = m.GetColor("_EmissionColor") });
                    }
                    if (System.Array.IndexOf(LitLamps, n) >= 0)
                    {
                        // a warm light hanging just below the shade; it lives under the lamp, so it goes off with the room
                        var go = new GameObject("PeaceLampLight");
                        go.transform.SetParent(r.transform, false);
                        // (the room may be switched off: take the mesh's own bounds, not the renderer's)
                        var mf = r.GetComponent<MeshFilter>();
                        var center = mf != null && mf.sharedMesh != null ? r.transform.TransformPoint(mf.sharedMesh.bounds.center) : r.transform.position;
                        go.transform.position = center + Vector3.down * 0.15f;
                        var l = go.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = LampWarm;
                        l.range = n.StartsWith("Kitchen") ? 5.5f : 4.5f;
                        l.intensity = 0f;
                        l.shadows = LightShadows.None;
                        l.enabled = false;
                        nightLights.Add(l);
                    }
                }
            }
            if (lighting != null)
            {
                foreach (var l in lighting.GetComponentsInChildren<Light>(true))
                    if (l != null && l.name.Contains("Sun")) sunFills.Add((l, l.intensity));
                foreach (var t in lighting.GetComponentsInChildren<Transform>(true))
                    if (t != null && t.name.StartsWith("Sun Particles")) sunShafts.Add(t.gameObject);
                // the hall's ceiling light (off in the story's daytime)
                var hc = Util.FindDeep(lighting.transform, "Main Lighting");
                var hl = hc != null ? Util.FindDeep(hc, "PointLight Realtime") : null;
                hallCeiling = hl != null ? hl.GetComponent<Light>() : null;
            }
            atmosphereReady = true;
            Plugin.Log.LogInfo($"[night] windows {windowGlow.Count} lamps {lampShades.Count} lamp lights {nightLights.Count} sun fills {sunFills.Count} sun shafts {sunShafts.Count}");
        }

        // 0 in daylight, 1 at night: dusk 17:30-20:30, dawn 5:00-7:00
        float Nightness()
        {
            float h = S.Minute / 60f;
            var sl = SL;   // (dawn and dusk move with the season)
            if (h >= sl.dawn1 && h < sl.dusk0) return 0f;
            if (h >= sl.dusk0 && h < sl.dusk1) return Mathf.SmoothStep(0f, 1f, (h - sl.dusk0) / (sl.dusk1 - sl.dusk0));
            if (h >= sl.dawn0 && h < sl.dawn1) return Mathf.SmoothStep(1f, 0f, (h - sl.dawn0) / (sl.dawn1 - sl.dawn0));
            return 1f;
        }

        void ApplyAtmosphere()
        {
            if (!atmosphereReady) return;
            float n = Nightness();
            // (the weather greys the day's sky: gloom 0 clear .. 0.6 rain; snow is a whiter grey)
            float key = n + gloom * 10f + Season * 100f;
            if (Mathf.Abs(key - lastNight) < 0.004f) return;
            lastNight = key;
            var overcast = weather == Weather.Snow ? new Color(0.92f, 0.95f, 1f) : new Color(0.62f, 0.66f, 0.74f);
            var sky = SL.skyTint;   // (the season's light outside: warmer in autumn, paler in winter, brighter in summer)
            foreach (var w in windowGlow)
                if (w.mat != null)
                {
                    var seasonDay = new Color(w.day.r * sky.r, w.day.g * sky.g, w.day.b * sky.b, w.day.a);
                    var day = Color.Lerp(seasonDay, overcast * w.day.maxColorComponent, gloom);
                    w.mat.SetColor(w.prop, Color.Lerp(day, NightSky * 0.5f, n));
                }
            // the glass turns into the night outside: nearly opaque dark blue over the bright backdrop behind it
            var gt = SL.glassTint;
            foreach (var g in windowGlass) if (g.mat != null) g.mat.SetColor(g.prop, Color.Lerp(new Color(g.day.r * gt.r, g.day.g * gt.g, g.day.b * gt.b, g.day.a), NightGlass, n));
            // lamps come on from the middle of dusk
            float lamp = Mathf.Clamp01((n - 0.35f) / 0.4f);
            foreach (var s in lampShades) if (s.mat != null) s.mat.SetColor(s.prop, Color.Lerp(s.day, LampWarm * 1.6f, lamp));
            foreach (var l in nightLights)
                if (l != null) { l.intensity = 1.1f * lamp; l.enabled = lamp > 0.01f; }
            foreach (var (l, day) in sunFills) if (l != null) { l.intensity = day * (1f - n) * (1f - gloom * 0.8f) * SL.sun; l.color = SL.sunTint; }
            if (hallCeiling != null)
            {
                bool on = lamp > 0.01f;
                if (on && !hallCeiling.gameObject.activeSelf) hallCeiling.gameObject.SetActive(true);
                if (on) { hallCeiling.color = Color.Lerp(Color.white, LampWarm, 0.5f); hallCeiling.intensity = 0.9f * lamp; }
                else if (hallCeiling.gameObject.activeSelf) hallCeiling.gameObject.SetActive(false);
            }
            bool shafts = n < 0.5f && gloom < 0.2f;
            foreach (var g in sunShafts) if (g != null && g.activeSelf != shafts) g.SetActive(shafts);
        }
    }
}
