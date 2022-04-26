using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(HLODDataImporter))]
    public class HLODDataImporterEditor : ScriptedImporterEditor
    {
        static bool s_textureFoldout = false;
        static bool s_meshFoldout = false;

        private List<KeyValuePair<string, string>> m_texture = new List<KeyValuePair<string, string>>();
        private List<KeyValuePair<string, string>> m_mesh = new List<KeyValuePair<string, string>>();
        private string m_totalTexture= "";
        private string m_totalMesh= "";

        public override void OnEnable()
        {
            base.OnEnable();

            string path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                HLODData data = HLODDataSerializer.Read(stream);

                long totalTextureSize = 0;
                long totalMeshSize = 0;

                var serializableMaterials = data.GetMaterials();
                for (int mi = 0; mi < serializableMaterials.Count; ++mi)
                {
                    var material = serializableMaterials[mi];
                    for (int ti = 0; ti < material.GetTextureCount(); ++ti)
                    {
                        var texture = material.GetTexture(ti);
                        totalTextureSize += texture.BytesLength;

                        m_texture.Add(new KeyValuePair<string, string>(texture.TextureName, FormattingSize(texture.BytesLength)));
                    }
                }


                var serializableObjects = data.GetObjects();
                for (int oi = 0; oi < serializableObjects.Count; ++oi)
                {
                    var mesh = serializableObjects[oi].GetMesh();
                    int meshSpaceUsage = mesh.GetSpaceUsage();

                    totalMeshSize += meshSpaceUsage;
                    m_mesh.Add(new KeyValuePair<string, string>(mesh.Name, FormattingSize(meshSpaceUsage)));
                    
                }

                m_totalTexture = FormattingSize(totalTextureSize);
                m_totalMesh = FormattingSize(totalMeshSize);
            }
        }

        public override void OnInspectorGUI()
        {

            s_textureFoldout = EditorGUILayout.Foldout(s_textureFoldout, "Textures");
            EditorGUI.indentLevel += 1;
            if (s_textureFoldout == true)
            {
                for (int ti = 0; ti < m_texture.Count; ++ti)
                {
                    EditorGUILayout.LabelField($"{m_texture[ti].Key}: {m_texture[ti].Value}");
                }
            }
            EditorGUI.indentLevel -= 1;


            s_meshFoldout = EditorGUILayout.Foldout(s_meshFoldout, "Mesh");
            EditorGUI.indentLevel += 1;


            if (s_meshFoldout == true)
            {
                for (int mi = 0; mi < m_mesh.Count; ++mi)
                {
                    EditorGUILayout.LabelField($"{m_mesh[mi].Key}: {m_mesh[mi].Value}");
                }
            }

            EditorGUI.indentLevel -= 1;

            EditorGUILayout.LabelField($"Total texture: {m_totalTexture}");
            EditorGUILayout.LabelField($"Total mesh: {m_totalMesh}");

            ApplyRevertGUI();
        }

        private string FormattingSize(long length)
        {
            //gb
            if ( length > 1 <<30)
            {
                float val = (float)length / (1 << 30);
                return string.Format("{0:0.00}GB", val);
            }
            //mb
            if ( length > 1<<20)
            {
                float val = (float)length / (1 << 20);
                return string.Format("{0:0.00}MB", val);
            }
            //kb
            if ( length > 1<<10)
            {
                float val = (float)length / (1 << 10);
                return string.Format("{0:0.00}KB", val);
            }

            return string.Format("{0:0.00}B", length);
        }
    }
}