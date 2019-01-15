using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.HLODSystem.Simplifier;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor.Experimental.SceneManagement;

namespace Unity.HLODSystem
{
    static class HLODCreator
    {
        public static HLOD Setup(GameObject root)
        {
            if (root.GetComponent<HLOD>() != null)
            {
                Debug.LogWarning("It has already been set.");
                return null;
            }

            HLOD hlod = root.AddComponent<HLOD>();
            return hlod;           
        }
        public static IEnumerator Create(HLOD hlod)
        {
            List<HLOD> targetHlods = new List<HLOD>();
           
            hlod.CalcBounds();
            if (hlod.RecursiveGeneration == true)
            {
                if (hlod.Bounds.size.x > hlod.MinSize)
                {
                    ISplitter splitter = new OctSplitter();
                    splitter.Split(hlod);
                }

                //GetComponentsInChildren is not working.
                //so, I made it manually.
                targetHlods.AddRange(FindObjects.GetComponentsInChildren<HLOD>(hlod.gameObject));
            }
            else
            {
                targetHlods.Add(hlod);
            }

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                var curHlod = targetHlods[i];
                curHlod.HighRoot = CreateHigh(curHlod.gameObject);
                curHlod.LowRoot = CreateLow(curHlod, curHlod.HighRoot);

                curHlod.HighRoot.transform.SetParent(curHlod.transform);
                curHlod.LowRoot.transform.SetParent(curHlod.transform);
            }

            AssetDatabase.Refresh();

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                ISimplifier simplifier = (ISimplifier)Activator.CreateInstance(targetHlods[i].SimplifierType);
                yield return new BranchCoroutine(simplifier.Simplify(targetHlods[i]));
            }

            yield return new WaitForBranches();

            IBatcher batcher = (IBatcher)Activator.CreateInstance(hlod.BatcherType);
            batcher.Batch(targetHlods.Last(), targetHlods.Select(h => h.LowRoot).ToArray());

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                IStreamingBuilder builder = (IStreamingBuilder)Activator.CreateInstance(targetHlods[i].StreamingType);
                builder.Build(targetHlods[i], targetHlods[i] == hlod);
            }

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                SavePrefab(targetHlods[i]);
            }

        }


        static void SavePrefab(HLOD hlod)
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

        
        static GameObject CreateHigh(GameObject root)
        {
            GameObject high = new GameObject("High");

            while (root.transform.childCount > 0)
            {
                Transform child = root.transform.GetChild(0);
                child.SetParent(high.transform);
            }

            return high;
        }

        static GameObject CreateLow(HLOD hlod, GameObject highGameObject)
        {
            GameObject low = new GameObject("Low");

            List<Renderer> renderers = new List<Renderer>();

            //Convert gameobject to MeshRenderer.
            //This gameObjects are mixed LODGroup and MeshRenderer.
            List<GameObject> gameObjects = FindObjects.HLODTargets(highGameObject);
            for (int i = 0; i < gameObjects.Count; ++i)
            {
                var lodGroup = gameObjects[i].GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    renderers.AddRange(lodGroup.GetLODs().Last().renderers);
                    continue;
                }

                var renderer = gameObjects[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderers.Add(renderer);
                }
            }

            for (int i = 0; i < renderers.Count; ++i)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                float max = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                if (max < hlod.ThresholdSize)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                GameObject rendererObject = new GameObject(renderers[i].name, typeof(MeshFilter), typeof(MeshRenderer), typeof(LowMeshHolder));

                EditorUtility.CopySerialized(filter, rendererObject.GetComponent<MeshFilter>());
                EditorUtility.CopySerialized(renderer, rendererObject.GetComponent<MeshRenderer>());
                var holder = rendererObject.AddComponent<Utils.SimplificationDistanceHolder>();
                holder.OriginGameObject = renderer.gameObject;

                rendererObject.transform.SetParent(low.transform);
                rendererObject.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                rendererObject.transform.localScale = renderer.transform.lossyScale;
            }

            
            return low;
        }

    }
}
