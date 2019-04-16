using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Unity.HLODSystem
{
    class BuildProcess : IProcessSceneWithReport
    {
        List<GameObject> instantiatePrefabs = new List<GameObject>();

        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var roots = scene.GetRootGameObjects();

            //first, if we use HLODPrefab, we have to create prefab instance while build.
            for (int i = 0; i < roots.Length; ++i)
            {
                var prefabs = roots[i].GetComponentsInChildren<HLODPrefab>();
                for (int pi = 0; pi < prefabs.Length; ++pi)
                {
                    GameObject obj = PrefabUtility.InstantiatePrefab(prefabs[pi].Prefab) as GameObject;
                    obj.transform.parent = prefabs[pi].transform;
                    instantiatePrefabs.Add(obj);
                }
            }

            List<HLOD> hlods = new List<HLOD>();
            for (int i = 0; i < roots.Length; ++i)
            {
                hlods.AddRange(roots[i].GetComponentsInChildren<HLOD>());
            }


        }
    }
}
