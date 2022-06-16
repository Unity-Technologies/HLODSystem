using System;
using System.Collections.Generic;
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

        public static T FindObject<T>(Object[] objectList, string name) where T : Object
        {
            for (int i = 0; i < objectList.Length; ++i)
            {
                if (objectList[i] == null)
                    continue;
                if (objectList[i] is not T)
                    continue;
                
                if (objectList[i].name == name)
                    return objectList[i] as T;
            }

            return null;
        }
    }
}