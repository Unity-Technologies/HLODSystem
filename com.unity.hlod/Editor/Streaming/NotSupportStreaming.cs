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

        public void Build(SpaceNode rootNode, DisposableList<HLODBuildInfo> infos, Action<float> onProgress)
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
                var spaceNode = infos[i].Target;
                var hlodTreeNode = convertedTable[infos[i].Target];

                for (int oi = 0; oi < spaceNode.Objects.Count; ++oi)
                {
                    int highId = defaultController.AddHighObject(spaceNode.Objects[oi]);
                    hlodTreeNode.HighObjectIds.Add(highId);
                }

                for (int oi = 0; oi < infos[i].WorkingObjects.Count; ++oi)
                {
                    string currentHLODName = $"{this.m_hlod.name}{infos[i].Name}_{oi}";
                    HLODMesh createdMesh = ObjectUtils.SaveHLODMesh(path, currentHLODName, infos[i].WorkingObjects[oi]);
                    m_hlod.GeneratedObjects.Add(createdMesh);

                    int lowId = defaultController.AddLowObject(createdMesh);
                    hlodTreeNode.LowObjectIds.Add(lowId);
                }

                if (onProgress != null)
                    onProgress((float)i / (float)infos.Count);
            }

            defaultController.Root = convertedRootNode;
            defaultController.CullDistance = m_hlod.CullDistance;
            defaultController.LODDistance = m_hlod.LODDistance;
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
