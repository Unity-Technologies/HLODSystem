using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public interface IBatcher
    {
        void Batch(GameObject root);

    }
}