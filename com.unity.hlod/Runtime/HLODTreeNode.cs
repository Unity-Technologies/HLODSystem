using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    [Serializable]
    public class HLODTreeNode
    {
        [SerializeField]
        private Bounds m_bounds;
        [SerializeField]
        private List<HLODTreeNode> m_childTreeNodes;

        [SerializeField]
        private List<int> m_highObjectIds = new List<int>();
        [SerializeField]
        private List<int> m_lowObjectIds = new List<int>();

        private List<GameObject> m_highObjects = new List<GameObject>();
        private List<GameObject> m_lowObjects = new List<GameObject>();

        public Bounds Bounds
        {
            set { m_bounds = value; }
            get { return m_bounds; }
        }

        public List<HLODTreeNode> ChildNodes
        {
            set { m_childTreeNodes = value;}
            get { return m_childTreeNodes; }
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

        private ControllerBase m_controller;
        private ActiveHLODTreeNodeManager m_activeManager;
        private ISpaceManager m_spaceManager;

        public void Initialize(ControllerBase controller, ISpaceManager spaceManager, ActiveHLODTreeNodeManager activeManager)
        {
            for (int i = 0; i < m_childTreeNodes.Count; ++i)
            {
                m_childTreeNodes[i].Initialize(controller, spaceManager, activeManager);
            }

            //set to initialize state
            m_fsm.ChangeState(State.Release);

            m_fsm.RegisterEnteredFunction(State.Release, OnEnteredRelease);

            m_fsm.RegisterEnteringFunction(State.Low, OnEnteringLow);
            m_fsm.RegisterEnteredFunction(State.Low, OnEnteredLow);
            m_fsm.RegisterExitedFunction(State.Low, OnExitedLow);

            m_fsm.RegisterEnteringFunction(State.High, OnEnteringHigh);
            m_fsm.RegisterEnteredFunction(State.High, OnEnteredHigh);
            m_fsm.RegisterExitedFunction(State.High, OnExitedHigh);

            m_controller = controller;
            m_spaceManager = spaceManager;
            m_activeManager = activeManager;
        }

        public void Cull()
        {
            Release();
        }

        #region FSM functions

        void OnEnteredRelease()
        {
            for (int i = 0; i < m_childTreeNodes.Count; ++i)
            {
                m_childTreeNodes[i].Release();
                m_activeManager.Deactivate(m_childTreeNodes[i]);
            }

        }

        IEnumerator OnEnteringLow()
        {
            for (int i = 0; i < m_childTreeNodes.Count; ++i)
            {
                m_activeManager.Deactivate(m_childTreeNodes[i]);
            }

            yield return LoadLowMeshes();
        }

        void OnEnteredLow()
        {
            for (int i = 0; i < m_lowObjects.Count; ++i)
            {
                m_lowObjects[i].SetActive(true);
            }

            for (int i = 0; i < m_childTreeNodes.Count; ++i)
            {
                m_childTreeNodes[i].Release();
            }
            
        }

        void OnExitedLow()
        {
            for (int i = 0; i < m_lowObjectIds.Count; ++i)
            {
                m_controller.ReleaseLowObject(m_lowObjectIds[i]);
            }

            m_lowObjects.Clear();
        }

        IEnumerator OnEnteringHigh()
        {
            //child low mesh should be load before change to high.
            for (int i = 0; i < m_childTreeNodes.Count; ++i)
            {
                m_childTreeNodes[i].m_fsm.ChangeState(State.Low);
            }

            for (int i = 0; i < m_highObjectIds.Count; ++i)
            {
                int id = m_highObjectIds[i];
                yield return m_controller.GetHighObject(id, go =>
                {
                    go.SetActive(false);
                    m_highObjects.Add(go);
                });

            }

            //wait for child nodes were finished.
            //it needs because avoid flickering.
            for (int i = 0; i < m_childTreeNodes.Count; ++i)
            {
                yield return m_childTreeNodes[i].m_fsm.LastRunEnumerator;
            }
        }
        void OnEnteredHigh()
        {
            for (int i = 0; i < m_childTreeNodes.Count; ++i)
            {
                m_activeManager.Activate(m_childTreeNodes[i]);
                m_childTreeNodes[i].m_fsm.ChangeState(State.Low);
            }

            for (int i = 0; i < m_highObjects.Count; ++i)
            {
                m_highObjects[i].SetActive(true);
            }
        }

        void OnExitedHigh()
        {
            for (int i = 0; i < m_highObjectIds.Count; ++i)
            {
                m_controller.ReleaseHighObject(m_highObjectIds[i]);
            }

            m_highObjects.Clear();

        }

        IEnumerator LoadLowMeshes()
        {
            for (int i = 0; i < m_lowObjectIds.Count; ++i)
            {
                int id = m_lowObjectIds[i];
                yield return m_controller.GetLowObject(id, go =>
                {
                    go.SetActive(false);
                    m_lowObjects.Add(go);
                });

            }
        }

        void Release()
        {
            m_fsm.ChangeState(State.Release);
        }
        #endregion
        

        public void Update()
        {
            if (m_spaceManager.IsHigh(m_bounds))
            {
                if ( m_fsm.State == State.Release)
                    m_fsm.ChangeState(State.Low);
                m_fsm.ChangeState(State.High);
            }
            else
            {
                m_fsm.ChangeState(State.Low);
            }
        }


    }

}