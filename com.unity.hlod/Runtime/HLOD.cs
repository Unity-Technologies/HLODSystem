using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLOD : MonoBehaviour, ISerializationCallbackReceiver
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
        private float m_ThresholdSize = 5.0f;

        [SerializeField]
        private GameObject m_HighRoot;
        [SerializeField]
        private GameObject m_LowRoot;

        private Streaming.ControllerBase m_HighController;
        private Streaming.ControllerBase m_LowController;

        private Type m_BatcherType;
        private Type m_SimplifierType;
        private Type m_StreamingType;

        [SerializeField]
        private string m_BatcherTypeStr;        //< unity serializer is not support serialization with System.Type
                                                //< So, we should convert to string to store value.
        [SerializeField]
        private string m_SimplifierTypeStr;

        [SerializeField]
        private string m_StreamingTypeStr;

        [SerializeField]
        private SerializableDynamicObject m_BatcherOptions = new SerializableDynamicObject();

        [SerializeField]
        private float m_SimplifyPolygonRatio = 0.8f;

        [SerializeField]
        private int m_SimplifyMinPolygonCount = 10;
        [SerializeField]
        private int m_SimplifyMaxPolygonCount = 500;

        [SerializeField]
        private float m_SimplifyThresholdSize = 5.0f;


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
            set
            {
                m_HighRoot = value;
                if (m_HighRoot != null)
                    m_HighController = m_HighRoot.GetComponent<Streaming.ControllerBase>();
            }
            get { return m_HighRoot; }
        }

        public GameObject LowRoot
        {
            set
            {
                m_LowRoot = value;
                if (m_LowRoot != null)
                    m_LowController = m_LowRoot.GetComponent<Streaming.ControllerBase>();
            }
            get { return m_LowRoot; }
        }

        public Type BatcherType
        {
            set { m_BatcherType = value; }
            get { return m_BatcherType; }
        }

        public Type SimplifierType
        {
            set { m_SimplifierType = value; }
            get { return m_SimplifierType; }
        }

        public Type StreamingType
        {
            set { m_StreamingType = value; }
            get { return m_StreamingType; }
        }

        public SerializableDynamicObject BatcherOptions
        {
            get { return m_BatcherOptions; }
        }

        public float SimplifyPolygonRatio
        {
            set { m_SimplifyPolygonRatio = value; }
            get { return m_SimplifyPolygonRatio; }
        }

        public int SimplifyMinPolygonCount
        {
            set { m_SimplifyMinPolygonCount = value; }
            get { return m_SimplifyMinPolygonCount; }
        }

        public int SimplifyMaxPolygonCount
        {
            set { m_SimplifyMaxPolygonCount = value; }
            get { return m_SimplifyMaxPolygonCount; }
        }

        public float ThresholdSize
        {
            set { m_ThresholdSize = value; }
            get { return m_ThresholdSize; }
        }

        public Bounds Bounds
        {
            set{ m_Bounds = value; }
            get{ return m_Bounds; }
        }

        void Awake()
        {
            //Set default state
            if (m_LowRoot != null)
                m_LowRoot.SetActive(true);

            if (m_HighRoot != null)
                m_HighRoot.SetActive(false);
        }

        void OnEnable()
        {
            if (s_ActiveHLODs == null)
            {
                s_ActiveHLODs = new List<HLOD>();
                Camera.onPreCull += OnPreCull;
            }

            s_ActiveHLODs.Add(this);
            UpdateController();

            if (m_LowController != null)
            {
                m_LowController.Prepare();
            }
        }

        void OnDisable()
        {
            s_ActiveHLODs.Remove(this);

            if ( m_LowController != null)
                m_LowController.Hide();
            if (m_HighController != null)
                m_HighController.Show();
        }

        public void EnableAll()
        {
            var hlods = GetComponentsInChildren<HLOD>(true);
            for (int i = 0; i < hlods.Length; ++i)
            {
                hlods[i].enabled = true;
                hlods[i].UpdateController();

                if ( hlods[i].m_LowRoot != null )
                    hlods[i].m_LowRoot.SetActive(true);
                if ( hlods[i].m_HighRoot != null )
                    hlods[i].m_HighRoot.SetActive(false);

                if ( hlods[i].m_LowController != null )
                    hlods[i].m_LowController.Enable();
                if (hlods[i].m_HighController != null)
                    hlods[i].m_HighController.Enable();
            }
        }

        public void DisableAll()
        {
            var hlods = GetComponentsInChildren<HLOD>(true);
            for (int i = 0; i < hlods.Length; ++i)
            {
                hlods[i].enabled = false;
                hlods[i].UpdateController();

                if ( hlods[i].m_LowRoot != null )
                    hlods[i].m_LowRoot.SetActive(false);
                if ( hlods[i].m_HighRoot != null )
                    hlods[i].m_HighRoot.SetActive(true);

                if ( hlods[i].m_LowController != null )
                    hlods[i].m_LowController.Disable();
                if (hlods[i].m_HighController != null)
                    hlods[i].m_HighController.Disable();
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
            var renderers = GetComponentsInChildren<Renderer>();
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
            if (cam != Camera.main)
                return;

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

                curHlod.UpdateController();

                if (cam.orthographic == false)
                    distance = Vector3.Distance(curHlod.m_Bounds.center, cameraPosition);
                float relativeHeight = curHlod.m_Bounds.size.x * preRelative / distance;

                if (relativeHeight > curHlod.m_LODDistance)
                {
                    if (curHlod.m_HighController.IsShow() == false)
                    {
                        if (curHlod.m_HighController.IsReady())
                        {
                            curHlod.m_HighController.Show();
                            curHlod.m_LowController.Hide();
                        }
                        else
                        {
                            curHlod.m_HighController.Prepare();
                        }
                    }
                    else if (curHlod.m_LowController.IsShow() == true)
                    {
                        curHlod.m_LowController.Hide();
                    }
                }
                else if (relativeHeight > curHlod.m_CullDistance)
                {
                    if (curHlod.m_LowController.IsShow() == false)
                    {
                        if (curHlod.m_LowController.IsReady())
                        {
                            curHlod.m_LowController.Show();
                            curHlod.m_HighController.Hide();
                        }
                        else
                        {
                            curHlod.m_LowController.Prepare();
                        }
                    }
                    else if (curHlod.m_HighController.IsShow() == true)
                    {
                        curHlod.m_HighController.Hide();
                    }
                }
                else
                {
                    curHlod.m_LowController.Hide();
                    curHlod.m_HighController.Hide();
                }
            }
        }

        public void OnBeforeSerialize()
        {
            if ( m_BatcherType != null )
                m_BatcherTypeStr = m_BatcherType.AssemblyQualifiedName;
            if (m_SimplifierType != null)
                m_SimplifierTypeStr = m_SimplifierType.AssemblyQualifiedName;
            if (m_StreamingType != null)
                m_StreamingTypeStr = m_StreamingType.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_BatcherTypeStr))
            {
                m_BatcherType = null;
            }
            else
            {
                m_BatcherType = Type.GetType(m_BatcherTypeStr);
            }

            if (string.IsNullOrEmpty(m_SimplifierTypeStr))
            {
                m_SimplifierType = null;
            }
            else
            {
                m_SimplifierType = Type.GetType(m_SimplifierTypeStr);
            }

            if (string.IsNullOrEmpty(m_StreamingTypeStr))
            {
                m_StreamingType = null;
            }
            else
            {
                m_StreamingType = Type.GetType(m_StreamingTypeStr);
            }
            
        }

        private void UpdateController()
        {
            
            if (m_HighController == null)
            {
                if (m_HighRoot != null)
                {
                    m_HighController = m_HighRoot.GetComponent<Streaming.ControllerBase>();
                }

                if ( m_HighController != null )
                    m_HighController.Hide();
            }

            if (m_LowController == null)
            {
                if (m_LowRoot != null)
                {
                    m_LowController = m_LowRoot.GetComponent<Streaming.ControllerBase>();
                }

                if (m_LowController != null)
                    m_LowController.Show();
            }
        }

    }

}