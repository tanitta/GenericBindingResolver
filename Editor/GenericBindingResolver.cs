#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace trit.GenericBindingResolver{

    [ExecuteInEditMode]
    public class GenericBindingResolver : MonoBehaviour, IResolver
    {

        public Component _targetComponent;

        [SerializeField]
        Transform _proxyTransform;

        // Matching Options
        [HideInInspector]
        [SerializeField]
        public bool _useParmNameToComparing = false;

        public List<GenericBinding> _genericBindings = new List<GenericBinding>();

        [HideInInspector]
        [SerializeField]
        public GameObject _applyTargetPrefab;

        [ContextMenu("GBR/Collect")]
        public void Collect(){
            var result = new List<GenericBinding>();
            if (_targetComponent == null)return;
            var resolverPath = SceneUtils.GetHierarchyPath(gameObject);

            var so = new SerializedObject(_targetComponent);
            var it = so.GetIterator();

            bool enterChildren = true;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (it.propertyPath == "m_Script") continue;
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;

                var obj = it.objectReferenceValue;
                if (obj == null) continue;

                if (!(obj is GameObject) && !(obj is Component)) continue;

                var binding = new GenericBinding
                {
                    propertyPath = it.propertyPath,
                    hierarchyPath = SceneUtils.GetRelativePath(resolverPath, SceneUtils.GetHierarchyPath(obj)),
                    refKind = obj is Component ? ObjectRefKind.Component : ObjectRefKind.GameObject,
                    componentTypeAQN = (obj is Component) ? obj.GetType().AssemblyQualifiedName : null,
                    componentIndexSameType = (obj is Component c) ? SceneUtils.GetComponentIndexSameType(c) : -1,
                };

                result.Add(binding);
            }
            _genericBindings = result;
        }

        [ContextMenu("GBR/CollectAndApplyPrefab")]
        public void CollectAndApplyPrefab(){
            Collect();
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            ApplyPrefab(_applyTargetPrefab);
        }

        public void ApplyPrefab(GameObject applyTargetPrefab){
            var assetPath = applyTargetPrefab==null?UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this):AssetDatabase.GetAssetPath(applyTargetPrefab);
            UnityEditor.PrefabUtility.ApplyObjectOverride(this, assetPath, InteractionMode.UserAction);
        }

        [ContextMenu("GBR/Apply")]
        public void Apply(){
            var genericBindings = _genericBindings.OrderBy(b => b.propertyPath).ToList();
            var resolverPath = SceneUtils.GetHierarchyPath(gameObject);

            foreach(var binding in genericBindings){
                var so = new SerializedObject(_targetComponent);
                var prop = so.FindProperty(binding.propertyPath);
                if (prop == null) continue;

                var targetPath = SceneUtils.GetAbsolutePath(binding.hierarchyPath, resolverPath);
                var targetGO = SceneUtils.FindGameObjectFromPath(targetPath);
                if (targetGO == null) continue;

                UnityEngine.Object targetObj = null;
                if (binding.refKind == ObjectRefKind.GameObject)
                {
                    targetObj = targetGO;
                }
                else if (binding.refKind == ObjectRefKind.Component)
                {
                    var compType = System.Type.GetType(binding.componentTypeAQN);
                    if (compType == null) continue;
                    var comps = targetGO.GetComponents(compType);
                    if (binding.componentIndexSameType < 0 || binding.componentIndexSameType >= comps.Length) continue;
                    targetObj = comps[binding.componentIndexSameType];
                }

                if (targetObj == null) continue;

                prop.objectReferenceValue = targetObj;
                so.ApplyModifiedProperties();
            }
        }

        [InitializeOnLoadMethod]
        static void GenericResolverEventRegistar(){
            ResolverEventRegistrar<GenericBindingResolver>.RegisterEvents();
        }
    }

    public enum ObjectRefKind
    {
        GameObject,
        Component
    }
    [System.Serializable]

    public struct GenericBinding
    {
        public string propertyPath;
        public string hierarchyPath;
        public ObjectRefKind refKind;
        public string componentTypeAQN;
        public int componentIndexSameType;
    };
}
#endif
