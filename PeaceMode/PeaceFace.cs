using System.Collections.Generic;
using UnityEngine;

namespace MiSidePeaceMode
{
    // A face the story doesn't have: utter contempt ("contempt" as a line's emotion, and her look when she is cold
    // and you stare). Her "discontent" face with blend shapes pushed over it every frame, after the animator and
    // before her skin copies the face: lids half down, one brow up and the others down, the mouth pulled to one
    // side, the nose wrinkled.
    public partial class PeaceDirector
    {
        static readonly (string shape, float weight)[] ContemptFace =
        {
            ("CozyBlink", 55f), ("EyesSquints", 55f), ("EyesAngry", 35f),
            ("EyeBrowsDown", 55f), ("EyeBrowLeftUp", 90f),
            ("AnnoyToLeft", 100f), ("AnnoySmall", 35f), ("Sniff", 70f),
        };

        bool contempt;
        float contemptBlend;
        SkinnedMeshRenderer faceSmr;
        int[] contemptIdx;

        // (every other emotion ends it)
        void Contempt(bool on)
        {
            if (on == contempt) return;
            contempt = on;
            if (on) try { mita?.FaceEmotion("discontent"); } catch { }
        }

        void ApplyFaceOverlay()
        {
            contemptBlend = Mathf.MoveTowards(contemptBlend, contempt ? 1f : 0f, Time.deltaTime * 4f);
            if (contemptBlend <= 0f || mitaBody == null) return;
            if (faceSmr == null)
            {
                var head = mitaBody.Find("Head");
                faceSmr = head != null ? head.GetComponent<SkinnedMeshRenderer>() : null;
                if (faceSmr == null || faceSmr.sharedMesh == null) return;
                contemptIdx = new int[ContemptFace.Length];
                for (int i = 0; i < ContemptFace.Length; i++) contemptIdx[i] = faceSmr.sharedMesh.GetBlendShapeIndex(ContemptFace[i].shape);
            }
            for (int i = 0; i < ContemptFace.Length; i++)
            {
                int k = contemptIdx[i];
                if (k < 0) continue;
                float now = faceSmr.GetBlendShapeWeight(k);
                faceSmr.SetBlendShapeWeight(k, Mathf.Lerp(now, ContemptFace[i].weight, contemptBlend));
            }
        }
    }
}
