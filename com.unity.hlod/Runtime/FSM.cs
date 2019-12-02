using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class FSM<T> where T : struct
    {
        private T m_currentState = default;
        private T m_lastState = default;
        private T m_transactionTargetState = default;
        
        //event ordering
        //exiting -> entering -> exited -> entered
        class Functions
        {
            public Action EnteringFunction;
            public Func<bool> IsReadyToEnterFunction;
            public Action EnteredFunction;

            public Action ExitingFunction;
            public Func<bool> IsReadyToExitFunction;
            public Action ExitedFunction;
        }

        private Dictionary<T, Functions> m_functions = new Dictionary<T, Functions>();

        public T CurrentState => m_currentState;
        public T LastState => m_lastState;

        public void Update()
        {
            //transaction still progressing. have to check to finish.
            if (Compare(m_currentState, m_transactionTargetState) == false)
            {
                if (GetFunctions(m_currentState).IsReadyToExitFunction?.Invoke() == false)
                    return;
                if (GetFunctions(m_transactionTargetState).IsReadyToEnterFunction?.Invoke() == false)
                    return;
                
                //the transaction has been finished.
                GetFunctions(m_currentState).ExitedFunction?.Invoke();
                GetFunctions(m_transactionTargetState).EnteredFunction?.Invoke();

                m_currentState = m_transactionTargetState;
            }
            
            Debug.Assert(Compare(m_currentState, m_transactionTargetState));
            
            //Here the transaction is always complete.
            //We have to check to start a new transaction.
            if (Compare(m_currentState, m_lastState) == false)
            {
                StartTransaction(m_currentState, m_lastState);
            }
        }

        public void ChangeState(T state)
        {
            m_lastState = state;
            if (Compare(m_currentState, m_lastState))
                return;
            
            //it means, completed the last transaction. we should do it immediately. 
            if (Compare(m_currentState, m_transactionTargetState))
            {
                StartTransaction(m_currentState, m_lastState);
            }
        }

        public void RegisterEnteringFunction(T state, Action func)
        {
            GetFunctions(state).EnteringFunction = func;
        }
        public void UnregisterEnteringFunction(T state)
        {
            GetFunctions(state).EnteringFunction = null;
        }

        public void RegisterIsReadyToEnterFunction(T state, Func<bool> func)
        {
            GetFunctions(state).IsReadyToEnterFunction = func;
        }
        public void UnregisterIsReadyToEnterFunction(T state)
        {
            GetFunctions(state).IsReadyToEnterFunction = null;
        }
        public void RegisterEnteredFunction(T state, Action func)
        {
            GetFunctions(state).EnteredFunction = func;
        }
        public void UnregisterEnteredFunction(T state)
        {
            GetFunctions(state).EnteredFunction = null;
        }

        public void RegisterExitingFunction(T state, Action func)
        {
            GetFunctions(state).ExitingFunction = func;
        }
        public void UnregisterExitingFunction(T state)
        {
            GetFunctions(state).ExitingFunction = null;
        }
        public void RegisterIsReadyToExitFunction(T state, Func<bool> func)
        {
            GetFunctions(state).IsReadyToExitFunction = func;
        }
        public void UnregisterIsReadyToExitFunction(T state)
        {
            GetFunctions(state).IsReadyToExitFunction = null;
        }
        public void RegisterExitedFunction(T state, Action func)
        {
            GetFunctions(state).ExitedFunction = func;
        }
        public void UnregisterExitedFunction(T state)
        {
            GetFunctions(state).ExitedFunction = null;
        }
        
        private Functions GetFunctions(T state)
        {
            if ( m_functions.ContainsKey(state) == false )
                m_functions.Add(state, new Functions());

            return m_functions[state];
        }

        private void StartTransaction(T current, T target)
        {
            m_transactionTargetState = target;
            
            GetFunctions(current).ExitingFunction?.Invoke();
            GetFunctions(target).EnteringFunction?.Invoke();
        }

        private static bool Compare(T lhs, T rhs)
        {
            return EqualityComparer<T>.Default.Equals(lhs, rhs);
        }

    }

}