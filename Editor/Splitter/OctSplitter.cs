using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    public class OctSplitter : SplitterBase
    {
        protected override ChildData[] GetData(HLOD hlod)
        {
            ChildData[] data = new ChildData[8];
            float size = hlod.Bounds.size.x;
            float extend = size * 0.5f;
            float offset = extend * 0.5f;
            Vector3 center = hlod.Bounds.center;

            data[0].Bounds = new Bounds(center + new Vector3(-offset, -offset, -offset),
                new Vector3(extend, extend, extend));
            data[0].GameObject = new GameObject(hlod.name + "_1");
            data[1].Bounds = new Bounds(center + new Vector3(offset, -offset, -offset),
                new Vector3(extend, extend, extend));
            data[1].GameObject = new GameObject(hlod.name + "_2");
            data[2].Bounds = new Bounds(center + new Vector3(-offset, -offset, offset),
                new Vector3(extend, extend, extend));
            data[2].GameObject = new GameObject(hlod.name + "_3");
            data[3].Bounds = new Bounds(center + new Vector3(offset, -offset, offset),
                new Vector3(extend, extend, extend));
            data[3].GameObject = new GameObject(hlod.name + "_4");
            data[4].Bounds = new Bounds(center + new Vector3(-offset, offset, -offset),
                new Vector3(extend, extend, extend));
            data[4].GameObject = new GameObject(hlod.name + "_5");
            data[5].Bounds = new Bounds(center + new Vector3(offset, offset, -offset),
                new Vector3(extend, extend, extend));
            data[5].GameObject = new GameObject(hlod.name + "_6");
            data[6].Bounds = new Bounds(center + new Vector3(-offset, offset, offset),
                new Vector3(extend, extend, extend));
            data[6].GameObject = new GameObject(hlod.name + "_7");
            data[7].Bounds = new Bounds(center + new Vector3(offset, offset, offset),
                new Vector3(extend, extend, extend));
            data[7].GameObject = new GameObject(hlod.name + "_8");

            return data;
        }

    }

}