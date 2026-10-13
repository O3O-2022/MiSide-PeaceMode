using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

using Il2CppInterop.Runtime.Attributes;

namespace MiSidePeaceMode
{
    // Cappie's dance battle (Scene 7, "spend time with Cappie"): the dance pad, the rhythm game on the TV with its songs,
    // her dance on the pad and the player's steps onto it. Her house there is our house turned around, so the whole
    // setup is borrowed as it is and set down in front of our TV the way it stood in front of hers; its calls into the
    // rest of the story (her animator, her look, the World's camera moves) are pointed at ours.
    // The story's order is kept: you step on the pad and pick a song, she dances it first, then it's your turn; she
    // says how you did, asks for another round (three songs at most). Her lines are the story's own, read from the
    // game's language files, so they come in every language the game has.
    public partial class PeaceDirector
    {
        GameObject danceTemplate;        // the borrowed setup, kept switched off; every game plays on a fresh copy
        GameObject danceNow;             // this game's copy (a pivot at our TV)
        Location7_GameDance dg;
        int danceStage;                  // 0 none, 1 picking a song, 2 her turn, 3 your turn, 4 she comments, 5 over
        GameObject[] danceWin, danceLose, danceDraw, danceNext;   // stand-ins the game switches on where the story had her lines
        Time_Events[] danceMitaDone;
        readonly List<(Transform t, Vector3 pos)> danceMoved = new List<(Transform, Vector3)>();
        Transform danceMitaPad, danceMitaSide, dancePadStand, dancePadCenter;
        UnityEvent danceMenuEvent;       // the story's "pick a song" (camera to the screen, the menu keys)
        int danceRound;
        bool danceIntroDone;
        Action danceArrive;              // where her walk (to the pad, beside it) ends
        float danceOnPadAt = -1f;        // when she took her stance (her song starts once she's in it)
        float danceWalkUntil;
        public static bool DevDanceAuto;
        public static float DevDanceHitAt = 0.87f;
        static readonly HashSet<IntPtr> dancePressed = new HashSet<IntPtr>();
        public static int DevDanceMore;   // dev: "one more" this many times, then enough  // dev: step on the pad / answer "another one?" by itself, hit the notes

        const string DanceFile = "LocationDialogue Location7";
        static string DL(int idx) => LineText(DanceFile, idx);

        bool DanceReady => danceTemplate != null && FindSofa() && Pn.Id == "cap";

