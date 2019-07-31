using System;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableStreaming : IStreamingBuilder
    {
        static class Styles
        {
            public static TextureFormat[] SupportTextureFormats = new[]
            {
                TextureFormat.RGBA32,
                TextureFormat.RGB24,
                TextureFormat.BC7,
                TextureFormat.DXT5,
                TextureFormat.DXT1,
                TextureFormat.ASTC_4x4,
                TextureFormat.ASTC_5x5,
                TextureFormat.ASTC_6x6,
                TextureFormat.ASTC_8x8,
                TextureFormat.ASTC_10x10,
                TextureFormat.ASTC_12x12,
                TextureFormat.ETC_RGB4,
                TextureFormat.ETC2_RGB,
                TextureFormat.ETC2_RGBA8,
                TextureFormat.PVRTC_RGB4,
                TextureFormat.PVRTC_RGB2,
                TextureFormat.PVRTC_RGBA4,
                TextureFormat.PVRTC_RGBA2,
            };

            public static string[] SupportTextureFormatStrings;

            static Styles()
            {
                SupportTextureFormatStrings = new string[SupportTextureFormats.Length];
                for (int i = 0; i < SupportTextureFormats.Length; ++i)
                {
                    SupportTextureFormatStrings[i] = SupportTextureFormats[i].ToString();
                }
            }
        }
        
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            StreamingBuilderTypes.RegisterType(typeof(AddressableStreaming));
        }
        
        private IGeneratedResourceManager m_manager;
        private SerializableDynamicObject m_streamingOptions;
        
        public AddressableStreaming(IGeneratedResourceManager manager, SerializableDynamicObject streamingOptions)
        {
            m_manager = manager;
            m_streamingOptions = streamingOptions;
        }


        public void Build(SpaceNode rootNode, DisposableList<HLODBuildInfo> infos, GameObject root, float cullDistance, float lodDistance, Action<float> onProgress)
        {
            dynamic options = m_streamingOptions;
            string path = options.OutputDirectory;

            var addressableController = root.AddComponent<AddressableController>();
            HLODTreeNode convertedRootNode = ConvertNode(rootNode);

            if (onProgress != null)
                onProgress(0.0f);

            var rootData = EmptyData.CreateInstance<EmptyData>();
            string filename = $"{path}{root.name}.asset";
            AssetDatabase.CreateAsset(rootData, filename);
            m_manager.AddGeneratedResource(rootData);

            for (int i = 0; i < infos.Count; ++i)
            {
                WriteInfo(filename, infos[i], options);
            }
            AssetDatabase.SaveAssets();

            return;
            
            //I think it is better to do when convert nodes.
            //But that is not easy because of the structure.
            for (int i = 0; i < infos.Count; ++i)
            {
                var spaceNode = infos[i].Target;
                var hlodTreeNode = convertedTable[infos[i].Target];

                for (int oi = 0; oi < spaceNode.Objects.Count; ++oi)
                {
                    int highId = -1;
                    
                    /*var address = GetAssetReference(spaceNode.Objects[oi]);
                    
                    if (address != null)
                    {
                        highId = addressableController.AddHighObject(address, spaceNode.Objects[oi]);
                    }
                    else
                    {
                        highId = addressableController.AddHighObject(spaceNode.Objects[oi]);
                    }*/

                    hlodTreeNode.HighObjectIds.Add(highId);
                }
                
                //List<MeshData> datas = WriteInfo(filename, infos[i], options);

//                go.transform.SetParent(hlodRoot.transform, false);
//                go.SetActive(false);
//                int lowId = defaultController.AddLowObject(go);
//                hlodTreeNode.LowObjectIds.Add(lowId);
                
//                m_manager.AddGeneratedResource(go);
                for (int oi = 0; oi < infos[i].WorkingObjects.Count; ++oi)
                {
                    
//                    string currentHLODName = $"{root.name}{infos[i].Name}_{oi}";
//                    HLODMesh createdMesh = ObjectUtils.SaveHLODMesh(path, currentHLODName, infos[i].WorkingObjects[oi]);
//                    m_manager.AddGeneratedResource(createdMesh);
//
//                    var address = GetAssetReference(createdMesh);
//                    int lowId = addressableController.AddLowObject(address);
//                    hlodTreeNode.LowObjectIds.Add(lowId);
                }

                if (onProgress != null)
                    onProgress((float)i/(float)infos.Count);
            }

            addressableController.Root = convertedRootNode;
            addressableController.CullDistance = cullDistance;
            addressableController.LODDistance = lodDistance;
        }

      
        Dictionary<SpaceNode, HLODTreeNode> convertedTable = new Dictionary<SpaceNode, HLODTreeNode>();

        private HLODTreeNode ConvertNode(SpaceNode rootNode)
        {
            HLODTreeNode root = new HLODTreeNode();

            Queue<HLODTreeNode> hlodTreeNodes = new Queue<HLODTreeNode>();
            Queue<SpaceNode> spaceNodes = new Queue<SpaceNode>();

            hlodTreeNodes.Enqueue(root);
            spaceNodes.Enqueue(rootNode);

            while (hlodTreeNodes.Count > 0)
            {
                var hlodTreeNode = hlodTreeNodes.Dequeue();
                var spaceNode = spaceNodes.Dequeue();

                convertedTable[spaceNode] = hlodTreeNode;

                hlodTreeNode.Bounds = spaceNode.Bounds;
                if (spaceNode.HasChild() == true)
                {
                    List<HLODTreeNode> childTreeNodes = new List<HLODTreeNode>(spaceNode.GetChildCount());
                    for (int i = 0; i < spaceNode.GetChildCount(); ++i)
                    {
                        var treeNode = new HLODTreeNode();
                        childTreeNodes.Add(treeNode);

                        hlodTreeNodes.Enqueue(treeNode);
                        spaceNodes.Enqueue(spaceNode.GetChild(i));
                    }

                    hlodTreeNode.ChildNodes = childTreeNodes;

                }
            }

            return root;
        }
        private void WriteInfo(string filename, HLODBuildInfo info, dynamic options)
        {
            for (int oi = 0; oi < info.WorkingObjects.Count; ++oi)
            {
                WorkingObject wo = info.WorkingObjects[oi];
                
//                MeshData.TextureCompressionData compressionData;
//                compressionData.PCTextureFormat = options.PCCompression;
//                compressionData.WebGLTextureFormat = options.WebGLCompression;
//                compressionData.AndroidTextureFormat = options.AndroidCompression;
//                compressionData.iOSTextureFormat = options.iOSCompression;
//                compressionData.tvOSTextureFormat = options.tvOSCompression;
//
//                MeshData meshData = MeshUtils.WorkingObjectToMeshData(wo, "HLOD" + info.Name);
//                meshData.CompressionData = compressionData;
//                meshData.WriteAppend(filename);
            }
        }

        
        static bool showFormat = true;
        public static void OnGUI(SerializableDynamicObject streamingOptions)
        {
            dynamic options = streamingOptions;

            #region Setup default values

            if (options.OutputDirectory == null)
            {
                string path = Application.dataPath;
                path = "Assets" + path.Substring(Application.dataPath.Length);
                path = path.Replace('\\', '/');
                if (path.EndsWith("/") == false)
                    path += "/";
                options.OutputDirectory = path;
            }

            if (options.PCCompression == null)
            {
                options.PCCompression = TextureFormat.BC7;
            }

            if (options.WebGLCompression == null)
            {
                options.WebGLCompression = TextureFormat.DXT5;
            }

            if (options.AndroidCompression == null)
            {
                options.AndroidCompression = TextureFormat.ETC2_RGBA8;
            }

            if (options.iOSCompression == null)
            {
                options.iOSCompression = TextureFormat.PVRTC_RGBA4;
            }

            if (options.tvOSCompression == null)
            {
                options.tvOSCompression = TextureFormat.ASTC_4x4;
            }

            #endregion

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("OutputDirectory");
            if (GUILayout.Button(options.OutputDirectory))
            {
                string selectPath = EditorUtility.OpenFolderPanel("Select output folder", "Assets", "");

                if (selectPath.StartsWith(Application.dataPath))
                {
                    selectPath = "Assets" + selectPath.Substring(Application.dataPath.Length);
                    selectPath = selectPath.Replace('\\', '/');
                    if (selectPath.EndsWith("/") == false)
                        selectPath += "/";
                    options.OutputDirectory = selectPath;
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Select directory under {Application.dataPath}", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();

            if (showFormat = EditorGUILayout.Foldout(showFormat, "Compress Format"))
            {
                EditorGUI.indentLevel += 1;
                options.PCCompression = PopupFormat("PC & Console", (TextureFormat) options.PCCompression);
                options.WebGLCompression = PopupFormat("WebGL", (TextureFormat) options.WebGLCompression);
                options.AndroidCompression = PopupFormat("Android", (TextureFormat) options.AndroidCompression);
                options.iOSCompression = PopupFormat("iOS", (TextureFormat) options.iOSCompression);
                options.tvOSCompression = PopupFormat("tvOS", (TextureFormat) options.tvOSCompression);
                EditorGUI.indentLevel -= 1;
            }
        }
        
        private static TextureFormat PopupFormat(string label, TextureFormat format)
        {
            int selectIndex = Array.IndexOf(Styles.SupportTextureFormats, format);
            selectIndex = EditorGUILayout.Popup(label, selectIndex, Styles.SupportTextureFormatStrings);
            if (selectIndex < 0)
                selectIndex = 0;
            return Styles.SupportTextureFormats[selectIndex];
        }


        private AssetReference GetAssetReference(Object obj)
        {
            //create settings if there is no settings.
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
            }

            
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            string path = "";

            if ( obj is GameObject && PrefabUtility.IsAnyPrefabInstanceRoot(obj as GameObject) == true )
                path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            else
                path = AssetDatabase.GetAssetPath(obj);
            
            if (string.IsNullOrEmpty(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);

            if (entry != null)
                return new AssetReference(guid);

            var entriesAdded = new List<AddressableAssetEntry>
            {
                settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false)
            };

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);

            return new AssetReference(guid);
        }
    }

}