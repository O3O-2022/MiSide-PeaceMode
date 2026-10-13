using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MiSidePeaceMode
{
    // Development helper: runs a command script from BepInEx/config/peacemode_dev.txt.
    // Does nothing if the file is absent. The game's Save folder is snapshotted at start
    // and restored on "quit", so dev runs never change the player's progress.
    public class DevRunner : MonoBehaviour
    {
        public DevRunner(IntPtr ptr) : base(ptr) { }

        static readonly string ConfigDir = BepInEx.Paths.ConfigPath;
        Queue<string> commands;
        float waitUntil, waitIdleUntil;
        bool waitReady, waitImports;
        Dictionary<string, byte[]> saveSnapshot;
        string saveDir;

        public static bool Active;

        void Start()
        {
            var path = Path.Combine(ConfigDir, "peacemode_dev.txt");
            if (!File.Exists(path)) return;
            Active = true;
            commands = new Queue<string>();
            foreach (var l in File.ReadAllLines(path))
                if (l.Trim().Length > 0 && !l.StartsWith("#")) commands.Enqueue(l.Trim());
            saveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "AIHASTO", "MiSideFull", "Save");
            saveSnapshot = new Dictionary<string, byte[]>();
            if (Directory.Exists(saveDir))
                foreach (var f in Directory.GetFiles(saveDir)) saveSnapshot[f] = File.ReadAllBytes(f);
            SnapshotPeaceSaves();
            waitUntil = Time.realtimeSinceStartup + 3f;
            // (a script keeps running with the window unfocused or the PC locked)
            try { Application.runInBackground = true; } catch { }
            Plugin.Log.LogInfo($"[dev] {commands.Count} commands queued, {saveSnapshot.Count} save files snapshotted");
        }

        // peace-mode progress files live in BepInEx/config: keep them out of test runs too
        Dictionary<string, byte[]> peaceSnapshot;
        void SnapshotPeaceSaves()
        {
            peaceSnapshot = new Dictionary<string, byte[]>();
            foreach (var f in Directory.GetFiles(ConfigDir, "MiSidePeaceMode*.save")) peaceSnapshot[f] = File.ReadAllBytes(f);
        }
        void RestorePeaceSaves()
        {
            if (peaceSnapshot == null) return;
            PeaceSave.Frozen = true;   // the director writes on quit/destroy too; keep the restored files
            foreach (var f in Directory.GetFiles(ConfigDir, "MiSidePeaceMode*.save"))
                if (!peaceSnapshot.ContainsKey(f)) File.Delete(f);
            foreach (var kv in peaceSnapshot) File.WriteAllBytes(kv.Key, kv.Value);
        }

        void RestoreSaves()
        {
            RestorePeaceSaves();
            if (saveSnapshot == null) return;
            foreach (var f in Directory.GetFiles(saveDir))
                if (!saveSnapshot.ContainsKey(f)) File.Delete(f);
            foreach (var kv in saveSnapshot) File.WriteAllBytes(kv.Key, kv.Value);
            Plugin.Log.LogInfo("[dev] save folder restored");
        }

        void OnApplicationQuit() => RestoreSaves();

        static void RenderSettingsDump(StringBuilder sb)
        {
            sb.AppendLine($"ambient mode={RenderSettings.ambientMode} light={RenderSettings.ambientLight} sky={RenderSettings.ambientSkyColor} int={RenderSettings.ambientIntensity} " +
                          $"fog={RenderSettings.fog} fogColor={RenderSettings.fogColor} skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name : "-")}");
        }

        // Screenshot that works even while the window is minimized: every screen camera is rendered into a texture
        // in depth order, with overlay canvases temporarily switched to camera space (one frame earlier) so the UI shows too.
        string camshotPath;
        int camshotStage;

        // record <prefix> <seconds> [fps]: game time locked to fps (Time.captureFramerate), every frame saved as
        // frames/<prefix>_0000.jpg - smooth video however slow the saving is (no sound: audio runs in real time)
        // recordav <name> <seconds> [fps] [width] [height]: the same, with sound, straight into a video (AvRecorder)
        string recPrefix; int recLeft, recIndex, recW = 1280, recH = 720;
        bool recAv;
        RenderTexture recRT; Texture2D recTex;

        void RecordStart(string prefix, float seconds, int fps, int w = 1280, int h = 720, bool av = false)
        {
            recPrefix = prefix; recIndex = 0; recLeft = Mathf.RoundToInt(seconds * fps);
            recW = w; recH = h;
            Time.captureFramerate = fps;
            Directory.CreateDirectory(Path.Combine(ConfigDir, "frames"));
            var cams = ScreenCameras();
            var top = cams.Count > 0 ? cams[cams.Count - 1] : null;
            camshotCanvases.Clear();
            foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (cv == null || !cv.isRootCanvas || cv.renderMode != RenderMode.ScreenSpaceOverlay || top == null) continue;
                Camera host = null;
                for (int i = cams.Count - 1; i >= 0 && host == null; i--)
                    if ((cams[i].cullingMask & (1 << cv.gameObject.layer)) != 0) host = cams[i];
                if (host == null) host = top;
                camshotCanvases.Add((cv, cv.renderMode, cv.worldCamera, cv.planeDistance));
                cv.renderMode = RenderMode.ScreenSpaceCamera;
                cv.worldCamera = host;
                cv.planeDistance = host.nearClipPlane + 0.05f;
            }
            recRT = new RenderTexture(w, h, 24);
            if (av && AvRecorder.Msaa > 1) recRT.antiAliasing = AvRecorder.Msaa;
            recTex = new Texture2D(w, h, TextureFormat.RGB24, false);
            recAv = av && AvRecorder.Start(Path.Combine(ConfigDir, "video", prefix), fps, w, h);
            if (av && !recAv) { recLeft = 0; Time.captureFramerate = 0; }
            Plugin.Log.LogInfo($"[dev] recording {prefix}: {recLeft} frames at {fps} fps");
        }

        void RecordFrame()
        {
            AvRecorder.Stage("game");
            PeaceDirector.Instance?.DevPrepareCapture();
            var cams = ScreenCameras();
            if (!recRT.IsCreated()) recRT.Create();
            var prevActive = RenderTexture.active;
            // (between scenes no camera draws: a black frame keeps the picture in step with the sound)
            if (cams.Count == 0) { RenderTexture.active = recRT; GL.Clear(true, true, Color.black); RenderTexture.active = prevActive; }
            foreach (var cam in cams)
            {
                var prev = cam.targetTexture;
                cam.targetTexture = recRT;
                cam.Render();
                cam.targetTexture = prev;
            }
            AvRecorder.Stage("render");
            RenderTexture.active = recRT;
            recTex.ReadPixels(new Rect(0, 0, recW, recH), 0, 0);
            recTex.Apply();
            AvRecorder.Stage("readback");
            RenderTexture.active = prevActive;
            if (recAv) AvRecorder.Frame(recTex);
            else File.WriteAllBytes(Path.Combine(ConfigDir, "frames", $"{recPrefix}_{recIndex:0000}.jpg"), ImageConversion.EncodeToJPG(recTex, 90));
            recIndex++;
            if (--recLeft > 0) return;
            EndRecording();
        }

        void EndRecording()
        {
            recLeft = 0;
            Time.captureFramerate = 0;
            if (recAv) { AvRecorder.Stop(); recAv = false; }
            foreach (var (c, mode, cam, dist) in camshotCanvases)
                if (c != null) { c.renderMode = mode; c.worldCamera = cam; c.planeDistance = dist; }
            camshotCanvases.Clear();
            UnityEngine.Object.Destroy(recRT); UnityEngine.Object.Destroy(recTex);
            Plugin.Log.LogInfo($"[dev] recorded {recPrefix}: {recIndex} frames");
            // (picks and timed commands meant for this clip that never came up - a game over early - end with it)
            if (pendingPicks.Count > 0) Plugin.Log.LogInfo($"[dev] dropped {pendingPicks.Count} picks left over: {string.Join(", ", pendingPicks.Select(p => p.what))}");
            pendingPicks.Clear();
            scheduled.Clear();
        }
        string camframePrefix; int camframeLeft, camframeIndex; float camframeInterval, camframeNext;
        readonly List<(Canvas c, RenderMode mode, Camera cam, float dist)> camshotCanvases = new List<(Canvas, RenderMode, Camera, float)>();

        static List<Camera> ScreenCameras()
        {
            var list = new List<Camera>();
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>()) if (c != null && c.isActiveAndEnabled && c.targetTexture == null) list.Add(c);
            list.Sort((a, b) => a.depth.CompareTo(b.depth));
            return list;
        }

        void CamShotStep()
        {
            var cams = ScreenCameras();
            if (cams.Count == 0) { Plugin.Log.LogWarning("[dev] camshot: no camera"); camshotPath = null; return; }
            var top = cams[cams.Count - 1];
            if (camshotStage == 0)
            {
                camshotCanvases.Clear();
                foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>())
                {
                    if (cv == null || !cv.isRootCanvas || cv.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    // the topmost camera that actually draws this canvas's layer
                    Camera host = null;
                    for (int i = cams.Count - 1; i >= 0 && host == null; i--)
                        if ((cams[i].cullingMask & (1 << cv.gameObject.layer)) != 0) host = cams[i];
                    if (host == null) host = top;
                    camshotCanvases.Add((cv, cv.renderMode, cv.worldCamera, cv.planeDistance));
                    cv.renderMode = RenderMode.ScreenSpaceCamera;
                    cv.worldCamera = host;
                    cv.planeDistance = host.nearClipPlane + 0.05f;
                    if (camframeLeft <= 0) Plugin.Log.LogInfo($"[dev] camshot canvas {cv.name} scene={cv.gameObject.scene.name} layer={cv.gameObject.layer} order={cv.sortingOrder} active={cv.isActiveAndEnabled} -> {host.name}");
                }
                if (camframeLeft <= 0) foreach (var c in cams) Plugin.Log.LogInfo($"[dev] camshot camera {c.name} depth={c.depth} mask={c.cullingMask:X} clear={c.clearFlags} pos={c.transform.position} parent={(c.transform.parent != null ? c.transform.parent.name : "-")}");
                if (camframeLeft <= 0) { var pmx = UnityEngine.Object.FindObjectOfType<PlayerMove>(); if (pmx != null && pmx.mainCamera != null) Plugin.Log.LogInfo($"[dev] camshot player camera transform {pmx.mainCamera.name} pos={pmx.mainCamera.position} hasCam={(pmx.mainCamera.GetComponent<Camera>() != null)}"); }
                camshotStage = 1;
                return;
            }
            const int w = 1280, h = 720;
            PeaceDirector.Instance?.DevPrepareCapture();
            var rt = new RenderTexture(w, h, 24);
            foreach (var cam in cams)
            {
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prev;
            }
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            byte[] png = ImageConversion.EncodeToPNG(tex);
            File.WriteAllBytes(camshotPath, png);
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
            foreach (var (c, mode, cam, dist) in camshotCanvases)
                if (c != null) { c.renderMode = mode; c.worldCamera = cam; c.planeDistance = dist; }
            camshotCanvases.Clear();
            if (camframeLeft <= 0) Plugin.Log.LogInfo($"[dev] camshot saved ({cams.Count} cameras, top={top.name})");
            camshotPath = null;
        }

        bool lookAtMita;
        string lookMode = "face";
        string framePrefix; int frameLeft, frameIndex; float frameInterval, nextFrame;

        string gestureKind; float gestureStart, gestureBaseX, gestureBaseY;

        void Update0Gesture()
        {
            if (gestureKind == null) return;
            float t = Time.realtimeSinceStartup - gestureStart;
            var pmg = UnityEngine.Object.FindObjectOfType<PlayerMove>();
            if (pmg == null || t > 1.2f) { if (pmg != null) { pmg.rotX = gestureBaseX; pmg.rotY = gestureBaseY; } gestureKind = null; return; }
            float w = Mathf.Sin(t * Mathf.PI * 2f * 2.5f) * 12f;
            if (gestureKind == "nod") pmg.rotY = gestureBaseY + w; else pmg.rotX = gestureBaseX + w;
        }

        bool staged, introStopped; float stageYaw;
        Camera devCam;

        Vector3 stageTarget; int stageCheck;
        Vector3 freeTarget; int freeFrames;

        Transform stagedMita, stagedHips; bool stageFlip;

        void KeepStageView()
        {
            if (freeFrames > 0)
            {
                freeFrames--;
                var pmz = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                if (pmz != null)
                {
                    pmz.pointFloorFix = freeTarget;
                    if (freeFrames % 10 == 0) Plugin.Log.LogInfo($"[dev] free check: player {pmz.transform.position} cam {(pmz.mainCamera != null ? pmz.mainCamera.position.ToString() : "-")} floorFix={pmz.pointFloorFix} count={pmz.countFloorFix}");
                }
            }
            if (staged && stagedMita != null)
            {
                // keep her facing the player while recording
                var pmf = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                if (pmf != null && pmf.mainCamera != null)
                {
                    // closed loop on her pelvis: turn the root until the (animated) hips face the camera, whatever the idle clip does
                    if (stagedHips == null) stagedHips = Util.FindDeep(stagedMita, "Hips");
                    if (stagedHips != null)
                    {
                        var hf = stagedHips.forward; hf.y = 0;
                        var to = pmf.mainCamera.position - stagedHips.position; to.y = 0;
                        if (hf.sqrMagnitude > 0.0001f && to.sqrMagnitude > 0.0001f)
                            stagedMita.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(hf, to, Vector3.up), Vector3.up) * stagedMita.rotation;
                    }
                }
            }
            if (stageCheck > 0)
            {
                stageCheck--;
                var pmc = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                if (pmc != null)
                {
                    float off = (pmc.transform.position - stageTarget).magnitude;
                    Plugin.Log.LogInfo($"[dev] stage check: player at {pmc.transform.position} (off {off:0.00}) rb={(pmc.rb != null ? pmc.rb.position.ToString() : "-")} root={pmc.transform.root.name}");
                    if (off > 0.3f)
                    {
                        // something pulled the player back: move the whole top-level object instead
                        var root = pmc.transform.root;
                        var delta = stageTarget - pmc.transform.position;
                        root.position += delta;
                        if (pmc.rb != null) pmc.rb.position = stageTarget;
                        Physics.SyncTransforms();
                    }
                }
            }
            if (!staged || PeaceDirector.Instance == null) return;
            var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
            if (pm == null || pm.mainCamera == null || gestureKind != null) return;   // (Mita's facing above is kept during gestures)
            var head = PeaceDirector.Instance.HeadPoint;
            var cam = pm.mainCamera.position;
            var flat = head - cam; float h = flat.y; flat.y = 0;
            pm.rotX = Quaternion.LookRotation(flat.sqrMagnitude > 0.0001f ? flat : Vector3.forward).eulerAngles.y;
            pm.rotY = -Mathf.Atan2(h, flat.magnitude) * Mathf.Rad2Deg;
        }

        void LateUpdate()
        {
            KeepStageView();
            if (recLeft > 0) { try { RecordFrame(); } catch (Exception e) { Plugin.Log.LogError("[dev] record: " + e); try { EndRecording(); } catch (Exception e2) { recLeft = 0; Time.captureFramerate = 0; Plugin.Log.LogError("[dev] record end: " + e2.Message); } } }
            if (camshotPath == null && camframeLeft > 0 && Time.realtimeSinceStartup >= camframeNext)
            {
                camshotPath = Path.Combine(ConfigDir, "frames", $"{camframePrefix}_{camframeIndex:000}.png");
                camshotStage = 0;
                camframeIndex++; camframeLeft--; camframeNext = Time.realtimeSinceStartup + camframeInterval;
            }
            if (camshotPath != null) { try { CamShotStep(); } catch (Exception e) { Plugin.Log.LogError("[dev] camshot: " + e); camshotPath = null; } }
            if (lookAtMita)
            {
                var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                var body = PeaceDirector.Instance != null ? PeaceDirector.Instance.MitaBody : null;
                if (pm != null && body != null && pm.mainCamera != null)
                {
                    var target = lookMode == "body" ? body.position + Vector3.up * 0.9f : PeaceDirector.Instance.HeadPoint;
                    // (the whole player turns, not only the camera: a camera turned far from the body sees its own shoulder)
                    var d = target - pm.mainCamera.position;
                    pm.rotX = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z)).eulerAngles.y;
                    pm.rotY = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
                }
            }
            if (frameLeft > 0 && Time.realtimeSinceStartup >= nextFrame)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(ConfigDir, "frames", $"{framePrefix}_{frameIndex:000}.png"));
                frameIndex++; frameLeft--; nextFrame = Time.realtimeSinceStartup + frameInterval;
            }
        }

        // a script waiting a long time is not the player being away: no handheld console taking over the view
        public static bool KeepAwake = true;
        public static bool ScriptAwake;   // (a script is running and keeps "you" awake: no AFK reactions either)
        static GameController devGc;

        string lastScene;
        float menuWaitSince = -1f;
        bool requeued;

        // menu commands (peace, continue, menupeace) wait for the main menu to exist; a fresh PC's first start (intro,
        // shader warm-up) took far longer than the script's first wait and `peace` fell back to loading the scene by itself
        bool languagePicked;

        // the game's first start on a PC shows a language (and voice) choice in SceneLoading and waits for a click:
        // a script picks Korean with the Japanese voice (the user's own choice) and goes on
        void PickFirstLanguage()
        {
            if (languagePicked) return;
            var cls = UnityEngine.Object.FindObjectOfType<ChangeLanguageStart>();
            if (cls == null || !cls.gameObject.activeInHierarchy) return;
            languagePicked = true;
            try
            {
                int lang = -1, voice = -1;
                var langs = cls.languages; var voices = cls.voices;
                if (langs != null) for (int i = 0; i < langs.Length; i++) if (langs[i] == "Korean") lang = i;
                if (voices != null) for (int i = 0; i < voices.Length; i++) if (voices[i] != null && voices[i].StartsWith("Japanese")) voice = i;
                Plugin.Log.LogInfo($"[dev] first-start language screen: languages {(langs != null ? langs.Length : 0)}, voices {(voices != null ? string.Join("|", voices) : "-")} -> Korean #{lang}, voice #{voice}");
                if (lang >= 0) cls.ChangeLanguage(lang);
                if (voice >= 0) cls.ChangeVoice(voice, true);
                cls.Apply();
            }
            catch (Exception e) { Plugin.Log.LogWarning("[dev] language screen: " + e.Message); }
        }

        bool MenuMissing(string cmd)
        {
            if (UnityEngine.Object.FindObjectOfType<Menu>() != null) { menuWaitSince = -1f; return false; }
            PickFirstLanguage();
            float now = Time.realtimeSinceStartup;
            if (menuWaitSince < 0f) { menuWaitSince = now; Plugin.Log.LogInfo($"[dev] {cmd}: no main menu yet, waiting for it (up to 300 s)"); }
            if (now - menuWaitSince > 300f) { Plugin.Log.LogWarning($"[dev] {cmd}: no main menu after 300 s (scene {SceneManager.GetActiveScene().name}), going on anyway"); menuWaitSince = -1f; return false; }
            var rest = new Queue<string>();
            rest.Enqueue(cmd);
            foreach (var c in commands) rest.Enqueue(c);
            commands = rest;
            waitUntil = now + 1f;
            requeued = true;   // (not logged again every second)
            return true;
        }

        float nextStopCheck;

        void Update()
        {
            // run_dev.ps1 asks for a clean stop (BepInEx/config/peacemode_stop.txt) before it kills the game at its
            // timeout: the saves are put back first (a killed game never restored them)
            if (Active && Time.realtimeSinceStartup > nextStopCheck)
            {
                nextStopCheck = Time.realtimeSinceStartup + 1f;
                var stop = Path.Combine(ConfigDir, "peacemode_stop.txt");
                if (File.Exists(stop))
                {
                    try { File.Delete(stop); } catch { }
                    Plugin.Log.LogInfo("[dev] stop requested: restoring saves and quitting");
                    RestoreSaves();
                    Application.Quit();
                    return;
                }
            }
            var scene = SceneManager.GetActiveScene().name;
            if (scene != lastScene) { lastScene = scene; if (Active) Plugin.Log.LogInfo($"[dev] scene -> {scene} (t={Time.realtimeSinceStartup:0.0})"); }
            ScriptAwake = KeepAwake && commands != null && commands.Count > 0;
            if (ScriptAwake)
            {
                if (devGc == null) devGc = UnityEngine.Object.FindObjectOfType<GameController>();
                if (devGc != null) devGc.timeAFK = 0f;
            }
            Update0Gesture();
            DevWalk.TickClicks();
            if (tapUntil > 0f && Time.realtimeSinceStartup >= tapUntil)
            {
                tapUntil = -1f; AudioTap.Capturing = false;
                AudioTap.WriteWav(Path.Combine(ConfigDir, "tap.wav"), AudioSettings.outputSampleRate);
                Plugin.Log.LogInfo($"[dev] audio tap: {AudioTap.Blocks} blocks, {AudioTap.Samples} samples x{AudioTap.Channels}, peak {AudioTap.Peak():0.000}, calls {AudioTap.Calls}, last block peak {AudioTap.LastPeak:0.000}");
            }
            // "after <sec> <command>": commands timed in game seconds, run even while a recording holds the script
            for (int i = scheduled.Count - 1; i >= 0; i--)
                if (Time.time >= scheduled[i].at)
                {
                    var c = scheduled[i].cmd; scheduled.RemoveAt(i);
                    Plugin.Log.LogInfo($"[dev] (after) {c}");
                    try { Run(c); } catch (Exception e) { Plugin.Log.LogError($"[dev] {c}: {e}"); }
                }
            // "pickwhen <text>" / "choosewhen <n>": once that option is on screen (and has been for a moment), pick it
            // (in order: the first one waiting is the next menu's)
            for (int i = 0; i < Math.Min(1, pendingPicks.Count); i++)
            {
                var (what, until, seen) = pendingPicks[i];
                var pd = PeaceDirector.Instance;
                if (Time.time > until) { Plugin.Log.LogWarning("[dev] pickwhen timed out: " + what); pendingPicks.RemoveAt(i); continue; }
                if (pd == null || !pd.DevMenuHas(what)) continue;
                if (seen < 0f) { pendingPicks[i] = (what, until, Time.time); continue; }
                if (Time.time - seen < 1.2f) continue;
                pendingPicks.RemoveAt(i);
                Plugin.Log.LogInfo("[dev] (pickwhen) " + what);
                if (what.StartsWith("#")) pd.DevChoose(int.Parse(what.Substring(1))); else pd.DevPick(what);
            }
            if (commands == null || commands.Count == 0) return;
            if (Time.realtimeSinceStartup < waitUntil) return;
            if (waitIdleUntil > 0f)
            {
                var pd = PeaceDirector.Instance;
                bool waiting = waitImports ? (pd == null || !pd.DevImportsDone) : waitReady ? (pd == null || !pd.DevReady) : (pd == null || !pd.DevIdle);
                if (waiting && Time.realtimeSinceStartup < waitIdleUntil) return;
                if (waiting) Plugin.Log.LogWarning("[dev] " + (waitReady ? "waitready" : "waitidle") + " timed out");
                waitIdleUntil = 0f; waitReady = false; waitImports = false;
            }
            if (camframeLeft > 0 || camshotPath != null || recLeft > 0) return;   // let captures finish, however slow the frames are
            if (DevWalk.Active) return;
            var cmd = commands.Dequeue();
            if (!requeued) Plugin.Log.LogInfo($"[dev] {cmd}");
            requeued = false;
            try { Run(cmd); } catch (Exception e) { Plugin.Log.LogError($"[dev] {cmd}: {e}"); }
        }

        readonly List<(float at, string cmd)> scheduled = new List<(float, string)>();
        float tapUntil = -1f;
        readonly List<(string what, float until, float seen)> pendingPicks = new List<(string, float, float)>();

        void Run(string cmd)
        {
            var parts = cmd.Split(' ', 2);
            var arg = parts.Length > 1 ? parts[1] : "";
            switch (parts[0])
            {
                case "audioprobe":   // audioprobe: every loaded AudioClip - load type, and whether its samples can be read (GetData)
                {
                    var sb = new StringBuilder();
                    int ok = 0, bad = 0;
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>()))
                    {
                        var c = o.Cast<AudioClip>();
                        if (c == null) continue;
                        string res;
                        try
                        {
                            int n = Math.Min(48000, c.samples / 2) * c.channels;
                            var buf = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<float>(Math.Max(n, 1));
                            bool got = c.GetData(buf, c.samples / 3);
                            float peak = 0f; for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs(buf[i]));
                            res = $"GetData {got} peak {peak:0.000}";
                            if (got) ok++; else bad++;
                        }
                        catch (Exception e) { res = "GetData threw " + e.Message.Split((char)10)[0]; bad++; }
                        sb.AppendLine($"{c.name}	{c.length:0.00}s	{c.frequency}Hz x{c.channels}	{c.loadType}	{c.loadState}	{res}");
                    }
                    File.WriteAllText(Path.Combine(ConfigDir, "audioprobe.txt"), sb.ToString(), Encoding.UTF8);
                    Plugin.Log.LogInfo($"[dev] audioprobe: {ok} readable, {bad} not");
                    break;
                }
                case "audiotap":   // audiotap <real seconds>: capture the game's mixed sound at the listener (a test), write tap.wav
                {
                    var tap = AudioTap.Attach();
                    foreach (var l in UnityEngine.Object.FindObjectsOfType<AudioListener>()) Plugin.Log.LogInfo($"[dev] listener {Util.PathOf(l.transform)} enabled {l.enabled} active {l.gameObject.activeInHierarchy}");
                    Plugin.Log.LogInfo($"[dev] audio: listener volume {AudioListener.volume} paused {AudioListener.pause} game volume {GlobalGame.VolumeGame} sources playing {UnityEngine.Object.FindObjectsOfType<AudioSource>().Count(a => a.isPlaying)}");
                    AudioTap.Reset(); AudioTap.Capturing = tap != null;
                    tapUntil = Time.realtimeSinceStartup + float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
                    waitUntil = tapUntil + 0.2f;
                    break;
                }
                case "calm": PeaceDirector.DevCalm = arg != "off"; break;   // calm [off]: she does nothing of her own (recordings)
                case "pickwhen": pendingPicks.Add((arg, Time.time + 120f, -1f)); break;          // pickwhen <option text>: pick it once her menu shows it
                case "choosewhen": pendingPicks.Add(("#" + arg, Time.time + 120f, -1f)); break;  // choosewhen <n>: the n-th option (from 0) of the next menu
                case "after":   // after <game seconds> <command>: e.g. picks in a menu while "record" runs
                {
                    var sp = arg.Split(' ', 2);
                    scheduled.Add((Time.time + float.Parse(sp[0], System.Globalization.CultureInfo.InvariantCulture), sp[1]));
                    break;
                }
                case "wait":
                    waitUntil = Time.realtimeSinceStartup + float.Parse(arg);
                    break;
                case "waitimports":   // waitimports [s]: until every borrowed story scene is in (the dance pad, the hammer game...)
                    waitImports = true;
                    waitIdleUntil = Time.realtimeSinceStartup + (string.IsNullOrEmpty(arg) ? 300f : float.Parse(arg));
                    waitUntil = Time.realtimeSinceStartup + 0.5f;
                    break;
                case "waitready":
                    // until the peace scene's director has finished setting up (max N seconds)
                    waitReady = true;
                    waitIdleUntil = Time.realtimeSinceStartup + (string.IsNullOrEmpty(arg) ? 60f : float.Parse(arg));
                    waitUntil = Time.realtimeSinceStartup + 0.5f;
                    break;
                case "waitidle":
                    // until Mita has finished talking and any staged scene is over (max N seconds)
                    waitIdleUntil = Time.realtimeSinceStartup + (string.IsNullOrEmpty(arg) ? 120f : float.Parse(arg));
                    waitUntil = Time.realtimeSinceStartup + 0.5f;
                    break;
                case "menuload":
                {
                    var menu = UnityEngine.Object.FindObjectOfType<Menu>();
                    if (menu == null) { Plugin.Log.LogWarning("[dev] no Menu"); break; }
                    menu.ButtonLoadScene(arg);
                    break;
                }
                case "scene":
                {
                    var sp = arg.Split(' ', 2);
                    GlobalGame.levelLoad = int.Parse(sp[0]);
                    GlobalGame.LoadingLevel = sp[1];
                    SceneManager.LoadScene("SceneLoading");
                    break;
                }
                case "continue":
                {
                    if (MenuMissing(cmd)) break;
                    var menu = UnityEngine.Object.FindObjectOfType<Menu>();
                    if (menu != null) menu.ButtonContinue();
                    break;
                }
                case "peace":
                    if (MenuMissing(cmd)) break;
                    PeaceMode.Start(arg.Length > 0 ? arg : null);
                    break;
                case "menupeace":
                {
                    if (MenuMissing(cmd)) break;
                    var b = Util.FindDeepAll("Button PeaceMode");
                    var c = b != null ? b.GetComponent<ButtonMouseClick>() : null;
                    Plugin.Log.LogInfo("[dev] peace button " + (c != null));
                    if (c != null) c.eventClick.Invoke();
                    break;
                }
                case "menuhover":
                {
                    int i = int.Parse(arg);
                    if (i < MenuPatch.PersonaButtons.Count) MenuPatch.Describe(Personas.All[i]);
                    break;
                }
                case "scenes":
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var sc = SceneManager.GetSceneAt(i);
                        Plugin.Log.LogInfo($"[dev] scene {i}: {sc.name} loaded={sc.isLoaded} roots={sc.rootCount} active={(sc == SceneManager.GetActiveScene())}");
                    }
                    foreach (var sl in UnityEngine.Object.FindObjectsOfType<SceneLoading>())
                        Plugin.Log.LogInfo($"[dev] SceneLoading alive in {sl.gameObject.scene.name}: go={sl.go} async={sl.sceneAcyncGo} ready={sl.loadReady} level={sl.levelLoadName}");
                    break;
                case "camlist":
                    foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>())
                        Plugin.Log.LogInfo($"[dev] camera {c.name} depth={c.depth} clear={c.clearFlags} rt={(c.targetTexture != null)} pos={c.transform.position} on={c.isActiveAndEnabled}");
                    break;
                case "writesave":
                {
                    var sp = arg.Split('|', 2);
                    File.WriteAllText(Path.Combine(saveDir, sp[0]), sp[1].Replace("\\n", "\r\n"));
                    break;
                }
                case "activate":
                {
                    var go = Util.FindPath(arg);
                    Plugin.Log.LogInfo("[dev] activate " + arg + " -> " + (go != null));
                    if (go != null) { for (var t = go.transform; t != null; t = t.parent) t.gameObject.SetActive(true); }
                    break;
                }
                case "deactivate":
                {
                    var go = Util.FindPath(arg);
                    if (go != null) go.SetActive(false);
                    break;
                }
                case "tpmita":
                {
                    var mita = UnityEngine.Object.FindObjectOfType<MitaPerson>();
                    var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (mita == null || pm == null) { Plugin.Log.LogWarning("[dev] no mita/player"); break; }
                    var mt = mita.nma != null ? mita.nma.transform : mita.transform;
                    var pos = mt.position + mt.forward * 1.4f;
                    pm.transform.position = pos;
                    var dir = mt.position - pos; dir.y = 0;
                    pm.transform.rotation = Quaternion.LookRotation(dir);
                    Plugin.Log.LogInfo($"[dev] mita at {mt.position} fwd {mt.forward}; player -> {pos}");
                    break;
                }
                case "where":
                {
                    foreach (var n in arg.Split(','))
                    {
                        var go = Util.FindPath(n.Trim());
                        Plugin.Log.LogInfo($"[dev] where {n}: " + (go == null ? "null" : go.transform.position + " active=" + go.activeInHierarchy));
                    }
                    break;
                }
                case "clips":
                {
                    var sb = new StringBuilder();
                    try
                    {
                        var body = Util.FindPath("MitaPerson Mita");
                        var an = body != null ? body.GetComponent<Animator>() : null;
                        sb.AppendLine("ANIMATOR " + (an != null) + " ctrl=" + (an != null && an.runtimeAnimatorController != null ? an.runtimeAnimatorController.name : "-"));
                        if (an != null && an.runtimeAnimatorController != null)
                            foreach (var c in an.runtimeAnimatorController.animationClips) if (c != null) sb.AppendLine("  clip " + c.name + " len=" + c.length);
                    }
                    catch (Exception e) { sb.AppendLine("animator err " + e.Message); }
                    try
                    {
                        var addon = UnityEngine.Object.FindObjectOfType<Location34_Communication>();
                        if (addon != null && addon.animationsBehaviour != null)
                            foreach (var c in addon.animationsBehaviour) sb.AppendLine("  behaviour " + (c != null ? c.name : "null"));
                    }
                    catch (Exception e) { sb.AppendLine("addon err " + e.Message); }
                    try
                    {
                        var all = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimationClip>());
                        foreach (var o in all)
                        {
                            if (o == null) continue;
                            var n = o.name;
                            if (n != null && (n.StartsWith("Mita") || n.Contains("Hug") || n.Contains("Head"))) sb.AppendLine("  anyclip " + n);
                        }
                    }
                    catch (Exception e) { sb.AppendLine("res err " + e.Message); }
                    File.WriteAllText(Path.Combine(ConfigDir, "clips.txt"), sb.ToString());
                    break;
                }
                case "peacemenu": PeaceDirector.Instance?.DevOpenMenu(); break;
                case "peacechoose": PeaceDirector.Instance?.DevChoose(int.Parse(arg)); break;
                case "greet": PeaceDirector.Instance?.DevGreet(); break;
                case "wallsim":
                {
                    var sp = arg.Split(' ', 2);
                    if (PeaceDirector.Instance != null) PeaceDirector.Instance.DevWallSim(sp[0], sp.Length > 1 ? sp[1] : "");
                    else PeaceDirector.DevWallSimStatic(sp[0], sp.Length > 1 ? sp[1] : "");
                    break;
                }
                case "symbols":
                    File.WriteAllText(Path.Combine(ConfigDir, "symbols_" + arg + ".txt"), PeaceDirector.Instance?.DevSymbols() ?? "-", Encoding.UTF8);
                    break;
                case "showcase":
                {
                    // showcase mx mz px pz room
                    var sp = arg.Split(' ');
                    PeaceDirector.Instance?.DevShowcase(new Vector3(float.Parse(sp[0]), 0f, float.Parse(sp[1])),
                        new Vector3(float.Parse(sp[2]), 0f, float.Parse(sp[3])), sp.Length > 4 ? sp[4] : "Kitchen");
                    break;
                }
                case "lookdump": Plugin.Log.LogInfo("[dev] look: " + PeaceDirector.Instance?.DevLookDump()); break;
                case "pitch": PeaceDirector.Instance?.DevPitch(float.Parse(arg)); break;
                case "reidle": PeaceDirector.Instance?.DevReIdle(); break;
                case "animnow": Plugin.Log.LogInfo("[dev] anim: " + PeaceDirector.Instance?.DevAnimNow()); break;
                case "mitalook": PeaceDirector.Instance?.DevMitaLook(arg != "off"); break;
                case "unshowcase": PeaceDirector.Instance?.DevUnshowcase(); break;
                case "act": PeaceDirector.Instance?.DevAct(arg); break;
                case "wall":
                {
                    var sp = arg.Split(' ', 2);
                    PeaceDirector.Instance?.DevWall(sp[0], sp.Length > 1 ? sp[1] : null);
                    break;
                }
                case "gamelang":
                {
                    // gamelang <Name>: the game's own language (a Data/Languages folder name), set in the main menu the way its
                    // language option does. (The game takes Steam's language for itself at launch, whatever its prefs say.)
                    var menu = UnityEngine.Object.FindObjectOfType<Menu>();
                    var opt = menu != null ? menu.buttonLanguage : null;
                    int idx = -1;
                    if (opt != null && opt.languages != null)
                        for (int i = 0; i < opt.languages.Length; i++) if (opt.languages[i] == arg) idx = i;
                    // (the value itself: every text loaded from now on - the next scene's - reads it. The menu's own language
                    // functions, and its reload, pick by their own index and land elsewhere (Arabic), so the menu stays as it is.)
                    GlobalGame.Language = arg;
                    PlayerPrefs.SetString("Language", arg);
                    Loc.UseFont(false); Loc.Init("auto"); Loc.UseFont(true);
                    Plugin.Log.LogInfo($"[dev] gamelang {arg}: option index {idx} of {(opt != null && opt.languages != null ? opt.languages.Length : 0)}, game={GlobalGame.Language}, ours={Loc.Lang}");
                    break;
                }
                case "lightcmp":
                {
                    // lightcmp <tag>: every light and the scene's ambient/fog, to compare before and after something
                    var sb = new StringBuilder($"ambient {RenderSettings.ambientMode} {RenderSettings.ambientLight} sky {RenderSettings.ambientSkyColor} eq {RenderSettings.ambientEquatorColor} gnd {RenderSettings.ambientGroundColor} x{RenderSettings.ambientIntensity}\n");
                    sb.AppendLine($"fog {RenderSettings.fog} {RenderSettings.fogColor} {RenderSettings.fogDensity}");
                    foreach (var l in UnityEngine.Object.FindObjectsOfType<Light>())
                        if (l != null) sb.AppendLine($"{(l.enabled && l.gameObject.activeInHierarchy ? "on " : "off")} {l.type} {l.color} x{l.intensity:0.00}  {Util.PathOf(l.transform)}");
                    foreach (var c in UnityEngine.Object.FindObjectsOfType<Light_LightingColor>())
                        if (c != null) sb.AppendLine($"LightingColor {Util.PathOf(c.transform)}");
                    File.WriteAllText(Path.Combine(ConfigDir, $"lights_{arg}.txt"), sb.ToString(), Encoding.UTF8);
                    Plugin.Log.LogInfo($"[dev] lights_{arg}.txt written");
                    break;
                }
                case "nearcam":
                {
                    // nearcam [metres]: what is drawn right in front of the camera (a stray arm or prop in the shot)
                    float r = string.IsNullOrEmpty(arg) ? 1f : float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
                    var cam = Camera.main;
                    if (cam == null) { Plugin.Log.LogWarning("[dev] nearcam: no camera"); break; }
                    var sb = new StringBuilder($"camera {cam.transform.position} fwd {cam.transform.forward}\n");
                    foreach (var rd in UnityEngine.Object.FindObjectsOfType<Renderer>())
                    {
                        if (rd == null || !rd.enabled || !rd.gameObject.activeInHierarchy) continue;
                        var d = Mathf.Sqrt(rd.bounds.SqrDistance(cam.transform.position));
                        if (d > r) continue;
                        bool seen = (cam.cullingMask & (1 << rd.gameObject.layer)) != 0;
                        sb.AppendLine($"{d:0.00} m  {(seen ? "seen" : "culled")} layer {rd.gameObject.layer}  {Util.PathOf(rd.transform)}  ({rd.GetIl2CppType().Name})");
                    }
                    File.WriteAllText(Path.Combine(ConfigDir, "nearcam.txt"), sb.ToString(), Encoding.UTF8);
                    Plugin.Log.LogInfo("[dev] nearcam written");
                    break;
                }
                case "mitadiag":
                    File.WriteAllText(Path.Combine(ConfigDir, "mitadiag_" + arg + ".txt"), PeaceDirector.Instance?.DevMitaDiag() ?? "-", Encoding.UTF8);
                    break;
                case "peaceset":
                {
                    // peaceset <id> key=value: edit a peace-mode save line (restored after the run like the rest)
                    var sp = arg.Split(' ', 2);
                    var file = Path.Combine(ConfigDir, sp[0] == "crazy" ? "MiSidePeaceMode.save" : $"MiSidePeaceMode_{sp[0]}.save");
                    var kv = sp[1].Split('=', 2);
                    var lines = File.Exists(file) ? new List<string>(File.ReadAllLines(file)) : new List<string>();
                    lines.RemoveAll(l => l.StartsWith(kv[0] + "="));
                    lines.Add(kv[0] + "=" + kv[1]);
                    File.WriteAllLines(file, lines);
                    break;
                }
                case "peacetime":
                {
                    var hm = arg.Split(':');
                    PeaceDirector.Instance?.DevSetTime(int.Parse(hm[0]) * 60 + int.Parse(hm[1]));
                    break;
                }
                case "look":
                    lookAtMita = arg != "off";
                    if (arg == "face" || arg == "body") lookMode = arg;
                    if (!lookAtMita) { var pmo = UnityEngine.Object.FindObjectOfType<PlayerMove>(); if (pmo != null) pmo.stopMouseMove = false; }
                    break;
                case "tpdist":
                {
                    // put the player <arg> meters in front of Mita
                    var mita = UnityEngine.Object.FindObjectOfType<MitaPerson>();
                    var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (mita == null || pm == null) break;
                    var mt = mita.nma != null ? mita.nma.transform : mita.transform;
                    float d = float.Parse(arg);
                    // first direction around her that isn't blocked by a wall
                    var pos = mt.position + mt.forward * d;
                    foreach (var a in new[] { 0f, 40f, -40f, 80f, -80f, 120f, -120f, 180f })
                    {
                        var dirA = Quaternion.Euler(0, a, 0) * mt.forward;
                        var from = mt.position + Vector3.up * 1.2f;
                        bool blocked = false;
                        foreach (var h in Physics.RaycastAll(from, dirA, d + 0.4f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (h.collider == null) continue;
                            var ht = h.collider.transform;
                            if (ht.IsChildOf(mt.root) || ht.IsChildOf(pm.transform)) continue;
                            blocked = true; break;
                        }
                        if (!blocked) { pos = mt.position + dirA * d; break; }
                    }
                    var cc = pm.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    pm.transform.position = pos;
                    if (pm.rb != null) { pm.rb.position = pos; pm.rb.velocity = Vector3.zero; }
                    var dir = mt.position - pos; dir.y = 0;
                    pm.transform.rotation = Quaternion.LookRotation(dir);
                    // face her through the player's own look angles (no camera hijack)
                    pm.rotX = Quaternion.LookRotation(dir).eulerAngles.y;
                    pm.rotY = 8f;
                    if (cc != null) cc.enabled = true;
                    Physics.SyncTransforms();
                    break;
                }
                case "frames":
                {
                    // frames <prefix> <count> <interval>
                    var sp = arg.Split(' ');
                    framePrefix = sp[0]; frameLeft = int.Parse(sp[1]); frameInterval = float.Parse(sp[2]); frameIndex = 0; nextFrame = 0;
                    waitUntil = Time.realtimeSinceStartup + frameLeft * frameInterval + 0.5f;
                    break;
                }
                case "audiocheck":
                {
                    foreach (var a in UnityEngine.Object.FindObjectsOfType<AudioSource>())
                        if (a.isPlaying) Plugin.Log.LogInfo($"[dev] audio playing: {a.gameObject.name} clip={(a.clip != null ? a.clip.name : "-")} vol={a.volume}");
                    Plugin.Log.LogInfo($"[dev] LanguageVoice='{GlobalGame.LanguageVoice}'");
                    break;
                }
                case "musiclist":
                {
                    // musiclist [min seconds]: long audio clips in memory (music), and the scene's music sources
                    float min = string.IsNullOrEmpty(arg) ? 40f : float.Parse(arg);
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>()))
                    {
                        var c = o.Cast<AudioClip>();
                        if (c != null && c.length >= min) Plugin.Log.LogInfo($"[dev] clip {c.name} {c.length:0.0}s state={c.loadState} flags={c.hideFlags}");
                    }
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioSource>()))
                    {
                        var a = o.Cast<AudioSource>();
                        if (a == null || !a.gameObject.scene.IsValid() || a.clip == null || a.clip.length < min) continue;
                        Plugin.Log.LogInfo($"[dev] source {a.gameObject.scene.name}:{a.name} clip={a.clip.name} on={a.isActiveAndEnabled} playing={a.isPlaying} vol={a.volume:0.00} loop={a.loop} pitch={a.pitch:0.00}");
                    }
                    break;
                }
                case "listsaves":
                    foreach (var f in Directory.GetFiles(saveDir)) Plugin.Log.LogInfo("[dev] save: " + Path.GetFileName(f) + " = " + File.ReadAllText(f).Replace("\r\n", "\\n"));
                    break;
                case "dump":   // dump <file> [pos]: every loaded scene's tree (pos: with world positions)
                {
                    var sp = arg.Split(' ');
                    File.WriteAllText(Path.Combine(ConfigDir, sp[0]), SceneDumper.Dump(sp.Length > 1 && sp[1] == "pos"), Encoding.UTF8);
                    break;
                }
                    break;
                case "globals":
                    Plugin.Log.LogInfo($"[dev] dataFiles={GlobalGame.dataFiles} LoadingLevel={GlobalGame.LoadingLevel} levelLoad={GlobalGame.levelLoad} lang={GlobalGame.Language} voice={GlobalGame.LanguageVoice} play={GlobalGame.play} playWorld={GlobalGame.playWorld} scene={SceneManager.GetActiveScene().name}");
                    break;
                case "quit":
                    RestoreSaves();
                    Application.Quit();
                    break;
                case "linelayout": PeaceDirector.DevLineLayout = arg != "off"; break;   // log how each line's letters are laid out
                case "layoutfix": PeaceDirector.DevLayoutFix = arg != "off"; break;       // the letter spacing fix on / off
                case "ptheme":  // the theme (colour) your own lines use from now on: Player, White, ...
                    PeaceDirector.PlayerTheme = arg.Length > 0 ? arg : "Player";
                    break;
                case "story":   // what Kind Mita would read from the story's save folder; `story play [0-3]`: her words about it now
                    if (arg.StartsWith("play")) { PeaceDirector.Instance?.DevStoryPlay(arg.Length > 5 ? int.Parse(arg.Substring(5)) : 3); break; }
                    Plugin.Log.LogInfo("[dev] story: " + (PeaceDirector.Instance != null ? PeaceDirector.Instance.DevStory() : "no director"));
                    break;
                case "screenshot":
                    ScreenCapture.CaptureScreenshot(Path.Combine(ConfigDir, arg));
                    break;
                case "key":
                    DevInput.Press(arg);
                    break;
                case "personage":
                    File.WriteAllText(Path.Combine(ConfigDir, "personage_" + arg.Replace('/', '_') + ".txt"),
                        MitaSkin.Report(arg, PeaceDirector.Instance != null ? PeaceDirector.Instance.MitaBody : null), Encoding.UTF8);
                    break;
                case "skin":
                    PeaceDirector.Instance?.DevSkin(arg);
                    break;
                case "dlginfo":
                {
                    // native dialogue objects: how they are set up (theme, speaker, sounds, placement)
                    var sb = new StringBuilder();
                    var cam = UnityEngine.Object.FindObjectOfType<PlayerMove>()?.mainCamera;
                    int count = 0;
                    foreach (var d in Resources.FindObjectsOfTypeAll(Il2CppType.Of<Dialogue_3DText>()))
                    {
                        var t = d.Cast<Dialogue_3DText>();
                        if (t == null || t.gameObject.scene.name == null) continue;
                        if (arg.Length > 0 && !t.name.Contains(arg) && (t.transform.parent == null || !t.transform.parent.name.Contains(arg))) continue;
                        if (count++ > 60) break;
                        var tr = t.transform;
                        var toCam = cam != null ? (tr.position - cam.position) : Vector3.zero;
                        sb.AppendLine($"{t.name} parent={(tr.parent != null ? tr.parent.name : "-")} theme={t.themeDialogue} speaker={(t.speaker != null ? t.speaker.name : "null")} " +
                                      $"audioPrint={t.audioPrint} dontVoice={t.dontVoice} voicePlay={t.voicePlay} timeSound={t.timeSound} voice={(t.audioVoice != null ? t.audioVoice.name : "-")} " +
                                      $"sounds={(t.sounds != null ? t.sounds.name + "/" + (t.sounds.sounds != null ? t.sounds.sounds.Length : 0) + "/" + (t.sounds.audioSource != null ? t.sounds.audioSource.name : "-") : "null")} " +
                                      $"lookOnPlayer={t.lookOnPlayer} align={t.align} size={t.sizeWidth}x{t.sizeHeight} font={(t.font != null ? t.font.name : "-")} " +
                                      $"pos={tr.position} rot={tr.rotation.eulerAngles} fwdDotCam={(toCam.sqrMagnitude > 0 ? Vector3.Dot(tr.forward, toCam.normalized) : 0):0.00} scale={tr.lossyScale} active={t.gameObject.activeInHierarchy}");
                    }
                    File.WriteAllText(Path.Combine(ConfigDir, "dlginfo_" + (arg.Length > 0 ? arg.Replace(' ', '_').Replace('>', '_') : "all") + ".txt"), sb.ToString(), Encoding.UTF8);
                    break;
                }
                case "camstate":
                {
                    var pmv = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (pmv == null) break;
                    var camT = pmv.mainCamera;
                    Plugin.Log.LogInfo($"[dev] camstate {arg}: bodyYaw={pmv.transform.eulerAngles.y:0.0} camYaw={(camT != null ? camT.eulerAngles.y : -1):0.0} camPitch={(camT != null ? camT.eulerAngles.x : -1):0.0} " +
                                       $"rotX={pmv.rotX:0.0} rotY={pmv.rotY:0.0} other={(pmv.cameraOtherTarget != null ? pmv.cameraOtherTarget.name : "-")} otherLerp={pmv.timeCameraOtherTargetLerp:0.00} " +
                                       $"dontMove={pmv.dontMove} stopMouse={pmv.stopMouseMove} camParent={(camT != null && camT.parent != null ? camT.parent.name : "-")} fwdDot={(camT != null ? Vector3.Dot(Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized, pmv.transform.forward) : 0):0.00}");
                    break;
                }
                case "rooms":
                    foreach (var r in arg.Split(',')) PeaceDirector.Instance?.ShowRoom(r.Trim());
                    break;
                case "roomson":
                {
                    // count (and with "on" force) renderers around a point: roomson x z radius [on]
                    var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var c = new Vector3(float.Parse(sp[0]), 0, float.Parse(sp[1]));
                    float r = float.Parse(sp[2]);
                    bool force = sp.Length > 3 && sp[3] == "on";
                    int total = 0, off = 0, inactive = 0;
                    var owners = new Dictionary<string, int>();
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<MeshRenderer>()))
                    {
                        var mr = o.Cast<MeshRenderer>();
                        if (mr == null || mr.gameObject.scene != SceneManager.GetActiveScene()) continue;
                        var p0 = mr.transform.position; p0.y = 0;
                        if ((p0 - c).magnitude > r) continue;
                        total++;
                        if (!mr.enabled) off++;
                        if (!mr.gameObject.activeInHierarchy)
                        {
                            inactive++;
                            var top = mr.transform; while (top.parent != null && !top.parent.gameObject.activeInHierarchy) top = top.parent;
                            owners[top.name] = owners.TryGetValue(top.name, out var n) ? n + 1 : 1;
                            if (force) { for (var t = mr.transform; t != null; t = t.parent) if (!t.gameObject.activeSelf) t.gameObject.SetActive(true); }
                        }
                        if (force) mr.enabled = true;
                    }
                    Plugin.Log.LogInfo($"[dev] roomson {arg}: renderers {total}, disabled {off}, inactive {inactive}; inactive roots: {string.Join(", ", owners.Select(kv => kv.Key + "x" + kv.Value).Take(15))}");
                    break;
                }
                case "facecam":
                {
                    // facecam [distance]: the extra camera right in front of her face
                    var d = PeaceDirector.Instance;
                    if (d == null || d.MitaBody == null) break;
                    float dist = string.IsNullOrEmpty(arg) ? 0.8f : float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
                    var head = d.HeadPoint;
                    var face = d.FaceForward; face.y = 0; face.Normalize();
                    var at = head + face * dist + Vector3.up * 0.05f;
                    Run($"devcam {at.x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {at.y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {at.z.ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 0 40");
                    Run($"devcam look {head.x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {head.y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {head.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    break;
                }
                case "devcam":
                {
                    // devcam x y z yaw pitch [fov] | devcam look x y z | devcam off : an extra camera drawn on top, for recordings anywhere
                    var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (sp.Length > 0 && sp[0] == "off") { if (devCam != null) UnityEngine.Object.Destroy(devCam.gameObject); devCam = null; break; }
                    if (devCam == null)
                    {
                        var go = new GameObject("DevRecordCamera");
                        devCam = go.AddComponent<Camera>();
                        devCam.depth = 50;
                        devCam.nearClipPlane = 0.05f;
                        devCam.useOcclusionCulling = false;
                        var main = Camera.main;
                        if (main != null) { devCam.cullingMask = main.cullingMask | (1 << 5) | (1 << 7) | (1 << 8); devCam.clearFlags = main.clearFlags; devCam.backgroundColor = main.backgroundColor; }
                    }
                    if (sp[0] == "look")
                    {
                        devCam.transform.LookAt(new Vector3(float.Parse(sp[1]), float.Parse(sp[2]), float.Parse(sp[3])));
                        break;
                    }
                    devCam.transform.position = new Vector3(float.Parse(sp[0]), float.Parse(sp[1]), float.Parse(sp[2]));
                    devCam.transform.rotation = Quaternion.Euler(float.Parse(sp[4]), float.Parse(sp[3]), 0);
                    devCam.fieldOfView = sp.Length > 5 ? float.Parse(sp[5]) : 60f;
                    break;
                }
                case "magnets":
                {
                    // magnets [off]: list active Transform_Magnet components (and switch off those pulling the player)
                    foreach (var m in UnityEngine.Object.FindObjectsOfType<Transform_Magnet>())
                    {
                        if (m == null || !m.isActiveAndEnabled) continue;
                        bool player = m.playerRoot || !string.IsNullOrEmpty(m.magnetPlayer);
                        Plugin.Log.LogInfo($"[dev] magnet {m.name} active={m.active} player={player} magnetPlayer='{m.magnetPlayer}' root={m.playerRoot} target={(m.transformTarget != null ? m.transformTarget.name : "-")} magnet={(m.transformMagnet != null ? m.transformMagnet.name : "-")}");
                        if (arg == "off" && player) { m.active = false; m.enabled = false; }
                    }
                    break;
                }
                case "bounds":
                {
                    foreach (var n in arg.Split(','))
                    {
                        var t = Util.FindDeepAll(n.Trim());
                        if (t == null) { Plugin.Log.LogInfo($"[dev] bounds {n}: not found"); continue; }
                        var r = t.GetComponent<Renderer>();
                        Plugin.Log.LogInfo($"[dev] bounds {n}: pos={t.position} rot={t.eulerAngles} fwd={t.forward} " + (r != null ? $"center={r.bounds.center} size={r.bounds.size}" : "no renderer"));
                    }
                    break;
                }
                case "fixhead":
                {
                    // fixhead x y z yaw pitch | fixhead off : pin the player's camera to a point (seated / lying views)
                    var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (pm == null) break;
                    if (arg == "off") { pm.fixHead = null; break; }
                    var sp = arg.Split(' ');
                    var go = GameObject.Find("DevFixHead") ?? new GameObject("DevFixHead");
                    go.transform.position = new Vector3(float.Parse(sp[0]), float.Parse(sp[1]), float.Parse(sp[2]));
                    go.transform.rotation = Quaternion.Euler(float.Parse(sp[4]), float.Parse(sp[3]), 0);
                    pm.fixHead = go.transform;
                    break;
                }
                case "cliplist":
                {
                    var names = new SortedSet<string>();
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimationClip>())) if (o != null) names.Add(o.name + " (" + o.Cast<AnimationClip>().length.ToString("0.0") + "s)");
                    File.WriteAllLines(Path.Combine(ConfigDir, "cliplist.txt"), names);
                    break;
                }
                case "clipevents":
                {
                    // animation events of the clips peace mode plays, and what Mita's eventAnimation slots call
                    var sb = new StringBuilder();
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<AnimationClip>()))
                    {
                        var c = o.Cast<AnimationClip>();
                        if (c == null || !arg.Split(',').Contains(c.name)) continue;
                        sb.AppendLine($"CLIP {c.name} len={c.length:0.00}");
                        foreach (var ev in c.events) sb.AppendLine($"   t={ev.time:0.00} {ev.functionName}(int={ev.intParameter}, str='{ev.stringParameter}', float={ev.floatParameter})");
                    }
                    var body = PeaceDirector.Instance != null ? PeaceDirector.Instance.MitaBody : null;
                    var afo = body != null ? body.GetComponent<Animator_FunctionsOverride>() : null;
                    if (afo != null && afo.eventAnimation != null)
                        for (int i = 0; i < afo.eventAnimation.Count; i++) sb.AppendLine($"eventAnimation[{i}]: {SceneDumper.EventString(afo.eventAnimation[i])}");
                    File.WriteAllText(Path.Combine(ConfigDir, "clipevents.txt"), sb.ToString(), Encoding.UTF8);
                    break;
                }
                case "animdump":
                {
                    // Mita's override controller (base clip -> override) and the clips inside each gallery model's animator
                    var sb = new StringBuilder();
                    var body = PeaceDirector.Instance != null ? PeaceDirector.Instance.MitaBody : null;
                    var afo = body != null ? body.GetComponent<Animator_FunctionsOverride>() : null;
                    if (afo != null && afo.animOver != null)
                    {
                        var baseCtrl = afo.animOver.runtimeAnimatorController;
                        sb.AppendLine("MITA base=" + (baseCtrl != null ? baseCtrl.name : "-"));
                        if (baseCtrl != null)
                            foreach (var c in baseCtrl.animationClips)
                            {
                                if (c == null) continue;
                                AnimationClip ov = null;
                                try { ov = afo.animOver[c]; } catch { }
                                sb.AppendLine($"  {c.name} ({c.length:0.00}s loop={c.isLooping}) -> {(ov != null ? ov.name : "-")}");
                            }
                    }
                    foreach (var path in arg.Split(','))
                    {
                        var o = Resources.Load(path.Trim(), Il2CppType.Of<GameObject>());
                        var go = o != null ? o.Cast<GameObject>() : null;
                        var an = go != null ? go.GetComponentInChildren<Animator>(true) : null;
                        sb.AppendLine("PREFAB " + path + " animator=" + (an != null) + " ctrl=" + (an != null && an.runtimeAnimatorController != null ? an.runtimeAnimatorController.name : "-") + " avatar=" + (an != null && an.avatar != null ? an.avatar.name + " human=" + an.avatar.isHuman : "-"));
                        if (an != null && an.runtimeAnimatorController != null)
                            foreach (var c in an.runtimeAnimatorController.animationClips)
                                if (c != null) sb.AppendLine($"  clip {c.name} ({c.length:0.00}s loop={c.isLooping} legacy={c.legacy} human={c.humanMotion})");
                    }
                    File.WriteAllText(Path.Combine(ConfigDir, "animdump.txt"), sb.ToString(), Encoding.UTF8);
                    break;
                }
                case "introstop":
                {
                    var intro = Util.FindDeepAll("AnimationPlayer Start");
                    var ia = intro != null ? intro.GetComponent<ObjectAnimationPlayer>() : null;
                    Plugin.Log.LogInfo($"[dev] intro: found={(ia != null)} active={(intro != null && intro.gameObject.activeInHierarchy)} enabled={(ia != null && ia.enabled)}");
                    if (ia != null) { try { ia.AnimationStop(); introStopped = true; } catch (Exception e) { Plugin.Log.LogWarning("[dev] intro stop: " + e.Message); } }
                    break;
                }
                case "stage":
                {
                    // stage [x z yaw dist]: park Mita at a spot (default: her kitchen spot) and put the player in front, looking at her face
                    var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float x = 12.88f, z = 1.30f, yaw = 270f, dist = 1.5f;
                    if (sp.Length >= 3) { x = float.Parse(sp[0]); z = float.Parse(sp[1]); yaw = float.Parse(sp[2]); }
                    if (sp.Length == 1) dist = float.Parse(sp[0]);
                    if (sp.Length >= 4) dist = float.Parse(sp[3]);
                    var mita = UnityEngine.Object.FindObjectOfType<MitaPerson>();
                    var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (mita == null || pm == null) break;
                    // the scene's opening animation holds the player in the bathroom until there is input: end it
                    var intro = Util.FindDeepAll("AnimationPlayer Start");
                    var introAnim = intro != null ? intro.GetComponent<ObjectAnimationPlayer>() : null;
                    if (introAnim != null && !introStopped) { try { introAnim.AnimationStop(); introStopped = true; } catch (Exception e) { Plugin.Log.LogWarning("[dev] intro stop: " + e.Message); } }
                    var nma = mita.nma;
                    var mt = nma != null ? nma.transform : mita.transform;
                    var pos = new Vector3(x, mt.position.y, z);
                    if (nma != null) { nma.Warp(pos); nma.isStopped = true; } else mt.position = pos;
                    mt.rotation = Quaternion.Euler(0, yaw, 0);
                    var ppos = pos + mt.forward * dist;
                    // a running player animation (the opening scene) puts the player back where it started once it ends:
                    // make that "saved" spot our target, stop the animation, then use the game's own teleport
                    Plugin.Log.LogInfo($"[dev] stage: player anim run={pm.animationRun} loop={pm.animationLoopRun} now={(pm.scrAnimationNow != null ? pm.scrAnimationNow.name : "-")}");
                    pm.positionPlayerSave = ppos;
                    pm.rotationPlayerSave = Quaternion.Euler(0, yaw + 180f, 0);
                    if (pm.animationRun) { try { pm.AnimationStopFull(); } catch (Exception e) { Plugin.Log.LogWarning("[dev] player anim stop: " + e.Message); } }
                    pm.TeleportPlayer(ppos, yaw + 180f);
                    staged = true; stageYaw = yaw;
                    stageTarget = ppos; stageCheck = 5;
                    Plugin.Log.LogInfo($"[dev] stage: mita -> {pos}, player -> {ppos}; player parent={(pm.transform.parent != null ? pm.transform.parent.name : "-")} pmEnabled={pm.enabled} rbKin={(pm.rb != null && pm.rb.isKinematic)} interp={(pm.rb != null ? pm.rb.interpolation.ToString() : "-")} anim={(pm.GetComponent<Animator>() != null && pm.GetComponent<Animator>().enabled)}");
                    break;
                }
                case "freeplayer":
                {
                    // release the opening scene's hold on the player (mirror view, animation anchor) and put the player somewhere
                    var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (pm == null) break;
                    Plugin.Log.LogInfo($"[dev] freeplayer: fixHead={(pm.fixHead != null ? pm.fixHead.name : "-")} animRun={pm.animationRun} anchor={(pm.animationPosition != null ? pm.animationPosition.name : "-")} save={pm.positionPlayerSave} dontMove={pm.dontMove}");
                    var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var target = sp.Length >= 3 ? new Vector3(float.Parse(sp[0]), 0f, float.Parse(sp[1])) : pm.transform.position;
                    float yaw = sp.Length >= 3 ? float.Parse(sp[2]) : pm.rotX;
                    pm.fixHead = null;
                    pm.positionPlayerSave = target;
                    pm.rotationPlayerSave = Quaternion.Euler(0, yaw, 0);
                    try { pm.AnimationStopFull(); } catch (Exception e) { Plugin.Log.LogWarning("[dev] anim stop: " + e.Message); }
                    try { pm.LookStop(); } catch { }
                    pm.dontMove = false; pm.stopMouseMove = false;
                    // the floor-safety fix would put the player back on the last "good" floor point
                    pm.pointFloorFix = target; pm.countFloorFix = 0; pm.timeFloorFix = 0f;
                    pm.TeleportPlayer(target, yaw);
                    freeTarget = target; freeFrames = 30;
                    break;
                }
                case "stagehere":
                {
                    // stagehere [dist] [side]: bring Mita in front of the player's camera (the player may be held at the opening spot)
                    var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float dist = sp.Length > 0 ? float.Parse(sp[0]) : 1.3f;
                    float side = sp.Length > 1 ? float.Parse(sp[1]) : 0f;
                    var mita = UnityEngine.Object.FindObjectOfType<MitaPerson>();
                    var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (mita == null || pm == null || pm.mainCamera == null) break;
                    var cam = pm.mainCamera;
                    var fwd = cam.forward; fwd.y = 0; fwd.Normalize();
                    var right = Vector3.Cross(Vector3.up, fwd);
                    var pos = cam.position + fwd * dist + right * side;
                    pos.y = pm.transform.position.y;
                    var nma = mita.nma;
                    var mt = nma != null ? nma.transform : mita.transform;
                    if (nma != null) { nma.Warp(pos); nma.isStopped = true; } else mt.position = pos;
                    // experiment: which root rotation shows her face (sp[2] = "flip" turns the root away from the camera)
                    bool flip = !(sp.Length > 2 && sp[2] == "back");   // her face is on the agent's -Z side
                    mt.rotation = Quaternion.LookRotation(flip ? fwd : -fwd);
                    if (nma != null) nma.updateRotation = false;
                    stagedMita = mt; stageFlip = flip;
                    staged = true;
                    PeaceDirector.Instance?.DevLookAtPlayer();
                    var hips = Util.FindDeep(mt, "Hips");
                    Plugin.Log.LogInfo($"[dev] stagehere: mita -> {pos} (camera {cam.position}) rootFwd={mt.forward} hipsFwd={(hips != null ? hips.forward.ToString() : "-")}");
                    break;
                }
                case "unstage":
                {
                    staged = false; stagedMita = null;
                    var mita = UnityEngine.Object.FindObjectOfType<MitaPerson>();
                    if (mita != null && mita.nma != null) { mita.nma.isStopped = false; mita.nma.updateRotation = true; }
                    break;
                }
                case "mitaclip":
                {
                    var sp = arg.Split(' ', 2);
                    PeaceDirector.Instance?.DevClip(sp[0], sp[1]);
                    break;
                }
                case "walkto":
                {
                    // walkto <x> <z> [~timeout] | walkto @<object path> [~timeout]: walk there like a player (blocks the script)
                    float timeout = 40f;
                    int tilde = arg.LastIndexOf(" ~");
                    if (tilde >= 0) { timeout = float.Parse(arg.Substring(tilde + 2)); arg = arg.Substring(0, tilde); }
                    Vector3 target;
                    if (arg.StartsWith("mita"))
                    {
                        // walkto mita [distance]: up to her, stopping that far away on your side
                        var body = PeaceDirector.Instance != null ? PeaceDirector.Instance.MitaBody : null;
                        var pmw = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                        if (body == null || pmw == null) { Plugin.Log.LogWarning("[dev] walkto mita: no mita/player"); break; }
                        var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        float dist = sp.Length > 1 ? float.Parse(sp[1], System.Globalization.CultureInfo.InvariantCulture) : 1.6f;
                        var away = pmw.transform.position - body.position; away.y = 0;
                        target = body.position + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * dist;
                    }
                    else if (arg.StartsWith("@"))
                    {
                        var go = Util.FindPath(arg.Substring(1));
                        if (go == null) { Plugin.Log.LogWarning("[dev] walkto: no " + arg); break; }
                        // (a disabled collider has no bounds: use the box's own center)
                        var box = go.GetComponent<BoxCollider>();
                        target = box != null ? go.transform.TransformPoint(box.center) : go.transform.position;
                    }
                    else
                    {
                        var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        target = new Vector3(float.Parse(sp[0]), 0f, float.Parse(sp[1]));
                    }
                    DevWalk.Start(target, timeout);
                    break;
                }
                case "click":
                {
                    // click <object path>: what the player's click on an interactive object does
                    // the live copy first (our inactive templates carry the same names)
                    GameObject go = null;
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<ObjectInteractive>()))
                    {
                        var c = o.Cast<ObjectInteractive>();
                        if (c != null && c.gameObject.name == arg && c.gameObject.scene.IsValid() && (go == null || c.gameObject.activeInHierarchy)) go = c.gameObject;
                    }
                    if (go == null) go = Util.FindPath(arg);
                    var oi = go != null ? go.GetComponent<ObjectInteractive>() : null;
                    Plugin.Log.LogInfo($"[dev] click {arg}: {(oi == null ? "no interactive" : "active=" + go.activeInHierarchy + " enabled=" + oi.enabled + " oi.active=" + oi.active)}");
                    // Click() only acts while the player's view is on the object (oi.active): otherwise fire its event directly
                    if (oi != null) { if (oi.active) oi.Click(); else oi.eventClick?.Invoke(); }
                    break;
                }
                case "importcook":
                    PeaceDirector.Instance?.DevImportCooking();
                    waitUntil = Time.realtimeSinceStartup + 5f;
                    break;
                case "cook": PeaceDirector.Instance?.DevCook(); break;   // "같이 요리하기" at noon
                case "cookstate": Plugin.Log.LogInfo("[dev] cook: " + PeaceDirector.Instance?.DevCookState()); break;
                case "cut": PeaceDirector.Instance?.DevCut(); break;     // one knife stroke
                case "cutfinish": PeaceDirector.Instance?.DevCutFinish(); break;
                case "mouse":   // mouse hold | release | clicks <n> [seconds apart]
                {
                    var sp = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (sp[0] == "clicks") DevWalk.Clicks(int.Parse(sp[1]), sp.Length > 2 ? float.Parse(sp[2], System.Globalization.CultureInfo.InvariantCulture) : 0.35f);
                    else DevWalk.SetHold(sp[0] == "hold");
                    break;
                }
                case "cutauto": PeaceDirector.Instance?.DevCutAuto(); break;
                case "eat": PeaceDirector.Instance?.DevEatKey(); break;   // press the meal's eat key
                case "icon": PeaceDirector.Instance?.DevIcon(arg != "off"); break;
                case "routine": PeaceDirector.Instance?.DevRoutine(arg); break;   // routine meal|bed|wake|ready
                case "animevents": File.WriteAllText(Path.Combine(ConfigDir, "animevents.txt"), PeaceDirector.Instance?.DevAnimEvents(arg) ?? "-", Encoding.UTF8); break;
                case "mitago":
                {
                    var sp = arg.Split(' ');
                    PeaceDirector.Instance?.DevMitaGo(new Vector3(float.Parse(sp[0], System.Globalization.CultureInfo.InvariantCulture), 0f, float.Parse(sp[1], System.Globalization.CultureInfo.InvariantCulture)));
                    break;
                }
                case "choices": PeaceDirector.Instance?.DevChoices(arg); break;          // choices a|b|c: the story's choice list
                case "lookat":
                {
                    // lookat @<path> | lookat x y z: turn the player's view to a thing
                    var pml = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    Vector3 to;
                    if (arg.StartsWith("@"))
                    {
                        var go = Util.FindPath(arg.Substring(1));
                        if (go == null || pml == null) { Plugin.Log.LogWarning("[dev] lookat: no " + arg); break; }
                        var mf = go.GetComponent<MeshFilter>();
                        to = mf != null && mf.sharedMesh != null ? go.transform.TransformPoint(mf.sharedMesh.bounds.center) : go.transform.position;
                    }
                    else if (arg == "mita") { if (PeaceDirector.Instance == null) break; to = PeaceDirector.Instance.HeadPoint; }
                    else { var sp = arg.Split(' '); to = new Vector3(float.Parse(sp[0]), float.Parse(sp[1]), float.Parse(sp[2])); }
                    if (pml == null || pml.mainCamera == null) break;
                    var d = to - pml.mainCamera.position;
                    float flat = new Vector2(d.x, d.z).magnitude;
                    pml.rotX = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z)).eulerAngles.y;
                    pml.rotY = -Mathf.Atan2(d.y, flat) * Mathf.Rad2Deg;
                    Plugin.Log.LogInfo($"[dev] lookat {arg}: {to}");
                    break;
                }
                case "turn": { var pmt = UnityEngine.Object.FindObjectOfType<PlayerMove>(); if (pmt != null) pmt.rotX += float.Parse(arg); break; }   // turn <deg>: the view, right = +
                case "aitest":
                    // aitest <endpoint>: AI chat on, against a test server (not written to the config file)
                    PeaceMode.AiEnabled.ConfigFile.SaveOnConfigSet = false;
                    PeaceMode.AiEnabled.Value = true;
                    PeaceMode.AiEndpoint.Value = arg;
                    break;
                case "chatopen": PeaceDirector.Instance?.DevChatOpen(arg); break;   // chatopen [text]: the typing box, with that text in it
                case "afk": { var g = UnityEngine.Object.FindObjectOfType<GameController>(); Plugin.Log.LogInfo($"[dev] afk: timeAFK={(g != null ? g.timeAFK : -1f):0.0} needTetris={(g != null && g.needTetris)} tetrisPlay={(g != null && g.tetrisPlay)}"); break; }
                case "glasses": PeaceDirector.Instance?.DevGlasses(arg); break;   // glasses on|off|pos x y z [rx ry rz]
                case "chat": PeaceDirector.Instance?.DevChat(arg); break;   // chat <text>: say that to her (AI chat)
                case "chatprompt": File.WriteAllText(Path.Combine(ConfigDir, "chatprompt.txt"), PeaceDirector.Instance?.DevChatPrompt() ?? "", Encoding.UTF8); break;
                case "game": PeaceDirector.Instance?.DevGame(arg); break;   // game follow|stay|request|words|riddle|hide|found|cham
                case "gamestate": Plugin.Log.LogInfo("[dev] game: " + PeaceDirector.Instance?.DevGameState()); break;
                case "spot": PeaceDirector.Instance?.DevSpot(arg); break;       // spot <id>: as if you clicked that thing
                case "spots": Plugin.Log.LogInfo("[dev] spots: " + PeaceDirector.Instance?.DevSpots()); break;
                case "housedump": PeaceDirector.Instance?.DevHouseDump(); break;
                case "cloth": PeaceDirector.Instance?.DevCloth(arg); break;   // cloth <key> [variant]
                case "clothdump":
                {
                    // the costumes the game knows (MitaClothesResource) and the Mita wearing them (MitaClothes)
                    var sb = new StringBuilder();
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<MitaClothesResource>()))
                    {
                        var r = o.Cast<MitaClothesResource>();
                        sb.AppendLine($"resource {Util.PathOf(r.transform)} scene='{r.gameObject.scene.name}' clothes {r.clothes?.Count}");
                        if (r.clothes != null)
                            for (int i = 0; i < r.clothes.Count; i++)
                            {
                                var c = r.clothes[i];
                                string name = "?";
                                try { name = GlobalLanguage.GetString("Menu", c.stringName - 1); } catch { }
                                sb.AppendLine($"  {i}: #{c.stringName} '{name}' save='{c.fileSave}' mod='{c.modNameCloth}' variants {c.variants?.Count} body={(c.bodyMesh != null ? c.bodyMesh.name : "-")} sweater={(c.sweaterMesh != null ? c.sweaterMesh.name : "-")} prefab={(c.prefabFull != null ? c.prefabFull.name : "-")}");
                            }
                    }
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<MitaClothes>()))
                    {
                        var m = o.Cast<MitaClothes>();
                        sb.AppendLine($"clothes on {Util.PathOf(m.transform)} scene='{m.gameObject.scene.name}' mad={m.isMadMita} body={(m.rendBody != null && m.rendBody.sharedMesh != null ? m.rendBody.sharedMesh.name : "-")} sweater={(m.rendSweater != null && m.rendSweater.sharedMesh != null ? m.rendSweater.sharedMesh.name : "-")}");
                    }
                    foreach (var key in new[] { "Cloth", "Clothes", "ClothMita", "MitaCloth", "Dress" })
                        try { sb.AppendLine($"data {key}: exists={GlobalAM.ExistsData(key)} {(GlobalAM.ExistsData(key) ? string.Join("|", (string[])GlobalAM.LoadData(key)) : "")}"); } catch (Exception e) { sb.AppendLine($"data {key}: {e.Message}"); }
                    File.WriteAllText(Path.Combine(ConfigDir, "clothdump.txt"), sb.ToString(), Encoding.UTF8);
                    break;
                }
                case "pick": PeaceDirector.Instance?.DevPick(arg); break;
                case "keepawake": KeepAwake = arg != "off"; break;   // keepawake off: let the AFK console come out (for testing it)
                case "blendshapes":   // her face's blend shapes (original Mita and her skin): index name weight
                {
                    var body = PeaceDirector.Instance?.MitaBody;
                    if (body == null) break;
                    var root = body.root;
                    foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        var mesh = smr.sharedMesh;
                        if (mesh == null || mesh.blendShapeCount == 0) continue;
                        var sb = new StringBuilder();
                        for (int i = 0; i < mesh.blendShapeCount; i++) sb.Append(i).Append(':').Append(mesh.GetBlendShapeName(i)).Append('=').Append(smr.GetBlendShapeWeight(i).ToString("0")).Append("  ");
                        Plugin.Log.LogInfo($"[dev] blendshapes {Util.PathOf(smr.transform)} ({mesh.blendShapeCount}): {sb}");
                    }
                    break;
                }
                case "aff": PeaceDirector.Instance?.DevSetAff(int.Parse(arg)); break;   // aff <n>: her affection now (steps react)
                case "weather": PeaceDirector.Instance?.DevWeather(arg); break;   // weather clear|cloudy|rain|snow
                case "dance": PeaceDirector.Instance?.DevDance(arg); break;       // dance start|menu 0/1|pick|pad|key 0-2|state|auto [off] [hitAt] [more]: Cappie's dance battle
                case "hammer": PeaceDirector.Instance?.DevHammer(arg); break;     // hammer start|auto [off]|press|state|lines <from> <to>: Cappie's hammer game
                case "lounge": PeaceDirector.Instance?.DevLounge(arg); break;     // lounge tv|read|rest | lounge cal <mitaSide> <hipsUp> <hipsBack> <playerSide> <playerBack> <playerYaw>
                case "propose": PeaceDirector.Instance?.DevPropose(arg); break;   // propose [id]: she suggests something now (talk|rps|cham|hide|words|riddle|photo|window|walk|cook|sleep|special|tv|read|rest)
                case "gestureprobe": PeaceDirector.Instance?.DevGestureProbe(arg); break;   // gestureprobe [clip,clip]: play her gestures one by one, log how far each moves her
                case "gestureall": PeaceDirector.DevAllGestures = arg != "off"; break;   // gestureall [off]: a talk gesture on every line (recording)
                case "name": PeaceDirector.DevPlayerName = arg == "off" ? null : arg; break;   // name [text|off]: what she calls you, this run only (no text: the plain "player")
                case "contempt": PeaceDirector.Instance?.DevContempt(arg != "off"); break;   // contempt [off]: her scornful face
                case "bedview": PeaceDirector.Instance?.DevBedView(arg != "off"); break;   // the pillow view of her face (bed)
                case "lifeemo":   // lifeemo [on|off]: the story's MitaLifeEmotion (shy when stared at): its state and lines
                {
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<MitaLifeEmotion>()))
                    {
                        var le = o.Cast<MitaLifeEmotion>();
                        if (le == null || !le.gameObject.scene.IsValid()) continue;
                        if (arg == "on") le.Activation(true); else if (arg == "off") le.Activation(false);
                        string Names(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GameObject> a) => a == null ? "-" : string.Join(" | ", a.Select(g => g == null ? "null" : Util.PathOf(g.transform) + "#" + (g.GetComponent<Dialogue_3DText>()?.indexString ?? -1)));
                        Plugin.Log.LogInfo($"[dev] lifeemo {Util.PathOf(le.transform)} active={le.active} enabled={le.enabled} go={le.gameObject.activeInHierarchy} timeShy={le.timeShy:0.0} levelShy={le.levelShy} look={le.lookHead} type={le.typeEmotionLongSee} head={(le.colliderHead != null ? Util.PathOf(le.colliderHead.transform) : "-")} layer={le.layerPart.value} || shy: {Names(le.dialogueShy)} || far: {Names(le.dialogueFarAway)} || close: {Names(le.dialogueClosely)}");
                    }
                    break;
                }
                case "bedcam": { var sp = arg.Split(' '); PeaceDirector.BedCamAcross = float.Parse(sp[0], System.Globalization.CultureInfo.InvariantCulture); PeaceDirector.BedCamUp = float.Parse(sp[1], System.Globalization.CultureInfo.InvariantCulture); break; }
                case "scenedump":   // scenedump <scene>: borrow a story scene and write scene_<n>.txt (its whole tree, components, events, positions)
                    SceneImport.Load(arg, s => File.WriteAllText(Path.Combine(ConfigDir, "scene_" + arg.Split(' ')[1] + ".txt"), SceneDumper.DumpScene(s), Encoding.UTF8));
                    break;
                case "animscan":   // animscan <scene>: borrow a story scene and write anim_<n>.txt (clips, animation players, events)
                    PeaceDirector.DevScanBefore = new HashSet<int>();
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<AnimationClip>())) if (o != null) PeaceDirector.DevScanBefore.Add(o.GetInstanceID());
                    SceneImport.Load(arg, s => File.WriteAllText(Path.Combine(ConfigDir, "anim_" + arg.Split(' ')[1] + ".txt"), PeaceDirector.AnimScan(s), Encoding.UTF8));
                    break;
                case "lang": Loc.UseFont(false); Loc.Init(arg); Loc.UseFont(true); Plugin.Log.LogInfo($"[dev] lang={Loc.Lang} game={GlobalGame.Language}"); break;   // lang en|ja|zh|ko|auto
                case "choiceescape": PeaceDirector.Instance?.DevChoiceEscape(); break;
                case "choicestate": Plugin.Log.LogInfo("[dev] choice: " + PeaceDirector.Instance?.DevChoiceState()); break;
                case "choiceclick": PeaceDirector.Instance?.DevChoiceClick(int.Parse(arg)); break;
                case "music": Plugin.Log.LogInfo("[dev] music: " + PeaceDirector.Instance?.DevMusic()); break;
                case "muteevents": PeaceDirector.Instance?.DevMuteEvents(arg != "off"); break;
                case "mitawalk": PeaceDirector.Instance?.DevMitaWalkTo(Util.FindPath(arg)?.transform); break;
                case "matdump":
                {
                    // matdump <name>[,<name>...]: shader and color/float properties of each renderer's materials
                    var sb = new StringBuilder();
                    foreach (var n in arg.Split(','))
                    {
                        var go = Util.FindPath(n.Trim());
                        if (go == null) { sb.AppendLine("## " + n + ": none"); continue; }
                        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        {
                            sb.AppendLine($"## {n} / {r.name} ({r.GetIl2CppType().Name}) on={r.enabled && r.gameObject.activeInHierarchy}");
                            var mats = r.sharedMaterials;
                            for (int m = 0; m < mats.Length; m++)
                            {
                                var mat = mats[m];
                                if (mat == null) continue;
                                var sh = mat.shader;
                                sb.AppendLine($"  [{m}] {mat.name} shader={(sh != null ? sh.name : "-")} kw={string.Join(" ", mat.shaderKeywords)}");
                                if (sh == null) continue;
                                int pc = sh.GetPropertyCount();
                                for (int i = 0; i < pc; i++)
                                {
                                    var pn = sh.GetPropertyName(i);
                                    var pt = sh.GetPropertyType(i);
                                    string v = pt == UnityEngine.Rendering.ShaderPropertyType.Color ? mat.GetColor(pn).ToString()
                                             : pt == UnityEngine.Rendering.ShaderPropertyType.Float || pt == UnityEngine.Rendering.ShaderPropertyType.Range ? mat.GetFloat(pn).ToString("0.###")
                                             : pt == UnityEngine.Rendering.ShaderPropertyType.Texture ? (mat.GetTexture(pn) != null ? mat.GetTexture(pn).name : "-") : pt.ToString();
                                    sb.AppendLine($"     {pn} {pt} = {v}");
                                }
                            }
                        }
                    }
                    File.WriteAllText(Path.Combine(ConfigDir, "matdump.txt"), sb.ToString(), Encoding.UTF8);
                    break;
                }
                case "invoke":
                {
                    // invoke <object path>|<component type>|<method>[|<bool/int/float/string arg>]: call a game method for experiments
                    var sp = arg.Split('|');
                    var go = Util.FindPath(sp[0].Trim());
                    var type = sp.Length > 1 ? SceneDumper.FindType(sp[1].Trim()) : null;
                    if (go == null || type == null) { Plugin.Log.LogWarning($"[dev] invoke: object {go != null} type {type != null}"); break; }
                    Component comp = null;
                    foreach (var c in go.GetComponents<Component>()) if (c != null && c.GetIl2CppType().Name == sp[1].Trim()) { comp = c; break; }
                    if (comp == null) { Plugin.Log.LogWarning("[dev] invoke: no " + sp[1] + " on " + sp[0]); break; }
                    var wrapper = Activator.CreateInstance(type, new object[] { comp.Pointer });
                    var name = sp[2].Trim();
                    System.Reflection.MethodInfo mi = null;
                    foreach (var m in type.GetMethods()) if (m.Name == name && m.GetParameters().Length == (sp.Length > 3 ? 1 : 0)) { mi = m; break; }
                    if (mi == null) { Plugin.Log.LogWarning("[dev] invoke: no method " + name); break; }
                    object[] args = null;
                    if (sp.Length > 3)
                    {
                        var pt = mi.GetParameters()[0].ParameterType; var a = sp[3].Trim();
                        args = new object[] { pt == typeof(bool) ? (object)bool.Parse(a) : pt == typeof(int) ? int.Parse(a) : pt == typeof(float) ? float.Parse(a, System.Globalization.CultureInfo.InvariantCulture) : a };
                    }
                    var res = mi.Invoke(wrapper, args);
                    Plugin.Log.LogInfo($"[dev] invoke {sp[1]}.{name} on {sp[0]} -> {res}");
                    break;
                }
                case "lightdump":
                {
                    var sb = new StringBuilder();
                    string P(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + ">" + s; } return s; }
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<Light>()))
                    {
                        var l = o.Cast<Light>();
                        if (l == null || !l.gameObject.scene.IsValid()) continue;
                        sb.AppendLine($"{P(l.transform)} type={l.type} on={l.enabled && l.gameObject.activeInHierarchy} int={l.intensity:0.###} range={l.range:0.#} color={l.color} shadows={l.shadows} pos={l.transform.position}");
                    }
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<ParticleSystem>()))
                    {
                        var p = o.Cast<ParticleSystem>();
                        if (p == null || !p.gameObject.scene.IsValid()) continue;
                        if (!(p.name.Contains("Sun") || p.name.Contains("Star") || p.name.Contains("Window") || p.name.Contains("Background") || p.name.Contains("Dust"))) continue;
                        sb.AppendLine($"PS {P(p.transform)} on={p.gameObject.activeInHierarchy} playing={p.isPlaying}");
                    }
                    RenderSettingsDump(sb);
                    File.WriteAllText(Path.Combine(ConfigDir, "lightdump.txt"), sb.ToString(), Encoding.UTF8);
                    break;
                }
                case "cardstop":
                {
                    // end the card game as if the last round were over
                    var g = UnityEngine.Object.FindObjectOfType<Location4TableCardGame>();
                    Plugin.Log.LogInfo($"[dev] cardstop: {(g == null ? "no game" : $"play={g.play} step={g.stepGame} score {g.scorePlayer}:{g.scoreMita}")}");
                    g?.StopGame();
                    break;
                }
                case "pmstate":
                {
                    var pm = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (pm == null) { Plugin.Log.LogInfo("[dev] pmstate: no player"); break; }
                    Plugin.Log.LogInfo($"[dev] pmstate: pos={pm.transform.position} rotX={pm.rotX:0} rotY={pm.rotY:0} dontMove={pm.dontMove} stopMouse={pm.stopMouseMove} " +
                                       $"fixHead={(pm.fixHead != null ? pm.fixHead.name : "-")} animRun={pm.animationRun} anchor={(pm.animationPosition != null ? pm.animationPosition.name : "-")} " +
                                       $"rbKin={(pm.rb != null && pm.rb.isKinematic)} moveH={pm.moveH} moveV={pm.moveV} speed={pm.speed} paused={Time.timeScale == 0}");
                    break;
                }
                case "mitawhere": PeaceDirector.Instance?.DevWhere(); break;
                case "poseseat": PeaceDirector.Instance?.DevPoseSeat(); break;
                case "sleepnow": PeaceDirector.Instance?.DevSleep(); break;
                case "mealnow": PeaceDirector.Instance?.DevMeal(); break;
                case "persona":
                    PeaceDirector.Instance?.DevPersona(arg);
                    break;
                case "gesture":
                    // gesture nod|shake: wiggle the player's view for ~1.2 s like a mouse shake
                    gestureKind = arg; gestureStart = Time.realtimeSinceStartup;
                    var pmg = UnityEngine.Object.FindObjectOfType<PlayerMove>();
                    if (pmg != null) { gestureBaseX = pmg.rotX; gestureBaseY = pmg.rotY; }
                    break;
                case "emo":
                    PeaceDirector.Instance?.DevEmotion(arg);
                    break;
                case "sounddump":
                {
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<DataValues_Sounds>()))
                    {
                        var ds = o.Cast<DataValues_Sounds>();
                        if (ds == null || ds.gameObject.scene.name == null) continue;
                        var names = new List<string>();
                        if (ds.sounds != null) foreach (var c in ds.sounds) names.Add(c != null ? $"{c.name}({c.length:0.00}s)" : "null");
                        var a = ds.audioSource;
                        Plugin.Log.LogInfo($"[dev] sounds {ds.name} clips=[{string.Join(",", names)}] loop={(ds.soundLoop != null ? ds.soundLoop.name + "(" + ds.soundLoop.length.ToString("0.00") + "s)" : "-")} loopVol={ds.volumeSoundLoop} " +
                                           $"src={(a != null ? a.name : "-")} srcOn={(a != null && a.isActiveAndEnabled)} vol={(a != null ? a.volume : -1)} mute={(a != null && a.mute)} clip={(a != null && a.clip != null ? a.clip.name : "-")} playing={(a != null && a.isPlaying)} blend={(a != null ? a.spatialBlend : -1)} fs={ds.fs}");
                    }
                    break;
                }
                case "uidump":
                {
                    // uidump <root canvas name>: hierarchy with visibility (active, CanvasGroup alpha, graphic alpha, text)
                    var sb = new StringBuilder();
                    foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>())
                    {
                        if (cv == null || !cv.isRootCanvas || cv.name != arg) continue;
                        void W(Transform t, int depth)
                        {
                            var info = new List<string>();
                            var cg = t.GetComponent<CanvasGroup>(); if (cg != null) info.Add($"cg={cg.alpha:0.00}");
                            var cvv = t.GetComponent<Canvas>(); if (cvv != null) info.Add($"canvasOn={cvv.enabled}");
                            var g = t.GetComponent<UnityEngine.UI.Graphic>(); if (g != null) info.Add($"a={g.color.a:0.00} on={g.enabled}");
                            var tx = t.GetComponent<UnityEngine.UI.Text>(); if (tx != null) info.Add("'" + tx.text + "'");
                            var comps = new List<string>();
                            foreach (var c in t.GetComponents<Component>()) if (c != null) comps.Add(c.GetIl2CppType().Name);
                            sb.AppendLine(new string(' ', depth * 2) + (t.gameObject.activeSelf ? "+ " : "- ") + t.name + " {" + string.Join(",", comps) + "} " + string.Join(" ", info));
                            for (int i = 0; i < t.childCount && depth < 5; i++) W(t.GetChild(i), depth + 1);
                        }
                        W(cv.transform, 0);
                    }
                    File.WriteAllText(Path.Combine(ConfigDir, "ui_" + arg + "_" + Time.frameCount + ".txt"), sb.ToString(), Encoding.UTF8);
                    break;
                }
                case "posedump":
                    File.WriteAllText(Path.Combine(ConfigDir, "pose_" + arg.Replace('/', '_') + ".txt"),
                        MitaSkin.PoseDump(arg, PeaceDirector.Instance != null ? PeaceDirector.Instance.MitaBody : null), Encoding.UTF8);
                    break;
                case "record":
                {
                    var sp = arg.Split(' ');
                    RecordStart(sp[0], float.Parse(sp[1], System.Globalization.CultureInfo.InvariantCulture), sp.Length > 2 ? int.Parse(sp[2]) : 30);
                    break;
                }
                case "recordav":
                {
                    var sp = arg.Split(' ');
                    int ifps = sp.Length > 2 ? int.Parse(sp[2]) : 60;
                    RecordStart(sp[0], float.Parse(sp[1], System.Globalization.CultureInfo.InvariantCulture), ifps,
                        sp.Length > 3 ? int.Parse(sp[3]) : 1920, sp.Length > 4 ? int.Parse(sp[4]) : 1080, true);
                    break;
                }
                case "ffmpeg": AvRecorder.FfmpegPath = arg.Trim().Trim('"'); break;
                case "avenc": AvRecorder.Encoder = arg.Trim(); break;                                   // avenc nvenc|qsv|x264|auto
                case "avframe": AvRecorder.FrameSeconds = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture); break;   // avframe <real seconds a frame> (a multiple of 1/fps)
                case "avmsaa": AvRecorder.Msaa = int.Parse(arg); break;                                 // avmsaa 1|2|4|8
                case "musicoff": PeaceDirector.DevMusicOff = arg != "off"; break;                       // the house music silent (a video with its own music)
                case "screen":   // screen <w> <h> [full]: the game window's size (small while recording video; the capture keeps its own size)
                {
                    var sp = arg.Split(' ');
                    // (screen native: back to the display's own size, full screen - what the game had before a script shrank it)
                    if (sp[0] == "native") { Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow); Plugin.Log.LogInfo($"[dev] screen -> native {Display.main.systemWidth}x{Display.main.systemHeight}"); break; }
                    Screen.SetResolution(int.Parse(sp[0]), int.Parse(sp[1]), sp.Length > 2 && sp[2] == "full" ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
                    Plugin.Log.LogInfo($"[dev] screen -> {arg}");
                    break;
                }
                case "metronome": AvRecorder.MetronomeEvery = int.Parse(arg); break;   // metronome <frames> (0 off): a click every N recorded frames, a sync check   // ffmpeg <path to ffmpeg.exe> (for recordav)
                case "camframes":
                {
                    // camframes <prefix> <count> <interval>: a series of camshots (frames/<prefix>_000.png ...)
                    var sp = arg.Split(' ');
                    camframePrefix = sp[0]; camframeLeft = int.Parse(sp[1]); camframeInterval = float.Parse(sp[2]);
                    camframeIndex = 0; camframeNext = 0;
                    Directory.CreateDirectory(Path.Combine(ConfigDir, "frames"));
                    waitUntil = Time.realtimeSinceStartup + camframeLeft * camframeInterval + 0.5f;
                    break;
                }
                case "camshot":
                    camshotPath = Path.Combine(ConfigDir, arg);
                    camshotStage = 0;
                    waitUntil = Time.realtimeSinceStartup + 0.3f;
                    break;
            }
        }
    }

    // Lets dev scripts simulate a key press for one frame (read by our own systems only).
    public static class DevInput
    {
        public static string Pressed;
        public static int Frame;
        public static void Press(string k) { Pressed = k; Frame = Time.frameCount; }
        public static bool Get(string k) => Pressed == k && Time.frameCount <= Frame + 1;
    }

    public static class SceneDumper
    {
        public static string Dump(bool positions = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"GlobalGame.LoadingLevel={GlobalGame.LoadingLevel} levelLoad={GlobalGame.levelLoad}");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                sb.AppendLine($"### SCENE {s.name} loaded={s.isLoaded}");
                foreach (var root in s.GetRootGameObjects()) Walk(root.transform, 0, sb, positions);
            }
            var probe = new GameObject("__probe");
            UnityEngine.Object.DontDestroyOnLoad(probe);
            var dd = probe.scene;
            sb.AppendLine("### SCENE DontDestroyOnLoad");
            foreach (var root in dd.GetRootGameObjects()) if (root != probe) Walk(root.transform, 0, sb);
            UnityEngine.Object.Destroy(probe);
            return sb.ToString();
        }

        // one borrowed story scene (SceneImport), with positions: what its objects are and where they stand
        public static string DumpScene(Scene s)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"### SCENE {s.name}");
            foreach (var root in s.GetRootGameObjects()) Walk(root.transform, 0, sb, true);
            return sb.ToString();
        }

        static void Walk(Transform t, int depth, StringBuilder sb) => Walk(t, depth, sb, false);

        static void Walk(Transform t, int depth, StringBuilder sb, bool positions)
        {
            var go = t.gameObject;
            var comps = go.GetComponents<Component>();
            var names = new List<string>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                var n = c.GetIl2CppType().Name;
                if (n == "Transform") continue;
                names.Add(n);
            }
            sb.Append(' ', depth * 2).Append(go.activeSelf ? "+ " : "- ").Append(go.name);
            if (names.Count > 0) sb.Append("  {").Append(string.Join(",", names)).Append('}');
            if (positions) sb.Append($"  @{t.position:F2} r{t.eulerAngles.y:F0}");
            sb.AppendLine();
            foreach (var c in comps)
            {
                if (c == null) continue;
                try { Describe(c, depth, sb); } catch (Exception e) { sb.Append(' ', depth * 2 + 4).AppendLine("!describe " + e.Message); }
            }
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1, sb, positions);
        }

        public static string EventString(UnityEventBase ev)
        {
            if (ev == null) return null;
            var group = ev.m_PersistentCalls;
            if (group == null || group.m_Calls == null || group.m_Calls.Count == 0) return null;
            var sb = new StringBuilder();
            for (int i = 0; i < group.m_Calls.Count; i++)
            {
                var call = group.m_Calls[i];
                var tgt = call.m_Target;
                string tn = tgt == null ? "null" : tgt.name + "(" + tgt.GetIl2CppType().Name + ")";
                sb.Append(tn).Append('.').Append(call.m_MethodName);
                var a = call.m_Arguments;
                if (a != null)
                {
                    switch (call.m_Mode)
                    {
                        case PersistentListenerMode.Int: sb.Append('(').Append(a.m_IntArgument).Append(')'); break;
                        case PersistentListenerMode.Float: sb.Append('(').Append(a.m_FloatArgument).Append(')'); break;
                        case PersistentListenerMode.String: sb.Append("(\"").Append(a.m_StringArgument).Append("\")"); break;
                        case PersistentListenerMode.Bool: sb.Append('(').Append(a.m_BoolArgument).Append(')'); break;
                        case PersistentListenerMode.Object: sb.Append('(').Append(a.m_ObjectArgument != null ? a.m_ObjectArgument.name : "null").Append(')'); break;
                    }
                }
                sb.Append("; ");
            }
            return sb.ToString();
        }

        static void Ev(string label, UnityEventBase ev, int depth, StringBuilder sb)
        {
            var s = EventString(ev);
            if (s == null) return;
            sb.Append(' ', depth * 2 + 4).Append(label).Append(": ").AppendLine(s);
        }

        static void Describe(Component c, int depth, StringBuilder sb)
        {
            string pad = new string(' ', depth * 2 + 4);
            var type = c.GetIl2CppType().Name;
            switch (type)
            {
                case "World":
                {
                    var w = c.Cast<World>();
                    sb.AppendLine($"{pad}World nameLocation={w.nameLocation} levelLoad={(w.levelLoad == null ? 0 : w.levelLoad.Length)}");
                    if (w.levelLoad != null)
                        for (int i = 0; i < w.levelLoad.Length; i++)
                        {
                            var ll = w.levelLoad[i];
                            sb.AppendLine($"{pad}  levelLoad[{i}] pos={ll.positionPlayer}");
                            Ev($"  levelLoad[{i}].eventLoad", ll.eventLoad, depth, sb);
                        }
                    break;
                }
                case "Dialogue_3DText":
                {
                    var d = c.Cast<Dialogue_3DText>();
                    sb.AppendLine($"{pad}Dialogue idx={d.indexString} speaker={(d.speaker != null ? d.speaker.name : "-")} next={(d.nextText != null ? d.nextText.name : "-")}");
                    break;
                }
            }
            GenericProps(c, type, depth, sb);
        }

        static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        public static Type FindType(string name)
        {
            if (typeCache.TryGetValue(name, out var t)) return t;
            t = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var an = asm.GetName().Name;
                if (an != "Assembly-CSharp" && an != "Assembly-CSharp-firstpass") continue;
                t = asm.GetType(name);
                if (t != null) break;
            }
            typeCache[name] = t;
            return t;
        }

        static void GenericProps(Component c, string type, int depth, StringBuilder sb)
        {
            var t = FindType(type);
            if (t == null) return;
            object w;
            try { w = Activator.CreateInstance(t, new object[] { c.Pointer }); } catch { return; }
            foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                try
                {
                    if (typeof(UnityEventBase).IsAssignableFrom(p.PropertyType))
                        Ev(type + "." + p.Name, p.GetValue(w) as UnityEventBase, depth, sb);
                    else if (p.PropertyType == typeof(string) || p.PropertyType == typeof(int) || p.PropertyType == typeof(bool) || p.PropertyType == typeof(float))
                    {
                        if (!type.StartsWith("Location") && !type.StartsWith("Trigger") && !type.StartsWith("Mita") && !type.StartsWith("Events_") && !type.StartsWith("Minigames") && type != "ObjectInteractive") continue;
                        sb.Append(' ', depth * 2 + 4).Append(type).Append('.').Append(p.Name).Append('=').AppendLine(Convert.ToString(p.GetValue(w)));
                    }
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(p.PropertyType) && (type.StartsWith("Location") || type.StartsWith("Minigames")))
                    {
                        var o = p.GetValue(w) as UnityEngine.Object;
                        if (o != null) sb.Append(' ', depth * 2 + 4).Append(type).Append('.').Append(p.Name).Append("->").AppendLine(o.name);
                    }
                }
                catch { }
            }
        }
    }
}
