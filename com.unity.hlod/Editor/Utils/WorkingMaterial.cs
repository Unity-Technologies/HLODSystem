using System;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Utils
{
    public static class MaterialExtension
    {
        public static WorkingMaterial ToWorkingMaterial(this Material mat, Allocator allocator)
        {
            WorkingMaterial wm = new WorkingMaterial(allocator);
            wm.FromMaterial(mat);
            return wm;

        }
    }
    public class WorkingMaterial : IDisposable
    {
        private Allocator m_allocator;
        private int m_instanceID;
        private DisposableDictionary<string, WorkingTexture> m_textures;

        public int InstanceID
        {
            get { return m_instanceID; }
        }

        public WorkingMaterial(Allocator allocator)
        {
            m_allocator = allocator;
            m_instanceID = 0;
            m_textures = new DisposableDictionary<string, WorkingTexture>();
        }
        public WorkingMaterial(Allocator allocator, int materialId) : this(allocator)
        {
            m_instanceID = materialId;
        }

        public WorkingMaterial Clone()
        {
            WorkingMaterial nwm = new WorkingMaterial(m_allocator);

            nwm.m_instanceID = m_instanceID;
            nwm.m_textures = new DisposableDictionary<string, WorkingTexture>();

            foreach (var pair in m_textures)
            {
                nwm.m_textures.Add(pair.Key, pair.Value.Clone());
            }

            return nwm;
        }

        public bool NeedWrite()
        {
            string path = AssetDatabase.GetAssetPath(m_instanceID);
            return string.IsNullOrEmpty(path) == false;
        }

        public void AddTexture(string name, WorkingTexture texture)
        {
            lock (m_textures)
            {
                m_textures.Add(name, texture);
            }
        }

        public string[] GetTextureNames()
        {
            lock (m_textures)
            {
                return m_textures.Keys.ToArray();
            }
        }
        public WorkingTexture GetTexture(string name)
        {
            lock (m_textures)
            {
                WorkingTexture ret;
                if (m_textures.TryGetValue(name, out ret))
                    return ret;

                return null;
            }
        }
        public void FromMaterial(Material mat)
        {
            m_instanceID = mat.GetInstanceID();
            m_textures.Dispose();
            m_textures = new DisposableDictionary<string, WorkingTexture>();
                
            string[] names = mat.GetTexturePropertyNames();
            for (int i = 0; i < names.Length; ++i)
            {
                Texture2D texture = mat.GetTexture(names[i]) as Texture2D;
                if (texture == null)
                    continue;
                    
                m_textures.Add(names[i], texture.ToWorkingTexture(m_allocator));
            }
        }

        public Material ToMaterial()
        {
            Material mat = EditorUtility.InstanceIDToObject(m_instanceID) as Material;
            
            if (mat == null)
            {
                return new Material(Shader.Find("Standard"));
            }

            return mat;
        }

        public void Dispose()
        {
            m_textures.Dispose();
        }
    }
}