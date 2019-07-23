using System.Collections.Generic;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    public class HLODBuilder : IProcessSceneWithReport
    {
        public int callbackOrder
        {
            get { return 0; }
        }
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            //this case is enter the playmode.
            if (report == null)
                return;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int oi = 0; oi < rootObjects.Length; ++oi)
            {
                List<HLOD> hlods = new List<HLOD>();
                List<TerrainHLOD> terrainHlods = new List<TerrainHLOD>();
                
                FindComponentsInChild(rootObjects[oi], ref hlods);
                FindComponentsInChild(rootObjects[oi], ref terrainHlods);

                for (int hi = 0; hi < hlods.Count; ++hi)
                {
                    Object.DestroyImmediate(hlods[hi]);
                }
                for (int hi = 0; hi < terrainHlods.Count; ++hi)
                {
                    Object.DestroyImmediate(terrainHlods[hi]);
                }
            }
        }

        private void FindComponentsInChild<T>(GameObject target, ref List<T> components)
        {
            var component = target.GetComponent<T>();
            if (component != null)
                components.Add(component);

            foreach (Transform child in target.transform)
            {
                FindComponentsInChild(child.gameObject, ref components);
            }
        }

    }
}