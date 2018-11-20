using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        public MaterialPreservingBatcher()
        {
            
        }
        public void Batch(GameObject[] roots)
        {

            for (int i = 0; i < roots.Length; ++i)
            {
                Combine(roots[i]);
            }

        }

        private void Combine(GameObject root)
        {
            var instancesTable = new Dictionary<Material, List<CombineInstance>>();

            for(int i = root.transform.childCount - 1; i >= 0; --i)
            {
                var child = root.transform.GetChild(i);
                var go = child.gameObject;
                var renderers = go.GetComponentsInChildren<Renderer>();

                foreach(var renderer in renderers )
                {
                    if (renderer == null)
                        continue;

                    var materials = renderer.sharedMaterials;
                    
                    for(int m = 0; m < materials.Length; ++m)
                    {
                        if (instancesTable.ContainsKey(materials[m]) == false)
                        {
                            instancesTable.Add(materials[m], new List<CombineInstance>());
                        }
                        var instance = new CombineInstance();
                        instance.transform = child.localToWorldMatrix;
                        instance.mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        instance.subMeshIndex = m;

                        instancesTable[materials[m]].Add(instance);

                    }
                }

                Object.DestroyImmediate(go);
            }

            foreach (var instances in instancesTable)
            {
                var mesh = new Mesh();
                mesh.CombineMeshes(instances.Value.ToArray(), true, true, false);
                mesh.name = instances.Key.name;

                var go = new GameObject(instances.Key.name, typeof(MeshRenderer), typeof(MeshFilter));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = instances.Key;

                go.transform.SetParent(root.transform);
            }
        }

        static void OnGUI(HLOD hlod)
        {

        }

    }
}
