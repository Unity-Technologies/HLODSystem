using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.HLODSystem;
using NUnit.Framework;
using System;
using Unity.HLODSystem.Utils;
using System.Reflection;
using Unity.HLODSystem.SpaceManager;

namespace Unity.HLODSystem.EditorTests
{

    [TestFixture]
    public class MaterialPreservingBatcherTests
    {
        GameObject m_mesh1material1;
        GameObject m_mesh2material1;
        GameObject m_mesh2material2;
        GameObject m_mesh3material2;

        GameObject m_mesh1material1T;
        GameObject m_mesh2material1T;
        GameObject m_mesh2material2T;
        GameObject m_mesh3material2T;

        MethodInfo m_buildInfoFunc;
        Type m_batcherType;

        GameObject m_hlodGameObject;
        HLOD m_hlodComponent;


        [SetUp]
        public void Setup()
        {
            //1개의 매터리얼 1개의 메시를 이용하여 테스트
            //1개의 매터리얼 2개의 메시를 이용
            //2개의 매터리얼 2개의 메시를 이용
            //2개의 매터리얼 3개의 메시를 이용

            m_mesh1material1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD0_2.prefab");
            m_mesh2material1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1.prefab");
            m_mesh2material2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1_2.prefab");
            m_mesh3material2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD2.prefab");

            m_mesh1material1T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD0T_2.prefab");
            m_mesh2material1T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1T.prefab");
            m_mesh2material2T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD1T_2.prefab");
            m_mesh3material2T = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD2T.prefab");

            m_hlodGameObject = new GameObject();
            m_hlodComponent = m_hlodGameObject.AddComponent<HLOD>();

            m_buildInfoFunc = typeof(HLODCreator).GetMethod("CreateBuildInfo", BindingFlags.Static | BindingFlags.NonPublic);

            var types = BatcherTypes.GetTypes();
            for ( int i = 0; i < types.Length; ++i )
            {
                if (types[i].Name == "MaterialPreservingBatcher")
                    m_batcherType = types[i];
            }
        }

        [TearDown]
        public void Cleanup()
        {

        }

        [Test]
        public void OneMaterialOneMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);


            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(1, infos[0].WorkingObjects.Count);
            }
        }
        [Test]
        public void OneTextureMaterialOneMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(1, infos[0].WorkingObjects.Count);
            }
        }

        [Test]
        public void OneMaterialTwoMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material1);
            obj3.transform.SetParent(m_hlodGameObject.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1);
            obj4.transform.SetParent(m_hlodGameObject.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);


            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(1, infos[0].WorkingObjects.Count);
            }
        }

        [Test]
        public void OneTextureMaterialTwoMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material1T);
            obj3.transform.SetParent(m_hlodGameObject.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1T);
            obj4.transform.SetParent(m_hlodGameObject.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);


            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(1, infos[0].WorkingObjects.Count);
            }
        }

        [Test]
        public void TwoMaterialTwoMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2);
            obj3.transform.SetParent(m_hlodGameObject.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1);
            obj4.transform.SetParent(m_hlodGameObject.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);


            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(2, infos[0].WorkingObjects.Count);
            }
        }

        [Test]
        public void TwoTextureMaterialTwoMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2T);
            obj3.transform.SetParent(m_hlodGameObject.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1T);
            obj4.transform.SetParent(m_hlodGameObject.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);


            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(2, infos[0].WorkingObjects.Count);
            }
        }

        [Test]
        public void TwoMaterialThreeMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2);
            obj3.transform.SetParent(m_hlodGameObject.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1);
            obj4.transform.SetParent(m_hlodGameObject.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);

            var obj5 = GameObject.Instantiate(m_mesh3material2);
            obj5.transform.SetParent(m_hlodGameObject.transform);
            obj5.transform.position = new Vector3(0.0f, 0.0f, -1.0f);

            

            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(2, infos[0].WorkingObjects.Count);
            }
        }

        [Test]
        public void TwoTextureMaterialThreeMesh()
        {
            var obj1 = GameObject.Instantiate(m_mesh1material1T);
            obj1.transform.SetParent(m_hlodGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            var obj2 = GameObject.Instantiate(m_mesh1material1T);
            obj2.transform.SetParent(m_hlodGameObject.transform);
            obj2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            var obj3 = GameObject.Instantiate(m_mesh2material2T);
            obj3.transform.SetParent(m_hlodGameObject.transform);
            obj3.transform.position = new Vector3(-1.0f, 0.0f, 0.0f);

            var obj4 = GameObject.Instantiate(m_mesh2material1T);
            obj4.transform.SetParent(m_hlodGameObject.transform);
            obj4.transform.position = new Vector3(0.0f, 0.0f, 1.0f);

            var obj5 = GameObject.Instantiate(m_mesh3material2T);
            obj5.transform.SetParent(m_hlodGameObject.transform);
            obj5.transform.position = new Vector3(0.0f, 0.0f, -1.0f);



            using (var infos = CreateBuildInfo())
            {
                DoBatch(infos);

                Assert.AreEqual(1, infos.Count);
                Assert.AreEqual(2, infos[0].WorkingObjects.Count);
            }
        }

        private DisposableList<HLODBuildInfo> CreateBuildInfo()
        {
            
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(m_hlodComponent.gameObject);
            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(null);
            List<SpaceNode> rootNodes = spliter.CreateSpaceTree(m_hlodComponent.GetBounds(), 10.0f, m_hlodComponent.transform, hlodTargets, null);

            Assert.AreEqual(1, rootNodes.Count);
            return  (DisposableList<HLODBuildInfo>)m_buildInfoFunc.Invoke(null, new object[] { null, rootNodes[0], 0.0f });
        }
        private void DoBatch(DisposableList<HLODBuildInfo> infos)
        {
            IBatcher batcher = (IBatcher)Activator.CreateInstance(m_batcherType, new object[] { m_hlodComponent.BatcherOptions });
            batcher.Batch(m_hlodComponent.transform, infos, null);
        }
    }

}