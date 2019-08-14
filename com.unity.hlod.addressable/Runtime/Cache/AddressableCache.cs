using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Streaming.Cache
{
    static class AddressableCache
    {
        public class LoadOperation : IEnumerator
        {
            public LoadOperation(object key)
            {
                m_key = key;

                if (m_loadingObjects.ContainsKey(m_key))
                {
                    m_loadingObjects[m_key].Completed += operation =>
                    {
                        m_isLoadDone = true;
                        m_result = operation.Result;
                        m_completeCallback?.Invoke(this);
                    };
                }
                else if ( m_usingObjects.ContainsKey(m_key) )
                {
                    m_isLoadDone = true;
                    m_result = m_usingObjects[m_key].Result;
                }
            }

            public event Action<LoadOperation> Completed
            {
                add
                {
                    if (m_loadingObjects.ContainsKey(m_key) == false)
                    {
                        value(this);
                    }
                    else
                    {
                        m_completeCallback += value;
                    }
                    
                }
                remove
                {
                    m_completeCallback -= value;
                }
            }
            public bool MoveNext()
            {
                return m_isLoadDone == false;
            }

            public void Reset()
            {   
            }

            public object Current
            {
                get { return null; }

            }

            public Object Result
            {
                get { return m_result; }
            }

            private Object m_result;

            private object m_key;
            private Action<LoadOperation> m_completeCallback;
            private bool m_isLoadDone = false;
        }
        #region interface
        public static LoadOperation Load(AssetReference reference)
        {
            object key = reference.RuntimeKey;
            if (m_usingObjects.ContainsKey(key) == false)
            {
                var ao = Addressables.LoadAsset<Object>(reference);
                ao.Completed += operation =>
                {
                    if (m_usingObjects.ContainsKey(key) == true)
                    {
                        m_usingObjects[key].Result = operation.Result;
                    }

                    m_loadingObjects.Remove(key);
                };
                m_loadingObjects[key] = ao;

                m_usingObjects[key] = new UseInfo()
                {
                    Count = 1,
                    Result = null,
                };
            }
            return new LoadOperation(key);
        }

        public static void Unload(AssetReference reference)
        {
            object key = reference.RuntimeKey;
            if (m_usingObjects.ContainsKey(key) == false)
                return;

            m_usingObjects[key].Count -= 1;
            if (m_usingObjects[key].Count != 0)
                return;

            //This means loading now.
            //So, after loading, we check asset again for remove or not.
            if (m_usingObjects[key].Result == null)
            {
                m_loadingObjects[key].Completed += operation =>
                {
                    if (m_usingObjects[key].Count == 0)
                    {
                        Addressables.Release(m_usingObjects[key].Result);
                        m_usingObjects.Remove(key);
                    }
                };
            }
            else
            {
                Addressables.Release(m_usingObjects[key].Result);
                m_usingObjects.Remove(key);
            }
        }
        #endregion


        class UseInfo
        {
            public Object Result;
            public int Count;
        };

        private static Dictionary<object, UseInfo> m_usingObjects = new Dictionary<object, UseInfo>();
        private static Dictionary<object, AsyncOperationHandle<Object>> m_loadingObjects = new Dictionary<object, AsyncOperationHandle<Object>>();


    }
}
