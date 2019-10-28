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


        class LoadInfo
        {
            public GameObject GameObject;
            public AddressableLoadManager.Handle Handle;
            public List<Action<GameObject>> Callbacks;
        }
        

        private Dictionary<int, LoadInfo> m_createdHighObjects = new Dictionary<int, LoadInfo>();
        private Dictionary<int, LoadInfo> m_createdLowObjects = new Dictionary<int, LoadInfo>();

        private GameObject m_hlodMeshesRoot;

        public event Action<GameObject> HighObjectCreated;
        
        public override void OnStart()
        {
            m_hlodMeshesRoot = new GameObject("HLODMeshesRoot");
            m_hlodMeshesRoot.transform.SetParent(transform, false);

            AddressableLoadManager.Instance.RegisterController(this);

        }

        public override void OnStop()
        {
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

        private void GetHighObjectImpl(int id, int level, float distance, Action<GameObject> callback)
        {
            int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
            if (layer < 0 || layer > 31)
                layer = 0;
            
            LoadInfo loadInfo = new LoadInfo();
            m_createdHighObjects.Add(id, loadInfo);

            if (m_highObjects[id].GameObject != null)
            {
                loadInfo.GameObject = m_highObjects[id].GameObject;
                ChangeLayersRecursively(loadInfo.GameObject.transform, layer);
                callback?.Invoke(loadInfo.GameObject);
            }
            else
            {
                //high object's priority is always lowest.
                loadInfo.Callbacks = new List<Action<GameObject>>();
                loadInfo.Callbacks.Add(callback);
                loadInfo.Handle =
                    AddressableLoadManager.Instance.LoadAsset(this, m_highObjects[id].Address, Int32.MaxValue,
                        distance);
                loadInfo.Handle.Completed += handle =>
                {
                    if (loadInfo.Handle.Status == AsyncOperationStatus.Failed)
                    {
                        Debug.LogError("Failed to load asset: " + m_highObjects[id].Address);
                        return;
                    }

                    GameObject gameObject = Instantiate(handle.Result, m_highObjects[id].Parent.transform, true);
                    gameObject.transform.localPosition = m_highObjects[id].Position;
                    gameObject.transform.localRotation = m_highObjects[id].Rotation;
                    gameObject.transform.localScale = m_highObjects[id].Scale;
                    gameObject.SetActive(false);
                    ChangeLayersRecursively(gameObject.transform, layer);

                    loadInfo.GameObject = gameObject;
                    HighObjectCreated?.Invoke(gameObject);
                    
                    for (int i = 0; i < loadInfo.Callbacks.Count; ++i)
                    {
                        loadInfo.Callbacks[i]?.Invoke(gameObject);
                    }
                    loadInfo.Callbacks.Clear();
                };
                
            }

        }

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
                GetHighObjectImpl(id, level, distance, loadDoneCallback);    
            }            
            
        }

        public void GetLowObjectImpl(int id, int level, float distance, Action<GameObject> callback)
        {
            int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
            if (layer < 0 || layer > 31)
                layer = 0;

            LoadInfo loadInfo = new LoadInfo();
            m_createdLowObjects.Add(id, loadInfo);

            loadInfo.Callbacks = new List<Action<GameObject>>();
            loadInfo.Callbacks.Add(callback);
            
            loadInfo.Handle = AddressableLoadManager.Instance.LoadAsset(this, m_lowObjects[id], level, distance);
            loadInfo.Handle.Completed += handle =>
            {
                if (loadInfo.Handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError("Failed to load asset: " + m_lowObjects[id]);
                    return;
                }
                
                
                GameObject go = Instantiate(loadInfo.Handle.Result, m_hlodMeshesRoot.transform, false);
                go.SetActive(false);
                ChangeLayersRecursively(go.transform, layer);
                loadInfo.GameObject = go;
                
                for (int i = 0; i < loadInfo.Callbacks.Count; ++i)
                {
                    loadInfo.Callbacks[i]?.Invoke(go);
                }
                loadInfo.Callbacks.Clear();
            };

      
            
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
                GetLowObjectImpl(id, level, distance, loadDoneCallback);    
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