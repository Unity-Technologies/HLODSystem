using UnityEngine;

namespace Unity.HLODSystem
{
    public interface IBatcher
    {
        void Batch(GameObject[] roots);

    }
}