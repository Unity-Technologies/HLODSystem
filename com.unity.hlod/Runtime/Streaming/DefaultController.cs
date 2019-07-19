using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public class DefaultController : ControllerBase
    {
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

        public override IEnumerator GetHighObject(int id, Action<GameObject> callback)
        {
            if (callback != null)
            {
                callback(m_gameObjectList[id]);
            }
            yield break;
        }

        public override IEnumerator GetLowObject(int id, Action<GameObject> callback)
        {
            if (callback != null)
            {
                callback(m_lowGameObjects[id]);
            }
            yield break;
        }
        
        public override void OnStart()
        {
#if UNITY_EDITOR
            Install();
#endif

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