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

        public bool RemoveAtBuild
        {
            get
            {
                return m_removeAtBuild;
            }
        }

        public int GroupCount
        {
            get
            {
                return m_meshSettings.Count;
            }
        }

        public HLODMeshSetterGroup GetGroup(int index)
        {
            return m_meshSettings[index];
        }

        public HLODMeshSetterGroup FindGroup(int level)
        {
            HLODMeshSetterGroup group = null;
            for (int i = 0; i < m_meshSettings.Count; ++i)
            {
                if (m_meshSettings[i].TargetLevel <= level)
                {
                    group = m_meshSettings[i];
                }
                else
                {
                    break;
                }
            }

            return group;
        }

    }
}