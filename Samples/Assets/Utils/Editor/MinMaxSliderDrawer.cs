using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer (typeof (MinMaxSliderAttribute))]
class MinMaxSliderDrawer : PropertyDrawer
{
	
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{

		if (property.propertyType == SerializedPropertyType.Vector2) {
			Vector2 range = property.vector2Value;
			float min = range.x;
			float max = range.y;
			MinMaxSliderAttribute attr = attribute as MinMaxSliderAttribute;
			EditorGUI.BeginChangeCheck ();
            EditorGUILayout.MinMaxSlider(label, ref min, ref max, attr.Min, attr.Max);
		    EditorGUI.indentLevel += 1;
		    min = EditorGUILayout.FloatField("Min", min);
		    max = EditorGUILayout.FloatField("Max", max);
            EditorGUI.indentLevel -= 1;
			if (EditorGUI.EndChangeCheck ())
			{
				range.x = min;
				range.y = max;
				property.vector2Value = range;
			}
		}
		else
		{
			EditorGUI.LabelField (position, label, "property is not vector2");
		}
	}
}