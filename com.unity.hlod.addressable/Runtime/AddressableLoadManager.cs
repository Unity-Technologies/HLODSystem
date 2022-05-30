using System;
using System.Collections.Generic;
using Unity.HLODSystem.Streaming;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#region POC_ADDRESSABLE_SCENE_STREAMING
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
#endregion

namespace Unity.HLODSystem
{
    
    public class AddressableLoadManager : MonoBehaviour
    {
        public class Handle
        {
            public event Action<Handle> Completed;

#region POC_ADDRESSABLE_SCENE_STREAMING
            public Handle(AddressableHLODController controller, string address, int priority, float distance, bool isScene)
            {
                m_controller = controller;
                m_address = address;
                m_priority = priority;
                m_distance = distance;

                // added a boolean to separate the case where the loading target is a scene
                m_IsScene = isScene;
            }
#endregion            

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

#region POC_ADDRESSABLE_SCENE_STREAMING
                    if (m_IsScene)
                    {
                        return m_asyncSceneHandle.Status;
                    }
                    else
                    {
                        return m_asyncHandle.Status;
                    }
#endregion
                }
            }

            public GameObject Result
            {
                get { return m_asyncHandle.Result; }
            }

#region POC_ADDRESSABLE_SCENE_STREAMING
            public Scene ResultScene
            {
                get { return m_asyncSceneHandle.Result.Scene; }
            }

            public bool IsScene
            {
                get { return m_IsScene; }
            }
#endregion            
            
            public void Start()
            {
                m_startLoad = true;

#region POC_ADDRESSABLE_SCENE_STREAMING
                if (m_IsScene)
                {
                    m_asyncSceneHandle = Addressables.LoadSceneAsync(m_address, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                }
                else
                {
                    m_asyncHandle = Addressables.LoadAssetAsync<GameObject>(m_address);
                }

                if (m_IsScene)
                {
                    m_asyncSceneHandle.Completed += handle =>
                    {
                        Completed?.Invoke(this);
                    };
                }
                else
                {
                    m_asyncHandle.Completed += handle =>
                    {
                        Completed?.Invoke(this);
                    };                    
                }
#endregion
            }

            public void Stop()
            {
                if (m_startLoad == true)
                {
#region POC_ADDRESSABLE_SCENE_STREAMING
                    if (m_IsScene)
                    {
                        Addressables.Release(m_asyncSceneHandle);
                    }
                    else
                    {
                        Addressables.Release(m_asyncHandle);
                    }
#endregion
                }
            }


            private AddressableHLODController m_controller;
            private string m_address;
            private int m_priority;
            private float m_distance;
            private bool m_startLoad = false;
            private AsyncOperationHandle<GameObject> m_asyncHandle;

#region POC_ADDRESSABLE_SCENE_STREAMING
            private bool m_IsScene = false;
            private AsyncOperationHandle<SceneInstance> m_asyncSceneHandle;
#endregion
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
#region POC_ADDRESSABLE_SCENE_STREAMING
            Handle handle = new Handle(controller, address, priority, distance, false);
#endregion

            InsertHandle(handle);
            return handle;
        }

        public void UnloadAsset(Handle handle)
        {
            m_loadQueue.Remove(handle);
            handle.Stop();
        }

#region POC_ADDRESSABLE_SCENE_STREAMING
        public Handle LoadScene(AddressableHLODController controller, string address, int priority, float distance)
        {
            Handle handle = new Handle(controller, address, priority, distance, true);
            InsertHandle(handle);
            return handle;
        }        

        public void UnloadScene(Handle handle)
        {
            m_loadQueue.Remove(handle);
            handle.Stop();            
        }
#endregion

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