using System;
using System.Collections.Generic;


namespace Unity.HLODSystem.SpaceManager
{
    public static class SpaceSplitterTypes
    {
        private static List<Type> s_Types = new List<Type>();

        public static void RegisterSpaceSplitterType(Type type)
        {
            s_Types.Add(type);
        }

        public static void UnregisterSpaceSplitterType(Type type)
        {
            s_Types.Remove(type);
        }

        public static Type[] GetTypes()
        {
            return s_Types.ToArray();
        }

        public static ISpaceSplitter CreateInstance(HLOD hlod)
        {
            if (s_Types.IndexOf(hlod.SpaceSplitterType) < 0)
                return null;
            
            ISpaceSplitter spaceSplitter =
                (ISpaceSplitter) Activator.CreateInstance(hlod.SpaceSplitterType,
                    new object[] {hlod.SpaceSplitterOptions});
            
            return spaceSplitter;
        }
    }



}