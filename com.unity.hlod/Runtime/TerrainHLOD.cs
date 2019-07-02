﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public class TerrainHLOD : MonoBehaviour, ISerializationCallbackReceiver, IGeneratedResourceManager
    {
        private Type m_SimplifierType;
        private Type m_StreamingType;

        [SerializeField] private string m_SimplifierTypeStr = "";
        [SerializeField] private string m_StreamingTypeStr = "";

        [SerializeField] private TerrainData m_TerrainData;
        [SerializeField] private float m_MinSize = 30.0f;
        [SerializeField] private float m_LODDistance = 0.3f;
        [SerializeField] private float m_CullDistance = 0.01f;
        [SerializeField] private SerializableDynamicObject m_SimplifierOptions = new SerializableDynamicObject();
        [SerializeField] private SerializableDynamicObject m_StreamingOptions = new SerializableDynamicObject();

        [SerializeField] private string m_materialGUID = "";
        [SerializeField] private int m_textureSize = 64;

        [SerializeField] private bool m_useNormal = false;
        [SerializeField] private bool m_useMask = false;

        [SerializeField] private string m_albedoPropertyName = "";
        [SerializeField] private string m_normalPropertyName = "";
        [SerializeField] private string m_maskPropertyName = "";
        
        [SerializeField]
        private List<Object> m_generatedObjects = new List<Object>();
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

        public float LODDistance
        {
            get { return m_LODDistance; }
        }

        public float CullDistance
        {
            get { return m_CullDistance; }
        }
        
        public SerializableDynamicObject SimplifierOptions
        {
            get { return m_SimplifierOptions; }
        }

        public SerializableDynamicObject StreamingOptions
        {
            get { return m_StreamingOptions; }
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
        
        public List<Object> GeneratedObjects
        {
            get { return m_generatedObjects; }
        }

        public void OnBeforeSerialize()
        {
            if (m_SimplifierType != null)
                m_SimplifierTypeStr = m_SimplifierType.AssemblyQualifiedName;
            if (m_StreamingType != null)
                m_StreamingTypeStr = m_StreamingType.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_SimplifierTypeStr))
            {
                m_SimplifierType = null;
            }
            else
            {
                m_SimplifierType = Type.GetType(m_SimplifierTypeStr);
            }

            if (string.IsNullOrEmpty(m_StreamingTypeStr))
            {
                m_StreamingType = null;
            }
            else
            {
                m_StreamingType = Type.GetType(m_StreamingTypeStr);
            }

        }

        public void AddGeneratedResource(Object obj)
        {
            m_generatedObjects.Add(obj);
        }
    }
}