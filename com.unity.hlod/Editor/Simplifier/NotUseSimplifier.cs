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

        public NotUseSimplifier(HLOD hlod)
        {

        }

        public IEnumerator Simplify(HLODBuildInfo info)
        {
            info.simplifiedMeshes = new List<Mesh>(info.renderers.Count);
            for (int i = 0; i < info.renderers.Count; ++i)
            {
                var mf = info.renderers[i].GetComponent<MeshFilter>();
                if (mf == null)
                {
                    info.simplifiedMeshes.Add(null);
                }
                else
                {
                    info.simplifiedMeshes.Add(Object.Instantiate(mf.sharedMesh));    
                }
            }

            yield break;
        }

        public IEnumerator Simplify(HLOD hlod)
        {
            yield break;
        }
    }
}
