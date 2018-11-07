using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    [ExecuteInEditMode]
    public class HLOD : MonoBehaviour
    {
        [Serializable]
        public struct Bounds
        {
            public Vector3 Center;
            public float Size;
        }
        [SerializeField]
        private Bounds m_Bounds;

        [SerializeField]
        private float m_MinSize;
        [SerializeField]
        private float m_LODDistance = 0.3f;
        [SerializeField]
        private float m_CullDistance = 0.01f;
        [SerializeField]
        private float m_ThresholdSize;

        [SerializeField]
        private GameObject m_HighRoot;
        [SerializeField]
        private GameObject m_LowRoot;

        public GameObject HighRoot
        {
            set
            {
                m_HighRoot = value;
                CalcBounds();
            }
            get { return m_HighRoot; }
        }

        public GameObject LowRoot
        {
            set { m_LowRoot = value; }
            get { return m_LowRoot; }
        }


        void OnEnable()
        {
            if (s_ActiveHLODs == null)
            {
                s_ActiveHLODs = new List<HLOD>();
                Camera.onPreCull += OnPreCull;
            }

            s_ActiveHLODs.Add(this);

        }

        void OnDisable()
        {
            s_ActiveHLODs.Remove(this);
        }


        void CalcBounds()
        {
            if (m_HighRoot == null)
                return;

            var renderers = m_HighRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                m_Bounds.Center = Vector3.zero;
                m_Bounds.Size = 0.0f;
                return;
            }

            UnityEngine.Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; ++i)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            m_Bounds.Center = bounds.center;
            m_Bounds.Size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        }

        private static List<HLOD> s_ActiveHLODs;
        private static void OnPreCull(Camera cam)
        {
            if (s_ActiveHLODs == null)
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

            for (int i = 0; i < s_ActiveHLODs.Count; ++i)
            {
                float distance = 1.0f;
                if (cam.orthographic == false)
                    distance = Vector3.Distance(s_ActiveHLODs[i].m_Bounds.Center, cameraPosition);
                float relativeHeight = s_ActiveHLODs[i].m_Bounds.Size * preRelative / distance;

                if (relativeHeight > s_ActiveHLODs[i].m_LODDistance)
                {
                    s_ActiveHLODs[i].m_HighRoot.SetActive(true);
                    s_ActiveHLODs[i].m_LowRoot.SetActive(false);
                }
                else if (relativeHeight > s_ActiveHLODs[i].m_CullDistance)
                {
                    s_ActiveHLODs[i].m_HighRoot.SetActive(false);
                    s_ActiveHLODs[i].m_LowRoot.SetActive(true);
                    
                }
                else
                {
                    s_ActiveHLODs[i].m_HighRoot.SetActive(false);
                    s_ActiveHLODs[i].m_LowRoot.SetActive(false);
                }
            }
        }
    }

}