using System.IO;

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
        
    }
}