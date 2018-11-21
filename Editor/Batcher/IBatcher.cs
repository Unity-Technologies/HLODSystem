using UnityEngine;

namespace Unity.HLODSystem
{
    public interface IBatcher
    {
        void Batch(HLOD rootHlod, GameObject[] targets);

    }
}