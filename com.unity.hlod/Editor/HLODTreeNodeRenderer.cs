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
        private Vector3[] m_allocedVertices = new Vector3[8];
        #region public
        public void Render(HLODTreeNode node, Color color, float width)
        {
            if (node == null || node.Controller == null)
                return;
            
            Vector3 min = node.Bounds.min;
            Vector3 max = node.Bounds.max;

            
            m_allocedVertices[0] = new Vector3(min.x, min.y, min.z);
            m_allocedVertices[1] = new Vector3(min.x, min.y, max.z);
            m_allocedVertices[2] = new Vector3(max.x, min.y, max.z);
            m_allocedVertices[3] = new Vector3(max.x, min.y, min.z);

            m_allocedVertices[4] = new Vector3(min.x, max.y, min.z);
            m_allocedVertices[5] = new Vector3(min.x, max.y, max.z);
            m_allocedVertices[6] = new Vector3(max.x, max.y, max.z);
            m_allocedVertices[7] = new Vector3(max.x, max.y, min.z);

            for (int i = 0; i < m_allocedVertices.Length; ++i)
            {
                m_allocedVertices[i] = node.Controller.transform.localToWorldMatrix.MultiplyPoint(m_allocedVertices[i]);
            }
            
            Handles.color = color;

            Handles.DrawLine(m_allocedVertices[0], m_allocedVertices[1], width);
            Handles.DrawLine(m_allocedVertices[1], m_allocedVertices[2], width);
            Handles.DrawLine(m_allocedVertices[2], m_allocedVertices[3], width);
            Handles.DrawLine(m_allocedVertices[3], m_allocedVertices[0], width);

            Handles.DrawLine(m_allocedVertices[0], m_allocedVertices[4], width);
            Handles.DrawLine(m_allocedVertices[1], m_allocedVertices[5], width);
            Handles.DrawLine(m_allocedVertices[2], m_allocedVertices[6], width);
            Handles.DrawLine(m_allocedVertices[3], m_allocedVertices[7], width);

            Handles.DrawLine(m_allocedVertices[4], m_allocedVertices[5], width);
            Handles.DrawLine(m_allocedVertices[5], m_allocedVertices[6], width);
            Handles.DrawLine(m_allocedVertices[6], m_allocedVertices[7], width);
            Handles.DrawLine(m_allocedVertices[7], m_allocedVertices[4], width);
        }

        #endregion

        private HLODTreeNodeRenderer()
        {
        }

#endif
    }
}