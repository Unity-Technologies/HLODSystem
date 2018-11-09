using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    public class QuadSplitter : ISplitter
    {
        public void Split(HLOD hlod)
        {
            if (hlod == null || hlod.HighRoot == null)
                return;

            GameObject highRoot = hlod.HighRoot;
            float size = hlod.Bounds.size.x;
            float extend = size * 0.5f;
            float offset = extend * 0.5f;
            Vector3 center = hlod.Bounds.center;

            var childGroups = highRoot.GetComponentsInChildren<LODGroup>();

            Bounds[] childBounds =
            {
                new Bounds(center + new Vector3(-offset, 0.0f, -offset), new Vector3(extend, extend, extend)),
                new Bounds(center + new Vector3(offset, 0.0f, -offset), new Vector3(extend, extend, extend)),
                new Bounds(center + new Vector3(-offset, 0.0f, offset), new Vector3(extend, extend, extend)),
                new Bounds(center + new Vector3(offset, 0.0f, offset), new Vector3(extend, extend, extend))
            };
            GameObject[] childObjects =
            {
                new GameObject(hlod.name + "_1"),
                new GameObject(hlod.name + "_2"),
                new GameObject(hlod.name + "_3"),
                new GameObject(hlod.name + "_4")
            };

            for (int i = 0; i < childGroups.Length; ++i)
            {
                for (int c = 0; c < 4; ++c)
                {
                    if (childBounds[c].Contains(childGroups[i].transform.position) == false)
                    {
                        continue;
                    }

                    childGroups[i].transform.SetParent(childObjects[c].transform);
                }
            }

            for (int c = 0; c < 4; ++c)
            {
                childObjects[c].transform.SetParent(highRoot.transform);

                HLODCreator.Setup(childObjects[c]);
                HLOD childHLOD = childObjects[c].GetComponent<HLOD>();
                GameObject low = childHLOD.LowRoot;
                GameObject high = childHLOD.HighRoot;

                EditorUtility.CopySerialized(hlod, childHLOD);

                childHLOD.Bounds = childBounds[c];
                childHLOD.LowRoot = low;
                childHLOD.HighRoot = high;

                HLODCreator.Create(childHLOD);
            }
        }


    }

}