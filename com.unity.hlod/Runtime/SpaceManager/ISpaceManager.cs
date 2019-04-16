using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{

    public interface ISpaceManager
    {
        void UpdateCamera(Camera cam);

        bool IsHigh(Bounds bounds);

        bool IsCull(Bounds bounds);
    }

}