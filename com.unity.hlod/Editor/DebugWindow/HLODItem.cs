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
    public class HLODItem : VisualElement
    {
        private static readonly string s_uxmlGuid = "a3d94d4fe01e43d4eb8f2fc24c533851";

        private HLODControllerBase m_controller;

        private Label m_lable;
        private Button m_ping;
        private ListView m_hierarchyView;
        public HLODItem()
        {
            var uxmlPath = AssetDatabase.GUIDToAssetPath(s_uxmlGuid);
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

            var root = template.CloneTree();
            Add(root);

            m_lable = this.Q<Label>("Label");
            m_ping = this.Q<Button>("Ping");
            m_hierarchyView = this.Q<ListView>("Hierarchy");
            
            m_ping.clickable.clicked += PingOnclicked;
        }
        public void BindController(HLODControllerBase controller)
        {
            m_controller = controller;
            
            this.Bind(new SerializedObject(controller));
            m_lable.Bind(new SerializedObject(controller.gameObject));
        }

        private void InitializeHierarchy()
        {
            
        }
        
        private void PingOnclicked()
        {
            EditorGUIUtility.PingObject(m_controller);
        }
    }

}