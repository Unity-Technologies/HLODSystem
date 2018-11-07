using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLOD))]
    public class HLODEditor : Editor
    {
      

        void OnEnable()
        {
           
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            
            if (GUILayout.Button("Generate"))
            {
                MaterialPreservingBatcher batcher = new MaterialPreservingBatcher();
                batcher.Batch(((HLOD) target).HighRoot);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}