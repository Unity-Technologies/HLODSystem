using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using UnityEngine;

namespace Unity.HLODSystem
{
    [Serializable]
    public class SerializableDynamicObject : DynamicObject, ISerializationCallbackReceiver
    {
        [Serializable]
        public class SerializeItem
        {
            [SerializeField]
            public string TypeStr;
            [SerializeField]
            public string Name;
            [SerializeField]
            public string Data;
        }
        [SerializeField]
        private List<SerializeItem> m_SerializeItems = new List<SerializeItem>();
        private Dictionary<string, object> m_DynamicContext = new Dictionary<string, object>();

        public bool ContainsKey(string key)
        {            
            return m_DynamicContext.ContainsKey(key);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (m_DynamicContext.ContainsKey(binder.Name) == false)
            {
                m_DynamicContext.Add(binder.Name, value);
            }
            else
            {
                m_DynamicContext[binder.Name] = value;
            }

            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (m_DynamicContext.TryGetValue(binder.Name, out result) == false)
            {
                result = null;
                m_DynamicContext.Add(binder.Name, null);
            }

            return true;
        }
        

        public void OnBeforeSerialize()
        {
            m_SerializeItems.Clear();
                
            foreach (var pair in m_DynamicContext)
            {
                if (pair.Value == null)
                    continue;

                SerializeItem item = new SerializeItem();
                item.TypeStr = pair.Value.GetType().AssemblyQualifiedName;
                item.Name = pair.Key;
                item.Data = pair.Value.ToString();

                m_SerializeItems.Add(item);
            }
        }

        public void OnAfterDeserialize()
        {
            m_DynamicContext.Clear();

            for (int i = 0; i < m_SerializeItems.Count; ++i)
            {
                Type type = Type.GetType(m_SerializeItems[i].TypeStr);
                if (type == null)
                    continue;

                object converted = Convert.ChangeType(m_SerializeItems[i].Data, type);
                if (converted == null)
                    continue;

                m_DynamicContext.Add(m_SerializeItems[i].Name, converted);
            }

            m_SerializeItems.Clear();
        }

        public SerializableDynamicObject()
        {
        }


    }

}