using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class TexturePacker
    {
        public class TextureAtlas
        {
            public Texture2D[] PackedTexture;
            public MultipleTexture[] MultipleTextures;
            public Rect[] UVs;
        };

        public class MultipleTexture
        {
            public List<Texture2D> textureList = new List<Texture2D>();
        }

        class MultipleTextureComparer : IEqualityComparer<MultipleTexture>
        {
            public bool Equals(MultipleTexture x, MultipleTexture y)
            {
                if (x.textureList.Count != y.textureList.Count) return false;

                for (int i = 0; i < x.textureList.Count; ++i)
                {
                    if (x.textureList[i] != y.textureList[i])
                        return false;
                }

                return true;
            }

            public int GetHashCode(MultipleTexture obj)
            {
                string hashcode = "";
                for (int i = 0; i < obj.textureList.Count; ++i)
                {
                    hashcode += obj.textureList[i].GetHashCode();
                }

                return hashcode.GetHashCode();
            }
        }
        class Group
        {
            public object obj;
            public HashSet<MultipleTexture> multipleTextures;
        }
        class AtlasGroup
        {
            public List<object> Objects;
            public TextureAtlas Atlas;
        }
        class PackTexture
        {
            public List<object> Objects;
            public HashSet<MultipleTexture> MultipleTextures;
            public int PackableTextureCount;
        }
        class Score
        {
            public PackTexture Lhs;
            public PackTexture Rhs;
            public int Match;

            public static Score GetScore(PackTexture lhs, PackTexture rhs)
            {
                int match = lhs.MultipleTextures.Intersect(rhs.MultipleTextures).Count();
                return new Score()
                {
                    Lhs = lhs,
                    Rhs = rhs,
                    Match = match
                };
            }
        }

        List<Group> groups = new List<Group>();
        List<AtlasGroup> atlasGroups = new List<AtlasGroup>();

        public TexturePacker()
        {

        }

         
        public void AddTextureGroup(object obj, MultipleTexture[] textures)
        {
            Group group = new Group();
            group.obj = obj;
            group.multipleTextures = new HashSet<MultipleTexture>(textures, new MultipleTextureComparer());

            groups.Add(group);
        }

        public TextureAtlas GetAtlas(object obj)
        {
            foreach (var group in atlasGroups)
            {
                if (group.Objects.Contains(obj))
                    return group.Atlas;
            }
            return null;
        }

        public TextureAtlas[] GetAllAtlases()
        {
            return atlasGroups.Select(t => t.Atlas).ToArray();
        }

        public void Pack(int packTextureSize, int maxPieceSize)
        {
            //First, we should separate each group by count.
            Dictionary<int, List<Group>> groupCluster = new Dictionary<int, List<Group>>();

            for (int i = 0; i < groups.Count; ++i)
            {
                Group group = groups[i];
                int maximum = GetMaximumTextureCount(packTextureSize, maxPieceSize, group.multipleTextures.Count);
                if ( groupCluster.ContainsKey(maximum) == false )
                    groupCluster[maximum] = new List<Group>();

                groupCluster[maximum].Add(group);
            }

            //Second, we should figure out which group should be combined from each cluster.
            foreach (var cluster in groupCluster)
            {
                int maximum = cluster.Key;
                List<PackTexture> packTextures = new List<PackTexture>();

                foreach (var group in cluster.Value)
                {
                    packTextures.Add(new PackTexture()
                    {
                        Objects = new List<object>() {group.obj},
                        MultipleTextures = new HashSet<MultipleTexture>(group.multipleTextures, new MultipleTextureComparer()),
                        PackableTextureCount = maximum
                    });                    
                }

                List<Score> scoreList = new List<Score>();
                for (int i = 0; i < packTextures.Count; ++i)
                {
                    for (int j = i + 1; j < packTextures.Count; ++j)
                    {
                        scoreList.Add(Score.GetScore(packTextures[i], packTextures[j]));
                    }
                }

                scoreList.Sort((lhs, rhs) => rhs.Match - lhs.Match);

                for (int i = 0; i < scoreList.Count; ++i)
                {
                    HashSet<MultipleTexture> unionMultipleTextures = new HashSet<MultipleTexture>(scoreList[i].Lhs.MultipleTextures.Union(scoreList[i].Rhs.MultipleTextures), new MultipleTextureComparer());
                    if (unionMultipleTextures.Count <= maximum)
                    {
                        PackTexture lhs = scoreList[i].Lhs;
                        PackTexture rhs = scoreList[i].Rhs;

                        List<object> newObjects = new List<object>(scoreList[i].Lhs.Objects.Concat(scoreList[i].Rhs.Objects));

                        PackTexture newPackTexture = new PackTexture()
                        {
                            Objects = newObjects,
                            MultipleTextures = unionMultipleTextures,
                            PackableTextureCount = maximum
                        };

                        
                        packTextures.Remove(lhs);
                        packTextures.Remove(rhs);
                        packTextures.Add(newPackTexture);

                        //Remove merged Score and make Score by new Pack Texture.
                        scoreList.RemoveAll(score => score.Lhs == lhs || score.Lhs == rhs ||
                                                     score.Rhs == lhs || score.Rhs == rhs );

                        for (int j = 0; j < packTextures.Count - 1; ++j)
                        {
                            scoreList.Add(Score.GetScore(packTextures[j], newPackTexture));
                        }

                        scoreList.Sort((l, r) => r.Match - l.Match);

                        //for start first loop
                        i = -1;
                    }
                }

                foreach (var pack in packTextures)
                {
                    var atlas = MakeTextureAtlas(pack, packTextureSize);
                    atlasGroups.Add(new AtlasGroup()
                    {
                        Objects = pack.Objects,
                        Atlas = atlas
                    });
                }
                
                Debug.Log("Packing count : " + maximum + ", textures : " + packTextures.Count);
            }
        }
        private TextureAtlas MakeTextureAtlas(PackTexture packTexture, int packTextureSize)
        {
            TextureAtlas atlas = new TextureAtlas();
            //Texture2D packedTexture = new Texture2D(packTextureSize, packTextureSize, TextureFormat.RGBA32, false);

            int itemCount = (int) Math.Sqrt(packTexture.PackableTextureCount);
            int itemSize = packTextureSize / itemCount;

            int packedTextureCount = 0;
            int index = 0;

            foreach( var multipleTexture in packTexture.MultipleTextures)
            {
                packedTextureCount = Math.Max(packedTextureCount, multipleTexture.textureList.Count);
            }

            Texture2D[] packedTexture = new Texture2D[packedTextureCount];
            for (int i = 0; i < packedTextureCount; ++i)
            {
                packedTexture[i] = new Texture2D(packTextureSize, packTextureSize, TextureFormat.RGBA32, false);
                
            }

            atlas.UVs = new Rect[packTexture.MultipleTextures.Count];
            atlas.MultipleTextures = packTexture.MultipleTextures.ToArray();

            foreach (var texture in atlas.MultipleTextures)
            {
                int width = 0, height = 0;
                Color[] buffer = GetTextureColors(texture.textureList[0], itemSize, out width, out height);

                int col = index % itemCount;
                int row = index / itemCount;

                int x = col * itemSize;
                int y = row * itemSize;

                packedTexture[0].SetPixels(x, y, width, height, buffer);

                atlas.UVs[index] = new Rect(
                    (float)x / (float)packTextureSize,
                    (float)y / (float)packTextureSize,
                    (float)width / (float)packTextureSize,
                    (float)height / (float)packTextureSize);

                index += 1;

                for (int i = 1; i < texture.textureList.Count; ++i)
                {
                    int extWidth = 0, extHeight = 0;
                    Color[] extBuffer = GetTextureColors(texture.textureList[i], itemSize, out extWidth, out extHeight);

                    if (extWidth != width || extHeight != height)
                    {
                        Debug.Log("Resize texture: " + width + "_" + extWidth + ", " + height + "_" + extHeight);
                        extBuffer = ResizeImage(extBuffer, extWidth, extHeight, width, height);
                    }

                    packedTexture[i].SetPixels(x, y, width, height, extBuffer);
                }
            }

            for (int i = 0; i < packedTexture.Length; ++i)
            {
                packedTexture[i].Apply();
            }

            atlas.PackedTexture = packedTexture;
            return atlas;
        }

        
        private Color[] GetTextureColors(Texture2D texture, int maxItemSize, out int width, out int height)
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


                int sideSize = Math.Max(texture.width, texture.height);

                //if texture can put into an atlas by original size, go ahead.
                //Also, check mipmap is able to put into an atlas.
                for (int i = 0; i < texture.mipmapCount; ++i)
                {
                    if ((sideSize >> i) <= maxItemSize)
                    {
                        width = texture.width >> i;
                        height = texture.height >> i;

                        return texture.GetPixels(i);
                    }
                }

                //we should resize texture and return it buffers.
                float ratio = (float)texture.width / (float)texture.height;
                if (ratio > 1.0f)
                {
                    width = maxItemSize;
                    height = (int)(maxItemSize / ratio);
                }
                else
                {
                    width = (int)(maxItemSize / ratio);
                    height = maxItemSize;
                }

                Texture2D resizeTexture = new Texture2D(texture.width, texture.height, texture.format, false);
                Graphics.CopyTexture(texture, resizeTexture);
                resizeTexture.Resize(width, height);

                return resizeTexture.GetPixels();

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

        private static Color[] ResizeImage(Color[] originBuffer, int originWidth, int originHeight, int targetWidth,
            int targetHeight)
        {
            Color[] result = new Color[targetWidth * targetHeight];

            float widthRatio = (float)originWidth / (float)targetWidth;
            float heightRatio = (float)originHeight / (float)targetHeight;

            for (int y = 0; y < targetHeight; ++y)
            {
                for (int x = 0; x < targetWidth; ++x)
                {
                    int sourceX = (int)(x * widthRatio);
                    int sourceY = (int)(y * heightRatio);
                    result[y * targetWidth + x] = originBuffer[sourceY * originWidth + sourceX];
                }
            }

            return result;

        }

        private static int GetMaximumTextureCount(int packTextureSize, int maxPieceSize, int textureCount)
        {
            int minTextureCount = packTextureSize / maxPieceSize;
            //width * height
            minTextureCount = minTextureCount * minTextureCount;

            //we can't pack one texture.
            //so, we should use half size texture.
            while (minTextureCount < textureCount)
                minTextureCount = minTextureCount * 4;

            return minTextureCount;
        }

    }

}