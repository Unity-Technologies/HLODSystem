using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;

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
            if (hlod == null)
                return;

            var childTargets = ObjectUtils.HLODTargets(hlod.gameObject);
            var data = GetData(hlod);

            for (int c = 0; c < data.Length; ++c)
            {
                data[c].GameObject.transform.SetParent(hlod.transform);
            }

            for (int i = 0; i < childTargets.Count; ++i)
            {
                for (int c = 0; c < data.Length; ++c)
                {
                    if (data[c].Bounds.Contains(childTargets[i].transform.position) == false)
                    {
                        continue;
                    }

                    ObjectUtils.HierarchyMove(childTargets[i], hlod.gameObject, data[c].GameObject);
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
                    HLODCreator.Setup(data[c].GameObject);
                    HLOD childHLOD = data[c].GameObject.GetComponent<HLOD>();
                    EditorUtility.CopySerialized(hlod, childHLOD);

                    childHLOD.Bounds = data[c].Bounds;

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
                    GameObject go = ObjectUtils.CopyGameObjectWithComponent(curTransform.gameObject);
                    go.transform.SetParent(parent);    
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