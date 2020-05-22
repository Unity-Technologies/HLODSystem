using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class RootData : ScriptableObject, ISerializationCallbackReceiver
    {
        private Dictionary<string, GameObject> m_rootObjects = new Dictionary<string, GameObject>();

        [SerializeField] private List<string> m_serializedNames = new List<string>();
        [SerializeField] private List<GameObject> m_serializedGameObjects = new List<GameObject>();
        

        public void SetRootObject(string name, GameObject gameObject)
        {
            if (m_rootObjects.ContainsKey(name) == false)
            {
                m_rootObjects.Add(name, gameObject);
            }
            else
            {
                m_rootObjects[name] = gameObject;
            }
        }

        public  GameObject GetRootObject(String name)
        {
            GameObject go;
            if (m_rootObjects.TryGetValue(name, out go))
            {
                return go;
            }
            else
            {
                return null;
            }
        }

        public void OnBeforeSerialize()
        {
            foreach (var item in m_rootObjects)
            {
                m_serializedNames.Add(item.Key);
                m_serializedGameObjects.Add(item.Value);
            }
            m_rootObjects.Clear();
        }

        public void OnAfterDeserialize()
        {
            int len = Mathf.Min(m_serializedNames.Count, m_serializedGameObjects.Count);
            for (int i = 0; i < len; ++i)
            {
                if (m_rootObjects.ContainsKey(m_serializedNames[i]))
                    m_rootObjects.Remove(m_serializedNames[i]);
                
                m_rootObjects.Add(m_serializedNames[i], m_serializedGameObjects[i]);
            }
            
            m_serializedNames.Clear();
            m_serializedGameObjects.Clear();
        }
    }
}
