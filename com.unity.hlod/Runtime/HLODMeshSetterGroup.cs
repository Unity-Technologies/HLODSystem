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
    }

}