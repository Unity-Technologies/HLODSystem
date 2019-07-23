using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.HLODSystem
{
    [ExecuteAlways]
    public class MeshDataRenderer : MonoBehaviour
    {
        [SerializeField]
        private MeshData m_data;

        public MeshData Data
        {
            set
            {
                m_data = value;
                UpdateMesh();
            }
            get => m_data;
        }

        private void Start()
        {
            UpdateMesh();
        }

        public void UpdateMesh()
        {
            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
            if (mf != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(mf);
#else
                Destroy(mf);
#endif
            }

            if (mr != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(mr);
#else
                Destroy(mr);
#endif
            }

            if (m_data == null)
                return;

            mf = gameObject.AddComponent<MeshFilter>();
            mr = gameObject.AddComponent<MeshRenderer>();

            mf.hideFlags = HideFlags.HideInInspector;
            mr.hideFlags = HideFlags.HideInInspector;
            
            List<Material> materials = new List<Material>();
            for (int i = 0; i < m_data.GetMaterialDataCount(); ++i)
            {
                var materialData = m_data.GetMaterialData(i);
                Material mat = materialData.Material;

                for (int ti = 0; ti < materialData.Textures.Count; ++ti)
                {
                    var textureData = materialData.Textures[ti];
                    Texture2D texture = new Texture2D(
                        textureData.Width, 
                        textureData.Height, 
                        GraphicsFormatUtility.GetTextureFormat(textureData.Format),
                        false, 
                        !GraphicsFormatUtility.IsSRGBFormat(textureData.Format));
                    texture.LoadRawTextureData(textureData.Bytes);
                    texture.Apply();
                    
                    mat.SetTexture(textureData.Name, texture);
                }
                mat.EnableKeyword("_NORMALMAP");
                materials.Add(mat);
            }
            
            mf.sharedMesh = m_data.Mesh;
            mr.sharedMaterials= materials.ToArray();
        }
    }
}