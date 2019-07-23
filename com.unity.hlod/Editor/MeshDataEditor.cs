using System;
using UnityEditor;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(MeshDataRenderer))]
    public class MeshDataRendererEditor : Editor
    {
        private SerializedProperty m_dataProperty;
        private void OnEnable()
        {
            m_dataProperty = serializedObject.FindProperty("m_data");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_dataProperty);
            
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                MeshDataRenderer data = serializedObject.targetObject as MeshDataRenderer;
                data.UpdateMesh();
            }
        }
    }
}