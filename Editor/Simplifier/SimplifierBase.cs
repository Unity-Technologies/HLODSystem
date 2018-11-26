using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.Simplifier
{
    abstract class SimplifierBase : ISimplifier
    {
        public IEnumerator Simplify(HLOD hlod)
        {
            var root = hlod.LowRoot;

            foreach (Transform child in root.transform)
            {
                var meshFilter = child.GetComponent<MeshFilter>();

                var mesh = meshFilter.sharedMesh;
                var holder = meshFilter.GetComponent<Utils.SimplificationDistanceHolder>();

                int triangleCount = mesh.triangles.Length / 3;
                float maxQuality = Mathf.Min((float)hlod.SimplifyMaxPolygonCount / (float)triangleCount, hlod.SimplifyPolygonRatio);
                float minQuality = Mathf.Max((float)hlod.SimplifyMinPolygonCount / (float)triangleCount, 0.0f);

                var ratio = CalcRatio(maxQuality, holder, hlod);
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

                meshFilter.sharedMesh = simplifiedMesh;
            }
        }

        protected abstract IEnumerator GetSimplifiedMesh(Mesh origin, float quality, Action<Mesh> resultCallback);
        private float CalcRatio(float initRatio, Utils.SimplificationDistanceHolder holder, HLOD hlod)
        {
            float ratio = initRatio;

            if (holder == null || holder.OriginGameObject == null)
                return ratio;

            var transform = holder.OriginGameObject.transform;
            
            while (transform.gameObject != hlod.gameObject)
            {
                var curHlod = transform.GetComponent<HLOD>();
                if (curHlod != null)
                {
                    ratio = ratio * curHlod.SimplifyPolygonRatio;
                }

                transform = transform.parent;
            }

            holder.Ratio = ratio;
            return ratio;
        }

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
