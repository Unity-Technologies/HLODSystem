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

                var meshRenderer = meshFilters[f].GetComponent<MeshRenderer>();
                var material = meshRenderer.sharedMaterial;

                HLODMesh hlodmesh = ScriptableObject.CreateInstance<HLODMesh>();
                hlodmesh.FromMesh(mesh);

                
                string meshName = path;
                if (string.IsNullOrEmpty(mesh.name) == false)
                    meshName = meshName + "_" + mesh.name;

                AssetDatabase.CreateAsset(hlodmesh, meshName + ".hlodmesh");
                controller.AddHLODMesh(hlodmesh, material);

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))
                {
                    AssetDatabase.AddObjectToAsset(material, meshName + ".hlodmesh");
                }

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