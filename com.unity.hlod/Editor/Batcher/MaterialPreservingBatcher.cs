using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.HLODSystem
{
    /// <summary>
    /// A batcher that preserves materials when combining meshes (does not reduce draw calls)
    /// </summary>
    class MaterialPreservingBatcher : IBatcher
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            BatcherTypes.RegisterBatcherType(typeof(MaterialPreservingBatcher));
        }

        private HLOD m_hlod;

        public MaterialPreservingBatcher(HLOD hlod)
        {
            m_hlod = hlod;
        }
  
        public void Batch(List<HLODBuildInfo> targets, Action<float> onProgress)
        {
            for (int i = 0; i < targets.Count; ++i)
            {
                Combine(targets[i]);

                if (onProgress != null)
                    onProgress((float) i / (float)targets.Count);
            }

        }

        private struct CombineInfo
        {
            public Matrix4x4 Transform;
            public WorkingMesh Mesh;
            public int MeshIndex;

        }

        //first original index
        //second new index
        private Dictionary<int, int> CalculateMeshRemap(int[] indices)
        {
            Dictionary<int, int> remapper = new Dictionary<int, int>();

            for (int i = 0; i < indices.Length; ++i)
            {
                if (remapper.ContainsKey(indices[i]))
                    continue;
                
                remapper.Add(indices[i], remapper.Count);
            }

            return remapper;
        }

        private void FillBuffer<T>(ref List<T> buffer, T[] source, Dictionary<int,int> remapper, T defaultValue)
        { 
            int startIndex = buffer.Count;
            buffer.AddRange(Enumerable.Repeat(defaultValue, remapper.Count));
            
            if (source == null || source.Length == 0)
            {

                return;
            }

            foreach (var pair in remapper)
            {
                buffer[pair.Value + startIndex] = source[pair.Key];
            }
        }

        private void FillIndices(ref List<int> buffer, int[] source, Dictionary<int,int> remapper, int startIndex )
        {
            for (int i = 0; i < source.Length; ++i)
            {
                int newIndex = remapper[source[i]] + startIndex;
                buffer.Add(newIndex);
            }
        }

        private WorkingMesh CombineMesh(Allocator allocator, List<CombineInfo> infos)
        {
            //I didn't consider animation mesh combine.
            int verticesCount = 0;
            int normalCount = 0;
            int tangentCount = 0;
            int UV1Count = 0;
            int UV2Count = 0;
            int UV3Count = 0;
            int UV4Count = 0;
            int colorCount = 0;

            int trianglesCount = 0;
            
            List<Dictionary<int,int>> remappers = new List<Dictionary<int, int>>(infos.Count);
            
            for (int i = 0; i < infos.Count; ++i)
            {
                int[] meshIndices = infos[i].Mesh.GetTriangles(infos[i].MeshIndex);
                Dictionary<int, int> remapper = CalculateMeshRemap(meshIndices);

                verticesCount += (infos[i].Mesh.vertices.Length > 0) ? remapper.Count : 0;
                normalCount += (infos[i].Mesh.normals.Length > 0) ? remapper.Count : 0;
                tangentCount += (infos[i].Mesh.tangents.Length > 0) ? remapper.Count : 0;
                UV1Count += (infos[i].Mesh.uv.Length > 0) ? remapper.Count : 0;
                UV2Count += (infos[i].Mesh.uv2.Length > 0) ? remapper.Count : 0;
                UV3Count += (infos[i].Mesh.uv3.Length > 0) ? remapper.Count : 0;
                UV4Count += (infos[i].Mesh.uv4.Length > 0) ? remapper.Count : 0;
                colorCount += (infos[i].Mesh.colors.Length > 0) ? remapper.Count : 0;

                trianglesCount += meshIndices.Length;
                
                remappers.Add(remapper);
            }
            
            WorkingMesh combinedMesh = new WorkingMesh(allocator, verticesCount, trianglesCount, 1, 0);
            
            List<Vector3> vertices = new List<Vector3>(verticesCount);
            List<Vector3> normals = new List<Vector3>(verticesCount);
            List<Vector4> tangents = new List<Vector4>(verticesCount);
            List<Vector2> uv1s = new List<Vector2>(verticesCount);
            List<Vector2> uv2s = new List<Vector2>(verticesCount);
            List<Vector2> uv3s = new List<Vector2>(verticesCount);
            List<Vector2> uv4s = new List<Vector2>(verticesCount);
            List<Color> colors = new List<Color>(colorCount);
            
            List<int> triangles = new List<int>(trianglesCount);

            for (int i = 0; i < infos.Count; ++i)
            {
                WorkingMesh mesh = infos[i].Mesh;
                Dictionary<int, int> remapper = remappers[i];
                int startIndex = vertices.Count;

                if (verticesCount > 0)
                {
                    FillBuffer(ref vertices, mesh.vertices, remapper, Vector3.zero);
                    for (int vi = startIndex; vi < vertices.Count; ++vi)
                    {
                        vertices[vi] = infos[i].Transform.MultiplyPoint(vertices[vi]);
                    }
                }

                if ( normalCount > 0 )
                    FillBuffer(ref normals, mesh.normals, remapper, Vector3.up);
                if (tangentCount> 0 )
                    FillBuffer(ref tangents, mesh.tangents, remapper, new Vector4(1,0,0,1));
                if ( UV1Count > 0 )
                    FillBuffer(ref uv1s, mesh.uv, remapper, Vector2.zero);
                if ( UV2Count > 0 )
                    FillBuffer(ref uv2s, mesh.uv2, remapper, Vector2.zero);
                if ( UV3Count > 0 )
                    FillBuffer(ref uv3s, mesh.uv3, remapper, Vector2.zero);
                if ( UV4Count > 0 )
                    FillBuffer(ref uv4s, mesh.uv4, remapper, Vector2.zero);
                if ( colorCount > 0 )
                    FillBuffer(ref colors, mesh.colors, remapper, Color.white);

                FillIndices(ref triangles, mesh.GetTriangles(infos[i].MeshIndex), remapper, startIndex);

            }

            combinedMesh.vertices = vertices.ToArray();
            combinedMesh.normals = normals.ToArray();
            combinedMesh.tangents = tangents.ToArray();
            combinedMesh.uv = uv1s.ToArray();
            combinedMesh.uv2 = uv2s.ToArray();
            combinedMesh.uv3 = uv3s.ToArray();
            combinedMesh.uv4 = uv4s.ToArray();
            combinedMesh.colors = colors.ToArray();
            
            combinedMesh.SetTriangles(triangles.ToArray(), 0);

            return combinedMesh;
        }
        private void Combine(HLODBuildInfo info)
        {
            var instancesTable = new Dictionary<Material, List<CombineInstance>>();
            var combineInfos = new Dictionary<Guid, List<CombineInfo>>();

            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                var materials = info.WorkingObjects[i].Materials;
                for (int m = 0; m < materials.Count; ++m)
                {
                    //var mat = materials[m];
                    CombineInfo combineInfo = new CombineInfo();

                    combineInfo.Transform = info.WorkingObjects[i].LocalToWorld;
                    combineInfo.Mesh = info.WorkingObjects[i].Mesh;
                    combineInfo.MeshIndex = m;

                    if (combineInfos.ContainsKey(materials[m].GUID) == false)
                    {
                        combineInfos.Add(materials[m].GUID, new List<CombineInfo>());
                    }
                    
                    combineInfos[materials[m].GUID].Add(combineInfo);
                }
            }

            List<WorkingObject> combinedObjects = new List<WorkingObject>();
            foreach (var pair in combineInfos)
            {
                WorkingMesh combinedMesh = CombineMesh(Allocator.Persistent, pair.Value);
                WorkingObject combinedObject = new WorkingObject(Allocator.Persistent);
                WorkingMaterial material = new WorkingMaterial(Allocator.Persistent, pair.Key);

                combinedObject.SetMesh(combinedMesh);
                combinedObject.AddMaterial(material);
                
                combinedObjects.Add(combinedObject);
            }

            //release before change
            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                info.WorkingObjects[i].Dispose();
            }

            info.WorkingObjects = combinedObjects;


            /*
            for ( int i = 0; i < info.renderers.Count; ++i)
            {
                if (info.renderers[i] == null)
                    continue;

                var materials = info.renderers[i].sharedMaterials;
                    
                for(int m = 0; m < materials.Length; ++m)
                {
                    if (instancesTable.ContainsKey(materials[m]) == false)
                    {
                        instancesTable.Add(materials[m], new List<CombineInstance>());
                    }
                    var instance = new CombineInstance();
                    Matrix4x4 mat = info.renderers[i].localToWorldMatrix;
                    Vector3 position = m_hlod.transform.position;
                    mat.m03 -= position.x;
                    mat.m13 -= position.y;
                    mat.m23 -= position.z;
                    instance.transform = mat;
                    instance.mesh = info.WorkingMeshes[i].ToMesh();
                    instance.subMeshIndex = m;

                    instancesTable[materials[m]].Add(instance);

                }

            }

            foreach (var instances in instancesTable)
            {
                var mesh = new Mesh();
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(instances.Value.ToArray(), true, true, false);
                mesh.name = instances.Key.name;

                var go = new GameObject(info.name + instances.Key.name, typeof(MeshRenderer), typeof(MeshFilter));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = instances.Key;

                go.transform.SetParent(m_hlod.transform);

                info.combinedGameObjects.Add(go);
            }*/
        }

        static void OnGUI(HLOD hlod)
        {

        }

    }
}
