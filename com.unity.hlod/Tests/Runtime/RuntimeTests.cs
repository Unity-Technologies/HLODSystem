using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.HLODSystem.Streaming;
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

            mHlodGameObject.GetComponentInChildren<HLODControllerBase>().Install();

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
        public IEnumerator CheckGameObjectActiveState_1()
        {
            TestData testData = TestData.CreateFromJson("Assets/TestAssets/RawTestData/TestData_1.json");
            Camera hlodCamera = mHlodCameraObject.GetComponent<Camera>();
            
            SetUpCamera(hlodCamera, testData.cameraSettings);

            yield return new WaitForSeconds(0.1f);

            CheckGameObjectActiveState(testData.listOfGameObjects);

            yield return null;
        }

        private void SetUpCamera(Camera camera, CameraSettings cameraSettings)
        {
            camera.transform.position = new Vector3(
                cameraSettings.location.x,
                cameraSettings.location.y,
                cameraSettings.location.z);

            camera.transform.eulerAngles = new Vector3(
                cameraSettings.rotation.x,
                cameraSettings.rotation.y + 180,
                cameraSettings.rotation.z);

            HLODManager.Instance.OnPreCull(camera);
        }

        private void CheckGameObjectActiveState(List<PlayModeTestGameObject> listOfGameObjects)
        {
            foreach (PlayModeTestGameObject playModeTestGameObject in listOfGameObjects)
            {
                Transform rinNumbers = mHlodGameObject.transform.Find(playModeTestGameObject.groupName);

                for (int i = 0; i < rinNumbers.childCount; i++)
                {
                    Assert.AreEqual(rinNumbers.GetChild(i).gameObject.activeSelf, playModeTestGameObject.enabled[i]);
                }
            }
        }
    }

    [Serializable]
    public class TestData
    {
        public CameraSettings cameraSettings;
        public List<PlayModeTestGameObject> listOfGameObjects;

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
    public class PlayModeTestGameObject
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