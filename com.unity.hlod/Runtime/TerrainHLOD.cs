using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class TerrainHLOD : MonoBehaviour
    {
        private Type m_SimplifierType;
        private Type m_StreamingType;

        [SerializeField] private TerrainData m_TerrainData;
        [SerializeField] private float m_MinSize = 30.0f;
        [SerializeField] private float m_LODDistance = 0.3f;
        [SerializeField] private float m_CullDistance = 0.01f;
        [SerializeField] private SerializableDynamicObject m_SimplifierOptions = new SerializableDynamicObject();

        [SerializeField] private string m_materialGUID = "";
        [SerializeField] private int m_textureSize = 64;

        [SerializeField] private bool m_useNormal = false;
        [SerializeField] private bool m_useMask = false;

        [SerializeField] private string m_albedoPropertyName = "";
        [SerializeField] private string m_normalPropertyName = "";
        [SerializeField] private string m_maskPropertyName = "";
        //[SerializeField] private Material 
        
        public Type SimplifierType
        {
            set { m_SimplifierType = value; }
            get { return m_SimplifierType; }
        }

        public Type StreamingType
        {
            set { m_StreamingType = value; }
            get { return m_StreamingType; }
        }

        public TerrainData TerrainData
        {
            get { return m_TerrainData; }
        }
        public float MinSize
        {
            get { return m_MinSize; }
        }
        
        public SerializableDynamicObject SimplifierOptions
        {
            get { return m_SimplifierOptions; }
        }

        public int TextureSize
        {
            set { m_textureSize = value; }
            get { return m_textureSize; }
        }

        public string MaterialGUID
        {
            set { m_materialGUID = value; }
            get { return m_materialGUID; }
        }

        public bool UseNormal
        {
            set { m_useNormal = value; }
            get { return m_useNormal; }
        }

        public bool UseMask
        {
            set { m_useMask = value; }
            get { return m_useMask; }
        }

        public string AlbedoPropertyName
        {
            set { m_albedoPropertyName = value; }
            get { return m_albedoPropertyName; }
        }

        public string NormalPropertyName
        {
            set { m_normalPropertyName = value; }
            get { return m_normalPropertyName; }
        }

        public string MaskPropertyName
        {
            set { m_maskPropertyName = value; }
            get { return m_maskPropertyName; }
        }
    }
}