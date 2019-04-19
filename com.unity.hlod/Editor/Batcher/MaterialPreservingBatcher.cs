using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.HLODSystem
{
    /// <summary>
    /// A batcher that preserves materials when combining meshes (does not reduce draw calls)
    /// </summary>
    class MaterialPreservingBatcher : IBatcher
    {
        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            BatcherTypes.RegisterBatcherType(typeof(MaterialPreservingBatcher));
        }

        private HLOD m_hlod;

        public MaterialPreservingBatcher(HLOD hlod)
        {
            m_hlod = hlod;
        }
  
        public void Batch(List<HLODBuildInfo> targets, Action<float> onProgress)
        {
            for (int i = 0; i < targets.Count; ++i)
            {
                Combine(targets[i]);

                if (onProgress != null)
                    onProgress((float) i / (float)targets.Count);
            }

        }

        private void Combine(HLODBuildInfo info)
        {
            var instancesTable = new Dictionary<Material, List<CombineInstance>>();

            for ( int i = 0; i < info.renderers.Count; ++i)
            {
                if (info.renderers[i] == null)
                    continue;

                var materials = info.renderers[i].sharedMaterials;
                    
                for(int m = 0; m < materials.Length; ++m)
                {
                    if (instancesTable.ContainsKey(materials[m]) == false)
                    {
                        instancesTable.Add(materials[m], new List<CombineInstance>());
                    }
                    var instance = new CombineInstance();
                    Matrix4x4 mat = info.renderers[i].localToWorldMatrix;
                    Vector3 position = m_hlod.transform.position;
                    mat.m03 -= position.x;
                    mat.m13 -= position.y;
                    mat.m23 -= position.z;
                    instance.transform = mat;
                    instance.mesh = info.simplifiedMeshes[i];
                    instance.subMeshIndex = m;

                    instancesTable[materials[m]].Add(instance);

                }

            }

            foreach (var instances in instancesTable)
            {
                var mesh = new Mesh();
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(instances.Value.ToArray(), true, true, false);
                mesh.name = instances.Key.name;

                var go = new GameObject(info.name + instances.Key.name, typeof(MeshRenderer), typeof(MeshFilter));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = instances.Key;

                go.transform.SetParent(m_hlod.transform);

                info.combinedGameObjects.Add(go);
            }
        }

        static void OnGUI(HLOD hlod)
        {

        }

    }
}
