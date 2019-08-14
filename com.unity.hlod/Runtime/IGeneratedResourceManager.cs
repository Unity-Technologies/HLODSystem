using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public interface IGeneratedResourceManager
    {
        void AddGeneratedResource(Object obj);
        bool IsGeneratedResource(Object obj);

        void AddConvertedPrefabResource(GameObject obj);

    }
}