using System;
using System.Collections.Generic;

namespace Unity.HLODSystem.Streaming
{
    public interface IStreamingBuilder
    {
        void Build(SpaceManager.SpaceNode rootNode, List<HLODBuildInfo> infos, Action<float> onProgress);

    }
}