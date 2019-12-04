using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.HLODSystem.RuntimeTests
{
    public class RuntimeTests
    {
        private GameObject mGameObject;
        private GameObject mHlodGameObject;
        
        [SetUp]
        public void SetUp()
        {
            mGameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/HLODTestPrefabBaked.prefab");
            mGameObject = GameObject.Instantiate(mGameObject, new Vector3(0, 0, 10), new Quaternion(0, 180, 0, 0));
            new WaitForSeconds(0.1f);
            
            Assert.NotNull(mGameObject);
            
            mHlodGameObject= mGameObject.transform.Find("HLOD").gameObject ;
            Assert.NotNull(mHlodGameObject);
        }

        [Test]
        public void HlodGameObjectHasChildren()
        {
            int childrenCount = mHlodGameObject.transform.childCount;
            Assert.True(childrenCount == 11);
        }
        
        /*[UnityTest]
        public IEnumerator BakeHlodForGameObject()
        {
            yield return null;
        }*/
    }
}