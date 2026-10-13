using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace MiSidePeaceMode
{
    // The living room sofa, together: TV, a book she reads to you, or just a rest. You sit down with Scene 3's chair
    // animation (the meal's, re-aimed at the sofa), she sits beside you on one of the story's sitting clips - her hips
    // held on the cushion, since each clip seats them somewhere else - and talks in her own way about the show, the
    // book, or nothing much, until you both get up. The TV lights the room with its changing picture and plays the
    // game console's menu music; the book is a small one in her hands.
    public partial class PeaceDirector
    {
        enum LoungeKind { Tv, Read, Rest }

        // (dev: loungecal <mitaSide> <hipsUp> <hipsBack> <playerSide> <playerBack> <playerYaw>)
        public static float LoungeMitaSide = 0.43f, LoungeHipsUp = 0.55f, LoungeHipsBack = -0.24f;
        public static float LoungePlayerSide = 0.43f, LoungePlayerBack = 0f, LoungePlayerYaw = -90f;   // (the chair animation sits you facing 90° to its right)
        // where the chair animation puts your eyes, in its own frame (measured: the seated eyes and the animation's transform)
        static readonly Vector3 SitEyes = new Vector3(0.20f, 0f, 0.47f);

        Transform sofaT, tvT;
        bool lounging;
        LoungeKind loungeKind;
        GameObject loungeRoot, loungeScreen, loungeBook;
        Light loungeTvLight;
        AudioSource loungeTvSound;
        ObjectAnimationPlayer loungeSit, loungeStand;
        Vector3 seatHips;                  // her hips, held here while she sits (KeepPose)
        float loungeFlickerAt;

        bool LoungeReady => mealTemplate != null && FindSofa() && !lounging;

        bool FindSofa()
        {
            if (sofaT != null && tvT != null) return true;
            var main = Util.FindPath("HouseGame Tamagotchi>House>Main");
            if (main == null) return false;
            sofaT = main.transform.Find("Sofa");
            tvT = main.transform.Find("TV");
            return sofaT != null && tvT != null;
        }

        Vector3 ToTv { get { var d = tvT.position - sofaT.position; d.y = 0; return d.sqrMagnitude > 0.001f ? d.normalized : Vector3.back; } }

        void StartLounge(LoungeKind kind)
        {
            if (!LoungeReady) { Plugin.Log.LogWarning("[lounge] not ready"); return; }
            if (activity == MitaActivity.OnSofa) { activity = MitaActivity.Free; ShowQuestIcon(false); }
            lounging = true;
            loungeKind = kind;
            Did(kind == LoungeKind.Tv ? "tv" : kind == LoungeKind.Read ? "read" : "rest");
            EndRoutine();
            var lines = Personas.Lounge(Pn.Id);
            var talk = kind == LoungeKind.Tv ? Pick(lines.Tv) : kind == LoungeKind.Read ? Pick(lines.Read) : Pick(lines.Rest);
            fadeTarget = 1f;
            afterFade = () =>
            {
                ClearLeftoverText();
                SitOnSofa(kind);
                fadeTarget = 0f;
                After(1.2f, () => Play(talk, () => { AddAff(2); EndLounge(); }));
            };
        }

        // (screen black) you and her on the sofa, the TV / book / nothing
        void SitOnSofa(LoungeKind kind)
        {
            ShowRoom("Main");
            var toTv = ToTv;
            var right = Vector3.Cross(toTv, Vector3.up);
            var center = new Vector3(sofaT.position.x, 0f, sofaT.position.z);

            // you: the meal's sit-down, re-aimed
            loungeRoot = new GameObject("PeaceLounge");
            var sitSrc = Util.FindDeep(mealTemplate.transform, "Interactive SitOnChair");
            var standSrc = Util.FindDeep(mealTemplate.transform, "AnimaitonPlayer Stand");
            if (sitSrc != null)
            {
                // your eyes go over your half of the sofa: the animation's transform is placed from there
                var rot = Quaternion.LookRotation(toTv) * Quaternion.Euler(0f, LoungePlayerYaw, 0f);
                var eyes = center - right * LoungePlayerSide - toTv * LoungePlayerBack + toTv * 0.03f;
                var at = eyes - rot * SitEyes;
                loungeSit = CopyPlayerAnimation(sitSrc, at, rot);
                if (standSrc != null)
                {
                    // the stand-up sits where it did relative to the chair
                    var relPos = Quaternion.Inverse(sitSrc.rotation) * (standSrc.position - sitSrc.position);
                    var relRot = Quaternion.Inverse(sitSrc.rotation) * standSrc.rotation;
                    loungeStand = CopyPlayerAnimation(standSrc, at + rot * relPos, rot * relRot);
                }
                try { loungeSit?.AnimationPlay(); } catch (Exception e) { Plugin.Log.LogWarning("[lounge] sit: " + e.Message); }
                Plugin.Log.LogInfo($"[lounge] you sit at {at} (clips {loungeSit?.animationStart?.name}/{loungeSit?.animationLoop?.name})");
            }

            // her: beside you, hips on the cushion
            SeatHer(kind);
            if (kind == LoungeKind.Tv && loungeScreen == null) TvOn(true);
            // your eyes: the TV, turning most of the way to her while she talks; with a book or resting, her
            StageView(() =>
            {
                bool herTurn = busy && currentIsMita;
                var face = HeadPoint + Vector3.down * 0.05f;
                if (kind == LoungeKind.Tv && loungeScreen != null) return herTurn ? Vector3.Lerp(loungeScreen.transform.position, face, 0.75f) : loungeScreen.transform.position;
                return kind == LoungeKind.Read && !herTurn ? HeadPoint + Vector3.down * 0.35f : face;
            });
        }

        ObjectAnimationPlayer CopyPlayerAnimation(Transform src, Vector3 pos, Quaternion rot)
        {
            var t = UnityEngine.Object.Instantiate(src.gameObject, loungeRoot.transform).transform;
            t.SetPositionAndRotation(pos, rot);
            foreach (var oi in t.GetComponentsInChildren<ObjectInteractive>(true)) oi.enabled = false;
            foreach (var c in t.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            var oap = t.GetComponent<ObjectAnimationPlayer>();
            if (oap != null)
            {
                // (its events pointed at the meal: the eat key, the plates)
                oap.eventStartAnimaiton = new UnityEvent();
                oap.eventStartLoop = new UnityEvent();
                oap.eventFinish = new UnityEvent();
            }
            t.gameObject.SetActive(true);
            return oap;
        }

        void EndLounge()
        {
            fadeTarget = 1f;
            afterFade = () =>
            {
                ClearLeftoverText();
                StageView(null);
                if (loungeKind == LoungeKind.Rest) SleepFace(false);
                TvOn(false);
                if (loungeBook != null) UnityEngine.Object.Destroy(loungeBook);
                loungeBook = null;
                seatHips = Vector3.zero;
                ReleaseClip();
                var toTv = ToTv;
                var right = Vector3.Cross(toTv, Vector3.up);
                var center = new Vector3(sofaT.position.x, 0f, sofaT.position.z);
                // up: you with the story's stand-up, her beside the sofa
                bool standing = false;
                try { if (loungeStand != null) { loungeStand.AnimationPlay(); standing = true; } else pm?.AnimationStopFull(); } catch { try { pm?.AnimationStopFull(); } catch { } }
                ReleasePose(center + right * (LoungeMitaSide + 0.2f) + toTv * 0.75f, pm.transform.position);
                SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
                try { mitaLook?.Activation(true); mitaLook?.LookOnPlayer(); } catch { }
                fadeTarget = 0f;
                After(standing ? 3.5f : 0.2f, () =>
                {
                    if (loungeRoot != null) UnityEngine.Object.Destroy(loungeRoot);
                    loungeRoot = null; loungeSit = loungeStand = null;
                    lounging = false;
                    FaceHer();
                    afterFade = CheckMilestones;
                });
            };
        }

        // ---- the TV: a light that changes like a picture, the console's menu tune ----

        void TvOn(bool on)
        {
            if (!on)
            {
                if (loungeScreen != null) UnityEngine.Object.Destroy(loungeScreen);
                loungeScreen = null; loungeTvLight = null;
                if (loungeTvSound != null) loungeTvSound.Stop();
                loungeTvSound = null;
                return;
            }
            var mf = tvT.GetComponent<MeshFilter>();
            var b = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, new Vector3(1.2f, 0.7f, 0.1f));
            var center = tvT.TransformPoint(b.center);
            var size = tvT.TransformVector(b.size);
            var toSofa = -ToTv;
            float width = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.z));
            loungeScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            loungeScreen.name = "PeaceTvScreen";
            UnityEngine.Object.Destroy(loungeScreen.GetComponent<Collider>());
            loungeScreen.transform.position = center + toSofa * (Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.z)) * 0.5f + 0.012f);
            loungeScreen.transform.rotation = Quaternion.LookRotation(-toSofa, Vector3.up);
            loungeScreen.transform.localScale = new Vector3(width * 0.86f, Mathf.Abs(size.y) * 0.8f, 1f);
            var sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Unlit/Color");
            var r = loungeScreen.GetComponent<MeshRenderer>();
            if (sh != null) r.material = new Material(sh);
            var lgo = new GameObject("PeaceTvLight");
            lgo.transform.SetParent(loungeScreen.transform, false);
            lgo.transform.localPosition = new Vector3(0, 0, -0.6f);
            loungeTvLight = lgo.AddComponent<Light>();
            loungeTvLight.type = LightType.Point;
            loungeTvLight.range = 5f;
            loungeTvLight.intensity = 0.8f;
            loungeTvLight.shadows = LightShadows.None;
            // (the console's menu tune, from the TV's own audio)
            var menuTune = Util.FindDeep(tvT, "Audio Menu");
            var src = menuTune != null ? menuTune.GetComponent<AudioSource>() : null;
            if (src != null && src.clip != null)
            {
                loungeTvSound = loungeScreen.AddComponent<AudioSource>();
                loungeTvSound.clip = src.clip;
                loungeTvSound.loop = true;
                loungeTvSound.spatialBlend = 0.7f;
                loungeTvSound.volume = 0.35f;
                loungeTvSound.Play();
            }
            loungeFlickerAt = 0f;
            Plugin.Log.LogInfo($"[lounge] TV on ({(sh != null ? sh.name : "no shader")}, tune {(loungeTvSound != null)})");
        }

        // LateUpdate: the picture changes now and then
        void TickLounge()
        {
            if (loungeScreen == null || Time.time < loungeFlickerAt) return;
            loungeFlickerAt = Time.time + Random.Range(0.25f, 1.6f);
            var c = Color.HSVToRGB(Random.value, Random.Range(0.25f, 0.7f), Random.Range(0.65f, 1f));
            var r = loungeScreen.GetComponent<MeshRenderer>();
            if (r != null && r.material != null) r.material.color = c;
            if (loungeTvLight != null) loungeTvLight.color = Color.Lerp(c, Color.white, 0.4f);
        }

        // ---- a small book in her hands ----

        GameObject MakeBook()
        {
            var hand = mitaBody != null ? Util.FindDeep(mitaBody, "RightItem") : null;
            if (hand == null) return null;
            var book = GameObject.CreatePrimitive(PrimitiveType.Cube);
            book.name = "PeaceBook";
            UnityEngine.Object.Destroy(book.GetComponent<Collider>());
            // (a cover from the house's own books)
            var shelf = Util.FindPath("HouseGame Tamagotchi>House>Main>Books1");
            var sr = shelf != null ? shelf.GetComponent<MeshRenderer>() : null;
            if (sr != null && sr.sharedMaterial != null) book.GetComponent<MeshRenderer>().material = sr.sharedMaterial;
            book.transform.SetParent(hand, false);
            book.transform.localPosition = new Vector3(0.02f, 0.06f, 0.05f);
            book.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            book.transform.localScale = new Vector3(0.15f, 0.025f, 0.21f);
            return book;
        }

        // ---- her own time on the sofa: now and then, when she's free in the daytime, she reads, watches TV or dozes
        // there by herself for a few minutes; you can sit down next to her from her menu ----
        float nextSofa = -1f;
        LoungeKind sofaKind;

        void TryGoToSofa(bool now = false)
        {
            if (!FindSofa() || lounging || following || request != null || IsNight() || mealTemplate == null)
            {
                if (now) Plugin.Log.LogInfo($"[lounge] not now: sofa {FindSofa()} lounging {lounging} following {following} request {request != null} night {IsNight()} meal set {mealTemplate != null}");
                return;
            }
            if (DevCalm && !now) return;
            if (nextSofa < 0f) nextSofa = Time.time + Random.Range(240f, 420f);
            if (!now && Time.time < nextSofa) return;
            nextSofa = Time.time + Random.Range(480f, 900f);
            // (not right in front of you: she goes off to do it while you're doing something else)
            if (!now && PlayerNearMita(2.5f, 360f)) return;
            float h = S.Minute / 60f;
            sofaKind = Pn.Id == "sleepy" && Random.value < 0.5f ? LoungeKind.Rest
                     : h >= 13f && h < 16f && Random.value < 0.3f ? LoungeKind.Rest
                     : h >= 17f || Random.value < 0.4f ? LoungeKind.Tv : LoungeKind.Read;
            var toTv = ToTv;
            var right = Vector3.Cross(toTv, Vector3.up);
            var front = new Vector3(sofaT.position.x, 0f, sofaT.position.z) + right * LoungeMitaSide + toTv * 0.7f;
            activity = MitaActivity.ToSofa;
            StopFollow(false);
            Plugin.Log.LogInfo($"[lounge] she goes to the sofa: {sofaKind}");
            WalkMita(front, OnReachedSofa);
        }

        void OnReachedSofa()
        {
            if (activity != MitaActivity.ToSofa) return;
            activity = MitaActivity.OnSofa;
            activityUntil = Time.time + Random.Range(150f, 300f);
            ShowRoom("Main");
            SeatHer(sofaKind);
            if (sofaKind == LoungeKind.Tv) TvOn(true);
        }

        // her, on her half of the sofa (with you or alone)
        void SeatHer(LoungeKind kind)
        {
            var toTv = ToTv;
            var right = Vector3.Cross(toTv, Vector3.up);
            var center = new Vector3(sofaT.position.x, 0f, sofaT.position.z);
            var mitaFloor = center + right * LoungeMitaSide;
            PoseMitaUpright(mitaFloor, toTv);
            uprightFront = Vector3.zero;   // (a sitting pelvis doesn't point forward: that correction turned her sideways)
            seatHips = mitaFloor + Vector3.up * LoungeHipsUp - toTv * LoungeHipsBack;
            var clip = Clip(kind == LoungeKind.Read ? "Mita Eating" : kind == LoungeKind.Rest ? "Mita SitIdle HalfSleep" : "Mita Sit Idle")
                       ?? Clip("Mita SitIdle") ?? Clip("Mita Sit Idle");
            if (clip != null) { HoldClip(clip); try { mitaAnim.ResetOrder(); mitaAnim.AnimationClipSimpleNext(clip); } catch { } }
            SettleCloth();
            if (kind == LoungeKind.Rest) SleepFace(true);
            // the story's sitting clips face other ways than her standing idle: once she's in the pose, turn her by
            // how far her face is off the TV (her eyes decide: her look is off meanwhile, so her head follows her body)
            try { mitaLook?.Activation(false); } catch { }
            After(0.8f, () => AlignSeated(toTv));
            After(2f, () => { AlignSeated(toTv); try { if (seatHips != Vector3.zero) mitaLook?.Activation(kind != LoungeKind.Rest); } catch { } });
            if (kind == LoungeKind.Read && loungeBook == null) loungeBook = MakeBook();
            Plugin.Log.LogInfo($"[lounge] {kind}: she sits at {mitaFloor} ({clip?.name})");
        }

        void AlignSeated(Vector3 want)
        {
            if (!posed || seatHips == Vector3.zero || !FaceFrame(out _, out var fwd, out _)) { Plugin.Log.LogInfo($"[lounge] align: posed {posed} seat {seatHips != Vector3.zero} (no face frame)"); return; }
            var hipsFwd = poseHips != null ? poseHips.forward : Vector3.zero;
            fwd.y = 0;
            if (fwd.sqrMagnitude < 0.01f) { Plugin.Log.LogInfo("[lounge] align: her face points straight down"); return; }
            float a = Vector3.SignedAngle(fwd.normalized, want, Vector3.up);
            Plugin.Log.LogInfo($"[lounge] align: face {fwd.normalized} want {want} -> {a:0}° (hips fwd {hipsFwd}, body fwd {mitaBody.forward})");
            if (Mathf.Abs(a) < 6f) return;
            poseRot = Quaternion.Euler(0f, a, 0f) * poseRot;
            Plugin.Log.LogInfo($"[lounge] turned her {a:0}° to face the TV");
        }

        // off the sofa (her time alone ended, or you took her somewhere)
        void UpFromSofa()
        {
            SleepFace(false);
            if (!lounging) TvOn(false);
            if (loungeBook != null) UnityEngine.Object.Destroy(loungeBook);
            loungeBook = null;
            seatHips = Vector3.zero;
            ReleaseClip();
            var toTv = ToTv;
            var right = Vector3.Cross(toTv, Vector3.up);
            var center = new Vector3(sofaT.position.x, 0f, sofaT.position.z);
            ReleasePose(center + right * (LoungeMitaSide + 0.2f) + toTv * 0.75f, pm.transform.position);
            SetIdle(skin != null && !skin.OwnAnimator ? skin.IdleClip : null);
            try { mitaLook?.Activation(true); } catch { }
            HoldAddon(false);
        }

        // her menu while she's on the sofa by herself: sit down next to her
        bool OnSofaAlone => activity == MitaActivity.OnSofa && !lounging;
        void JoinHerOnSofa() => StartLounge(sofaKind);

        public void DevLounge(string what)
        {
            if (what == "alone")
            {
                // (whatever she's doing - a meal waiting, cooking - she drops it for the test)
                if (activity != MitaActivity.Free) { Plugin.Log.LogInfo("[lounge] (dev) she stops " + activity); EndRoutine(); }
                TryGoToSofa(true);
                return;
            }
            if (what.StartsWith("cal "))
            {
                var p = what.Substring(4).Split(' ');
                float F(int i, float d) => p.Length > i ? float.Parse(p[i], System.Globalization.CultureInfo.InvariantCulture) : d;
                LoungeMitaSide = F(0, LoungeMitaSide); LoungeHipsUp = F(1, LoungeHipsUp); LoungeHipsBack = F(2, LoungeHipsBack);
                LoungePlayerSide = F(3, LoungePlayerSide); LoungePlayerBack = F(4, LoungePlayerBack); LoungePlayerYaw = F(5, LoungePlayerYaw);
                return;
            }
            var k = what == "read" ? LoungeKind.Read : what == "rest" ? LoungeKind.Rest : LoungeKind.Tv;
            StartLounge(k);
        }
    }

    public class LoungeLines
    {
        public Line[][] Tv, Read, Rest;
    }
}
