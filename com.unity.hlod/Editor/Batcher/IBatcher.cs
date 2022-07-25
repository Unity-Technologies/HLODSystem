using System;
using Unity.HLODSystem.Utils;
using UnityEngine;

namespace Unity.HLODSystem
{
    public interface IBatcher : IDisposable
    {
        
        void Batch(Transform rootTransform, DisposableList<HLODBuildInfo> targets, Action<float> onProgress);

    }
}