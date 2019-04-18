using System;
using System.Collections.Generic;
using Unity.HLODSystem.Streaming;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
                    prefabs[pi].IsEdit = false;
                    GameObject obj = Object.Instantiate(prefabs[pi].Prefab) as GameObject;
                    obj.transform.SetParent(prefabs[pi].transform, false);
                    instantiatePrefabs.Add(obj);

                    Object.DestroyImmediate(prefabs[pi]);
                }
            }

            List<HLOD> hlods = new List<HLOD>();
            for (int i = 0; i < roots.Length; ++i)
            {
                hlods.AddRange(roots[i].GetComponentsInChildren<HLOD>());
            }

            for (int i = 0; i < hlods.Count; ++i)
            {
                var controller = hlods[i].GetComponent<ControllerBase>();

                if (controller != null)
                {
                    if (hlods[i].enabled)
                    {
                        controller.enabled = true;
                        controller.Install();
                    }
                    else
                    {
                        controller.enabled = false;
                    }
                }
            }

        }
    }
}
