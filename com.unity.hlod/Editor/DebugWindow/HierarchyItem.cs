using System;
using Unity.HLODSystem.Streaming;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.HLODSystem.DebugWindow
{
    public class HierarchyItem : VisualElement, IDisposable
    {
        private static readonly string s_uxmlGuid = "7b9b7a1f48292534bb048103f56e3404";


        private HLODItemData m_hlodData;
        private HierarchyItemData m_data;

        private HLODDebugWindow m_window;
        private HLODItem m_hlodItem;

        private UQueryBuilder<VisualElement> m_treeOffset;
        private Toggle m_foldoutToggle;
        private VisualElement m_root;

        public HierarchyItemData Data
        {
            get
            {
                return m_data;
            }
        }
        
        public HierarchyItem(HLODDebugWindow window, HLODItem hlodItem, HLODItemData hlodData)
        {
            m_window = window;
            m_hlodItem = hlodItem;
            m_hlodData = hlodData;
            
            var uxmlPath = AssetDatabase.GUIDToAssetPath(s_uxmlGuid);
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

            m_root = template.CloneTree();
            Add(m_root);

            m_treeOffset = m_root.Query<VisualElement>("TreeOffset");
            
            m_foldoutToggle = m_root.Q<Toggle>("Foldout");
            m_foldoutToggle.RegisterValueChangedCallback(FoldoutValueChanged);
            m_foldoutToggle.RegisterCallback<ClickEvent>(FoldoutClick);
            
            RegisterCallback<ClickEvent>(ItemClick);
            
            EditorApplication.update += Update;
        }
        
        public void Dispose()
        {
            this.Unbind();
            
            if (m_foldoutToggle != null)
            {
                m_foldoutToggle.UnregisterValueChangedCallback(FoldoutValueChanged);
                m_foldoutToggle.UnregisterCallback<ClickEvent>(FoldoutClick);
            }
            EditorApplication.update -= Update;
        }
        

        private void ItemClick(ClickEvent evt)
        {
            m_window.SetSelectItem(this);
        }

        private void Update()
        {
            if (m_data == null)
                return;
            
            var node = m_data.TreeNode;
            var isRendered = false;

            if (m_window.HighlightRendered)
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

            if (m_data.TreeNode.GetChildTreeNodeCount() == 0)
            {
                m_foldoutToggle.visible = false;
            }
            else
            {
                m_foldoutToggle.visible = true;
            }

            this.Bind(new SerializedObject(m_data));
            
            //setup offset
            m_treeOffset.ForEach(element =>
            {
                element.style.width = data.TreeNode.Level * 30;
            });
        }

        public void SelectItem()
        {
            AddToClassList("unity-collection-view__item--selected");
        }
        public void UnselectItem()
        {
            RemoveFromClassList("unity-collection-view__item--selected");
        }
        private void FoldoutClick(ClickEvent evt)
        {
            m_hlodItem.UpdateList();
        }
        private void FoldoutValueChanged(ChangeEvent<bool> evt)
        {
            m_data.IsOpen = evt.newValue;
        }

        
    }
}