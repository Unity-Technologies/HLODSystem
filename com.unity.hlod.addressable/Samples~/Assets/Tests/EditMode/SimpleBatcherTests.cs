using NUnit.Framework;

using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class SimpleBatcherTests 
    {
        private const string m_prefabName = "Assets/TestAssets/Prefabs/SimpleBatcherTestPrefab.prefab";
        private HLOD m_hlod;
        private GameObject m_hlodGameObject;

        private string[] m_artifactFiles =
        {
            "Assets/TestAssets/Artifacts/HLOD_group-1.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group0.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group1.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group2.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group3.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group4.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group5.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group6.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group7.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group8.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group9.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group10.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group11.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group12.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group13.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group14.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group15.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group16.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group17.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group18.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group19.hlod",
            "Assets/TestAssets/Artifacts/HLOD_group20.hlod",
        };

        [OneTimeSetUp]
        public void Setup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_prefabName);
            m_hlodGameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(m_hlodGameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            m_hlod = m_hlodGameObject.GetComponentInChildren<HLOD>();
            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Create(m_hlod));

            while (coroutine.MoveNext())
            {
                //Wait until coroutine is finished
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Destroy(m_hlod));

            while (coroutine.MoveNext())
            {

            }
        }

        [Test]
        public void MaterialSetupTest()
        {
            for (int fi = 0; fi < m_artifactFiles.Length; ++fi)
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(m_artifactFiles[fi]);
                for (int oi = 0; oi < objects.Length; ++oi)
                {
                    var go = objects[oi] as GameObject;
                    if (go == null)
                        continue;

                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr == null)
                        continue;

                    Assert.AreNotEqual(0, mr.sharedMaterials.Length);
                }
            }
        }

        [Test]
        public void TextureExtractedTest()
        {
            for (int fi = 0; fi < m_artifactFiles.Length; ++fi)
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(m_artifactFiles[fi]);
                for (int oi = 0; oi < objects.Length; ++oi)
                {
                    Assert.False(objects[oi] is Material);
                    Assert.False(objects[oi] is Texture2D);
                }
            }
        }
    }

}