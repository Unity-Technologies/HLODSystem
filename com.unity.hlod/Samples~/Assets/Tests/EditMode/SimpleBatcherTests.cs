using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEditor.Sprites;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class SimpleBatcherTests 
    {

        GameObject m_mesh1material1;
        GameObject m_mesh2material1;
        GameObject m_mesh2material2;
        GameObject m_mesh3material2;

        GameObject m_mesh1material1T;
        GameObject m_mesh2material1T;
        GameObject m_mesh2material2T;
        GameObject m_mesh3material2T;

        GameObject m_material1T;
        GameObject m_material2T;
        GameObject m_material3T;
        GameObject m_material4T;


        [SetUp]
        public void Setup()
        {
            m_mesh1material1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD0_2.prefab");
            m_mesh2material1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1.prefab");
            m_mesh2material2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1_2.prefab");
            m_mesh3material2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD2.prefab");

            m_mesh1material1T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD0T_2.prefab");
            m_mesh2material1T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1T.prefab");
            m_mesh2material2T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1T_2.prefab");
            m_mesh3material2T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD2T.prefab");

            m_material1T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD0T.prefab");
            m_material2T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1T.prefab");
            m_material3T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD2T.prefab");
            m_material4T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD3T.prefab");

        }

        [TearDown]
        public void Cleanup()
        {

        }

        [Test]
        public void OneMaterialOneMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }
        [Test]
        public void OneTextureMaterialOneMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }

        [Test]
        public void OneMaterialTwoMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material1);
            obj3.transform.SetParent(hlodComponent.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1);
            obj4.transform.SetParent(hlodComponent.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);


            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }

        [Test]
        public void OneTextureMaterialTwoMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material1T);
            obj3.transform.SetParent(hlodComponent.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1T);
            obj4.transform.SetParent(hlodComponent.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }

        [Test]
        public void TwoMaterialTwoMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2);
            obj3.transform.SetParent(hlodComponent.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1);
            obj4.transform.SetParent(hlodComponent.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);


            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {                
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }

        [Test]
        public void TwoTextureMaterialTwoMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2T);
            obj3.transform.SetParent(hlodComponent.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1T);
            obj4.transform.SetParent(hlodComponent.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);


            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }

        [Test]
        public void TwoMaterialThreeMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();


            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2);
            obj3.transform.SetParent(hlodComponent.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1);
            obj4.transform.SetParent(hlodComponent.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);

            var obj5 = GameObject.Instantiate(m_mesh3material2);
            obj5.transform.SetParent(hlodComponent.transform);
            obj5.transform.position = new Vector3(0.0f, 0.0f, -1.0f);

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }

        [Test]
        public void TwoTextureMaterialThreeMesh()
        {
            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2T);
            obj3.transform.SetParent(hlodComponent.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1T);
            obj4.transform.SetParent(hlodComponent.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);

            var obj5 = GameObject.Instantiate(m_mesh3material2T);
            obj5.transform.SetParent(hlodComponent.transform);
            obj5.transform.position = new Vector3(0.0f, 0.0f, -1.0f);

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {                
                batcher.Batch(hlodGameObject.transform, buildInfo, null);

                Assert.AreEqual(buildInfo.Count, 1);
                Assert.AreEqual(buildInfo[0].WorkingObjects.Count, 1);
            }
        }

        [Test]
        public void PackingTextureTest()
        {
            MethodInfo packingTextureMethod = typeof(SimpleBatcher).GetMethod("PackingTexture", BindingFlags.Instance | BindingFlags.NonPublic);

            dynamic batcherOptions = CreateSimpleBatcherOptions();
            GameObject hlodGameObject = new GameObject();
            HLOD hlodComponent = hlodGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_material1T);
            obj1.transform.SetParent(hlodComponent.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_material2T);
            obj2.transform.SetParent(hlodComponent.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_material3T);
            obj3.transform.SetParent(hlodComponent.transform);
            obj3.transform.position = new Vector3(2.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_material4T);
            obj4.transform.SetParent(hlodComponent.transform);
            obj4.transform.position = new Vector3(3.0f, 0.0f, 0.0f);

            batcherOptions.PackTextureSize = 256;
            batcherOptions.LimitTextureSize = 128;

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (TexturePacker packer = new TexturePacker())
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                packingTextureMethod.Invoke(batcher, new object[] { packer, buildInfo, batcherOptions, null });

                Assert.AreEqual(packer.GetAllAtlases().Length, 1);

                var atlas = packer.GetAllAtlases()[0];
                Assert.AreEqual(atlas.Objects[0], buildInfo[0]);
                Assert.AreEqual(atlas.Textures.Count, 1);

                var texture = atlas.Textures[0];

                Assert.AreEqual(256, texture.Width);
                Assert.AreEqual(256, texture.Height);

                Assert.AreEqual(new Rect(0.0f, 0.0f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[0].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.5f, 0.0f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[1].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.0f, 0.5f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[2].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.5f, 0.5f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[3].Materials[0].GetTexture("_MainTex").GetGUID()));

                Assert.AreEqual(new Color(1.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(0, 0));
                Assert.AreEqual(new Color(0.0f, 0.0f, 1.0f, 1.0f), texture.GetPixel(255, 0));
                Assert.AreEqual(new Color(0.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(0, 255));
                Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f, 1.0f), texture.GetPixel(255, 255));

                Assert.AreEqual(new Color(1.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(127, 127));
                Assert.AreEqual(new Color(0.0f, 0.0f, 1.0f, 1.0f), texture.GetPixel(128, 127));
                Assert.AreEqual(new Color(0.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(127, 128));
                Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f, 1.0f), texture.GetPixel(128, 128));
            }

            batcherOptions.PackTextureSize = 128;
            batcherOptions.LimitTextureSize = 128;

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (TexturePacker packer = new TexturePacker())
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                packingTextureMethod.Invoke(batcher, new object[] { packer, buildInfo, batcherOptions, null });

                Assert.AreEqual(packer.GetAllAtlases().Length, 1);

                var atlas = packer.GetAllAtlases()[0];
                Assert.AreEqual(atlas.Objects[0], buildInfo[0]);
                Assert.AreEqual(atlas.Textures.Count, 1);

                var texture = atlas.Textures[0];

                Assert.AreEqual(128, texture.Width);
                Assert.AreEqual(128, texture.Height);

                Assert.AreEqual(new Rect(0.0f, 0.0f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[0].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.5f, 0.0f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[1].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.0f, 0.5f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[2].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.5f, 0.5f, 0.5f, 0.5f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[3].Materials[0].GetTexture("_MainTex").GetGUID()));

                Assert.AreEqual(new Color(1.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(0, 0));
                Assert.AreEqual(new Color(0.0f, 0.0f, 1.0f, 1.0f), texture.GetPixel(127, 0));
                Assert.AreEqual(new Color(0.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(0, 127));
                Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f, 1.0f), texture.GetPixel(127, 127));

                Assert.AreEqual(new Color(1.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(63, 63));
                Assert.AreEqual(new Color(0.0f, 0.0f, 1.0f, 1.0f), texture.GetPixel(64, 63));
                Assert.AreEqual(new Color(0.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(63, 64));
                Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f, 1.0f), texture.GetPixel(64, 64));

            }

            batcherOptions.PackTextureSize = 512;
            batcherOptions.LimitTextureSize = 128;

            using (var buildInfo = CreateBuildInfo(hlodComponent))
            using (TexturePacker packer = new TexturePacker())
            using (SimpleBatcher batcher = new SimpleBatcher(batcherOptions))
            {
                packingTextureMethod.Invoke(batcher, new object[] { packer, buildInfo, batcherOptions, null });

                Assert.AreEqual(packer.GetAllAtlases().Length, 1);

                var atlas = packer.GetAllAtlases()[0];
                Assert.AreEqual(atlas.Objects[0], buildInfo[0]);
                Assert.AreEqual(atlas.Textures.Count, 1);

                var texture = atlas.Textures[0];

                Assert.AreEqual(512, texture.Width);
                Assert.AreEqual(512, texture.Height);

                Assert.AreEqual(new Rect(0.0f, 0.0f, 0.25f, 0.25f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[0].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.5f, 0.0f, 0.25f, 0.25f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[1].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.0f, 0.5f, 0.25f, 0.25f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[2].Materials[0].GetTexture("_MainTex").GetGUID()));
                Assert.AreEqual(new Rect(0.5f, 0.5f, 0.25f, 0.25f),
                    atlas.GetUV(buildInfo[0].WorkingObjects[3].Materials[0].GetTexture("_MainTex").GetGUID()));

                Assert.AreEqual(new Color(1.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(0, 0));
                Assert.AreEqual(new Color(1.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(127, 127));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(127, 128));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(128, 127));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(255, 255));

                Assert.AreEqual(new Color(0.0f, 0.0f, 1.0f, 1.0f), texture.GetPixel(256, 0));
                Assert.AreEqual(new Color(0.0f, 0.0f, 1.0f, 1.0f), texture.GetPixel(383, 127));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(384, 127));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(383, 128));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(511, 255));

                Assert.AreEqual(new Color(0.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(0, 256));
                Assert.AreEqual(new Color(0.0f, 1.0f, 0.0f, 1.0f), texture.GetPixel(127, 383));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(128, 383));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(127, 384));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(255, 511));

                Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f, 1.0f), texture.GetPixel(256, 256));
                Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f, 1.0f), texture.GetPixel(383, 383));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(384, 383));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(383, 384));
                Assert.AreEqual(new Color(0.0f, 0.0f, 0.0f, 0.0f), texture.GetPixel(511, 511));
            }

        }

        private SerializableDynamicObject CreateSimpleBatcherOptions()
        {
            SerializableDynamicObject options = new SerializableDynamicObject();
            dynamic batcherOptions = options;

            batcherOptions.PackTextureSize = 1024;
            batcherOptions.LimitTextureSize = 128;
            batcherOptions.MaterialGUID = "";

            batcherOptions.TextureInfoList = new List<SimpleBatcher.TextureInfo>();
            batcherOptions.TextureInfoList.Add(new SimpleBatcher.TextureInfo()
            {
                InputName = "_MainTex",
                OutputName = "_MainTex",
                Type = SimpleBatcher.PackingType.White
            });

            batcherOptions.EnableTintColor = false;
            batcherOptions.TintColorName = "";


            return options;
        }

        private DisposableList<HLODBuildInfo> CreateBuildInfo(HLOD hlod)
        {
            MethodInfo buildInfoFunc = typeof(HLODCreator).GetMethod("CreateBuildInfo", BindingFlags.Static | BindingFlags.NonPublic);
            
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(hlod.gameObject);

            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(null);
            List<SpaceNode> rootNodes = spliter.CreateSpaceTree(hlod.GetBounds(), 5.0f, hlod.transform, hlodTargets, null);
            Assert.AreEqual(1, rootNodes.Count);
            return (DisposableList<HLODBuildInfo>)buildInfoFunc.Invoke(null, new object[] { null, rootNodes[0], 0.0f });
        }

    }

}