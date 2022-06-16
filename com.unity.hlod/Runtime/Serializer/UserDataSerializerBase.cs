using UnityEngine;

namespace Unity.HLODSystem.Serializer
{
    public abstract class UserDataSerializerBase : MonoBehaviour
    {
        #region Interface
        public abstract void SerializeUserData(GameObject gameObject, HLODUserData data);
        public abstract void DeserializeUserData(GameObject gameObject, HLODUserData data);
        #endregion
    }
}