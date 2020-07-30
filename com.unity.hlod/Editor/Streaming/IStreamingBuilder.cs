using System;
using Unity.HLODSystem.Utils;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public interface IStreamingBuilder
    {
        void Build(SpaceManager.SpaceNode rootNode, DisposableList<HLODBuildInfo> infos, GameObject root, 
            float cullDistance, float lodDistance, bool writeNoPrefab, bool extractMaterial, Action<float> onProgress);

    }
}