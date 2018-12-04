using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Streaming
{
    public class DefaultController : ControllerBase
    {
        private bool m_IsShow = false;

        public override bool IsReady()
        {
            return true;
        }

        public override bool IsShow()
        {
            return m_IsShow;
        }
        public override void Prepare()
        {

        }
        public override void Show()
        {
            m_IsShow = true;
            gameObject.SetActive(true);            
        }

        public override void Hide()
        {
            m_IsShow = false;
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