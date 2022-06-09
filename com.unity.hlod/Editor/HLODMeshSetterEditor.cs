using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLODMeshSetter))]
    public class HLODMeshSetterEditor : Editor
    {
        class Styles
        {
            public const float kSingleLineHeight = 18.0f;
            public const float kSpacing = 5;
            public readonly GUIContent meshSettingHeader = new GUIContent("Mesh settings");
            public readonly GUIContent meshRenderersHeader = new GUIContent("Mesh renderers");
        }

        class MeshGroupUI
        {
            private SerializedProperty m_property;
            private ReorderableList m_meshRendererList;

            private SerializedProperty m_targetLevelProperty;
            private SerializedProperty m_meshRenderersProperty;
            public MeshGroupUI(SerializedObject serializedObject, SerializedProperty serializedProperty)
            {
                m_property = serializedProperty;

                m_targetLevelProperty = m_property.FindPropertyRelative("m_targetLevel");
                m_meshRenderersProperty = m_property.FindPropertyRelative("m_meshRenderers");

                m_meshRendererList =
                    new ReorderableList(serializedObject, m_meshRenderersProperty, false, true, true, true);
                
                m_meshRendererList.drawHeaderCallback += DrawHeaderCallback;
                m_meshRendererList.drawElementCallback += DrawElementCallback;
                m_meshRendererList.elementHeightCallback += ElementHeightCallback;
                m_meshRendererList.onAddCallback += OnAddCallback;
                m_meshRendererList.onRemoveCallback += OnRemoveCallback;
            }
            private void DrawHeaderCallback(Rect rect)
            {
                EditorGUI.LabelField(rect, styles.meshRenderersHeader);
            }
            private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
            {
                Rect objectRect = new Rect(rect.x, rect.y, rect.width * 0.6f, rect.height);
                Rect labelRect = new Rect(rect.x + objectRect.width + 10, rect.y, rect.width - objectRect.width, rect.height);

                var meshRendererProp = m_meshRenderersProperty.GetArrayElementAtIndex(index);
                EditorGUI.ObjectField(objectRect, meshRendererProp, typeof(MeshRenderer), GUIContent.none);

                int triCount = 0;
                int meshCount = 0;
                var meshRenderer = meshRendererProp.objectReferenceValue as MeshRenderer;

                if (meshRenderer != null)
                {
                    var meshFilter =meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        //after divided by 3, we can get a real triangle count.
                        triCount = meshFilter.sharedMesh.triangles.Length / 3;
                        meshCount = meshFilter.sharedMesh.subMeshCount;
                        EditorGUI.LabelField(labelRect, $"{triCount} Tris {meshCount} Sub Mesh(es)");
                    }
                    
                }
            }
            private float ElementHeightCallback(int index)
            {
                return Styles.kSingleLineHeight;
            }
            private void OnAddCallback(ReorderableList list)
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
            }
            private void OnRemoveCallback(ReorderableList list)
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }

            public void Draw(Rect rect, int index)
            {
                rect.height = Styles.kSingleLineHeight;
            
                m_property.isExpanded = EditorGUI.Foldout(rect, m_property.isExpanded, "HLOD Group " + index);
                if (m_property.isExpanded)
                {
                    rect.y += Styles.kSingleLineHeight + Styles.kSpacing;
                    EditorGUI.PropertyField(rect, m_targetLevelProperty);
                    
                    rect.y += Styles.kSingleLineHeight + Styles.kSpacing;
                    m_meshRendererList.DoList(rect);
                }
            }

            public float GetHeight()
            {
                return m_meshRendererList.GetHeight() + Styles.kSingleLineHeight * 2 + Styles.kSpacing;
            }
        }

        private static Styles styles;
        
        private SerializedProperty m_meshSettingsProperty;
        private SerializedProperty m_removeAtBuildProperty;
        
        private ReorderableList m_meshSettingList;

        private List<MeshGroupUI> m_meshSettingGroupList;


        private void OnEnable()
        {
            m_meshSettingsProperty = serializedObject.FindProperty("m_meshSettings");
            m_removeAtBuildProperty = serializedObject.FindProperty("m_removeAtBuild");
            
            m_meshSettingList = new ReorderableList(serializedObject, m_meshSettingsProperty, false, true, true, true);
          
            m_meshSettingList.drawHeaderCallback += DrawHeaderMeshSetting;
            m_meshSettingList.drawElementCallback += DrawElementMeshSetting;
            m_meshSettingList.elementHeightCallback += ElementHeighMeshSetting;
            m_meshSettingList.onAddCallback += OnAddMeshSetting;
            m_meshSettingList.onRemoveCallback += OnRemoveMeshSetting;


            ResetMeshGroup();
        }

        private void ResetMeshGroup()
        {
            m_meshSettingGroupList = new List<MeshGroupUI>();

            for (int i = 0; i < m_meshSettingsProperty.arraySize; ++i)
            {
                var groupProperty = m_meshSettingsProperty.GetArrayElementAtIndex(i);
                m_meshSettingGroupList.Add(new MeshGroupUI(serializedObject, groupProperty));
            }
        }

        private float ElementHeighMeshSetting(int index)
        {
            var prop = m_meshSettingsProperty.GetArrayElementAtIndex(index);
            if (prop.isExpanded)
            {
                return m_meshSettingGroupList[index].GetHeight() + Styles.kSpacing;
            }
            else
            {
                return Styles.kSingleLineHeight;
            }
        }
 
        private void DrawHeaderMeshSetting(Rect rect)
        {
            GUI.Label(rect, styles.meshSettingHeader);
        }
        private void DrawElementMeshSetting(Rect rect, int index, bool isactive, bool isfocused)
        {
            rect.x += 10.0f;
            rect.width -= 10.0f;
            
            m_meshSettingGroupList[index].Draw(rect, index);
        }

        private void OnAddMeshSetting(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            ResetMeshGroup();
        }
        private void OnRemoveMeshSetting(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            ResetMeshGroup();
        }

        public override void OnInspectorGUI()
        {
            if (styles == null)
            {
                styles = new Styles();
            }
            
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_removeAtBuildProperty);
            m_meshSettingList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}