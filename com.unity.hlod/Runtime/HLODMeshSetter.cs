using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLODMeshSetter : MonoBehaviour
    {
        [SerializeField] 
        private bool m_removeAtBuild;
        [SerializeField] 
        private List<HLODMeshSetterGroup> m_meshSettings;
    }
}