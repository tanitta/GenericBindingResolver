#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace trit.GenericBindingResolver.Tests
{
    public class GenericBindingResolverTests
    {
        const string TempSceneDirectory = "Assets/__GenericBindingResolverTests";
        readonly List<Scene> _createdScenes = new List<Scene>();
        readonly List<string> _createdScenePaths = new List<string>();

        [SetUp]
        public void SetUp()
        {
            EnsureTempSceneDirectoryExists();
            CreateAndSaveScene("TestRoot", NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var scenePath in _createdScenePaths)
            {
                AssetDatabase.DeleteAsset(scenePath);
            }

            _createdScenes.Clear();
            _createdScenePaths.Clear();
            AssetDatabase.Refresh();
        }

        [Test]
        public void GetRelativePath_InsideSameScene_DoesNotIncludeSceneName()
        {
            var scene = CreateScene("ResolverScene");
            var resolver = CreateGameObject(scene, "Resolver");
            var child = CreateGameObject(scene, "Child", resolver.transform);

            var resolverPath = SceneUtils.GetHierarchyPath(resolver);
            var childPath = SceneUtils.GetHierarchyPath(child);

            var relativePath = SceneUtils.GetRelativePath(resolverPath, childPath);

            Assert.That(relativePath, Is.EqualTo("Child〈0〉"));
            Assert.That(relativePath, Does.Not.Contain(scene.name));
            Assert.That(SceneUtils.GetAbsolutePath(relativePath, resolverPath), Is.EqualTo(childPath));
        }

        [Test]
        public void GetRelativePath_AcrossScenes_IncludesTargetSceneName()
        {
            var resolverScene = CreateScene("ResolverScene");
            var targetScene = CreateScene("TargetScene");
            var resolver = CreateGameObject(resolverScene, "Resolver");
            var target = CreateGameObject(targetScene, "Target");

            var resolverPath = SceneUtils.GetHierarchyPath(resolver);
            var targetPath = SceneUtils.GetHierarchyPath(target);

            var relativePath = SceneUtils.GetRelativePath(resolverPath, targetPath);

            Assert.That(relativePath, Does.Contain(targetScene.name + "〈0〉/"));
            Assert.That(SceneUtils.GetAbsolutePath(relativePath, resolverPath), Is.EqualTo(targetPath));
        }

        [Test]
        public void CollectAndApply_UsesResolverRelativePaths_ForGameObjectAndComponent()
        {
            var resolverScene = CreateScene("ResolverScene");
            var targetScene = CreateScene("TargetScene");

            var resolverGameObject = CreateGameObject(resolverScene, "Resolver");
            var localTarget = CreateGameObject(resolverScene, "LocalTarget", resolverGameObject.transform);
            var remoteTarget = CreateGameObject(targetScene, "RemoteTarget");
            var remoteTransform = remoteTarget.transform;

            var resolver = resolverGameObject.AddComponent<GenericBindingResolver>();
            var bindingTarget = resolverGameObject.AddComponent<TestBindingTargetComponent>();
            resolver._targetComponent = bindingTarget;

            bindingTarget.selfGameObject = resolverGameObject;
            bindingTarget.localGameObject = localTarget;
            bindingTarget.remoteTransform = remoteTransform;

            resolver.Collect();

            Assert.That(resolver._genericBindings, Has.Count.EqualTo(3));
            Assert.That(GetBinding(resolver, nameof(TestBindingTargetComponent.selfGameObject)).hierarchyPath, Is.EqualTo("."));
            Assert.That(GetBinding(resolver, nameof(TestBindingTargetComponent.localGameObject)).hierarchyPath, Is.EqualTo("LocalTarget〈0〉"));

            var remoteBinding = GetBinding(resolver, nameof(TestBindingTargetComponent.remoteTransform));
            Assert.That(remoteBinding.refKind, Is.EqualTo(ObjectRefKind.Component));
            Assert.That(remoteBinding.hierarchyPath, Does.Contain(targetScene.name + "〈0〉/"));

            bindingTarget.selfGameObject = null;
            bindingTarget.localGameObject = null;
            bindingTarget.remoteTransform = null;

            resolver.Apply();

            Assert.That(bindingTarget.selfGameObject, Is.SameAs(resolverGameObject));
            Assert.That(bindingTarget.localGameObject, Is.SameAs(localTarget));
            Assert.That(bindingTarget.remoteTransform, Is.SameAs(remoteTransform));
        }

        [Test]
        public void GetRelativePath_AcrossDuplicateNamedScenes_IncludesSceneSiblingIndex()
        {
            var resolverScene = CreateScene("ResolverScene");
            var duplicateSceneA = CreateSceneWithFileName("DuplicateScene", "A");
            var duplicateSceneB = CreateSceneWithFileName("DuplicateScene", "B");
            var resolver = CreateGameObject(resolverScene, "Resolver");
            var targetA = CreateGameObject(duplicateSceneA, "Target");
            var targetB = CreateGameObject(duplicateSceneB, "Target");

            var resolverPath = SceneUtils.GetHierarchyPath(resolver);
            var targetPathA = SceneUtils.GetHierarchyPath(targetA);
            var targetPathB = SceneUtils.GetHierarchyPath(targetB);

            var relativePathA = SceneUtils.GetRelativePath(resolverPath, targetPathA);
            var relativePathB = SceneUtils.GetRelativePath(resolverPath, targetPathB);

            Assert.That(relativePathA, Does.Contain("DuplicateScene〈0〉/"));
            Assert.That(relativePathB, Does.Contain("DuplicateScene〈1〉/"));
            Assert.That(SceneUtils.GetAbsolutePath(relativePathA, resolverPath), Is.EqualTo(targetPathA));
            Assert.That(SceneUtils.GetAbsolutePath(relativePathB, resolverPath), Is.EqualTo(targetPathB));
            Assert.That(SceneUtils.FindGameObjectFromPath(SceneUtils.GetAbsolutePath(relativePathB, resolverPath)), Is.SameAs(targetB));
        }

        [Test]
        public void CollectAndApply_UsesSceneSiblingIndex_ForDuplicateNamedScenes()
        {
            var resolverScene = CreateScene("ResolverScene");
            CreateSceneWithFileName("DuplicateScene", "A");
            var duplicateSceneB = CreateSceneWithFileName("DuplicateScene", "B");

            var resolverGameObject = CreateGameObject(resolverScene, "Resolver");
            var remoteTarget = CreateGameObject(duplicateSceneB, "RemoteTarget");
            var remoteTransform = remoteTarget.transform;

            var resolver = resolverGameObject.AddComponent<GenericBindingResolver>();
            var bindingTarget = resolverGameObject.AddComponent<TestBindingTargetComponent>();
            resolver._targetComponent = bindingTarget;
            bindingTarget.remoteTransform = remoteTransform;

            resolver.Collect();

            var remoteBinding = GetBinding(resolver, nameof(TestBindingTargetComponent.remoteTransform));
            Assert.That(remoteBinding.hierarchyPath, Does.Contain("DuplicateScene〈1〉/"));

            bindingTarget.remoteTransform = null;
            resolver.Apply();

            Assert.That(bindingTarget.remoteTransform, Is.SameAs(remoteTransform));
        }

        static GenericBinding GetBinding(GenericBindingResolver resolver, string propertyPath)
        {
            return resolver._genericBindings.Single(b => b.propertyPath == propertyPath);
        }

        Scene CreateScene(string prefix)
        {
            return CreateSceneWithFileName(prefix + "_" + Guid.NewGuid().ToString("N"), null);
        }

        Scene CreateSceneWithFileName(string fileName, string subDirectory)
        {
            return CreateAndSaveScene(fileName, NewSceneMode.Additive, subDirectory);
        }

        Scene CreateAndSaveScene(string fileName, NewSceneMode mode, string subDirectory = null)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            var directoryPath = string.IsNullOrEmpty(subDirectory) ? TempSceneDirectory : $"{TempSceneDirectory}/{subDirectory}";
            EnsureTempSceneDirectoryExists(directoryPath);
            var scenePath = $"{directoryPath}/{fileName}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            _createdScenes.Add(scene);
            _createdScenePaths.Add(scenePath);
            return scene;
        }

        static void EnsureTempSceneDirectoryExists(string directoryPath = TempSceneDirectory)
        {
            if (AssetDatabase.IsValidFolder(directoryPath))
            {
                return;
            }

            var split = directoryPath.Split('/');
            var current = split[0];
            for (int i = 1; i < split.Length; i++)
            {
                var next = $"{current}/{split[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, split[i]);
                }

                current = next;
            }
        }

        static GameObject CreateGameObject(Scene scene, string name, Transform parent = null)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            return gameObject;
        }
    }

    public class TestBindingTargetComponent : MonoBehaviour
    {
        public GameObject selfGameObject;
        public GameObject localGameObject;
        public Transform remoteTransform;
    }
}
#endif
