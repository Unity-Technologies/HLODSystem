using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public class SimpleBatcher : IBatcher
    {
        enum PackingType
        {
            White,
            Black,
            Normal,
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

        [Serializable]
        class TextureInfo
        {
            public string InputName = "_InputProperty";
            public string OutputName = "_OutputProperty";
            public PackingType Type = PackingType.White;
        }

        public SimpleBatcher(HLOD hlod)
        {
            m_hlod = hlod;
        }
        
        public void Batch(List<HLODBuildInfo> targets, Action<float> onProgress)
        {
            dynamic options = m_hlod.BatcherOptions;
            if (onProgress != null)
                onProgress(0.0f);

            PackingTexture(targets, options, onProgress);

            for (int i = 0; i < targets.Count; ++i)
            {
                Combine(targets[i], options);
                if (onProgress != null)
                    onProgress(0.5f + ((float) i / (float) targets.Count) * 0.5f);
            }
        }

        private void PackingTexture(List<HLODBuildInfo> targets, dynamic options, Action<float> onProgress)
        {
            /*
            List<TextureInfo> textureInfoList = options.TextureInfoList;

            for (int i = 0; i < targets.Count; ++i)
            {
                var renderers = targets[i].renderers;
                var textures = new HashSet<TexturePacker.MultipleTexture>();

                for (int r = 0; r < renderers.Count; ++r)
                {
                    var materials = renderers[r].sharedMaterials;

                    for (int m = 0; m < materials.Length; ++m)
                    {
                        TexturePacker.MultipleTexture multipleTexture = new TexturePacker.MultipleTexture();
                        
                        foreach (var info in textureInfoList)
                        {
                            Texture2D tex = materials[m].GetTexture(info.InputName) as Texture2D;

                            if (tex == null)
                            {
                                multipleTexture.textureList.Add(GetDefaultTexture(info.Type));
                            }
                            else
                            {
                                multipleTexture.textureList.Add(tex);
                            }
                        }

                        textures.Add(multipleTexture);
                    }
                }


                m_Packer.AddTextureGroup(targets[i], textures.ToArray());

                if (onProgress != null)
                    onProgress(((float) i / targets.Count) * 0.1f);
            }

            m_Packer.Pack(options.PackTextureSize, options.LimitTextureSize);
            if ( onProgress != null) onProgress(0.3f);

            int index = 1;
            var atlases = m_Packer.GetAllAtlases();

            foreach (var atlas in atlases)
            {
                for (int i = 0; i < atlas.PackedTexture.Length; ++i)
                {
                    var name = GetPrefabDirectory() + Path.DirectorySeparatorChar + m_hlod.name + index++ + "_" + i + ".png";
                    atlas.PackedTexture[i] = SaveTexture(atlas.PackedTexture[i], name, textureInfoList[i].Type == PackingType.Normal);
                }
            }

            if ( onProgress != null) onProgress(0.5f);

            var savedAtlases = m_Packer.GetAllAtlases();
            for (int i = 0; i < savedAtlases.Length; ++i)
            {
                m_hlod.GeneratedObjects.AddRange(savedAtlases[i].PackedTexture);
            }
            */
        }

        static Texture2D  SaveTexture(Texture2D texture, string path, bool isNormal)
        {           
            var dirPath = Path.GetDirectoryName(path);
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(dirPath);
            }

            
            byte[] binary = texture.EncodeToPNG();
            File.WriteAllBytes(path,binary);

            AssetDatabase.ImportAsset(path);

            if (isNormal == true)
            {
                var assetImporter = AssetImporter.GetAtPath(path);
                var textureImporter = assetImporter as TextureImporter;
                if (textureImporter)
                {
                    textureImporter.textureType = TextureImporterType.NormalMap;
                    textureImporter.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        
        static string GetPrefabDirectory()
        {
            string path = PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath;
            return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
        }

        private void Combine(HLODBuildInfo info, dynamic options)
        {
            /*
            var renderers = info.renderers;
            var atlas = m_Packer.GetAtlas(info);
            var atlasLookup = new Dictionary<Texture2D, Rect>();

            for (int i = 0; i < atlas.MultipleTextures.Length; ++i)
            {
                atlasLookup[atlas.MultipleTextures[i].textureList[0]] = atlas.UVs[i];
            }

            List<TextureInfo> textureInfoList = options.TextureInfoList;
            var combineInstances = new List<CombineInstance>();
            var combinedMesh = new Mesh();

            for ( int i = 0; i < info.renderers.Count; ++i )
            {
                var mf = info.renderers[i].GetComponent<MeshFilter>();
                if (mf == null)
                    continue;

                var mesh = ConvertMesh(mf, info.WorkingMeshes[i], atlasLookup, textureInfoList[0]);

                for (int j = 0; j < mesh.subMeshCount; ++j)
                {
                    var ci = new CombineInstance();
                    ci.mesh = mesh.ToMesh();
                    ci.subMeshIndex = j;

                    Matrix4x4 mat = mf.transform.localToWorldMatrix;
                    Vector3 position = m_hlod.transform.position;
                    mat.m03 -= position.x;
                    mat.m13 -= position.y;
                    mat.m23 -= position.z;
                    ci.transform = mat;
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
            meshRenderer.material = GetMaterial(options, atlas.PackedTexture);

            info.combinedGameObjects.Add(go);
            */
        }


        private Utils.WorkingMesh ConvertMesh(MeshFilter filter, Utils.WorkingMesh mesh, Dictionary<Texture2D, Rect> atlasLookup, TextureInfo mainInfo)
        {
            var defaultTexture = GetDefaultTexture(mainInfo.Type);

//            var ret = mesh.Clone();
//            var meshRenderer = filter.GetComponent<MeshRenderer>();
//            var sharedMaterials = meshRenderer.sharedMaterials;
//
//            var uv = mesh.uv;
//            var updated = new bool[uv.Length];
//
//            var triangles = new List<int>();
//            // Some meshes have submeshes that either aren't expected to render or are missing a material, so go ahead and skip
//            var subMeshCount = Mathf.Min(mesh.subMeshCount, sharedMaterials.Length);
//
//            for (int j = 0; j < subMeshCount; j++)
//            {
//                var sharedMaterial = sharedMaterials[j];
//                var mainTexture = defaultTexture;
//
//                if (sharedMaterial)
//                {
//                    var texture = sharedMaterial.GetTexture(mainInfo.InputName) as Texture2D;
//                    if (texture)
//                        mainTexture = texture;
//                }
//
//                if (mesh.GetTopology(j) != MeshTopology.Triangles)
//                {
//                    Debug.LogWarning("Mesh must have triangles", filter);
//                    continue;
//                }
//
//                triangles.Clear();
//                mesh.GetTriangles(triangles, j);
//                var uvOffset = atlasLookup[mainTexture];
//                foreach (var t in triangles)
//                {
//                    if (!updated[t])
//                    {
//                        var uvCoord = uv[t];
//                        if (mainTexture == defaultTexture)
//                        {
//                            // Sample at center of white texture to avoid sampling edge colors incorrectly
//                            uvCoord.x = 0.5f;
//                            uvCoord.y = 0.5f;
//                        }
//
//                        uvCoord.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord.x);
//                        uvCoord.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord.y);
//                        uv[t] = uvCoord;
//                        updated[t] = true;
//                    }
//                }
//            }
//
//            ret.uv = uv;
//            ret.uv2 = null;

//            return ret;
            return null;
        }


        private Dictionary<Texture2D, Material> m_cachedMaterial = new Dictionary<Texture2D, Material>();
        private Material GetMaterial(dynamic options, Texture2D[] texture)
        {            
            string materialGUID = options.MaterialGUID;
            Material material = null;

            if (m_cachedMaterial.ContainsKey(texture[0]) == true)
            {
                return m_cachedMaterial[texture[0]];
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

            List<TextureInfo> textureInfoList = options.TextureInfoList;
            for ( int i = 0; i < textureInfoList.Count; ++i)
            {
                material.SetTexture(textureInfoList[i].OutputName, texture[i]);
            }

            string texturePath = AssetDatabase.GetAssetPath(texture[0]);
            if (string.IsNullOrEmpty(texturePath) == false)
            {
                string materialPath = Path.ChangeExtension(texturePath, "mat");
                AssetDatabase.CreateAsset(material, materialPath);
                m_hlod.GeneratedObjects.Add(material);
            }

            m_cachedMaterial[texture[0]] = material;
            return material;
        }

        private Texture2D GetDefaultTexture(PackingType type)
        {
            switch (type)
            {
                case PackingType.White:
                    return Texture2D.whiteTexture;
                case PackingType.Black:
                    return Texture2D.blackTexture;
                case PackingType.Normal:
                    return Texture2D.normalTexture;
            }
            
            return Texture2D.whiteTexture;
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

        private static string[] inputTexturePropertyNames = null;
        private static string[] outputTexturePropertyNames = null;
        private static TextureInfo addingTextureInfo = new TextureInfo();
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
            if (batcherOptions.TextureInfoList == null)
            {
                batcherOptions.TextureInfoList = new List<TextureInfo>();
                batcherOptions.TextureInfoList.Add(new TextureInfo()
                {
                    InputName = "_MainTex",
                    OutputName = "_MainTex",
                    Type = PackingType.White
                });
            }

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
            matGUID = AssetDatabase.AssetPathToGUID(path);
            if (matGUID != batcherOptions.MaterialGUID)
            {
                batcherOptions.MaterialGUID = matGUID;
                outputTexturePropertyNames = mat.GetTexturePropertyNames();
            }



            if (inputTexturePropertyNames == null)
            {
                inputTexturePropertyNames = GetAllMaterialTextureProperties(hlod.gameObject);
            }
            if (outputTexturePropertyNames == null)
            {
                if( mat == null)
                    mat = new Material(Shader.Find("Standard"));

                outputTexturePropertyNames = mat.GetTexturePropertyNames();
            }

            //ext textures
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Textures");
            EditorGUI.indentLevel += 1;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            //EditorGUILayout.LabelField();
            EditorGUILayout.SelectableLabel("Input");
            EditorGUILayout.SelectableLabel("Output");
            EditorGUILayout.SelectableLabel("Type");
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < batcherOptions.TextureInfoList.Count; ++i)
            {
                TextureInfo info = batcherOptions.TextureInfoList[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(" ");

                info.InputName = StringPopup(info.InputName, inputTexturePropertyNames);
                info.OutputName = StringPopup(info.OutputName, outputTexturePropertyNames);
                info.Type = (PackingType)EditorGUILayout.EnumPopup(info.Type);

                if (i == 0)
                    GUI.enabled = false;
                if (GUILayout.Button("x") == true)
                {
                    batcherOptions.TextureInfoList.RemoveAt(i);
                    i -= 1;
                }
                if (i == 0)
                    GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("New texture");
            addingTextureInfo.InputName =
                StringPopup(addingTextureInfo.InputName, inputTexturePropertyNames);
            addingTextureInfo.OutputName =
                StringPopup(addingTextureInfo.OutputName, outputTexturePropertyNames);
            addingTextureInfo.Type = (PackingType)EditorGUILayout.EnumPopup(addingTextureInfo.Type);
            if (GUILayout.Button("+") == true)
            {
                batcherOptions.TextureInfoList.Add(addingTextureInfo);
                addingTextureInfo = new TextureInfo();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Update texture properties"))
            {
                //TODO: Need update automatically
                inputTexturePropertyNames = GetAllMaterialTextureProperties(hlod.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel -= 1;
            EditorGUI.indentLevel -= 1;
        }

        static string StringPopup(string select, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                EditorGUILayout.Popup(0, new string[] {select});
                return select;
            }

            int index = Array.IndexOf(options, select);
            if (index < 0)
                index = 0;

            int selected = EditorGUILayout.Popup(index, options);
            return options[selected];
        }

        static string[] GetAllMaterialTextureProperties(GameObject root)
        {
            var meshRenderers = root.GetComponentsInChildren<MeshRenderer>();
            HashSet<string> texturePropertyNames = new HashSet<string>();
            for (int m = 0; m < meshRenderers.Length; ++m)
            {
                var mesh = meshRenderers[m];
                foreach (Material material in mesh.sharedMaterials)
                {
                    var names = material.GetTexturePropertyNames();
                    for (int n = 0; n < names.Length; ++n)
                    {
                        texturePropertyNames.Add(names[n]);
                    }    
                }
                
            }

            return texturePropertyNames.ToArray();
        }

        
    }

}
