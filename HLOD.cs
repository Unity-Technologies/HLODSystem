using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLOD : MonoBehaviour
    {
        [SerializeField]
        private float m_MinSize;
        [SerializeField]
        private float m_LODDistance = 0.3f;
        [SerializeField]
        private float m_CullDistance = 0.01f;
        [SerializeField]
        private float m_ThresholdSize;

        [SerializeField]
        private GameObject m_LowRoot;

        [SerializeField]
        private GameObject m_HighRoot;


        public GameObject LowRoot
        {
            set { m_LowRoot = value; }
            get { return m_LowRoot; }
        }

        public GameObject HighRoot
        {
            set { m_HighRoot = value; }
            get { return m_HighRoot; }
        }


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}