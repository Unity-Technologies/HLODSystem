using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.HLODSystem.Streaming;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace Unity.HLODSystem.DebugWindow
{
    public class HLODItem : VisualElement, IDisposable
    {
        private static readonly string s_uxmlGuid = "a3d94d4fe01e43d4eb8f2fc24c533851";

        private HLODDebugWindow m_window;
        private HLODItemData m_data;

        private ListView m_hierarchyView;
        private List<HierarchyItem> m_hierarchyItems = new List<HierarchyItem>();
        
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
            
            m_hierarchyView = this.Q<ListView>("Hierarchy");
        }
        public void Dispose()
        {
            this.Unbind();
            
            var ping = this.Q<Button>("Ping");
            ping.clickable.clicked -= PingOnclicked;

            foreach (var item in m_hierarchyItems)
            {
                item.Dispose();
            }
            m_hierarchyItems.Clear();
        }
        


        public void BindData(HLODItemData data)
        {
            m_data = data;
            this.Bind(new SerializedObject(data));

            foreach (var hierarchyData in data.HierarchyItemDatas)
            {
                var item = new HierarchyItem(m_window, this, m_data);
                item.BindTreeNode(hierarchyData);
                m_hierarchyItems.Add(item);
                
            }
            
            UpdateList();
        }
        
        
        
        private void PingOnclicked()
        {
            if (m_data == null)
                return;
            
            EditorGUIUtility.PingObject(m_data.Controller);
        }

        public void UpdateList()
        {
            var view = m_hierarchyView.hierarchy[0] as ScrollView;
            view.Clear();

            for ( int i = 0; i < m_hierarchyItems.Count;)
            {
                var item = m_hierarchyItems[i];
                view.Add(item);
                if (item.Data.IsOpen)
                {
                    
                    ++i;
                }
                else
                {
                    for (i = i+1; i < m_hierarchyItems.Count; ++i)
                    {
                        var nextItem = m_hierarchyItems[i];
                        if (item.Data.TreeNode.Level >= nextItem.Data.TreeNode.Level)
                            break;
                    }
                }
            }
        }

    }

}