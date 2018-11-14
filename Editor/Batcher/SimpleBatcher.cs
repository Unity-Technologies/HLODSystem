using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class SimpleBatcher : IBatcher
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            BatcherTypes.RegisterBatcherType(typeof(SimpleBatcher));
        }
        public void Batch(GameObject root)
        {
        }

        public static void OnGUI(HLOD hlod)
        {
            EditorGUI.indentLevel += 1;
            dynamic batcherOptions = hlod.BatcherOptions;

            if (batcherOptions.PackTextureSize == null)
                batcherOptions.PackTextureSize = 1024;
            if (batcherOptions.LimitTextureSize == null)
                batcherOptions.LimitTextureSize = 128;
            if (batcherOptions.Material == null)
                batcherOptions.Material = (Material) null;
 
            batcherOptions.Material = EditorGUILayout.ObjectField("Material", batcherOptions.Material, typeof(Material), false) as Material;
            EditorGUI.indentLevel -= 1;
        }
    }

}
