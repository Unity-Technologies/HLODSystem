using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
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

        private System.Type[] m_BatcherTypes;
        private string[] m_BatcherNames;

        void OnEnable()
        {
            m_RecursiveGenerationProperty = serializedObject.FindProperty("m_RecursiveGeneration");
            m_MinSizeProperty = serializedObject.FindProperty("m_MinSize");
            m_LODDistanceProperty = serializedObject.FindProperty("m_LODDistance");
            m_CullDistanceProperty = serializedObject.FindProperty("m_CullDistance");

            m_LODSlider = new LODSlider(true, "Cull");
            m_LODSlider.InsertRange("High", m_LODDistanceProperty);
            m_LODSlider.InsertRange("Low", m_CullDistanceProperty);

            m_BatcherTypes = BatcherTypes.GetTypes();
            m_BatcherNames = m_BatcherTypes.Select(t => t.Name).ToArray();

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            HLOD hlod = target as HLOD;
            if (hlod == null)
            {
                EditorGUILayout.LabelField("HLOD is null.");
                return;
            }

            EditorGUILayout.PropertyField(m_RecursiveGenerationProperty);
            if ( m_RecursiveGenerationProperty.boolValue )
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(m_MinSizeProperty);
                EditorGUI.indentLevel -= 1;
            }

            m_LODSlider.Draw();

            int batcherIndex = Math.Max(Array.IndexOf(m_BatcherTypes, hlod.BatcherType), 0);
            batcherIndex = EditorGUILayout.Popup("Batcher", batcherIndex, m_BatcherNames);
            hlod.BatcherType = m_BatcherTypes[batcherIndex];

            var info = m_BatcherTypes[batcherIndex].GetMethod("OnGUI");
            if (info != null)
            {
                if (info.IsStatic == true)
                {
                    info.Invoke(null, new object[] { hlod });
                }
            }


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
            
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                EditorSceneManager.MarkSceneDirty(PrefabStageUtility.GetCurrentPrefabStage().scene);
            }
        }
    }

}