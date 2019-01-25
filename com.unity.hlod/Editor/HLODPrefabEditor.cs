using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLODPrefab))]
    public class HLODPrefabEditor : Editor
    {
        public static class Styles
        {
            public static GUIContent PrefabContent = new GUIContent("Prefab");
        }

        void OnEnable()
        {
            
        }
        public override void OnInspectorGUI()
        {
            HLODPrefab hlodPrefab = target as HLODPrefab;
            GUI.enabled = hlodPrefab.enabled;

            using (new GUILayout.HorizontalScope())
            {
                if (hlodPrefab.IsEdit)
                {
                    GUILayout.Box("Show");
                    if (GUILayout.Button("Hide"))
                    {
                        hlodPrefab.IsEdit = false;
                    }
                }
                else
                {
                    GUILayout.Box("Hide");
                    if (GUILayout.Button("Show for Edit"))
                    {
                        hlodPrefab.IsEdit = true;
                    }
                }
            }

            
            hlodPrefab.Prefab = EditorGUILayout.ObjectField("Prefab", hlodPrefab.Prefab, typeof(GameObject), false) as GameObject;
            GUI.enabled = true;
        }
    }

}