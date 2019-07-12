using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.HLODSystem
{
    [ExecuteInEditMode]
    public class HLODPrefab : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField]
        private GameObject m_prefab;
        [SerializeField]
        private bool m_isEdit = false;

        private GameObject m_instantiatePrefab;
        private bool m_needUpdate = false;


        public GameObject Prefab
        {
            set
            {
                if (m_prefab != value)
                {
                    m_prefab = value;
                    UpdatePrefab();
                }
            }
            get { return m_prefab; }
        }

        public bool IsEdit
        {
            set
            {
                m_isEdit = value;
                UpdatePrefab();
            }
            get { return m_isEdit; }
        }

        void Start()
        {
            List<GameObject> childList = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.gameObject != m_instantiatePrefab)
                    childList.Add(child.gameObject);
            }

            for (int i = 0; i < childList.Count; ++i)
            {
                DestroyImmediate(childList[i]);
            }

            
        }

        void OnEnable()
        {
            //for the avoid editor crash.
            //if the update prefab in here, editor will be crash. I don't know why.            
            m_needUpdate = true;
        }
        void OnDisable()
        {
            DestroyPrefab();
        }

        void Update()
        {
            if (m_needUpdate)
            {
                UpdatePrefab();
                m_needUpdate = false;
            }
        }

        void UpdatePrefab()
        {
            if (m_prefab == null)
                return;

            if ( m_instantiatePrefab != null )
                DestroyPrefab();

            m_instantiatePrefab = PrefabUtility.InstantiatePrefab(m_prefab) as GameObject;
            if (m_instantiatePrefab == null)
                return;

            if (m_isEdit == true)
            {
                m_instantiatePrefab.hideFlags = HideFlags.DontSave;
            }
            else
            {
                PrefabUtility.UnpackPrefabInstance(m_instantiatePrefab, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                m_instantiatePrefab.hideFlags = HideFlags.HideAndDontSave;

                int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
                if ( layer >= 0 && layer <= 31)
                    ChangeLayersRecursively(m_instantiatePrefab.transform, layer);

                foreach (var hlod in FindHLODinPrefab(m_instantiatePrefab))
                {
//                    HLODManager.Instance.RegisterHLOD(hlod);
//                    hlod.StartUseInEditor();
                }
            }
          

            m_instantiatePrefab.transform.SetParent(transform, false);
        }

        void DestroyPrefab()
        {
            if (m_instantiatePrefab == null)
                return;

            foreach (var hlod in FindHLODinPrefab(m_instantiatePrefab))
            {
                //HLODManager.Instance.UnregisterHLOD(hlod);
                //hlod.StopUseInEditor();
            }
            m_instantiatePrefab.SetActive(false);
            DestroyImmediate(m_instantiatePrefab);

        }

        static HLOD[] FindHLODinPrefab(GameObject prefab)
        {
            List<HLOD> prefabHlods  = new List<HLOD>();
            return prefab.GetComponentsInChildren<HLOD>();
        }

        static void ChangeLayersRecursively(Transform trans, int layer)
        {
            trans.gameObject.layer = layer;
            foreach (Transform child in trans)
            {
                ChangeLayersRecursively(child, layer);
            }
        }
#endif
    }
}