using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatchHelper : MonoBehaviour
{
    [SerializeField]
    private GameObject m_prefab;

    [SerializeField]
    private Bounds m_area;

    [SerializeField]
    private float m_density;


    [SerializeField]
    [MinMaxSlider(-180.0f, 180.0f)]
    private Vector2 m_rotation = new Vector2(-180.0f, 180.0f);
    [SerializeField]
    [MinMaxSlider(0.01f, 10.0f)]
    private Vector2 m_scale = new Vector2(0.8f, 1.2f);


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.3f);
        Gizmos.DrawCube(m_area.center + transform.position, m_area.extents);
    }



}
