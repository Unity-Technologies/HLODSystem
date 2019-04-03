using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public class DefaultController : ControllerBase
    {

        [SerializeField]
        private List<HLODMesh> m_meshDataList = new List<HLODMesh>();

        public void AddHLODMesh(HLODMesh mesh)
        {
            m_meshDataList.Add(mesh);
        }

        public void AddHLODMeshes(List<HLODMesh> meshes)
        {
            m_meshDataList.AddRange(meshes);
        }
        
        public override IEnumerator Load()
        {
            for (int i = 0; i < m_meshDataList.Count; ++i)
            {
                GameObject go = new GameObject();

                go.AddComponent<MeshFilter>().sharedMesh = m_meshDataList[i].ToMesh();
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