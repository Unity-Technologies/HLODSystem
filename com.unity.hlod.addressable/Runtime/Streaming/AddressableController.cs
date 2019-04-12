using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableController : ControllerBase
    {
        [Serializable]
        public class ChildObject
        {
            public AssetReference Reference;

            public GameObject Parent;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        [SerializeField]
        [HideInInspector]
        private List<ChildObject> m_childObjects = new List<ChildObject>();
        [SerializeField]
        [HideInInspector]
        private List<HLOD> m_childHlods = new List<HLOD>();

        [SerializeField]
        private List<GameObject> m_instantitedObjects = new List<GameObject>();

        [SerializeField]
        private int m_maxInstantiateCount = 10;

        private List<AssetReference> m_loadedReferences = new List<AssetReference>();

        public int MaxInstantiateCount
        {
            set { m_maxInstantiateCount = value; }
            get { return m_maxInstantiateCount; }
        }


        public override int AddHighObject(GameObject gameObject)
        {
            return -1;
        }
        public override int AddLowObject(HLODMesh hlodMesh)
        {
            return -1;
        }

        public override IEnumerator GetHighObject(int id, Action<GameObject> callback)
        {
            yield break;
        }

        public override IEnumerator GetLowObject(int id, Action<GameObject> callback)
        {
            yield break;
        }

        public override void ReleaseHighObject(int id)
        {

        }
        public override void ReleaseLowObject(int id)
        {

        }

        public void AddHLOD(HLOD hlod)
        {
            m_childHlods.Add(hlod);
        }

        public void AddHLODMeshReferences(List<AssetReference> refs)
        {
            for (int i = 0; i < refs.Count; ++i)
            {
                ChildObject obj = new ChildObject();
                obj.Reference = refs[i];
                obj.Parent = gameObject;
                obj.Position = Vector3.zero;
                obj.Rotation = Quaternion.identity;
                obj.Scale = Vector3.one;

                m_childObjects.Add(obj);
            }
        }

        public void AddObject(AssetReference reference, Transform objectTransform)
        {
            if (reference == null || objectTransform == null)
                return;

            ChildObject obj = new ChildObject();
            obj.Reference = reference;
            obj.Parent = objectTransform.parent.gameObject;
            obj.Position = objectTransform.position;
            obj.Rotation = objectTransform.rotation;
            obj.Scale = objectTransform.localScale;

            m_childObjects.Add(obj);
        }

        public IEnumerator Load()
        {
            if (m_instantitedObjects.Count > 0)
                yield break;

            for (int i = 0; i < m_childHlods.Count; ++i)
            {
                //var lowRoot = m_childHlods[i].LowRoot;
                //if (lowRoot == null)
                //    continue;
                //var controller = lowRoot.GetComponent<ControllerBase>();
                //if (controller == null)
                //    continue;

                //yield return controller.Load();
            }

            yield return CreateChildObjects(false);
        }

        public void Show()
        {
            for (int i = 0; i < m_childHlods.Count; ++i)
            {
                //var lowRoot = m_childHlods[i].LowRoot;
                //if (lowRoot == null)
                //    continue;
                //var controller = lowRoot.GetComponent<ControllerBase>();
                //if (controller == null)
                //    continue;

                //controller.Show();
            }

            for (int i = 0; i < m_instantitedObjects.Count; ++i)
            {
                m_instantitedObjects[i].SetActive(true);
            }

            gameObject.SetActive(true);
        }

        public  void Hide()
        {
            for (int i = 0; i < m_childHlods.Count; ++i)
            {
                //var lowRoot = m_childHlods[i].LowRoot;
                //if (lowRoot == null)
                //    continue;
                //var controller = lowRoot.GetComponent<ControllerBase>();
                //if (controller == null)
                //    continue;

                //controller.Hide();
            }

            for (int i = 0; i < m_instantitedObjects.Count; ++i)
            {
#if UNITY_EDITOR
                DestroyImmediate(m_instantitedObjects[i]);
#else

                Destroy(m_instantitedObjects[i]);
#endif
            }

            m_instantitedObjects.Clear();

            for (int i = 0; i < m_loadedReferences.Count; ++i)
            {
                Cache.AddressableCache.Unload(m_loadedReferences[i]);
            }
            m_loadedReferences.Clear();

            gameObject.SetActive(false);
        }

        public void Enable()
        {
            if (enabled == true)
                return;

            for (int i = 0; i < m_instantitedObjects.Count; ++i)
            {
#if UNITY_EDITOR
                DestroyImmediate(m_instantitedObjects[i]);
#else
                Destroy(m_instantitedObjects[i]);
#endif
            }
            m_instantitedObjects.Clear();
            enabled = true;
        }
        public void Disable()
        {
            if (enabled == false)
                return;

            CreateChildObjectsByCallback(true);
            enabled = false;
        }

        private void LoadDoneObject(UnityEngine.Object asset, ChildObject obj, bool active)
        {
            if (asset == null)
                return;

            if (asset is HLODMesh)
            {
                HLODMesh mesh = (HLODMesh)asset;
                GameObject go = new GameObject();

                go.AddComponent<MeshFilter>().sharedMesh = mesh.ToMesh();
                go.AddComponent<MeshRenderer>().sharedMaterial = mesh.Material;

                go.transform.parent = obj.Parent.transform;
                go.transform.position = obj.Position;
                go.transform.rotation = obj.Rotation;
                go.transform.localScale = obj.Scale;
                go.SetActive(active);

                m_instantitedObjects.Add(go);

            }
            if (asset is GameObject)
            {
                GameObject instance = Instantiate((GameObject)asset, obj.Parent.transform);
                instance.transform.position = obj.Position;
                instance.transform.rotation = obj.Rotation;
                instance.transform.localScale = obj.Scale;
                instance.SetActive(active);

                m_instantitedObjects.Add(instance);
            }
        }

        private IEnumerator CreateChildObjects(bool active)
        {
            int instantiateCount = 0;
            for (int i = 0; i < m_childObjects.Count; ++i)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying == false)
                {
                    UnityEngine.Object asset = m_childObjects[i].Reference.editorAsset;
                    LoadDoneObject(asset, m_childObjects[i], active);
                    continue;
                }
#endif
                var objectInfo = m_childObjects[i];
                var ao = Cache.AddressableCache.Load(objectInfo.Reference);
                m_loadedReferences.Add(objectInfo.Reference);
                if (ao.Result == null)
                    yield return ao;
                if (ao.Result == null)
                {
                    Debug.LogError("Failed to load object: " + objectInfo.Reference);
                }

                LoadDoneObject(ao.Result, objectInfo, active);
                if (++instantiateCount >= MaxInstantiateCount)
                {
                    instantiateCount = 0;
                    yield return null;
                }

            }
        }

        private void CreateChildObjectsByCallback(bool active)
        {
            for (int i = 0; i < m_childObjects.Count; ++i)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying == false)
                {
                    UnityEngine.Object asset = m_childObjects[i].Reference.editorAsset;
                    LoadDoneObject(asset, m_childObjects[i], active);
                    continue;
                }
#endif
                var objectInfo = m_childObjects[i];
                Addressables.LoadAsset<UnityEngine.Object>(objectInfo.Reference).Completed += o =>
                {
                    LoadDoneObject(o.Result, objectInfo, active);
                };
            }
        }
    }

}