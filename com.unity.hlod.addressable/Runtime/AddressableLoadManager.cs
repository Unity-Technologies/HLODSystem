using System;
using System.Collections.Generic;
using Unity.HLODSystem.Streaming;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;



namespace Unity.HLODSystem
{
    
    public class AddressableLoadManager : MonoBehaviour
    {
        public class Handle
        {
            public event Action<Handle> Completed;
            public Handle(AddressableHLODController controller, string address, int priority, float distance)
            {
                m_controller = controller;
                m_address = address;
                m_priority = priority;
                m_distance = distance;
            }

            public string Address => m_address;

            public int Priority
            {
                get { return m_priority; }
            }

            public float Distance
            {
                get { return m_distance; }
            }

            public AddressableHLODController Controller
            {
                get { return m_controller; }
            }
            
            public AsyncOperationStatus Status
            {
                get
                {
                    if (m_startLoad == false)
                    {
                        return AsyncOperationStatus.None;
                    }
                    return m_asyncHandle.Status;
                }
            }

            public GameObject Result
            {
                get { return m_asyncHandle.Result; }
            }
            
            public void Start()
            {
                m_startLoad = true;
                m_asyncHandle = Addressables.LoadAssetAsync<GameObject>(m_address);
                m_asyncHandle.Completed += handle =>
                {
                    Completed?.Invoke(this);
                };
            }

            public void Stop()
            {
                if (m_startLoad == true)
                {
                    Addressables.Release(m_asyncHandle);
                }
            }


            private AddressableHLODController m_controller;
            private string m_address;
            private int m_priority;
            private float m_distance;
            private bool m_startLoad = false;

            private AsyncOperationHandle<GameObject> m_asyncHandle;
        }
        #region Singleton
        private static AddressableLoadManager s_instance;
        private static bool s_isDestroyed = false;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnLoad()
        {
            s_instance = null;
            s_isDestroyed = false;
        }
        public static AddressableLoadManager Instance
        {
            get
            {
                if (s_isDestroyed)
                    return null;
                
                if (s_instance == null)
                {
                    GameObject go = new GameObject("AddressableLoadManager");
                    s_instance = go.AddComponent<AddressableLoadManager>();
                    DontDestroyOnLoad(go);
                }

                return s_instance;
            }
        }
        #endregion


        private bool m_isLoading = false;
        private LinkedList<Handle> m_loadQueue = new LinkedList<Handle>();

        private void OnDestroy()
        {
            s_isDestroyed = true;
        }

        public void RegisterController(AddressableHLODController controller)
        {
        }

        public void UnregisterController(AddressableHLODController controller)
        {
            var node = m_loadQueue.First;
            while (node != null)
            {
                if (node.Value.Controller == controller)
                {
                    var remove = node;
                    node = node.Next;
                    m_loadQueue.Remove(remove);
                }
                else
                {
                    node = node.Next;
                }
            }

        }

        public Handle LoadAsset(AddressableHLODController controller, string address, int priority, float distance)
        {
            Handle handle = new Handle(controller, address, priority, distance);
            InsertHandle(handle);
            return handle;
        }

        public void UnloadAsset(Handle handle)
        {
            m_loadQueue.Remove(handle);
            handle.Stop();
        }

        private void InsertHandle(Handle handle)
        {
            var node = m_loadQueue.First;
            while (node != null && node.Value.Priority < handle.Priority)
            {
                node = node.Next;
            }

            while (node != null && node.Value.Priority == handle.Priority && node.Value.Distance < handle.Distance)
            {
                node = node.Next;
            }

            if (node == null)
            {
                if (m_isLoading == true)
                {
                    m_loadQueue.AddLast(handle);
                }
                else
                {
                    StartLoad(handle);
                }
            }
            else
            {
                m_loadQueue.AddBefore(node, handle);
            }
        }

        private void StartLoad(Handle handle)
        {
            handle.Completed += handle1 =>
            {
                m_isLoading = false;
                if (m_loadQueue.Count > 0)
                {
                    Handle nextHandle = m_loadQueue.First.Value;
                    m_loadQueue.RemoveFirst();
                    StartLoad(nextHandle);
                }
            };
            m_isLoading = true;
            handle.Start();
        }
   
    }
}