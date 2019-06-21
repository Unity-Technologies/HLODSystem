using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using Unity.Collections;
using Unity.HLODSystem.Simplifier;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
            yield break;
        }



        class Layer : IDisposable
        {
            public Layer(TerrainLayer layer)
            {
                m_diffuseTexture = layer.diffuseTexture.ToWorkingTexture(Allocator.Persistent);
            }
            
            public void Dispose()
            {
                m_diffuseTexture?.Dispose();
            }

            public Color GetColor(int x, int y)
            {
                return m_diffuseTexture.GetPixel(x, y);
            }

            public Color GetColor(float u, float v)
            {
                return m_diffuseTexture.GetPixel(u, v);
            }

            private WorkingTexture m_diffuseTexture;


        }

        private TerrainHLOD m_hlod;
        
        private JobQueue m_queue = new JobQueue(8);
        private Heightmap m_heightmap;

        private Vector3 m_size;
        private DisposableList<WorkingTexture> m_alphamaps;
        private DisposableList<Layer> m_layers;

        private string m_outputDir;
        private string m_outputName;
        
        
        
        private TerrainHLODCreator(TerrainHLOD hlod)
        {
            m_hlod = hlod;
        }
        private static Bounds GetBounds(TerrainData data)
        {
            return new Bounds(data.size * 0.5f, data.size);
        }

        private WorkingObject CreateBakedTerrain(HLODBuildInfo info, Bounds bounds)
        {
            WorkingObject wo = new WorkingObject(Allocator.Persistent);
            int beginX = Mathf.RoundToInt(bounds.min.x / m_size.x * (m_heightmap.Width-1));
            int beginZ = Mathf.RoundToInt(bounds.min.z / m_size.z * (m_heightmap.Height-1));
            int endX = Mathf.RoundToInt(bounds.max.x / m_size.x * (m_heightmap.Width-1));
            int endZ = Mathf.RoundToInt(bounds.max.z / m_size.z * (m_heightmap.Height-1));

            int width = endX - beginX + 1;
            int height = endZ - beginZ + 1;

            info.Heightmap = m_heightmap.GetHeightmap(beginX, beginZ, width, height);
            m_queue.EnqueueJob(() =>
            {
                WorkingMesh mesh = CreateBakedGeometry(info.Heightmap, bounds);
                wo.SetMesh(mesh);
            });

            m_queue.EnqueueJob(() =>
            {
                WorkingMaterial material = CreateBakedMaterial(info, bounds); 
                wo.Materials.Add(material);
            });


            return wo;
        }

        private WorkingMesh CreateBakedGeometry(Heightmap heightmap, Bounds bounds)
        {

            WorkingMesh mesh =
                new WorkingMesh(Allocator.Persistent, heightmap.Width * heightmap.Height,
                    (heightmap.Width - 1) * (heightmap.Height - 1) * 6, 1, 0);

            
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

                    normals[index] = heightmap.GetInterpolatedNormal(uvs[index].x, uvs[index].y);
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

        private WorkingMaterial CreateBakedMaterial(HLODBuildInfo info, Bounds bounds)
        {
            Guid guid = Guid.Empty;
            if (Guid.TryParse(m_hlod.MaterialGUID, out guid) == false)
            {
                guid = Guid.Empty;
            }

            WorkingMaterial material = new WorkingMaterial(Allocator.Persistent, guid);

            m_queue.EnqueueJob(()=>
            {
                WorkingTexture albedo = BakeAlbedo(info, bounds, m_hlod.TextureSize);
                material.AddTexture(m_hlod.AlbedoPropertyName, albedo);
            });

            return material;
        }

        private WorkingTexture BakeAlbedo(HLODBuildInfo info, Bounds bounds, int resolution)
        {
            WorkingTexture albedoTexture = new WorkingTexture(Allocator.Persistent, resolution, resolution);
            
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

                            color += m_layers[li].GetColor(u, v);
                        }

                        color.a = 1.0f;
                        albedoTexture.SetPixel(x, y, color);
                    }
                }
            });
            
            
            return albedoTexture;
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

        private List<BorderVertex> GenerateBorderVertices(Heightmap heightmap, int borderOffset)
        {
            //generate border vertices
            List<BorderVertex> borderVertices = new List<BorderVertex>((heightmap.Width + heightmap.Height) * 2);
            //upside
            for (int i = 0; i < heightmap.Width-1; i += borderOffset)
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
            for (int i = 0; i < heightmap.Height-1; i += borderOffset)
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
            for (int i = heightmap.Width-1; i > 0; i -= borderOffset)
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
            for (int i = heightmap.Height - 1; i > 0; i -= borderOffset)
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
        

        private WorkingMesh MakeBorder(WorkingMesh source, Heightmap heightmap, int borderOffset)
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
                
                List<BorderVertex> borderVertices = GenerateBorderVertices(heightmap, borderOffset);
                
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
                    normals.Add(heightmap.GetInterpolatedNormal(uv.x, uv.y));
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
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();

            for (int i = 0; i < subMeshTris.Count; ++i)
            {
                mesh.SetTriangles(subMeshTris[i], i);
            }

            return mesh;
        }

