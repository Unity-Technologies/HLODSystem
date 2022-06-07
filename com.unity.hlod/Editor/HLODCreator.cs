using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
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

        private struct TravelQueueItem
        {
            public SpaceNode Node;
            public int Parent;
            public string Name;
            public int Level;
            public List<GameObject> TargetGameObjects;
        }

        private static void CopyObjectsToParent(List<TravelQueueItem> list, int curIndex, List<GameObject> objects)
        {
            if (curIndex < 0)
                return;

            int parentIndex = list[curIndex].Parent;
            
            if (parentIndex < 0)
                return;

            var parent = list[parentIndex];
            parent.TargetGameObjects.AddRange(objects);
            
            CopyObjectsToParent(list, parentIndex, objects);

        }
        private static DisposableList<HLODBuildInfo> CreateBuildInfo(HLOD hlod, SpaceNode root, float minObjectSize)
        {
            //List<HLODBuildInfo> resultsCandidates = new List<HLODBuildInfo>();
            
            Queue<TravelQueueItem> travelQueue = new Queue<TravelQueueItem>();
            
            List<TravelQueueItem> candidateItems = new List<TravelQueueItem>();
            List<HLODBuildInfo> buildInfoCandidates = new List<HLODBuildInfo>();
            
            int maxLevel = 0;
            
            travelQueue.Enqueue(new TravelQueueItem()
            {
                Node = root,
                Parent = -1,
                Level = 0,
                Name = "",
                TargetGameObjects = new List<GameObject>(),
            });

            while (travelQueue.Count > 0)
            {
                int currentNodeIndex = candidateItems.Count;
                TravelQueueItem item = travelQueue.Dequeue();

                for (int i = 0; i < item.Node.GetChildCount(); ++i)
                {
                    travelQueue.Enqueue(new TravelQueueItem()
                    {
                        Node = item.Node.GetChild(i),
                        Parent = currentNodeIndex,
                        Level = item.Level + 1,
                        Name = item.Name + "_" + (i+1),
                        TargetGameObjects = new List<GameObject>(),
                    });
                }

                maxLevel = Math.Max(maxLevel, item.Level);
                candidateItems.Add(item);
                buildInfoCandidates.Add(new HLODBuildInfo()
                {
                    Name = item.Name,
                    Target = item.Node
                });
                item.TargetGameObjects.AddRange(item.Node.Objects);

                CopyObjectsToParent(candidateItems, currentNodeIndex, item.Node.Objects);
            }

            for (int i = 0; i < candidateItems.Count; ++i)
            {
                var item = candidateItems[i];
                var level = maxLevel - item.Level;  //< It needs to be turned upside down. The terminal node must have level 0.
                var meshRenderers = CreateUtils.GetMeshRenderers(item.TargetGameObjects, minObjectSize, level);
                var colliders = GetColliders(item.TargetGameObjects, minObjectSize);

                int distance = 0;
                int currentNodeIndex = i;
                while (currentNodeIndex >= 0)
                {
                    var curInfo = buildInfoCandidates[currentNodeIndex];
                    var curItem = candidateItems[currentNodeIndex];
                    
                    for (int mi = 0; mi < meshRenderers.Count; ++mi)
                    {
                        curInfo.WorkingObjects.Add(meshRenderers[mi].ToWorkingObject(Allocator.Persistent));
                        curInfo.Distances.Add(distance);
                    }

                    for (int ci = 0; ci < colliders.Count; ++ci)
                    {
                        curInfo.Colliders.Add(colliders[ci].ToWorkingCollider(hlod));
                    }

                    currentNodeIndex = curItem.Parent;
                    distance += 1;
                    break;
                }


            }
            
            DisposableList<HLODBuildInfo> results = new DisposableList<HLODBuildInfo>();
            
            for (int i = 0; i < buildInfoCandidates.Count; ++i)
            {
                if (buildInfoCandidates[i].WorkingObjects.Count > 0)
                {
                    results.Add(buildInfoCandidates[i]);
                }
                else
                {
                    buildInfoCandidates[i].Dispose();
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
                float looseSize = Mathf.Min(hlod.ChunkSize * 0.3f, 5.0f); //< If the chunk size is small, there is a problem that it may get caught in an infinite loop.
                                                                          //So, the size can be determined according to the chunk size.
                ISpaceSplitter spliter = new QuadTreeSpaceSplitter(looseSize);
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
                

                using (DisposableList<HLODBuildInfo> buildInfos = CreateBuildInfo(hlod, rootNode, hlod.MinObjectSize))
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
