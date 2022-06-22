using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using UnityEngine;

namespace Unity.HLODSystem
{
    [Serializable]
    public class HLODUserData
    {
        [Serializable]
        public class UserDataTable<T> : ISerializationCallbackReceiver
        {
            [NonSerialized]
            private Dictionary<string, T> m_datas = new Dictionary<string, T>();
            
            [SerializeField]
            private List<string> m_keys;
            [SerializeField]
            private List<T> m_values;

            public bool AddData(string key, T value)
            {
                return m_datas.TryAdd(key, value);
            }

            public T GetData(string key)
            {
                return m_datas[key];
            }

            public bool TryGetData(string key, out T value)
            {
                return m_datas.TryGetValue(key, out value);
            }

            public bool HasData(string key)
            {
                return m_datas.ContainsKey(key);
            }

            public void OnBeforeSerialize()
            {
                if (m_datas != null)
                {
                    m_keys = new List<string>(m_datas.Count);
                    m_values = new List<T>(m_datas.Count);

                    foreach (var data in m_datas)
                    {
                        m_keys.Add(data.Key);
                        m_values.Add(data.Value);
                    }

                    m_datas = null;
                }
            }

            public void OnAfterDeserialize()
            {
                if (m_keys != null)
                {
                    m_datas = new Dictionary<string, T>(m_keys.Count);
                    for (int i = 0; i < m_keys.Count; ++i)
                    {
                        m_datas.Add(m_keys[i], m_values[i]); 
                    }

                    m_keys = null;
                    m_values = null;
                }
                else
                {
                    m_datas = new Dictionary<string, T>();
                }

            }
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