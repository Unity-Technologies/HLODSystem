using UnityEngine;

namespace Unity.HLODSystem.Serializer
{
    public class EmptyUserDataSerializer : UserDataSerializerBase
    {
        public override void SerializeUserData(GameObject gameObject, HLODUserData data)
        {
            
        }

        public override void DeserializeUserData(GameObject gameObject, HLODUserData data)
        {
            
        }
    }
}