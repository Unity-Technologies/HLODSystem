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
        private dynamic m_options;
        public SimplifierBase(SerializableDynamicObject simplifierOptions)
        {
            m_options = simplifierOptions;
        }
        public IEnumerator Simplify(HLODBuildInfo buildInfo)
        {
            buildInfo.simplifiedMeshes = new List<Mesh>(buildInfo.renderers.Count);
            for (int i = 0; i < buildInfo.renderers.Count; ++i)
            {
                var meshFilter = buildInfo.renderers[i].GetComponent<MeshFilter>();
                var mesh = meshFilter.sharedMesh;

                int triangleCount = mesh.triangles.Length / 3;
                float maxQuality = Mathf.Min((float)m_options.SimplifyMaxPolygonCount / (float)triangleCount, (float)m_options.SimplifyPolygonRatio);
                float minQuality = Mathf.Max((float)m_options.SimplifyMinPolygonCount / (float)triangleCount, 0.0f);

                var ratio = maxQuality * Mathf.Pow((float)m_options.SimplifyPolygonRatio, buildInfo.distances[i]);
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

        protected static void OnGUIBase(SerializableDynamicObject simplifierOptions)
        {
            EditorGUI.indentLevel += 1;

            dynamic options = simplifierOptions;

            if (options.SimplifyPolygonRatio == null)
                options.SimplifyPolygonRatio = 0.8f;
            if (options.SimplifyMinPolygonCount == null)
                options.SimplifyMinPolygonCount = 10;
            if (options.SimplifyMaxPolygonCount == null)
                options.SimplifyMaxPolygonCount = 500;
            

            options.SimplifyPolygonRatio = EditorGUILayout.Slider("Polygon Ratio", options.SimplifyPolygonRatio, 0.0f, 1.0f);
            EditorGUILayout.LabelField("Triangle Range");
            EditorGUI.indentLevel += 1;
            options.SimplifyMinPolygonCount = EditorGUILayout.IntSlider("Min", options.SimplifyMinPolygonCount, 10, 100);
            options.SimplifyMaxPolygonCount = EditorGUILayout.IntSlider("Max", options.SimplifyMaxPolygonCount, 10, 5000);
            EditorGUI.indentLevel -= 1;

            EditorGUI.indentLevel -= 1;
        }
        
    }
}
