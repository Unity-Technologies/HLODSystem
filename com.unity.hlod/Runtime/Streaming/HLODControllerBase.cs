using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using UnityEngine;


namespace Unity.HLODSystem.Streaming
{
    using ControllerID = Int32;
    public abstract class HLODControllerBase : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region Interface
        public abstract void Install();


        public abstract void OnStart();
        public abstract void OnStop();

        public abstract int HighObjectCount { get; }
        public abstract int LowObjectCount { get; }

        //This should be a coroutine.
        public abstract void GetHighObject(ControllerID id, int level, float distance, Action<GameObject> loadDoneCallback);
        public abstract void GetLowObject(ControllerID id, int level, float distance, Action<GameObject> loadDoneCallback);

        public abstract void ReleaseHighObject(ControllerID id);
        public abstract void ReleaseLowObject(ControllerID id);
        #endregion

        #region Unity Events
        public void Awake()
        {
            m_spaceManager = new QuadTreeSpaceManager();
        }

        public void Start()
        {
            m_root.Initialize(this, m_spaceManager, null);
            OnStart();
        }

        public void OnEnable()
        {
            HLODManager.Instance.Register(this);
        }

        public void OnDisable()
        {
            HLODManager.Instance.Unregister(this);
        }

        public void OnDestroy()
        {
            OnStop();
            HLODManager.Instance.Unregister(this);
            m_spaceManager = null;
            m_root = null;
        }
        public void OnRenderObject()
        {
            if (m_runtimeDebug == false)
                return;

            m_root.RenderBounds();
        }
        #endregion

        #region Method
        public void UpdateCull(Camera camera)
        {
            if (m_spaceManager == null)
                return;

            m_spaceManager.UpdateCamera(this.transform, camera);

            m_root.Cull(m_spaceManager.IsCull(m_cullDistance, m_root.Bounds));
            m_root.Update(m_lodDistance);
        }

        public bool IsLoadDone()
        {
            return Root.IsLoadDone();
        }
        public int GetNodeCount()
        {
            return m_treeNodeContainer.Count;
        }
        public int GetReadyNodeCount()
        {
            int count = 0;
            for ( int i = 0; i < m_treeNodeContainer.Count; ++i )
            {
                var node = m_treeNodeContainer.Get(i);
                if (node.IsNodeReadySelf())
                    count += 1;
            }

            return count;
        }
        #endregion

        #region variables
        private ISpaceManager m_spaceManager;

        [SerializeField] 
        private HLODTreeNodeContainer m_treeNodeContainer;
        [SerializeField]
        private HLODTreeNode m_root;

        [SerializeField] private float m_cullDistance;
        [SerializeField] private float m_lodDistance;

        [SerializeField] private bool m_runtimeDebug = false;

        public HLODTreeNodeContainer Container
        {
            set
            {
                m_treeNodeContainer = value; 
                UpdateContainer();
            }
            get { return m_treeNodeContainer; }
        }
        public HLODTreeNode Root
        {
            set
            {
                m_root = value; 
                UpdateContainer();
            }
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

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            UpdateContainer();
        }

        private void UpdateContainer()
        {
            if (m_root == null)
                return;
            
            m_root.SetContainer(m_treeNodeContainer);
        }
    }

}