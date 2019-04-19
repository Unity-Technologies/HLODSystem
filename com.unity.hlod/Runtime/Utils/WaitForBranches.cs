using System;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public class WaitForBranches : YieldInstruction
    {
        private Action<float> m_onProgress;
        public WaitForBranches(Action<float> onProgress)
        {
            m_onProgress = onProgress;
        }

        public void OnProgress(float progress)
        {
            if (m_onProgress != null)
                m_onProgress(progress);
        }
    }
}