        void KeepDanceGame(Scene s)
        {
            Location7_GameDance game = null;
            foreach (var r in s.GetRootGameObjects())
            {
                foreach (var g in r.GetComponentsInChildren<Location7_GameDance>(true)) { game = g; break; }
                if (game != null) break;
            }
            if (game == null) { Plugin.Log.LogWarning("[dance] no dance game in " + s.name); return; }
            var group = game.transform.parent;
            // her TV: the one closest to the game's screen (the story has another TV elsewhere)
            Transform tv = null; float best = float.MaxValue;
            foreach (var r in s.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                    if (t != null && t.name == "TV") { float d = (t.position - game.transform.position).sqrMagnitude; if (d < best) { best = d; tv = t; } }
            if (tv == null || !FindSofa()) { Plugin.Log.LogWarning($"[dance] her TV {tv != null}, ours {tvT != null}"); return; }

            // her animator, her look and the World's camera moves: ours; her quest (walks, doors, the achievement): gone
            var log = new StringBuilder();
            log.AppendLine($"dance group {group.name} under {(group.parent != null ? group.parent.name : "-")}, her TV {tv.position} {tv.eulerAngles.y:0}, ours {tvT.position} {tvT.eulerAngles.y:0}");
            int moved = SceneImport.Retarget(group.gameObject, s, MapDanceTarget, log);
            // (her clips' own events, numbered, go through her animator's list: what the dance ones did in the story)
            foreach (var r in s.GetRootGameObjects())
                foreach (var a in r.GetComponentsInChildren<Animator_FunctionsOverride>(true))
                {
                    if (a == null || a.name != "MitaPerson Know" || a.eventAnimation == null) continue;
                    for (int i = 0; i < a.eventAnimation.Count; i++) log.AppendLine($"   Cappie's animation event {i}: {SceneDumper.EventString(a.eventAnimation[i])}");
                    var ea = a.GetComponent<Events_Animator>();
                    if (ea != null && ea.events != null) for (int i = 0; i < ea.events.Length; i++) log.AppendLine($"   Cappie's Events_Animator {i}: {SceneDumper.EventString(ea.events[i]?.eventAnim)}");
                }

            // kept switched off, placed relative to her TV: a copy is set down on ours for each game
            group.gameObject.SetActive(false);
            danceTemplate = new GameObject("PeaceDanceTemplate");
            SceneManager.MoveGameObjectToScene(danceTemplate, tvT.gameObject.scene);
            danceTemplate.SetActive(false);
            danceTemplate.transform.SetPositionAndRotation(tv.position, tv.rotation);
            group.SetParent(danceTemplate.transform, true);
            group.gameObject.SetActive(true);
            Plugin.Log.LogInfo($"[dance] Cappie's dance battle borrowed ({moved} calls now ours)");
            if (DevRunner.Active) File.WriteAllText(Path.Combine(BepInEx.Paths.ConfigPath, "dance_events.txt"), log.ToString(), Encoding.UTF8);
        }

        UnityEngine.Object MapDanceTarget(UnityEngine.Object tgt)
        {
            var comp = tgt.TryCast<Component>();
            if (comp == null) return null;
            switch (comp.GetIl2CppType().Name)
            {
                case "Animator_FunctionsOverride": return mitaAnim;
                case "Character_Look": return mitaLook;
                // (her magnet onto the pad: we hold her there ourselves)
                case "MitaPerson": case "Achievement_function": case "Light_LightingColor": return null;
            }
            // the World's own components (the camera's moves to the screen and back)
            if (comp.gameObject.name == "World" && world != null) return world.gameObject.GetComponent(comp.GetIl2CppType());
            return null;
        }

        Transform DanceFind(string name) => danceNow != null ? Util.FindDeep(danceNow.transform, name) : null;

        // a fresh copy of the setup in front of our TV, its lines taken out (she says them through ours)
        bool BuildDance()
        {
            var pivot = new GameObject("PeaceDance");
            SceneManager.MoveGameObjectToScene(pivot, tvT.gameObject.scene);
            pivot.SetActive(false);
            pivot.transform.SetPositionAndRotation(danceTemplate.transform.position, danceTemplate.transform.rotation);
            var src = danceTemplate.transform.GetChild(0);
            var copy = UnityEngine.Object.Instantiate(src.gameObject, pivot.transform);
            copy.transform.localPosition = src.localPosition;
            copy.transform.localRotation = src.localRotation;
            pivot.transform.SetPositionAndRotation(tvT.position, tvT.rotation);
            danceNow = pivot;
            dg = copy.GetComponentInChildren<Location7_GameDance>(true);
            if (dg == null) { UnityEngine.Object.Destroy(pivot); danceNow = null; return false; }
            dg.animatorMita = mitaAnim;

            // the story's lines: "pick a song" is kept for its camera move and keys, the rest goes
            var menuLine = DanceFind("TextMita 5");
            danceMenuEvent = menuLine != null ? menuLine.GetComponent<Dialogue_3DText>()?.eventFinish : null;
            foreach (var d in copy.GetComponentsInChildren<Dialogue_3DText>(true))
                if (d != null && d.transform != menuLine) UnityEngine.Object.Destroy(d.gameObject);
            if (menuLine != null) menuLine.gameObject.SetActive(false);

            // where the game switches her lines on, stand-ins that tell us (win / lose / draw per round, "another one?")
            var holder = new GameObject("PeaceDanceCues");
            holder.transform.SetParent(pivot.transform, false);
            GameObject[] Cues(string n, int count)
            {
                var a = new GameObject[count];
                for (int i = 0; i < count; i++) { a[i] = new GameObject(n + " " + i); a[i].transform.SetParent(holder.transform, false); a[i].SetActive(false); }
                return a;
            }
            danceWin = Cues("win", 3); danceLose = Cues("lose", 3); danceDraw = Cues("draw", 3); danceNext = Cues("next", 3);
            dg.dialgouesWin = new Il2CppReferenceArray<GameObject>(danceWin);
            dg.dialgouesLose = new Il2CppReferenceArray<GameObject>(danceLose);
            dg.dialgouesDraw = new Il2CppReferenceArray<GameObject>(danceDraw);
            dg.dialgouesNext = new Il2CppReferenceArray<GameObject>(danceNext);
            // her song over: the story's "ReadyMita n" (a Time_Events) - ours, emptied, with our callback on it
            var teSrc = DanceFind("AnimationMita StartDance");
            danceMitaDone = new Time_Events[3];
            for (int i = 0; i < 3; i++)
            {
                int n = i;
                var go = teSrc != null ? UnityEngine.Object.Instantiate(teSrc.gameObject, holder.transform) : new GameObject();
                go.name = "mita done " + i;
                go.SetActive(false);
                var te = go.GetComponent<Time_Events>() ?? go.AddComponent<Time_Events>();
                te.eventStart = new UnityEvent();
                te.EventsOnTime = new Il2CppReferenceArray<TimePoint>(0);
                te.eventStart.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => OnMitaDanceDone(n))));
                danceMitaDone[i] = te;
            }
            dg.mitaFinish = new Il2CppReferenceArray<Time_Events>(danceMitaDone);
            // every song done (the third one): the story ends the game there
            dg.eventReadyAllMusic.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => After(0.1f, OnDanceAllDone))));
            // "this one!": the menu's pick key
            var pick = DanceFind("2D HintKey Change")?.GetComponent<Interface_KeyHint_Key>();
            pick?.eventKeyDown.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(OnDanceSongPicked)));
            if (dg.audioMusic != null && !gameMusic.Contains(dg.audioMusic)) gameMusic.Add(dg.audioMusic);

            danceMitaPad = DanceFind("PositionMita Dance");
            danceMitaSide = DanceFind("PositionMita SpeakRight");
            dancePadStand = DanceFind("AnimationPlayer StayDance");
            dancePadCenter = DanceFind("IKLegBody");   // (the pad's middle: where feet go)
            pivot.SetActive(true);
            if (DevRunner.Active && dg.music != null)
                for (int i = 0; i < dg.music.Length; i++) { var m = dg.music[i]; Plugin.Log.LogInfo("[dance] song " + i + ": " + m?.music?.name + " " + (m?.music != null ? m.music.length.ToString("0") : "-") + "s, notes " + m?.notes?.Length); }
            Plugin.Log.LogInfo($"[dance] set up: pad {dancePadStand?.position}, her spot {danceMitaPad?.position}, beside {danceMitaSide?.position}, menu {danceMenuEvent != null}, pick {pick != null}");
            return true;
        }

        // the coffee table sits where the pad goes: moved towards the sofa for the game, with what's on it
        static readonly string[] DanceTableThings = { "LivingTable", "GamepadBlue", "GamepadPink", "RemoteTV", "Books2", "Books3", "PenBucket" };

        void MoveTableAside(bool aside)
        {
            if (!aside)
            {
                foreach (var (t, pos) in danceMoved) if (t != null) t.position = pos;
                danceMoved.Clear();
                return;
            }
            var main = Util.FindPath("HouseGame Tamagotchi>House>Main");
            if (main == null || danceMoved.Count > 0) return;
            var shift = -ToTv * 0.75f;
            foreach (var n in DanceTableThings)
            {
                var t = main.transform.Find(n);
                if (t == null) continue;
                danceMoved.Add((t, t.position));
                t.position += shift;
            }
        }

        // ---- the game ----

        void StartDance()
        {
            if (!DanceReady) { Plugin.Log.LogWarning("[dance] not ready"); return; }
            Did("dance");
            KeepAmbient();
            EndRoutine();
            StopFollow(false);
            danceStage = 1;
            danceRound = 0;
            fadeTarget = 1f;
            afterFade = () =>
            {
                ClearLeftoverText();
                ShowRoom("Main");
                MoveTableAside(true);
                if (!BuildDance()) { Plugin.Log.LogWarning("[dance] no game"); EndDanceNow(); fadeTarget = 0f; return; }
                // her: beside the pad, facing you; you: onto the pad (the story's own step up)
                var toTv = ToTv;
                var side = danceMitaSide != null ? danceMitaSide.position : dancePadStand.position + Vector3.Cross(Vector3.up, toTv) * 0.9f;
                PoseMitaUpright(side, -toTv);
                ReleasePose(side, dancePadStand.position - toTv);
                MovePlayer(dancePadStand.position - toTv * 0.4f, tvT.position);
                try { DanceFind("AnimationPlayer StayDance")?.GetComponent<ObjectAnimationPlayer>()?.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[dance] step on: " + e.Message); }
                fadeTarget = 0f;
                var intro = new List<Line>();
                if (!danceIntroDone) intro.Add(new Line(Who.Mita, DL(459), "smile"));
                intro.Add(new Line(Who.Mita, DL(460), "smile"));
                intro.Add(new Line(Who.Mita, DL(461)));
                intro.Add(new Line(Who.Mita, DL(462), "smile"));
                danceIntroDone = true;
                After(1.4f, () => Play(intro.FindAll(l => l.text != null), OpenDanceMenu));
            };
        }

        // the story's song menu: the camera to the screen, left / right / pick keys
        void OpenDanceMenu()
        {
            if (danceStage != 1) return;
            try { danceMenuEvent?.Invoke(); } catch (Exception e) { Plugin.Log.LogWarning("[dance] menu: " + e.Message); }
            try { mitaLook?.LookOnObject(tvT); } catch { }
            Plugin.Log.LogInfo("[dance] pick a song");
        }

        void OnDanceSongPicked()
        {
            if (danceStage != 1) return;
            danceStage = 2;
            Plugin.Log.LogInfo($"[dance] song picked: {dg.musicIndexPlay}");
            var lines = new List<Line> { new Line(Who.Player, DL(464)), new Line(Who.Mita, DL(466), "smile"), new Line(Who.Mita, DL(467)), new Line(Who.Mita, DL(468), "smile") };
            After(0.8f, () => Play(lines.FindAll(l => l.text != null), MitaToPad));
        }

        // she steps onto the pad; the story's own timing takes it from there (her stance, the song, her jumps)
        void MitaToPad()
        {
            if (danceStage != 2 && danceStage != 4) return;
            danceStage = 2;
            DanceWalk(danceMitaPad.position - ToTv * 0.35f, () =>
            {
                if (danceNow == null) return;
                walkId++;
                // (the story's stance clip steps her forward onto the pad by root motion, which a held pose doesn't
                // take: she's set on its middle instead)
                var onPad = dancePadCenter != null ? new Vector3(dancePadCenter.position.x, danceMitaPad.position.y, dancePadCenter.position.z) : danceMitaPad.position;
                PoseMitaUpright(onPad, ToTv);
                try { mitaLook?.LimitAngleLookOff(true); } catch { }
                SetIdle(Clip("Mita Dance Idle"));
                danceOnPadAt = Time.time;
                // the dance clips carry her hips off her feet's spot: once she stands in her stance, her middle goes
                // over the pad's middle
                After(2.6f, () =>
                {
                    if (danceStage != 2 || !posed || poseHips == null) return;
                    var d = onPad - poseHips.position; d.y = 0;
                    if (d.magnitude > 0.8f) return;
                    posePos += d;
                    Plugin.Log.LogInfo($"[dance] her hips were {d.magnitude:0.00} m off the pad's middle: moved");
                });
                try { danceMitaPad.GetComponent<MitaAIMovePoint>()?.eventFinish?.Invoke(); } catch (Exception e) { Plugin.Log.LogWarning("[dance] on the pad: " + e.Message); }
                Plugin.Log.LogInfo("[dance] she's on the pad");
            });
        }

        void OnMitaDanceDone(int i)
        {
            if (danceNow == null || danceStage != 2) return;
            Plugin.Log.LogInfo($"[dance] her song {i} done: her {dg.scoreMita}");
            danceStage = 3;
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            var off = Clip("Mita UnStand Dance");
            if (off != null) try { mitaAnim.AnimationClipSimpleNext(off); } catch { }
            int[] idx = i == 0 ? new[] { 471, 472, 473, 474 } : i == 1 ? new[] { 477, 478, 479 } : new[] { 482, 483, 484 };
            var lines = new List<Line>();
            foreach (var n in idx) { var t = DL(n); if (t != null) lines.Add(new Line(Who.Mita, t, lines.Count == 0 ? "smile" : null)); }
            After(1.9f, () =>
            {
                // off the pad (the game's to know: the next song is yours), to her place beside it
                try { dg.MitaStayDance(false); } catch (Exception e) { Plugin.Log.LogWarning("[dance] off the pad: " + e.Message); }
                if (posed) ReleasePose(danceMitaPad.position, dancePadStand.position);
                try { mitaLook?.LimitAngleLookOff(false); } catch { }
                DanceWalk(danceMitaSide.position, () => { walkId++; HoldAddon(false); try { mitaLook?.LookOnPlayer(); } catch { } });
                Play(lines, () =>
                {
                    // your turn: the pad lights up to be stepped on
                    var start = DanceFind("Interactive DanceStart");
                    if (start != null) { start.gameObject.SetActive(true); try { start.GetComponent<ObjectInteractive>()?.Activation(true); } catch { } }
                    Plugin.Log.LogInfo("[dance] your turn: step on the pad");
                    if (DevDanceAuto) After(2f, () => DevDance("pad"));
                });
            });
        }

        // her walks here end by distance (the routine's walks are watched by TickRoutine); the pad sits where the
        // coffee table stood, a hole in the floor she can walk on: close is close enough, and a walk gives up after a while
        [HideFromIl2Cpp] void DanceWalk(Vector3 to, Action arrive)
        {
            danceArrive = arrive;
            danceWalkUntil = Time.time + 8f;
            WalkMita(to, () => { var a = danceArrive; danceArrive = null; a?.Invoke(); });
        }

        void TickDance()
        {
            if (danceStage == 0 || danceNow == null) return;
            if (danceArrive != null && mitaBody != null)
            {
                var d = mitaBody.position - walkTo; d.y = 0;
                if (d.magnitude < 0.45f || Time.time > danceWalkUntil) { walking = false; var a = danceArrive; danceArrive = null; a(); }
            }
            // her song: in the story her stance clip's own event starts it; here, once she stands in it
            if (danceStage == 2 && danceOnPadAt > 0f && dg.mitaStayOnDance && !dg.playMusic && Time.time > danceOnPadAt + 3.4f)
            {
                danceOnPadAt = -1f;
                try { dg.PlayGame(); Plugin.Log.LogInfo($"[dance] her song starts: playing {dg.playMusic}, screen {dg.indexScreenGame}"); }
                catch (Exception e) { Plugin.Log.LogWarning("[dance] her song: " + e.Message); }
            }
            // (one press per note, as a player would: at a high frame rate a note stays in the window for several frames, and
            // pressing again on the next ones spoils the hit)
            if (DevDanceAuto && danceStage == 3 && dg.spheres != null)
            {
                // (only notes still on screen: a gone note's pointer can come back as a new one)
                var live = new HashSet<IntPtr>();
                foreach (var q in dg.spheres) if (q != null) live.Add(q.Pointer);
                dancePressed.IntersectWith(live);
                foreach (var sp in dg.spheres)
                    if (sp != null && !sp.mitaJump && sp.timeMove >= DevDanceHitAt && dancePressed.Add(sp.Pointer))
                    {
                        if (Random.value < 0.9f) try { dg.ButtonSide(sp.side); } catch { }
                        break;
                    }
            }
            for (int i = 0; i < 3; i++)
            {
                if (danceWin[i].activeSelf) { danceWin[i].SetActive(false); DanceResult(0, i); }
                if (danceLose[i].activeSelf) { danceLose[i].SetActive(false); DanceResult(1, i); }
                if (danceDraw[i].activeSelf) { danceDraw[i].SetActive(false); DanceResult(2, i); }
                if (danceNext[i].activeSelf) { danceNext[i].SetActive(false); DanceAgain(i); }
                if (danceMitaDone[i].gameObject.activeSelf) { danceMitaDone[i].gameObject.SetActive(false); OnMitaDanceDone(i); }
            }
        }

        // how you did against her: you won (0), she won (1), a draw (2) - the story's lines for this round
        void DanceResult(int how, int i)
        {
            danceStage = 4;
            danceRound = i + 1;
            Plugin.Log.LogInfo($"[dance] round {i}: you {dg.scorePlayer}, her {dg.scoreMita} -> {(how == 0 ? "you won" : how == 1 ? "she won" : "draw")}");
            int[][] set = how == 0 ? new[] { new[] { 501, 502, 503 }, new[] { 506, 507, 508 }, new[] { 511, 512, 513 } }
                        : how == 1 ? new[] { new[] { 487, 488 }, new[] { 491, 492, 493 }, new[] { 496, 497, 498 } }
                        : new[] { new[] { 516, 517, 518 }, new[] { 521, 522 }, new[] { 525, 526, 527 } };
            string emo = how == 0 ? "surprise" : how == 1 ? "smile" : "smile";
            var lines = new List<Line>();
            foreach (var n in set[Mathf.Clamp(i, 0, 2)]) { var t = DL(n); if (t != null) lines.Add(new Line(Who.Mita, t, lines.Count == 0 ? emo : null)); }
            AddAff(how == 0 ? 2 : 1);
            After(1.2f, () => Play(lines, () => { try { dg.MitaSpeakResult(); } catch (Exception e) { Plugin.Log.LogWarning("[dance] result: " + e.Message); } }));
        }

        // "another one?"
        void DanceAgain(int i)
        {
            var lines = new List<Line>();
            foreach (var n in i == 0 ? new[] { 530, 531 } : new[] { 534, 535 }) { var t = DL(n); if (t != null) lines.Add(new Line(Who.Mita, t, lines.Count == 0 ? "smile" : null)); }
            Play(lines, () =>
            {
                if (DevDanceAuto) After(2f, () => { if (DevDanceMore > 0) { DevDanceMore--; DevPick("한 판 더!"); } else DevPick("이제 그만할래"); });
                ShowMenu(Pn.Short, new List<string> { "한 판 더!", "이제 그만할래" }, new List<Action>
                {
                    () =>
                    {
                        try { dg.MitaGoNext(); } catch (Exception e) { Plugin.Log.LogWarning("[dance] next: " + e.Message); }
                        try { mitaLook?.LookOnObject(tvT); } catch { }
                        MitaToPad();
                    },
                    () => EndDance(false),
                });
            });
        }

        void OnDanceAllDone()
        {
            if (danceStage == 0 || danceStage == 5) return;
            EndDance(true);
        }

        void EndDance(bool tired)
        {
            danceStage = 5;
            var lines = new List<Line>();
            if (tired) { foreach (var n in new[] { 538, 539 }) { var t = DL(n); if (t != null) lines.Add(new Line(Who.Mita, t, lines.Count == 0 ? "surprise" : "smile")); } }
            else lines.Add(new Line(Who.Mita, "재밌었어요! 다음에 또 춰요!", "smile"));
            AddAff(2);
            Play(lines, () =>
            {
                fadeTarget = 1f;
                afterFade = () =>
                {
                    ClearLeftoverText();
                    EndDanceNow();
                    fadeTarget = 0f;
                };
            });
        }

        // (screen black) everything back the way it was
        void EndDanceNow()
        {
            try { var wp = world != null ? world.GetComponent<WorldPlayer>() : null; wp?.CameraLerpOtherTargetStop(); wp?.CameraFOVLerpReset(); } catch { }
            if (dg != null && dg.audioMusic != null) { try { dg.audioMusic.Stop(); } catch { } gameMusic.Remove(dg.audioMusic); }
            if (danceNow != null) UnityEngine.Object.Destroy(danceNow);
            danceNow = null; dg = null;
            MoveTableAside(false);
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            try { mitaLook?.LimitAngleLookOff(false); mitaLook?.Activation(true); } catch { }
            if (pm != null && mitaBody != null)
            {
                var you = pm.transform.position;
                if (posed) ReleasePose(mitaBody.position, you);
                walking = false; walkId++;
                HoldAddon(false);
            }
            danceStage = 0;
            RestoreAmbient();
            Plugin.Log.LogInfo("[dance] over");
        }

        public void DevDance(string what)
        {
            var sp = what.Split(' ');
            switch (sp[0])
            {
                case "start": StartDance(); break;
                case "menu": if (dg != null) try { dg.ButtonMoveMenu(int.Parse(sp[1])); } catch (Exception e) { Plugin.Log.LogWarning("[dance] " + e.Message); } break;   // dance menu 0|1
                case "pick": try { DanceFind("2D HintKey Change")?.GetComponent<Interface_KeyHint_Key>()?.eventKeyDown?.Invoke(); } catch (Exception e) { Plugin.Log.LogWarning("[dance] " + e.Message); } break;
                case "pad": try { DanceFind("Interactive DanceStart")?.GetComponent<ObjectInteractive>()?.eventClick?.Invoke(); } catch (Exception e) { Plugin.Log.LogWarning("[dance] " + e.Message); } break;
                case "key": if (dg != null) try { dg.ButtonSide(int.Parse(sp[1])); } catch (Exception e) { Plugin.Log.LogWarning("[dance] " + e.Message); } break;   // dance key 0|1|2
                case "auto": DevDanceAuto = sp.Length < 2 || sp[1] != "off"; if (sp.Length > 2) DevDanceHitAt = float.Parse(sp[2], System.Globalization.CultureInfo.InvariantCulture); if (sp.Length > 3) DevDanceMore = int.Parse(sp[3]); break;   // dance auto [off] [hitAt] [one-more count]
                case "state":
                    if (dg == null) { Plugin.Log.LogInfo($"[dance] no game (template {danceTemplate != null})"); break; }
                    if (dg.spheres != null) foreach (var s in dg.spheres) if (s != null) Plugin.Log.LogInfo($"[dance]   sphere side {s.side} t {s.timeMove:0.00} mita {s.mitaJump} gone {s.destroyAlpha}");
                    Plugin.Log.LogInfo($"[dance] stage {danceStage} screen {dg.indexScreenGame} music {dg.musicIndexPlay} playing {dg.playMusic} t {dg.timeMusicPlay:0.0} you {dg.scorePlayer} her {dg.scoreMita} " +
                                       $"herOnPad {dg.mitaStayOnDance} goNext {dg.goNextMusicMita} finishMita {dg.indexFinishMita} dlg {dg.indexDialogue}/{dg.indexDialogueNext} spheres {dg.spheres?.Count}");
                    break;
            }
        }
    }
}