//        private WorkingMesh MakeFillHoleMesh()
//        {
//            
//        }

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
            nameQueue.Enqueue("");
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
                    ContainerObject = ScriptableObject.CreateInstance<HLODAsset>()
                };

                if (node.ChildTreeNodes != null)
                {
                    for (int i = 0; i < node.ChildTreeNodes.Count; ++i)
                    {
                        trevelQueue.Enqueue(node.ChildTreeNodes[i]);
                        parentQueue.Enqueue(currentNodeIndex);
                        nameQueue.Enqueue(name + "_" + (i + 1));
                        depthQueue.Enqueue(depth +1);
                    }
                }

                string path = $"{m_outputDir}{m_outputName}{name}.asset";
                AssetDatabase.CreateAsset(info.ContainerObject, path);
                info.WorkingObjects.Add(CreateBakedTerrain(info, node.Bounds));
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

        private void SaveBuildInfo(HLODBuildInfo info)
        {
            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                WorkingObject obj = info.WorkingObjects[i];
                string path = $"{m_outputDir}{m_outputName}{info.Name}.asset";

                Mesh mesh = obj.Mesh.ToMesh();
                mesh.name = "Mesh";
                AssetDatabase.AddObjectToAsset(mesh, path);

                for (int mi = 0; mi < obj.Materials.Count; ++mi)
                {
                    WorkingMaterial wm = obj.Materials[mi]; 
                    
                    Material source = wm.ToMaterial();
                    if ( source == null )
                        source = new Material(Shader.Find("Standard"));
                    
                    Material mat = new Material(source);


                    WorkingTexture albedo = wm.GetTexture(m_hlod.AlbedoPropertyName);
                    if (albedo != null)
                    {
                        string albedoPath = $"{m_outputDir}{m_outputName}{info.Name}_albedo.png";
                        Texture2D albedoTexture = albedo.ToTexture();
                        albedoTexture.name = "Albedo";
                        byte[] bytes = albedoTexture.EncodeToPNG();
                        File.WriteAllBytes(albedoPath, bytes);
                        AssetDatabase.ImportAsset(albedoPath);
                        albedoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                        mat.SetTexture(m_hlod.AlbedoPropertyName, albedoTexture);
                    }
                    
                    AssetDatabase.AddObjectToAsset(mat, path);
                    wm.GUID = Guid.Parse(AssetDatabase.AssetPathToGUID(path));

                }
                
                AssetDatabase.ImportAsset(path);


            }
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
                
                PrefabStage stage = PrefabStageUtility.GetPrefabStage(m_hlod.gameObject);
                m_outputDir = stage.prefabAssetPath;
                m_outputName = Path.GetFileNameWithoutExtension(m_outputDir);
                m_outputDir = Path.GetDirectoryName(m_outputDir) + "/";
                
                TerrainData data = m_hlod.TerrainData;

                m_size = data.size;

                m_heightmap = new Heightmap(data.heightmapWidth, data.heightmapHeight, data.size,
                    data.GetHeights(0, 0, data.heightmapWidth, data.heightmapHeight));

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
                        new QuadTreeSpaceSplitter(m_hlod.transform.position, 0.0f, m_hlod.MinSize);

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
                        
//                        yield return new WaitForBranches(progress =>
//                        {
//                            EditorUtility.DisplayProgressBar("Bake HLOD", "Simplify meshes",
//                                0.25f + progress * 0.25f);
//                        });

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
                                    WorkingMesh m = MakeBorder(o.Mesh, info.Heightmap, 2);
                                    
                                    o.SetMesh(m);
                                }
                            });
                        }
                        yield return m_queue.WaitFinish();
                        
                        Debug.Log("[TerrainHLOD] Make Border: " + sw.Elapsed.ToString("g"));
                        sw.Reset();
                        sw.Start();
                        
                        EditorUtility.DisplayProgressBar("Bake HLOD", "Save build info", 0.0f);
                        for (int i = 0; i < buildInfos.Count; ++i)
                        {
                            SaveBuildInfo(buildInfos[i]);
                        }
                        Debug.Log("[TerrainHLOD] SaveBuildInfo: " + sw.Elapsed.ToString("g"));

                        AssetDatabase.SaveAssets();

                        for (int i = 0; i < buildInfos.Count; ++i)
                        {
                            GameObject go = new GameObject(buildInfos[i].Name);
                            go.AddComponent<MeshFilter>().sharedMesh = buildInfos[i].WorkingObjects[0].Mesh.ToMesh();
                            go.AddComponent<MeshRenderer>().sharedMaterial =
                                buildInfos[i].WorkingObjects[0].Materials[0].ToMaterial();
                            go.transform.SetParent(m_hlod.transform);
                        }

                    }
                }
                /*
                //remove seam and hole
                
                //controller
                try
                {
                    AssetDatabase.StartAssetEditing();
                    IStreamingBuilder builder = null;
                        //(IStreamingBuilder)Activator.CreateInstance(hlod.StreamingType, new object[] { hlod });
                    builder.Build(rootNode, buildInfos, progress =>
                    {
                        EditorUtility.DisplayProgressBar("Bake HLOD", "Storing results.", 0.75f + progress * 0.25f);
                    });
                    Debug.Log("[TerrainHLOD] Build: " + sw.Elapsed.ToString("g"));
                    sw.Reset();
                    sw.Start();
                }
                finally
                {

                    AssetDatabase.StopAssetEditing();
                    Debug.Log("[TerrainHLOD] Importing: " + sw.Elapsed.ToString("g"));
                }*/

            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            yield break;
        }

        
    }

}