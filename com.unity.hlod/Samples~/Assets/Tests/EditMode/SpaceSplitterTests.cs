using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using System.Linq;
using System.Reflection;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class SpaceSplitterTests
    {
        GameObject m_hlodRootGameObject;
        HLOD m_hlodComponent;

        GameObject m_prefab;
        GameObject m_prefabMesh;

        List<GameObject> m_includeObjects = new List<GameObject>();
        List<GameObject> m_excludeObjects = new List<GameObject>();

        delegate DisposableList<HLODBuildInfo> CreateBuildInfoFunc(SpaceNode root, float minObjectSize);
        MethodInfo m_buildInfoFunc;
    
        [SetUp]
        public void Setup()
        {
            m_prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber.prefab");
            m_prefabMesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD3.prefab");

            m_buildInfoFunc = typeof(HLODCreator).GetMethod("CreateBuildInfo", BindingFlags.Static | BindingFlags.NonPublic);

            m_hlodRootGameObject = new GameObject();
            m_hlodComponent = m_hlodRootGameObject.AddComponent<HLOD>();

            var obj1 = GameObject.Instantiate(m_prefab);
            obj1.transform.SetParent(m_hlodRootGameObject.transform);
            obj1.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            m_includeObjects.Add(obj1);

            var obj2 = GameObject.Instantiate(m_prefab);
            obj2.transform.SetParent(m_hlodRootGameObject.transform);
            obj2.transform.position = new Vector3(10.0f, 0.0f, -10.0f);
            m_includeObjects.Add(obj2);

            var obj3 = GameObject.Instantiate(m_prefab);
            obj3.transform.SetParent(m_hlodRootGameObject.transform);
            obj3.transform.position = new Vector3(-10.0f, 0.0f, -10.0f);
            m_includeObjects.Add(obj3);

            var obj4 = GameObject.Instantiate(m_prefab);
            obj4.transform.SetParent(m_hlodRootGameObject.transform);
            obj4.transform.position = new Vector3(10.0f, 0.0f, 10.0f);
            m_includeObjects.Add(obj4);

            var obj5 = GameObject.Instantiate(m_prefab);
            obj5.transform.SetParent(m_hlodRootGameObject.transform);
            obj5.transform.position = new Vector3(-10.0f, 0.0f, 10.0f);
            m_includeObjects.Add(obj5);

            var obj6 = GameObject.Instantiate(m_prefabMesh);
            obj6.transform.SetParent(m_hlodRootGameObject.transform);
            obj6.transform.position = new Vector3(-5.0f, 0.0f, -5.0f);
            obj6.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            m_includeObjects.Add(obj6);

            var obj7 = GameObject.Instantiate(m_prefabMesh);
            obj7.transform.SetParent(m_hlodRootGameObject.transform);
            obj7.transform.position = new Vector3(-5.0f, 0.0f, 5.0f);
            obj7.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            m_includeObjects.Add(obj7);

            var obj8 = GameObject.Instantiate(m_prefabMesh);
            obj8.transform.SetParent(m_hlodRootGameObject.transform);
            obj8.transform.position = new Vector3( 5.0f, 0.0f, -5.0f);
            obj8.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            m_includeObjects.Add(obj8);

            var obj9 = GameObject.Instantiate(m_prefabMesh);
            obj9.transform.SetParent(m_hlodRootGameObject.transform);
            obj9.transform.position = new Vector3( 5.0f, 0.0f, 5.0f);
            obj9.transform.localScale = new Vector3(5.0f, 5.0f, 5.0f);
            m_includeObjects.Add(obj9);


            //this should be exclude.
            var obj10 = new GameObject();
            obj10.transform.SetParent(m_hlodRootGameObject.transform);
            obj10.transform.position = new Vector3(40.0f, 0.0f, 40.0f);
            m_excludeObjects.Add(obj10);

            var obj11 = new GameObject();
            obj11.transform.SetParent(m_hlodRootGameObject.transform);
            obj11.transform.position = new Vector3(-40.0f, 0.0f, -40.0f);
            m_excludeObjects.Add(obj11);
        }

        [TearDown]
        public void Cleanup()
        {
            GameObject.DestroyImmediate(m_hlodRootGameObject);
        }

        [Test]
        public void BoundsTest()
        {
            const float EPSILON = 0.001f;   // error should less than 0.1%.
            var bounds1 = m_hlodComponent.GetBounds();

            Assert.LessOrEqual((bounds1.center - new Vector3(0.0f, 5.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds1.min - new Vector3(-10.5f, -5.5f, -10.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds1.max - new Vector3( 10.5f, 15.5f,  10.5f)).magnitude, EPSILON);

            //add new object to outside of area
            var addObj1 = GameObject.Instantiate(m_prefab);
            addObj1.transform.SetParent(m_hlodRootGameObject.transform);
            addObj1.transform.position = new Vector3(20.0f, 0.0f, 0.0f);
            var bounds2 = m_hlodComponent.GetBounds();

            Assert.LessOrEqual((bounds2.center - new Vector3(5.0f, 5.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds2.min - new Vector3(-10.5f, -10.5f, -15.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds2.max - new Vector3(20.5f, 20.5f, 15.5f)).magnitude, EPSILON);

            //add new object to inside of area
            var addObj3 = GameObject.Instantiate(m_prefab);
            addObj3.transform.SetParent(m_hlodRootGameObject.transform);
            addObj3.transform.position = new Vector3(5.0f, 0.0f, 5.0f);
            var bounds3 = m_hlodComponent.GetBounds();

            Assert.LessOrEqual((bounds3.center - new Vector3(5.0f, 5.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds3.min - new Vector3(-10.5f, -10.5f, -15.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds3.max - new Vector3(20.5f, 20.5f, 15.5f)).magnitude, EPSILON);

            //add new empty object to ouside of area.
            //the bounds must not changed.
            var addObj4 = new GameObject();
            addObj4.transform.SetParent(m_hlodRootGameObject.transform);
            addObj4.transform.position = new Vector3(5.0f, 0.0f, 5.0f);
            var bounds4 = m_hlodComponent.GetBounds();

            Assert.LessOrEqual((bounds4.center - new Vector3(5.0f, 5.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds4.min - new Vector3(-10.5f, -10.5f, -15.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds4.max - new Vector3(20.5f, 20.5f, 15.5f)).magnitude, EPSILON);
        }

        [Test]
        public void HLODTargetsTest()
        {
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(m_hlodComponent.gameObject);

            Assert.AreEqual(hlodTargets.Count, 9);
            for ( int i = 0; i < hlodTargets.Count; ++i )
            {
                Assert.True(m_includeObjects.Contains(hlodTargets[i]));
            }

            for (int i = 0; i < m_excludeObjects.Count; ++i)
            {
                Assert.False(hlodTargets.Contains(m_excludeObjects[i]));
            }
        }
        [Test]
        public void SpaceSplitTestSize5()
        {
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(m_hlodComponent.gameObject);

            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(null);
            List<SpaceNode> rootNodes = spliter.CreateSpaceTree(m_hlodComponent.GetBounds(), 5.0f, m_hlodComponent.transform, hlodTargets, null);

            Assert.AreEqual(1, rootNodes.Count);
            Assert.AreEqual(CalcLevel(rootNodes[0]), 4);
            Assert.AreEqual(GetTargetCount(rootNodes[0]), 9);

        }
        [Test]
        public void SpaceSplitTestSize10()
        {
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(m_hlodComponent.gameObject);

            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(null);
            List<SpaceNode> rootNodes = spliter.CreateSpaceTree(m_hlodComponent.GetBounds(), 10.0f, m_hlodComponent.transform, hlodTargets, null);

            Assert.AreEqual(1, rootNodes.Count);
            Assert.AreEqual(CalcLevel(rootNodes[0]), 3);
            Assert.AreEqual(GetTargetCount(rootNodes[0]), 9);
        }

         [Test]
        public void SpaceSplitSubTreeTestSize5()
        {
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(m_hlodComponent.gameObject);

            var options = QuadTreeSpaceSplitter.CreateOptions(true, 5.0f, true, 20);
            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(options);
            List<SpaceNode> rootNodes = spliter.CreateSpaceTree(m_hlodComponent.GetBounds(), 5.0f, m_hlodComponent.transform, hlodTargets, null);

            Assert.AreEqual(4, rootNodes.Count);
            Assert.AreEqual(3, CalcLevel(rootNodes[0]));
            Assert.AreEqual(3, GetTargetCount(rootNodes[0]));
            
            Assert.AreEqual(3, CalcLevel(rootNodes[1]));
            Assert.AreEqual(2, GetTargetCount(rootNodes[1]));
            
            Assert.AreEqual(3, CalcLevel(rootNodes[2]));
            Assert.AreEqual(2, GetTargetCount(rootNodes[2]));
            
            Assert.AreEqual(3, CalcLevel(rootNodes[3]));
            Assert.AreEqual(2, GetTargetCount(rootNodes[3]));
            
        }
        [Test]
        public void SpaceSplitSubTreeTestSize10()
        {
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(m_hlodComponent.gameObject);
            
            var options = QuadTreeSpaceSplitter.CreateOptions(true, 5.0f, true, 20);
            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(options);
            List<SpaceNode> rootNodes = spliter.CreateSpaceTree(m_hlodComponent.GetBounds(), 10.0f, m_hlodComponent.transform, hlodTargets, null);

            Assert.AreEqual(4, rootNodes.Count);
            Assert.AreEqual(2, CalcLevel(rootNodes[0]));
            Assert.AreEqual(3, GetTargetCount(rootNodes[0]));
            
            Assert.AreEqual(2, CalcLevel(rootNodes[1]));
            Assert.AreEqual(2, GetTargetCount(rootNodes[1]));
            
            Assert.AreEqual(2, CalcLevel(rootNodes[2]));
            Assert.AreEqual(2, GetTargetCount(rootNodes[2]));
            
            Assert.AreEqual(2, CalcLevel(rootNodes[3]));
            Assert.AreEqual(2, GetTargetCount(rootNodes[3]));
            
        }

        
        [Test]
        public void CreateBuildInfoTest()
        {
            List<GameObject> hlodTargets = ObjectUtils.HLODTargets(m_hlodComponent.gameObject);

            ISpaceSplitter spliter = new QuadTreeSpaceSplitter(null);
            List<SpaceNode> rootNodes = spliter.CreateSpaceTree(m_hlodComponent.GetBounds(), 5.0f, m_hlodComponent.transform, hlodTargets, null);

            Assert.AreEqual(1, rootNodes.Count);
            using (DisposableList<HLODBuildInfo> ret = (DisposableList<HLODBuildInfo>)m_buildInfoFunc.Invoke(null, new object[] { null, rootNodes[0], 0.0f }))
            {
                //only exists nodes are creating info.
                Assert.AreEqual(ret.Count, 11);

                Assert.AreEqual(ret[0].Name, "");
                Assert.AreEqual(ret[0].WorkingObjects.Count, 9);

                Assert.AreEqual(ret[1].Name, "_1");
                Assert.AreEqual(ret[1].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[2].Name, "_2");
                Assert.AreEqual(ret[2].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[3].Name, "_3");
                Assert.AreEqual(ret[3].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[4].Name, "_4");
                Assert.AreEqual(ret[4].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[5].Name, "_1_1");
                Assert.AreEqual(ret[5].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[6].Name, "_1_4");
                Assert.AreEqual(ret[6].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[7].Name, "_2_2");
                Assert.AreEqual(ret[7].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[8].Name, "_2_3");
                Assert.AreEqual(ret[8].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[9].Name, "_3_3");
                Assert.AreEqual(ret[9].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[10].Name, "_4_4");
                Assert.AreEqual(ret[10].WorkingObjects.Count, 1);
            }

            //exclude object smaller than 0.5.
            using (DisposableList<HLODBuildInfo> ret = (DisposableList<HLODBuildInfo>)m_buildInfoFunc.Invoke(null, new object[] { null, rootNodes[0], 0.5f }))
            {
                //only exists nodes are creating info.
                Assert.AreEqual(ret.Count, 10);

                Assert.AreEqual(ret[0].Name, "");
                Assert.AreEqual(ret[0].WorkingObjects.Count, 8);

                Assert.AreEqual(ret[1].Name, "_1");
                Assert.AreEqual(ret[1].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[2].Name, "_2");
                Assert.AreEqual(ret[2].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[3].Name, "_3");
                Assert.AreEqual(ret[3].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[4].Name, "_4");
                Assert.AreEqual(ret[4].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[5].Name, "_1_1");
                Assert.AreEqual(ret[5].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[6].Name, "_2_2");
                Assert.AreEqual(ret[6].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[7].Name, "_2_3");
                Assert.AreEqual(ret[7].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[8].Name, "_3_3");
                Assert.AreEqual(ret[8].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[9].Name, "_4_4");
                Assert.AreEqual(ret[9].WorkingObjects.Count, 1);
            }

            //exclude object smaller than 1.
            using (DisposableList<HLODBuildInfo> ret = (DisposableList<HLODBuildInfo>)m_buildInfoFunc.Invoke(null, new object[] { null, rootNodes[0], 1.0f }))
            {
                //only exists nodes are creating info.
                Assert.AreEqual(ret.Count, 9);

                Assert.AreEqual(ret[0].Name, "");
                Assert.AreEqual(ret[0].WorkingObjects.Count, 7);

                Assert.AreEqual(ret[1].Name, "_1");
                Assert.AreEqual(ret[1].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[2].Name, "_2");
                Assert.AreEqual(ret[2].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[3].Name, "_3");
                Assert.AreEqual(ret[3].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[4].Name, "_4");
                Assert.AreEqual(ret[4].WorkingObjects.Count, 2);

                Assert.AreEqual(ret[5].Name, "_1_1");
                Assert.AreEqual(ret[5].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[6].Name, "_2_2");
                Assert.AreEqual(ret[6].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[7].Name, "_3_3");
                Assert.AreEqual(ret[7].WorkingObjects.Count, 1);

                Assert.AreEqual(ret[8].Name, "_4_4");
                Assert.AreEqual(ret[8].WorkingObjects.Count, 1);
            }

        }


        private static int CalcLevel(SpaceNode node)
        {
            SpaceNode curNode = node;
            int level = 0;

            while (curNode != null)
            {
                level += 1;
                if (curNode.HasChild() == true)
                {
                    curNode = curNode.GetChild(0);
                }
                else
                {
                    curNode = null;
                }
            }

            return level;
        }
        private static int GetTargetCount(SpaceNode node)
        {
            int count = 0;
            Stack<SpaceNode> searchNodes = new Stack<SpaceNode>();
            searchNodes.Push(node);

            while(searchNodes.Count > 0 )
            {
                SpaceNode curNode = searchNodes.Pop();
                count += curNode.Objects.Count;

                for ( int i = 0; i < curNode.GetChildCount(); ++i )
                {
                    searchNodes.Push(curNode.GetChild(i));
                }
            }

            return count;
        }
    }

}