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
            WorkingMaterial wm = new WorkingMaterial(allocator, mat);
            return wm;

        }
    }
    public class WorkingMaterial : IDisposable
    {
        private Allocator m_allocator;
        public string m_guid;
        private int m_instanceID;
        private bool m_copy;
        private DisposableDictionary<string, WorkingTexture> m_textures;
        
        public string Name { set; get; }

        public string Guid
        {
            get { return m_guid; }
        }
        public int InstanceID
        {
            get { return m_instanceID; }
        }

        private WorkingMaterial(Allocator allocator)
        {
            m_allocator = allocator;
            m_instanceID = 0;
            m_textures = new DisposableDictionary<string, WorkingTexture>();
            m_guid = System.Guid.NewGuid().ToString("N");
        }

        public WorkingMaterial(Allocator allocator, Material mat) : this(allocator)
        {
            Name = mat.name;
            m_instanceID = mat.GetInstanceID();
            m_copy = false;
            m_textures.Dispose();
            m_textures = new DisposableDictionary<string, WorkingTexture>();
            m_guid = System.Guid.NewGuid().ToString("N");
                
            string[] names = mat.GetTexturePropertyNames();
            for (int i = 0; i < names.Length; ++i)
            {
                Texture2D texture = mat.GetTexture(names[i]) as Texture2D;
                if (texture == null)
                    continue;
                    
                m_textures.Add(names[i], texture.ToWorkingTexture(m_allocator));
            }
        }
        public  WorkingMaterial(Allocator allocator, int materialId, bool copy) : this(allocator)
        {
            Material mat = EditorUtility.InstanceIDToObject(materialId) as Material;

            Name = mat.name;
            m_instanceID = materialId;
            m_copy = copy;
            m_guid = System.Guid.NewGuid().ToString("N");
        }

        public bool NeedWrite()
        {
            if (m_copy == true)
                return true;
            string path = AssetDatabase.GetAssetPath(m_instanceID);
            return string.IsNullOrEmpty(path);
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