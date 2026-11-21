#if DEVTOOLS && !DEVREC
using UnityEngine;

namespace MiSidePeaceMode
{
    // (dev builds without the video/audio recorders - define DEVREC for them: this PC's antivirus takes a build
    // carrying them for a remote-access trojan, so normal test builds leave them out)
    public static class AvRecorder
    {
        public static string FfmpegPath, Encoder = "auto", MetronomeClip = "";
        public static int Msaa = 1, MetronomeEvery;
        public static float FrameSeconds = 0.3f, Slow = 1f;
        public static bool Active => false;
        public static void Stage(string name) { }
        public static bool Start(string path, int fps, int w, int h) { Plugin.Log.LogWarning("[dev] recordav: built without DEVREC"); return false; }
        public static void SlowAll() { }
        public static void Frame(Texture2D tex) { }
        public static void Stop() { }
    }

    public class AudioTap
    {
        public static bool Capturing;
        public static int Channels;
        public static long Blocks, Samples;
        public static int Calls;
        public static float LastPeak;
        public static void Reset() { }
        public static float Peak() => 0f;
        public static void WriteWav(string path, int rate) { }
        public static AudioTap Attach() { Plugin.Log.LogWarning("[dev] audiotap: built without DEVREC"); return null; }
    }
}
#endif
