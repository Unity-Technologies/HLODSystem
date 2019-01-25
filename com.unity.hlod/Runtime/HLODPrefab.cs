using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
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


        void OnEnable()
        {
            UpdatePrefab();
        }
        void OnDisable()
        {
            DestroyPrefab();
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
                m_instantiatePrefab.hideFlags = HideFlags.HideAndDontSave;

                int layer = LayerMask.NameToLayer(HLOD.HLODLayerStr);
                if ( layer >= 0 && layer <= 31)
                    ChangeLayersRecursively(m_instantiatePrefab.transform, layer);

                foreach (var hlod in FindHLODinPrefab(m_instantiatePrefab))
                {
                    HLODManager.Instance.RegisterHLOD(hlod);
                }
            }
          

            m_instantiatePrefab.transform.parent = transform;
        }

        void DestroyPrefab()
        {
            if (m_instantiatePrefab == null)
                return;

            foreach (var hlod in FindHLODinPrefab(m_instantiatePrefab))
            {
                HLODManager.Instance.UnregisterHLOD(hlod);
            }
            m_instantiatePrefab.SetActive(false);
            DestroyImmediate(m_instantiatePrefab);

        }

        static List<HLOD> FindHLODinPrefab(GameObject prefab)
        {
            List<HLOD> prefabHlods  = new List<HLOD>();
            HLOD[] hlods = prefab.GetComponentsInChildren<HLOD>();

            for ( int i = 0; i < hlods.Length; ++i )
            {
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(hlods[i]);
                if ( root == prefab )
                    prefabHlods.Add(hlods[i]);
            }

            return prefabHlods;

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