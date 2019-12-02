using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Simplifier;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public class TerrainHLODCreator
    {
        public static IEnumerator Create(TerrainHLOD hlod)
        {
            TerrainHLODCreator creator = new TerrainHLODCreator(hlod);
            yield return creator.CreateImpl();
        }
        public static IEnumerator Destroy(TerrainHLOD hlod)
        {
            var controller = hlod.GetComponent<HLODControllerBase>();
            if (controller == null)
                yield break;

            try
            {
                EditorUtility.DisplayProgressBar("Destroy HLOD", "Destroying HLOD files", 0.0f);
                var convertedPrefabObjects = hlod.ConvertedPrefabObjects;
                for (int i = 0; i < convertedPrefabObjects.Count; ++i)
                {
                    PrefabUtility.UnpackPrefabInstance(convertedPrefabObjects[i], PrefabUnpackMode.OutermostRoot,
                        InteractionMode.AutomatedAction);
                }

                
                var generatedObjects = hlod.GeneratedObjects;
                for (int i = 0; i < generatedObjects.Count; ++i)
                {
                    if (generatedObjects[i] == null)
                        continue;
                    var path = AssetDatabase.GetAssetPath(generatedObjects[i]);
                    if (string.IsNullOrEmpty(path) == false)
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                    else
                    {
                        //It means scene object.
                        //Destroy it.
                        Object.DestroyImmediate(generatedObjects[i]);
                    }

                    EditorUtility.DisplayProgressBar("Destroy HLOD", "Destroying HLOD files", (float)i / (float)generatedObjects.Count);
                }
                generatedObjects.Clear();
                Object.DestroyImmediate(controller);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            EditorUtility.SetDirty(hlod.gameObject);
            EditorUtility.SetDirty(hlod);
        }



        class Layer : IDisposable
        {
            public Layer(TerrainLayer layer)
            {
                WorkingTexture texture = layer.diffuseTexture.ToWorkingTexture(Allocator.Persistent);
                
                m_diffuseTexstures = new DisposableList<WorkingTexture>();
                m_diffuseTexstures.Add(texture);

                m_offset = layer.tileOffset;
                m_size = layer.tileSize;
                var diffuseRemapMin = layer.diffuseRemapMin;
                var diffuseRemapMax = layer.diffuseRemapMax;

                RemapTexture(texture, diffuseRemapMin, diffuseRemapMax);
                
                while (texture.Width > 2 || texture.Height > 2)
                {
                    texture = GenerateMipmap(texture);
                    m_diffuseTexstures.Add(texture);
                }
                
                
            }
            
            public void Dispose()
            {
                m_diffuseTexstures?.Dispose();
            }

            public Color GetColor(float u, float v, int mipLevel = 0)
            {
                u = u - Mathf.Floor(u);
                v = v - Mathf.Floor(v);

                mipLevel = Mathf.Min(mipLevel, m_diffuseTexstures.Count - 1);
                
                return m_diffuseTexstures[mipLevel].GetPixel(u, v);
            }

            public Color GetColorByWorld(float wx, float wz, float sx, float sz)
            {
                float u = (wx + m_offset.x) / m_size.x;
                float v = (wz + m_offset.y) / m_size.y;

                float mipx = sx / m_size.x;
                float mipy = sz / m_size.y;

                float mip = Mathf.Max(mipx, mipy);

                return GetColor(u, v, Mathf.RoundToInt(mip));
            }

            private WorkingTexture GenerateMipmap(WorkingTexture source)
            {
                int sx = Mathf.Max(source.Width >> 1, 1);
                int sy = Mathf.Max(source.Height >> 1, 1);
                
                WorkingTexture mipmap = new WorkingTexture(Allocator.Persistent, source.Format, sx, sy, source.Linear);
                mipmap.Name = source.Name;

                for (int y = 0; y < sy; ++y)
                {
                    for (int x = 0; x < sx; ++x)
                    {
                        Color color = new Color();

                        int x1 = Mathf.Min(x * 2 + 0, source.Width -1);
                        int x2 = Mathf.Min(x * 2 + 1, source.Width - 1);
                        int y1 = Mathf.Min(y * 2 + 0, source.Height - 1);
                        int y2 = Mathf.Min(y * 2 + 1, source.Height - 1);

                        color += source.GetPixel(x1, y1);
                        color += source.GetPixel(x1, y2);
                        color += source.GetPixel(x2, y1);
                        color += source.GetPixel(x2, y2);

                        color /= 4;

                        mipmap.SetPixel(x, y, color);
                    }
                }

                return mipmap;
            }

            private void RemapTexture(WorkingTexture source, Color min, Color max)
            {
                for (int y = 0; y < source.Height; ++y)
                {
                    for (int x = 0; x < source.Width; ++x)
                    {
                        var color = source.GetPixel(x, y);
                        color = color * max + min;
                        source.SetPixel(x, y, color);
                    }
                }
            }

            private DisposableList<WorkingTexture> m_diffuseTexstures;
            private Vector2 m_offset;
            private Vector2 m_size;
        }

      

        private TerrainHLOD m_hlod;
        
        private JobQueue m_queue = new JobQueue(8);
        private Heightmap m_heightmap;

        private Vector3 m_size;
        private DisposableList<WorkingTexture> m_alphamaps;
        private DisposableList<Layer> m_layers;

        private Material m_terrainMaterial;
        private int m_terrainMaterialInstanceId;
        private string m_terrainMaterialName;

        
        private TerrainHLODCreator(TerrainHLOD hlod)
        {
            m_hlod = hlod;
        }
        private static Bounds GetBounds(TerrainData data)
        {
            return new Bounds(data.size * 0.5f, data.size);
        }

        private WorkingObject CreateBakedTerrain(string name, Bounds bounds, out Heightmap heightmap)
        {
            WorkingObject wo = new WorkingObject(Allocator.Persistent);
            wo.Name = name;
            
            int beginX = Mathf.RoundToInt(bounds.min.x / m_size.x * (m_heightmap.Width-1));
            int beginZ = Mathf.RoundToInt(bounds.min.z / m_size.z * (m_heightmap.Height-1));
            int endX = Mathf.RoundToInt(bounds.max.x / m_size.x * (m_heightmap.Width-1));
            int endZ = Mathf.RoundToInt(bounds.max.z / m_size.z * (m_heightmap.Height-1));

            int width = endX - beginX + 1;
            int height = endZ - beginZ + 1;

            Heightmap subHeightmap =m_heightmap.GetHeightmap(beginX, beginZ, width, height);
            heightmap = subHeightmap;
            
            m_queue.EnqueueJob(() =>
            {
                WorkingMesh mesh = CreateBakedGeometry(name, subHeightmap, bounds);
                wo.SetMesh(mesh);
            });

            m_queue.EnqueueJob(() =>
            {
                WorkingMaterial material = CreateBakedMaterial(name, bounds); 
                wo.Materials.Add(material);
            });


            return wo;
        }

        private WorkingMesh CreateBakedGeometry(string name, Heightmap heightmap, Bounds bounds)
        {

            WorkingMesh mesh =
                new WorkingMesh(Allocator.Persistent, heightmap.Width * heightmap.Height,
                    (heightmap.Width - 1) * (heightmap.Height - 1) * 6, 1, 0);

            mesh.name = name + "_Mesh";

            
            Vector3[] vertices = new Vector3[(heightmap.Width -2)* (heightmap.Height-2)];
            Vector3[] normals = new Vector3[(heightmap.Width -2)* (heightmap.Height-2)];
            Vector2[] uvs = new Vector2[(heightmap.Width -2)* (heightmap.Height-2)];
            int[] triangles = new int[(heightmap.Width - 3) * (heightmap.Height - 3) * 6];


            int vi = 0;
            //except boder line
            for (int z = 1; z < heightmap.Height -1; ++z)
            {
                for (int x = 1; x < heightmap.Width -1; ++x)
                {
                    int index = vi++;

                    vertices[index].x = bounds.size.x * (x) / (heightmap.Width - 1) + bounds.min.x;
                    vertices[index].y = heightmap.Size.y * heightmap[z, x];
                    vertices[index].z = bounds.size.z * (z) / (heightmap.Height - 1) + bounds.min.z;

                    uvs[index].x = (float)x / (heightmap.Width - 1);
                    uvs[index].y = (float)z / (heightmap.Height - 1);

                    if (m_hlod.UseNormal)
                    {
                        normals[index] = Vector3.up;
                    }
                    else
                    {
                        normals[index] = heightmap.GetInterpolatedNormal(uvs[index].x, uvs[index].y);
                    }
                    
                    
                }
            }

            int ii = 0;
            for (int z = 0; z < heightmap.Height - 3; ++z)
            {
                for (int x = 0; x < heightmap.Width - 3; ++x)
                {
                    int i00 = z * (heightmap.Width -2)+ x;
                    int i10 = z * (heightmap.Width -2)+ x + 1;
                    int i01 = (z + 1) * (heightmap.Width -2)+ x;
                    int i11 = (z + 1) * (heightmap.Width -2)+ x + 1;

                    triangles[ii + 0] = i00;
                    triangles[ii + 1] = i11;
                    triangles[ii + 2] = i10;
                    triangles[ii + 3] = i11;
                    triangles[ii + 4] = i00;
                    triangles[ii + 5] = i01;
                    ii += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);

            return mesh;
        }

        private WorkingMaterial CreateBakedMaterial(string name, Bounds bounds)
        {
            WorkingMaterial material = new WorkingMaterial(Allocator.Persistent, m_terrainMaterialInstanceId, m_terrainMaterialName, true);
            material.Name = name + "_Material";

            m_queue.EnqueueJob(() =>
            {
                WorkingTexture albedo = BakeAlbedo(name, bounds, m_hlod.TextureSize);
                material.AddTexture(m_hlod.AlbedoPropertyName, albedo);
            });

            if (m_hlod.UseNormal)
            {
                m_queue.EnqueueJob(() =>
                {
                    WorkingTexture normal = BakeNormal(name, bounds, m_hlod.TextureSize);
                    material.AddTexture(m_hlod.NormalPropertyName, normal);
                });
            }

            return material;
        }

        private WorkingTexture BakeAlbedo(string name, Bounds bounds, int resolution)
        {
            WorkingTexture albedoTexture = new WorkingTexture(Allocator.Persistent, TextureFormat.RGB24, resolution, resolution, false);
            albedoTexture.Name = name + "_Albedo";
            albedoTexture.WrapMode = TextureWrapMode.Clamp;
            
            m_queue.EnqueueJob(() =>
            {
                float ustart = (bounds.min.x) / m_size.x;
                float vstart = (bounds.min.z) / m_size.z;
                float usize = (bounds.max.x - bounds.min.x) / m_size.x;
                float vsize = (bounds.max.z - bounds.min.z) / m_size.z;
                
                for (int y = 0; y < resolution; ++y)
                {
                    for (int x = 0; x < resolution; ++x)
                    {
                        float u = (float)x / (float)resolution * usize + ustart;
                        float v = (float)y / (float)resolution * vsize + vstart;
                        
                        Color color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

                        for (int li = 0; li < m_layers.Count; ++li)
                        {
                            float weight = 0.0f;
                            switch (li % 4)
                            {
                                case 0:
                                    weight = m_alphamaps[li / 4].GetPixel(u, v).r;
                                    break;
                                case 1:
                                    weight = m_alphamaps[li / 4].GetPixel(u, v).g;
                                    break;
                                case 2:
                                    weight = m_alphamaps[li / 4].GetPixel(u, v).b;
                                    break;
                                case 3:
                                    weight = m_alphamaps[li / 4].GetPixel(u, v).a;
                                    break;
                            }
                            
                            //optimize to skip not effect pixels.
                            if ( weight < 0.01f)
                                continue;

                            float wx = (float) x / (float) resolution * bounds.size.x + bounds.min.x;
                            float wy = (float) y / (float) resolution * bounds.size.z + bounds.min.z;

                            Color c = m_layers[li].GetColorByWorld(wx, wy, bounds.size.x, bounds.size.z);

                            color.r += Mathf.Pow(c.r, 2.2f) * weight;
                            color.g += Mathf.Pow(c.g, 2.2f) * weight;
                            color.b += Mathf.Pow(c.b, 2.2f) * weight;
                        }

                        color.r = Mathf.Pow(color.r, 0.45f);
                        color.g = Mathf.Pow(color.g, 0.45f);
                        color.b = Mathf.Pow(color.b, 0.45f);
                        color.a = 1.0f;
                        albedoTexture.SetPixel(x, y, color);
                    }
                }
            });
            
            
            return albedoTexture;
        }

        private WorkingTexture BakeNormal(string name, Bounds bounds, int resolution)
        {
            WorkingTexture normalTexture = new WorkingTexture(Allocator.Persistent, TextureFormat.RGB24, resolution, resolution, true);
            normalTexture.Name = name + "_Normal";
            normalTexture.WrapMode = TextureWrapMode.Clamp;

            m_queue.EnqueueJob(() =>
            {
                float ustart = (bounds.min.x) / m_size.x;
                float vstart = (bounds.min.z) / m_size.z;
                float usize = (bounds.max.x - bounds.min.x) / m_size.x;
                float vsize = (bounds.max.z - bounds.min.z) / m_size.z;

                for (int y = 0; y < resolution; ++y)
                {
                    for (int x = 0; x < resolution; ++x)
                    {
                        float u = (float) x / (float) resolution * usize + ustart;
                        float v = (float) y / (float) resolution * vsize + vstart;

                        Vector3 normal = m_heightmap.GetInterpolatedNormal(u, v);

                        Color color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                        color.r = normal.x * 0.5f + 0.5f;
                        color.g = -normal.z * 0.5f + 0.5f;
                        color.b = normal.y * 0.5f + 0.5f;
                        
                        color.a = 1.0f;
                        normalTexture.SetPixel(x, y, color);
                    }
                }
            });
            
            
            return normalTexture;
        }

        private List<Vector2Int> GetEdgeList(List<int> tris)
        {
            HashSet<Vector2Int> candidates = new HashSet<Vector2Int>();

            int trisCount = tris.Count / 3;

            for (int i = 0; i < trisCount; ++i)
            {
                Vector2Int[] edges = new[]
                {
                    new Vector2Int(tris[i * 3 + 0], tris[i * 3 + 1]),
                    new Vector2Int(tris[i * 3 + 1], tris[i * 3 + 2]),
                    new Vector2Int(tris[i * 3 + 2], tris[i * 3 + 0])
                };

                for (int ei = 0; ei < edges.Length; ++ei)
                {
                    Vector2Int otherSideEdge = new Vector2Int(edges[ei].y, edges[ei].x);
                    if (candidates.Contains(otherSideEdge) == true)
                    {
                        candidates.Remove(otherSideEdge);
                    }
                    else
                    {
                        candidates.Add(edges[ei]);
                    }
                }
            }
            

            return candidates.ToList();
        }

        struct BorderVertex
        {
            public Vector3 Pos;
            public int ClosestIndex;
        }

        private List<BorderVertex> GenerateBorderVertices(Heightmap heightmap, int borderCount)
        {
            //generate border vertices
            List<BorderVertex> borderVertices = new List<BorderVertex>((heightmap.Width + heightmap.Height) * 2);

            int xBorderOffset = Mathf.Max((heightmap.Width - 1) / borderCount, 1 );    //< avoid 0
            int yBorderOffset = Mathf.Max((heightmap.Height - 1) / borderCount, 1);    //< avoid 0
            
            //upside
            for (int i = 0; i < heightmap.Width-1; i += xBorderOffset)
            {
                float h = heightmap[0, i];

                BorderVertex v;
                v.Pos.x = (heightmap.Size.x * i) / (heightmap.Width-1);
                v.Pos.y = (heightmap.Size.y * h);
                v.Pos.z = 0.0f;
                v.Pos += heightmap.Offset;

                v.ClosestIndex = -1;

                borderVertices.Add(v);
            }

            //rightside
            for (int i = 0; i < heightmap.Height-1; i += yBorderOffset)
            {
                float h = heightmap[i, heightmap.Width - 1];

                BorderVertex v;
                v.Pos.x = heightmap.Size.x;
                v.Pos.y = (heightmap.Size.y * h);
                v.Pos.z = (heightmap.Size.z * i) / (heightmap.Height-1);
                v.Pos += heightmap.Offset;

                v.ClosestIndex = -1;

                borderVertices.Add(v);
            }

            //downside
            for (int i = heightmap.Width-1; i > 0; i -= xBorderOffset)
            {
                float h = heightmap[heightmap.Height - 1, i];

                BorderVertex v;
                v.Pos.x = (heightmap.Size.x * i) / (heightmap.Width-1);
                v.Pos.y = (heightmap.Size.y * h);
                v.Pos.z = heightmap.Size.z;
                v.Pos += heightmap.Offset;

                v.ClosestIndex = -1;

                borderVertices.Add(v);
            }

            //leftside
            for (int i = heightmap.Height - 1; i > 0; i -= yBorderOffset)
            {
                float h = heightmap[i, 0];

                BorderVertex v;
                v.Pos.x = 0.0f;
                v.Pos.y = (heightmap.Size.y * h);
                v.Pos.z = (heightmap.Size.z * i) / (heightmap.Height-1);
                v.Pos += heightmap.Offset;

                v.ClosestIndex = -1;

                borderVertices.Add(v);
            }

            return borderVertices;
        }
        

        private WorkingMesh MakeBorder(WorkingMesh source, Heightmap heightmap, int borderCount)
        {
            List<Vector3> vertices = source.vertices.ToList();
            List<Vector3> normals = source.normals.ToList();
            List<Vector2> uvs = source.uv.ToList();
            List<int[]> subMeshTris = new List<int[]>();

            int maxTris = 0;

            for (int si = 0; si < source.subMeshCount; ++si)
            {
                List<int> tris = source.GetTriangles(si).ToList();
                List<Vector2Int> edges = GetEdgeList(tris);
                HashSet<int> vertexIndces = new HashSet<int>();
                List<BorderVertex> edgeVertices = new List<BorderVertex>();
                
                for (int ei = 0; ei < edges.Count; ++ei)
                {
                    vertexIndces.Add(edges[ei].x);
                    vertexIndces.Add(edges[ei].y);
                }
                
                List<BorderVertex> borderVertices = GenerateBorderVertices(heightmap, borderCount);
                
                //calculate closest vertex from border vertices.
                for (int i = 0; i < borderVertices.Count; ++i)
                {
                    float closestDistance = Single.MaxValue;
                    BorderVertex v = borderVertices[i];
                    foreach (var index in vertexIndces)
                    {
                        Vector3 pos = vertices[index];
                        float dist = Vector3.SqrMagnitude(pos - borderVertices[i].Pos);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            v.ClosestIndex = index;
                        }
                    }

                    borderVertices[i] = v;
                }
                
                //generate tris
                int startAddIndex = vertices.Count;
                for (int bi = 0; bi < borderVertices.Count; ++bi)
                {
                    int next = (bi == borderVertices.Count - 1) ? 0 : bi + 1;
                    
                    tris.Add(bi + startAddIndex);
                    tris.Add(borderVertices[bi].ClosestIndex);
                    tris.Add(next + startAddIndex);

                    Vector2 uv;
                    uv.x = (borderVertices[bi].Pos.x - heightmap.Offset.x) / heightmap.Size.x;
                    uv.y = (borderVertices[bi].Pos.z - heightmap.Offset.z) / heightmap.Size.z;
                    vertices.Add(borderVertices[bi].Pos);
                    
                    if (m_hlod.UseNormal)
                    {
                        normals.Add(Vector3.up);
                    }
                    else
                    {
                        normals.Add(heightmap.GetInterpolatedNormal(uv.x, uv.y));
                    }
                    
                    uvs.Add(uv);
                    
                    if (borderVertices[bi].ClosestIndex == borderVertices[next].ClosestIndex)
                        continue;
                    
                    tris.Add(borderVertices[bi].ClosestIndex);
                    tris.Add(borderVertices[next].ClosestIndex);
                    tris.Add(next + startAddIndex);


                }

                maxTris += tris.Count;
                subMeshTris.Add(tris.ToArray());
            }

            WorkingMesh mesh = new WorkingMesh(Allocator.Persistent, vertices.Count, maxTris, subMeshTris.Count, 0);
            mesh.name = source.name;
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();

            for (int i = 0; i < subMeshTris.Count; ++i)
            {
                mesh.SetTriangles(subMeshTris[i], i);
            }

            return mesh;
        }
        
        

        public class EdgeGroup
        {
            public int Begin = -1;
            public int End = -1;
            public List<Vector2Int> EdgeList = new List<Vector2Int>();
        }

        private WorkingMesh MakeFillHoleMesh(WorkingMesh source)
        {
            int totalTris = 0;
            List<int[]> newTris = new List<int[]>();
            
            for (int si = 0; si < source.subMeshCount; ++si)
            {
                List<int> tris = source.GetTriangles(si).ToList();
                List<Vector2Int> edgeList = GetEdgeList(tris);
                
                
                List<EdgeGroup> groups = new List<EdgeGroup>();
                for (int i = 0; i < edgeList.Count; ++i)
                {
                    EdgeGroup group = new EdgeGroup();
                    group.Begin = edgeList[i].x;
                    group.End = edgeList[i].y;
                    group.EdgeList.Add(edgeList[i]);
                
                    groups.Add(group);
                }

                bool isFinish = false;

                while (isFinish == false)
                {
                    isFinish = true;

                    for (int gi1 = 0; gi1 < groups.Count; ++gi1)
                    {
                        for (int gi2 = gi1 + 1; gi2 < groups.Count; ++gi2)
                        {
                            EdgeGroup g1 = groups[gi1];
                            EdgeGroup g2 = groups[gi2];

                            if (g1.End == g2.Begin)
                            {
                                g1.End = g2.End;
                                g1.EdgeList.AddRange(g2.EdgeList);

                                groups[gi2] = groups[groups.Count - 1];
                                groups.RemoveAt(groups.Count - 1);

                                gi2 -= 1;
                                isFinish = false;
                            }
                            else if (g1.Begin == g2.End)
                            {
                                g2.End = g1.End;
                                g2.EdgeList.AddRange(g1.EdgeList);

                                groups[gi1] = groups[gi2];
                                groups[gi2] = groups[groups.Count - 1];
                                groups.RemoveAt(groups.Count - 1);

                                gi2 -= 1;
                                isFinish = false;
                            }
                        }
                    }
                }

                for (int gi = 0; gi < groups.Count; ++gi)
                {
                    EdgeGroup group = groups[gi];
                    for (int ei1 = 1; ei1 < group.EdgeList.Count-1; ++ei1)
                    {
                        for (int ei2 = ei1 + 1; ei2 < group.EdgeList.Count; ++ei2)
                        {
                            if (group.EdgeList[ei1].x == group.EdgeList[ei2].y)
                            {
                                EdgeGroup ng = new EdgeGroup();
                                ng.Begin = group.EdgeList[ei1].x;
                                ng.End = group.EdgeList[ei2].y;

                                for (int i = ei1; i <= ei2; ++i)
                                {
                                    ng.EdgeList.Add(group.EdgeList[i]);
                                }

                                for (int i = ei2; i >= ei1; --i)
                                {
                                    group.EdgeList.RemoveAt(i);
                                }
                                
                                groups.Add(ng);

                                ei1 = 0; // goto first
                                break;
                            }
                        }
                    }
                }

                if (groups.Count == 0)
                    continue;
                
                groups.Sort((g1, g2) => { return g2.EdgeList.Count - g1.EdgeList.Count; });
                
                //first group( longest group ) is outline. 
                for (int i = 1; i < groups.Count; ++i)
                {
                    EdgeGroup group = groups[i];
                    for (int ei = 1; ei < group.EdgeList.Count - 1; ++ei)
                    {
                        tris.Add(group.Begin);
                        tris.Add(group.EdgeList[ei].y);
                        tris.Add(group.EdgeList[ei].x);
                    }
                
                }

                totalTris += tris.Count;
                newTris.Add(tris.ToArray());
            }
            
            WorkingMesh mesh = new WorkingMesh(Allocator.Persistent, source.vertexCount, totalTris, source.subMeshCount, 0);
            mesh.name = source.name;
            mesh.vertices = source.vertices;
            mesh.normals = source.normals;
            mesh.uv = source.uv;

            for (int i = 0; i < newTris.Count; ++i)
            {
                mesh.SetTriangles(newTris[i], i);
            }

            return mesh;
        }

        private DisposableList<HLODBuildInfo> CreateBuildInfo(TerrainData data, SpaceNode root)
        {
            DisposableList<HLODBuildInfo> results = new DisposableList<HLODBuildInfo>();
            Queue<SpaceNode> trevelQueue = new Queue<SpaceNode>();
            Queue<int> parentQueue = new Queue<int>();
            Queue<string> nameQueue = new Queue<string>();
            Queue<int> depthQueue = new Queue<int>();

            int maxDepth = 0;

            trevelQueue.Enqueue(root);
            parentQueue.Enqueue(-1);
            nameQueue.Enqueue("HLOD");
            depthQueue.Enqueue(0);
            

            while (trevelQueue.Count > 0)
            {
                int currentNodeIndex = results.Count;
                string name = nameQueue.Dequeue();
                SpaceNode node = trevelQueue.Dequeue();
                int depth = depthQueue.Dequeue();
                HLODBuildInfo info = new HLODBuildInfo
                {
                    Name = name,
                    ParentIndex = parentQueue.Dequeue(),
                    Target = node,
                };


                for (int i = 0; i < node.GetChildCount(); ++i)
                {
                    trevelQueue.Enqueue(node.GetChild(i));
                    parentQueue.Enqueue(currentNodeIndex);
                    nameQueue.Enqueue(name + "_" + (i + 1));
                    depthQueue.Enqueue(depth + 1);
                }
                
                Heightmap createdHeightmap;
                info.WorkingObjects.Add(CreateBakedTerrain(name, node.Bounds, out createdHeightmap));
                info.Heightmap = createdHeightmap;
                info.Distances.Add(depth);
                results.Add(info);
                
                if (depth > maxDepth)
                    maxDepth = depth;
            }

            //convert depth to distance
            for (int i = 0; i < results.Count; ++i)
            {
                HLODBuildInfo info = results[i];
                for (int di = 0; di < info.Distances.Count; ++di)
                {
                    info.Distances[di] = maxDepth - info.Distances[di];
                }
            }

            return results;
        }
        
        public IEnumerator CreateImpl()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                sw.Reset();
                sw.Start();
                
                EditorUtility.DisplayProgressBar("Bake HLOD", "Initialize Bake", 0.0f);
                
                
                TerrainData data = m_hlod.TerrainData;

                m_size = data.size;

                m_heightmap = new Heightmap(data.heightmapResolution, data.heightmapResolution, data.size,
                    data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution));

                string materialPath = AssetDatabase.GUIDToAssetPath(m_hlod.MaterialGUID);
                m_terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if ( m_terrainMaterial == null )
                    m_terrainMaterial = new Material(Shader.Find("Standard"));

                m_terrainMaterialInstanceId = m_terrainMaterial.GetInstanceID();
                m_terrainMaterialName = m_terrainMaterial.name;

                using (m_alphamaps = new DisposableList<WorkingTexture>()) 
                using ( m_layers = new DisposableList<Layer>())
                {
                    for (int i = 0; i < data.alphamapTextures.Length; ++i)
                    {
                        m_alphamaps.Add(new WorkingTexture(Allocator.Persistent, data.alphamapTextures[i]));
                    }

                    for (int i = 0; i < data.terrainLayers.Length; ++i)
                    {
                        m_layers.Add(new Layer(data.terrainLayers[i]));
                    }


                    QuadTreeSpaceSplitter splitter =
                        new QuadTreeSpaceSplitter(m_hlod.transform.position, 0.0f, m_hlod.ChunkSize * 2.0f);

                    SpaceNode rootNode = splitter.CreateSpaceTree(GetBounds(data), null, progress => { });

                    EditorUtility.DisplayProgressBar("Bake HLOD", "Create mesh", 0.0f);
                    
                    using (DisposableList<HLODBuildInfo> buildInfos = CreateBuildInfo(data, rootNode))
                    {
                        yield return m_queue.WaitFinish();
                        //Write material & textures
                        
                        for (int i = 0; i < buildInfos.Count; ++i)
                        {
                            int curIndex = i;
                            m_queue.EnqueueJob(() =>
                            {
                                ISimplifier simplifier = (ISimplifier) Activator.CreateInstance(m_hlod.SimplifierType,
                                    new object[] {m_hlod.SimplifierOptions});
                                simplifier.SimplifyImmidiate(buildInfos[curIndex]);
                            });
                        }

                        EditorUtility.DisplayProgressBar("Bake HLOD", "Simplify meshes", 0.0f);
                        yield return m_queue.WaitFinish();

                        Debug.Log("[TerrainHLOD] Simplify: " + sw.Elapsed.ToString("g"));
                        sw.Reset();
                        sw.Start();
                        EditorUtility.DisplayProgressBar("Bake HLOD", "Make border", 0.0f);

                        for (int i = 0; i < buildInfos.Count; ++i)
                        {
                            HLODBuildInfo info = buildInfos[i];
                            m_queue.EnqueueJob(() =>
                            {
                                for (int oi = 0; oi < info.WorkingObjects.Count; ++oi)
                                {
                                    WorkingObject o = info.WorkingObjects[oi];
                                    int borderVertexCount = m_hlod.BorderVertexCount * Mathf.RoundToInt(Mathf.Pow(2.0f, (float)info.Distances[oi]));
                                    using (WorkingMesh m = MakeBorder(o.Mesh, info.Heightmap, borderVertexCount))
                                    {
                                        o.SetMesh(MakeFillHoleMesh(m));
                                    }
                                }
                            });
                        }
                        yield return m_queue.WaitFinish();
                        
                        Debug.Log("[TerrainHLOD] Make Border: " + sw.Elapsed.ToString("g"));
                        sw.Reset();
                        sw.Start();
                        

                        for (int i = 0; i < buildInfos.Count; ++i)
                        {
                            SpaceNode node = buildInfos[i].Target;
                            HLODBuildInfo info = buildInfos[i];
                            if (node.HasChild() == false)
                            {
                                SpaceNode parent = node.ParentNode;
                                node.ParentNode = null;
                                
                                GameObject go = new GameObject(buildInfos[i].Name);

                                for (int wi = 0; wi < info.WorkingObjects.Count; ++wi)
                                {
                                    WorkingObject wo = info.WorkingObjects[wi];
                                    GameObject targetGO = null;
                                    if (wi == 0)
                                    {
                                        targetGO = go;
                                    }
                                    else
                                    {
                                        targetGO = new GameObject(wi.ToString());
                                        targetGO.transform.SetParent(go.transform, false);
                                    }

                                    List<Material> materials = new List<Material>();
                                    for (int mi = 0; mi < wo.Materials.Count; ++mi)
                                    {
                                        
                                        WorkingMaterial wm = wo.Materials[mi];
                                        if (wm.NeedWrite() == false)
                                        {
                                            materials.Add(wm.ToMaterial());
                                            continue;
                                        }
                                        
                                        Material mat = new Material(wm.ToMaterial());
                                        string[] textureNames = wm.GetTextureNames();
                                        for (int ti = 0; ti < textureNames.Length; ++ti)
                                        {
                                            WorkingTexture wt = wm.GetTexture(textureNames[ti]);
                                            Texture2D tex = wt.ToTexture();
                                            tex.wrapMode = wt.WrapMode;
                                            mat.name = targetGO.name + "_Mat"; 
                                            mat.SetTexture(textureNames[ti],tex);
                                        }
                                        mat.EnableKeyword("_NORMALMAP");
                                        materials.Add(mat);
                                    }
                                    
                                    targetGO.AddComponent<MeshFilter>().sharedMesh = wo.Mesh.ToMesh();
                                    targetGO.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                                }
                                
                                go.transform.SetParent(m_hlod.transform, false);
                                m_hlod.AddGeneratedResource(go);

                                parent.Objects.Add(go);
                                buildInfos.RemoveAt(i);
                                i -= 1;
                            }
                        }

                        //controller
                        IStreamingBuilder builder =
                            (IStreamingBuilder) Activator.CreateInstance(m_hlod.StreamingType,
                                new object[] {m_hlod, m_hlod.StreamingOptions});
                        
                        builder.Build(rootNode, buildInfos, m_hlod.gameObject, m_hlod.CullDistance, m_hlod.LODDistance, true,
                            progress =>
                            {
                                EditorUtility.DisplayProgressBar("Bake HLOD", "Storing results.",
                                    0.75f + progress * 0.25f);
                            });
                        
                        Debug.Log("[TerrainHLOD] Build: " + sw.Elapsed.ToString("g"));

                    }
                }

                EditorUtility.SetDirty(m_hlod.gameObject);

            }
            finally
            {
                EditorUtility.ClearProgressBar();
                GC.Collect();
            }
        }

        
    }

}