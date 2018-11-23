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

        protected override Mesh GetSimplifiedMesh(Mesh origin, float quality)
        {
            Mesh cachedMesh = Cache.SimplifiedCache.Get(GetType(), origin, quality);
            if (cachedMesh != null)
                return cachedMesh;

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

            Cache.SimplifiedCache.Update(GetType(), origin, resultMesh, quality);

            return resultMesh;
        }

        

        public static void OnGUI(HLOD hlod)
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
#endif