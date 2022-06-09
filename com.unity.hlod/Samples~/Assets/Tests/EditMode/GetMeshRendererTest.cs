using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using NUnit.Framework;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class GetMeshRendererTest
    {
        MethodInfo m_getMeshRendererFunc;

        [SetUp]
        public void Setup()
        {           
        }

        [TearDown]
        public void Cleanup()
        {
        }

        [Test]
        public void MeshRendererTest()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/HLODTargetTest_MeshRenderer.prefab");
            var root = GameObject.Instantiate(prefab);
            var targets = Unity.HLODSystem.Utils.ObjectUtils.HLODTargets(root);
                        
            Assert.AreEqual(8, targets.Count);
            
            var renderers = CreateUtils.GetMeshRenderers(targets, 0.0f, 0);
            
            Assert.AreEqual(8, renderers.Count);
        }
        
        [Test]
        public void LODGroupTest()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/HLODTargetTest_LODGroup.prefab");
            var root = GameObject.Instantiate(prefab);
            var targets = Unity.HLODSystem.Utils.ObjectUtils.HLODTargets(root);
            
            Assert.AreEqual(2, targets.Count);
            
            var renderers = CreateUtils.GetMeshRenderers(targets, 0.0f, 0);
            
            Assert.AreEqual(2, renderers.Count);
        }
        
        [Test]
        public void LODGroupTest2()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/HLODTargetTest_LODGroup2.prefab");
            var root = GameObject.Instantiate(prefab);
            var targets = Unity.HLODSystem.Utils.ObjectUtils.HLODTargets(root);
            
            Assert.AreEqual(3, targets.Count);
            
            var renderers = CreateUtils.GetMeshRenderers(targets, 0.0f, 0);
            
            Assert.AreEqual(6, renderers.Count);
        }
        
        [Test]
        public void MeshSettingTest()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/HLODTargetTest_MeshSetting.prefab");
            var root = GameObject.Instantiate(prefab);
            var targets = Unity.HLODSystem.Utils.ObjectUtils.HLODTargets(root);
            
            Assert.AreEqual(5, targets.Count);
            
            var renderers = CreateUtils.GetMeshRenderers(targets, 0.0f, 0);
            
            Assert.AreEqual(7, renderers.Count);
        }
    }
}