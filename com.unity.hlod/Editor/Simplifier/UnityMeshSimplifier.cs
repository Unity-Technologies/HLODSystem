using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

[assembly:Unity.HLODSystem.OptionalDependency("UnityMeshSimplifier.MeshSimplifier", "ENABLE_UNITYMESHSIMPLIFIER")]
#if ENABLE_UNITYMESHSIMPLIFIER

namespace Unity.HLODSystem.Simplifier
{
    class UnityMeshSimplifier : SimplifierBase
    {

        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            SimplifierTypes.RegisterType(typeof(UnityMeshSimplifier));
        }

        public UnityMeshSimplifier(HLOD hlod) : base(hlod)
        {
        }

        protected override IEnumerator GetSimplifiedMesh(Mesh origin, float quality, Action<Mesh> resultCallback)
        {
            var meshSimplifier = new global::UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Vertices = origin.vertices;
            meshSimplifier.Normals = origin.normals;
            meshSimplifier.Tangents = origin.tangents;
            meshSimplifier.UV1 = origin.uv;
            meshSimplifier.UV2 = origin.uv2;
            meshSimplifier.UV3 = origin.uv3;
            meshSimplifier.UV4 = origin.uv4;
            meshSimplifier.Colors = origin.colors;

            var triangles = new int[origin.subMeshCount][];
            for (var submesh = 0; submesh < origin.subMeshCount; submesh++)
            {
                triangles[submesh] = origin.GetTriangles(submesh);
            }

            meshSimplifier.AddSubMeshTriangles(triangles);

            meshSimplifier.SimplifyMesh(quality);

            Mesh resultMesh = new Mesh();
            resultMesh.vertices = meshSimplifier.Vertices;
            resultMesh.normals = meshSimplifier.Normals;
            resultMesh.tangents = meshSimplifier.Tangents;
            resultMesh.uv = meshSimplifier.UV1;
            resultMesh.uv2 = meshSimplifier.UV2;
            resultMesh.uv3 = meshSimplifier.UV3;
            resultMesh.uv4 = meshSimplifier.UV4;
            resultMesh.colors = meshSimplifier.Colors;
            resultMesh.subMeshCount = meshSimplifier.SubMeshCount;
            for (var submesh = 0; submesh < resultMesh.subMeshCount; submesh++)
            {
                resultMesh.SetTriangles(meshSimplifier.GetSubMeshTriangles(submesh), submesh);
            }

            if (resultCallback != null)
            {
                resultCallback(resultMesh);
            }
            yield break;
        }

        

        public static void OnGUI(HLOD hlod)
        {
            OnGUIBase(hlod);
        }
    }
}
#endif