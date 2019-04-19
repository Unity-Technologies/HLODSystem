using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{
    public interface ISpaceSplitter
    {
        /**
         * @return created root space tree
         */
        SpaceNode CreateSpaceTree(Bounds initBounds, List<GameObject> targetObjects, Action<float> onProgress);
    }

}