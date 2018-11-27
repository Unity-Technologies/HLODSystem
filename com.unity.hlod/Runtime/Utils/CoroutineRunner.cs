using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public class CoroutineRunner : MonoBehaviour
    {
        class RunnerEnumerator : IEnumerator
        {
            private CoroutineRunner m_Runner;
            private Stack<IEnumerator> m_CoroutineStack = new Stack<IEnumerator>();
            private List<Coroutine> m_BranchList = new List<Coroutine>();
            public RunnerEnumerator(CoroutineRunner runner, IEnumerator coroutine)
            {
                m_Runner = runner;
                m_CoroutineStack.Push(coroutine);
            }

            public object Current
            {
                get { return m_CoroutineStack.Count > 0 ? m_CoroutineStack.Peek().Current : null; }
            }

            public bool MoveNext()
            {
                while (m_CoroutineStack.Count > 0)
                {
                    IEnumerator coroutine = m_CoroutineStack.Peek();
                    if (coroutine.MoveNext())
                    {
                        object cur = Current;

                        if (cur is IEnumerator)
                        {
                            m_CoroutineStack.Push(cur as IEnumerator);
                        }
                        else if (cur is BranchCoroutine)
                        {
                            var branch = cur as BranchCoroutine;
                            m_BranchList.Add(m_Runner.RunCoroutine(branch.GetBranch(), false));
                        }
                        else if (cur is WaitForBranches)
                        {
                            m_CoroutineStack.Push(WaitForBranchesImpl());
                        }
                        else
                        {
                            return true;
                        }
                        
                    }
                    else
                    {
                        m_CoroutineStack.Pop();
                    }
                }

                return false;
            }

            public void Reset()
            {
                while (m_CoroutineStack.Count > 1)
                    m_CoroutineStack.Pop();

                m_CoroutineStack.Peek().Reset();
            }

            private IEnumerator WaitForBranchesImpl()
            {
                Coroutine[] branches = m_BranchList.ToArray();
                for (int i = 0; i < branches.Length; ++i)
                {
                    yield return branches[i];
                }
            }
        }

        public Coroutine RunCoroutine(IEnumerator coroutine, bool autoRemoveGameObject = true)
        {
            return StartCoroutine(Run(coroutine, autoRemoveGameObject));
        }

        private IEnumerator Run(IEnumerator coroutine, bool autoRemoveGameObject)
        {
            yield return new RunnerEnumerator(this, coroutine);

            if (autoRemoveGameObject == true)
            {
                DestroyImmediate(gameObject);
            }
        }
    }

}