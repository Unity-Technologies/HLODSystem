using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using UnityEngine;


namespace Unity.HLODSystem.Streaming
{
    using ControllerID = Int32;
    public abstract class ControllerBase : MonoBehaviour
    {
        #region Interface
        public abstract void Install();


        public abstract void OnStart();
        public abstract void OnStop();

        //This should be a coroutine.
        public abstract IEnumerator GetHighObject(ControllerID id, Action<GameObject> callback);

        public abstract IEnumerator GetLowObject(ControllerID id, Action<GameObject> callback);

        public abstract void ReleaseHighObject(ControllerID id);
        public abstract void ReleaseLowObject(ControllerID id);
        #endregion

        #region Unity Events
        void Awake()
        {
            m_spaceManager = new QuadTreeSpaceManager();
            m_activeManager = new ActiveHLODTreeNodeManager();
        }

        void Start()
        {
            ControllerBase controller = GetComponent<ControllerBase>();
            m_root.Initialize(controller, m_spaceManager, m_activeManager);
            m_activeManager.Activate(m_root);

            OnStart();
        }
        void OnEnable()
        {
            HLODManager.Instance.Register(this);
        }

        void OnDisable()
        {
            HLODManager.Instance.Unregister(this);
        }

        void OnDestroy()
        {
            HLODManager.Instance.Unregister(this);
        }
        #endregion
        
        #region Method
        public void UpdateCull(Camera camera)
        {
            if (m_spaceManager == null)
                return;

            m_spaceManager.UpdateCamera(this.transform, camera);

            if (m_spaceManager.IsCull(m_cullDistance, m_root.Bounds) == true)
            {
                m_root.Cull();
            }
            else
            {
                m_activeManager.UpdateActiveNodes(m_lodDistance);
            }
            
         
        }
        #endregion
 
        #region variables
        private ISpaceManager m_spaceManager;
        private ActiveHLODTreeNodeManager m_activeManager;
        
        [SerializeField]
        private HLODTreeNode m_root;

        [SerializeField] private float m_cullDistance;
        [SerializeField] private float m_lodDistance;
        
        public HLODTreeNode Root
        {
            set { m_root = value; }
            get { return m_root; }
        }

        public float CullDistance
        {
            set { m_cullDistance = value; }
            get { return m_cullDistance; }
        }

        public float LODDistance
        {
            set { m_lodDistance = value; }
            get { return m_lodDistance; }
        }
        #endregion
        

    }

}