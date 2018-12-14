using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    class BuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var roots = scene.GetRootGameObjects();
            List<HLOD> hlods = new List<HLOD>();
            for (int i = 0; i < roots.Length; ++i)
            {
                hlods.AddRange(roots[i].GetComponentsInChildren<HLOD>());
            }

            for (int i = 0; i < hlods.Count; ++i)
            {
                hlods[i].Install();
            }

        }
    }
}
