using System;
using System.Linq;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLOD))]
    public class HLODEditor : Editor
    {
        public static class Styles
        {
            public static GUIContent GenerateButtonEnable = new GUIContent("Generate", "Generate a HLOD mesh.");
            public static GUIContent GenerateButtonDisable = new GUIContent("Generate", "Generate is only allow in prefab mode.");
            public static GUIContent GenerateButtonExists = new GUIContent("Generate", "HLOD already generated.");
            public static GUIContent DestroyButtonEnable = new GUIContent("Destroy", "Destory a HLOD mesh.");
            public static GUIContent DestroyButtonDisable = new GUIContent("Destroy", "Destory is only allow in prefab mode.");
            public static GUIContent DestroyButtonNotExists = new GUIContent("Destroy", "You need to generate HLOD before the destroy.");
        }        
        private SerializedProperty m_MinSizeProperty;
        private SerializedProperty m_LODDistanceProperty;
        private SerializedProperty m_CullDistanceProperty;
        private SerializedProperty m_ThresholdSizeProperty;

        private LODSlider m_LODSlider;

        private Type[] m_BatcherTypes;
        private string[] m_BatcherNames;

        private Type[] m_SimplifierTypes;
        private string[] m_SimplifierNames;

        private Type[] m_StreamingTypes;
        private string[] m_StreamingNames;

        private bool isShowCommon = true;
        private bool isShowBatcher = true;
        private bool isShowSimplifier = true;
        private bool isShowStreaming = true;

        [InitializeOnLoadMethod]
        static void InitTagTagUtils()
        {
            if (LayerMask.NameToLayer(HLOD.HLODLayerStr) == -1)
            {
                TagUtils.AddLayer(HLOD.HLODLayerStr);
                Tools.lockedLayers |= 1 << LayerMask.NameToLayer(HLOD.HLODLayerStr);
            }
        }

        void OnEnable()
        {            
            m_MinSizeProperty = serializedObject.FindProperty("m_MinSize");
            m_LODDistanceProperty = serializedObject.FindProperty("m_LODDistance");
            m_CullDistanceProperty = serializedObject.FindProperty("m_CullDistance");
            m_ThresholdSizeProperty = serializedObject.FindProperty("m_ThresholdSize");

            m_LODSlider = new LODSlider(true, "Cull");
            m_LODSlider.InsertRange("High", m_LODDistanceProperty);
            m_LODSlider.InsertRange("Low", m_CullDistanceProperty);

            m_BatcherTypes = BatcherTypes.GetTypes();
            m_BatcherNames = m_BatcherTypes.Select(t => t.Name).ToArray();

            m_SimplifierTypes = Simplifier.SimplifierTypes.GetTypes();
            m_SimplifierNames = m_SimplifierTypes.Select(t => t.Name).ToArray();

            m_StreamingTypes = Streaming.StreamingBuilderTypes.GetTypes();
            m_StreamingNames = m_StreamingTypes.Select(t => t.Name).ToArray();
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

            isShowCommon = EditorGUILayout.BeginFoldoutHeaderGroup(isShowCommon, "Common");
            if (isShowCommon == true)
            {

                EditorGUILayout.PropertyField(m_MinSizeProperty);

                m_LODSlider.Draw();
                EditorGUILayout.PropertyField(m_ThresholdSizeProperty);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            isShowSimplifier = EditorGUILayout.BeginFoldoutHeaderGroup(isShowSimplifier, "Simplifier");
            if (isShowSimplifier == true)
            {
                if (m_SimplifierTypes.Length > 0)
                {
                    int simplifierIndex = Math.Max(Array.IndexOf(m_SimplifierTypes, hlod.SimplifierType), 0);
                    simplifierIndex = EditorGUILayout.Popup("Simplifier", simplifierIndex, m_SimplifierNames);
                    hlod.SimplifierType = m_SimplifierTypes[simplifierIndex];

                    var info = m_SimplifierTypes[simplifierIndex].GetMethod("OnGUI");
                    if (info != null)
                    {
                        if (info.IsStatic == true)
                        {
                            info.Invoke(null, new object[] {hlod});
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Can not find Simplifiers.");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            isShowBatcher = EditorGUILayout.BeginFoldoutHeaderGroup(isShowBatcher, "Batcher");
            if (isShowBatcher == true)
            {
                if (m_BatcherTypes.Length > 0)
                {
                    int batcherIndex = Math.Max(Array.IndexOf(m_BatcherTypes, hlod.BatcherType), 0);
                    batcherIndex = EditorGUILayout.Popup("Batcher", batcherIndex, m_BatcherNames);
                    hlod.BatcherType = m_BatcherTypes[batcherIndex];

                    var info = m_BatcherTypes[batcherIndex].GetMethod("OnGUI");
                    if (info != null)
                    {
                        if (info.IsStatic == true)
                        {
                            info.Invoke(null, new object[] {hlod});
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Can not find Batchers.");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();



            isShowStreaming = EditorGUILayout.BeginFoldoutHeaderGroup(isShowStreaming, "Streaming");
            if (isShowStreaming == true)
            {
                if (m_StreamingTypes.Length > 0)
                {
                    int streamingIndex = Math.Max(Array.IndexOf(m_StreamingTypes, hlod.StreamingType), 0);
                    streamingIndex = EditorGUILayout.Popup("Streaming", streamingIndex, m_StreamingNames);
                    hlod.StreamingType = m_StreamingTypes[streamingIndex];

                    var info = m_StreamingTypes[streamingIndex].GetMethod("OnGUI");
                    if (info != null)
                    {
                        if (info.IsStatic == true)
                        {
                            info.Invoke(null, new object[] { hlod });
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Can not find StreamingSetters.");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            GUIContent generateButton = Styles.GenerateButtonEnable;
            GUIContent destroyButton = Styles.DestroyButtonNotExists;

            if (PrefabStageUtility.GetCurrentPrefabStage() == null ||
                PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot == null)
            {
                //generate is only allow in prefab mode.
                GUI.enabled = false;
                generateButton = Styles.GenerateButtonDisable;
                destroyButton = Styles.DestroyButtonDisable;
            }
            else if (hlod.GetComponent<ControllerBase>() != null)
            {
                generateButton = Styles.GenerateButtonExists;
                destroyButton = Styles.DestroyButtonEnable;
            }



            EditorGUILayout.Space();

            GUI.enabled = generateButton == Styles.GenerateButtonEnable;
            if (GUILayout.Button(generateButton))
            {
                CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
            }

            GUI.enabled = destroyButton == Styles.DestroyButtonEnable;
            if (GUILayout.Button(destroyButton))
            {
                CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod));
            }

            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    EditorSceneManager.MarkSceneDirty(PrefabStageUtility.GetCurrentPrefabStage().scene);
                }
            }
        }

    }

}