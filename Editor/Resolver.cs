#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;

namespace trit.GenericBindingResolver{
    public interface IResolver{
        public void Collect();
        public void CollectAndApplyPrefab();
        public void Apply();
    }

    public static class ResolverEventRegistrar<T> where T: IResolver{
        public static void RegisterEvents(){
            // [Unityエディタでプロジェクトを\(初回\)起動した時の判定【Unity】【エディタ拡張】 \- \(:3\[kanのメモ帳\]](https://kan-kikuchi.hatenablog.com/entry/Editor_Startup_Confirmer)
            bool shouldRegisteredEventOnStartupOnly = false; // For Debug
            var lockFirePrefix = "Temp/ResolverRunningLockfile";
            var lockfilePath = lockFirePrefix + typeof(T).ToString();
            if (shouldRegisteredEventOnStartupOnly){
                bool onStartup = !File.Exists(lockfilePath);
                if (!onStartup && shouldRegisteredEventOnStartupOnly) return;
                File.Create(lockfilePath);
            }
            // [Bug \- EditorSceneManager\.sceneOpened not called on Editor startup\. \- Unity Forum](https://forum.unity.com/threads/editorscenemanager-sceneopened-not-called-on-editor-startup.1259672/)
            EditorApplication.delayCall += ApplyResolverDelayCall;
            // [Unity \- Scripting API: SceneManagement\.EditorSceneManager\.sceneOpened](https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager-sceneOpened.html)
            EditorSceneManager.sceneOpened += ApplyResolverOnSceneLoadedCallback;
            Debug.Log("["+ResolverName()+"] Applied all bindings on scene loaded.");
        }

        static string ResolverName(){
            return typeof(T).ToString();
        }

        static void ApplyResolverDelayCall(){
            ApplyResolverOnSceneLoadedCallback(EditorSceneManager.GetActiveScene(),OpenSceneMode.Single);
            EditorApplication.delayCall -= ApplyResolverDelayCall; // Call on startup scene loading only. ignore re-calling on script compiled.
            var resolverName = typeof(T).ToString();
            Debug.Log("["+ResolverName()+"] Call Delay");
        }

        static void ApplyResolverOnSceneLoadedCallback(Scene scene, OpenSceneMode mode){
            var scenes = SceneUtils.AllScenes();
            foreach(var s in scenes){
                SceneUtils.GetResolvers<T>(s).ToList().ForEach(r => r.Apply());
            }
        }
    }
}
#endif
