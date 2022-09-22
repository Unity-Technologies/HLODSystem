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
            public static GUIStyle BlueTextColor = new GUIStyle();

            static Styles()
            {
                RedTextColor.normal.textColor = Color.red;
                BlueTextColor.normal.textColor = new Color(0.4f, 0.5f, 1.0f);
            }

        }        
        private SerializedProperty m_ChunkSizeProperty;
        private SerializedProperty m_LODDistanceProperty;
        private SerializedProperty m_CullDistanceProperty;
        private SerializedProperty m_MinObjectSizeProperty;

        private LODSlider m_LODSlider;

        private Type[] m_SpaceSplitterTypes;
        private string[] m_SpaceSplitterNames;

        private Type[] m_BatcherTypes;
        private string[] m_BatcherNames;

        private Type[] m_SimplifierTypes;
        private string[] m_SimplifierNames;

        private Type[] m_StreamingTypes;
        private string[] m_StreamingNames;

        private Type[] m_UserDataSerializerTypes;
        private string[] m_UserDataSerializerNames;

        private bool isShowCommon = true;
        private bool isShowSpaceSplitter = true;
        private bool isShowBatcher = true;
        private bool isShowSimplifier = true;
        private bool isShowStreaming = true;
        private bool isShowUserDataSerializer = true;

        private bool isFirstOnGUI = true;
        
        private ISpaceSplitter m_splitter;

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

            m_SpaceSplitterTypes = SpaceManager.SpaceSplitterTypes.GetTypes();
            m_SpaceSplitterNames = m_SpaceSplitterTypes.Select(t => t.Name).ToArray();

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
            if (m_splitter == null)
            {
                m_splitter = SpaceSplitterTypes.CreateInstance(hlod);
            }

            isShowCommon = EditorGUILayout.BeginFoldoutHeaderGroup(isShowCommon, "Common");
            if (isShowCommon == true)
            {
                EditorGUILayout.PropertyField(m_ChunkSizeProperty);

                m_ChunkSizeProperty.floatValue = HLODUtils.GetChunkSizePropertyValue(m_ChunkSizeProperty.floatValue);

                if (m_splitter != null)
                {
                    var bounds = hlod.GetBounds();
                    int depth = m_splitter.CalculateTreeDepth(bounds, m_ChunkSizeProperty.floatValue);

                    EditorGUILayout.LabelField($"The HLOD tree will be created with {depth} levels.", Styles.BlueTextColor);
                    if (depth > 5)
                    {
                        EditorGUILayout.LabelField($"Node Level Count greater than 5 may cause a frozen Editor.",
                            Styles.RedTextColor);
                        EditorGUILayout.LabelField($"I recommend keeping the level under 5.", Styles.RedTextColor);

                    }
                }

                m_LODSlider.Draw();
                EditorGUILayout.PropertyField(m_MinObjectSizeProperty);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            isShowSpaceSplitter = EditorGUILayout.BeginFoldoutHeaderGroup(isShowSpaceSplitter, "SpaceSplitter");
            if (isShowSpaceSplitter)
            {
                EditorGUI.indentLevel += 1;
                if (m_SpaceSplitterTypes.Length > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    
                    int spaceSplitterIndex = Math.Max(Array.IndexOf(m_SpaceSplitterTypes, hlod.SpaceSplitterType), 0);
                    spaceSplitterIndex = EditorGUILayout.Popup("SpaceSplitter", spaceSplitterIndex, m_SpaceSplitterNames);
                    hlod.SpaceSplitterType = m_SpaceSplitterTypes[spaceSplitterIndex];

                    var info = m_SpaceSplitterTypes[spaceSplitterIndex].GetMethod("OnGUI");
                    if (info != null)
                    {
                        if ( info.IsStatic == true )
                        {
                            info.Invoke(null, new object[] { hlod.SpaceSplitterOptions });
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_splitter = SpaceSplitterTypes.CreateInstance(hlod);
                    }

                    if (m_splitter != null)
                    {
                        int subTreeCount = m_splitter.CalculateSubTreeCount(hlod.GetBounds());
                        EditorGUILayout.LabelField($"The HLOD tree will be created with {subTreeCount} sub trees.",
                            Styles.BlueTextColor);
                    }

                }
                else
                {
                    EditorGUILayout.LabelField("Cannot find SpaceSplitters.");
                }
                EditorGUI.indentLevel -= 1;
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

            if (hlod.GeneratedObjects.Count > 0 )
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
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(hlod);
            }

            GUI.enabled = true;

            
            serializedObject.ApplyModifiedProperties();
            isFirstOnGUI = false;
        }

    }

}