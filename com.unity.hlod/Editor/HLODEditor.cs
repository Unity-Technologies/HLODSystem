using System;
using System.Linq;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLOD))]
    public class HLODEditor : Editor
    {
        public static class Styles
        {
            public static GUIContent GenerateButtonEnable = new GUIContent("Generate", "Generate HLOD mesh.");
            public static GUIContent GenerateButtonExists = new GUIContent("Generate", "HLOD already generated.");
            public static GUIContent DestroyButtonEnable = new GUIContent("Destroy", "Destroy HLOD mesh.");
            public static GUIContent DestroyButtonNotExists = new GUIContent("Destroy", "HLOD must be created before the destroying.");

            public static GUIStyle RedTextColor = new GUIStyle();

            static Styles()
            {
                RedTextColor.normal.textColor = Color.red;
            }

        }        
        private SerializedProperty m_ChunkSizeProperty;
        private SerializedProperty m_LODDistanceProperty;
        private SerializedProperty m_CullDistanceProperty;
        private SerializedProperty m_MinObjectSizeProperty;

        private LODSlider m_LODSlider;

        private Type[] m_BatcherTypes;
        private string[] m_BatcherNames;

        private Type[] m_SimplifierTypes;
        private string[] m_SimplifierNames;

        private Type[] m_StreamingTypes;
        private string[] m_StreamingNames;

        private Type[] m_UserDataSerializerTypes;
        private string[] m_UserDataSerializerNames;

        private bool isShowCommon = true;
        private bool isShowBatcher = true;
        private bool isShowSimplifier = true;
        private bool isShowStreaming = true;
        private bool isShowUserDataSerializer = true;

        private bool isFirstOnGUI = true;
        
        private ISpaceSplitter m_splitter = new QuadTreeSpaceSplitter(5.0f);

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
            m_ChunkSizeProperty = serializedObject.FindProperty("m_ChunkSize");
            m_LODDistanceProperty = serializedObject.FindProperty("m_LODDistance");
            m_CullDistanceProperty = serializedObject.FindProperty("m_CullDistance");
            m_MinObjectSizeProperty = serializedObject.FindProperty("m_MinObjectSize");

            m_LODSlider = new LODSlider(true, "Cull");
            m_LODSlider.InsertRange("High", m_LODDistanceProperty);
            m_LODSlider.InsertRange("Low", m_CullDistanceProperty);

            m_BatcherTypes = BatcherTypes.GetTypes();
            m_BatcherNames = m_BatcherTypes.Select(t => t.Name).ToArray();

            m_SimplifierTypes = Simplifier.SimplifierTypes.GetTypes();
            m_SimplifierNames = m_SimplifierTypes.Select(t => t.Name).ToArray();

            m_StreamingTypes = Streaming.StreamingBuilderTypes.GetTypes();
            m_StreamingNames = m_StreamingTypes.Select(t => t.Name).ToArray();

            m_UserDataSerializerTypes = Serializer.UserDataSerializerTypes.GetTypes();
            m_UserDataSerializerNames = m_UserDataSerializerTypes.Select(t => t.Name).ToArray();

            isFirstOnGUI = true;
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
                EditorGUILayout.PropertyField(m_ChunkSizeProperty);

                m_ChunkSizeProperty.floatValue = HLODUtils.GetChunkSizePropertyValue(m_ChunkSizeProperty.floatValue);
                
                var bounds = hlod.GetBounds();
                int depth = m_splitter.CalculateTreeDepth(bounds, m_ChunkSizeProperty.floatValue);
                
                EditorGUILayout.LabelField($"The HLOD tree will be created with {depth} levels.");
                if (depth > 5)
                {
                    EditorGUILayout.LabelField($"Node Level Count greater than 5 may cause a frozen Editor.", Styles.RedTextColor);
                    EditorGUILayout.LabelField($"I recommend keeping the level under 5.", Styles.RedTextColor);
                    
                }


                m_LODSlider.Draw();
                EditorGUILayout.PropertyField(m_MinObjectSizeProperty);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            isShowSimplifier = EditorGUILayout.BeginFoldoutHeaderGroup(isShowSimplifier, "Simplifier");
            if (isShowSimplifier == true)
            {
                EditorGUI.indentLevel += 1;
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
                            info.Invoke(null, new object[] {hlod.SimplifierOptions});
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Cannot find Simplifiers.");
                }
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            isShowBatcher = EditorGUILayout.BeginFoldoutHeaderGroup(isShowBatcher, "Batcher");
            if (isShowBatcher == true)
            {
                EditorGUI.indentLevel += 1;
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
                            info.Invoke(null, new object[] {hlod, isFirstOnGUI });
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Cannot find Batchers.");
                }
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            

            isShowStreaming = EditorGUILayout.BeginFoldoutHeaderGroup(isShowStreaming, "Streaming");
            if (isShowStreaming == true)
            {
                EditorGUI.indentLevel += 1;
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
                            info.Invoke(null, new object[] { hlod.StreamingOptions });
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Cannot find StreamingSetters.");
                }
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            
            isShowUserDataSerializer =
                EditorGUILayout.BeginFoldoutHeaderGroup(isShowUserDataSerializer, "UserData serializer");
            if (isShowUserDataSerializer)
            {
                EditorGUI.indentLevel += 1;
                if (m_UserDataSerializerTypes.Length > 0)
                {
                    int serializerIndex =
                        Math.Max(Array.IndexOf(m_UserDataSerializerTypes, hlod.UserDataSerializerType), 0);
                    serializerIndex =
                        EditorGUILayout.Popup("UserDataSerializer", serializerIndex, m_UserDataSerializerNames);
                    hlod.UserDataSerializerType = m_UserDataSerializerTypes[serializerIndex];
                }
                else
                {
                    EditorGUILayout.LabelField("Cannot find UserDataSerializer.");
                }
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            GUIContent generateButton = Styles.GenerateButtonEnable;
            GUIContent destroyButton = Styles.DestroyButtonNotExists;

            if (hlod.GetComponent<Streaming.HLODControllerBase>() != null)
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
            isFirstOnGUI = false;
        }

    }

}