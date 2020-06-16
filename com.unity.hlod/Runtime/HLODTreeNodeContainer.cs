using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    [Serializable]
    public class HLODTreeNodeContainer
    {

        [SerializeField]
        private List<HLODTreeNode> m_treeNodes = new List<HLODTreeNode>();

        public int Count
        {
            get => m_treeNodes.Count;
        }
        /**
         * @return node id
         */
        public int Add(HLODTreeNode node)
        {
            int id = m_treeNodes.Count;
            m_treeNodes.Add(node);

            return id;
        }

        public void Remove(int id)
        {
            
        }

        public void Remove(HLODTreeNode node)
        {
            
        }

        
        public HLODTreeNode Get(int id)
        {
            return m_treeNodes[id];
        }

    }

}