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
    [TestFixture]
    public class RuntimeTests //: IPrebuildSetup, IPostBuildCleanup
    {
        private GameObject mGameObject;
        private GameObject mHlodGameObject;
        private HLODControllerBase mHlodController;

        private Camera mHlodCameraComponent;

        [SetUp]
        public void Setup()
        {
            mGameObject =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Prefabs/HLODTestPrefabBaked.prefab");
            mGameObject = GameObject.Instantiate(mGameObject, Vector3.zero, Quaternion.identity);
            Assert.NotNull(mGameObject);

            mHlodGameObject = mGameObject.transform.Find("HLOD").gameObject;
            Assert.NotNull(mHlodGameObject);

            mHlodController = mHlodGameObject.GetComponentInChildren<HLODControllerBase>();
            mHlodController.Install();

            mHlodController.Awake();
            mHlodController.Start();
            mHlodController.OnEnable();

            mHlodCameraComponent = HLODCameraRecognizer.RecognizedCamera;
            Assert.NotNull(mHlodCameraComponent);

        }

        [TearDown]
        public void Cleanup()
        {
            mHlodController.OnDisable();
            mHlodController.OnDestroy();


            Object.Destroy(mHlodGameObject);
            Object.Destroy(mGameObject);
        }

     

        [Test]
        public void HlodGameObjectHasChildren()
        {
            int childrenCount = mHlodGameObject.transform.childCount;
            Assert.AreEqual(11, childrenCount);
        }

        [Test]
        public void CameraHasHlodCameraRecognizerComponent()
        {
            Assert.NotNull(mHlodCameraComponent);
        }

        [UnityTest]
        [TestCase("Assets/TestAssets/RawTestData/TestData_1.json", ExpectedResult = null)]
        [TestCase("Assets/TestAssets/RawTestData/TestData_2.json", ExpectedResult = null)]
        [TestCase("Assets/TestAssets/RawTestData/TestData_3.json", ExpectedResult = null)]
        [TestCase("Assets/TestAssets/RawTestData/TestData_4.json", ExpectedResult = null)]
        [TestCase("Assets/TestAssets/RawTestData/TestData_5.json", ExpectedResult = null)]
        public IEnumerator HlodKickedForSpecificObjects(string testDataPath)
        {
            TestData testData = TestData.CreateFromJson(testDataPath);

            SetUpCamera(testData.cameraSettings);

            var cam = mHlodCameraComponent;
            
            while (mHlodController.IsLoadDone() == false)
            {
                HLODManager.Instance.OnPreCull(cam);
                yield return null;
            }
            
            CheckGameObjectActiveState(testData.listOfGameObjects);
            CheckHlodObjectsActiveState(testData.listOfActiveHlods);

            yield return null;
        }

        private void SetUpCamera(CameraSettings cameraSettings)
        {
            Camera hlodCamera = mHlodCameraComponent;

            hlodCamera.transform.position = new Vector3(
                cameraSettings.location.x,
                cameraSettings.location.y,
                cameraSettings.location.z);

            hlodCamera.transform.eulerAngles = new Vector3(
                cameraSettings.rotation.x,
                cameraSettings.rotation.y,
                cameraSettings.rotation.z);

            HLODManager.Instance.OnPreCull(hlodCamera);
        }

        private void CheckGameObjectActiveState(List<PlayModeTestGameObject> listOfGameObjects)
        {
            foreach (PlayModeTestGameObject playModeTestGameObject in listOfGameObjects)
            {
                Transform rinNumbers = mHlodGameObject.transform.Find(playModeTestGameObject.groupName);

                for (int i = 0; i < rinNumbers.childCount; i++)
                    Assert.AreEqual(playModeTestGameObject.enabled[i], rinNumbers.GetChild(i).gameObject.activeSelf);
            }
        }

        private void CheckHlodObjectsActiveState(List<string> listOfActiveHlods)
        {
            //Just in case when this list gets too big in future TestCases
            HashSet<string> hashSet = new HashSet<string>(listOfActiveHlods);

            Transform hlods = mHlodGameObject.transform.Find("HLODMeshesRoot");

            foreach (Transform child in hlods.transform)
                Assert.AreEqual(child.gameObject.activeSelf, hashSet.Contains(child.gameObject.name));
        }
    }

    [Serializable]
    public class TestData
    {
        public CameraSettings cameraSettings;
        public List<PlayModeTestGameObject> listOfGameObjects;
        public List<string> listOfActiveHlods;

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