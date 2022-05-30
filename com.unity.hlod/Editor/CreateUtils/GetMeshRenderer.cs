using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.HLODSystem
{
    public static partial class CreateUtils
    {
        public static List<MeshRenderer> GetMeshRenderers(List<GameObject> gameObjects, float minObjectSize)
        {
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

            for (int oi = 0; oi < gameObjects.Count; ++oi)
            {
                GameObject obj = gameObjects[oi];

                if (obj.activeInHierarchy == false)
                    continue;

                LODGroup[] lodGroups = obj.GetComponentsInChildren<LODGroup>();
                List<MeshRenderer> allRenderers = obj.GetComponentsInChildren<MeshRenderer>().ToList();

                for (int gi = 0; gi < lodGroups.Length; ++gi)
                {
                    LODGroup lodGroup = lodGroups[gi];
                    if (lodGroup.enabled == false || lodGroup.gameObject.activeInHierarchy == false)
                        continue;

                    //all renderers using in LODGroup should be removed in the allRenderers
                    LOD[] lods = lodGroup.GetLODs();
                    for (int li = 0; li < lods.Length; ++li)
                    {
                        Renderer[] lodRenderers = lods[li].renderers;
                        for (int ri = 0; ri < lodRenderers.Length; ++ri)
                        {
                            MeshRenderer mr = lodRenderers[ri] as MeshRenderer;
                            if (mr == null)
                                continue;

                            allRenderers.Remove(mr);
                        }
                    }

                    Renderer[] renderers = lods.Last().renderers;
                    for (int ri = 0; ri < renderers.Length; ++ri)
                    {
                        MeshRenderer mr = renderers[ri] as MeshRenderer;

                        if (mr == null)
                            continue;

                        if (mr.gameObject.activeInHierarchy == false || mr.enabled == false)
                            continue;

                        float max = Mathf.Max(mr.bounds.size.x, mr.bounds.size.y, mr.bounds.size.z);
                        if (max < minObjectSize)
                            continue;

                        meshRenderers.Add(mr);
                    }
                }

                for (int ai = 0; ai < allRenderers.Count; ++ai)
                {
                    MeshRenderer mr = allRenderers[ai] as MeshRenderer;
                    ;

                    if (mr.enabled == false || mr.gameObject.activeInHierarchy == false)
                        continue;

                    float max = Mathf.Max(mr.bounds.size.x, mr.bounds.size.y, mr.bounds.size.z);
                    if (max < minObjectSize)
                        continue;

                    meshRenderers.Add(allRenderers[ai]);
                }
            }

            return meshRenderers;
        }
    }

}