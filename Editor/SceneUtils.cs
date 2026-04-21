#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace trit.GenericBindingResolver
{
    public static class SceneUtils{
        static string _siblingBracketL = "〈";
        static string _siblingBracketR = "〉";

        public static int GetComponentIndexSameType(Component c)
        {
            if (c == null) return -1;

            var type = c.GetType();
            var comps = c.gameObject.GetComponents(type);
            for (int i = 0; i < comps.Length; i++)
            {
                if (ReferenceEquals(comps[i], c)) return i;
            }
            return -1;
        }

        public static IEnumerable<T> GetResolvers<T>(Scene scene)where T:IResolver
        {
            List<T> resolvers = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                resolvers.AddRange(root.GetComponentsInChildren<T>(true));
            }
            return resolvers;

        }

        public static IEnumerable<UnityEngine.SceneManagement.Scene> AllScenes(){
            var scenes = new List<UnityEngine.SceneManagement.Scene>();
            for(int i=0; i<UnityEngine.SceneManagement.SceneManager.sceneCount; i++){
                scenes.Add(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));
            }
            return scenes;
        }

        public static void CollectAll<T>()where T:IResolver{
            var scenes = AllScenes();
            foreach(var scene in scenes){
                CollectAll<T>(scene);
            }
        }

        public static void CollectAll<T>(Scene scene)where T:IResolver{
            IEnumerable<T> resolvers = GetResolvers<T>(scene);
            foreach(var resolver in resolvers){
                resolver.Collect();
            }
        }

        public static void CollectAndApplyPrefabAll<T>()where T:IResolver{
            var scenes = AllScenes();
            foreach(var scene in scenes){
                CollectAndApplyPrefabAll<T>(scene);
            }
        }
        public static void CollectAndApplyPrefabAll<T>(Scene scene) where T:IResolver{
            var resolvers = GetResolvers<T>(scene);
            foreach(var resolver in resolvers){
                resolver.CollectAndApplyPrefab();
            }
        }

        static string GetHierarchyPathWithSibling(Transform current)
        {
            string path = current.name + _siblingBracketL + GetSiblingIndexEachSameNames(current) + _siblingBracketR;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + _siblingBracketL + GetSiblingIndexEachSameNames(current) + _siblingBracketR + "/" + path;
            }
            return GetSceneElement(current.gameObject.scene) + "/" + path;
        }

        public static string GetHierarchyPath(UnityEngine.Object o)
        {
            string path = "";
            if (o is UnityEngine.Component) path = GetHierarchyPath(o as UnityEngine.Component);
            if (o is GameObject) path = GetHierarchyPath(o as GameObject);
            return path;
        }

        public static string ConvertRelativePathToAbsolute(string relativePath, string fromPath)
        {
            if (string.IsNullOrEmpty(relativePath)) return fromPath;
            if (TryGetSceneByPathRoot(relativePath.Split('/')[0], out _)) return relativePath;

            List<string> baseElements = new List<string>(fromPath.Split('/'));
            string[] relativeElements = relativePath.Split('/');

            foreach (var element in relativeElements)
            {
                if (element == "." || string.IsNullOrEmpty(element))
                {
                    continue;
                }
                if (element == "..")
                {
                    // 一つ上の階層に移動
                    if (baseElements.Count > 0)
                        baseElements.RemoveAt(baseElements.Count - 1);
                }
                else
                {
                    // 下の階層に移動
                    baseElements.Add(element);
                }
            }
            return string.Join("/", baseElements);
        }

        public static string GetAbsolutePath(string path, string fromPath)
        {
            return ConvertRelativePathToAbsolute(path, fromPath);
        }

        public static string GetRelativePath(string fromPath, string toPath)
        {

            // string fromPath = GetHierarchyPath(from);
            // string toPath = GetHierarchyPath(to);
            if (fromPath == toPath) return ".";

            // Check if 'to' is a child descendant of 'from'
            if (toPath.StartsWith(fromPath + "/"))
            {
                return toPath.Substring(fromPath.Length + 1);
            }

            // Find common root
            string[] fromSplit = fromPath.Split('/');
            string[] toSplit = toPath.Split('/');
            int index = 0;
            while (index < fromSplit.Length && index < toSplit.Length && fromSplit[index] == toSplit[index])
            {
                index++;
            }

            string relativePath = "";
            // from common root to 'from'
            for (int i = index; i < fromSplit.Length; i++)
            {
                relativePath += "../";
            }
            // from common root to 'to'
            for (int i = index; i < toSplit.Length; i++)
            {
                relativePath += toSplit[i] + (i < toSplit.Length - 1 ? "/" : "");
            }
            return relativePath;
        }

        public static GameObject FindGameObjectFromPath(string path)
        {
            string[] elements = path.Split('/');
            if (elements.Length == 0) return null;

            if (!TryGetSceneByPathRoot(elements[0], out var targetScene) || elements.Length <= 1) return null;
            int rootElementIndex = 1;

            // Rootから探索を開始
            string rootName = GetNameFromElement(elements[rootElementIndex]);
            int rootIndex = GetSiblingIndexFromElement(elements[rootElementIndex]);
            Transform current = null;
            foreach (var root in targetScene.GetRootGameObjects())
            {
                if (rootName == root.name && rootIndex == GetSiblingIndexEachSameNames(root.transform))
                {
                    current = root.transform;
                    break;
                }
            }

            if (current == null) return null; // Rootが見つからなかった場合

            // /で分割して各階層に分ける
            for (int i = rootElementIndex + 1; i < elements.Length; i++)
            {
                bool isFound = false;
                foreach (Transform child in current)
                {
                    string expectedName = GetNameFromElement(elements[i]);
                    int expectedIndex = GetSiblingIndexFromElement(elements[i]);
                    if (child.name == expectedName && GetSiblingIndexEachSameNames(child) == expectedIndex)
                    {
                        current = child;
                        isFound = true;
                        break;
                    }
                }

                if (!isFound) return null; // 途中で該当する子が見つからなかった場合
            }

            return current.gameObject;
        }

        static bool TryGetSceneByPathRoot(string pathRoot, out Scene scene)
        {
            var sceneName = GetNameFromElement(pathRoot);
            var sceneIndex = GetSiblingIndexFromElement(pathRoot);
            var scenes = AllScenes().Where(s => s.name == sceneName).ToList();
            scene = default;

            if (scenes.Count == 0)
            {
                return false;
            }

            if (sceneIndex < 0)
            {
                if (scenes.Count != 1)
                {
                    return false;
                }

                scene = scenes[0];
                return true;
            }

            if (sceneIndex >= scenes.Count)
            {
                return false;
            }

            scene = scenes[sceneIndex];
            return scene.IsValid();
        }

        static string GetSceneElement(Scene scene)
        {
            return scene.name + _siblingBracketL + GetSiblingIndexEachSameNames(scene) + _siblingBracketR;
        }

        static string GetHierarchyPath(UnityEngine.Component c)
        {
            return GetHierarchyPathWithSibling(c.transform);
        }

        static string GetHierarchyPath(GameObject o)
        {
            return GetHierarchyPathWithSibling(o.transform);
        }

        static string GetLongestRelativePath(string fromPath, string toPath)
        {
            var fromSplit = fromPath.Split('/');
            var toSplit = toPath.Split('/');
            int index = 0;

            while (index < fromSplit.Length && index < toSplit.Length && fromSplit[index] == toSplit[index])
            {
                index++;
            }

            string relativePath = "";

            // from から共通のルートまで"../"を追加
            for (int i = index; i < fromSplit.Length; i++)
            {
                relativePath += "../";
            }

            // 共通のルートから to までのパスを追加
            for (int i = index; i < toSplit.Length; i++)
            {
                relativePath += toSplit[i] + (i < toSplit.Length - 1 ? "/" : "");
            }

            return relativePath;
        }


        static string GetNameFromElement(string element)
        {
            int indexStart = element.IndexOf(_siblingBracketL);
            if (indexStart == -1) return element; // _siblingBracketL がない場合
            return element.Substring(0, indexStart);
        }

        static int GetSiblingIndexFromElement(string element)
        {
            int indexStart = element.IndexOf(_siblingBracketL);
            int indexEnd = element.IndexOf(_siblingBracketR);
            if (indexStart == -1 || indexEnd == -1) return -1; // _siblingBracketL or _siblingBracketR がない場合
            string indexStr = element.Substring(indexStart + 1, indexEnd - indexStart - 1);
            return int.Parse(indexStr);
        }


        static int GetSiblingIndexEachSameNames(in Transform target)
        {
            List<Transform> children;
            if (target.parent == null)
            {
                children = target.gameObject.scene.GetRootGameObjects().Select(o => o.transform).ToList();
            }
            else
            {
                children = target.parent.Cast<Transform>().ToList();
            }
            var targetName = target.name;
            var targetHash = target.GetHashCode();
            return children.Where(c => c.name == targetName).ToList().FindIndex(t => t.GetHashCode() == targetHash);

            // High cost implementation
            // var path = GetHierarchyPath(target);
            // var sameNames = Resources.FindObjectsOfTypeAll<Transform>().Where(t => GetHierarchyPath(t) == path).ToList();
            // var hash = target.GetHashCode();
            // return sameNames.FindIndex(t => t.GetHashCode() == hash);
        }

        static int GetSiblingIndexEachSameNames(Scene target)
        {
            var scenes = AllScenes().Where(s => s.name == target.name).ToList();
            return scenes.FindIndex(s => s.handle == target.handle);
        }
    }
}
#endif
