using System;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLODCameraRecognizer : MonoBehaviour
    {
        private static HLODCameraRecognizer s_instance;
        public static HLODCameraRecognizer Instance => s_instance;

        private void Awake()
        {
            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}