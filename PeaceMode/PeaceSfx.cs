using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiSidePeaceMode
{
    // The UI's small sounds (2026-10): the game's own short clicks and pops, quiet, scaled by the game's sound volume
    // (its mixer group by name was a silent "Master": no group). Off in Settings > 효과음.
    public static class PeaceSfx
    {
        public const string Move = "SoftClock 1", Pick = "Click", Back = "Button 2D Up", Refuse = "Button 2D Down",
                            Pop = "DialogueSymbolPop", Page = "ItemSwitch", Open = "Click Case";

        static AudioSource src;
        static Dictionary<string, AudioClip> clips;
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        public static void Init(GameObject host)
        {
            if (host == null) return;
            src = host.GetComponent<AudioSource>() ?? host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = true;
            src.priority = 40;
        }

        static AudioClip Find(string name)
        {
            if (clips == null)
            {
                clips = new Dictionary<string, AudioClip>();
                foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<AudioClip>()))
                {
                    var c = o.TryCast<AudioClip>();
                    if (c != null && !clips.ContainsKey(c.name)) clips[c.name] = c;
                }
            }
            if (clips.TryGetValue(name, out var clip) && clip != null) return clip;
            clips = null;   // (a scene since may have brought it: look again next time)
            return null;
        }

        // the game's sound volume (its options, 0..1); full when it can't be read
        static float GameVolume()
        {
            try { float v = GlobalGame.VolumeGame; return v > 1.01f ? Mathf.Clamp01(v / 100f) : Mathf.Clamp01(v); } catch { return 1f; }
        }

        public static void Play(string name, float volume = 0.3f, float pitch = 1f)
        {
            if (src == null || !PeaceMode.UiSounds.Value || string.IsNullOrEmpty(name)) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(name, out var t) && now - t < 0.045f) return;   // (a held key: not a buzz)
            lastPlayed[name] = now;
            var clip = Find(name);
            if (DevRunner.Active) Plugin.Log.LogInfo($"[sfx] {name} clip={(clip != null ? clip.loadState.ToString() : "none")} src={src.isActiveAndEnabled} vol={volume} listenerVol={AudioListener.volume} game={GameVolume()}");
            if (clip == null) return;
            try
            {
                src.pitch = pitch;
                src.PlayOneShot(clip, volume * GameVolume());
            }
            catch (Exception e) { Plugin.Log.LogWarning("[sfx] " + name + ": " + e.Message); }
        }
    }
}
