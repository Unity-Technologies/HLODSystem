using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;

namespace Unity.HLODSystem.EditorTests
{

    [TestFixture]
    public class MeshSetterMiniTest
    {
        private GameObject instance;
        
        [SetUp]
        public void Setup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/MeshSettingMiniTest.prefab");
            instance = GameObject.Instantiate(prefab);
            //s_testMeshRenderer = prefab.GetComponentInChildren<MeshRenderer>();
            
            var hlod = instance.GetComponent<HLOD>();
            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
            
            while (coroutine.MoveNext())
            {
                //Wait until coroutine is finished
            }
        }   
        
        [TearDown]
        public void Cleanup()
        {
            GameObject.DestroyImmediate(instance);
        }
        
        [Test]
        public void StructureTest()
        {
            var controller = instance.GetComponent<DefaultHLODController>();
            Assert.NotNull(controller);
            
            Assert.AreEqual(3, controller.LowObjectCount);
            
            controller.GetLowObject(0, 0, 0.0f, o =>
            {
                var meshFilters = o.LoadedObject.GetComponentsInChildren<MeshFilter>();
                Assert.AreEqual(2, meshFilters.Length);
                
                Assert.AreEqual(1252, meshFilters[0].sharedMesh.vertexCount);
                Assert.AreEqual(688, meshFilters[1].sharedMesh.vertexCount);
            });
            
            controller.GetLowObject(1, 0, 0.0f, o =>
            {
                var meshFilters = o.LoadedObject.GetComponentsInChildren<MeshFilter>();
                Assert.AreEqual(2, meshFilters.Length);
                
                Assert.AreEqual(1252, meshFilters[0].sharedMesh.vertexCount);
                Assert.AreEqual(344, meshFilters[1].sharedMesh.vertexCount);
            });
            
            controller.GetLowObject(2, 0, 0.0f, o =>
            {
                var meshFilters = o.LoadedObject.GetComponentsInChildren<MeshFilter>();
                Assert.AreEqual(1, meshFilters.Length);
                
                Assert.AreEqual(1252, meshFilters[0].sharedMesh.vertexCount);
            });
            
            
        }
    }

}