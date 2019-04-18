using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

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

        
        private GameObject m_hlodMeshesRoot;
        void Start()
        {
           OnStart();
        }

        
        public override void OnStart()
        {
            HLOD hlod;
            hlod = GetComponent<HLOD>();

            m_hlodMeshesRoot = new GameObject("HLODMeshesRoot");
            m_hlodMeshesRoot.transform.SetParent(hlod.transform, false);

#if UNITY_EDITOR
            Install();
#endif

        }

        public override void OnStop()
        {
        }



        public override void Install()
        {
            for (int i = 0; i < m_highObjects.Count; ++i)
            {
                if (m_highObjects[i].Reference != null && m_highObjects[i].Reference.RuntimeKey.isValid)
                {
                    DestoryObject(m_highObjects[i].GameObject);
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
        public int AddLowObject(AssetReference hlodMesh)
        {
            int id = m_lowObjects.Count;
            m_lowObjects.Add(hlodMesh);
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
                    GameObject asset = null;
#if UNITY_EDITOR
                    asset = m_highObjects[id].Reference.editorAsset as GameObject;
#else
                    var op = m_highObjects[id].Reference.LoadAsset<GameObject>();
                    yield return op;
                    asset = op.Result;
#endif

                    go = Instantiate(asset, m_highObjects[id].Parent.transform);
                    go.SetActive(false);
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

                HLODMesh mesh = null;

#if UNITY_EDITOR
                mesh = m_lowObjects[id].editorAsset as HLODMesh;
#else

                var op = m_lowObjects[id].LoadAsset<HLODMesh>();
                yield return op;
                mesh = op.Result;
#endif

                GameObject go = new GameObject(mesh.name);
                go.SetActive(false);
                go.transform.SetParent(m_hlodMeshesRoot.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh.ToMesh();
                go.AddComponent<MeshRenderer>().material = mesh.Material;
                
                ChangeLayersRecursively(go.transform, layer);

                m_createdLowObjects[id] = go;

                ret = go;
            }

            callback(ret);

            
        }

        public override void ReleaseHighObject(int id)
        {
            if (m_highObjects[id].Reference == null || m_highObjects[id].Reference.RuntimeKey.isValid == false)
            {
                if ( m_createdHighObjects[id] != null)
                    m_createdHighObjects[id].SetActive(false);

            }
            else
            {
                DestoryObject(m_createdHighObjects[id]);
                if (m_highObjects[id].Reference.Asset != null) 
                    m_highObjects[id].Reference.ReleaseAsset();
            }

            m_createdHighObjects.Remove(id);
        }
        public override void ReleaseLowObject(int id)
        {
            GameObject go = m_createdLowObjects[id];
            m_createdLowObjects.Remove(id);

            if (go != null)
            {

                Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
                if (mesh != null)
                    DestoryObject(mesh);
                DestoryObject(go);
            }

            if (m_lowObjects[id].Asset != null)
                m_lowObjects[id].ReleaseAsset();
        }

        private void DestoryObject(Object obj)
        {
#if UNITY_EDITOR
            if ( UnityEditor.EditorApplication.isPlaying)
                Destroy(obj);
            else
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