using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public interface IStreamingBuilder
    {
        void Build(HLOD hlod);
        
    }
}