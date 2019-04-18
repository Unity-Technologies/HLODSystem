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
            camPosition = m_hlod.transform.worldToLocalMatrix.MultiplyPoint(cam.transform.position);

        }

        public bool IsHigh(Bounds bounds)
        {
            //float distance = 1.0f;
            //if (cam.orthographic == false)
            
                float distance = GetDistance(bounds.center, camPosition);
            float relativeHeight = bounds.size.x * preRelative / distance;
            return relativeHeight > m_hlod.LODDistance;
        }

        public bool IsCull(Bounds bounds)
        {
            float distance = GetDistance(bounds.center, camPosition);

            float relativeHeight = bounds.size.x * preRelative / distance;
            return relativeHeight < m_hlod.CullDistance;
        }

        private float GetDistance(Vector3 boundsPos, Vector3 camPos)
        {
            float x = Mathf.Abs(boundsPos.x - camPos.x);
            float z = Mathf.Abs(boundsPos.z - camPos.z);
            float square = x * x + z * z;
            return Mathf.Sqrt(square);
        }
    }

}