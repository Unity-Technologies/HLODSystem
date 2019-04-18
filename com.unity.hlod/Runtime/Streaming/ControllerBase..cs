using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.HLODSystem.Streaming
{
    using ControllerID = Int32;
    public abstract class ControllerBase : MonoBehaviour
    {
        #region Interface
        public abstract void Install();


        public abstract void OnStart();
        public abstract void OnStop();

        //This should be a coroutine.
        public abstract IEnumerator GetHighObject(ControllerID id, Action<GameObject> callback);

        public abstract IEnumerator GetLowObject(ControllerID id, Action<GameObject> callback);

        public abstract void ReleaseHighObject(ControllerID id);
        public abstract void ReleaseLowObject(ControllerID id);
        #endregion


    }

}