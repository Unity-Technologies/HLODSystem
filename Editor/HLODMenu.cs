using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProBuilder.EditorCore;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Unity.HLODSystem
{
    public static class HLODMenu
    {
        private const string k_SetupPath = "HLOD/Setup";

        [MenuItem(k_SetupPath)]
        static void OnSetup(MenuCommand command)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() == null ||
                PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot == null)
            {
                Debug.LogError("Setup HLOD can only be used while prefab editing.");
                return;
            }

            GameObject root = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
            if (root.GetComponent<HLOD>() != null)
            {
                Debug.LogWarning("It has already been set.");
                return;
            }

            GameObject low = CreateLow(root);
            GameObject high = CreateHigh(low);
            low.transform.SetParent(root.transform);
            high.transform.SetParent(root.transform);

            HLOD hlod = root.AddComponent<HLOD>();
            hlod.LowRoot = low;
            hlod.HighRoot = high;
        }

        [MenuItem(k_SetupPath, true)]
        static bool CanSetup(MenuCommand command)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() == null ||
                PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot == null)
                return false;

            return PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot.GetComponent<HLOD>() == null;
        }

        static GameObject CreateLow(GameObject root)
        {
            GameObject low = new GameObject("Low");

            while (root.transform.childCount > 0)
            {
                Transform child = root.transform.GetChild(0);
                child.SetParent(low.transform);
            }

            return low;
        }

        static GameObject CreateHigh(GameObject lowGameObject)
        {
            GameObject high = new GameObject("High");

            var lodGroups = lowGameObject.GetComponentsInChildren<LODGroup>();
            List<Renderer> lodRenderers = new List<Renderer>();

            for (int i = 0; i < lodGroups.Length; ++i)
            {
                LOD[] lods = lodGroups[i].GetLODs();
                Renderer[] renderers = lods.Last().renderers;
                lodRenderers.AddRange(renderers);
            }

            for (int i = 0; i < lodRenderers.Count; ++i)
            {
                Renderer renderer = lodRenderers[i];
                if (renderer == null)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                GameObject rendererObject = new GameObject(lodRenderers[i].name, typeof(MeshFilter), typeof(MeshRenderer));

                EditorUtility.CopySerialized(filter, rendererObject.GetComponent<MeshFilter>());
                EditorUtility.CopySerialized(renderer, rendererObject.GetComponent<MeshRenderer>());

                rendererObject.transform.SetParent(high.transform);
                rendererObject.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                rendererObject.transform.localScale = renderer.transform.lossyScale;
            }

            return high;
        }
    }

}