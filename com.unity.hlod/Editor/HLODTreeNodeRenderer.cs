using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TerrainUtils;

namespace Unity.HLODSystem
{
    public class HLODTreeNodeRenderer
    {
#if UNITY_EDITOR
        #region Singleton
        private static HLODTreeNodeRenderer s_instance;

        public static HLODTreeNodeRenderer Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new HLODTreeNodeRenderer();
                return s_instance;
            }
        }
        #endregion
        private Vector3[] m_allocatedVertices = new Vector3[8];
        #region public
        public void Render(HLODTreeNode node, Color color, float width)
        {
            if (node == null || node.Controller == null)
                return;
            
            Vector3 min = node.Bounds.min;
            Vector3 max = node.Bounds.max;

            
            m_allocatedVertices[0] = new Vector3(min.x, min.y, min.z);
            m_allocatedVertices[1] = new Vector3(min.x, min.y, max.z);
            m_allocatedVertices[2] = new Vector3(max.x, min.y, max.z);
            m_allocatedVertices[3] = new Vector3(max.x, min.y, min.z);

            m_allocatedVertices[4] = new Vector3(min.x, max.y, min.z);
            m_allocatedVertices[5] = new Vector3(min.x, max.y, max.z);
            m_allocatedVertices[6] = new Vector3(max.x, max.y, max.z);
            m_allocatedVertices[7] = new Vector3(max.x, max.y, min.z);

            for (int i = 0; i < m_allocatedVertices.Length; ++i)
            {
                m_allocatedVertices[i] = node.Controller.transform.localToWorldMatrix.MultiplyPoint(m_allocatedVertices[i]);
            }
            
            Handles.color = color;

            Handles.DrawLine(m_allocatedVertices[0], m_allocatedVertices[1], width);
            Handles.DrawLine(m_allocatedVertices[1], m_allocatedVertices[2], width);
            Handles.DrawLine(m_allocatedVertices[2], m_allocatedVertices[3], width);
            Handles.DrawLine(m_allocatedVertices[3], m_allocatedVertices[0], width);

            Handles.DrawLine(m_allocatedVertices[0], m_allocatedVertices[4], width);
            Handles.DrawLine(m_allocatedVertices[1], m_allocatedVertices[5], width);
            Handles.DrawLine(m_allocatedVertices[2], m_allocatedVertices[6], width);
            Handles.DrawLine(m_allocatedVertices[3], m_allocatedVertices[7], width);

            Handles.DrawLine(m_allocatedVertices[4], m_allocatedVertices[5], width);
            Handles.DrawLine(m_allocatedVertices[5], m_allocatedVertices[6], width);
            Handles.DrawLine(m_allocatedVertices[6], m_allocatedVertices[7], width);
            Handles.DrawLine(m_allocatedVertices[7], m_allocatedVertices[4], width);
        }

        #endregion

        private HLODTreeNodeRenderer()
        {
        }

#endif
    }
}