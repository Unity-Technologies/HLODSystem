using System.Collections;

namespace Unity.HLODSystem.Simplifier
{
    public interface ISimplifier
    {
        IEnumerator Simplify(HLODBuildInfo buildInfo);


    }
}