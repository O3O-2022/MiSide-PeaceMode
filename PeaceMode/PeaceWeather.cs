using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // Weather and the seasons. The season is the real one (the calendar); every in-game day draws its weather from it -
    // clear, cloudy, rain, and snow in winter - and it may turn at 13:00. Rain and snow fall outside the windows
    // (particles between the glass and the bright sky behind it), the sky in the windows greys over and the sunbeams
    // go (PeaceAtmosphere), rain patters (a sound made here: filtered noise with drops, a little muffled by the walls),
    // and she talks about it: in the morning, when it turns, now and then instead of a line to herself, and about the
    // season (Personas.WeatherTalk).
    public enum Weather { Clear, Cloudy, Rain, Snow }

    public partial class PeaceDirector
    {
        Weather weather = Weather.Clear;
        float gloom;                    // 0 clear .. 1 (eases toward the weather's own)
        readonly List<ParticleSystem> weatherFx = new List<ParticleSystem>();
        AudioSource rainAudio;
        bool weatherFxReady, weatherTurned, weatherTurnChecked;
        int weatherTurnDay = -1;

        static int Season
        {
            get
            {
                if (devSeason >= 0) return devSeason;
                int m = DateTime.Now.Month;
                return m >= 3 && m <= 5 ? 0 : m >= 6 && m <= 8 ? 1 : m >= 9 && m <= 11 ? 2 : 3;
            }
        }

        // the season's light: dawn and dusk (hours), the sun's strength and colour, the colour of the sky in the windows
        struct SeasonLight { public float dawn0, dawn1, dusk0, dusk1, sun; public Color sunTint, skyTint, glassTint; }
        static readonly SeasonLight[] SeasonLights =
        {
            // (the window sky is a painted emission map at ~0.5: a tint shows as long as it's a real shift, not a hint)
            new SeasonLight { dawn0 = 5f, dawn1 = 7f, dusk0 = 17.5f, dusk1 = 20.5f, sun = 1f,                                   // spring: soft, a little pink
                              sunTint = new Color(1f, 0.92f, 0.92f), skyTint = new Color(1.02f, 0.84f, 0.93f), glassTint = new Color(1f, 0.9f, 0.96f) },
            new SeasonLight { dawn0 = 4.5f, dawn1 = 6.5f, dusk0 = 18.5f, dusk1 = 21.5f, sun = 1.25f,                           // summer: long bright days
                              sunTint = new Color(1.1f, 1.04f, 0.84f), skyTint = new Color(1.25f, 1.2f, 1f), glassTint = new Color(1f, 1.02f, 1.05f) },
            new SeasonLight { dawn0 = 5.5f, dawn1 = 7.5f, dusk0 = 17f, dusk1 = 20f, sun = 0.9f,                               // autumn: golden
                              sunTint = new Color(1.04f, 0.85f, 0.64f), skyTint = new Color(1.08f, 0.8f, 0.55f), glassTint = new Color(1f, 0.86f, 0.7f) },
            new SeasonLight { dawn0 = 6.5f, dawn1 = 8.5f, dusk0 = 16f, dusk1 = 19f, sun = 0.7f,                               // winter: short, pale, cool
                              sunTint = new Color(0.82f, 0.9f, 1.08f), skyTint = new Color(0.66f, 0.79f, 1.08f), glassTint = new Color(0.8f, 0.88f, 1.05f) },
        };
        static int devSeason = -1;
        static SeasonLight SL => SeasonLights[devSeason >= 0 ? devSeason : Season];

        static float GloomOf(Weather w) => w switch { Weather.Cloudy => 0.35f, Weather.Rain => 0.6f, Weather.Snow => 0.3f, _ => 0f };

        static Weather Draw(Weather? not = null)
        {
            for (int i = 0; i < 8; i++)
            {
                float r = Random.value;
                var w = Season switch
                {
                    0 => r < 0.5f ? Weather.Clear : r < 0.75f ? Weather.Cloudy : Weather.Rain,
                    1 => r < 0.45f ? Weather.Clear : r < 0.65f ? Weather.Cloudy : Weather.Rain,
                    2 => r < 0.5f ? Weather.Clear : r < 0.8f ? Weather.Cloudy : Weather.Rain,
                    _ => r < 0.3f ? Weather.Clear : r < 0.6f ? Weather.Cloudy : Weather.Snow,
                };
                if (w != not) return w;
            }
            return not == Weather.Clear ? Weather.Cloudy : Weather.Clear;
        }

        void TickWeather()
        {
            // a new day: a new draw (kept in the save, so coming back the same day it's still raining)
            if (S.WeatherDay != S.Day || string.IsNullOrEmpty(S.Weather) || !Enum.TryParse(S.Weather, out weather))
            {
                weather = Draw();
                S.Weather = weather.ToString();
                S.WeatherDay = S.Day;
                weatherTurnChecked = false;
                Plugin.Log.LogInfo($"[weather] day {S.Day}: {weather} (season {Season})");
            }
            // it may turn once, around 13:00
            if (weatherTurnDay != S.Day) { weatherTurnDay = S.Day; weatherTurnChecked = S.Minute >= 13 * 60; }
            if (!weatherTurnChecked && S.Minute >= 13 * 60)
            {
                weatherTurnChecked = true;
                if (Random.value < 0.3f)
                {
                    weather = Draw(weather);
                    S.Weather = weather.ToString();
                    weatherTurned = true;   // (she remarks on it when she's free)
                    Plugin.Log.LogInfo("[weather] turned: " + weather);
                }
            }
            gloom = Mathf.MoveTowards(gloom, GloomOf(weather), Time.deltaTime / 20f);
            TickWeatherFx();
        }

        // ---- rain and snow outside the windows ----
        // (the game's build stripped most of the particle system's settings; what's left is Emit with each
        // particle's own position, speed, life, size and colour - so every drop is sent off by hand)

        class WeatherWindow { public ParticleSystem ps; public Vector3 size; public float carry; }
        readonly List<WeatherWindow> weatherWins = new List<WeatherWindow>();

        void SetupWeatherFx()
        {
            if (weatherFxReady) return;
            weatherFxReady = true;
            var house = Util.FindPath("HouseGame Tamagotchi>House") ?? Util.FindPath("House>House");
            if (house == null) return;
            // (a particle look the game already has: the sunbeams' soft sprite)
            // (a soft round particle the game already has: Unity's default one, else the dust motes)
            Material look = null;
            foreach (var r in house.transform.root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                var m = r != null ? r.sharedMaterial : null;
                if (m == null) continue;
                if (m.name.StartsWith("Default-Particle")) { look = m; break; }
                if (m.name.Contains("Dust") || look == null) look = m;
            }
            var inside = Vector3.zero; int n = 0;
            var wins = new List<(Vector3 c, Vector3 size, Transform t)>();
            foreach (var r in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null || !r.name.StartsWith("Windows ")) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                var c = r.transform.TransformPoint(b.center);
                var size = r.transform.TransformVector(b.size);
                size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                wins.Add((c, size, r.transform));
                inside += c; n++;
            }
            if (n == 0) return;
            inside /= n;
            // (what the house looks like to this code: the windows' boxes, the house's, the particle looks there are)
            var hb = new Bounds(house.transform.position, Vector3.zero);
            foreach (var r in house.GetComponentsInChildren<MeshRenderer>(true))
                if (r != null && (r.name.StartsWith("Room") || r.name.StartsWith("Wall"))) { var mf2 = r.GetComponent<MeshFilter>(); if (mf2 != null && mf2.sharedMesh != null) { var b2 = mf2.sharedMesh.bounds; hb.Encapsulate(r.transform.TransformPoint(b2.min)); hb.Encapsulate(r.transform.TransformPoint(b2.max)); } }
            Plugin.Log.LogInfo($"[weather] house walls {hb.min} .. {hb.max}; windows: {string.Join("; ", wins.Select(w => $"{w.t.name} {w.c} size {w.size}"))}");
            var looks = new HashSet<string>();
            foreach (var r in house.transform.root.GetComponentsInChildren<ParticleSystemRenderer>(true)) if (r != null && r.sharedMaterial != null) looks.Add(r.name + ":" + r.sharedMaterial.name);
            Plugin.Log.LogInfo("[weather] particle looks: " + string.Join(", ", looks));
            foreach (var (c, size, t) in wins)
            {
                // each side of the window's box: outside is where there's no floor to walk on just beyond it
                // (the kitchen's windows are one mesh over two walls)
                var half = size * 0.5f;
                foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    float reach = Mathf.Abs(Vector3.Dot(half, dir));
                    var probe = new Vector3(c.x, 0.1f, c.z) + dir * (reach + 0.7f);
                    var inward = new Vector3(c.x, 0.1f, c.z) - dir * Mathf.Max(0.7f - reach, 0.3f);
                    bool floorOut = UnityEngine.AI.NavMesh.SamplePosition(probe, out _, 0.45f, -1);
                    bool floorIn = UnityEngine.AI.NavMesh.SamplePosition(inward, out _, 1.2f, -1);
                    if (floorOut || !floorIn) continue;
                    float along = Mathf.Abs(dir.x) > 0 ? size.z : size.x;
                    if (along < 0.8f) continue;   // (a pane's own edge inside the wall)
                    var go = new GameObject("PeaceWeather " + t.name + " " + dir);
                    go.transform.SetParent(t, true);
                    // just inside the mesh's outer edge: between the glass and the painted sky behind it
                    go.transform.SetPositionAndRotation(new Vector3(c.x, c.y, c.z) + dir * (reach - 0.08f), Quaternion.identity);
                    var ps = go.AddComponent<ParticleSystem>();
                    var em = ps.emission;
                    em.rateOverTime = 0f;   // (only what we emit)
                    var pr = go.GetComponent<ParticleSystemRenderer>();
                    if (look != null) pr.sharedMaterial = look;
                    // the fall's box, in world axes: along the wall, the window's height, a little depth
                    var box = Mathf.Abs(dir.x) > 0 ? new Vector3(0.06f, size.y + 0.4f, along) : new Vector3(along, size.y + 0.4f, 0.06f);
                    weatherWins.Add(new WeatherWindow { ps = ps, size = box });
                    weatherFx.Add(ps);
                    Plugin.Log.LogInfo($"[weather] rain/snow outside {t.name} on its {dir} side ({along:0.0} m)");
                }
            }
            Plugin.Log.LogInfo($"[weather] {weatherFx.Count} windows, particle look {(look != null ? look.name : "-")}");
        }

        Weather fxShown = (Weather)(-1);
        public static bool WeatherDebug;   // (dev: weather debug -> big red rain, to see where it falls)

        void TickWeatherFx()
        {
            if (!weatherFxReady) SetupWeatherFx();
            if (weather != fxShown)
            {
                fxShown = weather;
                foreach (var w in weatherWins)
                {
                    if (w.ps == null) continue;
                    var pr = w.ps.GetComponent<ParticleSystemRenderer>();
                    try
                    {
                        if (weather == Weather.Rain) { pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = 0.04f; pr.lengthScale = 2f; }
                        else pr.renderMode = ParticleSystemRenderMode.Billboard;
                    }
                    catch (Exception e) { Plugin.Log.LogWarning("[weather] renderer: " + e.Message); }
                }
                Plugin.Log.LogInfo("[weather] showing " + weather);
            }
            if (weather == Weather.Rain || weather == Weather.Snow)
            {
                bool rain = weather == Weather.Rain;
                float perSec = rain ? 260f : 55f;
                float dark = Mathf.Lerp(1f, 0.45f, Nightness());
                foreach (var w in weatherWins)
                {
                    if (w.ps == null || !w.ps.gameObject.activeInHierarchy) continue;
                    w.carry += perSec * Time.deltaTime * Mathf.Max(0.5f, (w.size.x + w.size.z) / 3f);
                    int count = Mathf.Min((int)w.carry, 40);
                    w.carry -= count;
                    for (int i = 0; i < count; i++)
                    {
                        var pos = new Vector3((Random.value - 0.5f) * w.size.x, w.size.y * 0.5f, (Random.value - 0.5f) * w.size.z);
                        Vector3 vel; float life, size; Color col;
                        if (rain)
                        {
                            vel = new Vector3(Random.Range(-0.3f, 0.3f), -Random.Range(7f, 9f), Random.Range(-0.3f, 0.3f));
                            life = w.size.y / 8f + 0.1f;
                            size = Random.Range(0.028f, 0.04f);
                            col = new Color(0.36f * dark, 0.41f * dark, 0.52f * dark, 0.8f);   // (grey streaks: the sky behind is bright, the glass dims them)
                            if (WeatherDebug) { size = 0.12f; col = Color.red; }
                        }
                        else
                        {
                            vel = new Vector3(Random.Range(-0.25f, 0.25f), -Random.Range(0.45f, 0.8f), Random.Range(-0.25f, 0.25f));
                            life = w.size.y / 0.6f;
                            size = Random.Range(0.05f, 0.09f);
                            col = new Color(0.7f * dark, 0.74f * dark, 0.84f * dark, 1f);
                        }
                        var ep = new ParticleSystem.EmitParams
                        {
                            m_Particle = new ParticleSystem.Particle(),
                            m_PositionSet = true, m_VelocitySet = true, m_StartLifetimeSet = true, m_StartSizeSet = true, m_StartColorSet = true,
                        };
                        ep.m_Particle.m_Position = pos;
                        ep.m_Particle.m_Velocity = vel;
                        ep.m_Particle.m_StartLifetime = life;
                        ep.m_Particle.m_Lifetime = life;
                        ep.m_Particle.m_StartSize = new Vector3(size, size, size);
                        ep.m_Particle.m_StartColor = col;
                        w.ps.Emit(ep, 1);
                    }
                }
            }
            // the patter of rain, fading with it
            float rainVol = weather == Weather.Rain ? 0.22f : 0f;
            if (rainAudio == null && rainVol > 0f && !rainAudioTried)
            {
                rainAudioTried = true;
                try { rainAudio = MakeRainAudio(); } catch (Exception e) { Plugin.Log.LogWarning("[weather] rain sound: " + e.Message); }
            }
            if (rainAudio != null)
            {
                float vol = 1f;
                try { vol = GlobalGame.VolumeGame; } catch { }
                rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, rainVol * vol, Time.deltaTime * 0.05f);
                if (rainAudio.volume > 0.001f && !rainAudio.isPlaying) rainAudio.Play();
                else if (rainAudio.volume <= 0.001f && rainAudio.isPlaying) rainAudio.Stop();
            }
        }

        bool rainAudioTried;

        // Rain on the roof: a sound of the game's own that is rain, water or wind if one is loaded; otherwise ten
        // seconds made here (low rumble, hiss, drops; the end fading into the start) - if this build lets us fill a clip.
        AudioSource MakeRainAudio()
        {
            AudioClip found = null;
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<AudioClip>()))
            {
                var c = o?.Cast<AudioClip>();
                if (c == null || c.length < 3f) continue;
                var n = c.name.ToLowerInvariant();
                if (n.Contains("rain") || n.Contains("дожд")) { found = c; break; }
                if (found == null && (n.Contains("shower") || n.Contains("wind") || n.Contains("ветер"))) found = c;
            }
            if (found != null) { Plugin.Log.LogInfo($"[weather] rain sound: {found.name} ({found.length:0.0}s)"); return NewRainSource(found); }
            return NewRainSource(SynthRain());
        }

        AudioSource NewRainSource(AudioClip clip)
        {
            var go = new GameObject("PeaceRainSound");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.playOnAwake = false;
            var lp = go.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = 3200f;   // (heard from inside)
            return src;
        }

        AudioClip SynthRain()
        {
            const int rate = 44100;
            int len = rate * 10;
            var data = new float[len * 2];
            var rnd = new System.Random(7);
            float brownL = 0, brownR = 0, lpL = 0, lpR = 0;
            var drops = new List<(int at, float amp, float freq, bool left)>();
            for (int i = 0; i < len; i++) if (rnd.NextDouble() < 0.0009) drops.Add((i, 0.05f + (float)rnd.NextDouble() * 0.2f, 1800f + (float)rnd.NextDouble() * 2400f, rnd.NextDouble() < 0.5));
            for (int i = 0; i < len; i++)
            {
                float wl = (float)(rnd.NextDouble() * 2 - 1), wr = (float)(rnd.NextDouble() * 2 - 1);
                brownL = (brownL + wl * 0.02f) * 0.997f; brownR = (brownR + wr * 0.02f) * 0.997f;
                lpL += 0.25f * (wl - lpL); lpR += 0.25f * (wr - lpR);
                data[i * 2] = brownL * 1.6f + lpL * 0.22f;
                data[i * 2 + 1] = brownR * 1.6f + lpR * 0.22f;
            }
            foreach (var (at, amp, freq, left) in drops)
            {
                int n = 700;
                for (int k = 0; k < n && at + k < len; k++)
                {
                    float v = amp * Mathf.Exp(-k / 90f) * Mathf.Sin(2 * Mathf.PI * freq * k / rate);
                    data[(at + k) * 2 + (left ? 0 : 1)] += v;
                    data[(at + k) * 2 + (left ? 1 : 0)] += v * 0.4f;
                }
            }
            // loop seam: cross-fade the last half second into the first
            int fade = rate / 2;
            for (int i = 0; i < fade; i++)
            {
                float t = (float)i / fade;
                for (int ch = 0; ch < 2; ch++)
                {
                    int a = i * 2 + ch, b = (len - fade + i) * 2 + ch;
                    data[a] = data[a] * t + data[b] * (1 - t);
                }
            }
            var clip = AudioClip.Create("PeaceRain", len - fade, 2, rate, false);
            var trimmed = new float[(len - fade) * 2];
            Array.Copy(data, 0, trimmed, 0, trimmed.Length);
            clip.SetData(trimmed, 0);
            return clip;
        }

        void DestroyWeather()
        {
            if (rainAudio != null) UnityEngine.Object.Destroy(rainAudio.gameObject);
            rainAudio = null;
        }

        // ---- what she says about it ----

        WeatherTalk WT => Personas.WeatherLines(Pn.Id);

        // her morning greeting gets a word about the day's weather now and then
        Line[] WeatherMorning() => Random.value < 0.6f && WT.Morning.TryGetValue(weather, out var l) ? Pick(l) : new Line[0];

        // instead of a line to herself: the weather when it just turned, else sometimes the weather or the season
        bool TryWeatherTalk()
        {
            if (weatherTurned && WT.Turn.TryGetValue(weather, out var turn))
            {
                weatherTurned = false;
                Play(Pick(turn), null, true);
                return true;
            }
            if (Random.value > 0.18f) return false;
            if (Random.value < 0.35f) { Play(Pick(WT.Season[Season]), null, true); return true; }
            if (WT.Idle.TryGetValue(weather, out var idle)) { Play(Pick(idle), null, true); return true; }
            return false;
        }

        public void DevWeather(string w)
        {
            if (w == "debug") { WeatherDebug = !WeatherDebug; return; }
            if (w.StartsWith("season ")) { devSeason = int.Parse(w.Substring(7)); Plugin.Log.LogInfo("[dev] season " + devSeason); return; }   // weather season 0..3 (-1 = the real one)
            if (Enum.TryParse<Weather>(w, true, out var v)) { weather = v; S.Weather = v.ToString(); S.WeatherDay = S.Day; weatherTurned = true; }
            Plugin.Log.LogInfo($"[dev] weather {weather}, gloom {gloom:0.00} -> {GloomOf(weather)}");
        }
    }

    public class WeatherTalk
    {
        public Dictionary<Weather, Line[][]> Morning;   // a word about the day's weather after her morning greeting
        public Dictionary<Weather, Line[][]> Turn;      // it just turned
        public Dictionary<Weather, Line[][]> Idle;      // now and then
        public Line[][][] Season;                        // spring, summer, autumn, winter
    }
}
