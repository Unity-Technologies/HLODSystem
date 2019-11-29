using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.HLODSystem.EditorTests
{
    public class EditorTests
    {
        private string mHlodArtifactName = "Assets/TestRunner/HLOD.hlod";
        private HLOD hlod;
        private GameObject hlodGameObject;
        private int childrenCount;
        
        [SetUp]
        public void Setup()
        {
            hlodGameObject = GameObject.Find("HLOD");
            hlod = hlodGameObject.GetComponent<HLOD>() as HLOD;
        }
        
        [Test, Order(1)]
        public void HlodGameObjectIsNotNull()
        {
            Assert.NotNull(hlodGameObject);
        }
        
        [Test, Order(2)]
        public void HlodIsNotNull()
        {
            Assert.NotNull(hlod);
        }

        [UnityTest, Order(3)]
        public IEnumerator CreateHlodComponent()
        {
            childrenCount = hlodGameObject.transform.childCount;
            yield return CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
        }
        
        [Test, Order(4)]
        public void HlodRootIsAddedToHlodGroup()
        {
            Assert.True(hlodGameObject.transform.childCount == childrenCount + 1);
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
        public IEnumerator DestroyHlodComponent()
        {
            yield return CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod));
        }
        
        [Test, Order(8)]
        public void DeleteArtifacts()
        {
            File.Delete(mHlodArtifactName);
            Assert.False(File.Exists(mHlodArtifactName));
        }
    }
}
