using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public class HLOD : MonoBehaviour, ISerializationCallbackReceiver, IGeneratedResourceManager
    {
        public const string HLODLayerStr = "HLOD";

        [SerializeField]
        private float m_ChunkSize = 30.0f;
        [SerializeField]
        private float m_LODDistance = 0.3f;
        [SerializeField]
        private float m_CullDistance = 0.01f;
        [SerializeField]
        private float m_MinObjectSize = 0.0f;

        private Type m_SpaceSplitterType;
        private Type m_BatcherType;
        private Type m_SimplifierType;
        private Type m_StreamingType;
        private Type m_UserDataSerializerType;


        [SerializeField] 
        private string m_SpaceSplitterTypeStr;
        [SerializeField]
        private string m_BatcherTypeStr;        //< unity serializer is not support serialization with System.Type
                                                //< So, we should convert to string to store value.
        [SerializeField]
        private string m_SimplifierTypeStr;
        [SerializeField]
        private string m_StreamingTypeStr;
        [SerializeField]
        private string m_UserDataSerializerTypeStr;

        [SerializeField]
        private SerializableDynamicObject m_SpaceSplitterOptions = new SerializableDynamicObject();
        [SerializeField]
        private SerializableDynamicObject m_SimplifierOptions = new SerializableDynamicObject();
        [SerializeField]
        private SerializableDynamicObject m_BatcherOptions = new SerializableDynamicObject();
        [SerializeField]
        private SerializableDynamicObject m_StreamingOptions = new SerializableDynamicObject();
        
        [SerializeField]
        private List<Object> m_generatedObjects = new List<Object>();
        [SerializeField]
        private List<GameObject> m_convertedPrefabObjects = new List<GameObject>();


        public float ChunkSize
        {
            get { return m_ChunkSize; }
        }

        public float LODDistance
        {
            get { return m_LODDistance; }
        }
        public float CullDistance
        {
            set { m_CullDistance = value; }
            get { return m_CullDistance; }
        }

        public Type SpaceSplitterType
        {
            set { m_SpaceSplitterType = value; }
            get { return m_SpaceSplitterType; }
        }

        public Type BatcherType
        {
            set { m_BatcherType = value; }
            get { return m_BatcherType; }
        }

        public Type SimplifierType
        {
            set { m_SimplifierType = value; }
            get { return m_SimplifierType; }
        }

        public Type StreamingType
        {
            set { m_StreamingType = value; }
            get { return m_StreamingType; }
        }

        public Type UserDataSerializerType
        {
            set { m_UserDataSerializerType = value; }
            get { return m_UserDataSerializerType; }
        }

        public SerializableDynamicObject SpaceSplitterOptions
        {
            get { return m_SpaceSplitterOptions; }
        }
        public SerializableDynamicObject BatcherOptions
        {
            get { return m_BatcherOptions; }
        }

        public SerializableDynamicObject StreamingOptions
        {
            get { return m_StreamingOptions; }
        }

        public SerializableDynamicObject SimplifierOptions
        {
            get { return m_SimplifierOptions; }
        }

        public float MinObjectSize
        {
            set { m_MinObjectSize = value; }
            get { return m_MinObjectSize; }
        }

        
#if UNITY_EDITOR
        public List<Object> GeneratedObjects
        {
            get { return m_generatedObjects; }
        }

        public List<GameObject> ConvertedPrefabObjects
        {
            get { return m_convertedPrefabObjects; }
        }

        public List<HLODControllerBase> GetHLODControllerBases()
        {
            List<HLODControllerBase> controllerBases = new List<HLODControllerBase>();

            foreach (Object obj in m_generatedObjects)
            {
                var controllerBase = obj as HLODControllerBase;
                if ( controllerBase != null )
                    controllerBases.Add(controllerBase);
            }
            
            //if controller base doesn't exists in the generated objects, it was created from old version.
            //so adding controller base manually.
            if (controllerBases.Count == 0)
            {
                var controller = GetComponent<Streaming.HLODControllerBase>();
                if (controller != null)
                {
                    controllerBases.Add(controller);
                }
            }
            return controllerBases;
        }
#endif
        public Bounds GetBounds()
        {
            Bounds ret = new Bounds();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                ret.center = Vector3.zero;
                ret.size = Vector3.zero;
                return ret;
            }

            Bounds bounds = Utils.BoundsUtils.CalcLocalBounds(renderers[0], transform);
            for (int i = 1; i < renderers.Length; ++i)
            {
                bounds.Encapsulate(Utils.BoundsUtils.CalcLocalBounds(renderers[i], transform));
            }

            ret.center = bounds.center;
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            ret.size = new Vector3(max, max, max);  

            return ret;
        }

    

        public void OnBeforeSerialize()
        {
            if (m_SpaceSplitterType != null)
                m_SpaceSplitterTypeStr = m_SpaceSplitterType.AssemblyQualifiedName;
            if ( m_BatcherType != null )
                m_BatcherTypeStr = m_BatcherType.AssemblyQualifiedName;
            if (m_SimplifierType != null)
                m_SimplifierTypeStr = m_SimplifierType.AssemblyQualifiedName;
            if (m_StreamingType != null)
                m_StreamingTypeStr = m_StreamingType.AssemblyQualifiedName;
            if (m_UserDataSerializerType != null)
                m_UserDataSerializerTypeStr = m_UserDataSerializerType.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_SpaceSplitterTypeStr))
            {
                m_SpaceSplitterType = null;
            }
            else
            {
                m_SpaceSplitterType = Type.GetType(m_SpaceSplitterTypeStr);
            }
            
            if (string.IsNullOrEmpty(m_BatcherTypeStr))
            {
                m_BatcherType = null;
            }
            else
            {
                m_BatcherType = Type.GetType(m_BatcherTypeStr);
            }

            if (string.IsNullOrEmpty(m_SimplifierTypeStr))
            {
                m_SimplifierType = null;
            }
            else
            {
                m_SimplifierType = Type.GetType(m_SimplifierTypeStr);
            }

            if (string.IsNullOrEmpty(m_StreamingTypeStr))
            {
                m_StreamingType = null;
            }
            else
            {
                m_StreamingType = Type.GetType(m_StreamingTypeStr);
            }

            if (string.IsNullOrEmpty(m_UserDataSerializerTypeStr))
            {
                m_UserDataSerializerType = null;
            }
            else
            {
                m_UserDataSerializerType = Type.GetType(m_UserDataSerializerTypeStr);
            }
            
        }

        public void AddGeneratedResource(Object obj)
        {
            m_generatedObjects.Add(obj);
        }

        public bool IsGeneratedResource(Object obj)
        {
            return m_generatedObjects.Contains(obj);
        }

        public void AddConvertedPrefabResource(GameObject obj)
        {
            m_convertedPrefabObjects.Add(obj);
        }
    }

}