using System.Collections;
using System.Collections.Generic;
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

        [SerializeField]
        private Bounds m_bounds;
        [SerializeField]
        private List<SpaceNode> m_childTreeNodes;

        [SerializeField]
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
        public List<SpaceNode> ChildTreeNodes
        {
            set { m_childTreeNodes = value;}
            get { return m_childTreeNodes; }
        }
    }
}