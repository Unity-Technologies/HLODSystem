using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using TextureCompressionQuality = UnityEditor.TextureCompressionQuality;
using Unity.HLODSystem.CustomUnityCacheClient;

namespace Unity.HLODSystem
{
    [ScriptedImporter(1, "hlod")]
    public class HLODDataImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            List<byte[]> compressedTextures = new List<byte[]>();
            int textureIndex = 0;

            //Check if the Asset exists in the Cache Server.
            bool isTextureCached = CustomCacheClient.GetInstance().GetCachedTextures(
                                       ctx.assetPath,
                                       ctx.selectedBuildTarget,
                                       out compressedTextures) == DownloadResult.Success;

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(ctx.selectedBuildTarget);
            try
            {
                UpdateProgress(ctx.assetPath, 0, 1);
                using (Stream stream = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read))
                {
                    HLODData data = HLODDataSerializer.Read(stream);
                    RootData rootData = RootData.CreateInstance<RootData>();
                    TextureFormat compressFormat = GetCompressFormat(data, buildTargetGroup);

                    int currentProgress = 0;
                    int maxProgress = data.GetMaterials().Count + data.GetObjects().Count;

                    rootData.name = "Root";

                    var serializableMaterials = data.GetMaterials();
                    var loadedMaterials = new Dictionary<string, Material>();
                    for (int mi = 0; mi < serializableMaterials.Count; ++mi)
                    {
                        UpdateProgress(ctx.assetPath, currentProgress++, maxProgress);
                        var sm = serializableMaterials[mi];

                        if (loadedMaterials.ContainsKey(sm.ID))
                            continue;

                        Material mat = sm.To();
                        loadedMaterials.Add(sm.ID, mat);

                        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)) == false)
                            continue;

                        ctx.AddObjectToAsset(mat.name, mat);

                        for (int ti = 0; ti < sm.GetTextureCount(); ++ti)
                        {
                            HLODData.SerializableTexture st = sm.GetTexture(ti);
                            Texture2D texture;

                            if (isTextureCached && textureIndex < compressedTextures.Count)
                            {
                                //Compressed Texture is cached. Load it into the texture
                                var srgb = GraphicsFormatUtility.IsSRGBFormat(st.GraphicsFormat);
                                texture = new Texture2D(st.Width, st.Height, compressFormat, true, !srgb)
                                {
                                    name = st.Name, wrapMode = st.WrapMode
                                };

                                byte[] image = compressedTextures[textureIndex++];
                                texture.LoadRawTextureData(image);
                                texture.Apply();
                            }
                            else
                            {
                                //Compressed Texture is not cached. Compress it and put it to the
                                //list of Compressed Textures to be put into the Cache Server
                                texture = st.To();
                                EditorUtility.CompressTexture(texture, compressFormat,
                                    TextureCompressionQuality.Normal);

                                byte[] compTexture = texture.GetRawTextureData();
                                compressedTextures.Add(compTexture);
                            }

                            mat.SetTexture(st.Name, texture);
                            ctx.AddObjectToAsset(texture.name, texture);
                        }

                        mat.EnableKeyword("_NORMALMAP");
                    }

                    var serializableObjects = data.GetObjects();
                    Dictionary<string, List<GameObject>>
                        createdGameObjects = new Dictionary<string, List<GameObject>>();

                    for (int oi = 0; oi < serializableObjects.Count; ++oi)
                    {
                        UpdateProgress(ctx.assetPath, currentProgress++, maxProgress);

                        var so = serializableObjects[oi];
                        GameObject go = new GameObject();
                        go.name = so.Name;

                        MeshFilter mf = go.AddComponent<MeshFilter>();
                        MeshRenderer mr = go.AddComponent<MeshRenderer>();
                        List<string> materialIds = so.GetMaterialIds();
                        List<Material> materials = new List<Material>();

                        for (int mi = 0; mi < materialIds.Count; ++mi)
                        {
                            string id = materialIds[mi];
                            if (loadedMaterials.ContainsKey(id))
                            {
                                materials.Add(loadedMaterials[id]);
                            }
                        }

                        Mesh mesh = so.GetMesh();
                        mf.sharedMesh = mesh;
                        mr.sharedMaterials = materials.ToArray();

                        ctx.AddObjectToAsset(mesh.name, mesh);

                        if (createdGameObjects.ContainsKey(go.name) == false)
                            createdGameObjects.Add(go.name, new List<GameObject>());

                        createdGameObjects[go.name].Add(go);
                    }

                    foreach (var objects in createdGameObjects.Values)
                    {
                        if (objects.Count > 1)
                        {
                            GameObject root = new GameObject();
                            root.name = objects[0].name;
                            for (int i = 0; i < objects.Count; ++i)
                            {
                                objects[i].name = objects[i].name + "_" + i;
                                objects[i].transform.SetParent(root.transform, true);
                            }

                            rootData.SetRootObject(root.name, root);
                            ctx.AddObjectToAsset(root.name, root);
                        }
                        else
                        {
                            rootData.SetRootObject(objects[0].name, objects[0]);
                            ctx.AddObjectToAsset(objects[0].name, objects[0]);
                        }
                    }

                    ctx.AddObjectToAsset("Root", rootData);
                    ctx.SetMainObject(rootData);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!isTextureCached && compressedTextures.Count > 0)
            {
                //Upload the Compressed Textures to the Cache Server
                if (CustomCacheClient.GetInstance().PutCachedTextures(
                        ctx.assetPath,
                        compressedTextures,
                        ctx.selectedBuildTarget) == UploadResult.Failure)
                    Debug.LogError("Caching Failed for " + ctx.assetPath);
            }
        }

        private TextureFormat GetCompressFormat(HLODData data, BuildTargetGroup group)
        {
            if (group == BuildTargetGroup.Android)
                return data.CompressionData.AndroidTextureFormat;
            if (group == BuildTargetGroup.iOS)
                return data.CompressionData.iOSTextureFormat;
            if (group == BuildTargetGroup.tvOS)
                return data.CompressionData.tvOSTextureFormat;
            if (group == BuildTargetGroup.Facebook || group == BuildTargetGroup.WebGL)
                return data.CompressionData.WebGLTextureFormat;
            return data.CompressionData.PCTextureFormat;
        }

        private void UpdateProgress(string filename, int current, int max)
        {
            float pos = (float) current / (float) max;
            EditorUtility.DisplayProgressBar("Importing", "Importing " + filename, pos);
        }
    }

    public class HLODDataRemimporter : IActiveBuildTargetChanged
    {
        public int callbackOrder { get; }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            string[] guids = AssetDatabase.FindAssets("t:RootData");
            for (int i = 0; i < guids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AssetDatabase.ImportAsset(path);
            }
        }
    }
}