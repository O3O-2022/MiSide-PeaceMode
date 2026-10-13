using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiSidePeaceMode
{
    // Her glasses from Scene 3 (Act General>Mita TakeGlasses>...>Mita'sGlasses: she puts them on in the story):
    // "안경 써 줘" puts a copy on her head, "안경 벗기" takes it off.
    public partial class PeaceDirector
    {
        GameObject glassesTemplate, glassesOn;

        void BorrowGlasses(Scene s, Scene here)
        {
            var g = FindInScene(s, "Mita'sGlasses");
            if (g == null) { Plugin.Log.LogWarning("[glasses] none in Scene 3"); return; }
            var m = g.GetComponent<Transform_Magnet>();
            if (m != null)
            {
                Plugin.Log.LogInfo($"[glasses] magnet local={m.local} pos={m.position} rot={m.rotation} target={Util.PathOf(m.transformTarget)} magnet={Util.PathOf(m.transformMagnet)} re={Util.PathOf(m.reMagnetAndParent)}");
            }
            // the story puts them on "Head", a helper that follows her head bone (Transform_SimpleCopyPosition)
            foreach (var r in s.GetRootGameObjects())
                foreach (var h in r.GetComponentsInChildren<Transform_SimpleCopyPosition>(true))
                    if (h != null && h.name == "Head")
                        Plugin.Log.LogInfo($"[glasses] head helper copies {Util.PathOf(h.copyPosition)} add pos {h.positionAdd} rot {h.rotationAdd} lerp {h.lerpSpeed}; helper at {h.transform.position} rot {h.transform.eulerAngles}, bone at {(h.copyPosition != null ? h.copyPosition.position + " rot " + h.copyPosition.eulerAngles : "-")}");
            var mf = g.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Plugin.Log.LogInfo($"[glasses] mesh bounds {mf.sharedMesh.bounds.center} size {mf.sharedMesh.bounds.size}, scale {g.lossyScale}");
            glassesTemplate = Instantiate(g.gameObject, templates.transform);
            glassesTemplate.name = "PeaceGlasses Template";
            foreach (var c in glassesTemplate.GetComponents<Transform_Magnet>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var a in glassesTemplate.GetComponents<Animator>()) a.enabled = false;
            glassesTemplate.SetActive(false);
        }

        bool GlassesWorn => glassesOn != null;

        // on her eyes: placed from her eye bones (her face's own frame, whatever the clip does with her body),
        // then they follow the head bone
        float glassesFwd = 0.03f, glassesUp = -1f;   // (-1: her own; the eye bones sit below the drawn eyes, by model)
        float GlassesUp => glassesUp >= 0f ? glassesUp : Pn.Id == "sleepy" ? 0.075f : 0.05f;
        Vector3 glassesTurn = new Vector3(-90f, 180f, 0f);   // (the frame is modelled lying flat)

        bool FaceFrame(out Vector3 eyes, out Vector3 fwd, out Vector3 up)
        {
            eyes = fwd = up = Vector3.zero;
            var head = mitaBody != null ? Util.FindDeep(mitaBody, "Head") : null;
            var l = head != null ? Util.FindDeep(head, "Left Eye") : null;
            var r = head != null ? Util.FindDeep(head, "Right Eye") : null;
            var neck = head != null ? head.parent : null;
            if (l == null || r == null || neck == null) return false;
            eyes = (l.position + r.position) * 0.5f;
            var right = (r.position - l.position).normalized;
            up = Vector3.ProjectOnPlane(head.position - neck.position, right).normalized;
            fwd = Vector3.Cross(right, up).normalized;
            return true;
        }

        void PutGlassesOn()
        {
            if (glassesTemplate == null || mitaBody == null || glassesOn != null) return;
            var head = Util.FindDeep(mitaBody, "Head");
            if (head == null || !FaceFrame(out var eyes, out var fwd, out var up)) return;
            glassesOn = Instantiate(glassesTemplate);
            glassesOn.name = "PeaceGlasses";
            glassesOn.transform.SetPositionAndRotation(eyes + fwd * glassesFwd + up * GlassesUp, Quaternion.LookRotation(fwd, up) * Quaternion.Euler(glassesTurn));
            glassesOn.transform.SetParent(head, true);
            glassesOn.SetActive(true);
            Plugin.Log.LogInfo($"[glasses] on at {glassesOn.transform.position} (eyes {eyes}, face {fwd}) turn {glassesTurn}");
        }

        void TakeGlassesOff()
        {
            if (glassesOn != null) Destroy(glassesOn);
            glassesOn = null;
        }

        void DoGlasses()
        {
            if (GlassesWorn)
            {
                TakeGlassesOff();
                Play(Pick(Personas.GlassesOff(Pn.Id)));
                return;
            }
            PutGlassesOn();
            if (!GlassesWorn) return;
            Did("glasses");
            AddAff(1);
            Play(Pick(Personas.GlassesOn(Pn.Id)), CheckMilestones);
        }

        public Vector3 FaceForward => FaceFrame(out _, out var f, out _) ? f : -mitaBody.forward;

        public void DevGlasses(string arg)
        {
            if (arg == "off") { TakeGlassesOff(); return; }
            if (arg.StartsWith("pos "))
            {
                // glasses pos <forward> <up> <rx> <ry> <rz>
                var p = arg.Substring(4).Split(' ');
                glassesFwd = float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture);
                glassesUp = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
                if (p.Length >= 5) glassesTurn = new Vector3(float.Parse(p[2]), float.Parse(p[3]), float.Parse(p[4]));
                TakeGlassesOff();
            }
            PutGlassesOn();
        }
    }

    public static partial class Personas
    {
        public static Line[][] GlassesOn(string id) => id switch
        {
            "kind" => new[] { S(M("안경이요? …어때요. 좀 더 엔지니어 같아 보여요?", "smile"), M("도수는 없어요. 그냥 분위기용이에요.", "smiletonque")) },
            "cap" => new[] { S(M("안경 쓴 캐피! 지적인 락스타 콘셉트!", "smile"), M("…앞이 좀 흐릿하지만 멋있으면 된 거죠! 히히!", "smiletonque")) },
            "sleepy" => new[] { S(M("안경… 쓰니까 눈이 좀 떠지는 것 같아요…", "halfsleep"), M("…아니다, 더 졸려요…", "smile")) },
            "short" => new[] { S(M("안경 쓴 선생님! 이제 좀 선생님 같죠?", "smile"), M("자, 수업 시작합니다. 에헴!", "smiletonque")) },
            "chibi" => new[] { S(M("안경이 너무 커요! 코에서 흘러내려요!", "angry"), M("…그래도 똑똑해 보이죠? 헤헤!", "smile")) },
            "mila" => new[] { S(M("…저 이미 안경 쓰고 있거든요, 바보.", "discontent")) },
            _ => new[] { S(M("안경 쓴 저, 어때요?", "shy"), M("[player]님이 좋아하면 매일 쓸게요~", "smile")),
                         S(M("짜잔~ 똑똑해 보여요?", "smiletonque"), M("안경 너머로 보니까 [player]님이 더 잘 보여요.", "shy")) },
        };

        public static Line[][] GlassesOff(string id) => id switch
        {
            "cap" => new[] { S(M("안경 벗기! 락스타 모드 종료!", "smile")) },
            "chibi" => new[] { S(M("휴, 이제 앞이 잘 보여요!", "smile")) },
            "sleepy" => new[] { S(M("안경 벗으니까… 편해요…", "halfsleep")) },
            _ => new[] { S(M("안경은 이제 벗을게요. 잘 간직해 둘게요.", "smile")) },
        };
    }
}
