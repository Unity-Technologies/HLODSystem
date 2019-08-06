using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableController : ControllerBase
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

        

        private Dictionary<int, GameObject> m_createdHighObjects = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> m_createdLowObjects = new Dictionary<int, GameObject>();

        private GameObject m_hlodMeshesRoot;
        
        public override void OnStart()
        {

#if UNITY_EDITOR
            Install();
#endif

            m_hlodMeshesRoot = new GameObject("HLODMeshesRoot");
            m_hlodMeshesRoot.transform.SetParent(transform, false);

        }

        public override void OnStop()
        {
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

        public override IEnumerator GetHighObject(int id, Action<GameObject> callback)
        {
            GameObject ret = null;
            int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
            if (layer < 0 || layer > 31)
                layer = 0;

            if (m_createdHighObjects.ContainsKey(id))
            {
                //this id being loaded
                while (m_createdHighObjects[id] == null)
                    yield return null;

                ret = m_createdHighObjects[id];
            }
            else
            {
                //marking to this id being loaded.
                m_createdHighObjects.Add(id, null);

                GameObject go = null;

                if (m_highObjects[id].GameObject != null)
                {
                    go = m_highObjects[id].GameObject;
                    ChangeLayersRecursively(go.transform, layer);
                }
                else
                {

                    var op = Addressables.InstantiateAsync(m_highObjects[id].Address);
                    yield return op;

                    go = op.Result;
                    go.SetActive(false);
                    go.transform.parent = m_highObjects[id].Parent.transform;
                    go.transform.localPosition = m_highObjects[id].Position;
                    go.transform.localRotation = m_highObjects[id].Rotation;
                    go.transform.localScale = m_highObjects[id].Scale;

                    ChangeLayersRecursively(go.transform, layer);

                }
                m_createdHighObjects[id] = go;

                ret = go;

                
            }

            callback(ret);
        }

        public override IEnumerator GetLowObject(int id, Action<GameObject> callback)
        {
            GameObject ret = null;
            int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
            if (layer < 0 || layer > 31)
                layer = 0;

            if (m_createdLowObjects.ContainsKey(id))
            {
                //this id being loaded
                while (m_createdLowObjects[id] == null)
                    yield return null;

                ret = m_createdLowObjects[id];
            }
            else
            {
                m_createdLowObjects.Add(id, null);

                var op = Addressables.InstantiateAsync(m_lowObjects[id]);
                yield return op;
                
                if ( op.Status ==AsyncOperationStatus.Failed)
                    yield break;

                GameObject go = op.Result;
                go.SetActive(false);
                go.transform.SetParent(m_hlodMeshesRoot.transform, false);
                
                ChangeLayersRecursively(go.transform, layer);

                m_createdLowObjects[id] = go;

                ret = go;
            }

            callback(ret);

            
        }

        public override void ReleaseHighObject(int id)
        {
            if (string.IsNullOrEmpty(m_highObjects[id].Address) == true)
            {
                if ( m_createdHighObjects[id] != null)
                    m_createdHighObjects[id].SetActive(false);

            }
            else
            {
                Addressables.ReleaseInstance(m_createdHighObjects[id]);
            }

            m_createdHighObjects.Remove(id);
        }
        public override void ReleaseLowObject(int id)
        {
            GameObject go = m_createdLowObjects[id];
            m_createdLowObjects.Remove(id);

            Addressables.ReleaseInstance(go);
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