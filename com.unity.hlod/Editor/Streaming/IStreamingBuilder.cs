using System;
using Unity.HLODSystem.Utils;

namespace Unity.HLODSystem.Streaming
{
    public interface IStreamingBuilder
    {
        void Build(SpaceManager.SpaceNode rootNode, DisposableList<HLODBuildInfo> infos, Action<float> onProgress);

    }
}