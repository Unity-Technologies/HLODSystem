using System;
using System.Collections.Generic;
using System.IO;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
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
            public static Dictionary<TextureFormat, int> SupportTextureFormatIndex;

            static Styles()
            {
                SupportTextureFormatStrings = new string[SupportTextureFormats.Length];
                SupportTextureFormatIndex = new Dictionary<TextureFormat, int>();
                for (int i = 0; i < SupportTextureFormats.Length; ++i)
                {
                    SupportTextureFormatStrings[i] = SupportTextureFormats[i].ToString();
                    SupportTextureFormatIndex[SupportTextureFormats[i]] = i;
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

        HashSet<string> m_shaderGuids = new HashSet<string>();
        
        public AddressableStreaming(IGeneratedResourceManager manager, SerializableDynamicObject streamingOptions)
        {
            m_manager = manager;
            m_streamingOptions = streamingOptions;
        }
        
        public void Build(SpaceNode rootNode, DisposableList<HLODBuildInfo> infos, GameObject root, float cullDistance, float lodDistance, bool writeNoPrefab, Action<float> onProgress)
        {
            dynamic options = m_streamingOptions;
            string path = options.OutputDirectory;
            
            HLODTreeNodeContainer container = new HLODTreeNodeContainer();
            HLODTreeNode convertedRootNode = ConvertNode(container, rootNode);
            
            //create settings if there is no settings.
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
            }

            
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = GetGroup(settings, options.AddressablesGroupName);
            m_shaderGuids.Clear();
            
            if (onProgress != null)
                onProgress(0.0f);

            HLODData.TextureCompressionData compressionData;
            compressionData.PCTextureFormat = options.PCCompression;
            compressionData.WebGLTextureFormat = options.WebGLCompression;
            compressionData.AndroidTextureFormat = options.AndroidCompression;
            compressionData.iOSTextureFormat = options.iOSCompression;
            compressionData.tvOSTextureFormat = options.tvOSCompression;


            string filenamePrefix = $"{path}{root.name}";
            
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
            
            Dictionary<int, HLODData> hlodDatas = new Dictionary<int, HLODData>();

            for (int i = 0; i < infos.Count; ++i)
            {
                if (hlodDatas.ContainsKey(infos[i].ParentIndex) == false)
                {
                    HLODData newData = new HLODData();
                    newData.CompressionData = compressionData;
                    hlodDatas.Add(infos[i].ParentIndex, newData);
                }

                HLODData data = hlodDatas[infos[i].ParentIndex];
                data.AddFromWokringObjects(infos[i].Name, infos[i].WorkingObjects);
                data.AddFromWorkingColliders(infos[i].Name, infos[i].Colliders);

                if (writeNoPrefab)
                {
                    if (hlodDatas.ContainsKey(i) == false)
                    {
                        HLODData newData = new HLODData();
                        newData.CompressionData = compressionData;
                        hlodDatas.Add(i, newData);
                    }

                    HLODData prefabData = hlodDatas[i];
                    var spaceNode = infos[i].Target;

                    for (int oi = 0; oi < spaceNode.Objects.Count; ++oi)
                    {
                        if (PrefabUtility.IsAnyPrefabInstanceRoot(spaceNode.Objects[oi]) == false)
                        {
                            prefabData.AddFromGameObject(spaceNode.Objects[oi]);
                        }
                    }
                }

                if (onProgress != null)
                    onProgress((float) i / (float) infos.Count);
            }
             

            Dictionary<int, RootData> rootDatas = new Dictionary<int, RootData>();
            foreach (var item in hlodDatas)
            {
                string filename = $"{filenamePrefix}_group{item.Key}.hlod";
                using (Stream stream = new FileStream(filename, FileMode.Create))
                {
                    HLODDataSerializer.Write(stream, item.Value);
                    stream.Close();
                }
                
                AssetDatabase.ImportAsset(filename, ImportAssetOptions.ForceUpdate);
                RootData rootData = AssetDatabase.LoadAssetAtPath<RootData>(filename);
                m_manager.AddGeneratedResource(rootData);
                AddAddress(settings, group, rootData);
                
                rootDatas.Add(item.Key, rootData);
            }

            

            var addressableController = root.AddComponent<AddressableHLODController>();

            for (int i = 0; i < infos.Count; ++i)
            {
                var spaceNode = infos[i].Target;
                var hlodTreeNode = convertedTable[infos[i].Target];
                
                for (int oi = 0; oi < spaceNode.Objects.Count; ++oi)
                {
                    int highId = -1;
                    GameObject obj = spaceNode.Objects[oi];

                    if (PrefabUtility.IsPartOfAnyPrefab(obj) == false)
                    {
                        GameObject rootGameObject = rootDatas[i].GetRootObject(obj.name);
                        if (rootGameObject != null)
                        {
                            GameObject go = PrefabUtility.InstantiatePrefab(rootGameObject) as GameObject;
                            go.transform.SetParent(obj.transform.parent);
                            go.transform.localPosition = obj.transform.localPosition;
                            go.transform.localRotation = obj.transform.localRotation;
                            go.transform.localScale = obj.transform.localScale;

                            if (m_manager.IsGeneratedResource(obj))
                                m_manager.AddGeneratedResource(go);
                            else
                                m_manager.AddConvertedPrefabResource(go);

                            spaceNode.Objects.Add(go);

                            Object.DestroyImmediate(obj);
                            continue;
                        }
                    }

                    var address = GetAddress(spaceNode.Objects[oi]);
                    if (string.IsNullOrEmpty(address) && PrefabUtility.IsAnyPrefabInstanceRoot(spaceNode.Objects[oi]))
                    {
                        AddAddress(settings, group, spaceNode.Objects[oi]);
                        address = GetAddress(spaceNode.Objects[oi]);
                    }
                    
                    if (address != null)
                    {
                        highId = addressableController.AddHighObject(address, spaceNode.Objects[oi]);
                    }
                    else
                    {
                        highId = addressableController.AddHighObject(spaceNode.Objects[oi]);
                    }
                    
                    hlodTreeNode.HighObjectIds.Add(highId);
                }

                {
                    if (rootDatas[infos[i].ParentIndex].GetRootObject(infos[i].Name) != null)
                    {
                        string filename = $"{filenamePrefix}_group{infos[i].ParentIndex}.hlod";
                        int lowId = addressableController.AddLowObject(filename + "[" + infos[i].Name + "]");
                        hlodTreeNode.LowObjectIds.Add(lowId);    
                    }

                }
            }

            var shaderEntriesAdded = new List<AddressableAssetEntry>();
            foreach (var shaderGuid in m_shaderGuids)
            {
                if ( IsExistsInAddressables(shaderGuid) == false )
                    shaderEntriesAdded.Add(settings.CreateOrMoveEntry(shaderGuid, group, false, false));
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, shaderEntriesAdded, true);
            m_shaderGuids.Clear();

            addressableController.Container = container;
            addressableController.Root = convertedRootNode;
            addressableController.CullDistance = cullDistance;
            addressableController.LODDistance = lodDistance;
        }

      
        Dictionary<SpaceNode, HLODTreeNode> convertedTable = new Dictionary<SpaceNode, HLODTreeNode>();

        private HLODTreeNode ConvertNode(HLODTreeNodeContainer container, SpaceNode rootNode)
        {
            HLODTreeNode root = new HLODTreeNode();
            root.SetContainer(container);

            Queue<HLODTreeNode> hlodTreeNodes = new Queue<HLODTreeNode>();
            Queue<SpaceNode> spaceNodes = new Queue<SpaceNode>();
            Queue<int> levels = new Queue<int>();

            hlodTreeNodes.Enqueue(root);
            spaceNodes.Enqueue(rootNode);
            levels.Enqueue(0);

            while (hlodTreeNodes.Count > 0)
            {
                var hlodTreeNode = hlodTreeNodes.Dequeue();
                var spaceNode = spaceNodes.Dequeue();
                int level = levels.Dequeue();

                convertedTable[spaceNode] = hlodTreeNode;

                hlodTreeNode.Level = level;
                hlodTreeNode.Bounds = spaceNode.Bounds;
                if (spaceNode.HasChild() == true)
                {
                    List<HLODTreeNode> childTreeNodes = new List<HLODTreeNode>(spaceNode.GetChildCount());
                    for (int i = 0; i < spaceNode.GetChildCount(); ++i)
                    {
                        var treeNode = new HLODTreeNode();
                        treeNode.SetContainer(container);
                        childTreeNodes.Add(treeNode);

                        hlodTreeNodes.Enqueue(treeNode);
                        spaceNodes.Enqueue(spaceNode.GetChild(i));
                        levels.Enqueue(level + 1);
                    }

                    hlodTreeNode.SetChildTreeNode(childTreeNodes);

                }
            }

            return root;
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

            if (options.AddressablesGroupName == null)
            {
                options.AddressablesGroupName = "HLOD";
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

            options.AddressablesGroupName = EditorGUILayout.TextField("Addressables Group", options.AddressablesGroupName);
            

            // It stores return value from foldout and uses it as a condition.
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
            int selectIndex = 0;
            //no matter the format exists or not.
            Styles.SupportTextureFormatIndex.TryGetValue(format, out selectIndex);
            selectIndex = EditorGUILayout.Popup(label, selectIndex, Styles.SupportTextureFormatStrings);
            if (selectIndex < 0)
                selectIndex = 0;
            return Styles.SupportTextureFormats[selectIndex];
        }

        private void AddAddress(AddressableAssetSettings settings, AddressableAssetGroup group, Object obj)
        {
            
            string path = GetAssetPath(obj);
            
            if (string.IsNullOrEmpty(path))
                return;

            var entriesAdded = new List<AddressableAssetEntry>();

            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < objects.Length; ++i)
            {
                Material mat = objects[i] as Material;

                if (mat == null)
                    continue;

                Shader shader = mat.shader;
                var shaderPath = AssetDatabase.GetAssetPath(shader);
                var shaderGuid = AssetDatabase.AssetPathToGUID(shaderPath);

                m_shaderGuids.Add(shaderGuid);
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            entriesAdded.Add(settings.CreateOrMoveEntry(guid, group, false, false));
            
            

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
        }

        private string GetAddress(Object obj)
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                return null;
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            string path = GetAssetPath(obj);
            
            if (string.IsNullOrEmpty(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                string address = entry.address;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)))
                {
                    Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (AssetDatabase.IsMainAsset(prefab) == false)
                    {
                        address = address + "[" + prefab.name + "]";
                    }
                }

                return address;

            }
            
            return null;
        }

        private string GetAssetPath(Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path))
            {
                Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                path = AssetDatabase.GetAssetPath(prefab);   
            }

            return path;
        }

        private bool IsExistsInAddressables(string guid)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
                return false;

            for (int gi = 0; gi < settings.groups.Count; ++gi)
            {
                var group = settings.groups[gi];

                foreach (var entry in group.entries)
                {
                    if (entry.guid == guid)
                        return true;
                }
            }

            return false;
        }

        private AddressableAssetGroup GetGroup(AddressableAssetSettings settings, string groupName)
        {
            for (int i = 0; i < settings.groups.Count; ++i)
            {
                if (settings.groups[i].Name == groupName)
                    return settings.groups[i];
            }
            
            List<AddressableAssetGroupSchema> schemas = new List<AddressableAssetGroupSchema>();

            ContentUpdateGroupSchema contentUpdateGroupSchema = ScriptableObject.CreateInstance<ContentUpdateGroupSchema>();
            BundledAssetGroupSchema bundledAssetGroupSchema = ScriptableObject.CreateInstance<BundledAssetGroupSchema>();

            bundledAssetGroupSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            
            schemas.Add(contentUpdateGroupSchema);
            schemas.Add(bundledAssetGroupSchema);

            AddressableAssetGroup group = settings.CreateGroup(groupName, false, false, false, schemas);
            return group;
        }
    }

}