using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace Unity.HLODSystem
{
    public class SimpleBatcher : IBatcher
    {
        Texture2D whiteTexture
        {
            get
            {
                if (!m_WhiteTexture)
                {
                    if (!m_WhiteTexture)
                    {
                        m_WhiteTexture = Object.Instantiate(Texture2D.whiteTexture);
                    }
                }

                return m_WhiteTexture;
            }
        }

        Texture2D m_WhiteTexture;

        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            BatcherTypes.RegisterBatcherType(typeof(SimpleBatcher));
        }

        TexturePacker m_Packer = new TexturePacker();

        //from AutoLOD
        //https://github.com/Unity-Technologies/AutoLOD
        public void Batch(HLOD rootHlod, GameObject[] targets)
        {
            dynamic options = rootHlod.BatcherOptions;

            PackingTexture(targets, options);

            for (int i = 0; i < targets.Length; ++i)
            {
                Combine(targets[i], options);
            }
        }

        private void PackingTexture(GameObject[] targets, dynamic options)
        {
            
            for (int i = 0; i < targets.Length; ++i)
            {
                var renderers = targets[i].GetComponentsInChildren<Renderer>();
                var textures = new HashSet<Texture2D>();

                for (int r = 0; r < renderers.Length; ++r)
                {
                    var materials = renderers[r].sharedMaterials;
                    

                    for (int m = 0; m < materials.Length; ++m)
                    {
                        Texture2D tex = materials[m].mainTexture as Texture2D;
                        if (tex == null)
                        {
                            textures.Add(whiteTexture);
                        }
                        else
                        {
                            textures.Add(tex);
                        }
                    }
                }


                m_Packer.AddTextureGroup(targets[i], textures.ToArray());
            }

            
            m_Packer.Pack(options.PackTextureSize, options.LimitTextureSize);
        }

        private void Combine(GameObject root, dynamic options)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var atlas = m_Packer.GetAtlas(root);
            var atlasLookup = new Dictionary<Texture2D, Rect>();

            for (int i = 0; i < atlas.Textures.Length; ++i)
            {
                atlasLookup[atlas.Textures[i]] = atlas.UVs[i];
            }

            var meshFilters = root.GetComponentsInChildren<MeshFilter>();
            var combineInstances = new List<CombineInstance>();
            var combinedMesh = new Mesh();

            for (int i = 0; i < meshFilters.Length; ++i)
            {
                var mesh = ConvertMesh(meshFilters[i], atlasLookup);

                for (int j = 0; j < mesh.subMeshCount; ++j)
                {
                    var ci = new CombineInstance();
                    ci.mesh = mesh;
                    ci.subMeshIndex = j;
                    ci.transform = meshFilters[i].transform.localToWorldMatrix;
                    combineInstances.Add(ci);
                }
            }

            combinedMesh.indexFormat = IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combineInstances.ToArray());
            combinedMesh.RecalculateBounds();

            for (int i = 0; i < meshFilters.Length; i++)
            {
                Object.DestroyImmediate(meshFilters[i].gameObject);
            }

            var go = new GameObject("CombinedMesh");
            var meshRenderer = go.AddComponent<MeshRenderer>();
            var meshFilter = go.AddComponent<MeshFilter>();

            Material material = null;

            go.transform.SetParent(root.transform);

            string materialGUID = options.MaterialGUID;
            if (string.IsNullOrEmpty(materialGUID) == true)
            {
                material = new Material(Shader.Find("Standard"));
            }
            else
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGUID);
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(materialPath));
            }

            material.mainTexture = atlas.PacktedTexture;
            meshFilter.sharedMesh = combinedMesh;
            meshRenderer.material = material;
        }


        private Mesh ConvertMesh(MeshFilter filter, Dictionary<Texture2D, Rect> atlasLookup)
        {
            var sharedMesh = filter.sharedMesh;

            if (sharedMesh.isReadable == false)
            {
                var assetPath = AssetDatabase.GetAssetPath(sharedMesh);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    if (importer)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                }
            }

            var meshRenderer = filter.GetComponent<MeshRenderer>();
            var sharedMaterials = meshRenderer.sharedMaterials;

            var mesh = Object.Instantiate(sharedMesh);

            var uv = mesh.uv;
            var colors = mesh.colors;
            var updated = new bool[uv.Length];

            var triangles = new List<int>();
            // Some meshes have submeshes that either aren't expected to render or are missing a material, so go ahead and skip
            var subMeshCount = Mathf.Min(mesh.subMeshCount, sharedMaterials.Length);

            for (int j = 0; j < subMeshCount; j++)
            {
                var sharedMaterial = sharedMaterials[Mathf.Min(j, sharedMaterials.Length - 1)];
                var mainTexture = whiteTexture;

                if (sharedMaterial)
                {
                    var texture = GetTexture(sharedMaterial);
                    if (texture)
                        mainTexture = texture;
                }

                if (mesh.GetTopology(j) != MeshTopology.Triangles)
                {
                    Debug.LogWarning("Mesh must have triangles", filter);
                    continue;
                }

                triangles.Clear();
                mesh.GetTriangles(triangles, j);
                var uvOffset = atlasLookup[mainTexture];
                foreach (var t in triangles)
                {
                    if (!updated[t])
                    {
                        var uvCoord = uv[t];
                        if (mainTexture == whiteTexture)
                        {
                            // Sample at center of white texture to avoid sampling edge colors incorrectly
                            uvCoord.x = 0.5f;
                            uvCoord.y = 0.5f;
                        }

                        uvCoord.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord.x);
                        uvCoord.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord.y);
                        uv[t] = uvCoord;
                        updated[t] = true;
                    }
                }
            }

            mesh.uv = uv;
            mesh.uv2 = null;
            mesh.colors = colors;

            return mesh;
        }


        private Texture2D GetTexture(Material m)
        {
            if (m)
            {
                if ( m.mainTexture != null)
                    return m.mainTexture as Texture2D;            
            }

            return null;
        }


        
        static class Styles
        {
            public static int[] PackTextureSizes = new int[]
            {
                256, 512, 1024, 2048, 4096
            };
            public static string[] PackTextureSizeNames;

            public static int[] LimitTextureSizes = new int[]
            {
                32, 64, 128, 256, 512, 1024
            };
            public static string[] LimitTextureSizeNames;


            static Styles()
            {
                PackTextureSizeNames = new string[PackTextureSizes.Length];
                for (int i = 0; i < PackTextureSizes.Length; ++i)
                {
                    PackTextureSizeNames[i] = PackTextureSizes[i].ToString();
                }

                LimitTextureSizeNames = new string[LimitTextureSizes.Length];
                for (int i = 0; i < LimitTextureSizes.Length; ++i)
                {
                    LimitTextureSizeNames[i] = LimitTextureSizes[i].ToString();
                }
            }
        }
        public static void OnGUI(HLOD hlod)
        {
            EditorGUI.indentLevel += 1;
            dynamic batcherOptions = hlod.BatcherOptions;

            if (batcherOptions.PackTextureSize == null)
                batcherOptions.PackTextureSize = 1024;
            if (batcherOptions.LimitTextureSize == null)
                batcherOptions.LimitTextureSize = 128;
            if (batcherOptions.MaterialGUID == null)
                batcherOptions.MaterialGUID = "";

            batcherOptions.PackTextureSize = EditorGUILayout.IntPopup("Pack texture size", batcherOptions.PackTextureSize, Styles.PackTextureSizeNames, Styles.PackTextureSizes);
            batcherOptions.LimitTextureSize = EditorGUILayout.IntPopup("Limit texture size", batcherOptions.LimitTextureSize, Styles.LimitTextureSizeNames, Styles.LimitTextureSizes);

            Material mat = null;

            string matGUID = batcherOptions.MaterialGUID;
            string path = "";
            if (string.IsNullOrEmpty(matGUID) == false)
            {
                path = AssetDatabase.GUIDToAssetPath(matGUID);
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            mat = EditorGUILayout.ObjectField("Material", mat, typeof(Material), false) as Material;
            path = AssetDatabase.GetAssetPath(mat);
            batcherOptions.MaterialGUID = AssetDatabase.AssetPathToGUID(path);

            EditorGUI.indentLevel -= 1;
        }

        
    }

}
