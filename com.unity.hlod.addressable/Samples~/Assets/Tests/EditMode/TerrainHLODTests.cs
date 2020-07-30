using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEditor.VersionControl;

namespace Unity.HLODSystem.EditorTests
{
    [TestFixture]
    public class TerrainHLODTests
    {
        GameObject m_gameObject;
        TerrainHLOD m_terrainHLOD;
        TerrainData m_terrainData;


        [SetUp]
        public void Setup()
        {
            m_gameObject = new GameObject();
            m_terrainHLOD = m_gameObject.AddComponent<TerrainHLOD>();
            m_terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TestAssets/TerrainResources/TestTerrain.asset");

            m_terrainHLOD.TerrainData = m_terrainData;
            m_terrainHLOD.ChunkSize = 300.0f;
            m_terrainHLOD.BorderVertexCount = 256;

            m_terrainHLOD.MaterialGUID = AssetDatabase.AssetPathToGUID("Assets/TestAssets/TerrainResources/BakedMaterial.mat");
            m_terrainHLOD.TextureSize = 64;
            m_terrainHLOD.AlbedoPropertyName = "_MainTex";

            dynamic simplifierOptions = m_terrainHLOD.SimplifierOptions;
            simplifierOptions.SimplifyMaxPolygonCount = 99999999;
            simplifierOptions.SimplifyMinPolygonCount = 0;
            simplifierOptions.SimplifyPolygonRatio = 1.0f;

            m_terrainHLOD.SimplifierType = Simplifier.SimplifierTypes.GetTypes()[0];

            dynamic streamingOptions = m_terrainHLOD.StreamingOptions;
            streamingOptions.OutputDirectory = "Assets/TestAssets/Artifacts/";
            streamingOptions.PCCompression = TextureFormat.ARGB32;
            streamingOptions.WebGLCompression = TextureFormat.ARGB32;
            streamingOptions.AndroidCompression = TextureFormat.ARGB32;
            streamingOptions.iOSCompression = TextureFormat.ARGB32;
            streamingOptions.tvOSCompression = TextureFormat.ARGB32;

            m_terrainHLOD.StreamingType = Streaming.StreamingBuilderTypes.GetTypes()[0];

        }
        [TearDown]
        public void Cleanup()
        {
            Utils.CustomCoroutine routine = new Utils.CustomCoroutine(TerrainHLODCreator.Destroy(m_terrainHLOD));

            while (routine.MoveNext())
            {

            }
        }

        [Test]
        public void TextureExistsTest()
        {
            Utils.CustomCoroutine routine = new Utils.CustomCoroutine(TerrainHLODCreator.Create(m_terrainHLOD));


            while (routine.MoveNext())
            {

            }

            string artifactFiles = "Assets/TestAssets/Artifacts/New Game Object.hlod";

            bool isTextureExists = false;
            bool isMaterialExists = false;

            var objects = AssetDatabase.LoadAllAssetsAtPath(artifactFiles);
            for (int oi = 0; oi < objects.Length; ++oi)
            {
                isTextureExists = isTextureExists || (objects[oi] is Texture2D);
                isMaterialExists = isMaterialExists || (objects[oi] is Material);
            }

            Assert.IsTrue(isTextureExists);
            Assert.IsTrue(isMaterialExists);
        }


    }

}