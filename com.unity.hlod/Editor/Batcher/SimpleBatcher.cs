using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEditor.Experimental.SceneManagement;

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

        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            BatcherTypes.RegisterBatcherType(typeof(SimpleBatcher));
        }

        private Dictionary<TexturePacker.TextureAtlas, Material> m_createdMaterials = new Dictionary<TexturePacker.TextureAtlas, Material>();
        private SerializableDynamicObject m_batcherOptions;
        
        

        [Serializable]
        class TextureInfo
        {
            public string InputName = "_InputProperty";
            public string OutputName = "_OutputProperty";
            public PackingType Type = PackingType.White;
        }

        public SimpleBatcher(SerializableDynamicObject batcherOptions)
        {
            m_batcherOptions = batcherOptions;
        }
        
        public void Batch(Vector3 rootPosition, DisposableList<HLODBuildInfo> targets, Action<float> onProgress)
        {
            dynamic options = m_batcherOptions;
            if (onProgress != null)
                onProgress(0.0f);

            using (TexturePacker packer = new TexturePacker())
            {
                PackingTexture(packer, targets, options, onProgress);

                for (int i = 0; i < targets.Count; ++i)
                {
                    Combine(rootPosition, packer, targets[i], options);
                    if (onProgress != null)
                        onProgress(0.5f + ((float) i / (float) targets.Count) * 0.5f);
                }
            }
        }

        private void PackingTexture(TexturePacker packer, DisposableList<HLODBuildInfo> targets, dynamic options, Action<float> onProgress)
        { 
            List<TextureInfo> textureInfoList = options.TextureInfoList;

            using (var defaultTextures = CreateDefaultTextures())
            {

                for (int i = 0; i < targets.Count; ++i)
                {
                    var workingObjects = targets[i].WorkingObjects;
                    using (var textures = new DisposableDictionary<Guid, TexturePacker.MaterialTexture>())
                    {
                        for (int oi = 0; oi < workingObjects.Count; ++oi)
                        {
                            var materials = workingObjects[oi].Materials;

                            for (int m = 0; m < materials.Count; ++m)
                            {
                                string inputName = textureInfoList[0].InputName;
                                WorkingTexture texture = materials[m].GetTexture(inputName);
                                TexturePacker.MaterialTexture materialTexture = new TexturePacker.MaterialTexture();

                                if (texture == null)
                                    continue;

                                if (textures.ContainsKey(texture.GetGUID()) == true)
                                    continue;

                                materialTexture.Add(texture);

                                for (int ti = 1; ti < textureInfoList.Count; ++ti)
                                {
                                    string input = textureInfoList[ti].InputName;
                                    WorkingTexture tex = materials[m].GetTexture(input);

                                    if (tex == null)
                                    {
                                        tex = defaultTextures[textureInfoList[ti].Type];
                                    }

                                    materialTexture.Add(tex);
                                }

                                textures.Add(texture.GetGUID(), materialTexture);

                            }
                        }


                        packer.AddTextureGroup(targets[i], textures.Values.ToList());
                    }

                    if (onProgress != null)
                        onProgress(((float) i / targets.Count) * 0.1f);
                }
            }

            packer.Pack(options.PackTextureSize, options.LimitTextureSize);
            if ( onProgress != null) onProgress(0.3f);

            int index = 1;
            var atlases = packer.GetAllAtlases();
            foreach (var atlas in atlases)
            {
                Dictionary<string, Texture2D> savedTextures = new Dictionary<string, Texture2D>();
                for (int i = 0; i < atlas.Textures.Count; ++i)
                {
                    var texturePath = $"{GetPrefabDirectoryAndName()}_{index}_{textureInfoList[i].OutputName}.png";
                    Texture2D savedTexture = SaveTexture(atlas.Textures[i], texturePath, textureInfoList[i].Type == PackingType.Normal);
                    savedTextures.Add(textureInfoList[i].OutputName, savedTexture);
                }
                
                var materialPath = $"{GetPrefabDirectoryAndName()}_{index}.mat";
                Material mat = SaveMaterial(options.MaterialGUID, materialPath, savedTextures);
                m_createdMaterials.Add(atlas, mat);
                index += 1;
            }
        }

        static Texture2D SaveTexture(WorkingTexture texture, string path, bool isNormal)
        {           
            var dirPath = Path.GetDirectoryName(path);
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(dirPath);
            }
            
            
            byte[] binary = texture.ToTexture().EncodeToPNG();
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

        static Material SaveMaterial(string guid, string path, Dictionary<string, Texture2D> savedTextures)
        {
            Material material = null;
            if (string.IsNullOrEmpty(guid))
            {
                material = new Material(Shader.Find("Standard"));
            }
            else
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(guid);
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(materialPath));
            }

            foreach (var texture in savedTextures)
            {
                material.SetTexture(texture.Key, texture.Value);
            }
            
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        static string GetPrefabDirectoryAndName()
        {
            string path = PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath;
            return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path);
        }

        private void Combine(Vector3 rootPosition, TexturePacker packer, HLODBuildInfo info, dynamic options)
        {
            var atlas = packer.GetAtlas(info);
            if (atlas == null)
                return;

            List<TextureInfo> textureInfoList = options.TextureInfoList;
            List<MeshCombiner.CombineInfo> combineInfos = new List<MeshCombiner.CombineInfo>();

            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                var obj = info.WorkingObjects[i]; 
                ConvertMesh(obj.Mesh, obj.Materials, atlas, textureInfoList[0].InputName);

                for (int si = 0; si < obj.Mesh.subMeshCount; ++si)
                {
                    var ci = new MeshCombiner.CombineInfo();
                    ci.Mesh = obj.Mesh;
                    ci.MeshIndex = si;
                    
                    ci.Transform = obj.LocalToWorld;
                    ci.Transform.m03 -= rootPosition.x;
                    ci.Transform.m13 -= rootPosition.y;
                    ci.Transform.m23 -= rootPosition.z;
                    
                    combineInfos.Add(ci);
                }
            }
            
            MeshCombiner combiner = new MeshCombiner();
            WorkingMesh combinedMesh = combiner.CombineMesh(Allocator.Persistent, combineInfos);

            WorkingObject newObj = new WorkingObject(Allocator.Persistent);
            WorkingMaterial newMat = new WorkingMaterial(Allocator.Persistent, GetMaterialGuid(atlas));
            
            newObj.SetMesh(combinedMesh);
            newObj.Materials.Add(newMat);

            info.WorkingObjects.Dispose();
            info.WorkingObjects = new DisposableList<WorkingObject>();
            info.WorkingObjects.Add(newObj);
        }


        private void ConvertMesh(WorkingMesh mesh, DisposableList<WorkingMaterial> materials, TexturePacker.TextureAtlas atlas, string mainTextureName)
        {
            var uv = mesh.uv;
            var updated = new bool[uv.Length];
            // Some meshes have submeshes that either aren't expected to render or are missing a material, so go ahead and skip
            int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Count);
            for (int mi = 0; mi < subMeshCount; ++mi)
            {
                int[] indices = mesh.GetTriangles(mi);
                foreach (var i in indices)
                {
                    if ( updated[i] == false )
                    {
                        var uvCoord = uv[i];
                        var texture = materials[mi].GetTexture(mainTextureName);
                        
                        if (texture == null || texture.GetGUID() == Guid.Empty)
                        {
                            // Sample at center of white texture to avoid sampling edge colors incorrectly
                            uvCoord.x = 0.5f;
                            uvCoord.y = 0.5f;
                        }
                        else
                        {
                            var uvOffset = atlas.GetUV(texture.GetGUID());
                            uvCoord.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord.x);
                            uvCoord.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord.y);
                        }
                        
                        uv[i] = uvCoord;
                        updated[i] = true;
                    }
                }
                
            }

            mesh.uv = uv;
        }


        private Guid GetMaterialGuid(TexturePacker.TextureAtlas atlas)
        {
            string path = AssetDatabase.GetAssetPath(m_createdMaterials[atlas]);
            return Guid.Parse(AssetDatabase.AssetPathToGUID(path));
        }

        static private WorkingTexture CreateEmptyTexture(int width, int height, Color color)
        {
            WorkingTexture texture = new WorkingTexture(Allocator.Persistent, width, height);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            return texture;
        }
        static private DisposableDictionary<PackingType, WorkingTexture> CreateDefaultTextures()
        {
            DisposableDictionary<PackingType, WorkingTexture> textures = new DisposableDictionary<PackingType, WorkingTexture>();

            textures.Add(PackingType.White, CreateEmptyTexture(4, 4, Color.white));
            textures.Add(PackingType.Black, CreateEmptyTexture(4, 4, Color.black));
            textures.Add(PackingType.Normal, CreateEmptyTexture(4, 4, new Color(0.5f, 0.5f, 1.0f)));

            return textures;
        }

/*
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
        }*/


        
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
