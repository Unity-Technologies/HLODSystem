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
    public class SpaceSplitterSubTreeTests
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
        public void SpaceSplitTestSize5()
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
        public void SpaceSplitTestSize10()
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