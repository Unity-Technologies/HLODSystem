using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        public NotUseSimplifier(SerializableDynamicObject simplifierOptions)
        {

        }

        public IEnumerator Simplify(HLODBuildInfo info)
        {
            yield break;
        }

        public void SimplifyImmidiate(HLODBuildInfo buildInfo)
        {
            
        }
    }
}
