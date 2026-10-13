using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MiSidePeaceMode
{
    // Video with the game's own sound, for the showcase videos (dev only). The frames go straight into ffmpeg (no frame
    // files) and the game runs on frame time (Time.captureFramerate), so the picture is smooth however slowly the PC
    // renders. Unity's offline AudioRenderer is inert in this build (silence) and clip data can't be read either, so the
    // sound is taken live at the listener (AudioTap) while the game's sound is slowed down to match: every frame is
    // held to exactly T real seconds and every AudioSource plays at pitch / F (F = T x fps), so in real time the game's
    // sound runs F times slower, in step with the frames. Afterwards tools/video/fix_audio.py speeds it up F times
    // (pitch back to normal), using the per-frame sample positions written here to cut out any frame that overran T.
    // `recordav <name> <seconds> [fps] [w] [h]` writes BepInEx/config/video/<name>.mp4, <name>.raw.wav, <name>.sync.txt.
    static class AvRecorder
    {
        public static string FfmpegPath;
        // the encoder of the clips (re-encoded later anyway): nvenc | qsv | x264 | auto (by the graphics card)
        public static string Encoder = "auto";
        public static int Msaa = 8;                // anti-aliasing of the frames (the game's own screen has none at this size)
        public static float FrameSeconds = 0.3f;   // T: real time per recorded frame (above the slowest frame's work)

        static string FindFfmpeg()
        {
            var cands = new List<string>();
            try
            {
                // the one this repo keeps in dev_output/ffmpeg (the repo being inside the game folder)
                var root = Path.Combine(BepInEx.Paths.GameRootPath, "MiSide-KR-PeaceMode", "dev_output", "ffmpeg");
                if (Directory.Exists(root))
                    foreach (var d in Directory.GetDirectories(root)) cands.Add(Path.Combine(d, "bin", "ffmpeg.exe"));
                // a winget install (its links folder is on PATH only for shells started after it)
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                cands.Add(Path.Combine(local, "Microsoft", "WinGet", "Links", "ffmpeg.exe"));
                var pk = Path.Combine(local, "Microsoft", "WinGet", "Packages");
                if (Directory.Exists(pk))
                    foreach (var d in Directory.GetDirectories(pk, "Gyan.FFmpeg*"))
                        foreach (var b in Directory.GetDirectories(d)) cands.Add(Path.Combine(b, "bin", "ffmpeg.exe"));
                foreach (var d in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                    if (d.Trim().Length > 0) cands.Add(Path.Combine(d.Trim(), "ffmpeg.exe"));
            }
            catch { }
            foreach (var c in cands) { try { if (File.Exists(c)) return c; } catch { } }
            return null;
        }
        static readonly List<float> work = new List<float>();
        static readonly Dictionary<string, double> stageAt = new Dictionary<string, double>();
        static readonly StringBuilder slowLog = new StringBuilder();
        static double stageMark;
        static int gcWas;

        // dev: where a frame's time goes (logged for the slow ones)
        public static void Stage(string name)
        {
            if (proc == null) return;
            double now = clock.Elapsed.TotalSeconds;
            stageAt[name] = now - (name == "game" ? frameStart : stageMark);
            stageMark = now;
        }   // each frame's own time (render, capture, game), for the log
        static double frameStart;

        static Process proc;
        static Stream video;
        static int width, height, fps;
        static byte[] pixels;
        static string basePath;
        public static bool Active => proc != null;

        // the slowdown
        public static float Slow = 1f;             // F
        // dev: a click every N frames during the recording (a sync check: they must land every N / fps seconds)
        public static int MetronomeEvery;
        public static string MetronomeClip = "ConsoleCheat";
        static AudioSource metronome;
        static AudioClip metronomeClip;
        static readonly Dictionary<int, (float set, float orig)> pitches = new Dictionary<int, (float, float)>();
        static readonly Stopwatch clock = new Stopwatch();
        static long frameNo;
        static double lastDue;
        static readonly List<long> marks = new List<long>();   // tap sample count at each frame's end
        static readonly List<float> overruns = new List<float>();

        public static bool Start(string path, int framesPerSecond, int w, int h)
        {
            if (string.IsNullOrEmpty(FfmpegPath) || !File.Exists(FfmpegPath)) FfmpegPath = FindFfmpeg();
            if (string.IsNullOrEmpty(FfmpegPath) || !File.Exists(FfmpegPath)) { Plugin.Log.LogWarning("[dev] recordav: no ffmpeg (use `ffmpeg <path>`)"); return false; }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            basePath = path;
            width = w; height = h; fps = framesPerSecond;
            pixels = new byte[w * h * 3];
            // (a hardware encoder when there is one: software x264 at 1080p60 held every frame up; the file is re-encoded later anyway)
            string which = Encoder;
            if (which == "auto")
            {
                var gpu = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
                which = gpu.Contains("nvidia") || gpu.Contains("geforce") || gpu.Contains("rtx") ? "nvenc" : gpu.Contains("intel") ? "qsv" : "x264";
            }
            // (near-lossless intermediates; HEVC above 1080p: H.264 NVENC stops at 4096 wide)
            var enc = which == "nvenc" ? (w > 2560 ? "-c:v hevc_nvenc -preset p4 -rc constqp -qp 12" : "-c:v h264_nvenc -preset p4 -rc constqp -qp 12")
                    : which == "qsv" ? "-c:v h264_qsv -preset medium -global_quality 16"
                    : "-c:v libx264 -preset veryfast -crf 14";
            var args = $"-y -loglevel error -f rawvideo -pix_fmt rgb24 -s {w}x{h} -r {fps} -i - -vf vflip,format={(which == "qsv" ? "nv12" : "yuv420p")} -colorspace bt709 -color_primaries bt709 -color_trc bt709 {enc} \"{path}.mp4\"";
            Plugin.Log.LogInfo($"[dev] recordav encoder {which} ({SystemInfo.graphicsDeviceName}), ffmpeg {FfmpegPath}");
            proc = Process.Start(new ProcessStartInfo(FfmpegPath, args) { UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true });
            video = proc.StandardInput.BaseStream;

            // the sound: our listener with the tap, everything playing F times slower
            AudioTap.Attach();
            AudioTap.Reset();
            AudioTap.Capturing = true;
            Slow = Mathf.Max(1f, Mathf.Round(FrameSeconds * fps));
            pitches.Clear();
            SlowAll();
            marks.Clear(); overruns.Clear(); work.Clear(); frameStart = 0; slowLog.Clear(); stageAt.Clear();
            frameNo = 0;
            lastDue = 0;
            clock.Restart();
            Plugin.Log.LogInfo($"[dev] recordav {path}: {w}x{h} {fps} fps, sound slowed x{Slow} ({FrameSeconds:0.00} s a frame), rate {AudioSettings.outputSampleRate}, screen {Screen.width}x{Screen.height} {Screen.fullScreenMode}");
            return true;
        }

        // every playing sound at pitch / F; a pitch the game changed since is taken as its new own pitch
        public static void SlowAll()
        {
            if (Slow <= 1f) return;
            foreach (var s in UnityEngine.Object.FindObjectsOfType<AudioSource>())
            {
                if (s == null) continue;
                int id = s.GetInstanceID();
                float now = s.pitch;
                if (pitches.TryGetValue(id, out var p) && Mathf.Approximately(now, p.set)) continue;
                float orig = now;
                float set = orig / Slow;
                s.pitch = set;
                pitches[id] = (set, orig);
            }
        }

        static void RestoreAll()
        {
            foreach (var s in UnityEngine.Object.FindObjectsOfType<AudioSource>())
            {
                if (s == null) continue;
                if (pitches.TryGetValue(s.GetInstanceID(), out var p) && Mathf.Approximately(s.pitch, p.set)) s.pitch = p.orig;
            }
            pitches.Clear();
        }

        // after the frame's cameras were rendered into tex (RGB24, width x height)
        public static void Frame(Texture2D tex)
        {
            // (the texture's own CPU copy, straight: going through a managed array would copy it byte by byte)
            work.Add((float)(clock.Elapsed.TotalSeconds - frameStart));
            Marshal.Copy(tex.GetWritableImageData(0), pixels, 0, pixels.Length);
            video.Write(pixels, 0, pixels.Length);
            Stage("pipe");
            if (MetronomeEvery > 0 && frameNo % MetronomeEvery == 0) Tick();
            SlowAll();
            Stage("pitch");
            double total = clock.Elapsed.TotalSeconds - frameStart;
            int gc = GC.CollectionCount(0) + GC.CollectionCount(1) * 100 + GC.CollectionCount(2) * 10000;
            if (total > FrameSeconds * 0.9 && slowLog.Length < 20000)
                slowLog.AppendLine($"frame {frameNo + 1}: {total:0.000} s = game {stageAt.GetValueOrDefault("game"):0.000} render {stageAt.GetValueOrDefault("render"):0.000} readback {stageAt.GetValueOrDefault("readback"):0.000} pipe {stageAt.GetValueOrDefault("pipe"):0.000} pitch {stageAt.GetValueOrDefault("pitch"):0.000}{(gc != gcWas ? " (managed GC)" : "")}");
            gcWas = gc;
            // hold the frame to T: the slowed sound plays exactly one frame's worth meanwhile
            frameNo++;
            // (a fixed grid of T: the GPU's slow frame and the fast one after it even out; only a real stall - a frame two
            // T late - moves the grid on, and its extra sound is cut out afterwards)
            double due = lastDue + FrameSeconds;
            double late = clock.Elapsed.TotalSeconds - due;
            if (late > FrameSeconds * 2) { overruns.Add((float)late); due = clock.Elapsed.TotalSeconds; }
            lastDue = due;
            // (spinning, not sleeping: an idle laptop clocks down and the next frame's work takes twice as long)
            while (clock.Elapsed.TotalSeconds < due) System.Threading.Thread.SpinWait(2000);
            marks.Add(AudioTap.Samples);
            frameStart = clock.Elapsed.TotalSeconds;
        }

        static void Tick()
        {
            if (metronomeClip == null)
                foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<AudioClip>()))
                    if (o != null && o.name == MetronomeClip) { metronomeClip = o.Cast<AudioClip>(); break; }
            if (metronome == null)
            {
                var go = new GameObject("PeaceMetronome");
                UnityEngine.Object.DontDestroyOnLoad(go);
                metronome = go.AddComponent<AudioSource>();
                metronome.spatialBlend = 0f;
                metronome.volume = 1f;
            }
            // (slowed like everything else, and marked as already slowed)
            if (metronomeClip != null) { metronome.pitch = 1f / Slow; pitches[metronome.GetInstanceID()] = (1f / Slow, 1f); metronome.PlayOneShot(metronomeClip); }
        }

        public static void Stop()
        {
            if (proc == null) return;
            AudioTap.Capturing = false;
            RestoreAll();
            float slowWas = Slow;
            Slow = 1f;
            try { video.Close(); proc.WaitForExit(120000); } catch (Exception e) { Plugin.Log.LogWarning("[dev] ffmpeg: " + e.Message); }
            proc = null;
            int rate = AudioSettings.outputSampleRate;
            AudioTap.WriteWav(basePath + ".raw.wav", rate);
            var sb = new StringBuilder();
            sb.AppendLine($"fps {fps}");
            sb.AppendLine($"frame_seconds {FrameSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"slow {slowWas.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"rate {rate}");
            sb.AppendLine($"channels {AudioTap.Channels}");
            sb.AppendLine("marks " + string.Join(" ", marks));
            File.WriteAllText(basePath + ".sync.txt", sb.ToString());
            if (slowLog.Length > 0) File.WriteAllText(basePath + ".slow.txt", slowLog.ToString());
            float worst = 0f; foreach (var o in overruns) worst = Math.Max(worst, o);
            work.Sort();
            string W(float q) => work.Count > 0 ? work[Math.Min(work.Count - 1, (int)(work.Count * q))].ToString("0.000") : "-";
            Plugin.Log.LogInfo($"[dev] recordav done: {frameNo} frames, {AudioTap.Samples} sound samples, {overruns.Count} frames over {FrameSeconds:0.00} s (worst +{worst:0.00} s); frame work median {W(0.5f)} p90 {W(0.9f)} p99 {W(0.99f)} s");
        }
    }
}
