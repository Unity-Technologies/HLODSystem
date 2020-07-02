using System;
using System.Collections.Generic;
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
        private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
        private WorkingMaterialBuffer m_buffer;


        public string Name
        {
            set { m_buffer.Name = value; }
            get { return m_buffer.Name; }
        }

        public string Guid
        {
            get { return m_buffer.Guid; }
        }
        public int InstanceID
        {
            get { return m_buffer.InstanceID; }
        }
        public string Identifier
        {
            get { return m_buffer.Identifier; }
        }



        private WorkingMaterial()
        {
            
        }
        public WorkingMaterial(Allocator allocator, Material mat)
        {
            m_buffer = WorkingMaterialBufferManager.Instance.Get(allocator, mat);
        }
        public WorkingMaterial(Allocator allocator, int materialId, string name) 
        {
            m_buffer = WorkingMaterialBufferManager.Instance.Create(allocator, materialId, name);
        }


        public WorkingMaterial Clone()
        {
             WorkingMaterial nwm = new WorkingMaterial();
             nwm.m_buffer = m_buffer;
             nwm.m_buffer.AddRef();

             return nwm;

        }

        public bool NeedWrite()
        {
            return m_buffer.NeedWrite();
        }

        public void AddTexture(string name, WorkingTexture texture)
        {
            m_buffer.AddTexture(name, texture);
        }

        public string[] GetTextureNames()
        {
            return m_buffer.GetTextureNames();
        }

        public void SetTexture(string name, WorkingTexture texture)
        {
            m_buffer.SetTexture(name, texture);
        }
        public WorkingTexture GetTexture(string name)
        {
            return m_buffer.GetTexture(name);
        }

        public Color GetColor(string name)
        {
            return m_buffer.GetColor(name);
        }

        public Material ToMaterial()
        {
           return m_buffer.ToMaterial();
        }

        public void Dispose()
        {
            m_buffer.Release();
            m_buffer = null;
            
            m_detector.Dispose();
        }
    }    
    
    
    public class WorkingMaterialBufferManager
    {
        private static WorkingMaterialBufferManager s_instance;
        public static WorkingMaterialBufferManager Instance
        {
            get
            {
                if ( s_instance == null )
                    s_instance = new WorkingMaterialBufferManager();

                return s_instance;
            }
        }


        private Dictionary<string, WorkingMaterialBuffer> m_cache = new Dictionary<string, WorkingMaterialBuffer>();
        public WorkingMaterialBuffer Get(Allocator allocator, Material material)
        {
            WorkingMaterialBuffer buffer = null;
            string guid = "";
            long localId = 0;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out guid, out localId) == false)
            {
                //issue guid for just create
                guid = System.Guid.NewGuid().ToString("N");
            }
            else
            {
                guid = guid + material.GetInstanceID();
            }
            
            
            if (m_cache.ContainsKey(guid) == false)
            {
                buffer = new WorkingMaterialBuffer(allocator, material);
                m_cache[buffer.Identifier] = buffer;
                guid = buffer.Identifier;
            }

            buffer = m_cache[guid];
            buffer.AddRef();
            return buffer;
        }

        public WorkingMaterialBuffer Create(Allocator allocator, int materialId, string name)
        {
            WorkingMaterialBuffer buffer = new WorkingMaterialBuffer(allocator, materialId, name);
            buffer.AddRef();
            m_cache[buffer.Identifier] = buffer;
            return buffer;
        }

        public void Destroy(WorkingMaterialBuffer buffer)
        {
            m_cache.Remove(buffer.Identifier);
        }
    }

    public class WorkingMaterialBuffer : IDisposable
    {
        private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
        
        private int m_refCount;
        
        private Allocator m_allocator;
        private string m_name;
        private string m_guid;
        private int m_instanceID;
        private DisposableDictionary<string, WorkingTexture> m_textures;
        private Dictionary<string, Color> m_colors;

        public string Name
        {
            get => m_name;
            set => m_name = value;
        }
        public string Guid => m_guid;
        public int InstanceID => m_instanceID;

        public string Identifier
        {
            get
            {
                return Guid + InstanceID;
            }
        }


        private WorkingMaterialBuffer(Allocator allocator)
        {
            m_allocator = allocator;
            m_instanceID = 0;
            m_textures = new DisposableDictionary<string, WorkingTexture>();
            m_colors = new Dictionary<string, Color>();
            m_guid = System.Guid.NewGuid().ToString("N");
        }
        public WorkingMaterialBuffer(Allocator allocator, Material mat) : this(allocator)
        {
            m_name = mat.name;
            m_instanceID = mat.GetInstanceID();
            m_textures.Dispose();
            m_textures = new DisposableDictionary<string, WorkingTexture>();
            m_colors = new Dictionary<string, Color>();
            string path = AssetDatabase.GetAssetPath(mat.GetInstanceID());
            m_guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(m_guid))
            {
                m_guid = System.Guid.NewGuid().ToString("N");
            }
                
            string[] names = mat.GetTexturePropertyNames();
            for (int i = 0; i < names.Length; ++i)
            {
                Texture2D texture = mat.GetTexture(names[i]) as Texture2D;
                if (texture == null)
                    continue;
                    
                m_textures.Add(names[i], texture.ToWorkingTexture(m_allocator));
            }

            var shader = mat.shader;
            if (shader != null)
            {
                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.Color)
                    {
                        string name = ShaderUtil.GetPropertyName(shader, i);
                        m_colors.Add(name, mat.GetColor(name));
                    }
                }
            }
        }
        public WorkingMaterialBuffer(Allocator allocator, int materialId, string name) : this(allocator)
        {
            m_name = name;
            m_instanceID = materialId;
            m_guid = System.Guid.NewGuid().ToString("N");
        }

        
        public void AddRef()
        {
            m_refCount += 1;
        }

        public void Release()
        {
            m_refCount -= 1;

            if (m_refCount == 0)
            {
                WorkingMaterialBufferManager.Instance.Destroy(this);
                Dispose();
            }
        }

        
        public void Dispose()
        {
            m_textures.Dispose();
            m_detector.Dispose();
        }

        public bool NeedWrite()
        {
            string path = AssetDatabase.GUIDToAssetPath(m_guid);
            return string.IsNullOrEmpty(path);
        }
        
        
        public void AddTexture(string name, WorkingTexture texture)
        {
            lock (m_textures)
            {
                m_textures.Add(name, texture);
                m_guid = System.Guid.NewGuid().ToString("N");
            }
        }

        public string[] GetTextureNames()
        {
            lock (m_textures)
            {
                return m_textures.Keys.ToArray();
            }
        }

        public void SetTexture(string name, WorkingTexture texture)
        {
            lock (m_textures)
            {
                if (m_textures.ContainsKey(name) == true && m_textures[name] != null )
                    m_textures[name].Dispose();
                        
                m_textures[name] = texture;
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

        public Color GetColor(string name)
        {
            lock (m_colors)
            {
                if (m_colors.ContainsKey(name) == false)
                    return Color.white;

                return m_colors[name];
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
    }
}