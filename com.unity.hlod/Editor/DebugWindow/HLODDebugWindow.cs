using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.HLODSystem.Streaming;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.HLODSystem.DebugWindow
{
    public class HLODDebugWindow : EditorWindow
    {
        #region menu item
        [MenuItem("Window/HLOD/DebugWindow", false, 100000)]
        static void ShowWindow()
        {
            var window = GetWindow<HLODDebugWindow>("HLOD Debug window");
            window.Show();
        }
        #endregion

        private ListView m_hlodItemList;
        //private VisualElement m_
        
        private void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            
            MonoScript ms = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(ms);
            string scriptDirectory = Path.GetDirectoryName(scriptPath);
            
            // Import UXML
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(scriptDirectory + "/HLODDebugWindow.uxml");
            
            visualTree.CloneTree(root);
            
            //Initialize variables
            m_hlodItemList = root.Q<ListView>("HLODItemList");
            
            m_hlodItemList.makeItem += HLODItemListMakeItem;
            m_hlodItemList.bindItem += HLODItemListBindItem;

            m_hlodItemList.itemsSource = HLODManager.Instance.ActiveControllers;

        }

        private void HLODItemListBindItem(VisualElement element, int i)
        {
            var controller = m_hlodItemList.itemsSource[i] as HLODControllerBase;
            var item = element as HLODItem;
            if (item == null || controller == null)
                return;
            
            item.BindController(controller);
        }

        private VisualElement HLODItemListMakeItem()
        {
            return new HLODItem(this);
        }

        #region Debug rendering

        private List<HLODTreeNode> m_selectTreeNodes = new List<HLODTreeNode>();
        public void AddSelectTreeNode(HLODTreeNode node)
        {
            m_selectTreeNodes.Add(node);
        }

        public void ClearSelectTreeNodes()
        {
            m_selectTreeNodes.Clear();
        }
        
        #endregion
    }

}