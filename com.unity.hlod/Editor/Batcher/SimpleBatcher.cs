using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Utils;

namespace Unity.HLODSystem
{
    public class SimpleBatcher : IBatcher
    {
        public enum PackingType
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

        private DisposableDictionary<TexturePacker.TextureAtlas, WorkingMaterial> m_createdMaterials = new DisposableDictionary<TexturePacker.TextureAtlas, WorkingMaterial>();
        private SerializableDynamicObject m_batcherOptions;
        
        

        [Serializable]
        public class TextureInfo
        {
            public string InputName = "_InputProperty";
            public string OutputName = "_OutputProperty";
            public PackingType Type = PackingType.White;
        }

        public SimpleBatcher(SerializableDynamicObject batcherOptions)
        {
            m_batcherOptions = batcherOptions;
        }

        public void Dispose()
        {
            m_createdMaterials.Dispose();
        }

        public void Batch(Transform rootTransform, DisposableList<HLODBuildInfo> targets, Action<float> onProgress)
        {
            dynamic options = m_batcherOptions;
            if (onProgress != null)
                onProgress(0.0f);

            using (TexturePacker packer = new TexturePacker())
            {
                PackingTexture(packer, targets, options, onProgress);

                for (int i = 0; i < targets.Count; ++i)
                {
                    Combine(rootTransform, packer, targets[i], options);
                    if (onProgress != null)
                        onProgress(0.5f + ((float)i / (float)targets.Count) * 0.5f);
                }
            }

        }


        class MaterialTextureCache : IDisposable
        {
            private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
            
            private List<TextureInfo> m_textureInfoList;
            private DisposableDictionary<string, TexturePacker.MaterialTexture> m_textureCache;
            private DisposableDictionary<PackingType, WorkingTexture> m_defaultTextures;
                
            private bool m_enableTintColor;
            private string m_tintColorName;
            
            public MaterialTextureCache(dynamic options)
            {
                m_defaultTextures = CreateDefaultTextures();
                m_enableTintColor = options.EnableTintColor;
                m_tintColorName = options.TintColorName;
                m_textureInfoList = options.TextureInfoList;
                m_textureCache = new DisposableDictionary<string, TexturePacker.MaterialTexture>();
            }
            public TexturePacker.MaterialTexture GetMaterialTextures(WorkingMaterial material)
            {
                if (m_textureCache.ContainsKey(material.Guid) == false)
                {
                    AddToCache(material);
                }

                var textures = m_textureCache[material.Guid];
                if (textures != null)
                {
                    string inputName = m_textureInfoList[0].InputName;
                    material.SetTexture(inputName, textures[0].Clone());
                }

                return textures;
            }

            public void Dispose()
            {
                m_textureCache.Dispose();
                m_defaultTextures.Dispose();
                m_detector.Dispose();
                
            }

            private void AddToCache(WorkingMaterial material)
            {
                string inputName = m_textureInfoList[0].InputName;
                WorkingTexture texture = material.GetTexture(inputName);

                if (texture == null)
                {
                    texture = m_defaultTextures[m_textureInfoList[0].Type];
                }
                
                TexturePacker.MaterialTexture materialTexture = new TexturePacker.MaterialTexture();

                if (m_enableTintColor)
                {
                    Color tintColor = material.GetColor(m_tintColorName);

                    texture = texture.Clone();
                    ApplyTintColor(texture, tintColor);
                    materialTexture.Add(texture);
                    texture.Dispose();
                }
                else
                {
                    materialTexture.Add(texture);
                }
                

                for (int ti = 1; ti < m_textureInfoList.Count; ++ti)
                {
                    string input = m_textureInfoList[ti].InputName;
                    WorkingTexture tex = material.GetTexture(input);

                    if (tex == null)
                    {
                        tex = m_defaultTextures[m_textureInfoList[ti].Type];
                    }

                    materialTexture.Add(tex);
                }

                m_textureCache.Add(material.Guid, materialTexture);
            }
            private void ApplyTintColor(WorkingTexture texture, Color tintColor)
            {
                for (int ty = 0; ty < texture.Height; ++ty)
                {
                    for (int tx = 0; tx < texture.Width; ++tx)
                    {
                        Color c = texture.GetPixel(tx, ty);
                    
                        c.r = c.r * tintColor.r;
                        c.g = c.g * tintColor.g;
                        c.b = c.b * tintColor.b;
                        c.a = c.a * tintColor.a;
                    
                        texture.SetPixel(tx, ty, c);
                    }
                }
            }

