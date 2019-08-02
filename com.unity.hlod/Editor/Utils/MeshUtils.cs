using System.IO;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public static class MeshUtils
    {
        public static HLODData WorkingObjectToHLODData(WorkingObject wo, string name)
        {
            HLODData meshData = new HLODData();
            meshData.Name = name;
            meshData.SetMesh(wo.Mesh);

            for (int mi = 0; mi < wo.Materials.Count; ++mi)
            {
                WorkingMaterial wm = wo.Materials[mi];
                HLODData.SerializableMaterial materialData = new HLODData.SerializableMaterial();
                materialData.From(wm);
                
                string[] textureNames = wm.GetTextureNames();
                for (int ti = 0; ti < textureNames.Length; ++ti)
                {
                    WorkingTexture wt = wm.GetTexture(textureNames[ti]);
                    HLODData.SerializableTexture textureData = new HLODData.SerializableTexture();
                    textureData.From(wt.ToTexture());
                    textureData.Name = textureNames[ti];
                    
                    materialData.AddTexture(textureData);
                }
                
                meshData.AddMaterial(materialData);
            }

            return meshData;
        }

        public static void HLODBuildInfoToStream(HLODBuildInfo info, HLODData.TextureCompressionData compressionData,
            Stream stream)
        {
            for (int oi = 0; oi < info.WorkingObjects.Count; ++oi)
            {
                WorkingObject wo = info.WorkingObjects[oi];
                HLODData hlodData = WorkingObjectToHLODData(wo, info.Name);
                hlodData.CompressionData = compressionData;
                
                HLODDataSerializer.Write(stream, hlodData);
            }
        }

        public static void GameObjectToStream(GameObject gameObject, HLODData.TextureCompressionData compressionData,
            Stream stream)
        {
            var mf = gameObject.GetComponent<MeshFilter>();
            var mr = gameObject.GetComponent<MeshRenderer>();
            
            HLODData hlodData = new HLODData();
            hlodData.Name = gameObject.name;
            
            if ( mf != null )
                hlodData.SetMesh(mf.sharedMesh);
            if (mr != null)
            {
                for (int mi = 0; mi < mr.sharedMaterials.Length; ++mi)
                {
                    Material mat = mr.sharedMaterials[mi];
                    string[] textureNames = mat.GetTexturePropertyNames();
                    HLODData.SerializableMaterial sm = new HLODData.SerializableMaterial();
                    sm.From(mat);

                    for (int ti = 0; ti < textureNames.Length; ++ti)
                    {
                        Texture2D tex = mat.GetTexture(textureNames[ti]) as Texture2D;
                        if (tex == null)
                            continue;
                        HLODData.SerializableTexture st = new HLODData.SerializableTexture();
                        st.From(tex);
                        st.Name = textureNames[ti];
                        
                        sm.AddTexture(st);
                    }
                    
                    hlodData.AddMaterial(sm);
                }
            }

            hlodData.CompressionData = compressionData;
            
            HLODDataSerializer.Write(stream, hlodData);
        }
        
    }
}