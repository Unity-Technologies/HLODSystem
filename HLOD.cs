using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    [ExecuteInEditMode]
    public class HLOD : MonoBehaviour
    {
        [SerializeField]
        private Bounds m_Bounds;

        [SerializeField]
        private bool m_RecursiveGeneration = true;
        [SerializeField]
        private float m_MinSize = 30.0f;
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

        [SerializeField]
        private SerializableDynamicObject m_BatcherOptions = new SerializableDynamicObject();

        public bool RecursiveGeneration
        {
            get{ return m_RecursiveGeneration; }
        }
        public float MinSize
        {
            get{ return m_MinSize; }
        }

        public GameObject HighRoot
        {
            set{ m_HighRoot = value; }
            get { return m_HighRoot; }
        }

        public GameObject LowRoot
        {
            set { m_LowRoot = value; }
            get { return m_LowRoot; }
        }

        public SerializableDynamicObject BatcherOptions
        {
            get { return m_BatcherOptions; }
        }

        public Bounds Bounds
        {
            set{ m_Bounds = value; }
            get{ return m_Bounds; }
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

            if (LowRoot != null)
                LowRoot.SetActive(false);
            if (HighRoot != null)
                HighRoot.SetActive(true);
        }

        public void EnableAll()
        {
            var hlods = GetComponentsInChildren<HLOD>(true);
            for (int i = 0; i < hlods.Length; ++i)
            {
                hlods[i].enabled = true;
            }
        }

        public void DisableAll()
        {
            var hlods = GetComponentsInChildren<HLOD>(true);
            for (int i = 0; i < hlods.Length; ++i)
            {
                hlods[i].enabled = false;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
            }
        }
#endif

        public void CalcBounds()
        {
            if (m_HighRoot == null)
                return;

            var renderers = m_HighRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                m_Bounds.center = Vector3.zero;
                m_Bounds.size = Vector3.zero;
                return;
            }

            UnityEngine.Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; ++i)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            m_Bounds.center = bounds.center;
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            m_Bounds.size = new Vector3(max, max, max);
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
                HLOD curHlod = s_ActiveHLODs[i];

                if (curHlod.HighRoot == null || curHlod.LowRoot == null)
                    continue;

                if (cam.orthographic == false)
                    distance = Vector3.Distance(curHlod.m_Bounds.center, cameraPosition);
                float relativeHeight = curHlod.m_Bounds.size.x * preRelative / distance;

                if (relativeHeight > curHlod.m_LODDistance)
                {
                    curHlod.HighRoot.SetActive(true);
                    curHlod.LowRoot.SetActive(false);
                }
                else if (relativeHeight > curHlod.m_CullDistance)
                {
                    curHlod.HighRoot.SetActive(false);
                    curHlod.LowRoot.SetActive(true);
                }
                else
                {
                    curHlod.HighRoot.SetActive(false);
                    curHlod.LowRoot.SetActive(false);
                }
            }
        }
    }

}