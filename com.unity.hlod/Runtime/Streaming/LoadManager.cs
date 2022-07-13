using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public class LoadManager
    {
        #region Singleton
        private static LoadManager s_instance;
       
        public static LoadManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    GameObject go = new GameObject("LoadManager");
                    s_instance = new LoadManager();
                }

                return s_instance;
            }
        }
        #endregion
        public abstract class Handle
        {
            
            public Handle(HLODControllerBase controller, int id, int level, float distance,
                bool isHighObject, 
                Action<Handle> loadDoneCallback)
            {
                m_controller = controller;
                m_id = id;
                m_level = level;
                m_distnace = distance;
                m_isHighObject = isHighObject;
                m_loadDoneCallback = loadDoneCallback;
            }

            public HLODControllerBase Controller
            {
                get { return m_controller; }
            }

            public GameObject LoadedObject
            {
                get { return m_loadedObject; }
            }
            public bool IsLoading
            {
                get { return m_isLoading; }
            }

            public int Id
            {
                get { return m_id; }
            }

            public int Level
            {
                get { return m_level; }
            }

            public float Distance
            {
                get { return m_distnace; }
            }
            
            

            
            protected bool m_isLoading = false;
            protected bool m_isHighObject = false;
            protected GameObject m_loadedObject = null;
            
            private HLODControllerBase m_controller;
            private int m_id;
            private int m_level;
            private float m_distnace;
            
            protected Action<Handle> m_loadDoneCallback;

        }

        private class HandleLoader : Handle
        {
            public HandleLoader(HLODControllerBase controller, int id, int level, float distance,
                bool isHighObject,
                Action<Handle> loadDoneCallback)
                : base(controller, id, level, distance, isHighObject, loadDoneCallback)
            {
            }

            public void StartLoad()
            {
                m_isLoading = true;
                if (m_isHighObject)
                {
                    Controller.LoadHighObject(Id, (go) =>
                    {
                        m_isLoading = false;
                        m_loadedObject = go;
                        FinishLoad();
                        m_loadDoneCallback?.Invoke(this);
                    });
                }
                else
                {
                    Controller.LoadLowObject(Id, (go) =>
                    {
                        m_isLoading = false;
                        m_loadedObject = go;
                        FinishLoad();
                        m_loadDoneCallback?.Invoke(this);
                    });
                }
            }

            public void Unload()
            {
                if (m_isHighObject)
                {
                    Controller.UnloadHighObject(Id);
                }
                else
                {
                    Controller.UnloadLowObject(Id);
                }
            }

            private void FinishLoad()
            {
                LoadManager.Instance.FinishLoad(this);
            }
        }
        
        private LinkedList<HandleLoader> m_loadQueue = new LinkedList<HandleLoader>();
        
        public Handle LoadHighObject(HLODControllerBase controller, int id, int level, float distance, Action<Handle> loadDoneCallback)
        {
            HandleLoader handle = new HandleLoader(controller, id, level, distance, true, loadDoneCallback);
            InsertHandle(handle);
            return handle;
        }
        public Handle LoadLowObject(HLODControllerBase controller, int id, int level, float distance, Action<Handle> loadDoneCallback)
        {
            HandleLoader handle = new HandleLoader(controller, id, level, distance, false, loadDoneCallback);
            InsertHandle(handle);
            return handle;
        }

        public void UnloadHighObject(Handle handle)
        {
            HandleLoader handleLoader = handle as HandleLoader;
            if (handleLoader == null)
            {
                Debug.LogError("Handle is not created by LoadManager.");
                return;
            }

            handleLoader.Unload();
        }
        public void UnloadLowObject(Handle handle)
        {
            HandleLoader handleLoader = handle as HandleLoader;
            if (handleLoader == null)
            {
                Debug.LogError("Handle is not created by LoadManager.");
                return;
            }

            handleLoader.Unload();
        }

        public void RegisterController(HLODControllerBase controller)
        {
        }

        public void UnregisterController(HLODControllerBase controller)
        {
            var node = m_loadQueue.First;
            while (node != null)
            {
                if (node.Value.Controller == controller)
                {
                    var remove = node;
                    node = node.Next;
                    m_loadQueue.Remove(remove);
                }
                else
                {
                    node = node.Next;
                }
            }

        }

        private void InsertHandle(HandleLoader handle)
        {
            var node = m_loadQueue.First;
            while (node != null && node.Value.Level < handle.Level)
            {
                node = node.Next;
            }

            while (node != null && node.Value.Level == handle.Level && node.Value.Distance < handle.Distance)
            {
                node = node.Next;
            }

            if (node == null)
            {
                m_loadQueue.AddLast(handle);
            }
            else
            {
                m_loadQueue.AddBefore(node, handle);
            }
            
            if ( m_loadQueue.Count == 1)
                StartLoadFirst();
        }

        private void StartLoadFirst()
        {
            var node = m_loadQueue.First;
            if (node == null || node.Value.IsLoading == true)
                return;

            node.Value.StartLoad();
        }
        private void FinishLoad(HandleLoader handle)
        {
            m_loadQueue.Remove(handle);
            StartLoadFirst();
        }
         
    }
}