using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLOD : MonoBehaviour, ISerializationCallbackReceiver
    {
        class ControllerManager
        {
            enum CurrentShow
            {
                High,
                Low,
                Cull,
            }
            private CurrentShow m_currentShow = CurrentShow.Cull;

            private HLOD m_outer;
            private Streaming.ControllerBase m_highController;
            private Streaming.ControllerBase m_lowController;

            private Coroutine m_lastRunner = null;

            public ControllerManager(HLOD outer, Streaming.ControllerBase highController, Streaming.ControllerBase lowController)
            {
                m_outer = outer;
                m_highController = highController;
                m_lowController = lowController;
            }

            public void Enable()
            {
                m_highController.Enable();
                m_lowController.Enable();

                m_highController.gameObject.SetActive(false);
                m_lowController.gameObject.SetActive(true);
            }

            public void Disable()
            {
                m_highController.Disable();
                m_lowController.Disable();

                m_highController.gameObject.SetActive(true);
                m_lowController.gameObject.SetActive(false);
            }

            public void ShowHigh()
            {
                if (m_currentShow == CurrentShow.High)
                    return;

                int a = 0;

                m_currentShow = CurrentShow.High;
                IEnumerator coroutine = SwitchShow(m_lastRunner, m_highController, m_lowController);
                m_lastRunner = m_outer.StartCoroutine(coroutine);
            }

            public void ShowLow()
            {
                if (m_currentShow == CurrentShow.Low)
                    return;

                m_currentShow = CurrentShow.Low;
                IEnumerator coroutine = SwitchShow(m_lastRunner, m_lowController, m_highController);
                m_lastRunner = m_outer.StartCoroutine(coroutine);
            }

            public void Hide()
            {
                if (m_currentShow == CurrentShow.Cull)
                    return;

                m_highController.Hide();
                m_lowController.Hide();
            }

            private IEnumerator SwitchShow(Coroutine lastRunner, Streaming.ControllerBase show, Streaming.ControllerBase hide)
            {
                //wait for finish to last coroutine
                //for synchronize active state. 
                yield return lastRunner;

                yield return show.Load();
                show.Show();
                hide.Hide();
            }
        }
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

        private ControllerManager m_controllerManager;

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
            get { return m_RecursiveGeneration; }
        }
        public float MinSize
        {
            get { return m_MinSize; }
        }

        public GameObject HighRoot
        {
            set { m_HighRoot = value; }
            get { return m_HighRoot; }
        }

        public GameObject LowRoot
        {
            set { m_LowRoot = value; }
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
        }

        void OnDisable()
        {
            s_ActiveHLODs.Remove(this);
        }

        public void EnableAll()
        {
            var hlods = GetComponentsInChildren<HLOD>(true);
            for (int i = 0; i < hlods.Length; ++i)
            {
                hlods[i].enabled = true;
                hlods[i].UpdateController();
                hlods[i].m_controllerManager?.Enable();
            }
        }

        public void DisableAll()
        {
            var hlods = GetComponentsInChildren<HLOD>(true);
            for (int i = 0; i < hlods.Length; ++i)
            {
                hlods[i].enabled = false;
                hlods[i].UpdateController();                
                hlods[i].m_controllerManager?.Disable();
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
                if (curHlod.m_controllerManager == null)
                    continue;

                if (cam.orthographic == false)
                    distance = Vector3.Distance(curHlod.m_Bounds.center, cameraPosition);
                float relativeHeight = curHlod.m_Bounds.size.x * preRelative / distance;

                if (relativeHeight > curHlod.m_LODDistance)
                {
                    curHlod.m_controllerManager.ShowHigh();
                }
                else if (relativeHeight > curHlod.m_CullDistance)
                {
                    curHlod.m_controllerManager.ShowLow();
                }
                else
                {
                    curHlod.m_controllerManager.Hide();
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
            if (m_controllerManager != null)
                return;

            Streaming.ControllerBase highController = null;
            Streaming.ControllerBase lowController = null;

            if (m_HighRoot != null)
            {
                highController = m_HighRoot.GetComponent<Streaming.ControllerBase>();
            }

            if (m_LowRoot != null)
            {
                lowController = m_LowRoot.GetComponent<Streaming.ControllerBase>();
            }

            if (highController == null || lowController == null)
                return;

            m_controllerManager = new ControllerManager(this, highController, lowController);
        }

    }

}