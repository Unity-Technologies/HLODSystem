using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLODCameraRecognizerManager
    {
        private static HLODCameraRecognizerManager s_instance; 
        public static HLODCameraRecognizerManager Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new HLODCameraRecognizerManager();
                return s_instance;
            }
        }

        public static HLODCameraRecognizer ActiveRecognizer
        {
            get
            {
                var instance = Instance;
                if (instance.m_activeRecognizer == null)
                {
                    instance.ActiveHighestPriority();
                }

                return instance.m_activeRecognizer;
            }
        }
        public static Camera ActiveCamera
        {
            get
            {
                var recognizer = ActiveRecognizer;
                if (recognizer == null)
                    return null;

                return recognizer.RecognizedCamera;
            }
        }

        private bool m_enableAutoHighestPrioritySetting = true;
        private HLODCameraRecognizer m_activeRecognizer = null;
        private List<HLODCameraRecognizer> m_recognizers = new List<HLODCameraRecognizer>();
        
        public void RegisterRecognizer(HLODCameraRecognizer recognizer)
        {
            m_recognizers.Add(recognizer);
            m_recognizers.Sort((lhs, rhs) =>
            {
                //sort in descending order
                return rhs.Priority - lhs.Priority;
            });
            
            if ( m_enableAutoHighestPrioritySetting )
                ActiveHighestPriority();
        }

        public void UnregisterRecognizer(HLODCameraRecognizer recognizer)
        {
            m_recognizers.Remove(recognizer);

            if (m_activeRecognizer == recognizer)
            {
                m_activeRecognizer = null;
                ActiveHighestPriority();
            }
        }

        public void Active(HLODCameraRecognizer recognizer)
        {
            m_activeRecognizer = recognizer;
            m_enableAutoHighestPrioritySetting = false;
        }

        public void Active(int id)
        {
            for (int i = 0; i < m_recognizers.Count; ++i)
            {
                if (m_recognizers[i].ID == id)
                {
                    Active(m_recognizers[i]);
                    return;
                }
            }
        }

        public void ActiveHighestPriority()
        {
            if (m_recognizers.Count > 0)
            {
                m_activeRecognizer = m_recognizers[0];
            }
        }

        public void EnableAutoHighestPrioritySetting(bool enable)
        {
            m_enableAutoHighestPrioritySetting = enable;
            
            if ( enable )
                ActiveHighestPriority();
        }

    }
}