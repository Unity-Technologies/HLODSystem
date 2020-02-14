using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class TexturePacker : IDisposable
    {
        private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
        public class MaterialTexture : IDisposable
        {
            private DisposableList<WorkingTexture> m_textures = new DisposableList<WorkingTexture>();
            private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);


            public void Add(WorkingTexture texture)
            {
                m_textures.Add(texture.Clone());
            }

            public MaterialTexture Clone()
            {
                MaterialTexture nmt = new MaterialTexture();
                for (int i = 0; i < m_textures.Count; ++i)
                {
                    nmt.Add(m_textures[i]);    
                }

                return nmt;
            }


            public int Count => m_textures.Count;

            public WorkingTexture this[int index]
            {
                get => m_textures[index];
                set => m_textures[index] = value;
            }

            public void Dispose()
            {
                m_textures.Dispose();
                m_textures = null;
                m_detector.Dispose();
            }
        }

        public class TextureAtlas : IDisposable
        {
            private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
            
            private List<object> m_objects;
            private List<Rect> m_uvs;
            private List<Guid> m_guids;
            private DisposableList<WorkingTexture> m_textures;

            public List<object> Objects => m_objects;
            public List<Rect> UVs => m_uvs;
            public DisposableList<WorkingTexture> Textures => m_textures;

            protected TextureAtlas(List<object> objs, List<Rect> uvs, List<Guid> guids)
            {
                m_objects = objs;
                m_uvs = uvs;
                m_guids = guids;
                m_textures = new DisposableList<WorkingTexture>();
            }
            public bool Contains(object obj)
            {
                return m_objects.Contains(obj);
            }

            public Rect GetUV(Guid textureGuid)
            {
                int index = m_guids.IndexOf(textureGuid);
                return m_uvs[index];
            }
            public void Dispose()
            {
                m_textures.Dispose();
                m_detector.Dispose();
            }
        }

        class TextureAtlasCreator : TextureAtlas
        {
            public TextureAtlasCreator(List<object> objs, List<Rect> uvs, List<Guid> guids, DisposableList<TextureCombiner> combiners)
                : base(objs, uvs, guids) 
            {
                for (int i = 0; i < combiners.Count; ++i)
                {
                    Textures.Add(combiners[i].GetTexture().Clone());
                }
            }
        }
        
        class Score
        {
            public Source Lhs;
            public Source Rhs;
            public int MatchCount;
        }

        class TextureCombiner : IDisposable
        {
            private WorkingTexture m_texture;
            private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
            public TextureCombiner(Allocator allocator, TextureFormat format, int width, int height, bool linear)
            {
                m_texture = new WorkingTexture(allocator, format, width, height, linear);
            }
            public void Dispose()
            {
                m_texture?.Dispose();
                m_detector.Dispose();
            }
            
            public void SetTexture(WorkingTexture source, int x, int y)
            {
                m_texture.Blit(source, x, y);
            }

            public WorkingTexture GetTexture()
            {
                return m_texture;
            }

            
        }

        class Source : IDisposable
        {
            private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
            
            public static Score GetScore(Source lhs, Source rhs)
            {
                int match = lhs.m_textureGuids.Intersect(rhs.m_textureGuids).Count();
                return new Score()
                {
                    Lhs = lhs,
                    Rhs = rhs,
                    MatchCount = match
                };
            }

            public static Source Combine(Source lhs, Source rhs)
            {
                Source newSource = new Source();
                newSource.m_obj = new List<object>();
                newSource.m_obj.AddRange(lhs.m_obj);
                newSource.m_obj.AddRange(rhs.m_obj);

                newSource.m_textures = new DisposableList<MaterialTexture>();
                newSource.m_textureGuids = new List<Guid>();

                for (int i = 0; i < lhs.m_textures.Count; ++i)
                {
                    newSource.m_textures.Add(lhs.m_textures[i].Clone());
                    newSource.m_textureGuids.Add(lhs.m_textureGuids[i]);
                }
                
                for (int i = 0; i < rhs.m_textures.Count; ++i)
                {
                    if (newSource.m_textureGuids.Contains(rhs.m_textureGuids[i]) == false)
                    {
                        newSource.m_textures.Add(rhs.m_textures[i].Clone());
                        newSource.m_textureGuids.Add(rhs.m_textureGuids[i]);
                    }
                }

                return newSource;
            }

            public static int CombineTextureCount(Source lhs, Source rhs)
            {
                int count = lhs.m_textureGuids.Count;
                for (int i = 0; i < rhs.m_textureGuids.Count; ++i)
                {
                    if (lhs.m_textureGuids.Contains(rhs.m_textureGuids[i]) == false)
                    {
                        count += 1;
                    }
                }

                return count;
            }

            private List<object> m_obj;
            private List<Guid> m_textureGuids;
            private DisposableList<MaterialTexture> m_textures;


            //use for combine.
            //this constructor should not call from out
            private Source()
            {
                
            }
            public Source(object obj, DisposableList<MaterialTexture> textures)
            {
                m_obj = new List<object>();
                m_obj.Add(obj);
                
                m_textures = textures;
                m_textureGuids = new List<Guid>(textures.Count);

                for (int i = 0; i < textures.Count; ++i)
                {
                    m_textureGuids.Add(textures[i][0].GetGUID());
                }
            }
            public void Dispose()
            {
                m_textures?.Dispose();
                m_detector.Dispose();
            }

            public Source Clone()
            {
                Source ns = new Source();
                ns.m_obj = new List<object>();
                ns.m_obj.AddRange(m_obj);
                ns.m_textureGuids = new List<Guid>();
                ns.m_textureGuids.AddRange(m_textureGuids);
                ns.m_textures = new DisposableList<MaterialTexture>();
                for (int i = 0; i < m_textures.Count; ++i)
                {
                    ns.m_textures.Add(m_textures[i].Clone());
                }
                return ns;
            }

            public int GetMaxTextureCount(int packTextureSize, int maxSourceSize)
            {
                int minTextureCount = packTextureSize / maxSourceSize;
                //width * height
                minTextureCount = minTextureCount * minTextureCount;

                //we can't pack one texture.
                //so, we should use half size texture.
                while (minTextureCount < m_textures.Count)
                    minTextureCount = minTextureCount * 4;

                return minTextureCount;
            }

            public TextureAtlas CreateAtlas(TextureFormat format, int packTextureSize, bool linear)
            {
                if (m_textures.Count == 0)
                {
                    return null;
                }
                
                int itemCount = Mathf.CeilToInt(Mathf.Sqrt(m_textures.Count));
                int itemSize = packTextureSize / itemCount;
                TextureAtlas atlas;

                using (DisposableList<MaterialTexture> resizedTextures = CreateResizedTextures(itemSize, itemSize))
                using (DisposableList<TextureCombiner> combiners = new DisposableList<TextureCombiner>())
                {
                    List<Rect> uvs = new List<Rect>(resizedTextures.Count);
                    List<Guid> guids = new List<Guid>(resizedTextures.Count);
                    for (int i = 0; i < resizedTextures.Count; ++i)
                    {
                        int x = i % itemCount;
                        int y = i / itemCount;

                        for (int k = combiners.Count; k < resizedTextures[i].Count; ++k)
                            combiners.Add(new TextureCombiner(Allocator.Persistent, format, packTextureSize,
                                packTextureSize, linear));

                        uvs.Add(new Rect(
                            (float) (x * itemSize)/ (float) packTextureSize,
                            (float) (y * itemSize)/ (float) packTextureSize,
                            (float) resizedTextures[i][0].Width / (float) packTextureSize,
                            (float) resizedTextures[i][0].Height / (float) packTextureSize));

                        guids.Add(m_textures[i][0].GetGUID());

                        for (int k = 0; k < resizedTextures[i].Count; ++k)
                        {
                            combiners[k].SetTexture(resizedTextures[i][k], x * itemSize, y * itemSize);
                        }
                    }

                    atlas = new TextureAtlasCreator(m_obj, uvs, guids, combiners);
                }

                return atlas;
            }

            private DisposableList<MaterialTexture> CreateResizedTextures(int newWidth, int newHeight)
            {
                DisposableList<MaterialTexture> resized = new DisposableList<MaterialTexture>();
                for (int i = 0; i < m_textures.Count; ++i)
                {
                    MaterialTexture newMT = new MaterialTexture();

                    for (int k = 0; k < m_textures[i].Count; ++k)
                    {
                        int targetWidth = Mathf.Min(newWidth, m_textures[i][k].Width);
                        int targetHeight = Mathf.Min(newHeight, m_textures[i][k].Height);
                        WorkingTexture resizedTexture =
                            m_textures[i][k].Resize(Allocator.Persistent, targetWidth, targetHeight); 
                        newMT.Add(resizedTexture);
                        resizedTexture.Dispose();
                    }
                    
                    resized.Add(newMT);
                }

                return resized;
            }
            
        }


        class TaskGroup : IDisposable
        {
            private TextureFormat m_format;
            private int m_packTextureSize;
            private int m_maxCount;
            private bool m_linear;
            private DisposableList<Source> m_sources = new DisposableList<Source>();

            public TaskGroup(TextureFormat format, int packTextureSize, bool linear, int maxCount)
            {
                m_format = format;
                m_packTextureSize = packTextureSize;
                m_maxCount = maxCount;
                m_linear = linear;
            }

            public void Dispose()
            {
                m_sources.Dispose();
            }

            public void AddSource(Source source)
            {
                m_sources.Add(source.Clone());
            }


            public void CombineSources()
            {
                List<Score> scoreList = new List<Score>();
                for (int i = 0; i < m_sources.Count; ++i)
                {
                    for (int k = i + 1; k < m_sources.Count; ++k)
                    {
                        scoreList.Add(Source.GetScore(m_sources[i], m_sources[k]));
                    }
                }
                
                scoreList.Sort((lhs, rhs) => rhs.MatchCount - lhs.MatchCount);

                for (int i = 0; i < scoreList.Count; ++i)
                {
                    Score score = scoreList[i];
                    if (CanCombine(score.Lhs, score.Rhs) == false)
                        continue;

                    Source combinedSource = Source.Combine(score.Lhs, score.Rhs);
                    //Do not dispose lhs, rhs sources
                    //these just moved so must not be released.
                    m_sources.Remove(score.Lhs);
                    m_sources.Remove(score.Rhs);
                    m_sources.Add(combinedSource);

                    //Remove merged Score and make Score by combinedScore.
                    scoreList.RemoveAll(s => s.Lhs == score.Lhs || s.Lhs == score.Rhs ||
                                             s.Rhs == score.Lhs || s.Rhs == score.Rhs);

                    //last is combinedSource.
                    for (int k = 0; k < m_sources.Count - 1; ++k)
                    {
                        scoreList.Add(Source.GetScore(m_sources[k], combinedSource));
                    }

                    scoreList.Sort((lhs, rhs) => rhs.MatchCount - lhs.MatchCount);
                    i = -1; //for back to the first loop..

                }
            }


            public DisposableList<TextureAtlas> CreateTextureAtlases()
            {
                DisposableList<TextureAtlas> atlases = new DisposableList<TextureAtlas>();

                for (int i = 0; i < m_sources.Count; ++i)
                {
                    TextureAtlas item = m_sources[i].CreateAtlas(m_format, m_packTextureSize, m_linear);
                    if ( item != null )
                        atlases.Add(item);
                }
                return atlases;

            }
            
            private bool CanCombine(Source lhs, Source rhs)
            {
                
                return Source.CombineTextureCount(lhs, rhs) <= m_maxCount;
            }
        }

        DisposableList<Source> m_sources = new DisposableList<Source>();
        DisposableList<TextureAtlas> m_atlas = new DisposableList<TextureAtlas>();

        public TexturePacker()
        {

        }
        
        public void Dispose()
        {
            m_sources.Dispose();
            m_atlas.Dispose();
            m_detector.Dispose();
        }

         
        //TODO: must clear what the ownership of texture.
        public void AddTextureGroup(object obj, List<MaterialTexture> textures)
        {
            DisposableList<MaterialTexture> copyTextures = new DisposableList<MaterialTexture>();
            for (int i = 0; i < textures.Count; ++i)
            {
                copyTextures.Add(textures[i].Clone());
            }
            Source source = new Source(obj, copyTextures);
            m_sources.Add(source);
        }
        
        public void Pack(TextureFormat format, int packTextureSize, int maxSourceSize, bool linear)
        {
            //First, we should separate each group by count.
            using (DisposableDictionary<int, TaskGroup> taskGroups = new DisposableDictionary<int, TaskGroup>())
            {
                for (int i = 0; i < m_sources.Count; ++i)
                {
                    int maxCount = m_sources[i].GetMaxTextureCount(packTextureSize, maxSourceSize);
                    if (taskGroups.ContainsKey(maxCount) == false)
                        taskGroups.Add(maxCount, new TaskGroup(format, packTextureSize, linear, maxCount));

                    taskGroups[maxCount].AddSource(m_sources[i]);
                }

                //Second, we should figure out which group should be combined from each taskGroup.
                foreach (var taskGroup in taskGroups.Values)
                {
                    taskGroup.CombineSources();
                    m_atlas.AddRange(taskGroup.CreateTextureAtlases());
                }
            }
        }

        public TextureAtlas GetAtlas(object obj)
        {
            for (int i = 0; i < m_atlas.Count; ++i)
            {
                if (m_atlas[i].Contains(obj))
                    return m_atlas[i];
            }
            return null;
        }

        public TextureAtlas[] GetAllAtlases()
        {
            return m_atlas.ToArray();
        }
        
    }

}