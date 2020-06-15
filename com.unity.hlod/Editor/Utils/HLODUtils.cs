using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public static class HLODUtils
    {
        public static float GetChunkSizePropertyValue(float value)
        {
            if (value < 0.05f)
            {
                return 0.05f;
            }
            return value;
        }
    }
}