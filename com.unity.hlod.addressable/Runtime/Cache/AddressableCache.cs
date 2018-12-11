using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Streaming.Cache
{
    static class AddressableCache
    {
        public class LoadOperation : IEnumerator
        {
            public LoadOperation(Hash128 resourceHash)
            {
                m_resourceHash = resourceHash;

                if (m_loadingObjects.ContainsKey(resourceHash))
                {
                    m_loadingObjects[resourceHash].Completed += operation =>
                    {
                        m_isLoadDone = true;
                        m_result = operation.Result;
                        m_completeCallback?.Invoke(this);
                    };
                }
                else if ( m_usingObjects.ContainsKey(resourceHash) )
                {
                    m_isLoadDone = true;
                    m_result = m_usingObjects[resourceHash].Result;
                }
            }

            public event Action<LoadOperation> Completed
            {
                add
                {
                    if (m_loadingObjects.ContainsKey(m_resourceHash) == false)
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

            private Hash128 m_resourceHash;
            private Action<LoadOperation> m_completeCallback;
            private bool m_isLoadDone = false;
        }
        #region interface
        public static LoadOperation Load(AssetReference reference)
        {
            
            Hash128 hash = reference.RuntimeKey;
            if (m_usingObjects.ContainsKey(hash) == false)
            {
                var ao = Addressables.LoadAsset<Object>(reference);
                ao.Completed += operation =>
                {
                    m_usingObjects[hash].Result = operation.Result;
                    m_loadingObjects.Remove(hash);
                };
                m_loadingObjects[hash] = ao;

                m_usingObjects[hash] = new UseInfo()
                {
                    Count = 1,
                    Result = null,
                };
            }
            return new LoadOperation(hash);
        }

        public static void Unload(AssetReference reference)
        {
            Hash128 hash = reference.RuntimeKey;
            if (m_usingObjects.ContainsKey(hash) == false)
                return;

            m_usingObjects[hash].Count -= 1;
            if (m_usingObjects.Count == 0)
            {
                Addressables.ReleaseAsset(m_usingObjects[hash].Result);
                m_usingObjects.Remove(hash);
            }
        }
        #endregion


        class UseInfo
        {
            public Object Result;
            public int Count;
        };

        private static Dictionary<Hash128, UseInfo> m_usingObjects = new Dictionary<Hash128, UseInfo>();
        private static Dictionary<Hash128, IAsyncOperation<Object>> m_loadingObjects = new Dictionary<Hash128, IAsyncOperation<Object>>();


    }
}
