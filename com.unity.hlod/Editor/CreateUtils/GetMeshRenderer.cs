using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.HLODSystem
{
    public static partial class CreateUtils
    {
        private class MeshRendererCalculator
        {
            private bool m_isCalculated = false;
            private List<MeshRenderer> m_meshRenderers = new List<MeshRenderer>();
            private List<LODGroup> m_lodGroups = new List<LODGroup>();

            private List<MeshRenderer> m_resultMeshRenderers = new List<MeshRenderer>();

            public List<MeshRenderer> ResultMeshRenderers
            {
                get { return m_resultMeshRenderers; }
            }
            
            public MeshRendererCalculator(List<GameObject> targetGameObjects)
            {
                for (int oi = 0; oi < targetGameObjects.Count; ++oi)
                {
                    var target = targetGameObjects[oi];
                    
                    m_meshRenderers.AddRange(target.GetComponentsInChildren<MeshRenderer>());
                    m_lodGroups.AddRange(target.GetComponentsInChildren<LODGroup>());
                }

                RemoveDisabled(m_lodGroups);
                RemoveDisabled(m_meshRenderers);
            }

            public void Calculate(float minObjectSize)
            {
                if (m_isCalculated == true)
                    return;
                
                for (int gi = 0; gi < m_lodGroups.Count; ++gi)
                {
                    LODGroup lodGroup = m_lodGroups[gi];
                    LOD[] lods = lodGroup.GetLODs();
                    for (int li = 0; li < lods.Length; ++li)
                    {
                        Renderer[] lodRenderers = lods[li].renderers;

                        //Remove every mesh renderer which is registered to the LODGroup.
                        for (int ri = 0; ri < lodRenderers.Length; ++ri)
                        {
                            MeshRenderer mr = lodRenderers[ri] as MeshRenderer;
                            if (mr == null)
                                continue;

                            m_meshRenderers.Remove(mr);
                        }
                    }

                    //
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

                        m_resultMeshRenderers.Add(mr);
                    }
                }

                for (int mi = 0; mi < m_meshRenderers.Count; ++mi)
                {
                    MeshRenderer mr = m_meshRenderers[mi];
                    
                    float max = Mathf.Max(mr.bounds.size.x, mr.bounds.size.y, mr.bounds.size.z);
                    if (max < minObjectSize)
                        continue;

                    m_resultMeshRenderers.Add(mr);
                }

                m_isCalculated = true;
            }

            //enabled is not Component variable.
            //So we can not make this to Generic function.
            private void RemoveDisabled(List<LODGroup> componentList)
            {
                for (int i = 0; i < componentList.Count; ++i)
                {
                    if (componentList[i].enabled == true && componentList[i].gameObject.activeInHierarchy == true)
                    {
                        continue;
                    }

                    int backIndex = componentList.Count - 1;
                    componentList[i] = componentList[backIndex];
                    componentList.RemoveAt(backIndex);
                    i -= 1;
                }
            }
            private void RemoveDisabled(List<MeshRenderer> componentList)
            {
                for (int i = 0; i < componentList.Count; ++i)
                {
                    if (componentList[i].enabled == true && componentList[i].gameObject.activeInHierarchy == true)
                    {
                        continue;
                    }

                    int backIndex = componentList.Count - 1;
                    componentList[i] = componentList[backIndex];
                    componentList.RemoveAt(backIndex);
                    i -= 1;
                }
            }
        }
        public static List<MeshRenderer> GetMeshRenderers(List<GameObject> gameObjects, float minObjectSize)
        {
            MeshRendererCalculator calculator = new MeshRendererCalculator(gameObjects);
            calculator.Calculate(minObjectSize);
            return calculator.ResultMeshRenderers;
        }

    }

}