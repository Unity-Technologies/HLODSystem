using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

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
        private List<HLODItem> m_hlodItems = new List<HLODItem>();
        private List<HLODItemData> m_hlodItemDatas = new List<HLODItemData>();
        private HierarchyItem m_selectedItem;

        private RadioButtonGroup m_drawModeUI;

        [SerializeField]
        private bool m_drawSelected = true;
        [SerializeField] 
        private bool m_highlightRendered = true;

        [SerializeField]
        private DrawMode m_drawMode = DrawMode.RenderOnly;
        
        public bool HighlightRendered => m_highlightRendered;
        
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
            
            UpdateDataList();


            var serializedObject = new SerializedObject(this);
            var drawSelectedUI = root.Q<Toggle>("DrawSelected");
            drawSelectedUI.Bind(serializedObject);

            var highlightRenderedUI = root.Q<Toggle>("HighlightRendered");
            highlightRenderedUI.Bind(serializedObject);
            
            m_drawModeUI = root.Q<RadioButtonGroup>("DrawMode");
            m_drawModeUI.choices = new[]
            {
                DrawMode.None.ToString(),
                DrawMode.RenderOnly.ToString(),
                DrawMode.All.ToString(),
            };
            m_drawModeUI.Bind(serializedObject);
            
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneView.duringSceneGui += SceneViewOnDuringSceneGui;
        }
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SceneView.duringSceneGui -= SceneViewOnDuringSceneGui;
        }

     

        private void Update()
        {
            if (HLODManager.Instance.ActiveControllers.Count != m_hlodItemDatas.Count)
            {
                UpdateDataList();
            }
        }

        private void UpdateDataList()
        {
            foreach (var hlodItem in m_hlodItems)
            {
                hlodItem.Dispose();
            }
            
            m_hlodItemDatas.Clear();
            m_hlodItemList.Clear();
            m_hlodItems.Clear();

            foreach (var controller in HLODManager.Instance.ActiveControllers)
            {
                var data = new HLODItemData();
                data.Initialize(controller);
                m_hlodItemDatas.Add(data);
            }

            var view = m_hlodItemList.hierarchy[0] as ScrollView;
            view.Clear();
            foreach (var data in m_hlodItemDatas)
            {
                var item = new HLODItem(this);
                item.BindData(data);
                view.Add(item);
                
                m_hlodItems.Add(item);
                
            }
        }

   
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            UpdateDataList();
            m_selectedItem = null;
        }

        #region Debug rendering
        private void SceneViewOnDuringSceneGui(SceneView obj)
        {
            if (m_drawMode != DrawMode.None)
            {
                foreach (var itemData in m_hlodItemDatas)
                {
                    itemData.Render(m_drawMode);
                }
            }
            if (m_drawSelected)
            {
                if ( m_selectedItem != null)
                {
                    HLODTreeNodeRenderer.Instance.Render(m_selectedItem.Data.TreeNode, Color.red, 2.0f);
                }
            }
        }

        public void SetSelectItem(HierarchyItem item)
        {
            if ( m_selectedItem != null)
                m_selectedItem.UnselectItem();
            
            m_selectedItem = item;
            if ( m_selectedItem != null)
                m_selectedItem.SelectItem();
        }
        
        
        #endregion
    }

}