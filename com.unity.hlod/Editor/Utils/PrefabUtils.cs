using System.IO;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public static class PrefabUtils 
    {
      
        public static void SavePrefab(HLOD hlod)
        {
            string path = "";
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(hlod.gameObject);
            path = stage.prefabAssetPath;
            path = Path.GetDirectoryName(path) + "/";
            path = path + hlod.name;

            //store low lod meshes
            var meshFilters = hlod.LowRoot.GetComponentsInChildren<MeshFilter>();

            var controller = hlod.LowRoot.GetComponent<Streaming.ControllerBase>();
            for (int f = 0; f < meshFilters.Length; ++f)
            {
                var mesh = meshFilters[f].sharedMesh;

                HLODMesh hlodmesh = ScriptableObject.CreateInstance<HLODMesh>();
                hlodmesh.FromMesh(mesh);

                var meshRenderer = meshFilters[f].GetComponent<MeshRenderer>();
                var material = meshRenderer.sharedMaterial;
                AssetDatabase.CreateAsset(material, path + ".mat");
                AssetDatabase.CreateAsset(hlodmesh, path + "_" + mesh.name + ".hlodmesh");
                

                controller.AddHLODMesh(hlodmesh, material);
            }

            for (int i = hlod.LowRoot.transform.childCount - 1; i >= 0; --i )
            {
                GameObject.DestroyImmediate(hlod.LowRoot.transform.GetChild(i).gameObject);
            }

            hlod.LowRoot.SetActive(false);
            
            if (PrefabUtility.IsAnyPrefabInstanceRoot(hlod.gameObject) == false)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(hlod.gameObject, path + ".prefab",
                    InteractionMode.AutomatedAction);
            }

        }

        public static void RemovePrefab(HLOD hlod)
        {

        }
    }

}