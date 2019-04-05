using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{
    public class SpaceNode : ScriptableObject
    {
        public static SpaceNode CreateSpaceNodeWithBounds(Bounds bounds)
        {
            var spaceNode = CreateInstance<SpaceNode>();
            spaceNode.Bounds = bounds;
            return spaceNode;
        }

        [SerializeField]
        private Bounds m_bounds;
        [SerializeField]
        private List<SpaceNode> m_childTreeNodes;

        [SerializeField]
        private List<GameObject> m_objcets = new List<GameObject>();
        [SerializeField]
        private List<HLODMesh> m_hlodMeshes = new List<HLODMesh>();


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
        public List<HLODMesh> HLODMeshes
        {
            get { return m_hlodMeshes; }
        }
    }
}