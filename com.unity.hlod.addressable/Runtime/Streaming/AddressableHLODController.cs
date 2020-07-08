using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableHLODController : HLODControllerBase
    {
        [Serializable]
        public class ChildObject
        {
            public GameObject GameObject;

            public string Address;

            public Transform Parent;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        [SerializeField]
        private List<ChildObject> m_highObjects = new List<ChildObject>();

        [SerializeField]
        private List<string> m_lowObjects = new List<string>();

        [SerializeField]
        int m_priority = 0;
        
        class LoadInfo
        {
            public GameObject GameObject;
            public AddressableLoadManager.Handle Handle;
            public List<Action<GameObject>> Callbacks;
        }
        

        private Dictionary<int, LoadInfo> m_createdHighObjects = new Dictionary<int, LoadInfo>();
        private Dictionary<int, LoadInfo> m_createdLowObjects = new Dictionary<int, LoadInfo>();

        private GameObject m_hlodMeshesRoot;
        private int m_hlodLayerIndex;

        public event Action<GameObject> HighObjectCreated;
        
        public override void OnStart()
        {
            m_hlodMeshesRoot = new GameObject("HLODMeshesRoot");
            m_hlodMeshesRoot.transform.SetParent(transform, false);

            m_hlodLayerIndex = LayerMask.NameToLayer(HLOD.HLODLayerStr);

            AddressableLoadManager.Instance.RegisterController(this);

        }

        public override void OnStop()
        {
            if ( AddressableLoadManager.Instance != null)
                AddressableLoadManager.Instance.UnregisterController(this);
        }


        public override void Install()
        {
            
            for (int i = 0; i < m_highObjects.Count; ++i)
            {
                if (string.IsNullOrEmpty(m_highObjects[i].Address) == false)
                {
                    DestoryObject(m_highObjects[i].GameObject);
                }
                else if (m_highObjects[i].GameObject != null)
                {
                    m_highObjects[i].GameObject.SetActive(false);
                }
            }
        }

        public int AddHighObject(string address, GameObject origin)
        {
            int id = m_highObjects.Count;

            ChildObject obj = new ChildObject();
            obj.GameObject = origin;
            obj.Address = address;
            obj.Parent = origin.transform.parent;
            obj.Position = origin.transform.localPosition;
            obj.Rotation = origin.transform.localRotation;
            obj.Scale = origin.transform.localScale;

            m_highObjects.Add(obj);
            return id;
        }

        public int AddHighObject(GameObject gameObject)
        {
            int id = m_highObjects.Count;

            ChildObject obj = new ChildObject();
            obj.GameObject = gameObject;

            m_highObjects.Add(obj);
            return id;
        }
        public int AddLowObject(string address)
        {
            int id = m_lowObjects.Count;
            m_lowObjects.Add(address);
            return id;
        }

        public override int HighObjectCount { get => m_highObjects.Count; }
        public override int LowObjectCount { get => m_lowObjects.Count; }

        public string GetLowObjectAddr(int index) { return m_lowObjects[index]; }

        public override void GetHighObject(int id, int level, float distance, Action<GameObject> loadDoneCallback)
        {
            //already processing object to load.
            if (m_createdHighObjects.ContainsKey(id) == true)
            {
                //already load done.
                if (m_createdHighObjects[id].GameObject != null)
                {
                    loadDoneCallback?.Invoke(m_createdHighObjects[id].GameObject);

                }
                //not finished loading yet.
                else
                {
                    m_createdHighObjects[id].Callbacks.Add(loadDoneCallback);
                }
            }
            else
            {
                if (m_highObjects[id].GameObject != null)
                {
                    LoadInfo loadInfo = new LoadInfo();
                    loadInfo.GameObject = m_highObjects[id].GameObject;
                    ChangeLayersRecursively(loadInfo.GameObject.transform, m_hlodLayerIndex);
                    loadDoneCallback?.Invoke(loadInfo.GameObject);
                    m_createdHighObjects.Add(id, loadInfo);
                }
                else
                {
                    //high object's priority is always lowest.
                    LoadInfo loadInfo = CreateLoadInfo(m_highObjects[id].Address, m_priority, distance,
                        m_highObjects[id].Parent, m_highObjects[id].Position, m_highObjects[id].Rotation, m_highObjects[id].Scale);
                    m_createdHighObjects.Add(id, loadInfo);
                    
                    loadInfo.Callbacks = new List<Action<GameObject>>();
                    loadInfo.Callbacks.Add(loadDoneCallback);
                    loadInfo.Callbacks.Add(o => { HighObjectCreated?.Invoke(o); });
                }
                
            }            
            
        }


        public override void GetLowObject(int id, int level, float distance, Action<GameObject> loadDoneCallback)
        {
            //already processing object to load.
            if (m_createdLowObjects.ContainsKey(id) == true)
            {
                //already load done.
                if (m_createdLowObjects[id].GameObject != null)
                {
                    loadDoneCallback?.Invoke(m_createdLowObjects[id].GameObject);

                }
                //not finished loading yet.
                else
                {
                    m_createdLowObjects[id].Callbacks.Add(loadDoneCallback);
                }
            }
            else
            {
                LoadInfo loadInfo = CreateLoadInfo(m_lowObjects[id], m_priority, distance, m_hlodMeshesRoot.transform, Vector3.zero, Quaternion.identity,Vector3.one);
                m_createdLowObjects.Add(id, loadInfo);

                loadInfo.Callbacks = new List<Action<GameObject>>();
                loadInfo.Callbacks.Add(loadDoneCallback);    
            }
        }

        public override void ReleaseHighObject(int id)
        {
            if (m_createdHighObjects.ContainsKey(id) == false)
                return;
            
            if (string.IsNullOrEmpty(m_highObjects[id].Address) == true)
            { 
                m_createdHighObjects[id].GameObject.SetActive(false);
            }
            else
            {
                LoadInfo info = m_createdHighObjects[id];
                DestoryObject(info.GameObject);
                AddressableLoadManager.Instance.UnloadAsset(info.Handle);
            }

            m_createdHighObjects.Remove(id);
        }
        public override void ReleaseLowObject(int id)
        {
            if (m_createdLowObjects.ContainsKey(id) == false)
                return;
            
            LoadInfo info = m_createdLowObjects[id];
            m_createdLowObjects.Remove(id);
            
            DestoryObject(info.GameObject);
            AddressableLoadManager.Instance.UnloadAsset(info.Handle);
        }

        private void DestoryObject(Object obj)
        {
#if UNITY_EDITOR
            DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }
        
        private LoadInfo CreateLoadInfo(string address, int priority, float distance, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            LoadInfo loadInfo = new LoadInfo();
            loadInfo.Handle = AddressableLoadManager.Instance.LoadAsset(this, address, priority, distance);
            loadInfo.Handle.Completed += handle =>
            {
                if (loadInfo.Handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError("Failed to load asset: " + address);
                    return;
                }
   
                GameObject gameObject = Instantiate(handle.Result, parent, false);
                gameObject.transform.localPosition = localPosition;
                gameObject.transform.localRotation = localRotation;
                gameObject.transform.localScale = localScale;
                gameObject.SetActive(false);
                ChangeLayersRecursively(gameObject.transform, m_hlodLayerIndex);
                loadInfo.GameObject = gameObject;
                
                for (int i = 0; i < loadInfo.Callbacks.Count; ++i)
                {
                    loadInfo.Callbacks[i]?.Invoke(gameObject);
                }
                loadInfo.Callbacks.Clear();
            };
            return loadInfo;
        }

        static void ChangeLayersRecursively(Transform trans, int layer)
        {
            trans.gameObject.layer = layer;
            foreach (Transform child in trans)
            {
                ChangeLayersRecursively(child, layer);
            }
        }
    }

}