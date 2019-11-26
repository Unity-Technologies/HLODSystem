using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.HLODSystem;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using static Unity.HLODSystem.HLOD;

namespace Tests
{
    public class HLODEditModeTests
    {
        private string mHlodArtifactName = "Assets/HLOD.hlod";

        // A Test behaves as an ordinary method
        [Test]
        public void CheckHlodObjectPresentInScene()
        {
            var hlodGameObject = GameObject.Find("HLOD");
            Assert.NotNull(hlodGameObject);
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TestHlodComponentIsCreated()
        {
            if (File.Exists(mHlodArtifactName))
                File.Delete(mHlodArtifactName);

            var hlodGameObject = GameObject.Find("HLOD");
            HLOD hlod = hlodGameObject.GetComponent<HLOD>() as HLOD;

            int childrenCount = hlodGameObject.transform.childCount;

            Assert.NotNull(hlod);
            yield return CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));

            Assert.NotNull(hlod.GetComponent<HLODControllerBase>());

            Assert.IsTrue(File.Exists(mHlodArtifactName));

            Assert.True(hlodGameObject.transform.childCount == childrenCount + 1);

            yield return null;
        }
    }
}