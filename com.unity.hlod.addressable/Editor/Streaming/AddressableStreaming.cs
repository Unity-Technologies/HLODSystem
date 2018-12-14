using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
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

        public void Build(HLOD hlod)
        {
            if (hlod.HighRoot != null)
            {
                BuildHigh(hlod.HighRoot);
            }

            if (hlod.LowRoot != null)
            {
                BuildLow(hlod.name, hlod.LowRoot);
            }
        }

        private void BuildHigh(GameObject root)
        {
            var controller = root.AddComponent<AddressableController>();

            Stack<Transform> trevelStack = new Stack<Transform>();
            trevelStack.Push(root.transform);

            List<GameObject> needDestory = new List<GameObject>();

            while (trevelStack.Count > 0)
            {
                var current = trevelStack.Pop();
                foreach (Transform child in current)
                {
                    HLOD hlod = child.GetComponent<HLOD>();
                    if (hlod != null)
                    {
                        controller.AddHLOD(hlod);
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

            controller.Disable();
           
        }

        private void BuildLow(string name, GameObject root)
        {
            var controller = root.AddComponent<AddressableController>();

            PrefabStage stage = PrefabStageUtility.GetPrefabStage(root);
            string path = stage.prefabAssetPath;
            path = System.IO.Path.GetDirectoryName(path) + "/Low/";

            if (System.IO.Directory.Exists(path) == false)
            {
                System.IO.Directory.CreateDirectory(path);
            }

            path = path + name + ".prefab";

            root.SetActive(true);

            //Move every child object to new one for make prefab.
            GameObject go = new GameObject(name);

            while (root.transform.childCount > 0)
            {
                var child = root.transform.GetChild(0);
                child.SetParent(go.transform);
            }

            go.transform.SetParent(root.transform);

            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            AssetDatabase.Refresh();

            //store low lod meshes
            var meshFilters = go.GetComponentsInChildren<MeshFilter>();
            for (int f = 0; f < meshFilters.Length; ++f)
            {
                AssetDatabase.AddObjectToAsset(meshFilters[f].sharedMesh, path);
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

            PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);

            var reference = GetAssetReference(go);
            controller.AddObject(reference, go.transform);
            

            Object.DestroyImmediate(go);
        }


        private AssetReference GetAssetReference(GameObject obj)
        {
            //create settings if there is no settings.
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
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