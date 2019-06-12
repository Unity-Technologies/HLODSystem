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
    
        public void Register(Streaming.ControllerBase controller)
        {
            if (m_activeControllers == null)
            {
                m_activeControllers = new List<Streaming.ControllerBase>();
                Camera.onPreCull += OnPreCull;
                RenderPipeline.beginCameraRendering += OnPreCull;
            }
            m_activeControllers.Add(controller);
        }
        public void Unregister(Streaming.ControllerBase controller)
        {
            if ( m_activeControllers != null)
                m_activeControllers.Remove(controller);
        }

        private List<Streaming.ControllerBase> m_activeControllers = null;

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

            if (m_activeControllers == null)
                return;

            for (int i = 0; i < m_activeControllers.Count; ++i)
            {
                m_activeControllers[i].UpdateCull(cam);
            }
        }
    }

}