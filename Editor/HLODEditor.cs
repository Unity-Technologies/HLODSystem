using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLOD))]
    public class HLODEditor : Editor
    {
        private SerializedProperty m_LODDistanceProperty;
        private SerializedProperty m_CullDistanceProperty;

        private LODSlider m_LODSlider;

        void OnEnable()
        {
            m_LODDistanceProperty = serializedObject.FindProperty("m_LODDistance");
            m_CullDistanceProperty = serializedObject.FindProperty("m_CullDistance");

            m_LODSlider = new LODSlider(true, "Cull");
            m_LODSlider.InsertRange("High", m_LODDistanceProperty);
            m_LODSlider.InsertRange("Low", m_CullDistanceProperty);
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_LODSlider.Draw();

            if (GUILayout.Button("Generate"))
            {
                MaterialPreservingBatcher batcher = new MaterialPreservingBatcher();
                batcher.Batch(((HLOD) target).LowRoot);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}