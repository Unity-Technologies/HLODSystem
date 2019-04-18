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
            if ( m_activeHLODs != null)
                m_activeHLODs.Remove(hlod);
        }

        private List<HLOD> m_activeHLODs = null;

        private void OnPreCull(Camera cam)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying == false)
            {
                if (SceneView.currentDrawingSceneView == null)
                    return;
                if (cam != SceneView.currentDrawingSceneView.camera)
                    return;
            }
            else
            {
                if (cam != Camera.main)
                    return;
            }
#else
            if (cam != Camera.main)
                return;
#endif

            if (m_activeHLODs == null)
                return;

            for (int i = 0; i < m_activeHLODs.Count; ++i)
            {
                m_activeHLODs[i].UpdateCull(cam);
            }
        }
    }

}