using System;
using System.IO;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

namespace Unity.HLODSystem.Streaming
{
    class NotSupportStreaming : IStreamingBuilder
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            StreamingBuilderTypes.RegisterType(typeof(NotSupportStreaming), -1);
        }

        private HLOD m_hlod;
        public NotSupportStreaming(HLOD hlod)
        {
            m_hlod = hlod;
        }

        public void Build(SpaceNode rootNode, List<HLODBuildInfo> infos, Action<float> onProgress)
        {
            string path = "";
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(m_hlod.gameObject);
            path = stage.prefabAssetPath;
            path = Path.GetDirectoryName(path) + "/";

            var defaultController = m_hlod.gameObject.AddComponent<DefaultController>();
            HLODTreeNode convertedRootNode = ConvertNode(rootNode);

            if (onProgress != null)
                onProgress(0.0f);

            //I think it is better to do when convert nodes.
            //But that is not easy because of the structure.
            for (int i = 0; i < infos.Count; ++i)
            {
                var spaceNode = infos[i].target;
                var hlodTreeNode = convertedTable[infos[i].target];

                for (int oi = 0; oi < spaceNode.Objects.Count; ++oi)
                {
                    int highId = defaultController.AddHighObject(spaceNode.Objects[oi]);
                    hlodTreeNode.HighObjectIds.Add(highId);
                }

                for (int oi = 0; oi < infos[i].combinedGameObjects.Count; ++oi)
                {
                    List<HLODMesh> createdMeshes = ObjectUtils.SaveHLODMesh(path, m_hlod.name, infos[i].combinedGameObjects[oi]);
                    m_hlod.GeneratedObjects.AddRange(createdMeshes);

                    foreach (var mesh in createdMeshes)
                    {
                        int lowId = defaultController.AddLowObject(mesh);
                        hlodTreeNode.LowObjectIds.Add(lowId);
                    }
                }

                if (onProgress != null)
                    onProgress((float)i / (float)infos.Count);
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
        

    }
}
