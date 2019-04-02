using System.IO;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public static class PrefabUtils 
    {
      
        
        public static void SavePrefab(string path, HLOD hlod)
        {
            hlod.LowRoot.SetActive(false);
            
            if (PrefabUtility.IsAnyPrefabInstanceRoot(hlod.gameObject) == false)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(hlod.gameObject, path + hlod.name + ".prefab",
                    InteractionMode.AutomatedAction);
            }

        }

        public static void RemovePrefab(HLOD hlod)
        {

        }
    }

}