using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.HLODSystem.DebugWindow
{
    public class HLODDebugWindow : EditorWindow
    {
        [MenuItem("Window/HLOD/DebugWindow", false, 100000)]
        static void ShowWindow()
        {
            var window = GetWindow<HLODDebugWindow>("HLOD Debug window");
            window.Show();
        }
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
            
        }
    }

}