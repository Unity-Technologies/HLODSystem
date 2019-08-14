using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.HLODSystem.Utils
{
    
    public class DisposableList<T> : IDisposable, IList<T>, ICollection<T>, IEnumerable<T> 
        where T : IDisposable
    {
        List<T> m_list = new List<T>();
        public void Dispose()
        {
            for (int i = 0; i < m_list.Count; ++i)
            {
                m_list[i].Dispose();
            }
            m_list.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            m_list.Add(item);
        }
        
        public void AddRange(IEnumerable<T> collection)
        {
            m_list.AddRange(collection);
        }

        public void Clear()
        {
            m_list.Clear();
        }

        public bool Contains(T item)
        {
            return m_list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (item != null)
            {
                item.Dispose();
            }

            return m_list.Remove(item);
        }

        public int Count
        {
            get
            {
                return m_list.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(T item)
        {
            return m_list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (m_list[index] != null)
            {
                m_list[index].Dispose();
            }

            m_list.RemoveAt(index);
        }

        public T this[int index]
        {
            get => m_list[index];
            set => m_list[index] = value;
        }
    }
}