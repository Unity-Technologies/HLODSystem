using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Simplifier
{
    abstract class SimplifierBase : ISimplifier
    {
        private HLOD m_hlod;
        public SimplifierBase(HLOD hlod)
        {
            m_hlod = hlod;
        }
        public IEnumerator Simplify(HLODBuildInfo buildInfo)
        {
            buildInfo.simplifiedMeshes = new List<Mesh>(buildInfo.renderers.Count);
            for (int i = 0; i < buildInfo.renderers.Count; ++i)
            {
                var meshFilter = buildInfo.renderers[i].GetComponent<MeshFilter>();
                var mesh = meshFilter.sharedMesh;

                int triangleCount = mesh.triangles.Length / 3;
                float maxQuality = Mathf.Min((float)m_hlod.SimplifyMaxPolygonCount / (float)triangleCount, m_hlod.SimplifyPolygonRatio);
                float minQuality = Mathf.Max((float)m_hlod.SimplifyMinPolygonCount / (float)triangleCount, 0.0f);

                var ratio = maxQuality * Mathf.Pow(m_hlod.SimplifyPolygonRatio, buildInfo.distances[i]);
                ratio = Mathf.Max(ratio, minQuality);

                
                while (Cache.SimplifiedCache.IsGenerating(GetType(), mesh, ratio) == true)
                {
                    yield return null;
                }
                Mesh simplifiedMesh = Cache.SimplifiedCache.Get(GetType(), mesh, ratio);
                if (simplifiedMesh == null)
                {
                    Cache.SimplifiedCache.MarkGenerating(GetType(), mesh, ratio);
                    yield return GetSimplifiedMesh(mesh, ratio, (m) =>
                    {
                        simplifiedMesh = m;
                    });
                    Cache.SimplifiedCache.Update(GetType(), mesh, simplifiedMesh, ratio);
                    
                }

                buildInfo.simplifiedMeshes.Add(simplifiedMesh);
            }            
        }

        protected abstract IEnumerator GetSimplifiedMesh(Mesh origin, float quality, Action<Mesh> resultCallback);

        protected static void OnGUIBase(HLOD hlod)
        {
            EditorGUI.indentLevel += 1;

            hlod.SimplifyPolygonRatio = EditorGUILayout.Slider("Polygon Ratio", hlod.SimplifyPolygonRatio, 0.0f, 1.0f);
            EditorGUILayout.LabelField("Triangle Range");
            EditorGUI.indentLevel += 1;
            hlod.SimplifyMinPolygonCount = EditorGUILayout.IntSlider("Min", hlod.SimplifyMinPolygonCount, 10, 100);
            hlod.SimplifyMaxPolygonCount = EditorGUILayout.IntSlider("Max", hlod.SimplifyMaxPolygonCount, 10, 5000);
            EditorGUI.indentLevel -= 1;

            EditorGUI.indentLevel -= 1;
        }
        
    }
}
