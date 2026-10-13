using System;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace MiSidePeaceMode
{
    // The game's final sound, as it leaves the AudioListener (dev only: the showcase videos). Unity calls
    // OnAudioFilterRead on the audio thread with each mixed block; while capturing, the blocks are appended to a buffer.
    public class AudioTap : MonoBehaviour
    {
        public AudioTap(IntPtr ptr) : base(ptr) { }

        static readonly object gate = new object();
        static float[] buffer = new float[0];
        static long count;           // floats written
        public static bool Capturing;
        public static int Channels;
        public static long Blocks;

        public static void Reset()
        {
            lock (gate) { count = 0; Blocks = 0; }
        }

        public static long Samples { get { lock (gate) return Channels > 0 ? count / Channels : 0; } }

        public static volatile int Calls;
        public static volatile float LastPeak;

        void OnAudioFilterRead(Il2CppStructArray<float> data, int channels)
        {
            Calls++;
            if (data == null) return;
            if (Calls % 50 == 1) { float p = 0f; for (int i = 0; i < data.Length; i++) p = Math.Max(p, Math.Abs(data[i])); LastPeak = p; }
            if (!Capturing) return;
            int n = data.Length;
            lock (gate)
            {
                Channels = channels;
                if (count + n > buffer.Length) Array.Resize(ref buffer, (int)Math.Max(buffer.Length * 2L, count + n + 48000 * 8));
                for (int i = 0; i < n; i++) buffer[count + i] = data[i];
                count += n;
                Blocks++;
            }
        }

        public static float Peak()
        {
            lock (gate) { float p = 0f; for (long i = 0; i < count; i++) p = Math.Max(p, Math.Abs(buffer[i])); return p; }
        }

        public static void WriteWav(string path, int rate)
        {
            lock (gate)
            {
                using var f = new FileStream(path, FileMode.Create, FileAccess.Write);
                using var w = new BinaryWriter(f);
                int ch = Math.Max(1, Channels);
                long bytes = count * 2;
                w.Write("RIFF".ToCharArray()); w.Write((int)(36 + bytes)); w.Write("WAVE".ToCharArray());
                w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate);
                w.Write(rate * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
                w.Write("data".ToCharArray()); w.Write((int)bytes);
                for (long i = 0; i < count; i++) w.Write((short)(Mathf.Clamp(buffer[i], -1f, 1f) * 32767f));
            }
        }

        static GameObject own;

        // our own listener (the game's switched off), the tap right after it on the same object, following the camera
        public static AudioTap Attach()
        {
            var listener = UnityEngine.Object.FindObjectOfType<AudioListener>();
            if (listener == null) { Plugin.Log.LogWarning("[dev] audio tap: no listener"); return null; }
            if (own != null && own.GetComponent<AudioListener>() == listener) return own.GetComponent<AudioTap>();
            own = new GameObject("PeaceAudioTap");
            own.transform.SetParent(listener.transform, false);
            listener.enabled = false;
            own.AddComponent<AudioListener>();
            var tap = own.AddComponent<AudioTap>();
            Plugin.Log.LogInfo($"[dev] audio tap: own listener under {listener.name}");
            return tap;
        }
    }
}
