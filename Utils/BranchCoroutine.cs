using System;
using System.Collections;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public class BranchCoroutine : YieldInstruction
    {
        private IEnumerator m_Branch;
        public BranchCoroutine(IEnumerator branch)
        {
            m_Branch = branch;
        }

        public IEnumerator GetBranch()
        {
            return m_Branch;
        }
    }
}
