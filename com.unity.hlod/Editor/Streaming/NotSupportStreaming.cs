using UnityEditor;

namespace Unity.HLODSystem.Streaming
{
    class NotSupportStreaming : IStreamingBuilder
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            StreamingBuilderTypes.RegisterType(typeof(NotSupportStreaming), -1);
        }
        public void Build(HLOD hlod, bool isRoot)
        {
            if (hlod.LowRoot != null)
            {
                hlod.LowRoot.AddComponent<DefaultController>();
            }

            if (hlod.HighRoot != null)
            {
                hlod.HighRoot.AddComponent<DefaultController>();
            }

            Utils.PrefabUtils.SavePrefab(hlod);
        }
    }
}
