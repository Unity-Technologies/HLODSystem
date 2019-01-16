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
            path = System.IO.Path.GetDirectoryName(path) + "/";
            path = path + hlod.name + ".prefab";

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            if (PrefabUtility.IsAnyPrefabInstanceRoot(hlod.gameObject) == false)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(hlod.gameObject, path,
                    InteractionMode.AutomatedAction);
            }
            AssetDatabase.Refresh();


            //store low lod meshes
            var meshFilters = hlod.LowRoot.GetComponentsInChildren<MeshFilter>();
            for (int f = 0; f < meshFilters.Length; ++f)
            {
                string meshPath = AssetDatabase.GetAssetPath(meshFilters[f].sharedMesh);
                if (string.IsNullOrEmpty(meshPath))
                {
                    AssetDatabase.AddObjectToAsset(meshFilters[f].sharedMesh, path);
                }

                var meshRenderer = meshFilters[f].GetComponent<MeshRenderer>();
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    string materialPath = AssetDatabase.GetAssetPath(material);
                    if (string.IsNullOrEmpty(materialPath))
                    {
                        AssetDatabase.AddObjectToAsset(material, path);
                    }
                }

                AssetDatabase.Refresh();
                
            }

            hlod.LowRoot.SetActive(false);

            PrefabUtility.ApplyPrefabInstance(hlod.gameObject, InteractionMode.AutomatedAction);
        }

        public static void RemovePrefab(HLOD hlod)
        {

        }
    }

}