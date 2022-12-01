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
        enum DrawMode
        {
            None,
            RenderOnly,
            All,
        }
        #region menu item
        [MenuItem("Window/HLOD/DebugWindow", false, 100000)]
        static void ShowWindow()
        {
            var window = GetWindow<HLODDebugWindow>("HLOD Debug window");
            window.Show();
        }
        #endregion

        private ListView m_hlodItemList;

        private RadioButtonGroup m_drawModeUI;

        [SerializeField]
        private bool m_drawSelected = true;
        [SerializeField] 
        private bool m_highlightSelected = true;

        [SerializeField]
        private DrawMode m_drawMode = DrawMode.None;
        
        public bool HighlightSelected => m_highlightSelected;
        
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
            m_hlodItemList.unbindItem += HLODItemListUnbindItem;
            
            m_hlodItemList.itemsSource = HLODManager.Instance.ActiveControllers;
            
            m_hlodItemList.Rebuild();
            
            var serializedObject = new SerializedObject(this);
            var drawSelectedUI = root.Q<Toggle>("DrawSelected");
            drawSelectedUI.Bind(serializedObject);

            var highlightSelectedUI = root.Q<Toggle>("HighlightSelected");
            highlightSelectedUI.Bind(serializedObject);
            
            m_drawModeUI = root.Q<RadioButtonGroup>("DrawMode");
            m_drawModeUI.choices = new[]
            {
                DrawMode.None.ToString(),
                DrawMode.RenderOnly.ToString(),
                DrawMode.All.ToString(),
            };
            m_drawModeUI.Bind(serializedObject);
            
            Camera.onPostRender += OnPostRender;
            EditorApplication.playModeStateChanged += OnplayModeStateChanged;
        }

        private void OnDisable()
        {
            Camera.onPostRender -= OnPostRender;
            EditorApplication.playModeStateChanged -= OnplayModeStateChanged;
        }

        private void HLODItemListBindItem(VisualElement element, int i)
        {
            var controller = m_hlodItemList.itemsSource[i] as HLODControllerBase;
            var item = element as HLODItem;
            if (item == null || controller == null)
                return;
            
            item.BindController(controller);
        }
        
        private void HLODItemListUnbindItem(VisualElement element, int i)
        {
            var item = element as HLODItem;
            if (item == null)
                return;
            
            item.UnbindController();
        }
        private VisualElement HLODItemListMakeItem()
        {
            return new HLODItem(this);
        }
        
        private void OnplayModeStateChanged(PlayModeStateChange state)
        {
            //
            m_hlodItemList.Rebuild();
            ClearSelectTreeNodes();
        }

        #region Debug rendering

        private List<HLODTreeNode> m_treeNodes = new List<HLODTreeNode>();
        private List<HLODTreeNode> m_selectTreeNodes = new List<HLODTreeNode>();

        
        private void OnPostRender(Camera cam)
        {
            if (m_drawMode != DrawMode.None)
            {
                foreach (var node in m_treeNodes)
                {
                    if (node.CurrentState == HLODTreeNode.State.Low ||
                        ( node.CurrentState == HLODTreeNode.State.High && node.GetChildTreeNodeCount() == 0 ))
                    {
                        HLODTreeNodeRenderer.Instance.Render(node, Color.green, 2.0f);    
                    }
                    else if (m_drawMode == DrawMode.All)
                    {
                        HLODTreeNodeRenderer.Instance.Render(node, Color.yellow, 1.0f);
                    }
                }
            }
            if (m_drawSelected)
            {
                foreach (var node in m_selectTreeNodes)
                {
                    HLODTreeNodeRenderer.Instance.Render(node, Color.red, 2.0f);
                }
            }
        }

        public void AddDebugTreeNode(HLODTreeNode node)
        {
            m_treeNodes.Add(node);
        }

        public void RemoveDebugTreeNode(HLODTreeNode node)
        {
            m_treeNodes.Remove(node);
        }

        public void ClearDebugTreeNodes()
        {
            m_treeNodes.Clear();
        }

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