using Unity.HLODSystem.Streaming;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.HLODSystem.DebugWindow
{
    public class HierarchyItem : VisualElement
    {
        private static readonly string s_uxmlGuid = "7b9b7a1f48292534bb048103f56e3404";


        private HierarchyItemData m_data;

        private HLODDebugWindow m_window;
        private ListView m_hierarchyView;
        
        private UQueryBuilder<VisualElement> m_treeOffset;
        private Toggle m_foldoutToggle;
        private Label m_name;
        private VisualElement m_root;
        
        public HierarchyItem(HLODDebugWindow window, ListView hierarchyView)
        {
            m_window = window;
            m_hierarchyView = hierarchyView;
            
            var uxmlPath = AssetDatabase.GUIDToAssetPath(s_uxmlGuid);
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

            m_root = template.CloneTree();
            Add(m_root);

            m_treeOffset = m_root.Query<VisualElement>("TreeOffset");
            m_name = m_root.Q<Label>("Name");
            m_foldoutToggle = m_root.Q<Toggle>("Foldout");
            m_foldoutToggle.RegisterValueChangedCallback(FoldoutValueChanged);
            
            EditorApplication.update += Update;
        }

        private void Update()
        {
            var node = m_data.TreeNode;
            var isRendered = false;

            if (m_window.HighlightSelected)
            {
                isRendered = node.CurrentState == HLODTreeNode.State.Low ||
                             (node.CurrentState == HLODTreeNode.State.High && node.GetChildTreeNodeCount() == 0);
            }

            if (isRendered)
            {
                m_root.AddToClassList("HLODTreeNode_rendered");
            }
            else
            {
                m_root.RemoveFromClassList("HLODTreeNode_rendered");
            }
        }

        public void BindTreeNode(HierarchyItemData data)
        {
            m_data = data;

            m_name.text = data.Label;
            
            //setup offset
            m_treeOffset.ForEach(element =>
            {
                element.style.width = data.TreeNode.Level * 30;
            });
        }
        private void FoldoutValueChanged(ChangeEvent<bool> evt)
        {
            for (int i = m_data.Index + 1; i < m_hierarchyView.itemsSource.Count; ++i)
            {
                var data = m_hierarchyView.itemsSource[i] as HierarchyItemData;
                if (m_data.TreeNode.Level >= data.TreeNode.Level)
                    break;

                data.Item.SetVisible(evt.newValue);
            }
        }
        public void SetVisible(bool visible)
        {
            if (visible)
            {
                this.style.height = new StyleLength(StyleKeyword.Auto);
                this.style.visibility = Visibility.Visible;
            }
            else
            {
                this.style.height = 0;
                this.style.visibility = Visibility.Hidden;
            }
        }
    }
}