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
        public struct TextureData
        {
            [SerializeField] public string Name;
            [SerializeField] public GraphicsFormat Format;
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
        [SerializeField] private List<MaterialData> m_materialDatas = new List<MaterialData>();

        public Mesh Mesh
        {
            set { m_mesh = value; }
            get { return m_mesh; }
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