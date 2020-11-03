using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Simplifier;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public static class HLODCreator
    {
        private static List<MeshRenderer> GetMeshRenderers(List<GameObject> gameObjects, float minObjectSize)
        {
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

            for (int i = 0; i < gameObjects.Count; ++i)
            {
                GameObject obj = gameObjects[i];
                LODGroup lodGroup = obj.GetComponent<LODGroup>();

                Renderer[] renderers;

                if (lodGroup != null)
                {
                    renderers = lodGroup.GetLODs().Last().renderers;
                }
                else
                {
                    renderers = obj.GetComponents<Renderer>();
                }

                for (int ri = 0; ri < renderers.Length; ++ri)
                {
                    MeshRenderer mr = renderers[ri] as MeshRenderer;

                    if (mr == null)
                        continue;

                    float max = Mathf.Max(mr.bounds.size.x, mr.bounds.size.y, mr.bounds.size.z);
                    if (max < minObjectSize)
                        continue;

                    meshRenderers.Add(mr);
                }
            }

            return meshRenderers;
        }

        private static List<Collider> GetColliders(List<GameObject> gameObjects, float minObjectSize)
        {
            List<Collider> results = new List<Collider>();

            for (int i = 0; i < gameObjects.Count; ++i)
            {
                GameObject obj = gameObjects[i];
                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                
                for (int ci = 0; ci < colliders.Length; ++ci)
                {
                    Collider collider = colliders[ci];
                    float max = Mathf.Max(collider.bounds.size.x, collider.bounds.size.y, collider.bounds.size.z);
                    if (max < minObjectSize)
                        continue;
                    
                    results.Add(collider);
                }
            }

            return results;
        }

        private static DisposableList<HLODBuildInfo> CreateBuildInfo(SpaceNode root, float minObjectSize)
        {

            List<HLODBuildInfo> resultsCandidates = new List<HLODBuildInfo>();
            Queue<SpaceNode> trevelQueue = new Queue<SpaceNode>();
            Queue<int> parentQueue = new Queue<int>();
            Queue<string> nameQueue = new Queue<string>();
            Queue<int> levelQueue = new Queue<int>();
            
            trevelQueue.Enqueue(root);
            parentQueue.Enqueue(-1);
            levelQueue.Enqueue(0);
            nameQueue.Enqueue("");
            

            while (trevelQueue.Count > 0)
            {
                int currentNodeIndex = resultsCandidates.Count;
                string name = nameQueue.Dequeue();
                SpaceNode node = trevelQueue.Dequeue();
                HLODBuildInfo info = new HLODBuildInfo
                {
                    Name = name,
                    ParentIndex = parentQueue.Dequeue(),
                    Target = node
                };


                for (int i = 0; i < node.GetChildCount(); ++i)
                {
                    trevelQueue.Enqueue(node.GetChild(i));
                    parentQueue.Enqueue(currentNodeIndex);
                    nameQueue.Enqueue(name + "_" + (i + 1));
                }


                resultsCandidates.Add(info);

                //it should add to every parent.
                List<MeshRenderer> meshRenderers = GetMeshRenderers(node.Objects, minObjectSize);
                List<Collider> colliders = GetColliders(node.Objects, minObjectSize);
                int distance = 0;

                while (currentNodeIndex >= 0)
                {
                    var curInfo = resultsCandidates[currentNodeIndex];

                    for (int i = 0; i < meshRenderers.Count; ++i) 
                    {
                        curInfo.WorkingObjects.Add(meshRenderers[i].ToWorkingObject(Allocator.Persistent));
                        curInfo.Distances.Add(distance);
                    }

                    for (int i = 0; i < colliders.Count; ++i)
                    {
                        curInfo.Colliders.Add(colliders[i].ToWorkingCollider());
                    }

                    
                   
  
                    currentNodeIndex = curInfo.ParentIndex;
                    distance += 1;
                }
            }

            
            DisposableList<HLODBuildInfo> results = new DisposableList<HLODBuildInfo>();
            
            for (int i = 0; i < resultsCandidates.Count; ++i)
            {
                if (resultsCandidates[i].WorkingObjects.Count > 0)
                {
                    results.Add(resultsCandidates[i]);
                }
                else
                {
                    resultsCandidates[i].Dispose();
                }
            }
            
            return results;
        }

        public static IEnumerator Create(HLOD hlod)
        {
            try
            {


                Stopwatch sw = new Stopwatch();

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                sw.Reset();
                sw.Start();
                
                hlod.ConvertedPrefabObjects.Clear();
                hlod.GeneratedObjects.Clear();

                Bounds bounds = hlod.GetBounds();

                List<GameObject> hlodTargets = ObjectUtils.HLODTargets(hlod.gameObject);
                ISpaceSplitter spliter = new QuadTreeSpaceSplitter(5.0f);
                SpaceNode rootNode = spliter.CreateSpaceTree(bounds, hlod.ChunkSize, hlod.transform.position, hlodTargets, progress =>
                {
                    EditorUtility.DisplayProgressBar("Bake HLOD", "Splitting space", progress * 0.25f);
                });

                if (hlodTargets.Count == 0)
                {
                    EditorUtility.DisplayDialog("Empty HLOD sources.",
                        "There are no objects to be included in the HLOD.",
                        "Ok");
                    yield break;
                }
                

                using (DisposableList<HLODBuildInfo> buildInfos = CreateBuildInfo(rootNode, hlod.MinObjectSize))
                {
                    if (buildInfos.Count == 0 || buildInfos[0].WorkingObjects.Count == 0)
                    {
                        EditorUtility.DisplayDialog("Empty HLOD sources.",
                            "There are no objects to be included in the HLOD.",
                            "Ok");
                        yield break;
                    }
                  
                    
                    Debug.Log("[HLOD] Splite space: " + sw.Elapsed.ToString("g"));
                    sw.Reset();
                    sw.Start();

                    ISimplifier simplifier = (ISimplifier) Activator.CreateInstance(hlod.SimplifierType,
                        new object[] {hlod.SimplifierOptions});
                    for (int i = 0; i < buildInfos.Count; ++i)
                    {
                        yield return new BranchCoroutine(simplifier.Simplify(buildInfos[i]));
                    }

                    yield return new WaitForBranches(progress =>
                    {
                        EditorUtility.DisplayProgressBar("Bake HLOD", "Simplify meshes",
                            0.25f + progress * 0.25f);
                    });
                    Debug.Log("[HLOD] Simplify: " + sw.Elapsed.ToString("g"));
                    sw.Reset();
                    sw.Start();


                    using (IBatcher batcher =
                        (IBatcher)Activator.CreateInstance(hlod.BatcherType, new object[] { hlod.BatcherOptions }))
                    {
                        batcher.Batch(hlod.transform.position, buildInfos,
                            progress =>
                            {
                                EditorUtility.DisplayProgressBar("Bake HLOD", "Generating combined static meshes.",
                                    0.5f + progress * 0.25f);
                            });
                    }
                    Debug.Log("[HLOD] Batch: " + sw.Elapsed.ToString("g"));
                    sw.Reset();
                    sw.Start();


                    IStreamingBuilder builder =
                        (IStreamingBuilder) Activator.CreateInstance(hlod.StreamingType,
                            new object[] {hlod, hlod.StreamingOptions});
                    builder.Build(rootNode, buildInfos, hlod.gameObject, hlod.CullDistance, hlod.LODDistance, false, true,
                        progress =>
                        {
                            EditorUtility.DisplayProgressBar("Bake HLOD", "Storing results.",
                                0.75f + progress * 0.25f);
                        });
                    Debug.Log("[HLOD] Build: " + sw.Elapsed.ToString("g"));
                    sw.Reset();
                    sw.Start();
                 
                    EditorUtility.SetDirty(hlod.gameObject);
                }

            }
            finally
            {
                EditorUtility.ClearProgressBar();
                
            }
        }

        public static IEnumerator Destroy(HLOD hlod)
        {

            var controller = hlod.GetComponent<HLODControllerBase>();
            if (controller == null)
                yield break;

            try
            {
                EditorUtility.DisplayProgressBar("Destroy HLOD", "Destrying HLOD files", 0.0f);
                var convertedPrefabObjects = hlod.ConvertedPrefabObjects;
                for (int i = 0; i < convertedPrefabObjects.Count; ++i)
                {
                    PrefabUtility.UnpackPrefabInstance(convertedPrefabObjects[i], PrefabUnpackMode.OutermostRoot,
                        InteractionMode.AutomatedAction);
                }

                var generatedObjects = hlod.GeneratedObjects;
                for (int i = 0; i < generatedObjects.Count; ++i)
                {
                    if (generatedObjects[i] == null)
                        continue;
                    var path = AssetDatabase.GetAssetPath(generatedObjects[i]);
                    if (string.IsNullOrEmpty(path) == false)
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                    else
                    {
                        //It means scene object.
                        //destory it.
                        Object.DestroyImmediate(generatedObjects[i]);
                    }

                    EditorUtility.DisplayProgressBar("Destroy HLOD", "Destrying HLOD files", (float)i / (float)generatedObjects.Count);
                }
                generatedObjects.Clear();

                Object.DestroyImmediate(controller);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            EditorUtility.SetDirty(hlod.gameObject);
            EditorUtility.SetDirty(hlod);
        }

    }
}
