using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Utils
{
    public static class GUIDUtils
    {
        public static Guid ObjectToGUID(Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return Guid.NewGuid();
            return Guid.Parse(AssetDatabase.AssetPathToGUID(path));
        }

        public static T GUIDToObject<T>(Guid guid) where T:Object
        {
            string path = AssetDatabase.GUIDToAssetPath(guid.ToString("N"));
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}