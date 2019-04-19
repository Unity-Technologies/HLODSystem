using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public class CustomCoroutine : IEnumerator
    {
        class AsyncOperationEnumerator : IEnumerator
        {
            private AsyncOperation m_operation;

            public AsyncOperationEnumerator(AsyncOperation operation)
            {
                m_operation = operation;
            }
            public bool MoveNext()
            {
                return m_operation.isDone == false;
            }

            public void Reset()
            {
            }

            public object Current
            {
                get { return null; }
            }
        }
        private Stack<IEnumerator> m_routineStack = new Stack<IEnumerator>();
        private List<IEnumerator> m_branchList = new List<IEnumerator>();

        public CustomCoroutine(IEnumerator routine)
        {
            m_routineStack.Push(routine);
        }

        public bool MoveNext()
        {
            while (m_routineStack.Count > 0)
            {
                IEnumerator coroutine = m_routineStack.Peek();
                if (coroutine.MoveNext())
                {
                    object cur = Current;

                    if (cur == null)
                    {
                        return true;
                    }
                    else if (cur is IEnumerator)
                    {
                        m_routineStack.Push(cur as IEnumerator);
                    }
                    else if (cur is AsyncOperation)
                    {
                        m_routineStack.Push(new AsyncOperationEnumerator(cur as AsyncOperation));
                    }
                    else if (cur is BranchCoroutine)
                    {
                        var branch = cur as BranchCoroutine;
                        m_branchList.Add(CoroutineRunner.RunCoroutine(branch.GetBranch()));
                    }
                    else if (cur is WaitForBranches)
                    {
                        m_routineStack.Push(WaitForBranchesImpl(cur as WaitForBranches));
                    }
                    else
                    {
                        Debug.LogWarning("Not support yield instruction in CustomCoroutine. " + cur.GetType().Name);
                        return true;
                    }
                        
                }
                else
                {
                    m_routineStack.Pop();
                }
            }

            return false;
        }

        public void Reset()
        {
            while (m_routineStack.Count > 1)
                m_routineStack.Pop();

            m_routineStack.Peek().Reset();
        }

        public object Current { get { return m_routineStack.Count > 0 ? m_routineStack.Peek().Current : null; } }


        private IEnumerator WaitForBranchesImpl(WaitForBranches obj)
        {
            IEnumerator[] branches = m_branchList.ToArray();
            for (int i = 0; i < branches.Length; ++i)
            {
                yield return branches[i];
                obj.OnProgress((float)i / (float)branches.Length);
            }
        }
    }



    //Editor is not support coroutine
    //so, if it run on editor, it have to run coroutine manually.
    public class CoroutineRunner : MonoBehaviour
    {
        public static IEnumerator RunCoroutine(IEnumerator coroutine)
        {
            return Run(coroutine);
        }

#if UNITY_EDITOR
        private static List<CustomCoroutine> s_coroutines;
        [InitializeOnLoadMethod]
        private static void Setup()
        {
            s_coroutines = new List<CustomCoroutine>();
            EditorApplication.update+= EditorUpdate;
        }

        private static void EditorUpdate()
        {
            for (int i = 0; i < s_coroutines.Count; ++i)
            {
                if (s_coroutines[i].MoveNext() == false)
                {
                    s_coroutines.RemoveAt(i);
                    i -= 1;
                }
            }
        }

        private static CustomCoroutine Run(IEnumerator routine)
        {
            var coroutine = new CustomCoroutine(routine);
            s_coroutines.Add(coroutine);
            return coroutine;
        }
#else
        private static GameObject gameObjectInstance = null;
        private static IEnumerator Wrapping(Coroutine c)
        {
            yield return c;
        }
        private static IEnumerator Run(IEnumerator routine)
        {
            if (gameObjectInstance == null)
            {
                gameObjectInstance = new GameObject("Runner");
                gameObjectInstance.hideFlags = HideFlags.HideAndDontSave;
                gameObjectInstance.AddComponent<CoroutineRunner>();
            }

            var runner = gameObjectInstance.GetComponent<CoroutineRunner>();
            var coroutine = runner.StartCoroutine(routine);
            return Wrapping(coroutine);
        }
#endif
    }

}