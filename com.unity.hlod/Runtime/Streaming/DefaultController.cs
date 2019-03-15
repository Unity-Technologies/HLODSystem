using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public class DefaultController : ControllerBase
    {
        [Serializable]
        struct MeshData
        {
            [SerializeField] public HLODMesh Mesh;
            [SerializeField] public Material Material;
        }

        [SerializeField]
        private List<MeshData> m_meshDataList = new List<MeshData>();

        public override void AddHLODMesh(HLODMesh mesh, Material mat)
        {
            m_meshDataList.Add(new MeshData
            {
                Mesh = mesh,
                Material = mat
            });
        }
        
        public override IEnumerator Load()
        {
            for (int i = 0; i < m_meshDataList.Count; ++i)
            {
                GameObject go = new GameObject();

                go.AddComponent<MeshFilter>().sharedMesh = m_meshDataList[i].Mesh.ToMesh();
                go.AddComponent<MeshRenderer>().material = m_meshDataList[i].Material;
                go.transform.parent = gameObject.transform;
            }

            m_meshDataList.Clear();
            yield break;
        }
        public override void Show()
        {
            gameObject.SetActive(true);            
        }

        public override void Hide()
        {
            gameObject.SetActive(false);            
        }

        public override void Enable()
        {
            enabled = true;
        }
        public override void Disable()
        {
            enabled = false;
        }
    }

}