using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiSidePeaceMode
{
    public static class Util
    {
        // Finds a GameObject by "Root/Child/..." path, including inactive objects, across loaded scenes.
        public static GameObject FindPath(string path)
        {
            var parts = path.Split('/');
            // "a//b" style: deep search for the last segment under the first root, checking the parent name if given
            if (!path.Contains("/") && !path.Contains(">")) path = path + ">" + path.Substring(0, 0);
            if (path.Contains(">"))
            {
                var p2 = SplitDeep(path);
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    {
                        var t = FindDeep(root.transform, p2[0]);
                        if (t == null) continue;
                        for (int k = 1; k < p2.Length && t != null; k++) t = FindDeep(t, p2[k]);
                        if (t != null) return t.gameObject;
                    }
                return null;
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded) continue;
                foreach (var root in s.GetRootGameObjects())
                {
                    if (root.name != parts[0]) continue;
                    var t = root.transform;
                    for (int k = 1; k < parts.Length && t != null; k++) t = t.Find(parts[k]);
                    if (t != null) return t.gameObject;
                }
            }
            return null;
        }

        // "A>B>C" -> A, B, C. Some objects have '>' in their own name ("Interactive Cards >>"): a '>' right after a space
        // or next to another '>' belongs to the name, so "Addon>Interactive Cards >>" -> Addon, Interactive Cards >>.
        static string[] SplitDeep(string path)
        {
            var parts = new System.Collections.Generic.List<string>();
            int start = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] != '>') continue;
                bool inName = (i > 0 && (path[i - 1] == ' ' || path[i - 1] == '>')) || (i + 1 < path.Length && path[i + 1] == '>');
                if (inName) continue;
                if (i > start) parts.Add(path.Substring(start, i - start));
                start = i + 1;
            }
            if (start < path.Length) parts.Add(path.Substring(start));
            return parts.ToArray();
        }

        // Depth-first search by exact name across all loaded scenes (includes inactive).
        public static Transform FindDeepAll(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (var root in sc.GetRootGameObjects())
                {
                    var t = FindDeep(root.transform, name);
                    if (t != null) return t;
                }
            }
            return null;
        }

        // Depth-first search by name under a root (includes inactive).
        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        // "Root>Child>Leaf" of a transform, for logs
        public static string PathOf(Transform t)
        {
            if (t == null) return "-";
            var p = t.name;
            for (var q = t.parent; q != null; q = q.parent) p = q.name + ">" + p;
            return p;
        }
    }
}
