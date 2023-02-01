using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    [CustomPropertyDrawer(typeof(RangeInt))]
    public class RangeIntPropertyDrawer : PropertyDrawer
    {
        // public override void OnInspectorGUI()
        // {
        //     //base.OnInspectorGUI();
        //     var rect = EditorGUILayout.BeginHorizontal();
        //     EditorGUILayout.IntSlider(0, -1, 100);
        //     EditorGUILayout.EndHorizontal();
        //
        // }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            var valueProperty = property.FindPropertyRelative("m_value");
            var minProperty = property.FindPropertyRelative("m_minValue");
            var maxProperty = property.FindPropertyRelative("m_maxValue");

            int changeValue = EditorGUI.IntSlider(position, valueProperty.intValue , minProperty.intValue, maxProperty.intValue);

            if (changeValue != valueProperty.intValue)
            {
                valueProperty.intValue = changeValue;
            }

            EditorGUI.EndProperty();
        }
    }
}