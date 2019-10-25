using System;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLODCameraRecognizer : MonoBehaviour
    {
        private static HLODCameraRecognizer s_instance;
        private static Camera s_recognizedCamera;
        public static HLODCameraRecognizer Instance => s_instance;
        public static Camera RecognizedCamera => s_recognizedCamera;

        private void Awake()
        {
            s_instance = this;
            s_recognizedCamera = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
                s_recognizedCamera = null;
            }
        }
    }
}