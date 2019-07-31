using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using TextureCompressionQuality = UnityEditor.TextureCompressionQuality;

namespace Unity.HLODSystem
{
    [ScriptedImporter(1, "hlod")]
    public class HLODDataImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(ctx.selectedBuildTarget);
            
            using (Stream stream = new FileStream(ctx.assetPath, FileMode.Open))
            {
                try
                {
                    EmptyData rootData = EmptyData.CreateInstance<EmptyData>();
                    ctx.AddObjectToAsset("Root", rootData);
                    ctx.SetMainObject(rootData); 
                    
                    while (stream.Position < stream.Length)
                    {
                        UpdateProgress(ctx.assetPath, stream);
                        HLODData data = HLODDataSerializer.Read(stream);
                        TextureFormat compressFormat = GetCompressFormat(data, buildTargetGroup);
                        GameObject go = new GameObject(data.Name);
                        
                        Mesh mesh = data.GetMesh();
                        mesh.name = data.Name + "_Mesh";
                        ctx.AddObjectToAsset(mesh.name, mesh);
                        
                        List<Material> materials = new List<Material>();

                        for (int mi = 0; mi < data.GetMaterialCount(); ++mi)
                        {
                            HLODData.SerializableMaterial sm = data.GetMaterial(mi);
                            Material mat = sm.To();
                            mat.name = data.Name + "_Mat";
                            ctx.AddObjectToAsset(mat.name, mat);

                            for (int ti = 0; ti < sm.GetTextureCount(); ++ti)
                            {
                                HLODData.SerializableTexture st = sm.GetTexture(ti);
                                Texture2D texture = st.To();
                                texture.name = data.Name + "_" + st.Name;
                                EditorUtility.CompressTexture(texture, compressFormat, TextureCompressionQuality.Normal );
                                
                                mat.SetTexture(st.Name, texture);
                                ctx.AddObjectToAsset(texture.name, texture);
                            }
                            mat.EnableKeyword("_NORMALMAP");
                            materials.Add(mat);
                        }

                        var mf = go.AddComponent<MeshFilter>();
                        var mr = go.AddComponent<MeshRenderer>();

                        mf.sharedMesh = mesh;
                        mr.sharedMaterials = materials.ToArray();
                        
                        ctx.AddObjectToAsset(go.name, go);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
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

        private void UpdateProgress(string filename, Stream stream)
        {
            float pos = (float)stream.Position / (float)stream.Length;
            EditorUtility.DisplayProgressBar("Importing", "Importing " + filename, pos);            
        }
    }

    public class HLODDataRemimporter : IActiveBuildTargetChanged
    {
        public int callbackOrder { get; }
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            string[] guids = AssetDatabase.FindAssets("t:EmptyData");
            for (int i = 0; i < guids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AssetDatabase.ImportAsset(path);
            }
        }
    }
}