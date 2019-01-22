using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BatchHelper))]
public class BatchHelperGUI : Editor
{
    private SerializedProperty m_prefabProperty;
    private SerializedProperty m_areaProperty;
    private SerializedProperty m_densityProperty;
    private SerializedProperty m_rotationProperty;
    private SerializedProperty m_scaleProperty;

    void OnEnable()
    {
        m_prefabProperty = serializedObject.FindProperty("m_prefab");
        m_areaProperty = serializedObject.FindProperty("m_area");
        m_densityProperty = serializedObject.FindProperty("m_density");
        m_rotationProperty = serializedObject.FindProperty("m_rotation");
        m_scaleProperty = serializedObject.FindProperty("m_scale");

    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(m_prefabProperty);
        EditorGUILayout.PropertyField(m_areaProperty);
        EditorGUILayout.PropertyField(m_densityProperty);
        EditorGUILayout.PropertyField(m_rotationProperty);
        EditorGUILayout.PropertyField(m_scaleProperty);

        EditorGUILayout.Space();
         if (GUILayout.Button("Generate") == true)
        {
            GameObject prefab = (GameObject)m_prefabProperty.objectReferenceValue;
            Bounds area = m_areaProperty.boundsValue;
            float density = m_densityProperty.floatValue;
            Vector2 rotation = m_rotationProperty.vector2Value;
            Vector2 scale = m_scaleProperty.vector2Value;

            float areaSize = area.size.x * area.size.z;
            int createCount = (int)(areaSize * density + 0.5f);
            float randRotation = Random.Range(rotation.x, rotation.y);
            float randScale = Random.Range(scale.x, scale.y);


            Transform transform = ((Component) target).transform;

            float startY = transform.position.y + area.extents.y;

            for (int i = 0; i < createCount; ++i)
            {
                Vector3 pos;
                Quaternion rot;
                pos.x = Random.Range(-area.extents.x, area.extents.x) + transform.position.x + area.center.x;
                pos.y = startY;
                pos.z = Random.Range(-area.extents.z, area.extents.z) + transform.position.z + area.center.z;

                RaycastHit hit;
                if (Physics.Raycast(pos, Vector3.down, out hit, area.size.y) == true)
                {
                    pos.y = hit.point.y;
                }
                else
                {
                    pos.y = 0.0f;
                }

                rot = Quaternion.Euler(0.0f, randRotation, 0.0f);

                GameObject instance = Instantiate(prefab, pos, rot, transform);
                instance.transform.localScale = new Vector3(randScale, randScale, randScale);
            }

            Debug.Log("Batch finished. " + createCount + " objects created.");
        }

        if (GUILayout.Button("Destroy All") == true)
        {
            Transform transform = ((Component) target).transform;
            List<GameObject> objects = new List<GameObject>(transform.childCount);
            foreach ( Transform child in transform )
            {
                objects.Add(child.gameObject);
            }

            for (int i = 0; i < objects.Count; ++i)
            {
                DestroyImmediate(objects[i]);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
