using System;
using System.Collections;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.Simplifier
{
    public class UnityMeshSimplifier : SimplifierBase
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

            int triCount = 0;
            for (int i = 0; i < meshSimplifier.SubMeshCount; ++i)
            {
                triCount += meshSimplifier.GetSubMeshTriangles(i).Length;
            }

            Utils.WorkingMesh nwm = new WorkingMesh(Allocator.Persistent, meshSimplifier.Vertices.Length, triCount, meshSimplifier.SubMeshCount, 0);
            nwm.name = origin.name;
            nwm.vertices = meshSimplifier.Vertices;
            nwm.normals = meshSimplifier.Normals;
            nwm.tangents = meshSimplifier.Tangents;
            nwm.uv = meshSimplifier.UV1;
            nwm.uv2 = meshSimplifier.UV2;
            nwm.uv3 = meshSimplifier.UV3;
            nwm.uv4 = meshSimplifier.UV4;
            nwm.colors = meshSimplifier.Colors;
            nwm.subMeshCount = meshSimplifier.SubMeshCount;
            for (var submesh = 0; submesh < nwm.subMeshCount; submesh++)
            {
                nwm.SetTriangles(meshSimplifier.GetSubMeshTriangles(submesh), submesh);
            }

            if (resultCallback != null)
            {
                resultCallback(nwm);
            }
            yield break;
        }

        

        public static void OnGUI(SerializableDynamicObject simplifierOptions)
        {
            OnGUIBase(simplifierOptions);
        }
    }
}