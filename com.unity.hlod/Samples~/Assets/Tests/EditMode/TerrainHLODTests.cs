using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

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
            m_terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TerrainHLOD/TestResource/TestTerrain.asset");

            m_terrainHLOD.TerrainData = m_terrainData;
            m_terrainHLOD.ChunkSize = 300.0f;
            m_terrainHLOD.BorderVertexCount = 256;

            m_terrainHLOD.MaterialGUID = AssetDatabase.AssetPathToGUID("Assets/TerrainHLOD/TestResource/BakedMaterial.mat");
            m_terrainHLOD.TextureSize = 64;
            m_terrainHLOD.AlbedoPropertyName = "_MainTex";

            dynamic simplifierOptions = m_terrainHLOD.SimplifierOptions;
            simplifierOptions.SimplifyMaxPolygonCount = 99999999;
            simplifierOptions.SimplifyMinPolygonCount = 0;
            simplifierOptions.SimplifyPolygonRatio = 1.0f;

            m_terrainHLOD.SimplifierType = Simplifier.SimplifierTypes.GetTypes()[0];

            dynamic streamingOptions = m_terrainHLOD.StreamingOptions;
            streamingOptions.OutputDirectory = "Assets/TestResult/";
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
        public void BasicTest()
        {
            Utils.CustomCoroutine routine = new Utils.CustomCoroutine (TerrainHLODCreator.Create(m_terrainHLOD));
            

            while (routine.MoveNext())
            {

            }

            Assert.AreEqual(5, m_terrainHLOD.transform.childCount);
            Assert.AreEqual(1, m_terrainHLOD.transform.GetChild(0).childCount);

            var object0 = m_terrainHLOD.transform.GetChild(0).GetChild(0);
            var object1 = m_terrainHLOD.transform.GetChild(1);
            var object2 = m_terrainHLOD.transform.GetChild(2);
            var object3 = m_terrainHLOD.transform.GetChild(3);
            var object4 = m_terrainHLOD.transform.GetChild(4);

            var mesh0 = object0.GetComponent<MeshFilter>().sharedMesh;
            var mesh1 = object1.GetComponent<MeshFilter>().sharedMesh;
            var mesh2 = object2.GetComponent<MeshFilter>().sharedMesh;
            var mesh3 = object3.GetComponent<MeshFilter>().sharedMesh;
            var mesh4 = object4.GetComponent<MeshFilter>().sharedMesh;

            int halfHeightmapResolution = m_terrainData.heightmapResolution / 2 + 1;
            Assert.AreEqual(m_terrainData.heightmapResolution * m_terrainData.heightmapResolution, mesh0.vertices.Length);
            Assert.AreEqual(halfHeightmapResolution * halfHeightmapResolution, mesh1.vertices.Length);
            Assert.AreEqual(halfHeightmapResolution * halfHeightmapResolution, mesh2.vertices.Length);
            Assert.AreEqual(halfHeightmapResolution * halfHeightmapResolution, mesh3.vertices.Length);
            Assert.AreEqual(halfHeightmapResolution * halfHeightmapResolution, mesh4.vertices.Length);

            Assert.Less(0.01f, Mathf.Abs(mesh0.bounds.center.x - 250.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh0.bounds.center.z - 250.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh0.bounds.size.x - 250.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh0.bounds.size.z - 250.0f));

            Assert.Less(0.01f, Mathf.Abs(mesh1.bounds.center.x - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh1.bounds.center.z - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh1.bounds.size.x - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh1.bounds.size.z - 125.0f));

            Assert.Less(0.01f, Mathf.Abs(mesh2.bounds.center.x - 375.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh2.bounds.center.z - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh2.bounds.size.x - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh2.bounds.size.z - 125.0f));

            Assert.Less(0.01f, Mathf.Abs(mesh3.bounds.center.x - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh3.bounds.center.z - 375.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh3.bounds.size.x - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh3.bounds.size.z - 125.0f));

            Assert.Less(0.01f, Mathf.Abs(mesh4.bounds.center.x - 375.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh4.bounds.center.z - 375.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh4.bounds.size.x - 125.0f));
            Assert.Less(0.01f, Mathf.Abs(mesh4.bounds.size.z - 125.0f));

            CompareTexture(object0.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_Albedo.texture2D");
            CompareTexture(object1.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_1_Albedo.texture2D");
            CompareTexture(object2.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_2_Albedo.texture2D");
            CompareTexture(object3.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_3_Albedo.texture2D");
            CompareTexture(object4.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_4_Albedo.texture2D");
        }

        [Test]
        public void SimplifierTest()
        {
            m_terrainHLOD.SimplifierType = Simplifier.SimplifierTypes.GetTypes()[1];

            dynamic simplifierOptions = m_terrainHLOD.SimplifierOptions;
            simplifierOptions.SimplifyMaxPolygonCount = 1000;
            simplifierOptions.SimplifyMinPolygonCount = 0;
            simplifierOptions.SimplifyPolygonRatio = 1.0f;

            Utils.CustomCoroutine routine = new Utils.CustomCoroutine(TerrainHLODCreator.Create(m_terrainHLOD));


            while (routine.MoveNext())
            {

            }

            Assert.AreEqual(5, m_terrainHLOD.transform.childCount);
            Assert.AreEqual(1, m_terrainHLOD.transform.GetChild(0).childCount);

            var object0 = m_terrainHLOD.transform.GetChild(0).GetChild(0);
            var object1 = m_terrainHLOD.transform.GetChild(1);
            var object2 = m_terrainHLOD.transform.GetChild(2);
            var object3 = m_terrainHLOD.transform.GetChild(3);
            var object4 = m_terrainHLOD.transform.GetChild(4);

            var mesh0 = object0.GetComponent<MeshFilter>().sharedMesh;
            var mesh1 = object1.GetComponent<MeshFilter>().sharedMesh;
            var mesh2 = object2.GetComponent<MeshFilter>().sharedMesh;
            var mesh3 = object3.GetComponent<MeshFilter>().sharedMesh;
            var mesh4 = object4.GetComponent<MeshFilter>().sharedMesh;

            Assert.AreEqual(2562, mesh0.vertices.Length);
            Assert.AreEqual(1542, mesh1.vertices.Length);
            Assert.AreEqual(1545, mesh2.vertices.Length);
            Assert.AreEqual(1543, mesh3.vertices.Length);
            Assert.AreEqual(1543, mesh4.vertices.Length);
        }

        [Test]
        public void TextureSizeTest()
        {
            m_terrainHLOD.TextureSize = 128;

            Utils.CustomCoroutine routine = new Utils.CustomCoroutine(TerrainHLODCreator.Create(m_terrainHLOD));

            while (routine.MoveNext())
            {

            }

            Assert.AreEqual(5, m_terrainHLOD.transform.childCount);
            Assert.AreEqual(1, m_terrainHLOD.transform.GetChild(0).childCount);

            var object0 = m_terrainHLOD.transform.GetChild(0).GetChild(0);
            var object1 = m_terrainHLOD.transform.GetChild(1);
            var object2 = m_terrainHLOD.transform.GetChild(2);
            var object3 = m_terrainHLOD.transform.GetChild(3);
            var object4 = m_terrainHLOD.transform.GetChild(4);


            CompareTexture(object0.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_Albedo 128.texture2D");
            CompareTexture(object1.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_1_Albedo 128.texture2D");
            CompareTexture(object2.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_2_Albedo 128.texture2D");
            CompareTexture(object3.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_3_Albedo 128.texture2D");
            CompareTexture(object4.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D, "Assets/TestResult/HLOD_4_Albedo 128.texture2D");
        }

        [Test]
        public void NormalMapTest()
        {
            m_terrainHLOD.UseNormal = true;
            m_terrainHLOD.NormalPropertyName = "_BumpMap";

            Utils.CustomCoroutine routine = new Utils.CustomCoroutine(TerrainHLODCreator.Create(m_terrainHLOD));

            while (routine.MoveNext())
            {

            }

            Assert.AreEqual(5, m_terrainHLOD.transform.childCount);
            Assert.AreEqual(1, m_terrainHLOD.transform.GetChild(0).childCount);

            var object0 = m_terrainHLOD.transform.GetChild(0).GetChild(0);
            var object1 = m_terrainHLOD.transform.GetChild(1);
            var object2 = m_terrainHLOD.transform.GetChild(2);
            var object3 = m_terrainHLOD.transform.GetChild(3);
            var object4 = m_terrainHLOD.transform.GetChild(4);


            CompareTexture(object0.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BumpMap") as Texture2D, "Assets/TestResult/HLOD_Normal.texture2D");
            CompareTexture(object1.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BumpMap") as Texture2D, "Assets/TestResult/HLOD_1_Normal.texture2D");
            CompareTexture(object2.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BumpMap") as Texture2D, "Assets/TestResult/HLOD_2_Normal.texture2D");
            CompareTexture(object3.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BumpMap") as Texture2D, "Assets/TestResult/HLOD_3_Normal.texture2D");
            CompareTexture(object4.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BumpMap") as Texture2D, "Assets/TestResult/HLOD_4_Normal.texture2D");
        }
        
        [Test]
        public void PrefabTest()
        {
            Utils.CustomCoroutine routine = new Utils.CustomCoroutine(TerrainHLODCreator.Create(m_terrainHLOD));

            while (routine.MoveNext())
            {

            }
            
            Assert.AreEqual(5, m_terrainHLOD.transform.childCount);
            
            var object1 = m_terrainHLOD.transform.GetChild(1);
            var object2 = m_terrainHLOD.transform.GetChild(2);
            var object3 = m_terrainHLOD.transform.GetChild(3);
            var object4 = m_terrainHLOD.transform.GetChild(4);
            
            Assert.True(PrefabUtility.IsPartOfAnyPrefab(object1));
            Assert.True(PrefabUtility.IsPartOfAnyPrefab(object2));
            Assert.True(PrefabUtility.IsPartOfAnyPrefab(object3));
            Assert.True(PrefabUtility.IsPartOfAnyPrefab(object4));
        }

        private void CompareTexture(Texture2D texture, string targetTextureFilename)
        {
            var targetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetTextureFilename);
            var targetPixels = targetTexture.GetPixels();
            var pixels = texture.GetPixels();

            Assert.AreEqual(targetTexture.width, texture.width);
            Assert.AreEqual(targetTexture.height, texture.height);
            Assert.AreEqual(targetPixels.Length, pixels.Length);

            for ( int i = 0; i < pixels.Length; ++i )
            {
                Assert.AreEqual(targetPixels[i], pixels[i]);
            }
        }

    }

}