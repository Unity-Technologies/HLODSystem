using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Streaming;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.HLODSystem.DebugWindow
{
    public class HLODItem : VisualElement
    {
        private static readonly string s_uxmlGuid = "a3d94d4fe01e43d4eb8f2fc24c533851";

        private HLODDebugWindow m_window;
        private HLODControllerBase m_controller;

        private ListView m_hierarchyView;

        private List<HLODTreeNode> m_nodes = new List<HLODTreeNode>();

        private bool m_enableDebug;
        
        public HLODItem(HLODDebugWindow window)
        {
            var uxmlPath = AssetDatabase.GUIDToAssetPath(s_uxmlGuid);
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

            var root = template.CloneTree();
            Add(root);

            m_window = window;
            
            
            var ping = this.Q<Button>("Ping");
            ping.clickable.clicked += PingOnclicked;

            var enableDebugUI = this.Q<Toggle>("EnableDebug");
            enableDebugUI.RegisterValueChangedCallback(EnableDebugValueChanged);
            
            m_hierarchyView = this.Q<ListView>("Hierarchy");

            m_hierarchyView.makeItem += HierarchyMakeItem;
            m_hierarchyView.bindItem += HierarchyBindItem;
            
            m_hierarchyView.onSelectionChange += HierarchyViewOnSelectionChange;
        }

        private void EnableDebugValueChanged(ChangeEvent<bool> evt)
        {
            m_enableDebug = evt.newValue;
        }


        public void BindController(HLODControllerBase controller)
        {
            m_controller = controller;
            
            var lable = this.Q<Label>("Label");
            
            this.Bind(new SerializedObject(controller));
            lable.Bind(new SerializedObject(controller.gameObject));

            List<HierarchyItemData> itemDatas = new List<HierarchyItemData>();
            Stack<HLODTreeNode> treeNodeTravelStack = new Stack<HLODTreeNode>();
            Stack<string> labelStack = new Stack<string>();
            
            treeNodeTravelStack.Push(m_controller.Root);
            labelStack.Push("");

            while (treeNodeTravelStack.Count > 0)
            {
                var node = treeNodeTravelStack.Pop();
                var label = labelStack.Pop();
                itemDatas.Add(new HierarchyItemData()
                {
                    Index = itemDatas.Count,
                    TreeNode = node,
                    Label = label,
                });
                m_nodes.Add(node);
                
                for (int i = node.GetChildTreeNodeCount() - 1; i >= 0; --i)
                {
                    treeNodeTravelStack.Push(node.GetChildTreeNode(i));
                    labelStack.Push($"{label}_{i+1}");
                }
            }

            m_hierarchyView.itemsSource = itemDatas;
        }

        public void UnbindController()
        {
            m_nodes.Clear();
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

        private VisualElement HierarchyMakeItem()
        {
            return new HierarchyItem(m_window, m_hierarchyView);
        }
        private void HierarchyBindItem(VisualElement element, int i)
        {
            var data = m_hierarchyView.itemsSource[i] as HierarchyItemData;
            var item = element as HierarchyItem;

            if (item == null || data == null)
                return;

            data.Item = item;
            
            item.BindTreeNode(data);
        }
        private void HierarchyViewOnSelectionChange(IEnumerable<object> selectedItems)
        {
            m_window.ClearSelectTreeNodes();
            
            foreach (var selectedItem in selectedItems)
            {
                var data = selectedItem as HierarchyItemData;
                m_window.AddSelectTreeNode(data.TreeNode);
            }
        }
        
        private void PingOnclicked()
        {
            EditorGUIUtility.PingObject(m_controller);
        }
    }

}