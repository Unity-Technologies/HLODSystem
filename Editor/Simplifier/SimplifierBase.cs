using System;
using UnityEngine;
using UnityEngine.XR.WSA;

namespace Unity.HLODSystem.Simplifier
{
    abstract class SimplifierBase : ISimplifier
    {
        public void Simplify(HLOD hlod)
        {
            var root = hlod.LowRoot;
            foreach (Transform child in root.transform)
            {
                var holder = child.GetComponent<Utils.SimplificationDistanceHolder>();
                var meshFilter = child.GetComponent<MeshFilter>();

                if (meshFilter == null)
                    continue;

                var mesh = meshFilter.sharedMesh;

                int triangleCount = mesh.triangles.Length / 3;
                float maxQuality = Mathf.Min((float)hlod.SimplifyMaxPolygonCount / (float)triangleCount, hlod.SimplifyPolygonRatio);
                float minQuality = Mathf.Max((float)hlod.SimplifyMinPolygonCount / (float)triangleCount, 0.0f);

                var ratio = CalcRatio(maxQuality, holder, hlod);
                ratio = Mathf.Max(ratio, minQuality);

                Mesh simplifiedMesh = Cache.SimplifiedCache.Get(GetType(), mesh, ratio);
                if (simplifiedMesh == null)
                {
                    simplifiedMesh = GetSimplifiedMesh(mesh, ratio);
                    Cache.SimplifiedCache.Update(GetType(), mesh, simplifiedMesh, ratio);
                }

                meshFilter.sharedMesh = simplifiedMesh;
            }
        }

        protected abstract Mesh GetSimplifiedMesh(Mesh origin, float quality);
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

        
    }
}
