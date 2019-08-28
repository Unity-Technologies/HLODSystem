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

        //event ordering
        //exiting -> entering -> exited -> entered
        private Dictionary<T, Func<IEnumerator>> m_enteringFunctions = new Dictionary<T, Func<IEnumerator>>();
        private Dictionary<T, Func<IEnumerator>> m_exitingFunctions = new Dictionary<T, Func<IEnumerator>>();
        private Dictionary<T, Action> m_enteredFunctions = new Dictionary<T, Action>();
        private Dictionary<T, Action> m_exitedFunctions = new Dictionary<T, Action>();

        private CustomCoroutine m_currentCoroutine = null;
        private CustomCoroutine m_reservedCoroutine = null;

        public T CurrentState => m_currentState;
        public T LastState => m_lastState;

        public void Update()
        {
            if (m_currentCoroutine == null)
            {
                m_currentCoroutine = m_reservedCoroutine;
                m_reservedCoroutine = null;
            }

            if (m_currentCoroutine == null)
                return;

            if (m_currentCoroutine.MoveNext() == false)
            {
                m_currentCoroutine = null;
            }
        }

        public void ChangeState(T state)
        {
            if (EqualityComparer<T>.Default.Equals(state, m_lastState))
                return;

            m_lastState = state;

            var routine = ChangeStateRoutine(state);
            m_reservedCoroutine = new CustomCoroutine(routine);
            
        }

        

        public void RegisterEnteringFunction(T state, Func<IEnumerator> func)
        {
            AddOrUpdate(m_enteringFunctions, state, func);
        }
        public void UnregisterEnteringFunction(T state)
        {
            m_enteringFunctions.Remove(state);
        }

        public void RegisterExitingFunction(T state, Func<IEnumerator> func)
        {
            AddOrUpdate(m_exitingFunctions, state, func);
        }
        public void UnregisterExitingFunction(T state)
        {
            m_exitingFunctions.Remove(state);
        }

        public void RegisterEnteredFunction(T state, Action func)
        {
            AddOrUpdate(m_enteredFunctions, state, func);
        }
        public void UnregisterEnteredFunction(T state)
        {
            m_enteredFunctions.Remove(state);
        }

        public void RegisterExitedFunction(T state, Action func)
        {
            AddOrUpdate(m_exitedFunctions, state, func);
        }
        public void UnregisterExitedFunction(T state)
        {
            m_exitedFunctions.Remove(state);
        }

        private IEnumerator ChangeStateRoutine( T targetState )
        {
            if (EqualityComparer<T>.Default.Equals(targetState, m_currentState))
                yield break;


            Func<IEnumerator> entering = GetValue(m_enteringFunctions, targetState);
            Func<IEnumerator> exiting = GetValue(m_exitingFunctions, m_currentState);
            Action entered = GetValue(m_enteredFunctions, targetState);
            Action exited = GetValue(m_exitedFunctions, m_currentState);

            //exiting -> entering -> exited -> entered
            if (exiting != null) yield return exiting();
            if (entering != null) yield return entering();
            if (exited != null) exited();
            if (entered != null) entered();

            m_currentState = targetState;
        }
        
        private void AddOrUpdate<KEY, VALUE>(Dictionary<KEY, VALUE> container, KEY key, VALUE value)
        {
            if (container.ContainsKey(key))
            {
                container[key] = value;
            }
            else
            {
                container.Add(key, value);
            }
        }

        private VALUE GetValue<KEY, VALUE>(Dictionary<KEY, VALUE> container, KEY key)
        {
            VALUE value;
            if (container.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return default(VALUE);
            }
        }
    }

}