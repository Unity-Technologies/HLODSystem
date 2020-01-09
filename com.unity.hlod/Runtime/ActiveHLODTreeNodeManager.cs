using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class ActiveHLODTreeNodeManager
    {
        private List<HLODTreeNode> m_activeTreeNode = new List<HLODTreeNode>();

        public void UpdateActiveNodes(float lodDistance)
        {
            for (int i = 0; i < m_activeTreeNode.Count; ++i)
            {
                m_activeTreeNode[i].Update(lodDistance);
            }
        }

        public void Activate(HLODTreeNode node)
        {
            m_activeTreeNode.Add(node);
        }

        public void Deactivate(HLODTreeNode node)
        {
            if (m_activeTreeNode.Remove(node) == true)
            {
                //Succeed to remove, that means the child also activated.
                //so, we should remove child own.
                if (node.GetChildTreeNodeCount() == 0)
                    return;
                
                for (int i = 0; i < node.GetChildTreeNodeCount(); ++i)
                {
                    Deactivate(node.GetChildTreeNode(i));
                }
            }
        }
    }

}