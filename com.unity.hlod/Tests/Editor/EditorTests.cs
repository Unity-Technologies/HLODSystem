using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class EditorTests : IPrebuildSetup
    {
        private static string mHlodArtifactName = "Assets/TestAssets/Artifacts/HLOD.hlod";
        private static HLOD hlod;
        private static GameObject mHlodGameObject;
        private static int childrenCount;

        [OneTimeSetUp]
        public void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/TestAssets/EditModeTestScene.unity");
            GameObject[] gameObjects = scene.GetRootGameObjects();
            mHlodGameObject = gameObjects[0].transform.Find("HLOD").gameObject;
            hlod = mHlodGameObject.GetComponent<HLOD>() as HLOD;
        }

        [Test, Order(1)]
        public void HlodGameObjectIsNotNull()
        {
            Assert.NotNull(mHlodGameObject);
        }

        [Test, Order(2)]
        public void HlodIsNotNull()
        {
            Assert.NotNull(hlod);
        }

        [UnityTest, Order(3)]
        public IEnumerator HlodComponentIsCreated()
        {
            childrenCount = mHlodGameObject.transform.childCount;
            yield return CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
        }

        [Test, Order(4)]
        public void HlodRootIsAddedToHlodGroup()
        {
            Assert.True(mHlodGameObject.transform.childCount == childrenCount + 1);
        }

        [Test, Order(5)]
        public void HlodControllerIsNotNull()
        {
            Assert.NotNull(hlod.GetComponent<HLODControllerBase>());
        }

        [Test, Order(6)]
        public void ArtifactIsCreated()
        {
            Assert.IsTrue(File.Exists(mHlodArtifactName));
        }

        [UnityTest, Order(7)]
        public IEnumerator HlodComponentIsDestroyed()
        {
            yield return CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod));
        }

        [Test, Order(8)]
        public void ArtifactsIsDeleted()
        {
            File.Delete(mHlodArtifactName);
            Assert.False(File.Exists(mHlodArtifactName));
        }

        [UnityTest, Order(8)]
        public IEnumerator HlodAssetIsImported()
        {
            string assetPath = "Assets/TestAssets/BakedTerrainPatch.hlod";
            AssetDatabase.ImportAsset(assetPath);

            Object[] data = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Assert.Greater(data.Length, 0);

            foreach (Object obj in data)
            {
                if (obj is GameObject)
                {
                    Assert.AreEqual(obj.name, "HLOD");
                    break;
                }
            }

            yield return null;
        }
    }
}