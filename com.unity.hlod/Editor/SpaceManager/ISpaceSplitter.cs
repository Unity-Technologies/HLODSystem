using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{
    public interface ISpaceSplitter
    {
        int CalculateSubTreeCount(Bounds bounds);
        int CalculateTreeDepth(Bounds bounds, float chunkSize);
        /**
         * @return created root space tree
         */
        List<SpaceNode> CreateSpaceTree(Bounds initBounds, float chunkSize, Transform transform, List<GameObject> targetObjects, Action<float> onProgress);
    }

}