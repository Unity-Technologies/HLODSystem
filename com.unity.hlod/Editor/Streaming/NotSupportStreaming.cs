using System.IO;
using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

namespace Unity.HLODSystem.Streaming
{
    class NotSupportStreaming : IStreamingBuilder
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            StreamingBuilderTypes.RegisterType(typeof(NotSupportStreaming), -1);
        }
        public void Build(HLOD hlod)
        {
            string path = "";
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(hlod.gameObject);
            path = stage.prefabAssetPath;
            path = Path.GetDirectoryName(path) + "/";            

            if (hlod.LowRoot != null)
            {
                var controller = hlod.LowRoot.AddComponent<DefaultController>();

                List<HLODMesh> createdMeshes = ObjectUtils.SaveHLODMesh(path, hlod.name, hlod.LowRoot);
                controller.AddHLODMeshes(createdMeshes);
            }

            if (hlod.HighRoot != null)
            {
                hlod.HighRoot.AddComponent<DefaultController>();
            }

            PrefabUtils.SavePrefab(path, hlod);
        }
    }
}
