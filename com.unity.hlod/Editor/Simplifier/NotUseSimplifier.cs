using System.Collections;
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

        public IEnumerator Simplify(HLODBuildInfo info)
        {
            yield break;
        }

        public IEnumerator Simplify(HLOD hlod)
        {
            yield break;
        }
    }
}
