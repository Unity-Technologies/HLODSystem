using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{

    public static class TextureExtensions
    {
        public static WorkingTexture ToWorkingTexture(this Texture2D texture, Allocator allocator)
        {
            var wt = new WorkingTexture(allocator, texture);
           
            return wt;
            
        }
    }
    public class WorkingTexture : IDisposable
    {
        private WorkingTextureBuffer m_buffer;
        
        public WorkingTexture(Allocator allocator, int width, int height)
        {
            m_buffer = WorkingTextureBufferManager.Instance.Create(allocator, width, height);
        }

        public WorkingTexture(Allocator allocator, Texture2D source)
        {
            m_buffer = WorkingTextureBufferManager.Instance.Get(allocator, source);
        }

        public void SetPixel(int x, int y, Color color)
        {
            //int pos = y * m_width + x;

            //m_pixels[pos] = color;
        }

        public Color GetPixel(int x, int y)
        {
            return m_buffer.GetPixel(x, y);
        }
        

        public void Dispose()
        {
            m_buffer.Release();
        }
    }

    public class WorkingTextureBufferManager
    {
        private static WorkingTextureBufferManager s_instance;
        public static WorkingTextureBufferManager Instance
        {
            get
            {
                if ( s_instance == null )
                    s_instance = new WorkingTextureBufferManager();

                return s_instance;
            }
        }


        private Dictionary<Texture2D, WorkingTextureBuffer> m_cache = new Dictionary<Texture2D, WorkingTextureBuffer>();
        public WorkingTextureBuffer Get(Allocator allocator, Texture2D texture)
        {
            WorkingTextureBuffer buffer = null;
            if (m_cache.ContainsKey(texture) == true)
            {
                buffer = m_cache[texture];
                
            }
            else
            {
                buffer = new WorkingTextureBuffer(allocator, texture);
                m_cache.Add(texture, buffer);
            }
            buffer.AddRef();
            return buffer;
        }

        public WorkingTextureBuffer Create(Allocator allocator, int width, int height)
        {
            WorkingTextureBuffer buffer = new WorkingTextureBuffer(allocator, width, height);
            buffer.AddRef();
            return buffer;
        }

        public void Destroy(WorkingTextureBuffer buffer)
        {
            if (buffer.HasSource())
            {
                m_cache.Remove(buffer.GetSource());
            }
        }
    }
    
    public class WorkingTextureBuffer : IDisposable
    {
        private int m_width;
        private int m_height;
        
        private NativeArray<Color> m_pixels;

        private int m_refCount;
        private Texture2D m_source;
        public WorkingTextureBuffer(Allocator allocator, int width, int height)
        {
            m_width = width;
            m_height = height;
            m_pixels = new NativeArray<Color>( width * height, allocator);
            m_refCount = 0;
            m_source = null;
        }

        public WorkingTextureBuffer(Allocator allocator, Texture2D source) 
            : this(allocator, source.width, source.height)
        {
            m_source = source;
            CopyFrom(source);
        }

        public bool HasSource()
        {
            return m_source != null;
        }
        public Texture2D GetSource()
        {
            return m_source;
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
                WorkingTextureBufferManager.Instance.Destroy(this);
                Dispose();
            }
        }
        
        public void Dispose()
        {
            if( m_pixels.IsCreated )
                m_pixels.Dispose();
        }

        public Color GetPixel(int x, int y)
        {
            return m_pixels[y * m_width + x];
        }
        
        private void CopyFrom(Texture2D texture)
        {
            //make to texture readable.
            var assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            var textureImporter = assetImporter as TextureImporter;
            TextureImporterType type = TextureImporterType.Default;
            if (textureImporter)
            {
                type = textureImporter.textureType;
                textureImporter.isReadable = true;
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.SaveAndReimport();
            }

            try
            {
                int count = m_width * m_height;
                Color[] texturePixels = texture.GetPixels();
                if (texturePixels.Length != count)
                {
                    //TODO: logging
                    return;
                }

                m_pixels.Slice(0, count).CopyFrom(texturePixels);
            }
            finally
            {
                if (textureImporter)
                {
                    textureImporter.isReadable = false;
                    textureImporter.textureType = type;
                    textureImporter.SaveAndReimport();
                }
            }

        }

    }

}