using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = System.Object;
#if ENABLE_SIMPLYGON
using Simplygon.Unity.EditorPlugin;
using Simplygon.Unity.EditorPlugin.Jobs;
#endif

[assembly:Unity.HLODSystem.OptionalDependency("Simplygon.Unity.EditorPlugin.Window", "ENABLE_SIMPLYGON")]
#if ENABLE_SIMPLYGON
namespace Unity.HLODSystem.Simplifier
{
    class SimplygonSimplifier : SimplifierBase
    {
        private const string lodPath = "Assets/LODs/";
        private const string materialPath = lodPath + "Materials";
    
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            SimplifierTypes.RegisterType(typeof(SimplygonSimplifier));
        }

        protected override IEnumerator GetSimplifiedMesh(Mesh origin, float quality, Action<Mesh> resultCallback)
        {
            Renderer renderer = null;

            UnityCloudJob job = null;
            string jobName = null;

            var assembly = typeof(SharedData).Assembly;
            var cloudJobType = assembly.GetType("Simplygon.Cloud.Yoda.IntegrationClient.CloudJob");
            var jobNameField = cloudJobType.GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);

            var go = EditorUtility.CreateGameObjectWithHideFlags("Temp", HideFlags.HideAndDontSave,
                    typeof(MeshRenderer), typeof(MeshFilter));
            var mf = go.GetComponent<MeshFilter>();
            var mesh = UnityEngine.Object.Instantiate(origin);
            mf.sharedMesh = mesh;
            renderer = go.GetComponent<MeshRenderer>();

            var sharedMaterials = new Material[mesh.subMeshCount];

            if (Directory.Exists(materialPath) == false)
            {
                Directory.CreateDirectory(materialPath);
            }

            //For submesh, we should create material asset.
            //otherwise, simplygon will be combine uv of submesh.
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var material = new Material(Shader.Find("Standard"));
                material.name = "Material " + i.ToString();

                AssetDatabase.CreateAsset(material, materialPath + "/" + material.name);

                sharedMaterials[i] = material;
            }

            renderer.sharedMaterials = sharedMaterials;
            renderer.enabled = false;

            EditorWindow.GetWindow<Window>(); // Must be visible for background processing

            SharedData.Instance.Settings.SetDownloadAssetsAutomatically(true);

            var lodChainProperty = typeof(SharedData).GetProperty("LODChain");
            var lodChainList = lodChainProperty.GetValue(SharedData.Instance, null) as IList;
            var lodChain = lodChainList[0];

            var processNodeType = assembly.GetType("Simplygon.SPL.v80.Node.ProcessNode");
            var processorProperty = processNodeType.GetProperty("Processor");
            var processor = processorProperty.GetValue(lodChain, null);

            var reductionProcessorType = assembly.GetType("Simplygon.SPL.v80.Processor.ReductionProcessor");
            var reductionSettingsProperty = reductionProcessorType.GetProperty("ReductionSettings");
            var reductionSettingsType = assembly.GetType("Simplygon.SPL.v80.Settings.ReductionSettings");
            var reductionSettings = reductionSettingsProperty.GetValue(processor, null);

            var triangleRatioProperty = reductionSettingsType.GetProperty("TriangleRatio");
            triangleRatioProperty.SetValue(reductionSettings, quality, null);

            jobName = Path.GetRandomFileName().Replace(".", string.Empty);
            var prefabList = PrefabUtilityEx.GetPrefabsForSelection(new List<GameObject>() { go });
            var generalManager = SharedData.Instance.GeneralManager;
            generalManager.CreateJob(jobName, "myPriority", prefabList, () =>
            {
                foreach (var j in generalManager.JobManager.Jobs)
                {
                    var name = (string)jobNameField.GetValue(j.CloudJob);
                    if (name == jobName)
                        job = j;
                }
            });

            while (job == null)
            {
                yield return null;

            }


            while (string.IsNullOrEmpty(job.AssetDirectory))
            {
                yield return null;
            }

            var customDataType = assembly.GetType("Simplygon.Cloud.Yoda.IntegrationClient.CloudJob+CustomData");
            var pendingFolderNameProperty = customDataType.GetProperty("UnityPendingLODFolderName");
            var jobCustomDataProperty = cloudJobType.GetProperty("JobCustomData");
            var jobCustomData = jobCustomDataProperty.GetValue(job.CloudJob, null);
            var jobFolderName = pendingFolderNameProperty.GetValue(jobCustomData, null) as string;

            var lodAssetDir = lodPath + job.AssetDirectory;
            var simplifiedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(string.Format("{0}/{1}_LOD1.prefab", lodAssetDir,
                jobName));

            Mesh resultMesh = UnityEngine.Object.Instantiate(simplifiedMesh);

            //job.CloudJob.StateHandler.RequestJobDeletion();
            AssetDatabaseEx.DeletePendingLODFolder(jobFolderName);
            AssetDatabase.DeleteAsset(lodAssetDir);
            AssetDatabase.DeleteAsset(materialPath);

            UnityEngine.Object.DestroyImmediate(renderer.gameObject);

            if (resultCallback != null)
            {
                resultCallback(resultMesh);
            }
        }

        public static void OnGUI(HLOD hlod)
        {
            OnGUIBase(hlod);
        }
    }
}
#endif