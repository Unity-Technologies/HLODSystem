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
    public class SimplifierTests : IPrebuildSetup, IPostBuildCleanup
    {
        const float EPSILON = 0.002f;
        static MeshRenderer s_testMeshRenderer;

        public void Setup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber.prefab");
            s_testMeshRenderer = prefab.GetComponentInChildren<MeshRenderer>();

        }   
        
        public void Cleanup()
        {
        }

        [Test]        
        public void PolygonRatioTest()
        {
            SerializableDynamicObject dynamicObject = new SerializableDynamicObject();
            dynamic options = dynamicObject;

            options.SimplifyPolygonRatio = 0.6f;
            options.SimplifyMinPolygonCount = 1;
            options.SimplifyMaxPolygonCount = 1000000;

            //original tri count is 1200.

            //level 0 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(0);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                float ratio = (float)afterPolygonCount / (float)beforePolygonCount;

                Assert.Less(Mathf.Abs(ratio - 0.6f), EPSILON);
            }

            //level 1 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(1);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                float ratio = (float)afterPolygonCount / (float)beforePolygonCount;

                Assert.Less(Mathf.Abs(ratio - 0.6f * 0.6f), EPSILON);
            }
            
            //level 2 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(2);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                float ratio = (float)afterPolygonCount / (float)beforePolygonCount;

                Assert.Less(Mathf.Abs(ratio - 0.6f * 0.6f * 0.6f), EPSILON);
            }
        }
        
        [Test]
        public void MinPolygonTest()
        {
            SerializableDynamicObject dynamicObject = new SerializableDynamicObject();
            dynamic options = dynamicObject;

            options.SimplifyPolygonRatio = 0.6f;
            options.SimplifyMinPolygonCount = 500;
            options.SimplifyMaxPolygonCount = 1000000;

            //original tri count is 1200.

            //level 0 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(0);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                Assert.AreEqual(afterPolygonCount, 720 * 3);// convert tri count to poly count
            }

            //level 1 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(1);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                Assert.AreEqual(afterPolygonCount, 500 * 3);// convert tri count to poly count
            }

            //level 2 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(2);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                Assert.AreEqual(afterPolygonCount, 500 * 3); // convert tri count to poly count
            }
        }
        [Test]
        public void MaxPolygonTest()
        {
            SerializableDynamicObject dynamicObject = new SerializableDynamicObject();
            dynamic options = dynamicObject;

            options.SimplifyPolygonRatio = 0.6f;
            options.SimplifyMinPolygonCount = 1;
            options.SimplifyMaxPolygonCount = 500;

            //original tri count is 1200.

            //level 0 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(0);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                Assert.AreEqual(afterPolygonCount, 500 * 3);// convert tri count to poly count
            }

            //level 1 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(1);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                Assert.AreEqual(afterPolygonCount, 300 * 3);// convert tri count to poly count
            }

            //level 2 test
            using (HLODBuildInfo info = new HLODBuildInfo())
            {
                info.WorkingObjects.Add(s_testMeshRenderer.ToWorkingObject(Collections.Allocator.Persistent));
                info.Distances.Add(2);

                int beforePolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                var simplifer = new Simplifier.UnityMeshSimplifier(options);
                simplifer.SimplifyImmidiate(info);

                int afterPolygonCount = info.WorkingObjects[0].Mesh.triangles.Length;

                Assert.AreEqual(afterPolygonCount, 180 * 3); // convert tri count to poly count
            }
        }        
    }
}