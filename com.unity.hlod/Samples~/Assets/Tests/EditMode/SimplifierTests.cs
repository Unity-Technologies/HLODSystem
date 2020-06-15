using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.HLODSystem.Utils;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class SimplifierTests
    {
        
        MeshRenderer s_testMeshRenderer;

        [SetUp]
        public void Setup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber.prefab");
            s_testMeshRenderer = prefab.GetComponentInChildren<MeshRenderer>();

        }   
        
        [TearDown]
        public void Cleanup()
        {
        }

        [Test]        
        public void PolygonRatioTest()
        {
            const float EPSILON = 0.005f; //ratio error should less than 0.5%

            //original tri count is 1200.
            Assert.Less(Mathf.Abs(TestImpl(0.6f, 1, 1000000, 0) / 1200.0f - 0.6f), EPSILON);
            Assert.Less(Mathf.Abs(TestImpl(0.6f, 1, 1000000, 1) / (1200.0f * 0.6f) - 0.6f), EPSILON);
            Assert.Less(Mathf.Abs(TestImpl(0.6f, 1, 1000000, 2) / (1200.0f * 0.6f * 0.6f) - 0.6f), EPSILON);
        }
        
        [Test]
        public void MinPolygonTest()
        {
            Assert.AreEqual(TestImpl(0.6f, 500, 1000000, 0), 720);
            Assert.AreEqual(TestImpl(0.6f, 500, 1000000, 1), 500);
            Assert.AreEqual(TestImpl(0.6f, 500, 1000000, 2), 500);
        }
        [Test]
        public void MaxPolygonTest()
        {
            Assert.AreEqual(TestImpl(0.6f, 1, 500, 0), 500);
            Assert.AreEqual(TestImpl(0.6f, 1, 500, 1), 300);
            Assert.AreEqual(TestImpl(0.6f, 1, 500, 2), 180);            
        }

        private int TestImpl(float ratio, int min, int max, int level)
        {
            SerializableDynamicObject dynamicObject = new SerializableDynamicObject();
            dynamic options = dynamicObject;

            options.SimplifyPolygonRatio = ratio;
            options.SimplifyMinPolygonCount = min;
            options.SimplifyMaxPolygonCount = max;

            //original tri count is 1200.

            //level 0 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(level);

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterTriCount = info.WorkingObjects[0].Mesh.triangles.Length;

                return afterTriCount / 3;
            }
        }
    }
}