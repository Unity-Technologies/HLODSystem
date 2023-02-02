using System;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    [Serializable]
    public class RangeInt
    {
        [SerializeField]
        private int m_minValue;
        [SerializeField]
        private int m_maxValue;
        [SerializeField]
        private int m_value;
        
        public int minValue
        {
            set
            {
                m_minValue = value;
            }
            get
            {
                return m_minValue;
            }
        }

        public int maxValue
        {
            set
            {
                m_maxValue = value;
            }
            get
            {
                return m_maxValue;
            }
        }

        public int value
        {
            set
            {
                m_value = value;
            }
            get
            {
                return m_value;
            }
        }
        
        public RangeInt(int min = 0, int max = 0, int value = 0)
        {
            m_minValue = min;
            m_maxValue = max;
            m_value = value;
        }
    }
}