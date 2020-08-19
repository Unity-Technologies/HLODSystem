using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.HLODSystem.RuntimeTests
{
    [TestFixture]
    public class TerrainDestroyTest : IPrebuildSetup
    {
        //This should be implemented in pre build step.
        //That is why this test need to build
        public void Setup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestAssets/Terrain/BakedTerrainPrefab.prefab");
            var gameObject = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Assert.NotNull(gameObject);
        }

        [Test]
        public void TerrainExistsTest()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            Assert.AreEqual(2, rootObjects.Length);

            var terrain = rootObjects[1].GetComponentInChildren<Terrain>();
            Assert.IsNull(terrain);
        }
    }
}