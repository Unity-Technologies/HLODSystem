using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
            mGameObject = GameObject.Instantiate(mGameObject, Vector3.zero, Quaternion.identity);

            //mGameObject = MonoBehaviour.Instantiate(Resources.Load<GameObject>("HLODTestPrefabBaked"));

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

        [UnityTest]
        public IEnumerator Test_1()
        {
            TestData testData = TestData.CreateFromJson("Assets/TestAssets/RawTestData/TestData_1.json");

            Camera hlodCamera = mHlodCameraObject.GetComponent<Camera>();

            hlodCamera.transform.position = new Vector3(-2.5f, 30, -70);
            hlodCamera.transform.rotation = Quaternion.Euler(35, 0, 0);
            /*hlodCamera.transform.position = new Vector3(
                testData.cameraSettings.location.x,
                testData.cameraSettings.location.y,
                testData.cameraSettings.location.z);

            hlodCamera.transform.eulerAngles = new Vector3(
                testData.cameraSettings.rotation.x,
                testData.cameraSettings.rotation.y + 180,
                testData.cameraSettings.rotation.z);*/
            
            HLODManager.Instance.OnPreCull(hlodCamera);

            yield return new WaitForSeconds(0.1f);

            foreach (TestGameObject testGameObject in testData.listOfGameObjects)
            {
                Transform rimNumbers = mHlodGameObject.transform.Find(testGameObject.groupName);

                foreach (Transform child in rimNumbers)
                {
                    Debug.Log(child.gameObject.name + ": " + child.gameObject.activeSelf);
                }
            }
        }
    }

    [Serializable]
    public class TestData
    {
        public CameraSettings cameraSettings;
        public List<TestGameObject> listOfGameObjects;

        public static TestData CreateFromJson(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException();

            string dataAsJson = File.ReadAllText(jsonFilePath);

            return JsonUtility.FromJson<TestData>(dataAsJson);
        }
    }

    [Serializable]
    public class CustomVector3
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class TestGameObject
    {
        public string groupName;
        public bool[] enabled;
    }

    [Serializable]
    public class CameraSettings
    {
        public CustomVector3 location;
        public CustomVector3 rotation;
    }
}