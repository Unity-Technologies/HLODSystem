using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class TerrainHLODData : ScriptableObject
    {
        [Serializable]
        public struct TextureData
        {
            [SerializeField] private string m_name;
            [SerializeField] private byte[] m_binary;

            [SerializeField] private uint m_type;
            [SerializeField] private uint m_wrapType;
        }
        [Serializable]
        public struct MaterialData
        {
            [SerializeField]
            private Material m_material;
            [SerializeField]
            private List<TextureData> m_textures;

        }
        [SerializeField] private Mesh m_mesh;
        [SerializeField] private List<MaterialData> m_materials;
    }
}