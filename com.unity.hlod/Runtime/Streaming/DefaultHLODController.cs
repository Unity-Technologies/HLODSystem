using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public class DefaultHLODController : HLODControllerBase
    {
        class LoadHandle : ILoadHandle
        {
            public LoadHandle(GameObject result)
            {
                m_result = result;
            }
            public bool MoveNext()
            {
                return false;
            }

            public void Reset()
            {
            }

            public object Current
            {
                get { return null; }
            }

            public GameObject Result
            {
                get { return m_result; }
            }

            private GameObject m_result;
        }
        [SerializeField]
        private List<GameObject> m_gameObjectList = new List<GameObject>();
        [SerializeField]
        private List<GameObject> m_lowGameObjects = new List<GameObject>();

        public int AddHighObject(GameObject gameObject)
        {
            int id = m_gameObjectList.Count;
            m_gameObjectList.Add(gameObject);
            return id;
        }

        public int AddLowObject(GameObject gameObject)
        {
            int id = m_lowGameObjects.Count;
            m_lowGameObjects.Add(gameObject);
            return id;
        }

        public override ILoadHandle GetHighObject(int id, int level, float distance)
        {
            return new LoadHandle(m_gameObjectList[id]);
        }

        public override ILoadHandle GetLowObject(int id, int level, float distance)
        {
            return new LoadHandle(m_lowGameObjects[id]);
        }
        
        public override void OnStart()
        {

        }

        public override void OnStop()
        {

        }



        public override void Install()
        {
            for (int i = 0; i < m_gameObjectList.Count; ++i)
            {
                m_gameObjectList[i].SetActive(false);
            }
        }

        public override void ReleaseHighObject(int id)
        {
            m_gameObjectList[id].SetActive(false);
        }

        public override void ReleaseLowObject(int id)
        {
            m_lowGameObjects[id].SetActive(false);
        }
    }

}