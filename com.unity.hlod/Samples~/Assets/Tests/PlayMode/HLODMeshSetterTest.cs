using NUnit.Framework;
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
            var prefabs = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest.prefab");
            var prefabs2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest2.prefab");
            var prefabs3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest3.prefab");
            var prefabs4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest4.prefab");
            
            var instance = GameObject.Instantiate(prefabs);
            var instance2 = GameObject.Instantiate(prefabs2);
            var instance3 = GameObject.Instantiate(prefabs3);
            var instance4 = GameObject.Instantiate(prefabs4);
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
        


    }
}