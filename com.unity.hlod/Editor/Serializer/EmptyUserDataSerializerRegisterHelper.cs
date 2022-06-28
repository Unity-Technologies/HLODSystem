using UnityEditor;

namespace Unity.HLODSystem.Serializer
{
    public class EmptyUserDataSerializerRegisterHelper
    {
        [InitializeOnLoadMethod]
        static void Register()
        {
            UserDataSerializerTypes.RegisterType(typeof(EmptyUserDataSerializer), -1);
        }
    }
}