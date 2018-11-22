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

                var ratio = CalcRatio(holder, hlod);
                var mesh = GetSimplifiedMesh(meshFilter.sharedMesh, ratio);

                meshFilter.sharedMesh = mesh;
            }
        }

        protected abstract Mesh GetSimplifiedMesh(Mesh origin, float quality);
        private float CalcRatio(Utils.SimplificationDistanceHolder holder, HLOD hlod)
        {
            if (holder == null || holder.OriginGameObject == null)
                return hlod.SimplifyPolygonRatio;

            var transform = holder.OriginGameObject.transform;
            float ratio = hlod.SimplifyPolygonRatio;
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
