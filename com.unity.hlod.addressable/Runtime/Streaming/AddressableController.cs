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
    public class AddressableController : ControllerBase
    {
        class LoadHandle : ILoadHandle
        {
            public LoadHandle()
            {
                
            }
            public bool MoveNext()
            {
                return m_coroutine.MoveNext();
            }

            public void Reset()
            {
                m_coroutine.Reset();
            }

            public void OnLoad(GameObject gameObject)
            {
                m_gameObject = gameObject;
            }

            public object Current
            {
                get
                {
                    return m_coroutine.Current;
                }
            }

            public GameObject Result
            {
                set => m_gameObject = value;
                get => m_gameObject;
            }


            public void Start(IEnumerator routine)
            {
                m_coroutine = new CustomCoroutine(routine);
            }
          
            private CustomCoroutine  m_coroutine;
            private GameObject m_gameObject;
        }
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

        private IEnumerator GetHighObjectImpl(int id, int level, float distance, Action<GameObject> callback)
        {
            GameObject ret = null;
            int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
            if (layer < 0 || layer > 31)
                layer = 0;

            if (m_createdHighObjects.ContainsKey(id))
            {
                //this id being loaded
                while (m_createdHighObjects[id].Handle.Status == AsyncOperationStatus.None)
                    yield return null;
                
                ret = m_createdHighObjects[id].GameObject;
            }
            else
            {
                LoadInfo loadInfo = new LoadInfo();
                m_createdHighObjects.Add(id, loadInfo);

                GameObject go = null;

                if (m_highObjects[id].GameObject != null)
                {
                    go = m_highObjects[id].GameObject;
                    ChangeLayersRecursively(go.transform, layer);
                }
                else
                {

                    //high object's priority is always lowest.
                    loadInfo.Handle = AddressableLoadManager.Instance.LoadAsset(this, m_highObjects[id].Address, Int32.MaxValue, distance);
                    yield return loadInfo.Handle;

                    if (loadInfo.Handle.Status == AsyncOperationStatus.Failed)
                        yield break;
                    
                    if ( m_createdHighObjects.ContainsKey(id) == false )
                        yield break;

                    go = (GameObject)Instantiate(loadInfo.Handle.Result);
                    go.SetActive(false);
                    go.transform.parent = m_highObjects[id].Parent.transform;
                    go.transform.localPosition = m_highObjects[id].Position;
                    go.transform.localRotation = m_highObjects[id].Rotation;
                    go.transform.localScale = m_highObjects[id].Scale;

                    ChangeLayersRecursively(go.transform, layer);
                    loadInfo.GameObject = go;

                }

                ret = go;
            }

            HighObjectCreated?.Invoke(ret);
            callback(ret);
        }

        public override ILoadHandle GetHighObject(int id, int level, float distance)
        {
            LoadHandle handle = new LoadHandle();
            handle.Start( GetHighObjectImpl(id, level, distance, o => { handle.Result = o; }));
            return handle;
        }

        public IEnumerator GetLowObjectImpl(int id, int level, float distance, Action<GameObject> callback)
        {
            GameObject ret = null;
            int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
            if (layer < 0 || layer > 31)
                layer = 0;

            if (m_createdLowObjects.ContainsKey(id))
            {
                //this id being loaded
                while (m_createdLowObjects[id].Handle.Status == AsyncOperationStatus.None)
                    yield return null;

                ret = m_createdLowObjects[id].GameObject;
            }
            else
            {
                LoadInfo loadInfo = new LoadInfo();
                m_createdLowObjects.Add(id, loadInfo);

                loadInfo.Handle = AddressableLoadManager.Instance.LoadAsset(this, m_lowObjects[id], level, distance);
                yield return loadInfo.Handle;

                if (loadInfo.Handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError("Failed to load asset");
                    yield break;
                }

                if (m_createdLowObjects.ContainsKey(id) == false)
                {
                    yield break;
                }

                GameObject go = (GameObject) Instantiate(loadInfo.Handle.Result);
                go.SetActive(false);
                go.transform.SetParent(m_hlodMeshesRoot.transform, false);
                ChangeLayersRecursively(go.transform, layer);
                loadInfo.GameObject = go;

                ret = go;
                
            }

            
            callback(ret);

            
        }

        public override ILoadHandle GetLowObject(int id, int level, float distance)
        {
            LoadHandle handle = new LoadHandle();
            handle.Start(GetLowObjectImpl(id, level, distance, o => { handle.Result = o; }));
            return handle;
        }

        public override void ReleaseHighObject(int id)
        {
            if (string.IsNullOrEmpty(m_highObjects[id].Address) == true)
            {
                if ( m_createdHighObjects[id] != null)
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