#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace trit.GenericBindingResolver{
    [CustomEditor(typeof(GenericBindingResolver))]
    class GenericBindingResolverEditor: Editor{
        SerializedProperty _script;
        bool _showFoldoutHeaderOptions = false;
        SerializedProperty _applyTargetPrefab;

        void OnEnable()
        {
            _script = serializedObject.FindProperty("m_Script");
            _applyTargetPrefab  = serializedObject.FindProperty("_applyTargetPrefab");
        }

        public override void OnInspectorGUI(){
            var resolver = (GenericBindingResolver)target;
            if(GUILayout.Button("Collect Bindings And Apply Prefab",GUILayout.Width(240))){
                Undo.RecordObject(resolver, "Collect And Apply Prefab Changes");
                resolver.CollectAndApplyPrefab();
                EditorUtility.SetDirty(resolver);
            };
            if(GUILayout.Button("Collect Bindings",GUILayout.Width(120))){
                Undo.RecordObject(resolver, "Collect Changes");
                resolver.Collect();
                EditorUtility.SetDirty(resolver);
            };
            GUILayout.Space(10);

            OnInspectorGUIOptions();

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }

        public void OnInspectorGUIOptions(){
            _showFoldoutHeaderOptions = EditorGUILayout.BeginFoldoutHeaderGroup(_showFoldoutHeaderOptions, "Options");
            if (_showFoldoutHeaderOptions)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_applyTargetPrefab);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
#endif
