using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.HLODSystem.Simplifier;
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
                targetHlods.AddRange(FindChildComponents<HLOD>(hlod.gameObject));
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

            ISimplifier simplifier = (ISimplifier)Activator.CreateInstance(hlod.SimplifierType);
            for (int i = 0; i < targetHlods.Count; ++i)
            {
                yield return new BranchCoroutine(simplifier.Simplify(targetHlods[i]));
            }

            yield return new WaitForBranches();

            IBatcher batcher = (IBatcher)Activator.CreateInstance(hlod.BatcherType);
            batcher.Batch(targetHlods.Last(), targetHlods.Select(h=>h.LowRoot).ToArray());

            for (int i = 0; i < targetHlods.Count; ++i)
            {
                SavePrefab(targetHlods[i]);
            }

        }

        //It must order by child first.
        //Because we need to make child prefab first.
        static T[] FindChildComponents<T>(GameObject root) where T : Component
        {
            LinkedList<T> result = new LinkedList<T>();
            Queue<GameObject> queue = new Queue<GameObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                GameObject go = queue.Dequeue();
                T component = go.GetComponent<T>();
                if ( component != null )
                    result.AddFirst(component);

                foreach (Transform child in go.transform)
                {
                    queue.Enqueue(child.gameObject);
                }
            }

            return result.ToArray();
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

            PrefabUtility.ApplyPrefabInstance(hlod.gameObject, InteractionMode.AutomatedAction);
        }

        
        static GameObject CreateHigh(GameObject root)
        {
            GameObject low = new GameObject("High");

            while (root.transform.childCount > 0)
            {
                Transform child = root.transform.GetChild(0);
                child.SetParent(low.transform);
            }

            return low;
        }

        static GameObject CreateLow(HLOD hlod, GameObject highGameObject)
        {
            GameObject high = new GameObject("Low");

            var lodGroups = highGameObject.GetComponentsInChildren<LODGroup>();
            List<Renderer> lodRenderers = new List<Renderer>();

            for (int i = 0; i < lodGroups.Length; ++i)
            {
                LOD[] lods = lodGroups[i].GetLODs();
                Renderer[] renderers = lods.Last().renderers;
                lodRenderers.AddRange(renderers);
            }

            for (int i = 0; i < lodRenderers.Count; ++i)
            {
                Renderer renderer = lodRenderers[i];
                if (renderer == null)
                    continue;

                float max = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                if (max < hlod.ThresholdSize)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                GameObject rendererObject = new GameObject(lodRenderers[i].name, typeof(MeshFilter), typeof(MeshRenderer));

                EditorUtility.CopySerialized(filter, rendererObject.GetComponent<MeshFilter>());
                EditorUtility.CopySerialized(renderer, rendererObject.GetComponent<MeshRenderer>());
                var holder = rendererObject.AddComponent<Utils.SimplificationDistanceHolder>();
                holder.OriginGameObject = renderer.gameObject;

                rendererObject.transform.SetParent(high.transform);
                rendererObject.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                rendererObject.transform.localScale = renderer.transform.lossyScale;
            }

            return high;
        }

    }
}
