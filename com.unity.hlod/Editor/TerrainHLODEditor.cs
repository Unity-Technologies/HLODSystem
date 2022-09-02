using System;
using System.Linq;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(TerrainHLOD))]
    public class TerrainHLODEditor : Editor
    {
        public static class Styles
        {
            public static GUIContent SourceText = new GUIContent("Source");
            public static GUIContent DestoryTerrainText = new GUIContent("Destroy terrain", "Destory original terrain when build time.");
            public static GUIContent GenerateButtonEnable = new GUIContent("Generate", "Generate a HLOD mesh.");
            public static GUIContent GenerateButtonExists = new GUIContent("Generate", "HLOD already generated.");
            public static GUIContent DestroyButtonEnable = new GUIContent("Destroy", "Destroy a HLOD mesh.");
            public static GUIContent DestroyButtonNotExists = new GUIContent("Destroy", "You need to generate HLOD before the destroy.");
            
            public static int[] TextureSizes = new int[]
            {
                4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096
            };
            public static string[] TextureSizeStrings;
            
            public static GUIStyle RedTextColor = new GUIStyle();

            static Styles()
            {
                TextureSizeStrings = new string[TextureSizes.Length];
                for (int i = 0; i < TextureSizes.Length; ++i)
                {
                    TextureSizeStrings[i] = TextureSizes[i].ToString();
                }
                
                RedTextColor.normal.textColor = Color.red;
            }
        }        
        
        private SerializedProperty m_TerrainDataProperty;
        private SerializedProperty m_DestoryTerrainProperty;
        private SerializedProperty m_ChunkSizeProperty;
        private SerializedProperty m_BorderVertexCountProperty;
        private SerializedProperty m_LODDistanceProperty;
        private SerializedProperty m_CullDistanceProperty;
        
        private Type[] m_SimplifierTypes;
        private string[] m_SimplifierNames;

        private Type[] m_StreamingTypes;
        private string[] m_StreamingNames;
        
        private LODSlider m_LODSlider;
        
        private bool isShowCommon = true;
        private bool isShowSimplifier = true;
        private bool isShowMaterial = true;
        private bool isShowStreaming = true;

        private bool isShowTexturePropertices = true;

        private ISpaceSplitter m_splitter = new QuadTreeSpaceSplitter(null);
        
        void OnEnable()
        {
            m_TerrainDataProperty = serializedObject.FindProperty("m_TerrainData");
            m_DestoryTerrainProperty = serializedObject.FindProperty("m_DestroyTerrain");
            m_ChunkSizeProperty = serializedObject.FindProperty("m_ChunkSize");
            m_BorderVertexCountProperty = serializedObject.FindProperty("m_BorderVertexCount");
            
            m_LODDistanceProperty = serializedObject.FindProperty("m_LODDistance");
            m_CullDistanceProperty = serializedObject.FindProperty("m_CullDistance");
            
            m_LODSlider = new LODSlider(true, "Cull");
            m_LODSlider.InsertRange("High", m_LODDistanceProperty);
            m_LODSlider.InsertRange("Low", m_CullDistanceProperty);

            m_SimplifierTypes = Simplifier.SimplifierTypes.GetTypes();
            m_SimplifierNames = m_SimplifierTypes.Select(t => t.Name).ToArray();

            m_StreamingTypes = Streaming.StreamingBuilderTypes.GetTypes();
            m_StreamingNames = m_StreamingTypes.Select(t => t.Name).ToArray();
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            TerrainHLOD hlod = target as TerrainHLOD;
            if (hlod == null)
            {
                EditorGUILayout.LabelField("TerrainHLOD is null.");
                return;
            
            }
            isShowCommon = EditorGUILayout.BeginFoldoutHeaderGroup(isShowCommon, "Common");
            if (isShowCommon == true)
            {
                EditorGUILayout.PropertyField(m_TerrainDataProperty, Styles.SourceText);
                EditorGUILayout.PropertyField(m_DestoryTerrainProperty, Styles.DestoryTerrainText);
                EditorGUILayout.PropertyField(m_ChunkSizeProperty);
                
                m_ChunkSizeProperty.floatValue = HLODUtils.GetChunkSizePropertyValue(m_ChunkSizeProperty.floatValue);
                
                var bounds = hlod.GetBounds();
                int depth = m_splitter.CalculateTreeDepth(bounds, m_ChunkSizeProperty.floatValue);
                EditorGUILayout.LabelField($"The HLOD tree will be created with {depth} levels.");
                if (depth > 5)
                {
                    EditorGUILayout.LabelField($"Node Level Count greater than 5 may cause a frozen Editor.", Styles.RedTextColor);
                    EditorGUILayout.LabelField($"Use a value less than 5.", Styles.RedTextColor);
                    
                }

                
                EditorGUILayout.PropertyField(m_BorderVertexCountProperty);
                m_LODSlider.Draw();
                
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
                            info.Invoke(null, new object[] {hlod.SimplifierOptions});
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Can not find Simplifiers.");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            isShowMaterial = EditorGUILayout.BeginFoldoutHeaderGroup(isShowMaterial, "Material");
            if (isShowMaterial == true)
            {
                Material mat = null;
                string matGUID = hlod.MaterialGUID;
                if (string.IsNullOrEmpty(matGUID) == false)
                {
                    string path = AssetDatabase.GUIDToAssetPath(matGUID);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                
                mat = EditorGUILayout.ObjectField("Material", mat, typeof(Material), false) as Material;
                if (mat != null)
                {
                    string path = AssetDatabase.GetAssetPath(mat);
                    hlod.MaterialGUID = AssetDatabase.AssetPathToGUID(path);
                }

                matGUID = hlod.MaterialLowGUID;
                if (string.IsNullOrEmpty(matGUID) == false)
                {
                    string path = AssetDatabase.GUIDToAssetPath(matGUID);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                }

                mat = EditorGUILayout.ObjectField("MaterialLow", mat, typeof(Material), false) as Material;
                if (mat != null)
                {
                    string path = AssetDatabase.GetAssetPath(mat);
                    hlod.MaterialLowGUID = AssetDatabase.AssetPathToGUID(path);
                }

                hlod.TextureSize = EditorGUILayout.IntPopup("Size", hlod.TextureSize, Styles.TextureSizeStrings,
                    Styles.TextureSizes);
                //Output Property name
                //EditorGUILayout.
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Standard"));
                }

                string[] outputTexturePropertyNames = mat.GetTexturePropertyNames();
                int index = 0;
                isShowTexturePropertices = EditorGUILayout.Foldout(isShowTexturePropertices, "Texture propertices");
                
                if ( isShowTexturePropertices == true )
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Toggle(true, GUILayout.Width(20));
                    index = Array.IndexOf(outputTexturePropertyNames, hlod.AlbedoPropertyName);
                    index = EditorGUILayout.Popup("Albedo", index, outputTexturePropertyNames);
                    index = (index < 0) ? 0 : index;
                    hlod.AlbedoPropertyName = outputTexturePropertyNames[index];
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    hlod.UseNormal = EditorGUILayout.Toggle(hlod.UseNormal, GUILayout.Width(20));
                    index = Array.IndexOf(outputTexturePropertyNames, hlod.NormalPropertyName);
                    index = EditorGUILayout.Popup("Normal", index, outputTexturePropertyNames);
                    index = (index < 0) ? 0 : index;
                    hlod.NormalPropertyName = outputTexturePropertyNames[index];
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    hlod.UseMask = EditorGUILayout.Toggle(hlod.UseMask, GUILayout.Width(20));
                    index = Array.IndexOf(outputTexturePropertyNames, hlod.MaskPropertyName);
                    index = EditorGUILayout.Popup("Mask", index, outputTexturePropertyNames);
                    index = (index < 0) ? 0 : index;
                    hlod.MaskPropertyName = outputTexturePropertyNames[index];
                    EditorGUILayout.EndHorizontal();
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
                            info.Invoke(null, new object[] { hlod.StreamingOptions });
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

            if (hlod.GetComponent<HLODControllerBase>() != null)
            {
                generateButton = Styles.GenerateButtonExists;
                destroyButton = Styles.DestroyButtonEnable;
            }



            EditorGUILayout.Space();

            GUI.enabled = generateButton == Styles.GenerateButtonEnable;
            if (GUILayout.Button(generateButton))
            {
                CoroutineRunner.RunCoroutine(TerrainHLODCreator.Create(hlod));
            }

            GUI.enabled = destroyButton == Styles.DestroyButtonEnable;
            if (GUILayout.Button(destroyButton))
            {
                CoroutineRunner.RunCoroutine(TerrainHLODCreator.Destroy(hlod));
            }

            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        
        }
    }

}