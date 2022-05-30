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

#region POC_ADDRESSABLE_SCENE_STREAMING
        [Serializable]
        public class HighSceneData
        {
            public string _Address;
            public List<GameObject> _InputGameObjectList = new List<GameObject>();
        }

        [SerializeField]
        private List<HighSceneData> m_HighSceneDataList = new List<HighSceneData>();
#endregion

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
#region POC_ADDRESSABLE_SCENE_STREAMING
            // hide all input gameobjects
            foreach (var highSceneData in m_HighSceneDataList)
            {
                foreach (var inputGO in highSceneData._InputGameObjectList)
                {
                    inputGO.SetActive(false);
                }
            }

            // hide all input gameobjects
            foreach (var highObject in m_highObjects)
            {
                if (!string.IsNullOrEmpty(highObject.Address) && highObject.GameObject != null && highObject.GameObject.scene.IsValid())
                {
                    highObject.GameObject.SetActive(false);                    
                }
            }
#endregion

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
#region POC_ADDRESSABLE_SCENE_STREAMING
            for (int i = 0; i < m_HighSceneDataList.Count; ++i)
            {
                if (string.IsNullOrEmpty(m_HighSceneDataList[i]._Address) == false)
                {
                    foreach (var go in m_HighSceneDataList[i]._InputGameObjectList)
                    {
                        DestoryObject(go);
                    }

                    m_HighSceneDataList[i]._InputGameObjectList.Clear();
                    m_HighSceneDataList[i]._InputGameObjectList.Capacity = 0;
                }
                else
                {
                    Debug.LogError("???");
                }
            }
#endregion

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

#region POC_ADDRESSABLE_SCENE_STREAMING
        public int AddHighScene(string address, List<GameObject> inputGameObjectList)
        {
            int id = m_HighSceneDataList.Count;
            HighSceneData highSceneData = new HighSceneData();
            highSceneData._Address = address;
            highSceneData._InputGameObjectList.AddRange(inputGameObjectList);
            m_HighSceneDataList.Add(highSceneData);
            
            return id;
        }
#endregion

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

#if UNITY_EDITOR
#endif

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
#region POC_ADDRESSABLE_SCENE_STREAMING
                if (!string.IsNullOrEmpty(m_HighSceneDataList[id]._Address))
                {          
                    LoadInfo loadInfo = CreateSceneLoadInfo(m_HighSceneDataList[id]._Address, m_priority, distance);
                    m_createdHighObjects.Add(id, loadInfo);
                    
                    loadInfo.Callbacks = new List<Action<GameObject>>();
                    loadInfo.Callbacks.Add(loadDoneCallback);
                    loadInfo.Callbacks.Add(o => { HighObjectCreated?.Invoke(o); });                              
                }
                else
                {
                    Debug.LogError("?????");
                }
#endregion
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
            
#region POC_ADDRESSABLE_SCENE_STREAMING
            if (!string.IsNullOrEmpty(m_HighSceneDataList[id]._Address))
            {
                LoadInfo info = m_createdHighObjects[id];
                DestoryObject(info.GameObject);
                AddressableLoadManager.Instance.UnloadScene(info.Handle);
            }
            else
            {
                Debug.LogError("???");
            }
#endregion

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

#region POC_ADDRESSABLE_SCENE_STREAMING
        private LoadInfo CreateSceneLoadInfo(string address, int priority, float distance)
        {
            LoadInfo loadInfo = new LoadInfo();
            loadInfo.Handle = AddressableLoadManager.Instance.LoadScene(this, address, priority, distance);
            loadInfo.Handle.Completed += handle =>
            {
                if (loadInfo.Handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError("Failed to load asset: " + address);
                    return;
                }
   
                var scene = handle.ResultScene;
                GameObject gameObject = scene.GetRootGameObjects()[0];
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
#endregion

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