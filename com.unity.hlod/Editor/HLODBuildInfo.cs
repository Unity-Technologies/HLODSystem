using System;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEngine;

namespace Unity.HLODSystem
{
 
            //TODO: Separate Data and View
        public class Heightmap
        {
            private int m_width;
            private int m_height;
            private float[,] m_heights;

            private Vector3 m_size;
            private Vector3 m_scale;
            private Vector3 m_offset;

            public Vector3 Size => m_size;
            public Vector3 Offset => m_offset;

            public int Width => m_width;
            public int Height => m_height;
            
            

            public float this[int z, int x]
            {
                get { return m_heights[z, x]; }
            }

            private Heightmap()
            {
            }

            public Heightmap(int width, int height, Vector3 size, float[,] heights)
            {
                m_width = width;
                m_height = height;
                m_heights = heights;

                m_size = size;
                m_scale = new Vector3(size.x / (width - 1), size.y, size.z / (height - 1));
            }

            public Heightmap GetHeightmap(int beginX, int beginZ, int width, int height)
            {
                Heightmap heightmap = new Heightmap();
                heightmap.m_width = width;
                heightmap.m_height = height;

                heightmap.m_offset = new Vector3(beginX * m_scale.x, 0.0f, beginZ * m_scale.z);
                heightmap.m_size = new Vector3(m_scale.x * (width-1), m_size.y, m_scale.z * (height-1));
                heightmap.m_scale = m_scale;

                heightmap.m_heights = new float[height, width];

                for (int x = 0; x < width; ++x)
                {
                    for (int z = 0; z < height; ++z)
                    {
                        heightmap.m_heights[z, x] = m_heights[z + beginZ, x + beginX];
                    }
                }

                return heightmap;
            }

            public Vector3 GetInterpolatedNormal(float x, float y)
            {
                float fx = x * (m_width - 1);
                float fy = y * (m_height - 1);
                int lx = (int) fx;
                int ly = (int) fy;

                Vector3 n00 = CalculateNormalSobel(lx + 0, ly + 0);
                Vector3 n10 = CalculateNormalSobel(lx + 1, ly + 0);
                Vector3 n01 = CalculateNormalSobel(lx + 0, ly + 1);
                Vector3 n11 = CalculateNormalSobel(lx + 1, ly + 1);

                float u = fx - lx;
                float v = fy - ly;

                Vector3 s = Vector3.Lerp(n00, n10, u);
                Vector3 t = Vector3.Lerp(n01, n11, u);
                Vector3 value = Vector3.Lerp(s, t, v);

                value = Vector3.Normalize(value);
                return value;
            }


            Vector3 CalculateNormalSobel(int x, int y)
            {
                return CalculateNormalSobel(x, y, m_scale);
            }

            float CalculateHeight(int x, int y, float scale)
            {
                x = Mathf.Clamp(x, 0, m_width - 1);
                y = Mathf.Clamp(y, 0, m_height - 1);
                return m_heights[y, x] * scale;
            }


            Vector3 CalculateNormalSobel(int x, int y, Vector3 scale)
            {
                float dY, dX;

                // Do X sobel filter
                dX = CalculateHeight(x - 1, y - 1, scale.y) * -1.0F;
                dX += CalculateHeight(x - 1, y, scale.y) * -2.0F;
                dX += CalculateHeight(x - 1, y + 1, scale.y) * -1.0F;
                dX += CalculateHeight(x + 1, y - 1, scale.y) * 1.0F;
                dX += CalculateHeight(x + 1, y, scale.y) * 2.0F;
                dX += CalculateHeight(x + 1, y + 1, scale.y) * 1.0F;

                dX /= scale.x;

                // Do Y sobel filter
                dY = CalculateHeight(x - 1, y - 1, scale.y) * -1.0F;
                dY += CalculateHeight(x, y - 1, scale.y) * -2.0F;
                dY += CalculateHeight(x + 1, y - 1, scale.y) * -1.0F;
                dY += CalculateHeight(x - 1, y + 1, scale.y) * 1.0F;
                dY += CalculateHeight(x, y + 1, scale.y) * 2.0F;
                dY += CalculateHeight(x + 1, y + 1, scale.y) * 1.0F;
                dY /= scale.z;

                // Cross Product of components of gradient reduces to
                Vector3 normal = new Vector3(-dX, 8, -dY);
                return Vector3.Normalize(normal);
            }
        }
        
    public class HLODBuildInfo : IDisposable
    {
        public string Name = "";
        public int ParentIndex = -1;
        public SpaceNode Target;

        public DisposableList<WorkingObject> WorkingObjects = new DisposableList<WorkingObject>();
        public List<int> Distances = new List<int>();

        public Heightmap Heightmap;
        public HLODAsset ContainerObject; 

        public void Dispose()
        {
            WorkingObjects.Dispose();
        }
    }   
}