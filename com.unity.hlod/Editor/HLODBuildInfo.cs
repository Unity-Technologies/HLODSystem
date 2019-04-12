using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLODBuildInfo
    {
        public string name = "";
        public int parentIndex = -1;
        public SpaceNode target;

        public List<MeshRenderer> renderers = new List<MeshRenderer>();
        public List<Mesh> simplifiedMeshes = null;
        public List<int> distances = new List<int>();

        public List<GameObject> combinedGameObjects = new List<GameObject>();
    }

}