using UnityEditor;

namespace Unity.HLODSystem.Simplifier
{
    class NotUseSimplifier : ISimplifier
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            //This simplifier should be first always.
            SimplifierTypes.RegisterType(typeof(NotUseSimplifier), -1);
        }

        public void Simplify(HLOD hlod)
        {
        }
    }
}
