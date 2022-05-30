using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    [Serializable]
    public class HLODMeshSetterGroup 
    {
        [SerializeField] 
        private int m_targetLevel;
        [SerializeField] 
        private List<MeshRenderer> m_meshRenderers;

        public int TargetLevel
        {
            get
            {
                return m_targetLevel;
            }
        }

        public List<MeshRenderer> MeshRenderers
        {
            get
            {
                return m_meshRenderers;
            }
        }
    }

}