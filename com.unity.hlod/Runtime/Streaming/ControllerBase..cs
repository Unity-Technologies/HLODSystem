using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.HLODSystem.Streaming
{
    public abstract class ControllerBase : MonoBehaviour
    {
        public abstract bool IsReady();
        public abstract bool IsShow();

        public abstract void Show();
        public abstract void Hide();


    }

}