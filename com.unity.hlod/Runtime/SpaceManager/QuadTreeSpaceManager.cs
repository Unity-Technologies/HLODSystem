using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{
    public class QuadTreeSpaceManager : ISpaceManager
    {
        private HLOD m_hlod;

        private float preRelative;
        private Vector3 camPosition;
        public QuadTreeSpaceManager(HLOD hlod)
        {
            m_hlod = hlod;
        }
        public void UpdateCamera(Camera cam)
        {
            
            if (cam.orthographic)
            {
                preRelative = 0.5f / cam.orthographicSize;
            }
            else
            {
                float halfAngle = Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView * 0.5F);
                preRelative = 0.5f / halfAngle;
            }
            preRelative = preRelative * QualitySettings.lodBias;
            camPosition = cam.transform.position;

        }

        public bool IsHigh(Bounds bounds)
        {
            //float distance = 1.0f;
            //if (cam.orthographic == false)
                float distance = Vector3.Distance(bounds.center, camPosition);
            float relativeHeight = bounds.size.x * preRelative / distance;
            return relativeHeight > m_hlod.LODDistance;
        }
    }

}