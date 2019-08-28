using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{
    public class QuadTreeSpaceManager : ISpaceManager
    {

        private float preRelative;
        private Vector3 camPosition;
        public QuadTreeSpaceManager()
        {
        }
        public void UpdateCamera(Transform hlodTransform, Camera cam)
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
            camPosition = hlodTransform.worldToLocalMatrix.MultiplyPoint(cam.transform.position);
        }

        public bool IsHigh(float lodDistance, Bounds bounds)
        {
            //float distance = 1.0f;
            //if (cam.orthographic == false)
            
                float distance = GetDistance(bounds.center, camPosition);
            float relativeHeight = bounds.size.x * preRelative / distance;
            return relativeHeight > lodDistance;
        }

        public float GetDistanceSqure(Bounds bounds)
        {
            float x = bounds.center.x - camPosition.x;
            float z = bounds.center.z - camPosition.z;

            float square = x * x + z * z;
            return square;
        }
        
        public bool IsCull(float cullDistance, Bounds bounds)
        {
            float distance = GetDistance(bounds.center, camPosition);

            float relativeHeight = bounds.size.x * preRelative / distance;
            return relativeHeight < cullDistance;
        }

        private float GetDistance(Vector3 boundsPos, Vector3 camPos)
        {
            float x = boundsPos.x - camPos.x;
            float z = boundsPos.z - camPos.z;
            float square = x * x + z * z;
            return Mathf.Sqrt(square);
        }

       
    }

}