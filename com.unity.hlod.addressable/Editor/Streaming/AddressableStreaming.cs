using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableStreaming : IStreamingBuilder
    {
        
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            StreamingBuilderTypes.RegisterType(typeof(AddressableStreaming));
        }

        public void Build(HLOD hlod, bool isRoot)
        {
            string path = "";
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(hlod.gameObject);
            path = stage.prefabAssetPath;
            path = Path.GetDirectoryName(path) + "/";            


            if (hlod.HighRoot != null)
            {
                BuildHigh(hlod);
            }

            if (hlod.LowRoot != null)
            {
                BuildLow(hlod, isRoot);
            }

            PrefabUtils.SavePrefab(path, hlod);
        }

        public static void OnGUI(HLOD hlod)
        {
            dynamic options = hlod.StreamingOptions;

            if (options.LastLowInMemory == null)
                options.LastLowInMemory = false;
            if (options.MaxInstantiateCount == null)
                options.MaxInstantiateCount = 10;

            EditorGUI.indentLevel += 1;
            options.LastLowInMemory = EditorGUILayout.Toggle("Last low in memory", options.LastLowInMemory);
            options.MaxInstantiateCount =
                EditorGUILayout.IntSlider("Max instantiate count per frame", options.MaxInstantiateCount, 1, 100, null);
            
            EditorGUI.indentLevel -= 1;
        }

        private void BuildHigh(HLOD hlod)
        {
            dynamic options = hlod.StreamingOptions;
            var root = hlod.HighRoot;
            var controller = root.AddComponent<AddressableController>();

            Stack<Transform> trevelStack = new Stack<Transform>();
            trevelStack.Push(root.transform);

            List<GameObject> needDestory = new List<GameObject>();

            while (trevelStack.Count > 0)
            {
                var current = trevelStack.Pop();
                foreach (Transform child in current)
                {
                    HLOD childHlod = child.GetComponent<HLOD>();
                    if (childHlod != null)
                    {
                        controller.AddHLOD(childHlod);
                    }
                    else if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject) == true)
                    {
                        var reference = GetAssetReference(child.gameObject);
                        controller.AddObject(reference, child);
                        needDestory.Add(child.gameObject);
                    }
                    else
                    {
                        trevelStack.Push(child);
                    }
                }
            }

            for (int i = 0; i < needDestory.Count; ++i)
            {
                Object.DestroyImmediate(needDestory[i]);
            }

            controller.MaxInstantiateCount = options.MaxInstantiateCount;
            controller.Disable();
           
        }

        private void BuildLow(HLOD hlod, bool isRoot)
        {
            GameObject root = hlod.LowRoot;
            dynamic options = hlod.StreamingOptions;

            string path = "";
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(hlod.gameObject);
            path = stage.prefabAssetPath;
            path = Path.GetDirectoryName(path) + "/";            

            if (isRoot == true)
            {
                if (options.LastLowInMemory != null && options.LastLowInMemory == true)
                {
                    var rootController = root.AddComponent<DefaultController>();
                    List<HLODMesh> rootCreatedMeshes = ObjectUtils.SaveHLODMesh(path, hlod.name, hlod.LowRoot);
                    rootController.AddHLODMeshes(rootCreatedMeshes);
                    return;
                }
            }

            var controller = root.AddComponent<AddressableController>();

            List<HLODMesh> createdMeshes = ObjectUtils.SaveHLODMesh(path, hlod.name, hlod.LowRoot);
            List<AssetReference> references = new List<AssetReference>(createdMeshes.Count);
            for (int i = 0; i < createdMeshes.Count; ++i)
            {
                references.Add(GetAssetReference(createdMeshes[i]));
            }
            controller.AddHLODMeshReferences(references);
            controller.MaxInstantiateCount = options.MaxInstantiateCount;
        }


        private AssetReference GetAssetReference(Object obj)
        {
            //create settings if there is no settings.
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
            }

            
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            string path = AssetDatabase.GetAssetPath(obj);
            
            if (string.IsNullOrEmpty(path))
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            if (string.IsNullOrEmpty(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);

            if (entry != null)
                return new AssetReference(guid);

            var entriesAdded = new List<AddressableAssetEntry>
            {
                settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false)
            };

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);

            return new AssetReference(guid);
        }
    }

}