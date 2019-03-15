using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.HLODSystem.Streaming
{
    public abstract class ControllerBase : MonoBehaviour
    {

        public abstract void AddHLODMesh(HLODMesh mesh, Material mat);

        public abstract IEnumerator Load();

        public abstract void Show();
        public abstract void Hide();

        public abstract void Enable();
        public abstract void Disable();
    }

}