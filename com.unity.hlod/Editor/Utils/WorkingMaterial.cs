using System;
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

        public Guid GUID
        {
            get { return m_materialGuid; }
        }

        public WorkingMaterial(Allocator allocator)
        {
            m_allocator = allocator;
            m_materialGuid = Guid.Empty;
            m_textures = new DisposableDictionary<string, WorkingTexture>();
        }
        public WorkingMaterial(Allocator allocator, Guid materialGuid) : this(allocator)
        {
            m_materialGuid = materialGuid;
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
        
        public WorkingTexture GetTexture(string name)
        {
            WorkingTexture ret;
            if (m_textures.TryGetValue(name, out ret))
                return ret;

            return null;
        }

        public void FromMaterial(Material mat)
        {
            string materialPath = AssetDatabase.GetAssetPath(mat);
            m_materialGuid = Guid.Parse(AssetDatabase.AssetPathToGUID(materialPath));
            m_textures.Clear();
                
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