using System;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

namespace Unity.HLODSystem
{
    [Serializable]
    public class SerializableDynamicObject : DynamicObject, ISerializationCallbackReceiver
    {

        interface ISerializeItem
        {
            void SetName(string name);
            string GetName();

            object GetData();
        }
        [Serializable]
        class SerializeItem<T> : ISerializeItem
        {
            [SerializeField]
            public string Name;
            [SerializeField]
            public T Data;


            public void SetName(string name)
            {
                Name = name;
            }
            public string GetName()
            {
                return Name;
            }


            public void SetData(T data)
            {
                Data = data;
            }
            public object GetData()
            {
                return Data;
            }
        }

        [Serializable]
        class JsonSerializedData
        {
            [SerializeField]
            public string Type;
            [SerializeField]
            public string Data;
        }

        [SerializeField]
        private List<JsonSerializedData> m_SerializeItems = new List<JsonSerializedData>();

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

                Type genericClass = typeof(SerializeItem<>);
                Type constructedClass = genericClass.MakeGenericType(pair.Value.GetType());

                ISerializeItem item = Activator.CreateInstance(constructedClass) as ISerializeItem;
                if (item == null)
                    continue;

                var methodInfo = constructedClass.GetMethod("SetData");
                methodInfo.Invoke(item, new object[]{pair.Value});

                item.SetName(pair.Key);

                JsonSerializedData data = new JsonSerializedData();
                data.Type = item.GetType().AssemblyQualifiedName;
                data.Data = JsonUtility.ToJson(item);

                m_SerializeItems.Add(data);

            }
        }

        public void OnAfterDeserialize()
        {
            m_DynamicContext.Clear();

            for (int i = 0; i < m_SerializeItems.Count; ++i)
            {
                if (string.IsNullOrEmpty(m_SerializeItems[i].Type))
                    continue;

                Type type = Type.GetType(m_SerializeItems[i].Type);
                if (type == null)
                    continue;

                var data = JsonUtility.FromJson(m_SerializeItems[i].Data, type) as ISerializeItem;
                if (data == null)
                    continue;

                m_DynamicContext.Add(data.GetName(), data.GetData());
            }

            m_SerializeItems.Clear();
        }

        public SerializableDynamicObject()
        {
        }


    }

}