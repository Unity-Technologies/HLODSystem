using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{

    public interface ISpaceManager
    {
        void UpdateCamera(Transform hlodTransform, Camera cam);

        bool IsHigh(float lodDistance, Bounds bounds);

        bool IsCull(float cullDistance, Bounds bounds);

        float GetDistanceSqure(Bounds bounds);
    }

}