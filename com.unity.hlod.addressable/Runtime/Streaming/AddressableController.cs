using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableController : ControllerBase
    {
        [Serializable]
        public class ChildObject
        {
            public GameObject GameObject;

            public AssetReference Reference;

            public Transform Parent;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        [SerializeField]
        private List<ChildObject> m_highObjects = new List<ChildObject>();

        [SerializeField]
        private List<AssetReference> m_lowObjects = new List<AssetReference>();

        

        private Dictionary<int, GameObject> m_createdHighObjects = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> m_createdLowObjects = new Dictionary<int, GameObject>();

        private HLOD m_hlod;
        private GameObject m_hlodMeshesRoot;
        void Start()
        {
            m_hlod = GetComponent<HLOD>();
            m_hlodMeshesRoot = new GameObject("HLODMeshesRoot");
            m_hlodMeshesRoot.transform.SetParent(m_hlod.transform);

            for (int i = 0; i < m_highObjects.Count; ++i)
            {
                if (m_highObjects[i].Reference != null && m_highObjects[i].Reference.RuntimeKey.isValid)
                {
                    Destroy(m_highObjects[i].GameObject);
                }
                else if (m_highObjects[i].GameObject != null)
                {
                    m_highObjects[i].GameObject.SetActive(false);
                }
            }
        }

        public int AddHighObject(AssetReference reference, GameObject origin)
        {
            int id = m_highObjects.Count;

            ChildObject obj = new ChildObject();
            obj.GameObject = origin;
            obj.Reference = reference;
            obj.Parent = origin.transform.parent;
            obj.Position = origin.transform.position;
            obj.Rotation = origin.transform.rotation;
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
        public int AddLowObject(AssetReference hlodMesh)
        {
            int id = m_lowObjects.Count;
            m_lowObjects.Add(hlodMesh);
            return id;
        }

        public override IEnumerator GetHighObject(int id, Action<GameObject> callback)
        {
            GameObject ret = null;
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
                }
                else
                {
                    var op = m_highObjects[id].Reference.LoadAsset<GameObject>();
                    yield return op;
                    go = Instantiate(op.Result, m_highObjects[id].Parent.transform);
                    go.SetActive(false);
                    go.transform.position = m_highObjects[id].Position;
                    go.transform.rotation = m_highObjects[id].Rotation;
                    go.transform.localScale = m_highObjects[id].Scale;


                }
                m_createdHighObjects[id] = go;

                ret = go;

                
            }

            callback(ret);
        }

        public override IEnumerator GetLowObject(int id, Action<GameObject> callback)
        {
            GameObject ret = null;
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


                var op = m_lowObjects[id].LoadAsset<HLODMesh>();
                yield return op;

                GameObject go = new GameObject(op.Result.name);
                go.SetActive(false);
                go.transform.parent = m_hlodMeshesRoot.transform;
                go.AddComponent<MeshFilter>().sharedMesh = op.Result.ToMesh();
                go.AddComponent<MeshRenderer>().material = op.Result.Material;

                m_createdLowObjects[id] = go;

                ret = go;
            }

            callback(ret);

            
        }

        public override void ReleaseHighObject(int id)
        {
            if (m_highObjects[id].Reference == null || m_highObjects[id].Reference.RuntimeKey.isValid == false)
            {
                m_createdHighObjects[id].SetActive(false);

            }
            else
            {
                Destroy(m_createdHighObjects[id]);
                m_highObjects[id].Reference.ReleaseAsset();
            }

            m_createdHighObjects.Remove(id);
        }
        public override void ReleaseLowObject(int id)
        {
            GameObject go = m_createdLowObjects[id];
            m_createdLowObjects.Remove(id);

            Destroy(go);
            m_lowObjects[id].ReleaseAsset();
        }
    }

}