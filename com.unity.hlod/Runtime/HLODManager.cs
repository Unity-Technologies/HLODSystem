using System.Collections.Generic;
using Unity.HLODSystem.Streaming;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Unity.HLODSystem
{
    public class HLODManager
    {
        #region Singleton

        private static HLODManager s_instance = null;
        private bool IsSRP => GraphicsSettings.renderPipelineAsset != null || QualitySettings.renderPipeline != null;

        public static HLODManager Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new HLODManager();
                return s_instance;
            }
        }

        #endregion
        
       
        public List<HLODControllerBase> ActiveControllers
        {
            get
            {
                if (m_activeControllers == null)
                {
                    m_activeControllers = new List<HLODControllerBase>();
                    if (IsSRP)
                        RenderPipelineManager.beginCameraRendering += OnPreCull;
                    else
                        Camera.onPreCull += OnPreCull;
                }
                
                return m_activeControllers;
            }
        }
        public void Register(HLODControllerBase controller)
        {
            ActiveControllers.Add(controller);
        }
        public void Unregister(HLODControllerBase controller)
        {
            ActiveControllers.Remove(controller);
        }
        
        

        private List<HLODControllerBase> m_activeControllers = null;

        private void OnPreCull(ScriptableRenderContext context, Camera cam)
        {
            OnPreCull(cam);
        }

        public void OnPreCull(Camera cam)
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
                if (cam != HLODCameraRecognizerManager.ActiveCamera)
                    return;
            }
#else
            if (cam != HLODCameraRecognizer.RecognizedCamera)
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