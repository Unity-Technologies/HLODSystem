using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.AssetImporters;
using UnityEngine;
using TextureCompressionQuality = UnityEditor.TextureCompressionQuality;
using UnityEditor.Experimental;

namespace Unity.HLODSystem
{
    [ScriptedImporter(version: 1, ext: "hlod", AllowCaching = true)]
    public class HLODDataImporter : ScriptedImporter
    {
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            ctx.DependsOnCustomDependency("HLODSystemPlatform");
            
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
                    int maxProgress = 0;

                    if (data.GetMaterials() != null)
                        maxProgress += data.GetMaterials().Count;
                    if (data.GetObjects() != null)
                        maxProgress += data.GetObjects().Count;
                    if ( data.GetColliders() != null )
                        maxProgress += data.GetColliders().Count;

                    rootData.name = "Root";

                    var serializableMaterials = data.GetMaterials();
                    var loadedMaterials = new Dictionary<string, Material>();
                    if (serializableMaterials != null)
                    {
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
                                Texture2D texture = st.To();
                                EditorUtility.CompressTexture(texture, compressFormat,
                                    TextureCompressionQuality.Normal);

                                mat.SetTexture(st.Name, texture);
                                ctx.AddObjectToAsset(texture.name, texture);
                            }

                            mat.EnableKeyword("_NORMALMAP");
                        }
                    }

                    var serializableObjects = data.GetObjects();
                    var serializableColliders = data.GetColliders();
                    Dictionary<string, List<GameObject>>
                        createdGameObjects = new Dictionary<string, List<GameObject>>();
                    Dictionary<string, GameObject> createdColliders = new Dictionary<string, GameObject>();

                    if (serializableObjects != null)
                    {
                        for (int oi = 0; oi < serializableObjects.Count; ++oi)
                        {
                            UpdateProgress(ctx.assetPath, currentProgress++, maxProgress);

                            var so = serializableObjects[oi];
                            GameObject go = new GameObject();
                            go.name = so.Name;

                            MeshFilter mf = go.AddComponent<MeshFilter>();
                            MeshRenderer mr = go.AddComponent<MeshRenderer>();
                            List<string> materialIds = so.GetMaterialIds();
                            List<string> materialNames = so.GetMaterialNames();
                            List<Material> materials = new List<Material>();

                            for (int mi = 0; mi < materialIds.Count; ++mi)
                            {
                                string id = materialIds[mi];
                                if (loadedMaterials.ContainsKey(id))
                                {
                                    materials.Add(loadedMaterials[id]);
                                }
                                else 
                                {
                                    string path = AssetDatabase.GUIDToAssetPath(id);
                                    if (string.IsNullOrEmpty(path) == false)
                                    {
                                        
                                        var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                                        var material = Utils.GUIDUtils.FindObject<Material>(allAssets, materialNames[mi]);
                                        
                                        materials.Add(material);
                                    }
                                    else
                                    {
                                        materials.Add(null);
                                    }
                                }
                            }

                            Mesh mesh = so.GetMesh().To();
                            mf.sharedMesh = mesh;
                            mr.sharedMaterials = materials.ToArray();
                            mr.lightProbeUsage = so.LightProbeUsage;

                            ctx.AddObjectToAsset(mesh.name, mesh);

                            if (createdGameObjects.ContainsKey(go.name) == false)
                                createdGameObjects.Add(go.name, new List<GameObject>());

                            createdGameObjects[go.name].Add(go);
                        }
                    }

                    if (serializableColliders != null)
                    {
                        for (int ci = 0; ci < serializableColliders.Count; ++ci)
                        {
                            UpdateProgress(ctx.assetPath, currentProgress++, maxProgress);

                            var sc = serializableColliders[ci];
                            GameObject go;

                            if (createdColliders.ContainsKey(sc.Name) == false)
                            {
                                createdColliders[sc.Name] = new GameObject("Collider");
                            }

                            go = createdColliders[sc.Name];

                            var collider = sc.CreateGameObject();
                            if (collider != null)
                            {
                                collider.name = "Collider" + ci;
                                collider.transform.SetParent(go.transform, true);
                            }
                        }
                    }

                    foreach (var objects in createdGameObjects.Values)
                    {
                        GameObject root;
                        if (objects.Count > 1)
                        {
                            root = new GameObject();
                            root.name = objects[0].name;
                            for (int i = 0; i < objects.Count; ++i)
                            {
                                objects[i].name = objects[i].name + "_" + i;
                                objects[i].transform.SetParent(root.transform, true);
                            }
                        }
                        else
                        {
                            root = objects[0];
                        }

                        if (createdColliders.ContainsKey(root.name))
                        {
                            createdColliders[root.name].transform.SetParent(root.transform, true);
                        }
                        
                        rootData.SetRootObject(root.name, root);
                        ctx.AddObjectToAsset(root.name, root);
                    }

                    ctx.AddObjectToAsset("Root", rootData);
                    ctx.SetMainObject(rootData);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
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
            if (group == BuildTargetGroup.WebGL)
                return data.CompressionData.WebGLTextureFormat;
            return data.CompressionData.PCTextureFormat;
        }

        private void UpdateProgress(string filename, int current, int max)
        {
            float pos = (float) current / (float) max;
            EditorUtility.DisplayProgressBar("Importing", "Importing " + filename, pos);
        }
    }

    [InitializeOnLoad]
    public class HLODSystemStartUp : IActiveBuildTargetChanged
    {
        public int callbackOrder { get; }
        static HLODSystemStartUp()
        {
            UpdateBuildTaget(EditorUserBuildSettings.activeBuildTarget);
        }
        static void UpdateBuildTaget(BuildTarget target)
        {
            var hash = Hash128.Compute(target.ToString());
            AssetDatabase.RegisterCustomDependency("HLODSystemPlatform", hash);
        }
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            UpdateBuildTaget(newTarget);
        }
    }
}