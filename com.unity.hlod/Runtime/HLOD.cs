using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public class HLOD : MonoBehaviour, ISerializationCallbackReceiver
    {
        public const string HLODLayerStr = "HLOD";
        [SerializeField]
        private HLODTreeNode m_root;

        [SerializeField]
        private float m_MinSize = 30.0f;
        [SerializeField]
        private float m_LODDistance = 0.3f;
        [SerializeField]
        private float m_CullDistance = 0.01f;
        [SerializeField]
        private float m_ThresholdSize = 5.0f;

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
        private SerializableDynamicObject m_StreamingOptions = new SerializableDynamicObject();

        [SerializeField]
        private float m_SimplifyPolygonRatio = 0.8f;

        [SerializeField]
        private int m_SimplifyMinPolygonCount = 10;
        [SerializeField]
        private int m_SimplifyMaxPolygonCount = 500;

        [SerializeField]
        private float m_SimplifyThresholdSize = 5.0f;

        [SerializeField]
        private List<Object> m_generatedObjects = new List<Object>();

        private ISpaceManager m_spaceManager;
        private ActiveHLODTreeNodeManager m_activeManager;

        public float MinSize
        {
            get { return m_MinSize; }
        }

        public HLODTreeNode Root
        {
            set { m_root = value; }
            get { return m_root; }
        }

        public float LODDistance
        {
            get { return m_LODDistance; }
        }
        public float CullDistance
        {
            set { m_CullDistance = value; }
            get { return m_CullDistance; }
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

        public SerializableDynamicObject StreamingOptions
        {
            get { return m_StreamingOptions; }
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

        

        void Awake()
        {
            m_spaceManager = new QuadTreeSpaceManager(this);
            m_activeManager = new ActiveHLODTreeNodeManager();
        }

        void Start()
        {
            ControllerBase controller = GetComponent<ControllerBase>();
            m_root.Initialize(controller, m_spaceManager, m_activeManager);
            m_activeManager.Activate(m_root);
        }

        void OnEnable()
        {
            HLODManager.Instance.RegisterHLOD(this);
        }

        void OnDisable()
        {
            HLODManager.Instance.UnregisterHLOD(this);
        }
        private void OnDestroy()
        {
            HLODManager.Instance.UnregisterHLOD(this);
        }


#if UNITY_EDITOR
        public List<Object> GeneratedObjects
        {
            get { return m_generatedObjects; }
        }

        public void StartUseInEditor()
        {
            var controller = GetComponent<ControllerBase>();
            if (controller == null)
                return;

            Awake();
            Start();

            controller.OnStart();
        }

        public void StopUseInEditor()
        {
            var controller = GetComponent<ControllerBase>();
            if (controller == null)
                return;

            controller.OnStop();

            m_root.Cull();
            m_spaceManager = null;
            m_activeManager = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (UnityEditor.Selection.activeGameObject == gameObject && m_root != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(m_root.Bounds.center, m_root.Bounds.size);
            }
        }
#endif

        Bounds CalcLocalBounds(Renderer renderer)
        {
            Bounds bounds = renderer.bounds;
            bounds.center -= transform.position;

            return bounds;
        }
        public Bounds GetBounds()
        {
            Bounds ret = new Bounds();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                ret.center = Vector3.zero;
                ret.size = Vector3.zero;
                return ret;
            }

            Bounds bounds = CalcLocalBounds(renderers[0]);
            for (int i = 1; i < renderers.Length; ++i)
            {
                bounds.Encapsulate(CalcLocalBounds(renderers[i]));
            }

            ret.center = bounds.center;
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            ret.size = new Vector3(max, max, max);  

            return ret;
        }

        public void UpdateCull(Camera camera)
        {
            if (m_spaceManager == null)
                return;

            m_spaceManager.UpdateCamera(camera);

            if (m_spaceManager.IsCull(m_root.Bounds) == true)
            {
                m_root.Cull();
            }
            else
            {
                m_activeManager.UpdateActiveNodes();
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
    }

}