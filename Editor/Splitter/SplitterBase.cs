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

                    childGroups[i].transform.SetParent(data[c].GameObject.transform);
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

                    HLODCreator.Create(childHLOD);
                }
            }
        }
    }

}