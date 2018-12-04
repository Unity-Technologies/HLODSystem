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

        private bool isShow = true;
        private bool needPrepare = true;

        public void AddHLOD(HLOD hlod)
        {
            m_childHlods.Add(hlod);
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

        public override bool IsReady()
        {
            for (int i = 0; i < m_childHlods.Count; ++i)
            {
                var lowRoot = m_childHlods[i].LowRoot;
                if ( lowRoot == null )
                    continue;

                var controller = lowRoot.GetComponent<ControllerBase>();
                if (controller == null)
                    continue;

                if (controller.IsReady() == false)
                    return false;
            }


            return m_childObjects.Count == m_instantitedObjects.Count;
        }

        public override bool IsShow()
        {
            return isShow;
        }

        public override void Prepare()
        {
            if (needPrepare == false)
                return;

            for (int i = 0; i < m_childHlods.Count; ++i)
            {
                var lowRoot = m_childHlods[i].LowRoot;
                if (lowRoot == null)
                    continue;
                var controller = lowRoot.GetComponent<ControllerBase>();
                if (controller == null)
                    continue;

                controller.Prepare();
            }

            CreateChildObjects(false);
            needPrepare = false;
        }

        public override void Show()
        {
            isShow = true;

            for (int i = 0; i < m_instantitedObjects.Count; ++i)
            {
                m_instantitedObjects[i].SetActive(true);
            }

            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            isShow = false;

            for (int i = 0; i < m_instantitedObjects.Count; ++i)
            {
                m_instantitedObjects[i].SetActive(false);
            }

            gameObject.SetActive(false);
        }

        public override void Enable()
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
            needPrepare = true;
        }
        public override void Disable()
        {
            if (enabled == false)
                return;

            CreateChildObjects(true);
            enabled = false;
        }

        private void LoadDoneObject(GameObject prefab, ChildObject obj, bool active)
        {
            if (prefab == null)
                return;

            GameObject instance = Instantiate(prefab, obj.Parent.transform);
            instance.transform.position = obj.Position;
            instance.transform.rotation = obj.Rotation;
            instance.transform.localScale = obj.Scale;
            instance.SetActive(active);

            m_instantitedObjects.Add(instance);
        }

        private void CreateChildObjects(bool active)
        {
            for (int i = 0; i < m_childObjects.Count; ++i)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying == false)
                {
                    GameObject prefab = (GameObject) m_childObjects[i].Reference.editorAsset;
                    LoadDoneObject(prefab, m_childObjects[i], active);
                    continue;
                }
#endif
                var objectInfo = m_childObjects[i];
                Addressables.LoadAsset<UnityEngine.Object>(objectInfo.Reference).Completed += o =>
                {
                    
                    if ( o.OperationException != null)
                        Debug.Log(o.OperationException.Message);
                    var result = o.Result;
                    LoadDoneObject((GameObject)result, objectInfo, active);
                };
            }
        }
    }

}