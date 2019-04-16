using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.HLODSystem.Streaming
{
    using ControllerID = Int32;
    public abstract class ControllerBase : MonoBehaviour
    {
        //This should be a coroutine.
        public abstract IEnumerator GetHighObject(ControllerID id, Action<GameObject> callback);

        public abstract IEnumerator GetLowObject(ControllerID id, Action<GameObject> callback);

        public abstract void ReleaseHighObject(ControllerID id);
        public abstract void ReleaseLowObject(ControllerID id);


    }

}