            private static DisposableDictionary<PackingType, WorkingTexture> CreateDefaultTextures()
            {
                DisposableDictionary<PackingType, WorkingTexture> textures = new DisposableDictionary<PackingType, WorkingTexture>();

                textures.Add(PackingType.White, CreateEmptyTexture(4, 4, Color.white, false));
                textures.Add(PackingType.Black, CreateEmptyTexture(4, 4, Color.black, false));
                textures.Add(PackingType.Normal, CreateEmptyTexture(4, 4, new Color(0.5f, 0.5f, 1.0f), true));

                return textures;
            }

        }

        private void PackingTexture(TexturePacker packer, DisposableList<HLODBuildInfo> targets, dynamic options, Action<float> onProgress)
        { 
            List<TextureInfo> textureInfoList = options.TextureInfoList;
            using (MaterialTextureCache cache = new MaterialTextureCache(options))
            {
                for (int i = 0; i < targets.Count; ++i)
                {
                    var workingObjects = targets[i].WorkingObjects;
                    Dictionary<Guid, TexturePacker.MaterialTexture> textures =
                        new Dictionary<Guid, TexturePacker.MaterialTexture>();

                    for (int oi = 0; oi < workingObjects.Count; ++oi)
                    {
                        var materials = workingObjects[oi].Materials;

                        for (int m = 0; m < materials.Count; ++m)
                        {
                            var materialTextures = cache.GetMaterialTextures(materials[m]);
                            if (materialTextures == null)
                                continue;

                            if (textures.ContainsKey(materialTextures[0].GetGUID()) == true)
                                continue;

                            textures.Add(materialTextures[0].GetGUID(), materialTextures);
                        }
                    }


                    packer.AddTextureGroup(targets[i], textures.Values.ToList());


                    if (onProgress != null)
                        onProgress(((float) i / targets.Count) * 0.1f);
                }
            }

            packer.Pack(TextureFormat.RGBA32, options.PackTextureSize, options.LimitTextureSize, false);
            if ( onProgress != null) onProgress(0.3f);

            int index = 1;
            var atlases = packer.GetAllAtlases();
            foreach (var atlas in atlases)
            {
                Dictionary<string, WorkingTexture> textures = new Dictionary<string, WorkingTexture>();
                for (int i = 0; i < atlas.Textures.Count; ++i)
                {
                    WorkingTexture wt = atlas.Textures[i];
                    wt.Name = "CombinedTexture " + index + "_" + i;
                    if (textureInfoList[i].Type == PackingType.Normal)
                    {
                        wt.Linear = true;
                    }

                    textures.Add(textureInfoList[i].OutputName, wt);
                }
                
                WorkingMaterial mat = CreateMaterial(options.MaterialGUID, textures);
                mat.Name = "CombinedMaterial " + index;
                m_createdMaterials.Add(atlas, mat);
                index += 1;
            }
        }

        static WorkingMaterial CreateMaterial(string guidstr, Dictionary<string, WorkingTexture> textures)
        {
            WorkingMaterial material = null;
            string path = AssetDatabase.GUIDToAssetPath(guidstr);
            if (string.IsNullOrEmpty(path) == false)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    material = new WorkingMaterial(Allocator.Invalid, mat.GetInstanceID(), mat.name);
                }
            }

