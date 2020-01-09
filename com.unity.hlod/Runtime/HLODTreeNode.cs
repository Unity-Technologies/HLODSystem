using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Unity.HLODSystem
{
    [Serializable]
    public class HLODTreeNode
    {
        [SerializeField] 
        private int m_level;
        [SerializeField]
        private Bounds m_bounds;
        
        [NonSerialized]
        private HLODTreeNodeContainer m_container;
        [SerializeField]
        private List<int> m_childTreeNodeIds = new List<int>();

        [SerializeField]
        private List<int> m_highObjectIds = new List<int>();
        [SerializeField]
        private List<int> m_lowObjectIds = new List<int>();

        private Dictionary<int, GameObject> m_highObjects = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> m_lowObjects = new Dictionary<int, GameObject>();

        private Dictionary<int, GameObject> m_loadedHighObjects;
        private Dictionary<int, GameObject> m_loadedLowObjects;

        public int Level
        {
            set { m_level = value; }
            get { return m_level; }
        }
        public Bounds Bounds
        {
            set { m_bounds = value; }
            get { return m_bounds; }
        }

        public List<int> HighObjectIds
        {
            get { return m_highObjectIds; }
        }

        public List<int> LowObjectIds
        {
            get { return m_lowObjectIds; }
        }

        enum State
        {
            Release,
            Low,
            High,
        }

        private FSM<State> m_fsm = new FSM<State>();
        private State m_lastState = State.Release;

        private HLODControllerBase m_controller;
        private ISpaceManager m_spaceManager;
        private HLODTreeNode m_parent;

        private float m_boundsLength;
        private float m_distance;
        
        private bool m_isVisible;
        private bool m_isVisibleHierarchy;

        public HLODTreeNode()
        {
        }

        public void SetContainer(HLODTreeNodeContainer container)
        {
            m_container = container;
            ForEachChildTreeNode(node =>
            {
                node.SetContainer(container);
            });
        }
        public void SetChildTreeNode(List<HLODTreeNode> childNodes)
        {
            ClearChildTreeNode();
            m_childTreeNodeIds.Capacity = childNodes.Count;

            for (int i = 0; i < childNodes.Count; ++i)
            {
                int id = m_container.Add(childNodes[i]);
                m_childTreeNodeIds.Add(id);
                childNodes[i].SetContainer(m_container);
            }
        }

        public int GetChildTreeNodeCount()
        {
            return m_childTreeNodeIds.Count;
        }

        public HLODTreeNode GetChildTreeNode(int index)
        {
            int id = m_childTreeNodeIds[index];
            return m_container.Get(id);
        }

        public void ClearChildTreeNode()
        {
            for (int i = 0; i < m_childTreeNodeIds.Count; ++i)
            {
                m_container.Remove(m_childTreeNodeIds[i]);
            }
            m_childTreeNodeIds.Clear();
        }

        private void ForEachChildTreeNode(Action<HLODTreeNode> action)
        {
            for (int i = 0; i < m_childTreeNodeIds.Count; ++i)
            {
                var childTreeNode = m_container.Get(m_childTreeNodeIds[i]);
                action(childTreeNode);
            }
        }

        private bool ForEachChildTreeNodeAllTrue(Func<HLODTreeNode, bool> func)
        {
            for (int i = 0; i < m_childTreeNodeIds.Count; ++i)
            {
                var childTreeNode = m_container.Get(m_childTreeNodeIds[i]);
                if (func(childTreeNode) == false)
                    return false;
            }

            return true;
        }



        public void Initialize(HLODControllerBase controller, ISpaceManager spaceManager, HLODTreeNode parent)
        {

            ForEachChildTreeNode(node =>
            {
                node.Initialize(controller, spaceManager, this);
            });
            
            //set to initialize state
            m_fsm.ChangeState(State.Release);

            m_fsm.RegisterIsReadyToEnterFunction(State.Release, IsReadyToEnterRelease);
            m_fsm.RegisterEnteredFunction(State.Release, OnEnteredRelease);

            m_fsm.RegisterEnteringFunction(State.Low, OnEnteringLow);
            m_fsm.RegisterIsReadyToEnterFunction(State.Low, IsReadyToEnterLow);
            m_fsm.RegisterEnteredFunction(State.Low, OnEnteredLow);
            m_fsm.RegisterExitedFunction(State.Low, OnExitedLow);

            m_fsm.RegisterEnteringFunction(State.High, OnEnteringHigh);
            m_fsm.RegisterIsReadyToEnterFunction(State.High, IsReadyToEnterHigh);
            m_fsm.RegisterEnteredFunction(State.High, OnEnteredHigh);
            m_fsm.RegisterExitedFunction(State.High, OnExitedHigh);
            
            m_controller = controller;
            m_spaceManager = spaceManager;
            m_parent = parent;
            
            m_isVisible = true;
            m_isVisibleHierarchy = true;

            m_boundsLength = m_bounds.extents.x * m_bounds.extents.x + m_bounds.extents.z * m_bounds.extents.z;
        }

        public bool IsLoadDone()
        {
            if (m_parent == null && m_fsm.CurrentState == State.Release)
                return false;
            
            if (m_fsm.LastState != m_fsm.CurrentState)
                return false;

            if (m_fsm.CurrentState == State.High)
            {
                if (ForEachChildTreeNodeAllTrue(node => node.IsLoadDone()) == false)
                    return false;
                
                return m_highObjectIds.Count == m_highObjects.Count;
            }
            else if ( m_fsm.CurrentState == State.Low)
            {
                return m_lowObjectIds.Count == m_lowObjects.Count;
            }

            return true;
        }

        public void Cull(bool isCull)
        {
            if (isCull)
            {
                Release();
            }
            else
            {
                if (m_fsm.LastState == State.Release)
                {
                    m_fsm.ChangeState(State.Low);
                }
            }
        }

        #region FSM functions

        bool IsReadyToEnterRelease()
        {
            if (m_parent == null)
                return true;

            return m_parent.m_fsm.CurrentState != State.High;
        }
        
        void OnEnteredRelease()
        {
            ForEachChildTreeNode(node =>
            {
                node.m_isVisible = false;
                node.Release();
            });
        }

        void OnEnteringLow()
        {
            if ( m_loadedLowObjects == null ) 
                m_loadedLowObjects = new Dictionary<int, GameObject>();
            
            if (m_lowObjects.Count == m_lowObjectIds.Count)
                return;
            
            for (int i = 0; i < m_lowObjectIds.Count; ++i)
            {
                int id = m_lowObjectIds[i];

                m_controller.GetLowObject(id, Level, m_distance, o =>
                {
                    o.SetActive(false);
                    m_loadedLowObjects.Add(id, o);
                });
            }
        }
        bool IsReadyToEnterLow()
        {
            return m_loadedLowObjects.Count == m_lowObjectIds.Count;
        }
        
        
        void OnEnteredLow()
        {
            m_lowObjects = m_loadedLowObjects;
            m_loadedLowObjects = null;

            ForEachChildTreeNode(node =>
            {
                node.Release();
            });

        }

        void OnExitedLow()
        {
            foreach (var item in m_lowObjects)
            {
                item.Value.SetActive(false);
                m_controller.ReleaseLowObject(item.Key);
            }
            m_lowObjects.Clear();
        }

        void OnEnteringHigh()
        {
            //child low mesh should be load before change to high.
            ForEachChildTreeNode(node =>
            {
                node.m_isVisible = false;
                node.m_fsm.ChangeState(State.Low);
            });

            if ( m_loadedHighObjects == null )
                m_loadedHighObjects = new Dictionary<int, GameObject>();
            
            if (m_loadedHighObjects.Count == m_highObjectIds.Count)
                return;

            
            for (int i = 0; i < m_highObjectIds.Count; ++i)
            {
                int id = m_highObjectIds[i];

                m_controller.GetHighObject(id, Level, m_distance, (o =>
                {
                    o.SetActive(false);
                    m_loadedHighObjects.Add(id, o);
                }));
            }
        }

        bool IsReadyToEnterHigh()
        {
            if ( m_loadedHighObjects.Count != m_highObjectIds.Count )
                return false;

            return ForEachChildTreeNodeAllTrue(node => node.m_fsm.CurrentState != State.Release);
        }
        void OnEnteredHigh()
        {
            ForEachChildTreeNode(node =>
            {
                node.m_isVisible = true;
                
            });
            
            m_highObjects = m_loadedHighObjects;
            m_loadedHighObjects = null;
        }

        void OnExitedHigh()
        {
            foreach (var item in m_highObjects)
            {
                item.Value.SetActive(false);
                m_controller.ReleaseHighObject(item.Key);
            }
            m_highObjects.Clear();
            
            ForEachChildTreeNode(node =>
            {
                node.Release();
                node.m_isVisible = false;
            });
        }


        void Release()
        {
            m_fsm.ChangeState(State.Release);
        }
        #endregion
        

        public void Update(float lodDistance)
        {
            m_distance = m_spaceManager.GetDistanceSqure(m_bounds) - m_boundsLength;

            //Change state if a change to another state is needed immediately after changing the state.
            var beforeState = m_fsm.CurrentState;
            do
            {
                beforeState = m_fsm.CurrentState;
                if (m_fsm.LastState != State.Release)
                {
                    if (m_spaceManager.IsHigh(lodDistance, m_bounds))
                    {
                        //if isVisible is false, it loaded from parent but not showing. 
                        //We have to wait for showing after then, change state to high.
                        if (m_fsm.CurrentState == State.Low &&
                            m_isVisible == true)
                        {
                            m_fsm.ChangeState(State.High);
                        }
                    }
                    else
                    {
                        m_fsm.ChangeState(State.Low);
                    }
                }

                m_fsm.Update();
            } while (beforeState != m_fsm.CurrentState);

            UpdateVisible();

            ForEachChildTreeNode(node =>
            {
                node.Update(lodDistance);
            });
        }

        private void UpdateVisible()
        {
            if (m_parent != null)
            {
                m_isVisibleHierarchy = m_isVisible && m_parent.m_isVisibleHierarchy;
            }
            else
            {
                m_isVisibleHierarchy = m_isVisible;    
            }

            foreach (var item in m_highObjects)
            {
                item.Value.SetActive(m_isVisibleHierarchy);
            }

            foreach (var item in m_lowObjects)
            {
                item.Value.SetActive(m_isVisibleHierarchy);
            }
        }

    }

}