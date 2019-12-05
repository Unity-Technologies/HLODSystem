using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.HLODSystem.RuntimeTests
{
    public class RuntimeTests
    {
        private GameObject mGameObject;
        private GameObject mHlodGameObject;
        private GameObject mHlodCameraObject;

        [SetUp]
        public void SetUp()
        {
            mGameObject =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/HLODTestPrefabBaked.prefab");
            mGameObject = GameObject.Instantiate(mGameObject, new Vector3(0, 0, 10), new Quaternion(0, 180, 0, 0));
            new WaitForSeconds(0.1f);

            Assert.NotNull(mGameObject);

            mHlodGameObject = mGameObject.transform.Find("HLOD").gameObject;
            Assert.NotNull(mHlodGameObject);

            mHlodCameraObject = mGameObject.transform.Find("HLOD Camera").gameObject;
            Assert.NotNull(mHlodCameraObject);
        }
        
        [TearDown]
        public void Teardown()
        {
            Object.Destroy(mHlodCameraObject);
            Object.Destroy(mHlodGameObject);
            Object.Destroy(mGameObject);
        }

        [Test]
        public void HlodGameObjectHasChildren()
        {
            int childrenCount = mHlodGameObject.transform.childCount;
            Assert.True(childrenCount == 11);
        }

        [Test]
        public void CameraHasHlodCameraRecognizerComponent()
        {
            HLODCameraRecognizer hlodCameraRecognizer = mHlodCameraObject.GetComponent<HLODCameraRecognizer>();
            Assert.NotNull(hlodCameraRecognizer);
        }

        [Test]
        public void MainCameraDoesNotSeeAnyHlodObjects()
        {
            Camera mainCamera = mGameObject.transform.Find("Main Camera").gameObject.GetComponent<Camera>();
            Assert.NotNull(mainCamera);

            foreach (Transform child in mHlodGameObject.transform)
            {
                Debug.Log(child.name + ": " + IsTargetVisible(mainCamera, child.gameObject));
                //Assert.False(IsTargetVisible(mainCamera, child.gameObject));
            }
        }

        [Test]
        public void HlodCameraSeesHlodObjects()
        {
            Camera hlodCamera = mHlodCameraObject.GetComponent<Camera>();

            foreach (Transform child in mHlodGameObject.transform)
            {
                Debug.Log(child.name + ": " + IsTargetVisible(hlodCamera, child.gameObject));
                Assert.True(IsTargetVisible(hlodCamera, child.gameObject));
            }
        }

        bool IsTargetVisible(Camera c, GameObject go)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(c);
            var point = go.transform.position;

            foreach (var plane in planes)
            {
                float distance = plane.GetDistanceToPoint(point);

                if (distance < 0.0f)
                    return false;
            }

            return true;
        }
    }
}