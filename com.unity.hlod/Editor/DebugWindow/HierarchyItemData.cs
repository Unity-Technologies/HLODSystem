using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.DebugWindow
{
    public class HierarchyItemData: ScriptableObject
    {
        public int Index;
        public HLODTreeNode TreeNode;
        public string Label;
        public bool IsOpen;
    }

}