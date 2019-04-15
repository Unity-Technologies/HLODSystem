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
        private IEnumerator m_lastRun = null;

        //event ordering
        //exiting -> entering -> exited -> entered
        private Dictionary<T, Func<IEnumerator>> m_enteringFunctions = new Dictionary<T, Func<IEnumerator>>();
        private Dictionary<T, Func<IEnumerator>> m_exitingFunctions = new Dictionary<T, Func<IEnumerator>>();
        private Dictionary<T, Action> m_enteredFunctions = new Dictionary<T, Action>();
        private Dictionary<T, Action> m_exitedFunctions = new Dictionary<T, Action>();

        public T State
        {
            get { return m_currentState; }
        }

        public IEnumerator LastRunEnumerator
        {
            get { return m_lastRun; }
        }

        public void ChangeState(T state)
        {
            if (EqualityComparer<T>.Default.Equals(state, m_currentState))
                return;

            Func<IEnumerator> entering = GetValue(m_enteringFunctions, state);
            Func<IEnumerator> exiting = GetValue(m_exitingFunctions, m_currentState);
            Action entered = GetValue(m_enteredFunctions, state);
            Action exited = GetValue(m_exitedFunctions, m_currentState);

            m_lastRun = CoroutineRunner.RunCoroutine(Changing(m_lastRun, entering, exiting, entered, exited));
            m_currentState = state;
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


        private IEnumerator Changing(IEnumerator lastRun, Func<IEnumerator> entering, Func<IEnumerator> exiting, Action entered, Action exited)
        {
            //wait until the last coroutine finished.
            yield return lastRun;



            //exiting -> entering -> exited -> entered
            if (exiting != null) yield return exiting();
            if (entering != null) yield return entering();
            if (exited != null) exited();
            if (entered != null) entered();
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