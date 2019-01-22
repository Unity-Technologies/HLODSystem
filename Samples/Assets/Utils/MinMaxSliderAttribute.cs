
using System;
using UnityEngine;

public class MinMaxSliderAttribute : PropertyAttribute
{

    private readonly float m_max;
    private readonly float m_min;

    public float Max
    {
        get { return m_max; }
    }

    public float Min
    {
        get { return m_min; }
    }

    public MinMaxSliderAttribute (float min, float max)
    {
        m_min = min;
        m_max = max;
    }
}