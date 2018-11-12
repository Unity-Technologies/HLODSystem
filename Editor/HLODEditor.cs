using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLOD))]
    public class HLODEditor : Editor
    {
        private SerializedProperty m_RecursiveGenerationProperty;
        private SerializedProperty m_MinSizeProperty;
        private SerializedProperty m_LODDistanceProperty;
        private SerializedProperty m_CullDistanceProperty;

        private LODSlider m_LODSlider;

        void OnEnable()
        {
            m_RecursiveGenerationProperty = serializedObject.FindProperty("m_RecursiveGeneration");
            m_MinSizeProperty = serializedObject.FindProperty("m_MinSize");
            m_LODDistanceProperty = serializedObject.FindProperty("m_LODDistance");
            m_CullDistanceProperty = serializedObject.FindProperty("m_CullDistance");

            m_LODSlider = new LODSlider(true, "Cull");
            m_LODSlider.InsertRange("High", m_LODDistanceProperty);
            m_LODSlider.InsertRange("Low", m_CullDistanceProperty);
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_RecursiveGenerationProperty);
            if ( m_RecursiveGenerationProperty.boolValue )
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(m_MinSizeProperty);
                EditorGUI.indentLevel -= 1;
            }

            m_LODSlider.Draw();

            HLOD hlod = target as HLOD;
            if (hlod == null)
                GUI.enabled = false;

            if (GUILayout.Button("Generate"))
            {
                HLODCreator.Create(hlod);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Enable All"))
            {
                hlod.EnableAll();
            }

            if (GUILayout.Button("Disable All"))
            {
                hlod.DisableAll();
            }

            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();
        }
    }

}