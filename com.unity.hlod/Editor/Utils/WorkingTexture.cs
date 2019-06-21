using System;
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

        public int Width
        {
            get { return m_buffer.Widht; }
        }

        public int Height
        {
            get { return m_buffer.Height; }
        }
        private WorkingTexture()
        {
            
        }
        public WorkingTexture(Allocator allocator, int width, int height)
        {
            m_buffer = WorkingTextureBufferManager.Instance.Create(allocator, width, height);
        }

        public WorkingTexture(Allocator allocator, Texture2D source)
        {
            m_buffer = WorkingTextureBufferManager.Instance.Get(allocator, source);
        }

        public void Dispose()
        {
            m_buffer.Release();
        }

        public WorkingTexture Clone()
        {
            WorkingTexture nwt = new WorkingTexture();
            nwt.m_buffer = m_buffer;
            nwt.m_buffer.AddRef();

            return nwt;
        }

        public Texture2D ToTexture()
        {
            return m_buffer.ToTexture();
        }
        
        public Guid GetGUID()
        {
            return m_buffer.GetGUID(); 
                
        }

        public void SetPixel(int x, int y, Color color)
        {
            MakeWriteable();
            
            m_buffer.SetPixel(x, y, color);

        }
        //{
            //int pos = y * m_width + x;

            //m_pixels[pos] = color;
        //}

        public Color GetPixel(int x, int y)
        {
            return m_buffer.GetPixel(x, y);
        }

        public Color GetPixel(float u, float v)
        {
            float x = u * (Width - 1);
            float y = v * (Height - 1);
            
            int x1 = Mathf.FloorToInt(x);
            int x2 = Mathf.CeilToInt(x);

            int y1 = Mathf.FloorToInt(y);
            int y2 = Mathf.CeilToInt(y);

            float xWeight = x - x1;
            float yWeight = y - y1;

            Color c1 = Color.Lerp(GetPixel(x1, y1), GetPixel(x2, y1), xWeight);
            Color c2 = Color.Lerp(GetPixel(x1, y2), GetPixel(x2, y2), xWeight);

            return Color.Lerp(c1, c2, yWeight);
        }

        public void Blit(WorkingTexture source, int x, int y)
        {
            MakeWriteable();
           
            m_buffer.Blit(source.m_buffer, x, y);
        }
        

        
      

        public WorkingTexture Resize(Allocator allocator, int newWidth, int newHeight)
        {
            WorkingTexture wt = new WorkingTexture(allocator, newWidth, newHeight);

            float xWeight = (float) (m_buffer.Widht - 1) / (float) (newWidth - 1);
            float yWeight = (float) (m_buffer.Height - 1) / (float) (newHeight - 1);

            for (int y = 0; y < newHeight; ++y)
            {
                for (int x = 0; x < newWidth; ++x)
                {
                    float xpos = x * xWeight;
                    float ypos = y * yWeight;

                    float u = xpos / Width;
                    float v = ypos / Height;

                    wt.SetPixel(x, y, GetPixel(u, v));
                }
            }
            
            return wt;
        }

       

        private void MakeWriteable()
        {
            if (m_buffer.GetRefCount() > 1)
            {
                WorkingTextureBuffer newBuffer = WorkingTextureBufferManager.Instance.Clone(m_buffer);
                m_buffer.Release();
                m_buffer = newBuffer;
            }
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

        public WorkingTextureBuffer Clone(WorkingTextureBuffer buffer)
        {
            WorkingTextureBuffer nb = buffer.Clone();
            nb.AddRef();
            return nb;
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
        private Allocator m_allocator;
        private int m_width;
        private int m_height;
        
        private NativeArray<Color> m_pixels;

        private int m_refCount;
        private Texture2D m_source;

        private Guid m_guid;

        public int Widht => m_width;
        public int Height => m_height;

        public WorkingTextureBuffer(Allocator allocator, int width, int height)
        {
            m_allocator = allocator;
            m_width = width;
            m_height = height;
            m_pixels = new NativeArray<Color>( width * height, allocator);
            m_refCount = 0;
            m_source = null;
            m_guid = Guid.NewGuid();
        }

        public WorkingTextureBuffer(Allocator allocator, Texture2D source) 
            : this(allocator, source.width, source.height)
        {
            m_source = source;
            CopyFrom(source);
            m_guid = GUIDUtils.ObjectToGUID(source);
        }

        public WorkingTextureBuffer Clone()
        {
            WorkingTextureBuffer buffer = new WorkingTextureBuffer(m_allocator, m_width, m_height);
            buffer.Blit(this, 0, 0);
            return buffer;
        }
        public Texture2D ToTexture()
        {
            Texture2D texture = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false);
            texture.SetPixels(m_pixels.ToArray());
            texture.Apply();
            
            return texture;
        }
        public Guid GetGUID()
        {
            return m_guid;
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

        public int GetRefCount()
        {
            return m_refCount;
        }
        
        public void Dispose()
        {
            if( m_pixels.IsCreated )
                m_pixels.Dispose();
        }

        public void SetPixel(int x, int y, Color color)
        {
            m_pixels[y * m_width + x] = color;
        }

        public Color GetPixel(int x, int y)
        {
            return m_pixels[y * m_width + x];
        }

        public void Blit(WorkingTextureBuffer source, int x, int y)
        {
            int width = source.m_width;
            int height = source.m_height;

            for (int sy = 0; sy < height; ++sy)
            {
                int ty = y + sy;
                if ( ty < 0 || ty >= m_height )
                    continue;

                for (int sx = 0; sx < width; ++sx)
                {
                    int tx = x + sx;
                    if (tx < 0 || tx >= m_width)
                        continue;

                    SetPixel(tx, ty, source.GetPixel(sx, sy));
                }
            }
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