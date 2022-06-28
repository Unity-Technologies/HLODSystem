using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    [Serializable]
    public class HLODUserData
    {
        [Serializable]
        public class UserDataTable<T> : ISerializationCallbackReceiver
        {
            private Dictionary<string, int> m_idTable = new Dictionary<string, int>();

            [SerializeField]
            private List<string> m_keys = new List<string>();
            [SerializeField]
            private List<T> m_values = new List<T>();

            public bool AddData(string key, T value)
            {
                if (m_idTable.ContainsKey(key))
                    return false;
                
                m_keys.Add(key);
                m_values.Add(value);
                m_idTable[key] = m_values.Count - 1;

                return true;
            }

            public T GetData(string key)
            {
                int index = 0;
                if (m_idTable.TryGetValue(key, out index) == false)
                    throw new KeyNotFoundException("Key not found: " + key);
                return m_values[index];
            }

            public bool TryGetData(string key, out T value)
            {
                int index = 0;
                if (m_idTable.TryGetValue(key, out index) == false)
                {
                    value = default(T);
                    return false;
                }

                value = m_values[index];
                return true;
            }

            public bool HasAnyData()
            {
                return m_idTable.Count > 0;
            }

            public bool HasData(string key)
            {
                return m_idTable.ContainsKey(key);
            }

            public void OnBeforeSerialize()
            {
            }

            public void OnAfterDeserialize()
            {
                m_idTable.Clear();
                
                for (int i = 0; i < m_keys.Count; ++i)
                {
                    m_idTable[m_keys[i]] = i;
                }
            }
        }

        public bool HasAnyData()
        {
            return m_intDatas.HasAnyData() ||
                   m_floatDatas.HasAnyData() ||
                   m_stringDatas.HasAnyData();
        }

        public UserDataTable<int> IntDatas
        {
            get
            {
                return m_intDatas;
            }
        }

        public UserDataTable<float> FloatDatas
        {
            get
            {
                return m_floatDatas;
            }
        }

        public UserDataTable<string> StringDatas
        {
            get
            {
                return m_stringDatas;
            }
        }

        [SerializeField]
        private UserDataTable<int> m_intDatas = new UserDataTable<int>();
        [SerializeField]
        private UserDataTable<float> m_floatDatas = new UserDataTable<float>();
        [SerializeField]
        private UserDataTable<string> m_stringDatas = new UserDataTable<string>();
    
    }
}