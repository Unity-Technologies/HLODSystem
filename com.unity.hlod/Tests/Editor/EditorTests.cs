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
    public class EditorTests : IPrebuildSetup, IPostBuildCleanup
    {
        private static string mHlodArtifactName = "Assets/TestAssets/Artifacts/HLOD.hlod";
        private static HLOD hlod;
        private static GameObject mHlodGameObject;
        private static int childrenCount;

        public void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/TestAssets/EditModeTestScene.unity");
            GameObject[] gameObjects = scene.GetRootGameObjects();
            mHlodGameObject = gameObjects[0].transform.Find("HLOD").gameObject;
            hlod = mHlodGameObject.GetComponent<HLOD>() as HLOD;
            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
            
            while (coroutine.MoveNext())
            {
                //Wait until coroutine is finished
            }
        }

        public void Cleanup()
        {
            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod));
            while (coroutine.MoveNext())
            {
                //Wait until coroutine is finished
            }

            File.Delete(mHlodArtifactName);
            Assert.False(File.Exists(mHlodArtifactName));
        }

        [Test]
        public void HlodGameObjectIsNotNull()
        {
            Assert.NotNull(mHlodGameObject);
        }

        [Test]
        public void HlodIsNotNull()
        {
            Assert.NotNull(hlod);
        }

        [TestCase(11)]
        public void HlodRootIsAddedToHlodGroup(int childCount)
        {
            Assert.True(mHlodGameObject.transform.childCount == childCount);
        }

        [Test]
        public void HlodControllerIsNotNull()
        {
            Assert.NotNull(hlod.GetComponent<HLODControllerBase>());
        }

        /*[Test]
        public void ArtifactIsCreated()
        {
            Assert.IsTrue(File.Exists(mHlodArtifactName));
        }*/

        [UnityTest]
        [TestCase("Assets/TestAssets/BakedTerrainPatch.hlod", ExpectedResult = null)]
        public IEnumerator HlodAssetIsImported(string assetPath)
        {
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