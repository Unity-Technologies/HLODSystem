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

        public UnityMeshSimplifier(SerializableDynamicObject simplifierOptions): base(simplifierOptions)
        {
        }

        protected override IEnumerator GetSimplifiedMesh(Utils.WorkingMesh origin, float quality, Action<Utils.WorkingMesh> resultCallback)
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

            origin.vertices = meshSimplifier.Vertices;
            origin.normals = meshSimplifier.Normals;
            origin.tangents = meshSimplifier.Tangents;
            origin.uv = meshSimplifier.UV1;
            origin.uv2 = meshSimplifier.UV2;
            origin.uv3 = meshSimplifier.UV3;
            origin.uv4 = meshSimplifier.UV4;
            origin.colors = meshSimplifier.Colors;
            origin.subMeshCount = meshSimplifier.SubMeshCount;
            for (var submesh = 0; submesh < origin.subMeshCount; submesh++)
            {
                origin.SetTriangles(meshSimplifier.GetSubMeshTriangles(submesh), submesh);
            }

            if (resultCallback != null)
            {
                resultCallback(origin);
            }
            yield break;
        }

        

        public static void OnGUI(SerializableDynamicObject simplifierOptions)
        {
            OnGUIBase(simplifierOptions);
        }
    }
}
#endif