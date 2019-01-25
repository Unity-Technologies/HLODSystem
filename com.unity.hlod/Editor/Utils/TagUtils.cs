#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    static class TagUtils
    {
        const int k_MaxLayer = 31;
        const int k_MinLayer = 8;
        

        /// <summary>
        /// Add a layer to the tag manager if it doesn't already exist
        /// Start at layer 31 (max) and work down
        /// </summary>
        /// <param name="layerName"></param>
        public static void AddLayer(string layerName)
        {
            SerializedObject so;
            var layers = GetTagManagerProperty("layers", out so);
            if (layers != null)
            {
                var found = false;
                for (var i = 0; i < layers.arraySize; i++)
                {
                    if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var added = false;
                    for (var i = k_MaxLayer; i >= k_MinLayer; i--)
                    {
                        var layer = layers.GetArrayElementAtIndex(i);
                        if (!string.IsNullOrEmpty(layer.stringValue))
                            continue;

                        layer.stringValue = layerName;
                        added = true;
                        break;
                    }

                    if (!added)
                        Debug.LogWarning("Could not add layer " + layerName + " because there are no free layers");
                }
                so.ApplyModifiedProperties();
                so.Update();
            }
        }

        public static SerializedProperty GetTagManagerProperty(string name, out SerializedObject so)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                so = new SerializedObject(asset[0]);
                return so.FindProperty(name);
            }

            so = null;
            return null;
        }

    }

}
#endif