using System.Collections;

namespace Unity.HLODSystem.Simplifier
{
    public interface ISimplifier
    {
        IEnumerator Simplify(HLOD hlod);

        IEnumerator Simplify(HLODBuildInfo buildInfo);


    }
}