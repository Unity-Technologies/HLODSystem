using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
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
            HLOD hlod = HLODCreator.Setup(root);

            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        [MenuItem(k_SetupPath, true)]
        static bool CanSetup(MenuCommand command)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() == null ||
                PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot == null)
                return false;

            return PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot.GetComponent<HLOD>() == null;
        }

    }

}