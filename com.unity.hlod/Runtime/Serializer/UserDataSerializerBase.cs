using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.HLODSystem.Streaming;
using UnityEngine;

namespace Unity.HLODSystem.Serializer
{
    public abstract class UserDataSerializerBase : MonoBehaviour , ISerializationCallbackReceiver
    {
        private Dictionary<int, int> m_idTable = new Dictionary<int, int>();

        [SerializeField]
        [HideInInspector]
        private List<int> m_idList = new List<int>();
        [SerializeField]
        [HideInInspector]
        private List<HLODUserData> m_userDataList = new List<HLODUserData>();
        
        public void SerializeUserData(HLODControllerBase controller, int id, GameObject gameObject)
        {
            int index = 0;
            int encodedID = EncodeID(controller.ControllerID, id);
            if (m_idTable.TryGetValue(encodedID, out index) == false)
            {
                HLODUserData userData = new HLODUserData();
                SerializeUserData(gameObject, userData);

                if (userData.HasAnyData())
                {
                    m_idList.Add(encodedID);
                    m_userDataList.Add(userData);
                    m_idTable[encodedID] = m_userDataList.Count - 1;
                }
            }
            else
            {
                HLODUserData userData = m_userDataList[index];
                SerializeUserData(gameObject, userData);
            }
        }

        public void DeserializeUserData(HLODControllerBase controller, int id, GameObject gameObject)
        {
            int index = 0;
            int encodedID = EncodeID(controller.ControllerID, id);
            if (m_idTable.TryGetValue(encodedID, out index) == false)
                return;
            
            DeserializeUserData(gameObject, m_userDataList[index]);
        }
        
        #region Interface
        protected abstract void SerializeUserData(GameObject gameObject, HLODUserData data);
        protected abstract void DeserializeUserData(GameObject gameObject, HLODUserData data);
        #endregion

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_idTable.Clear();
            
            for (int i = 0; i < m_idList.Count; ++i)
            {
                m_idTable[m_idList[i]] = i;
            }
        }

        private int EncodeID(int controllerID, int id)
        {
            return (controllerID & 0xff) << 24 | id;
        }
    }
}