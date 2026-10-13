using System;
using System.Collections.Generic;
using System.Text;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace MiSidePeaceMode
{
    // Dresses the scene's Mita as another Mita version.
    // The game ships every Mita as a gallery model ("Personages/..." in Resources, used by the main menu's
    // character list). We instantiate that model with its scripts off, hide the original Mita's renderers and
    // retarget her animated pose + face blend shapes onto the model every frame, so navigation, animations,
    // emotions and look-at all keep working through the original Mita.
    // Bones share names across versions but not always their axes, so rotations are transferred relative to each
    // model's bind pose (taken from the meshes' bindposes): model = mita * inverse(mitaBind) * modelBind.
    public class MitaSkin
    {
        public GameObject Root { get; private set; }
        public AnimationClip IdleClip { get; set; }   // the model's own idle loop from its gallery animator (or her story one, see PeaceAnims)
        public AnimationClip GalleryIdle { get; private set; }
        public readonly List<Renderer> Renderers = new List<Renderer>();

        readonly List<(Transform src, Transform dst, Quaternion fix)> bones = new List<(Transform, Transform, Quaternion)>();
        Transform srcRoot;
        readonly List<(SkinnedMeshRenderer dst, int di, SkinnedMeshRenderer src, int si)> shapes = new List<(SkinnedMeshRenderer, int, SkinnedMeshRenderer, int)>();
        readonly List<Renderer> hidden = new List<Renderer>();
        readonly List<MagicaCloth.BaseCloth> cloths = new List<MagicaCloth.BaseCloth>();
        int syncCount;
        Transform hipsSrc, hipsDst;
        float hipsScale = 1f;

        public int MatchedBones => bones.Count;
        public bool OwnAnimator { get; private set; }
        public int MatchedShapes => shapes.Count;

        static GameObject LoadPrefab(string path)
        {
            var o = Resources.Load(path, Il2CppType.Of<GameObject>());
            return o != null ? o.Cast<GameObject>() : null;
        }

        static Dictionary<string, Transform> MapByName(Transform root, Func<Transform, bool> skip)
        {
            var map = new Dictionary<string, Transform>();
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                if (skip != null && skip(t)) continue;
                if (!map.ContainsKey(t.name)) map[t.name] = t;
                for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
            }
            return map;
        }

        // bone -> (rotation, position) at bind time, expressed in root's local space.
        // Some gallery models bind one accessory mesh in a different pose (Kind/Cappie "Attribute": arms off by 70-160°),
        // so for every bone we take the value most of its meshes agree on.
        static Dictionary<IntPtr, (Quaternion rot, Vector3 pos)> BindPoses(Transform root, IEnumerable<SkinnedMeshRenderer> smrs)
        {
            var all = new Dictionary<IntPtr, List<(Quaternion, Vector3)>>();
            foreach (var smr in smrs)
            {
                if (smr == null || smr.sharedMesh == null) continue;
                var bp = smr.sharedMesh.bindposes;
                var bs = smr.bones;
                var rel = root.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                int n = Math.Min(bp.Length, bs.Length);
                for (int i = 0; i < n; i++)
                {
                    var b = bs[i];
                    if (b == null) continue;
                    var m = rel * bp[i].inverse;
                    if (!all.TryGetValue(b.Pointer, out var list)) all[b.Pointer] = list = new List<(Quaternion, Vector3)>();
                    list.Add((m.rotation, (Vector3)m.GetColumn(3)));
                }
            }
            var d = new Dictionary<IntPtr, (Quaternion, Vector3)>();
            foreach (var kv in all)
            {
                var list = kv.Value;
                int best = 0, bestVotes = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    int votes = 0;
                    for (int j = 0; j < list.Count; j++)
                        if (Quaternion.Angle(list[i].Item1, list[j].Item1) < 2f && (list[i].Item2 - list[j].Item2).sqrMagnitude < 0.0004f) votes++;
                    if (votes > bestVotes) { bestVotes = votes; best = i; }
                }
                d[kv.Key] = list[best];
            }
            return d;
        }

        static readonly string[] ClothWords = { "skirt", "hair", "strand", "tail", "bang", "tie", "ribbon", "sweater", "cap", "braid", "bow", "scarf", "coat", "cloth", "lace", "frill", "ear" };

        static bool IsClothBone(string name)
        {
            var n = name.ToLowerInvariant();
            if (n.Contains("head") || n.Contains("neck") || n.Contains("spine") || n.Contains("chest") || n.Contains("hips") || n.Contains("shoulder")) return false;
            foreach (var w in ClothWords) if (n.Contains(w)) return true;
            return false;
        }

        static string ShapeKey(string n)
        {
            int dot = n.LastIndexOf('.');
            return (dot >= 0 ? n.Substring(dot + 1) : n).ToLowerInvariant();
        }

        public static MitaSkin Apply(Transform mitaBody, string path, Func<Transform, bool> excludeFromMita, float scale = 1f)
        {
            var prefab = LoadPrefab(path);
            if (prefab == null) { Plugin.Log.LogWarning("[skin] prefab not found: " + path); return null; }

            var skin = new MitaSkin();
            // instantiate under an inactive holder so none of the model's own scripts wake up
            var holder = new GameObject("PeaceSkinHolder");
            holder.SetActive(false);
            var inst = UnityEngine.Object.Instantiate(prefab, holder.transform, false);
            inst.name = "PeaceSkin " + prefab.name;
            // the model's own scripts stay off, except its cloth physics (hair, skirt, ribbons: MagicaCloth)
            var physicsBones = new HashSet<string>();
            foreach (var b in inst.GetComponentsInChildren<Behaviour>(true))
            {
                if (b.GetIl2CppType().Name.StartsWith("Magica"))
                {
                    var cloth = b.TryCast<MagicaCloth.BaseCloth>();
                    if (cloth != null)
                    {
                        skin.cloths.Add(cloth);
                        b.enabled = false;   // switched on by Sync once the body is posed
                        try
                        {
                            // cloths also report the body bones they hang from; only hair/skirt/ribbon chains are left to physics
                            var names = cloth.GetUsedComponentBoneNames();
                            if (names != null) for (int i = 0; i < names.Count; i++) if (IsClothBone(names[i])) physicsBones.Add(names[i]);
                        }
                        catch (Exception e) { Plugin.Log.LogWarning("[skin] cloth bones: " + e.Message); }
                    }
                    continue;
                }
                b.enabled = false;
            }
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var rb in inst.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;

            // earlier skins may still exist until the end of the frame: never treat them as Mita's own parts
            bool Skip(Transform t) => t == inst.transform || IsUnderName(t, "PeaceSkin ") || (excludeFromMita != null && excludeFromMita(t));
            var mitaMap = MapByName(mitaBody, t => Skip(t));

            // bones = everything the model's skinned meshes are bound to (plus their parents)
            var boneSet = new HashSet<IntPtr>();
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (var b in smr.bones)
                    for (var t = b; t != null && t != inst.transform; t = t.parent) boneSet.Add(t.Pointer);
                for (var t = smr.rootBone; t != null && t != inst.transform; t = t.parent) boneSet.Add(t.Pointer);
            }
            var mitaSmrs = new List<SkinnedMeshRenderer>();
            foreach (var smr in mitaBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr != null && !Skip(smr.transform) && !IsUnder(smr.transform, inst.transform)) mitaSmrs.Add(smr);
            var bindSrc = BindPoses(mitaBody, mitaSmrs);
            var bindDst = BindPoses(inst.transform, inst.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            skin.srcRoot = mitaBody;
            int nameMatches = 0;
            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                if (boneSet.Contains(t.Pointer) && mitaMap.ContainsKey(t.name)) nameMatches++;
            // pre-order, so parents are posed before their children
            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
            {
                if (!boneSet.Contains(t.Pointer)) continue;
                if (physicsBones.Contains(t.name)) continue;   // simulated by the model's own cloth
                if (!mitaMap.TryGetValue(t.name, out var src)) continue;
                if (!bindSrc.TryGetValue(src.Pointer, out var bs) || !bindDst.TryGetValue(t.Pointer, out var bd)) continue;
                skin.bones.Add((src, t, Quaternion.Inverse(bs.rot) * bd.rot));
                if (skin.hipsSrc == null && t.name.ToLowerInvariant().Contains("hips"))
                {
                    skin.hipsSrc = src; skin.hipsDst = t;
                    skin.hipsScale = Mathf.Abs(bs.pos.y) > 0.0001f ? bd.pos.y / bs.pos.y : 1f;
                }
            }
            // a different rig (Chibi): keep playing the model's own animations instead of mirroring Mita
            if (nameMatches < boneSet.Count * 0.3f)
            {
                skin.bones.Clear();
                skin.hipsSrc = skin.hipsDst = null;
                foreach (var an in inst.GetComponentsInChildren<Animator>(true)) { an.enabled = true; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
                skin.OwnAnimator = true;
            }

            // original renderers: hide them, and remember their blend shapes by name
            var srcShapes = new Dictionary<string, (SkinnedMeshRenderer, int)>();
            foreach (var r in mitaBody.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || Skip(r.transform) || IsUnder(r.transform, inst.transform)) continue;
                var smr = r.TryCast<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh != null)
                    for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                    {
                        var k = ShapeKey(smr.sharedMesh.GetBlendShapeName(i));
                        if (!srcShapes.ContainsKey(k)) srcShapes[k] = (smr, i);
                    }
                // only her body meshes: held props (cookie, juice...) keep working as before
                if (smr == null || !r.enabled) continue;
                r.enabled = false;
                skin.hidden.Add(r);
            }
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                skin.Renderers.Add(smr);
                if (smr.sharedMesh == null) continue;
                for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                    if (srcShapes.TryGetValue(ShapeKey(smr.sharedMesh.GetBlendShapeName(i)), out var s))
                        skin.shapes.Add((smr, i, s.Item1, s.Item2));
            }
            foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true)) skin.Renderers.Add(mr);

            // animators on the original keep running even though nothing of hers is visible now
            foreach (var an in mitaBody.GetComponentsInChildren<Animator>(true)) an.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var pls = prefab.transform.localScale;
            var mls = mitaBody.lossyScale;
            inst.transform.SetParent(mitaBody, false);
            inst.transform.localPosition = Vector3.zero;
            // gallery models already face the same way as the agent's front (a 180° turn showed the chibi's back)
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = new Vector3(pls.x / Mathf.Max(mls.x, 0.0001f), pls.y / Mathf.Max(mls.y, 0.0001f), pls.z / Mathf.Max(mls.z, 0.0001f)) * scale;
            UnityEngine.Object.Destroy(holder);
            skin.Root = inst;
            var pan = prefab.GetComponentInChildren<Animator>(true);
            if (pan != null && pan.runtimeAnimatorController != null)
                foreach (var c in pan.runtimeAnimatorController.animationClips)
                    if (c != null && c.isLooping) { skin.IdleClip = skin.GalleryIdle = c; break; }
            skin.Sync();
            Plugin.Log.LogInfo($"[skin] {path}: physics bones: {string.Join(", ", new List<string>(physicsBones).GetRange(0, Math.Min(physicsBones.Count, 25)))}");
            Plugin.Log.LogInfo($"[skin] {path}: cloths {skin.cloths.Count} physicsBones {physicsBones.Count}, bones {skin.bones.Count}/{boneSet.Count} own={skin.OwnAnimator} hipsScale={skin.hipsScale:0.00}, shapes {skin.shapes.Count}, hidden {skin.hidden.Count}, renderers {skin.Renderers.Count}");
            return skin;
        }

        static bool IsUnder(Transform t, Transform root)
        {
            for (; t != null; t = t.parent) if (t == root) return true;
            return false;
        }

        public void Sync()
        {
            if (Root == null || srcRoot == null) return;
            var root = Root.transform;
            var invSrc = Quaternion.Inverse(srcRoot.rotation);
            var dstRot = root.rotation;
            if (hipsSrc != null && hipsDst != null)
                hipsDst.position = root.TransformPoint(srcRoot.InverseTransformPoint(hipsSrc.position) * hipsScale);
            foreach (var (src, dst, fix) in bones)
            {
                if (src == null || dst == null) continue;
                dst.rotation = dstRot * (invSrc * src.rotation) * fix;
            }
            foreach (var (dst, di, src, si) in shapes)
                if (dst != null && src != null) dst.SetBlendShapeWeight(di, src.GetBlendShapeWeight(si));
            // start the model's cloth only once the body is posed, so hair and skirt don't swing in from the default pose
            if (++syncCount == 3)
                foreach (var c in cloths) if (c != null) c.enabled = true;
        }

        public void Remove()
        {
            foreach (var r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
            if (Root != null) UnityEngine.Object.Destroy(Root);
            Root = null;
        }

        // ------------------------------------------------------------ dev report

        // Every skinned bone of a model: current pose and bind pose, both in the model root's space.
        // path "mita" = the scene's Mita (current pose = her animated pose).
        public static string PoseDump(string path, Transform mitaBody)
        {
            var sb = new StringBuilder();
            GameObject holder = null;
            Transform root;
            if (path == "mita") root = mitaBody;
            else
            {
                var prefab = LoadPrefab(path);
                if (prefab == null) return "no prefab " + path;
                holder = new GameObject("PoseDumpHolder");
                holder.SetActive(false);
                root = UnityEngine.Object.Instantiate(prefab, holder.transform, false).transform;
            }
            if (root == null) return "no root";
            var smrs = new List<SkinnedMeshRenderer>();
            foreach (var s in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (!s.transform.name.StartsWith("PeaceSkin") && !IsUnderName(s.transform, "PeaceSkin ")) smrs.Add(s);
            sb.AppendLine("SMRS " + string.Join(",", smrs.ConvertAll(s => s.name + "@" + s.transform.localRotation.eulerAngles)));
            var inv = root.worldToLocalMatrix;
            var seen = new HashSet<IntPtr>();
            foreach (var smr in smrs)
            {
                if (smr.sharedMesh == null) continue;
                var bp = smr.sharedMesh.bindposes;
                var bs = smr.bones;
                var rel = inv * smr.transform.localToWorldMatrix;
                for (int i = 0; i < Math.Min(bp.Length, bs.Length); i++)
                {
                    var b = bs[i];
                    if (b == null) continue;
                    var bind = rel * bp[i].inverse;
                    var cur = inv * b.localToWorldMatrix;
                    bool first = seen.Add(b.Pointer);
                    // one line per (bone, renderer): name | renderer | parent | current pos | current rot | bind pos | bind rot | local rot
                    sb.AppendLine($"B|{b.name}|{smr.name}|{(b.parent != null ? b.parent.name : "-")}|{V((Vector3)cur.GetColumn(3))}|{Q(cur.rotation)}|{V((Vector3)bind.GetColumn(3))}|{Q(bind.rotation)}|{Q(b.localRotation)}|{(first ? 1 : 0)}");
                }
            }
            if (holder != null) UnityEngine.Object.Destroy(holder);
            return sb.ToString();
        }

        static bool IsUnderName(Transform t, string prefix)
        {
            for (; t != null; t = t.parent) if (t.name.StartsWith(prefix)) return true;
            return false;
        }

        static string V(Vector3 v) => $"{v.x:0.0000},{v.y:0.0000},{v.z:0.0000}";
        static string Q(Quaternion q) => $"{q.x:0.00000},{q.y:0.00000},{q.z:0.00000},{q.w:0.00000}";

        public static string Report(string path, Transform mitaBody)
        {
            var sb = new StringBuilder();
            var prefab = LoadPrefab(path);
            sb.AppendLine("PREFAB " + path + " -> " + (prefab != null ? prefab.name + " scale " + prefab.transform.localScale : "null"));
            if (prefab == null) return sb.ToString();
            Dictionary<string, Transform> mitaMap = mitaBody != null ? MapByName(mitaBody, null) : null;
            void Walk(Transform t, int depth)
            {
                if (depth > 4) return;
                var comps = new List<string>();
                foreach (var c in t.GetComponents<Component>()) if (c != null) comps.Add(c.GetIl2CppType().Name);
                sb.AppendLine(new string(' ', depth * 2) + t.name + "  {" + string.Join(",", comps) + "}");
                for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1);
            }
            Walk(prefab.transform, 0);
            foreach (var smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int match = 0;
                var names = new List<string>();
                foreach (var b in smr.bones)
                {
                    if (b == null) continue;
                    if (mitaMap != null && mitaMap.ContainsKey(b.name)) match++; else if (names.Count < 12) names.Add(b.name);
                }
                sb.AppendLine($"SMR {smr.name} mesh={(smr.sharedMesh != null ? smr.sharedMesh.name : "-")} bones={smr.bones.Length} matched={match} root={(smr.rootBone != null ? smr.rootBone.name : "-")} shapes={(smr.sharedMesh != null ? smr.sharedMesh.blendShapeCount : 0)} mats={smr.sharedMaterials.Length}");
                if (names.Count > 0) sb.AppendLine("   unmatched: " + string.Join(", ", names));
                if (smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
                {
                    var sn = new List<string>();
                    for (int i = 0; i < Math.Min(smr.sharedMesh.blendShapeCount, 40); i++) sn.Add(smr.sharedMesh.GetBlendShapeName(i));
                    sb.AppendLine("   shapes: " + string.Join(", ", sn));
                }
            }
            if (mitaBody != null)
            {
                sb.AppendLine("--- original Mita renderers");
                foreach (var r in mitaBody.GetComponentsInChildren<Renderer>(true))
                {
                    var smr = r.TryCast<SkinnedMeshRenderer>();
                    sb.AppendLine($"R {r.name} type={r.GetIl2CppType().Name} enabled={r.enabled} shapes={(smr != null && smr.sharedMesh != null ? smr.sharedMesh.blendShapeCount : 0)} root={(smr != null && smr.rootBone != null ? smr.rootBone.name : "-")}");
                    if (smr != null && smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
                    {
                        var sn = new List<string>();
                        for (int i = 0; i < Math.Min(smr.sharedMesh.blendShapeCount, 40); i++) sn.Add(smr.sharedMesh.GetBlendShapeName(i));
                        sb.AppendLine("   shapes: " + string.Join(", ", sn));
                    }
                }
                sb.AppendLine("--- original Mita lossyScale " + mitaBody.lossyScale);
            }
            return sb.ToString();
        }
    }
}
