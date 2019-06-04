using System;
using System.Collections.Generic;
using System.IO;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableStreaming : IStreamingBuilder
    {
        
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            StreamingBuilderTypes.RegisterType(typeof(AddressableStreaming));
        }

        
        private HLOD m_hlod;
        public AddressableStreaming(HLOD hlod)
        {
            m_hlod = hlod;
        }


        public void Build(SpaceManager.SpaceNode rootNode, List<HLODBuildInfo> infos, Action<float> onProgress)
        {
            string path = "";
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(m_hlod.gameObject);
            path = stage.prefabAssetPath;
            path = Path.GetDirectoryName(path) + "/";

            var addressableController = m_hlod.gameObject.AddComponent<AddressableController>();
            HLODTreeNode convertedRootNode = ConvertNode(rootNode);

            if (onProgress != null)
                onProgress(0.0f);

            //I think it is better to do when convert nodes.
            //But that is not easy because of the structure.
            for (int i = 0; i < infos.Count; ++i)
            {
                var spaceNode = infos[i].Target;
                var hlodTreeNode = convertedTable[infos[i].Target];

                for (int oi = 0; oi < spaceNode.Objects.Count; ++oi)
                {
                    var address = GetAssetReference(spaceNode.Objects[oi]);
                    int highId = -1;
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

                /*for (int oi = 0; oi < infos[i].combinedGameObjects.Count; ++oi)
                {
                    List<HLODMesh> createdMeshes = ObjectUtils.SaveHLODMesh(path, m_hlod.name, infos[i].combinedGameObjects[oi]);
                    m_hlod.GeneratedObjects.AddRange(createdMeshes);

                    foreach (var mesh in createdMeshes)
                    {
                        var address = GetAssetReference(mesh);
                        int lowId = addressableController.AddLowObject(address);
                        hlodTreeNode.LowObjectIds.Add(lowId);
                    }
                }*/

                if (onProgress != null)
                    onProgress((float)i/(float)infos.Count);
            }

            m_hlod.Root = convertedRootNode;
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
                if (spaceNode.ChildTreeNodes != null)
                {
                    List<HLODTreeNode> childTreeNodes = new List<HLODTreeNode>(spaceNode.ChildTreeNodes.Count);
                    for (int i = 0; i < spaceNode.ChildTreeNodes.Count; ++i)
                    {
                        var treeNode = new HLODTreeNode();
                        childTreeNodes.Add(treeNode);

                        hlodTreeNodes.Enqueue(treeNode);
                        spaceNodes.Enqueue(spaceNode.ChildTreeNodes[i]);
                    }

                    hlodTreeNode.ChildNodes = childTreeNodes;

                }
            }

            return root;
        }


        public static void OnGUI(HLOD hlod)
        {
            dynamic options = hlod.StreamingOptions;

            if (options.LastLowInMemory == null)
                options.LastLowInMemory = false;
            if (options.MaxInstantiateCount == null)
                options.MaxInstantiateCount = 10;

            EditorGUI.indentLevel += 1;
            options.LastLowInMemory = EditorGUILayout.Toggle("Last low in memory", options.LastLowInMemory);
            options.MaxInstantiateCount =
                EditorGUILayout.IntSlider("Max instantiate count per frame", options.MaxInstantiateCount, 1, 100, null);
            
            EditorGUI.indentLevel -= 1;
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