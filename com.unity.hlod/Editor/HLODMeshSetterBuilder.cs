using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    public class HLODMeshSetterBuilder : IProcessSceneWithReport
    {

        public int callbackOrder
        {
            get
            {
                return 0;
            }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var rootGameObjects = scene.GetRootGameObjects();

            for (int ri = 0; ri < rootGameObjects.Length; ++ri)
            {
                var meshSetters = rootGameObjects[ri].GetComponentsInChildren<HLODMeshSetter>();
                for (int si = 0; si < meshSetters.Length; ++si)
                {
                    ProcessMeshSetter(meshSetters[si]);
                }
            }
        }

        private void ProcessMeshSetter(HLODMeshSetter setter)
        {
            if (setter.RemoveAtBuild == false)
                return;

            for (int gi = 0; gi < setter.GroupCount; ++gi)
            {
                var group = setter.GetGroup(gi);
                var renderers = group.MeshRenderers;

                for (int ri = 0; ri < renderers.Count; ++ri)
                {
                    Object.DestroyImmediate(renderers[ri].gameObject);
                }
            }

        }
    }
}