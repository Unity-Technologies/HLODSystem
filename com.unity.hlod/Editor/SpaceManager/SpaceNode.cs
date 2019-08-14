using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{
    public class SpaceNode
    {
        public static SpaceNode CreateSpaceNodeWithBounds(Bounds bounds)
        {
            var spaceNode = new SpaceNode();
            spaceNode.Bounds = bounds;
            return spaceNode;
        }

        private Bounds m_bounds;
        private SpaceNode m_parentNode;
        private List<SpaceNode> m_childTreeNodes = new List<SpaceNode>(); 
        private List<GameObject> m_objcets = new List<GameObject>();
        

        public Bounds Bounds
        {
            set { m_bounds = value;}
            get { return m_bounds; }
        }
        public List<GameObject> Objects
        {
            get { return m_objcets; }
        }

        public SpaceNode ParentNode
        {
            set
            {
                if (m_parentNode != null)
                    m_parentNode.m_childTreeNodes.Remove(this);
                m_parentNode = value;
                if (value != null)
                    value.m_childTreeNodes.Add(this);
            }
            get { return m_parentNode; }
        }

        public SpaceNode GetChild(int index)
        {
            return m_childTreeNodes[index];
        }
        public int GetChildCount()
        {
            return m_childTreeNodes.Count;
        }
        public bool HasChild()
        {
            return m_childTreeNodes.Count > 0;
        }
//        public List<SpaceNode> ChildTreeNodes
//        {
//            set { m_childTreeNodes = value;}
//            get { return m_childTreeNodes; }
//        }
    }
}