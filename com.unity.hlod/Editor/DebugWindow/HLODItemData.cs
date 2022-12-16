using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Streaming;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.DebugWindow
{
    public class HLODItemData : ScriptableObject
    {
        private HLODControllerBase m_controller;
        [SerializeField]
        private string m_name;
        private List<HLODTreeNode> m_nodes = new List<HLODTreeNode>();
        [SerializeField]
        private bool m_enableDebug = true;
        private List<HierarchyItemData> m_hierarchyItemDatas = new List<HierarchyItemData>();

        public HLODControllerBase Controller
        {
            get
            {
                return m_controller;
            }
        }
        public List<HierarchyItemData> HierarchyItemDatas
        {
            get
            {
                return m_hierarchyItemDatas;
            }
        }
        
        public void Initialize(HLODControllerBase controller)
        {
            Stack<HLODTreeNode> treeNodeTravelStack = new Stack<HLODTreeNode>();
            Stack<string> labelStack = new Stack<string>();

            m_controller = controller;
            m_name = controller.gameObject.name;
            
            treeNodeTravelStack.Push(controller.Root);
            labelStack.Push("");

            while (treeNodeTravelStack.Count > 0)
            {
                var node = treeNodeTravelStack.Pop();
                var label = labelStack.Pop();
                m_hierarchyItemDatas.Add(new HierarchyItemData()
                {
                    Index = m_hierarchyItemDatas.Count,
                    TreeNode = node,
                    Label = label,
                    IsOpen = true,
                });
                m_nodes.Add(node);
                
                for (int i = node.GetChildTreeNodeCount() - 1; i >= 0; --i)
                {
                    treeNodeTravelStack.Push(node.GetChildTreeNode(i));
                    labelStack.Push($"{label}_{i+1}");
                }
            }
        }

        public void CleanUp()
        {
            m_controller = null;
            m_nodes.Clear();
            m_hierarchyItemDatas.Clear();
        }
        
        
        public void Render(DrawMode drawMode)
        {
            if (m_enableDebug == false)
                return;
            
            foreach (var node in m_nodes)
            {
                if (node.CurrentState == HLODTreeNode.State.Low ||
                    (node.CurrentState == HLODTreeNode.State.High && node.GetChildTreeNodeCount() == 0))
                {
                    HLODTreeNodeRenderer.Instance.Render(node, Color.green, 2.0f);
                }
                else if (drawMode == DrawMode.All)
                {
                    HLODTreeNodeRenderer.Instance.Render(node, Color.yellow, 1.0f);
                }
            }
        }

    }

}