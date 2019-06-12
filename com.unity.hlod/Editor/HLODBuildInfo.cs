using System;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;

namespace Unity.HLODSystem
{
    public class HLODBuildInfo : IDisposable
    {
        public string Name = "";
        public int ParentIndex = -1;
        public SpaceNode Target;

        public DisposableList<WorkingObject> WorkingObjects = new DisposableList<WorkingObject>();
        public List<int> Distances = new List<int>();

        public void Dispose()
        {
            WorkingObjects.Dispose();
        }
    }   
}