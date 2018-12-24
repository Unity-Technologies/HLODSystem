using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public class DefaultController : ControllerBase
    {
        public override IEnumerator Load()
        {
            yield break;
        }
        public override void Show()
        {
            gameObject.SetActive(true);            
        }

        public override void Hide()
        {
            gameObject.SetActive(false);            
        }

        public override void Enable()
        {
            enabled = true;
        }
        public override void Disable()
        {
            enabled = false;
        }
    }

}