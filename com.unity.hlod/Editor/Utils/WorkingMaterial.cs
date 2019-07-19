using System;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

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
        private Guid m_materialGuid;
        private DisposableDictionary<string, WorkingTexture> m_textures;
        private bool m_needWrite;

        public Guid GUID
        {
            set { m_materialGuid = value;}
            get { return m_materialGuid; }
        }

        public WorkingMaterial(Allocator allocator)
        {
            m_allocator = allocator;
            m_materialGuid = Guid.Empty;
            m_textures = new DisposableDictionary<string, WorkingTexture>();
            m_needWrite = false;
        }
        public WorkingMaterial(Allocator allocator, Guid sourceMaterialGuid) : this(allocator)
        {
            m_materialGuid = sourceMaterialGuid;
            m_needWrite = true;
        }

        public WorkingMaterial Clone()
        {
            WorkingMaterial nwm = new WorkingMaterial(m_allocator);

            nwm.m_materialGuid = m_materialGuid;
            nwm.m_textures = new DisposableDictionary<string, WorkingTexture>();

            foreach (var pair in m_textures)
            {
                nwm.m_textures.Add(pair.Key, pair.Value.Clone());
            }

            return nwm;
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

        public bool NeedWrite()
        {
            return m_needWrite;
        }

        public void FromMaterial(Material mat)
        {
            string materialPath = AssetDatabase.GetAssetPath(mat);
            m_materialGuid = Guid.Parse(AssetDatabase.AssetPathToGUID(materialPath));
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
            string path = AssetDatabase.GUIDToAssetPath(m_materialGuid.ToString("N"));
            return AssetDatabase.LoadAssetAtPath<Material>(path);
            
        }

        public void Dispose()
        {
            m_textures.Dispose();
        }
    }
}