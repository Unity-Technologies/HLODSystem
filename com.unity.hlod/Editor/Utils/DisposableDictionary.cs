using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.HLODSystem.Utils
{
    public class DisposableDictionary<TKey, TValue> : IDisposable, IDictionary<TKey, TValue> 
        where TValue:IDisposable
    {
        private Dictionary<TKey, TValue> m_dic = new Dictionary<TKey, TValue>();
        public void Dispose()
        {
            foreach (var value in m_dic.Values)
            {
                value.Dispose();
            }
            m_dic.Clear();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>) m_dic).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>) m_dic).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>) m_dic).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>) m_dic).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>) m_dic).CopyTo(array, arrayIndex);
            
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>) m_dic).Remove(item);
        }

        public int Count { get => ((IDictionary<TKey, TValue>) m_dic).Count; }
        public bool IsReadOnly { get => ((IDictionary<TKey, TValue>) m_dic).IsReadOnly; }
        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>) m_dic).Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>) m_dic).ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>) m_dic).Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>) m_dic).TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => ((IDictionary<TKey, TValue>) m_dic)[key];
            set => ((IDictionary<TKey, TValue>) m_dic)[key] = value;
        }

        public ICollection<TKey> Keys { get => ((IDictionary<TKey, TValue>) m_dic).Keys; }
        public ICollection<TValue> Values { get => ((IDictionary<TKey, TValue>) m_dic).Values; }
    }
}