            if (material == null)
            {
                material = new WorkingMaterial(Allocator.Persistent, new Material(Shader.Find("Standard")));
            }
            
            foreach (var texture in textures)
            {
                material.AddTexture(texture.Key, texture.Value.Clone());
            }
            
            return material;
        }

        private void Combine(Transform rootTransform, TexturePacker packer, HLODBuildInfo info, dynamic options)
        {
            var atlas = packer.GetAtlas(info);
            if (atlas == null)
                return;

            List<TextureInfo> textureInfoList = options.TextureInfoList;
            List<MeshCombiner.CombineInfo> combineInfos = new List<MeshCombiner.CombineInfo>();
            var hlodWorldToLocal = rootTransform.worldToLocalMatrix;

            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                var obj = info.WorkingObjects[i]; 
                ConvertMesh(obj.Mesh, obj.Materials, atlas, textureInfoList[0].InputName);

                for (int si = 0; si < obj.Mesh.subMeshCount; ++si)
                {
                    var ci = new MeshCombiner.CombineInfo();
                    var colliderLocalToWorld = obj.LocalToWorld;
                    var matrix = hlodWorldToLocal * colliderLocalToWorld;
                    
                    ci.Mesh = obj.Mesh;
                    ci.MeshIndex = si;
                    
                    ci.Transform = matrix;

                    if (ci.Mesh == null)
                        continue;
                    
                    combineInfos.Add(ci);
                }
            }
            
            MeshCombiner combiner = new MeshCombiner();
            WorkingMesh combinedMesh = combiner.CombineMesh(Allocator.Persistent, combineInfos);

            WorkingObject newObj = new WorkingObject(Allocator.Persistent);
            WorkingMaterial newMat = m_createdMaterials[atlas].Clone();

            combinedMesh.name = info.Name + "_Mesh";
            newObj.Name = info.Name;
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


     
        static private WorkingTexture CreateEmptyTexture(int width, int height, Color color, bool linear)
        {
            WorkingTexture texture = new WorkingTexture(Allocator.Persistent, TextureFormat.RGB24, width, height, linear);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            return texture;
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
        public static void OnGUI(HLOD hlod, bool isFirst)
        {
            if (isFirst )
            {
                inputTexturePropertyNames = null;
                outputTexturePropertyNames = null;
            }

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

            if (batcherOptions.EnableTintColor == null)
                batcherOptions.EnableTintColor = false;
            if (batcherOptions.TintColorName == null)
                batcherOptions.TintColorName = "";

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
            if( mat == null)
                mat = new Material(Shader.Find("Standard"));
            
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
                outputTexturePropertyNames = mat.GetTexturePropertyNames();
            }
            
            //apply tint color
            batcherOptions.EnableTintColor =
                EditorGUILayout.Toggle("Enable tint color", batcherOptions.EnableTintColor);
            if (batcherOptions.EnableTintColor == true)
            {
                EditorGUI.indentLevel += 1;
                
                var shader = mat.shader;
                List<string> colorPropertyNames = new List<string>();
                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    string name = ShaderUtil.GetPropertyName(shader, i);
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.Color)
                    {
                        colorPropertyNames.Add(name);
                    }
                }

                int index = colorPropertyNames.IndexOf(batcherOptions.TintColorName);
                index = EditorGUILayout.Popup("Tint color property", index, colorPropertyNames.ToArray());
                if (index >= 0)
                {
                    batcherOptions.TintColorName = colorPropertyNames[index];
                }
                else
                {
                    batcherOptions.TintColorName = "";
                }
                
                EditorGUI.indentLevel -= 1;
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
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Add new texture property") == true)
            {
                batcherOptions.TextureInfoList.Add(new TextureInfo());
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Update texture properties"))
            {
                //TODO: Need update automatically
                inputTexturePropertyNames = null;
                outputTexturePropertyNames = null;
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
