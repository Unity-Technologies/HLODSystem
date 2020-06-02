using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using System.Linq;

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

        [SetUp]
        public void Setup()
        {
            m_prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber.prefab");
            m_prefabMesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/RinNumber_LOD3.prefab");

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
            m_includeObjects.Add(obj6);

            var obj7 = GameObject.Instantiate(m_prefabMesh);
            obj7.transform.SetParent(m_hlodRootGameObject.transform);
            obj7.transform.position = new Vector3(-5.0f, 0.0f, 5.0f);
            m_includeObjects.Add(obj7);

            var obj8 = GameObject.Instantiate(m_prefabMesh);
            obj8.transform.SetParent(m_hlodRootGameObject.transform);
            obj8.transform.position = new Vector3( 5.0f, 0.0f, -5.0f);
            m_includeObjects.Add(obj8);

            var obj9 = GameObject.Instantiate(m_prefabMesh);
            obj9.transform.SetParent(m_hlodRootGameObject.transform);
            obj9.transform.position = new Vector3( 5.0f, 0.0f, 5.0f);
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

            Assert.LessOrEqual((bounds1.center - new Vector3(0.0f, 1.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds1.min - new Vector3(-10.5f, -9.5f, -10.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds1.max - new Vector3( 10.5f, 11.5f,  10.5f)).magnitude, EPSILON);

            //add new object to outside of area
            var addObj1 = GameObject.Instantiate(m_prefab);
            addObj1.transform.SetParent(m_hlodRootGameObject.transform);
            addObj1.transform.position = new Vector3(20.0f, 0.0f, 0.0f);
            var bounds2 = m_hlodComponent.GetBounds();

            Assert.LessOrEqual((bounds2.center - new Vector3(5.0f, 1.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds2.min - new Vector3(-10.5f, -14.5f, -15.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds2.max - new Vector3(20.5f, 16.5f, 15.5f)).magnitude, EPSILON);

            //add new object to inside of area
            var addObj3 = GameObject.Instantiate(m_prefab);
            addObj3.transform.SetParent(m_hlodRootGameObject.transform);
            addObj3.transform.position = new Vector3(5.0f, 0.0f, 5.0f);
            var bounds3 = m_hlodComponent.GetBounds();

            Assert.LessOrEqual((bounds3.center - new Vector3(5.0f, 1.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds3.min - new Vector3(-10.5f, -14.5f, -15.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds3.max - new Vector3(20.5f, 16.5f, 15.5f)).magnitude, EPSILON);

            //add new empty object to ouside of area.
            //the bounds must not changed.
            var addObj4 = new GameObject();
            addObj4.transform.SetParent(m_hlodRootGameObject.transform);
            addObj4.transform.position = new Vector3(5.0f, 0.0f, 5.0f);
            var bounds4 = m_hlodComponent.GetBounds();

            Assert.LessOrEqual((bounds4.center - new Vector3(5.0f, 1.0f, 0.0f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds4.min - new Vector3(-10.5f, -14.5f, -15.5f)).magnitude, EPSILON);
            Assert.LessOrEqual((bounds4.max - new Vector3(20.5f, 16.5f, 15.5f)).magnitude, EPSILON);
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
        public void SpaceSplitTest()
        {
            //ISpaceSplitter spliter = new QuadTreeSpaceSplitter(m_hlodComponent.transform.position, 0.0f, m_hlodComponent.ChunkSize);
            //SpaceNode rootNode = spliter.CreateSpaceTree(bounds, hlodTargets, null);
        }
    }

}