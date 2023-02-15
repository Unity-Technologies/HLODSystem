using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLODCameraRecognizer))]
    public class HLODCameraRecognizerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var recognizer = target as HLODCameraRecognizer;

            if (HLODCameraRecognizerManager.ActiveRecognizer == recognizer)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 15;
                style.normal.textColor = Color.blue;
                EditorGUILayout.LabelField("Activated HLODCameraRecognizer.", style);
            }

            if (GUILayout.Button("Active"))
            {
                if (recognizer == null)
                    return;
                
                recognizer.Active();
            }
        }
    }
}