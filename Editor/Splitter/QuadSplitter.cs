using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    public class QuadSplitter : SplitterBase
    {
        protected override ChildData[] GetData(HLOD hlod)
        {
            ChildData[] data = new ChildData[4];

            float size = hlod.Bounds.size.x;
            float extend = size * 0.5f;
            float offset = extend * 0.5f;
            Vector3 center = hlod.Bounds.center;

            data[0].Bounds = new Bounds(center + new Vector3(-offset, 0.0f, -offset), new Vector3(extend, size, extend));
            data[0].GameObject = new GameObject(hlod.name + "_1");
            data[1].Bounds = new Bounds(center + new Vector3(offset, 0.0f, -offset), new Vector3(extend, size, extend));
            data[1].GameObject = new GameObject(hlod.name + "_1");
            data[2].Bounds = new Bounds(center + new Vector3(-offset, 0.0f, offset), new Vector3(extend, size, extend));
            data[2].GameObject = new GameObject(hlod.name + "_1");
            data[3].Bounds = new Bounds(center + new Vector3(offset, 0.0f, offset), new Vector3(extend, size, extend));
            data[3].GameObject = new GameObject(hlod.name + "_1");

            return data;
        }
    }
}