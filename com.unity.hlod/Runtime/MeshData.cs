using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.HLODSystem
{
    [PreferBinarySerialization]
    public class MeshData : ScriptableObject
    {
        [Serializable]
        public struct TextureCompressionData
        {
            [SerializeField] public TextureFormat PCTextureFormat;
            [SerializeField] public TextureFormat WebGLTextureFormat;
            [SerializeField] public TextureFormat AndroidTextureFormat;
            [SerializeField] public TextureFormat IOSTextureFormat;
            [SerializeField] public TextureFormat TVOSTextureFormat;
        }
        [Serializable]
        public struct TextureData
        {
            [SerializeField] public string Name;
            [SerializeField] public GraphicsFormat Format;
            [SerializeField] public TextureWrapMode WrapMode;
            [SerializeField] public int Width;
            [SerializeField] public int Height;
            [SerializeField] public byte[] Bytes;
        }

        [Serializable]
        public struct MaterialData
        {
            [SerializeField] public Material Material;
            [SerializeField] public List<TextureData> Textures;
        }

        [SerializeField] private Mesh m_mesh;
        [SerializeField] private TextureCompressionData m_compressionData;
        [SerializeField] private List<MaterialData> m_materialDatas = new List<MaterialData>();

        public Mesh Mesh
        {
            set { m_mesh = value; }
            get { return m_mesh; }
        }

        public TextureCompressionData CompressionData
        {
            set { m_compressionData = value; }
            get { return m_compressionData; }
        }

        public void AddMaterialData(MaterialData data)
        {
            m_materialDatas.Add(data);
        }

        public MaterialData GetMaterialData(int index)
        {
            return m_materialDatas[index];
        }

        public int GetMaterialDataCount()
        {
            return m_materialDatas.Count;
        }

    }
}