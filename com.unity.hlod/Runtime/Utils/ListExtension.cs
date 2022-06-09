using System.Collections;
using System.Collections.Generic;

namespace Unity.HLODSystem.Utils
{
    public static class ListExtension
    {
        public static void RemoveAll<T>(this List<T> list, IEnumerable<T> removeList)
        {
            foreach(var item in removeList)
            {
                list.Remove(item);
            }
        }
    }
}