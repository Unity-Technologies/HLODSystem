using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.HLODSystem
{
    public class HLODManager
    {
        private static HLODManager s_instance = null;

        public static HLODManager Instance
        {
            get
            {
                if ( s_instance == null )
                    s_instance = new HLODManager();
                return s_instance;
            }
        }

        public void RegisterHLOD(HLOD hlod)
        {
            if (m_activeHLODs == null)
            {
                m_activeHLODs = new List<HLOD>();
                Camera.onPreCull += OnPreCull;
                RenderPipeline.beginCameraRendering += OnPreCull;
            }
            m_activeHLODs.Add(hlod);
        }
        public void UnregisterHLOD(HLOD hlod)
        {
            m_activeHLODs.Remove(hlod);
        }

        private List<HLOD> m_activeHLODs = null;

        private void OnPreCull(Camera cam)
        {
            if (cam != Camera.current)
                return;

            if (m_activeHLODs == null)
                return;

            var cameraTransform = cam.transform;
            var cameraPosition = cameraTransform.position;

            float preRelative = 0.0f;
            if (cam.orthographic)
            {
                preRelative = 0.5f / cam.orthographicSize;
            }
            else
            {
                float halfAngle = Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView * 0.5F);
                preRelative = 0.5f / halfAngle;
            }
            preRelative = preRelative * QualitySettings.lodBias;

            for (int i = 0; i < m_activeHLODs.Count; ++i)
            {
                if ( m_activeHLODs[i] != null )
                    m_activeHLODs[i].UpdateCull(cam.orthographic, cameraPosition, preRelative);
            }
        }
    }

}