using NUnit.Framework;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.HLODSystem.RuntimeTests
{
    [TestFixture]
    public class HLODMeshSetterTest : IPrebuildSetup
    {
        public void Setup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest.prefab");
            var prefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest2.prefab");
            var prefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest3.prefab");
            var prefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest4.prefab");
            
            var instance = GameObject.Instantiate(prefab);
            var instance2 = GameObject.Instantiate(prefab2);
            var instance3 = GameObject.Instantiate(prefab3);
            var instance4 = GameObject.Instantiate(prefab4);

            var hlodInstance = GameObject.Instantiate(prefab);
            var hlod = hlodInstance.GetComponent<HLOD>();

            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
            
            while (coroutine.MoveNext())
            {
                //Wait until coroutine is finished
            }   
        }

        private Camera m_camera;
        private DefaultHLODController m_controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GameObject cameraGameObject = new GameObject("Camera");
            m_camera = cameraGameObject.AddComponent<Camera>();
            cameraGameObject.AddComponent<HLODCameraRecognizer>();
            
            var scene = SceneManager.GetSceneAt(0);
            var root = scene.GetRootGameObjects();

            var hlodGameobject = root[5];
            m_controller = hlodGameobject.GetComponent<DefaultHLODController>();

        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var scene = SceneManager.GetSceneAt(0);
            var root = scene.GetRootGameObjects();

            var instance = root[5];
            AssetDatabase.DeleteAsset("Assets/" + instance.name + ".hlod");
        }



        [Test]
        public void MeshRendererExistsTest()
        {
            var scene = SceneManager.GetSceneAt(0);
            var root = scene.GetRootGameObjects();

            var instance = root[1];
            var instance2 = root[2];
            var instance3 = root[3];
            var instance4 = root[4];
            
            var renderers = instance.GetComponentsInChildren<MeshRenderer>();
            var renderer2 = instance2.GetComponentsInChildren<MeshRenderer>();
            var renderer3 = instance3.GetComponentsInChildren<MeshRenderer>();
            var renderer4 = instance4.GetComponentsInChildren<MeshRenderer>();
            
            Assert.AreEqual(20, renderers.Length);
            Assert.AreEqual(4, renderer2.Length);
            Assert.AreEqual(16, renderer3.Length);
            Assert.AreEqual(0, renderer4.Length);
        }

        [Test]
        public void MeshSetterTest()
        {
            m_camera.transform.position = new Vector3(0.0f, 0.0f, -30.0f);

            MeshSetterTestHelper(-30.0f, true, true, false, false, false);
            MeshSetterTestHelper(-40.0f, true, false, false, false, true);
            MeshSetterTestHelper(-45.0f, false, false, false, true, true);
            MeshSetterTestHelper(-60.0f, false, false, true, false, false);
        }

        private void MeshSetterTestHelper(float zpos, bool root1, bool root2, bool hlod1, bool hlod2, bool hlod3)
        {
            m_camera.transform.position = new Vector3(0.0f, 0.0f, zpos);
            
            //update hlod trees
            for (int i = 0; i < 3; ++i)
            {
                HLODManager.Instance.OnPreCull(m_camera);
            }
            
            m_controller.GetHighObject(0, 0, 0.0f, o =>
            {
                Assert.AreEqual(root1, o.LoadedObject.activeInHierarchy);
            });
            m_controller.GetHighObject(1, 0, 0.0f, o =>
            {
                Assert.AreEqual(root2, o.LoadedObject.activeInHierarchy);
            });
            
            m_controller.GetLowObject(0, 0, 0.0f, o =>
            {
                Assert.AreEqual(hlod1, o.LoadedObject.activeInHierarchy);
            });
            m_controller.GetLowObject(1, 0, 0.0f, o =>
            {
                Assert.AreEqual(hlod2, o.LoadedObject.activeInHierarchy);
            });
            m_controller.GetLowObject(2, 0, 0.0f, o =>
            {
                Assert.AreEqual(hlod3, o.LoadedObject.activeInHierarchy);
            });




        }
        


    }
}