using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class EditorTests
    {
        private const string m_prefabName = "Assets/TestAssets/Prefabs/HLODTestPrefab.prefab";
        private HLOD m_hlod;
        private GameObject m_hlodGameObject;
        private int childrenCount;

        [SetUp]
        public void Setup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_prefabName);
            m_hlodGameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(m_hlodGameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            m_hlod = m_hlodGameObject.GetComponentInChildren<HLOD>();

            dynamic streamingOptions = m_hlod.StreamingOptions;
            streamingOptions.AddressablesGroupName = "HLODEditorTest";

            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Create(m_hlod));

            while (coroutine.MoveNext())
            {
                //Wait until coroutine is finished
            }
        }

        [TearDown]
        public void Cleanup()
        {
            var coroutine = CoroutineRunner.RunCoroutine(HLODCreator.Destroy(m_hlod));

            while(coroutine.MoveNext())
            {

            }
        }

        [Test]
        public void ComponentTest()
        {
            Assert.NotNull(m_hlodGameObject);
            Assert.NotNull(m_hlod);
            Assert.NotNull(m_hlod.GetComponent<AddressableHLODController>());
        }

        [Test]
        public void AddressableHLODControllerTest()
        {
            string[] lowAddrs =
            {
                "Assets/TestAssets/Artifacts/HLOD_group-1.hlod[]",
                "Assets/TestAssets/Artifacts/HLOD_group0.hlod[_1]",
                "Assets/TestAssets/Artifacts/HLOD_group0.hlod[_2]",
                "Assets/TestAssets/Artifacts/HLOD_group0.hlod[_3]",
                "Assets/TestAssets/Artifacts/HLOD_group0.hlod[_4]",
                "Assets/TestAssets/Artifacts/HLOD_group1.hlod[_1_1]",
                "Assets/TestAssets/Artifacts/HLOD_group1.hlod[_1_2]",
                "Assets/TestAssets/Artifacts/HLOD_group1.hlod[_1_3]",
                "Assets/TestAssets/Artifacts/HLOD_group1.hlod[_1_4]",
                "Assets/TestAssets/Artifacts/HLOD_group2.hlod[_2_1]",
                "Assets/TestAssets/Artifacts/HLOD_group2.hlod[_2_2]",
                "Assets/TestAssets/Artifacts/HLOD_group2.hlod[_2_3]",
                "Assets/TestAssets/Artifacts/HLOD_group2.hlod[_2_4]",
                "Assets/TestAssets/Artifacts/HLOD_group3.hlod[_3_1]",
                "Assets/TestAssets/Artifacts/HLOD_group3.hlod[_3_2]",
                "Assets/TestAssets/Artifacts/HLOD_group3.hlod[_3_3]",
                "Assets/TestAssets/Artifacts/HLOD_group3.hlod[_3_4]",
                "Assets/TestAssets/Artifacts/HLOD_group4.hlod[_4_1]",
                "Assets/TestAssets/Artifacts/HLOD_group4.hlod[_4_2]",
                "Assets/TestAssets/Artifacts/HLOD_group4.hlod[_4_3]",
                "Assets/TestAssets/Artifacts/HLOD_group4.hlod[_4_4]",
                "Assets/TestAssets/Artifacts/HLOD_group5.hlod[_1_1_1]",
                "Assets/TestAssets/Artifacts/HLOD_group5.hlod[_1_1_2]",
                "Assets/TestAssets/Artifacts/HLOD_group5.hlod[_1_1_3]",
                "Assets/TestAssets/Artifacts/HLOD_group5.hlod[_1_1_4]",
                "Assets/TestAssets/Artifacts/HLOD_group6.hlod[_1_2_1]",
                "Assets/TestAssets/Artifacts/HLOD_group6.hlod[_1_2_2]",
                "Assets/TestAssets/Artifacts/HLOD_group6.hlod[_1_2_3]",
                "Assets/TestAssets/Artifacts/HLOD_group6.hlod[_1_2_4]",
                "Assets/TestAssets/Artifacts/HLOD_group7.hlod[_1_3_1]",
                "Assets/TestAssets/Artifacts/HLOD_group7.hlod[_1_3_2]",
                "Assets/TestAssets/Artifacts/HLOD_group7.hlod[_1_3_3]",
                "Assets/TestAssets/Artifacts/HLOD_group7.hlod[_1_3_4]",
                "Assets/TestAssets/Artifacts/HLOD_group8.hlod[_1_4_1]",
                "Assets/TestAssets/Artifacts/HLOD_group8.hlod[_1_4_2]",
                "Assets/TestAssets/Artifacts/HLOD_group8.hlod[_1_4_3]",
                "Assets/TestAssets/Artifacts/HLOD_group8.hlod[_1_4_4]",
                "Assets/TestAssets/Artifacts/HLOD_group9.hlod[_2_1_1]",
                "Assets/TestAssets/Artifacts/HLOD_group9.hlod[_2_1_2]",
                "Assets/TestAssets/Artifacts/HLOD_group9.hlod[_2_1_3]",
                "Assets/TestAssets/Artifacts/HLOD_group9.hlod[_2_1_4]",
                "Assets/TestAssets/Artifacts/HLOD_group10.hlod[_2_2_1]",
                "Assets/TestAssets/Artifacts/HLOD_group10.hlod[_2_2_2]",
                "Assets/TestAssets/Artifacts/HLOD_group10.hlod[_2_2_3]",
                "Assets/TestAssets/Artifacts/HLOD_group10.hlod[_2_2_4]",
                "Assets/TestAssets/Artifacts/HLOD_group11.hlod[_2_3_1]",
                "Assets/TestAssets/Artifacts/HLOD_group11.hlod[_2_3_2]",
                "Assets/TestAssets/Artifacts/HLOD_group11.hlod[_2_3_3]",
                "Assets/TestAssets/Artifacts/HLOD_group11.hlod[_2_3_4]",
                "Assets/TestAssets/Artifacts/HLOD_group12.hlod[_2_4_1]",
                "Assets/TestAssets/Artifacts/HLOD_group12.hlod[_2_4_2]",
                "Assets/TestAssets/Artifacts/HLOD_group12.hlod[_2_4_3]",
                "Assets/TestAssets/Artifacts/HLOD_group12.hlod[_2_4_4]",
                "Assets/TestAssets/Artifacts/HLOD_group13.hlod[_3_1_1]",
                "Assets/TestAssets/Artifacts/HLOD_group13.hlod[_3_1_2]",
                "Assets/TestAssets/Artifacts/HLOD_group13.hlod[_3_1_3]",
                "Assets/TestAssets/Artifacts/HLOD_group13.hlod[_3_1_4]",
                "Assets/TestAssets/Artifacts/HLOD_group14.hlod[_3_2_1]",
                "Assets/TestAssets/Artifacts/HLOD_group14.hlod[_3_2_2]",
                "Assets/TestAssets/Artifacts/HLOD_group14.hlod[_3_2_3]",
                "Assets/TestAssets/Artifacts/HLOD_group14.hlod[_3_2_4]",
                "Assets/TestAssets/Artifacts/HLOD_group15.hlod[_3_3_1]",
                "Assets/TestAssets/Artifacts/HLOD_group15.hlod[_3_3_2]",
                "Assets/TestAssets/Artifacts/HLOD_group15.hlod[_3_3_3]",
                "Assets/TestAssets/Artifacts/HLOD_group15.hlod[_3_3_4]",
                "Assets/TestAssets/Artifacts/HLOD_group16.hlod[_3_4_1]",
                "Assets/TestAssets/Artifacts/HLOD_group16.hlod[_3_4_2]",
                "Assets/TestAssets/Artifacts/HLOD_group16.hlod[_3_4_3]",
                "Assets/TestAssets/Artifacts/HLOD_group16.hlod[_3_4_4]",
                "Assets/TestAssets/Artifacts/HLOD_group17.hlod[_4_1_1]",
                "Assets/TestAssets/Artifacts/HLOD_group17.hlod[_4_1_2]",
                "Assets/TestAssets/Artifacts/HLOD_group17.hlod[_4_1_3]",
                "Assets/TestAssets/Artifacts/HLOD_group17.hlod[_4_1_4]",
                "Assets/TestAssets/Artifacts/HLOD_group18.hlod[_4_2_1]",
                "Assets/TestAssets/Artifacts/HLOD_group18.hlod[_4_2_2]",
                "Assets/TestAssets/Artifacts/HLOD_group18.hlod[_4_2_3]",
                "Assets/TestAssets/Artifacts/HLOD_group18.hlod[_4_2_4]",
                "Assets/TestAssets/Artifacts/HLOD_group19.hlod[_4_3_1]",
                "Assets/TestAssets/Artifacts/HLOD_group19.hlod[_4_3_2]",
                "Assets/TestAssets/Artifacts/HLOD_group19.hlod[_4_3_3]",
                "Assets/TestAssets/Artifacts/HLOD_group19.hlod[_4_3_4]",
                "Assets/TestAssets/Artifacts/HLOD_group20.hlod[_4_4_1]",
                "Assets/TestAssets/Artifacts/HLOD_group20.hlod[_4_4_2]",
                "Assets/TestAssets/Artifacts/HLOD_group20.hlod[_4_4_3]",
                "Assets/TestAssets/Artifacts/HLOD_group20.hlod[_4_4_4]"
            };

            var controller = m_hlod.GetComponent<AddressableHLODController>();
            Assert.AreEqual(84, controller.Container.Count);
            Assert.AreEqual(100, controller.HighObjectCount);
            Assert.AreEqual(85, controller.LowObjectCount);

            for ( int i = 0; i < 85; ++i )
            {
                Assert.AreEqual(lowAddrs[i], controller.GetLowObjectAddr(i));
            }
        }

        [Test]
        public void AddressableTest()
        {
            var controller = m_hlod.GetComponent<AddressableHLODController>();
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            Assert.NotNull(settings);
            Assert.AreEqual(85, controller.LowObjectCount);

            var group = settings.FindGroup("HLODEditorTest");
            Assert.NotNull(group);

            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            group.GatherAllAssets(entries, true, false, false);
            Assert.AreEqual(22, entries.Count);

            List<AddressableAssetEntry> allEntries = new List<AddressableAssetEntry>();
            group.GatherAllAssets(allEntries, false, false, true);
            for ( int i = 0; i < 85; ++i )
            {
                Assert.IsTrue(AddressableResourceExists(controller.GetLowObjectAddr(i), allEntries));
            }

            

        }

        private static bool AddressableResourceExists(string key, List<AddressableAssetEntry> assetEntries)
        {
            for (int i = 0; i < assetEntries.Count; ++i )
            {
                if (assetEntries[i].address == key)
                    return true;
            }
            
            return false;
        }



    }
}