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

            List<HLODControllerBase> controllers = new List<HLODControllerBase>();
            for (int i = 0; i < roots.Length; ++i)
            {
                controllers.AddRange(roots[i].GetComponentsInChildren<HLODControllerBase>());
            }

            for (int i = 0; i < controllers.Count; ++i)
            {
                var controller = controllers[i];

                if (controller != null && controller.enabled == true)
                {
                    controller.Install();
                }
            }

        }
    }
}
