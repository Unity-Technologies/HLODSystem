using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
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

        private TexturePacker m_Packer = new TexturePacker();
        private Dictionary<Texture2D, Material> m_createdMaterials = new Dictionary<Texture2D, Material>();
        private HLOD m_hlod;

        public SimpleBatcher(HLOD hlod)
        {
            m_hlod = hlod;
        }
        
        public void Batch(List<HLODBuildInfo> targets)
        {
            dynamic options = m_hlod.BatcherOptions;
            PackingTexture(targets, options);

            for (int i = 0; i < targets.Count; ++i)
            {
                Combine(targets[i], options);
            }
        }

        private void PackingTexture(List<HLODBuildInfo> targets, dynamic options)
        {
            for (int i = 0; i < targets.Count; ++i)
            {
                var renderers = targets[i].renderers;
                var textures = new HashSet<Texture2D>();

                for (int r = 0; r < renderers.Count; ++r)
                {
                    var materials = renderers[r].sharedMaterials;

                    for (int m = 0; m < materials.Length; ++m)
                    {
                        Texture2D tex = GetTexture(materials[m]);
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
            m_Packer.SaveTextures(GetPrefabDirectory(), m_hlod.name);
        }

        
        static string GetPrefabDirectory()
        {
            string path = PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath;
            return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
        }

        private void Combine(HLODBuildInfo info, dynamic options)
        {
            var renderers = info.renderers;
            var atlas = m_Packer.GetAtlas(info);
            var atlasLookup = new Dictionary<Texture2D, Rect>();

            for (int i = 0; i < atlas.Textures.Length; ++i)
            {
                atlasLookup[atlas.Textures[i]] = atlas.UVs[i];
            }


            var combineInstances = new List<CombineInstance>();
            var combinedMesh = new Mesh();

            for ( int i = 0; i < info.renderers.Count; ++i )
            {
                var mf = info.renderers[i].GetComponent<MeshFilter>();
                if (mf == null)
                    continue;

                var mesh = ConvertMesh(mf, info.simplifiedMeshes[i], atlasLookup);

                for (int j = 0; j < mesh.subMeshCount; ++j)
                {
                    var ci = new CombineInstance();
                    ci.mesh = mesh;
                    ci.subMeshIndex = j;
                    ci.transform = mf.transform.localToWorldMatrix;
                    combineInstances.Add(ci);
                }
            }

            combinedMesh.indexFormat = IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combineInstances.ToArray());
            combinedMesh.RecalculateBounds();

            var go = new GameObject(info.name);
            var meshRenderer = go.AddComponent<MeshRenderer>();
            var meshFilter = go.AddComponent<MeshFilter>();

            go.transform.SetParent(m_hlod.transform);
            meshFilter.sharedMesh = combinedMesh;
            meshRenderer.material = GetMaterial(options, atlas.PacktedTexture);

            info.combinedGameObjects.Add(go);
        }


        private Mesh ConvertMesh(MeshFilter filter, Mesh mesh, Dictionary<Texture2D, Rect> atlasLookup)
        {
            var ret = Object.Instantiate(mesh);
            var meshRenderer = filter.GetComponent<MeshRenderer>();
            var sharedMaterials = meshRenderer.sharedMaterials;

            var uv = mesh.uv;
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

            ret.uv = uv;
            ret.uv2 = null;

            return ret;
        }


        private Dictionary<Texture2D, Material> m_cachedMaterial = new Dictionary<Texture2D, Material>();
        private Material GetMaterial(dynamic options, Texture2D texture)
        {            
            string materialGUID = options.MaterialGUID;
            Material material = null;

            if (m_cachedMaterial.ContainsKey(texture) == true)
            {
                return m_cachedMaterial[texture];
            }

            if (string.IsNullOrEmpty(materialGUID) == true)
            {
                material = new Material(Shader.Find("Standard"));
            }
            else
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGUID);
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(materialPath));
            }
            SetTexture(material, texture);

            string texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath) == false)
            {
                string materialPath = Path.ChangeExtension(texturePath, "mat");
                AssetDatabase.CreateAsset(material, materialPath);
            }


            m_cachedMaterial[texture] = material;
            return material;
        }


        private void SetTexture(Material m, Texture2D t)
        {
            //for the LWRP
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", t);
                return;
            }

            m.SetTexture("_MainTex", t);
        }
        private Texture2D GetTexture(Material m)
        {
            if (m)
            {
                if (m.HasProperty("_BaseMap"))
                {
                    return m.GetTexture("_BaseMap") as Texture2D;
                }
                if ( m.HasProperty("_MainTex"))
                    return m.GetTexture("_MainTex") as Texture2D;            
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
