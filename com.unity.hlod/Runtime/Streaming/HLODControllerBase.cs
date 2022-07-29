using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using UnityEngine;


namespace Unity.HLODSystem.Streaming
{
    public abstract class HLODControllerBase : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region Interface

        //This method is only used during creation.
        //Because the GameObject may have been deleted in Runtime, it does not work.
        //So, explicitly make it available only in the Editor.
        #if UNITY_EDITOR
        public abstract GameObject GetHighSceneObject(int id);
        #endif
        
        public abstract void Install();


        public abstract void OnStart();
        public abstract void OnStop();

        public abstract int HighObjectCount { get; }
        public abstract int LowObjectCount { get; }

        public abstract void LoadHighObject(int id,Action<GameObject> loadDoneCallback);
        public abstract void LoadLowObject(int id, Action<GameObject> loadDoneCallback);
        
        public abstract void UnloadHighObject(int id);
        public abstract void UnloadLowObject(int id);
        
        #endregion
        
        #region Unity Events
        public void Awake()
        {
            m_spaceManager = new QuadTreeSpaceManager();
        }

        public void Start()
        {
            m_root.Initialize(this, m_spaceManager, null);
            LoadManager.Instance.RegisterController(this);
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
            LoadManager.Instance.UnregisterController(this);
            HLODManager.Instance.Unregister(this);
            m_spaceManager = null;
            m_root = null;
        }
        public void OnRenderObject()
        {
            if (m_runtimeDebug == false)
                return;

            m_root.RenderBounds(transform);
        }
        #endregion

        #region Method
        class LoadInfo
        {
            public LoadManager.Handle Handle;
            public List<Action<LoadManager.Handle>> Callbacks = new List<Action<LoadManager.Handle>>();

            public void LoadDone(LoadManager.Handle handle)
            {
                foreach (var callback in Callbacks)
                {
                    callback?.Invoke(handle);
                }
                Callbacks.Clear();
            }
        }
        
        private Dictionary<int, LoadInfo> m_createdHighObjects = new Dictionary<int, LoadInfo>();
        private Dictionary<int, LoadInfo> m_createdLowObjects = new Dictionary<int, LoadInfo>();

        public LoadManager.Handle GetHighObject(int id, int level, float distance, Action<LoadManager.Handle> loadDoneCallback)
        {
            LoadInfo loadInfo = null;
            //already processing object to load.
            if (m_createdHighObjects.TryGetValue(id, out loadInfo) == true)
            {
                //already load done.
                if (loadInfo.Handle.LoadedObject != null)
                {
                    loadDoneCallback?.Invoke(loadInfo.Handle);
                }
                //not finished loading yet.
                else
                {
                    loadInfo.Callbacks.Add(loadDoneCallback);
                }
            }
            else
            {
                loadInfo = new LoadInfo();
                m_createdHighObjects[id] = loadInfo;
                loadInfo.Callbacks.Add(loadDoneCallback);
                loadInfo.Handle = LoadManager.Instance.LoadHighObject(this, id, level, distance, loadInfo.LoadDone);
            }

            return loadInfo.Handle;
        }

        public LoadManager.Handle GetLowObject(int id, int level, float distance, Action<LoadManager.Handle> loadDoneCallback)
        {
            LoadInfo loadInfo = null;
            //already processing object to load.
            if (m_createdLowObjects.TryGetValue(id, out loadInfo) == true)
            {
                //already load done.
                if (loadInfo.Handle.LoadedObject != null)
                {
                    loadDoneCallback?.Invoke(loadInfo.Handle);
                }
                //not finished loading yet.
                else
                {
                    loadInfo.Callbacks.Add(loadDoneCallback);
                }
            }
            else
            {
                loadInfo = new LoadInfo();
                m_createdLowObjects[id] = loadInfo;
                loadInfo.Callbacks.Add(loadDoneCallback);
                loadInfo.Handle = LoadManager.Instance.LoadLowObject(this, id, level, distance, loadInfo.LoadDone);
            }

            return loadInfo.Handle;
        }

        public void ReleaseHighObject(LoadManager.Handle handle)
        {
            if (m_createdHighObjects.ContainsKey(handle.Id) == false)
            {
                return;
            }

            m_createdHighObjects.Remove(handle.Id);
            LoadManager.Instance.UnloadHighObject(handle);
        }

        public void ReleaseLowObject(LoadManager.Handle handle)
        {
            if (m_createdLowObjects.ContainsKey(handle.Id) == false)
            {
                return;
            }

            m_createdLowObjects.Remove(handle.Id);
            LoadManager.Instance.UnloadLowObject(handle);
        }
        
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