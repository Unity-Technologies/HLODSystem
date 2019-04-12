using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Unity.HLODSystem.Streaming
{
    public class AddressableStreaming : IStreamingBuilder
    {
        
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            StreamingBuilderTypes.RegisterType(typeof(AddressableStreaming));
        }

        public void Build(SpaceManager.SpaceNode rootNode, List<HLODBuildInfo> infos)
        {

        }
        public static void OnGUI(HLOD hlod)
        {
            dynamic options = hlod.StreamingOptions;

            if (options.LastLowInMemory == null)
                options.LastLowInMemory = false;
            if (options.MaxInstantiateCount == null)
                options.MaxInstantiateCount = 10;

            EditorGUI.indentLevel += 1;
            options.LastLowInMemory = EditorGUILayout.Toggle("Last low in memory", options.LastLowInMemory);
            options.MaxInstantiateCount =
                EditorGUILayout.IntSlider("Max instantiate count per frame", options.MaxInstantiateCount, 1, 100, null);
            
            EditorGUI.indentLevel -= 1;
        }

        private AssetReference GetAssetReference(Object obj)
        {
            //create settings if there is no settings.
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
            }

            
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            string path = AssetDatabase.GetAssetPath(obj);
            
            if (string.IsNullOrEmpty(path))
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            if (string.IsNullOrEmpty(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);

            if (entry != null)
                return new AssetReference(guid);

            var entriesAdded = new List<AddressableAssetEntry>
            {
                settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false)
            };

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);

            return new AssetReference(guid);
        }
    }

}