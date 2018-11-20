using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    public abstract class SplitterBase : ISplitter
    {
        protected struct ChildData
        {
            public Bounds Bounds;
            public GameObject GameObject;
        }

        protected abstract ChildData[] GetData(HLOD hlod);

        public void Split(HLOD hlod)
        {
            if (hlod == null || hlod.HighRoot == null)
                return;

            GameObject highRoot = hlod.HighRoot;
            var childGroups = highRoot.GetComponentsInChildren<LODGroup>();
            var data = GetData(hlod);

            for (int i = 0; i < childGroups.Length; ++i)
            {
                for (int c = 0; c < data.Length; ++c)
                {
                    if (data[c].Bounds.Contains(childGroups[i].transform.position) == false)
                    {
                        continue;
                    }

                    ChildMove(childGroups[i].gameObject, highRoot, data[c].GameObject);
                }
            }

            for (int c = 0; c < data.Length; ++c)
            {                
                if (data[c].GameObject.transform.childCount == 0)
                {
                    Object.DestroyImmediate(data[c].GameObject);
                }
                else
                {
                    data[c].GameObject.transform.SetParent(highRoot.transform);

                    HLODCreator.Setup(data[c].GameObject);
                    HLOD childHLOD = data[c].GameObject.GetComponent<HLOD>();
                    GameObject low = childHLOD.LowRoot;
                    GameObject high = childHLOD.HighRoot;

                    EditorUtility.CopySerialized(hlod, childHLOD);

                    childHLOD.Bounds = data[c].Bounds;
                    childHLOD.LowRoot = low;
                    childHLOD.HighRoot = high;

                    if (childHLOD.RecursiveGeneration == true)
                    {
                        if (childHLOD.Bounds.size.x > childHLOD.MinSize)
                        {
                            ISplitter splitter = new OctSplitter();
                            splitter.Split(childHLOD);
                        }
                    }
                }
            }
        }


        private Dictionary<GameObject, Dictionary<Transform, GameObject>> m_LinkObjectCache =new Dictionary<GameObject, Dictionary<Transform, GameObject>>();

        private void ChildMove(GameObject child, GameObject originRoot, GameObject targetRoot)
        {
            if (m_LinkObjectCache.ContainsKey(targetRoot) == false)
            {
                m_LinkObjectCache.Add(targetRoot, new Dictionary<Transform, GameObject>());
            }


            Stack<Transform> traceStack = new Stack<Transform>();
            Dictionary<Transform, GameObject> cache = m_LinkObjectCache[targetRoot];
            traceStack.Push(child.transform);

            while (traceStack.Peek().parent != originRoot.transform)
            {
                traceStack.Push(traceStack.Peek().parent);
            }

            Transform parent = targetRoot.transform;
            //last is child
            //we don't need to process last one.
            while (traceStack.Count > 1)
            {
                Transform curTransform = traceStack.Pop();
                if (cache.ContainsKey(curTransform) == false)
                {
                    GameObject go = new GameObject(curTransform.name);
                    go.transform.SetParent(parent);

                    var allComponents = curTransform.GetComponents<Component>();
                    for (int i = 0; i < allComponents.Length; ++i)
                    {
                        System.Type type = allComponents[i].GetType();
                        Component component = go.GetComponent(type);
                        if (component == null)
                        {
                            component = go.AddComponent(type);
                        }

                        EditorUtility.CopySerialized(allComponents[i], component);
                    }

                    cache.Add(curTransform, go);
                }

                GameObject linkGO = cache[curTransform];
                parent = linkGO.transform;
            }

            Transform oldParent = child.transform.parent;
            child.transform.SetParent(parent);

            //remove the object if empty because moves object.
            if (oldParent.childCount == 0)
            {
                Object.DestroyImmediate(oldParent.gameObject);
            }
        }
    }

}