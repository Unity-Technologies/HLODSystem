using System;
using System.Diagnostics;

namespace Unity.HLODSystem
{
    [Conditional("UNITY_CCU")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    class OptionalDependencyAttribute : Attribute
    {
        public string dependentClass;
        public string define;

        public OptionalDependencyAttribute(string dependentClass, string define)
        {
            this.dependentClass = dependentClass;
            this.define = define;
        }
    }
}
