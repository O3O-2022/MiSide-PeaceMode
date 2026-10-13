using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace MiSidePeaceMode
{
    // Music by the time of day, the way the official (unreleased) peace mode meant it (Location21_World's
    // musicMorning/Day/Evening/Night). Its own tracks never shipped, so the house's two calm tracks take turns:
    // Scene 3's day music in the morning, the kitchen's in the day, Scene 3's a little slower in the evening and the
    // kitchen's slowed down and quiet at night (the chapter's opening track is a 19 s loop: too short for hours).
    // The story keeps this source at 0.08 until the TV game, 0.3 after it.
    // It plays on the scene's "Music 2" (the kitchen music the story starts when you walk in), through its own
    // Audio_Volume and Audio_Pitch (they ease towards what they're given). A new track fades out and in.
    public partial class PeaceDirector
    {
        class Track { public string clip, fallback; public float volume, pitch; }
        static readonly Track MusicMorning = new Track { clip = "Music Ambient Scene", fallback = "Music Ambient Day", volume = 0.15f, pitch = 1f };
        static readonly Track MusicDay = new Track { clip = "Music Ambient Day", volume = 0.15f, pitch = 1f };
        static readonly Track MusicEvening = new Track { clip = "Music Ambient Scene", fallback = "Music Ambient Day", volume = 0.12f, pitch = 0.9f };
        static readonly Track MusicNight = new Track { clip = "Music Ambient Day", volume = 0.07f, pitch = 0.8f };

        AudioSource music;
        Audio_Volume musicVol;
        Audio_Pitch musicPitch;
        readonly Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> gameMusic = new List<AudioSource>();
        bool musicFading;
        float musicSwapBy, nextMusicCheck, musicVolNow = -1f, musicPitchNow = -1f;

        void SetupMusic()
        {
            var go = Util.FindPath("Music 2");
            music = go != null ? go.GetComponent<AudioSource>() : null;
            musicVol = go != null ? go.GetComponent<Audio_Volume>() : null;
            musicPitch = go != null ? go.GetComponent<Audio_Pitch>() : null;
            if (music == null || musicVol == null) { Plugin.Log.LogWarning("[music] no Music 2"); music = null; return; }
            // the TV game's own music (on the persistent "Game" objects)
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioSource>()))
            {
                var a = o.Cast<AudioSource>();
                if (a != null && a.clip != null && a.clip.name == "MusicGame") gameMusic.Add(a);
            }
            Plugin.Log.LogInfo($"[music] Music 2: clip {(music.clip != null ? music.clip.name : "-")} vol {music.volume:0.00} (target {musicVol.volume:0.00}, music setting {musicVol.volumeMusic:0.00}) pitch {music.pitch:0.00}");
        }

        // by name, among the clips in memory (Scene 3's stays after its scene goes, see KeepSceneClips)
        AudioClip FindAudio(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (audioClips.TryGetValue(name, out var c) && c != null) return c;
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>()))
            {
                if (o == null || o.name != name) continue;
                c = o.Cast<AudioClip>();
                c.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                audioClips[name] = c;
                return c;
            }
            return null;
        }

        Track TrackNow()
        {
            float h = S.Minute / 60f;
            if (h >= 6f && h < 11f) return MusicMorning;
            if (h >= 11f && h < 17f) return MusicDay;
            if (h >= 17f && h < 21f) return MusicEvening;
            return MusicNight;
        }

        // quiet while the TV game plays its own music
        public static bool DevMusicOff;

        bool MusicHushed()
        {
            if (musicHush || DevMusicOff) return true;   // she is singing (dev: a video with its own music)
            foreach (var a in gameMusic) if (a != null && a.isPlaying) return true;
            return false;
        }

        void TickMusic()
        {
            if (music == null || !freeMode || Time.time < nextMusicCheck) return;
            nextMusicCheck = Time.time + 0.5f;
            var t = TrackNow();
            var clip = FindAudio(t.clip) ?? FindAudio(t.fallback);
            if (clip == null) return;
            if (!music.gameObject.activeSelf) music.gameObject.SetActive(true);
            float vol = MusicHushed() ? 0f : musicBoost ? 0.35f : t.volume;   // (louder while the two of you listen at the recorder)
            if (music.clip != clip)
            {
                // fade out, then the new track fades in
                if (!musicFading) { musicFading = true; musicSwapBy = Time.time + 5f; SetMusic(0f, musicPitchNow < 0 ? t.pitch : musicPitchNow); }
                if (music.volume > 0.004f && Time.time < musicSwapBy) return;
                music.clip = clip;
                music.loop = true;
                music.Play();
                musicFading = false;
                Plugin.Log.LogInfo($"[music] {clip.name} ({clip.length:0}s) at {(int)(S.Minute / 60)}:{(int)(S.Minute % 60):00}, volume {vol:0.00} pitch {t.pitch:0.00}");
            }
            SetMusic(vol, t.pitch);
            if (!music.isPlaying && music.isActiveAndEnabled) music.Play();
        }

        void SetMusic(float vol, float pitch)
        {
            if (vol != musicVolNow) { musicVolNow = vol; try { musicVol.Volume(vol); } catch { } }
            if (pitch != musicPitchNow)
            {
                musicPitchNow = pitch;
                try { if (musicPitch != null) musicPitch.Pitch(pitch); else music.pitch = pitch; } catch { }
            }
        }

        public string DevMusic() => music == null ? "no music" :
            $"clip {(music.clip != null ? music.clip.name : "-")} playing {music.isPlaying} vol {music.volume:0.000} (target {musicVol.volume:0.00}, setting {musicVol.volumeMusic:0.00}) pitch {music.pitch:0.00} fading {musicFading}";
    }
}
