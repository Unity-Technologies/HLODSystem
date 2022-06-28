using UnityEngine;

namespace Unity.HLODSystem.Serializer
{
    public class EmptyUserDataSerializer : UserDataSerializerBase
    {
        protected override void SerializeUserData(GameObject gameObject, HLODUserData data)
        {
            
        }

        protected override void DeserializeUserData(GameObject gameObject, HLODUserData data)
        {
            
        }
    }
}