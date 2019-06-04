using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLODBuildInfo
    {
        public string Name = "";
        public int ParentIndex = -1;
        public SpaceNode Target;

        public List<WorkingObject> WorkingObjects = new List<WorkingObject>();
        public List<int> Distances = new List<int>();
    }   
}