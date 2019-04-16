using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.HLODSystem.Simplifier;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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

        private static List<MeshRenderer> GetMeshRenderers(List<GameObject> gameObjects)
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
                    if ( mr != null )
                        meshRenderers.Add(mr);
                }
            }

            return meshRenderers;
        }

        private static List<HLODBuildInfo> CreateBuildInfo(SpaceNode root)
        {
            List<HLODBuildInfo> results = new List<HLODBuildInfo>();
            Queue<SpaceNode> trevelQueue = new Queue<SpaceNode>();
            Queue<int> parentQueue = new Queue<int>();
            Queue<string> nameQueue = new Queue<string>();

            trevelQueue.Enqueue(root);
            parentQueue.Enqueue(-1);
            nameQueue.Enqueue("");
            

            while (trevelQueue.Count > 0)
            {
                int currentNodeIndex = results.Count;
                string name = nameQueue.Dequeue();
                SpaceNode node = trevelQueue.Dequeue();
                HLODBuildInfo info = new HLODBuildInfo
                {
                    name = name,
                    parentIndex = parentQueue.Dequeue(),
                    target = node
                };

                if (node.ChildTreeNodes != null)
                {
                    for (int i = 0; i < node.ChildTreeNodes.Count; ++i)
                    {
                        trevelQueue.Enqueue(node.ChildTreeNodes[i]);
                        parentQueue.Enqueue(currentNodeIndex);
                        nameQueue.Enqueue(name + "_" + (i + 1));
                    }
                }

                results.Add(info);

                //it should add to every parent.
                List<MeshRenderer> meshRenderers = GetMeshRenderers(node.Objects);
                int distance = 0;

                while (currentNodeIndex >= 0)
                {
                    var curInfo = results[currentNodeIndex];
                    
                    curInfo.renderers.AddRange(meshRenderers);
                    curInfo.distances.AddRange(Enumerable.Repeat(distance,meshRenderers.Count));

                    currentNodeIndex = curInfo.parentIndex;
                    distance += 1;
                }

            }

            return results;
        }

        public static IEnumerator Create(HLOD hlod)
        {
            Stopwatch sw = new Stopwatch();

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            sw.Reset();
            sw.Start();

            Bounds bounds = hlod.GetBounds();

            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(hlod.gameObject);
            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(5.0f, hlod.MinSize);
            SpaceNode rootNode = spliter.CreateSpaceTree(bounds, hlodTargets);

            List<HLODBuildInfo> buildInfos = CreateBuildInfo(rootNode);           
            
            Debug.Log("[HLOD] Splite space: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();
            
            ISimplifier simplifier = (ISimplifier)Activator.CreateInstance(hlod.SimplifierType, new object[]{hlod});
            for (int i = 0; i < buildInfos.Count; ++i)
            {
                yield return new BranchCoroutine(simplifier.Simplify(buildInfos[i]));
            }

            yield return new WaitForBranches();
            Debug.Log("[HLOD] Simplify: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();

            
            IBatcher batcher = (IBatcher)Activator.CreateInstance(hlod.BatcherType, new object[]{hlod});
            batcher.Batch(buildInfos);
            Debug.Log("[HLOD] Batch: " + sw.Elapsed.ToString("g"));
            sw.Reset();
            sw.Start();

            try
            {
                AssetDatabase.StartAssetEditing();
                IStreamingBuilder builder =
                    (IStreamingBuilder) Activator.CreateInstance(hlod.StreamingType, new object[] {hlod});
                builder.Build(rootNode, buildInfos);
                Debug.Log("[HLOD] Build: " + sw.Elapsed.ToString("g"));
                sw.Reset();
                sw.Start();
            }
            finally
            {

                AssetDatabase.StopAssetEditing();
                Debug.Log("[HLOD] Importing: " + sw.Elapsed.ToString("g"));
            }

            //hlod.Root = rootNode;
        }

        public static IEnumerator Update(HLOD hlod)
        {
            yield return Destroy(hlod);
            yield return Create(hlod);

        }

        public static IEnumerator Destroy(HLOD hlod)
        {
            //List<HLOD> targetHlods = ObjectUtils.GetComponentsInChildren<HLOD>(hlod.gameObject).ToList();

            //for (int i = 0; i < targetHlods.Count; ++i)
            //{
            //    GameObject obj = PrefabUtility.GetOutermostPrefabInstanceRoot(targetHlods[i].gameObject);
            //    if (obj == null)
            //        continue;

            //    PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            //}

            //for (int i = 0; i < targetHlods.Count; ++i)
            //{
            //    List<GameObject> hlodTargets = ObjectUtils.HLODTargets(targetHlods[i].HighRoot);

            //    for (int ti = 0; ti < hlodTargets.Count; ++ti)
            //    {
            //        ObjectUtils.HierarchyMove(hlodTargets[ti], targetHlods[i].HighRoot, hlod.gameObject );
            //    }

            //    if ( targetHlods[i] != hlod )
            //        Object.DestroyImmediate(targetHlods[i].gameObject);
            //}

            //Object.DestroyImmediate(hlod.HighRoot);
            //Object.DestroyImmediate(hlod.LowRoot);

            yield break;
            
        }
    }
}
