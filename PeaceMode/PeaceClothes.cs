using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace MiSidePeaceMode
{
    // "옷 갈아입기": the original Mita wears the game's school uniform (the main menu's "의상", resource
    // DataClothesMita: original, school uniform in three colours, vampire, Christmas). The scene's Mita drops her
    // MitaClothes after dressing at start (and a new one fails in ReCloth), so the costume's meshes, materials and
    // small extras are put on her renderers here.
    public partial class PeaceDirector
    {
        class Outfit { public Mesh mesh; public Material[] mats; public Transform[] bones; public Transform root; }
        readonly Dictionary<string, Outfit> originalOutfit = new Dictionary<string, Outfit>();
        readonly List<GameObject> costumeParts = new List<GameObject>();
        readonly Dictionary<int, (Renderer r, Mesh mesh, Material[] mats)> addonOriginal = new Dictionary<int, (Renderer, Mesh, Material[])>();
        string wearing = "original";
        int wearingVariant;

        static readonly string[] OutfitParts = { "Attribute", "Body", "Pantyhose", "Shoes", "Skirt", "Sweater" };

        DataClothMita Costume(string key)
        {
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<MitaClothesResource>()))
            {
                var r = o.Cast<MitaClothesResource>();
                if (r?.clothes == null) continue;
                foreach (var c in r.clothes) if (c != null && c.fileSave == key) return c;
            }
            return null;
        }

        // (by the renderer: "Sweater" and "Skirt" are also the names of her cloth-physics bones)
        SkinnedMeshRenderer Part(string n)
        {
            if (mitaBody == null) return null;
            foreach (var r in mitaBody.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (r != null && r.name == n) return r;
            return null;
        }

        bool Dress(string key, int variant)
        {
            if (mitaBody == null) return false;
            // what she wears at the start, to come back to
            if (originalOutfit.Count == 0)
                foreach (var n in OutfitParts)
                {
                    var r = Part(n);
                    if (r != null) originalOutfit[n] = new Outfit { mesh = r.sharedMesh, mats = r.sharedMaterials.ToArray(), bones = r.bones.ToArray(), root = r.rootBone };
                }
            // her own skeleton back on every part (a full costume rebinds them to bones of its own) before anything else
            foreach (var kv in originalOutfit)
            {
                var r = Part(kv.Key);
                if (r == null) continue;
                if (kv.Value.bones != null) r.bones = kv.Value.bones;
                r.rootBone = kv.Value.root;
                r.enabled = true;
            }
            foreach (var g in costumeParts) if (g != null) Destroy(g);
            costumeParts.Clear();
            if (key == "original")
            {
                foreach (var kv in originalOutfit)
                {
                    var r = Part(kv.Key);
                    if (r == null) continue;
                    r.sharedMesh = kv.Value.mesh;
                    r.sharedMaterials = kv.Value.mats;
                }
                foreach (var ao in addonOriginal.Values)
                {
                    if (ao.r == null) continue;
                    var mf = ao.r.GetComponent<MeshFilter>();
                    if (mf != null && ao.mesh != null) mf.sharedMesh = ao.mesh;
                    ao.r.sharedMaterials = ao.mats;
                }
                wearing = key; wearingVariant = 0;
                S.Outfit = "";
                return true;
            }
            var c = Costume(key);
            if (c == null) { Plugin.Log.LogWarning("[clothes] no costume " + key); return false; }
            var v = c.variants != null && c.variants.Count > 0 ? c.variants[Mathf.Clamp(variant, 0, c.variants.Count - 1)] : null;
            if (DevRunner.Active)
            {
                string M(Mesh m) => m == null ? "-" : $"{m.name}#{m.GetInstanceID()}";
                string A<T>(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T> a) where T : UnityEngine.Object => a == null ? "null" : $"[{string.Join(",", a.Select(x => x != null ? x.name : "-"))}]";
                Plugin.Log.LogInfo($"[clothes] {key}: body {M(c.bodyMesh)} (was {M(originalOutfit.TryGetValue("Body", out var ob) ? ob.mesh : null)}) sweater {M(c.sweaterMesh)} (was {M(originalOutfit.TryGetValue("Sweater", out var os) ? os.mesh : null)}) skirt {M(c.skirtMesh)} attr {M(c.attributeMesh)} face {(c.textureFace != null ? c.textureFace.name : "-")} hair {(c.textureHairs != null ? c.textureHairs.name : "-")}");
                if (v != null) Plugin.Log.LogInfo($"[clothes]   variant {variant}: bodyM {A(v.bodyMaterials)} bodyT {A(v.bodyTextures)} sweaterM {A(v.sweaterMaterials)} sweaterT {A(v.sweaterTextures)} skirtM {A(v.skirtMaterials)} skirtT {A(v.skirtTextures)} attrM {A(v.attributeMaterials)} attrT {A(v.attributeTextures)} addon {(v.addon != null ? v.addon.Length : 0)}");
            }
            void Put(string n, Mesh mesh, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Material> mats, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Texture2D> texs)
            {
                var r = Part(n);
                if (r == null) return;
                var orig = originalOutfit.TryGetValue(n, out var o) ? o : null;
                r.sharedMesh = mesh != null ? mesh : orig?.mesh;
                if (mats != null && mats.Length > 0) r.sharedMaterials = mats;
                else if (orig != null) r.sharedMaterials = orig.mats;
                if (texs != null && texs.Length > 0)
                {
                    var ms = r.materials;   // (instances: the textures go on her copy only)
                    for (int i = 0; i < ms.Length && i < texs.Length; i++) if (texs[i] != null) ms[i].mainTexture = texs[i];
                }
            }
            Put("Attribute", c.attributeMesh, v?.attributeMaterials, v?.attributeTextures);
            Put("Body", c.bodyMesh, v?.bodyMaterials, v?.bodyTextures);
            Put("Pantyhose", c.pantyhoseMesh, v?.pantyhoseMaterials, v?.pantyhoseTextures);
            Put("Shoes", c.shoesMesh, v?.shoesMaterials, v?.shoesTextures);
            Put("Skirt", c.skirtMesh, v?.skirtMaterials, v?.skirtTextures);
            Put("Sweater", c.sweaterMesh, v?.sweaterMaterials, v?.sweaterTextures);
            // the vampire and Christmas costumes: skinned to a skeleton of their own (Mita's bones + the dress hem, sleeves,
            // choker, the Santa hat...). Their extra bones go onto hers, and each part's bone list is rebuilt from the
            // costume's own (with her original list - 148 bones - the meshes' 175 / 72 bone indices pointed at the wrong
            // bones: the vampire lost her body, the Santa hat was black shards)
            if (c.prefabFull != null && !devCostumeParts)
            {
                try { BindFullCostume(c); }
                catch (Exception e) { Plugin.Log.LogError("[clothes] " + key + ": " + e); }
            }
            // her small extras (MitaClothesAddon: shoes' bows and the like) take the variant's meshes too
            if (v?.addon != null)
                foreach (var ad in mitaBody.GetComponentsInChildren<MitaClothesAddon>(true))
                {
                    if (ad?.objects == null) continue;
                    foreach (var ob in ad.objects)
                    {
                        if (ob?.objectMesh == null || ob.indexAddon < 0 || ob.indexAddon >= v.addon.Length) continue;
                        var add = v.addon[ob.indexAddon];
                        if (add == null) continue;
                        if (!addonOriginal.ContainsKey(ob.objectMesh.GetInstanceID()))
                            addonOriginal[ob.objectMesh.GetInstanceID()] = (ob.objectMesh, ob.objectMesh.GetComponent<MeshFilter>()?.sharedMesh, ob.objectMesh.sharedMaterials.ToArray());
                        var mf = ob.objectMesh.GetComponent<MeshFilter>();
                        if (mf != null && add.mesh != null) mf.sharedMesh = add.mesh;
                        if (add.materials != null && add.materials.Length > 0) ob.objectMesh.sharedMaterials = add.materials;
                        if (add.textures != null && add.textures.Length > 0)
                        {
                            var ms = ob.objectMesh.materials;
                            for (int i = 0; i < ms.Length && i < add.textures.Length; i++) if (add.textures[i] != null) ms[i].mainTexture = add.textures[i];
                        }
                    }
                }
            // the extra parts, skinned to her own bones
            // (a costume's prefab parts carry bones of their own that this Mita doesn't have: the vampire's cape broke
            // apart and the Santa hat left black shards, so only the dev command still tries them)
            if (c.prefabFull != null && devCostumeParts)
            {
                var extra = Instantiate(c.prefabFull, mitaBody);
                extra.name = "PeaceCostume " + key;
                int bound = 0, missing = 0;
                foreach (var smr in extra.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var bones = smr.bones;
                    var mine = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Transform>(bones.Length);
                    for (int i = 0; i < bones.Length; i++)
                    {
                        var b = bones[i] != null ? Util.FindDeep(mitaBody, bones[i].name) : null;
                        if (b != null && !b.IsChildOf(extra.transform)) { mine[i] = b; bound++; } else { mine[i] = bones[i]; missing++; }
                    }
                    smr.bones = mine;
                    if (smr.rootBone != null) { var rb = Util.FindDeep(mitaBody, smr.rootBone.name); if (rb != null && !rb.IsChildOf(extra.transform)) smr.rootBone = rb; }
                    smr.updateWhenOffscreen = true;
                }
                costumeParts.Add(extra);
                Plugin.Log.LogInfo($"[clothes] {key} extra parts: {bound} bones bound, {missing} kept");
            }
            wearing = key; wearingVariant = variant;
            S.Outfit = key + ":" + variant;
            Plugin.Log.LogInfo($"[clothes] now wearing {key} #{variant}");
            return true;
        }

        void BindFullCostume(DataClothMita c)
        {
            // the costume's rig, inactive (its scripts - cloth physics, MitaClothesMagic - stay asleep)
            var holder = new GameObject("PeaceCostumeHolder");
            holder.SetActive(false);
            var inst = Instantiate(c.prefabFull, holder.transform, false);
            foreach (var b in inst.GetComponentsInChildren<Behaviour>(true)) b.enabled = false;
            var mine = new Dictionary<string, Transform>();
            foreach (var t in mitaBody.GetComponentsInChildren<Transform>(true))
                if (t != null && !mine.ContainsKey(t.name) && !t.name.StartsWith("PeaceSkin")) mine[t.name] = t;
            // every bone of the costume that she doesn't have, whose parent she has: moved onto that bone of hers
            // (local pose kept - the same rig), with all of its own children
            var moved = new Dictionary<string, Transform>();
            var all = inst.GetComponentsInChildren<Transform>(true).ToList();
            foreach (var t in all)
            {
                if (t == null || mine.ContainsKey(t.name) || t.GetComponent<Renderer>() != null || t.parent == null) continue;
                if (!mine.TryGetValue(t.parent.name, out var parent)) continue;
                if (t.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0) continue;   // (not the model's own nodes)
                var lp = t.localPosition; var lr = t.localRotation; var ls = t.localScale;
                t.SetParent(parent, false);
                t.localPosition = lp; t.localRotation = lr; t.localScale = ls;
                t.gameObject.name = t.name;   // (kept: the skinned parts find it by name)
                costumeParts.Add(t.gameObject);
                foreach (var d in t.GetComponentsInChildren<Transform>(true)) if (!moved.ContainsKey(d.name)) moved[d.name] = d;
            }
            Transform Find(string n) => mine.TryGetValue(n, out var a) ? a : moved.TryGetValue(n, out var b) ? b : null;
            int parts = 0, unbound = 0;
            foreach (var n in OutfitParts)
            {
                var r = Part(n);
                if (r == null) continue;
                SkinnedMeshRenderer src = null;
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (smr.name == n) { src = smr; break; }
                if (src == null || src.sharedMesh == null) { r.enabled = false; continue; }   // (the Christmas dress has no skirt)
                var bones = src.bones;
                var mapped = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Transform>(bones.Length);
                for (int i = 0; i < bones.Length; i++)
                {
                    var b = bones[i] != null ? Find(bones[i].name) : null;
                    if (b == null) { unbound++; b = Find("Hips"); }
                    mapped[i] = b;
                }
                r.sharedMesh = src.sharedMesh;
                r.bones = mapped;
                r.rootBone = Find(src.rootBone != null ? src.rootBone.name : "Hips") ?? r.rootBone;
                r.updateWhenOffscreen = true;
                parts++;
            }
            Destroy(holder);
            Plugin.Log.LogInfo($"[clothes] full costume {c.fileSave}: {parts} parts rebound, {moved.Count} bones added, {unbound} bones unbound");
        }

        // a costume's big standing collar (the vampire one) folds down while she is right against you - in a hug the view
        // is at her shoulder and the collar's wing filled the whole screen - and stands up again as she lets go
        float collarFold = 1f, collarFoldTarget = 1f;
        void FoldCollar(bool fold) => collarFoldTarget = fold ? 0.3f : 1f;

        void TickCollar(float dt)
        {
            if (Mathf.Approximately(collarFold, collarFoldTarget)) return;
            collarFold = Mathf.MoveTowards(collarFold, collarFoldTarget, dt / (collarFoldTarget < collarFold ? 0.4f : 0.6f) * 0.7f);
            foreach (var g in costumeParts)
                if (g != null && g.name.StartsWith("Collar")) g.transform.localScale = Vector3.one * collarFold;
        }

        // the costume menu: every costume (and its colours)
        void OpenClothesMenu()
        {
            var opts = new List<string>();
            var acts = new List<Action>();
            void Add(string label, string key, int variant) { opts.Add(label); acts.Add(() => ChangeClothes(key, variant)); }
            if (wearing != "original") Add("원래 옷", "original", 0);
            if (Costume("FIIdClSchool") != null)
            {
                int n = Costume("FIIdClSchool").variants?.Count ?? 1;
                for (int i = 0; i < n; i++) if (wearing != "FIIdClSchool" || wearingVariant != i) Add(n > 1 ? Loc.F("교복 ({0})", i + 1) : "교복", "FIIdClSchool", i);
            }
            if (Costume("HellVamp") != null && wearing != "HellVamp") Add("뱀파이어 옷", "HellVamp", 0);
            if (Costume("Chirfns") != null && wearing != "Chirfns") Add("크리스마스 옷", "Chirfns", 0);
            opts.Add("그만두기"); acts.Add(() => { });
            ShowMenu("옷 갈아입기", opts, acts);
        }

        // behind a short fade, like the story's own changes
        void ChangeClothes(string key, int variant)
        {
            Did("clothes");
            fadeTarget = 1f;
            afterFade = () =>
            {
                Dress(key, variant);
                fadeTarget = 0f;
                afterFade = () => Play(Pick(Personas.ClothesLines(key)), () => { AddAff(1); CheckMilestones(); });
            };
        }

        // what she wore last time
        void DressAsSaved()
        {
            if (Pn.Id != "crazy" || skin != null || string.IsNullOrEmpty(S.Outfit)) return;
            var p = S.Outfit.Split(':');
            int v = p.Length > 1 && int.TryParse(p[1], out var n) ? n : 0;
            try { Dress(p[0], v); } catch (Exception e) { Plugin.Log.LogWarning("[clothes] saved outfit: " + e.Message); }
        }

        // skinmats <Resources path>: a gallery model's renderers, materials, textures and colour properties
        public static string DevSkinMats(string path)
        {
            var sb = new System.Text.StringBuilder();
            var o = Resources.Load(path, Il2CppType.Of<GameObject>());
            if (o == null) return "no " + path;
            var go = o.Cast<GameObject>();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                sb.AppendLine($"{r.name} ({r.GetIl2CppType().Name})");
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("  -"); continue; }
                    var t = m.mainTexture;
                    sb.Append($"  {m.name} shader={m.shader?.name} tex={(t != null ? t.name + " " + t.width + "x" + t.height : "-")}");
                    foreach (var prop in new[] { "_Color", "_BaseColor", "_MainColor", "_ShadeColor", "_1st_ShadeColor", "_2nd_ShadeColor", "_RimColor" })
                        if (m.HasProperty(prop)) sb.Append($" {prop}={m.GetColor(prop)}");
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        // costumedump <key>: what a costume is made of (its meshes, its prefab's tree and skinned parts) -> costume_<key>.txt
        public string DevCostumeDump(string key)
        {
            var sb = new System.Text.StringBuilder();
            var c = Costume(key);
            if (c == null) return "no costume " + key;
            string M(Mesh m) => m == null ? "-" : $"{m.name} v{m.vertexCount} bp{m.bindposes.Length}";
            sb.AppendLine($"body {M(c.bodyMesh)} sweater {M(c.sweaterMesh)} skirt {M(c.skirtMesh)} attr {M(c.attributeMesh)} pantyhose {M(c.pantyhoseMesh)} shoes {M(c.shoesMesh)}");
            sb.AppendLine($"variants {(c.variants != null ? c.variants.Count : 0)} prefabFull {(c.prefabFull != null ? c.prefabFull.name : "-")}");
            if (c.variants != null)
                for (int i = 0; i < c.variants.Count; i++)
                {
                    var v = c.variants[i];
                    string A<T>(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T> a) where T : UnityEngine.Object => a == null ? "null" : $"[{string.Join(",", a.Select(x => x != null ? x.name : "-"))}]";
                    sb.AppendLine($" v{i}: bodyM {A(v.bodyMaterials)} sweaterM {A(v.sweaterMaterials)} skirtM {A(v.skirtMaterials)} attrM {A(v.attributeMaterials)} addon {(v.addon != null ? v.addon.Length : 0)}");
                }
            if (c.prefabFull != null)
            {
                void Tree(Transform t, int d)
                {
                    if (d > 7) return;
                    var comps = string.Join(",", t.GetComponents<Component>().Select(x => x.GetIl2CppType().Name).Where(n => n != "Transform"));
                    sb.AppendLine(new string(' ', d * 2) + t.name + (comps.Length > 0 ? "  {" + comps + "}" : "") + $" pos{t.localPosition}");
                    for (int i = 0; i < t.childCount; i++) Tree(t.GetChild(i), d + 1);
                }
                Tree(c.prefabFull.transform, 0);
                var mine = mitaBody != null ? new HashSet<string>(mitaBody.GetComponentsInChildren<Transform>(true).Select(t => t.name)) : new HashSet<string>();
                foreach (var smr in c.prefabFull.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var bones = smr.bones;
                    int have = bones.Count(b => b != null && mine.Contains(b.name));
                    sb.AppendLine($"SMR {smr.name}: mesh {M(smr.sharedMesh)} root {(smr.rootBone != null ? smr.rootBone.name : "-")} bones {bones.Length} (in Mita {have}) mats [{string.Join(",", smr.sharedMaterials.Select(m => m != null ? m.name + "/" + m.shader.name : "-"))}]");
                    sb.AppendLine("   missing: " + string.Join(",", bones.Where(b => b != null && !mine.Contains(b.name)).Select(b => b.name).Take(40)));
                }
            }
            return sb.ToString();
        }

        bool devCostumeParts;
        public void DevCloth(string arg)
        {
            var sp = arg.Split(' ');
            devCostumeParts = sp.Length > 2 && sp[2] == "parts";
            Dress(sp[0], sp.Length > 1 ? int.Parse(sp[1]) : 0);
        }
    }

    public static partial class Personas
    {
        // (only the original Mita changes clothes: the others are their own models)
        public static Line[][] ClothesLines(string key) => key switch
        {
            "FIIdClSchool" => new[] { S(M("짜잔~ 교복이에요!", "smile"), M("학생이 아니어도 교복은 귀엽잖아요. 어때요?", "shy")),
                                      S(M("교복 입으니까 학교 다니는 기분이에요~", "smile"), M("[player]님은 선배 해요. 제가 후배 할게요!", "smiletonque")) },
            "HellVamp" => new[] { S(M("크크크… 오늘 밤 당신의 피를…", "smilestrange"), M("…농담이에요! 뱀파이어 옷, 멋있죠?", "smiletonque")) },
            "Chirfns" => new[] { S(M("메리 크리스마스~!", "smile"), M("선물은 저예요. 포장은 이 옷으로 대신했어요~", "shy")) },
            _ => new[] { S(M("역시 원래 옷이 제일 편하네요.", "smile"), M("[player]님도 이 옷이 제일 익숙하죠?", "smile")) },
        };
    }
}
