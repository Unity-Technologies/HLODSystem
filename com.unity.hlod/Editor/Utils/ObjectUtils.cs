using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public static class ObjectUtils
    {
        //It must order by child first.
        //Because we need to make child prefab first.
        public static List<T> GetComponentsInChildren<T>(GameObject root) where T : Component
        {
            LinkedList<T> result = new LinkedList<T>();
            Queue<GameObject> queue = new Queue<GameObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                GameObject go = queue.Dequeue();
                T component = go.GetComponent<T>();
                if (component != null)
                    result.AddFirst(component);

                foreach (Transform child in go.transform)
                {
                    queue.Enqueue(child.gameObject);
                }
            }

            return result.ToList();
        }

        
        public static List<HLODMesh> SaveHLODMesh(string path, string name, GameObject gameObject)
        {
            List<HLODMesh> result = new List<HLODMesh>();

            path = Path.GetDirectoryName(path) + "/";
            path = path + name;

            
            //store hlod meshes
            var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            for (int f = 0; f < meshFilters.Length; ++f)
            {
                var mesh = meshFilters[f].sharedMesh;

                var meshRenderer = meshFilters[f].GetComponent<MeshRenderer>();
                var material = meshRenderer.sharedMaterial;

                HLODMesh hlodmesh = ScriptableObject.CreateInstance<HLODMesh>();
                hlodmesh.FromMesh(mesh);
                hlodmesh.Material = material;
              
                string meshName = path + meshFilters[f].gameObject.name;

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))
                {
                    AssetDatabase.CreateAsset(material, meshName + ".mat");
                }
                AssetDatabase.CreateAsset(hlodmesh, meshName + ".asset");
                

                GameObject.DestroyImmediate(meshFilters[f].gameObject);
                result.Add(hlodmesh);

            }

            return result;
        }
        public static List<GameObject> HLODTargets(GameObject root)
        {
            List<GameObject> targets = new List<GameObject>();

            List<LODGroup> lodGroups = GetComponentsInChildren<LODGroup>(root);
            //This contains all of the mesh renderers, so we need to remove the duplicated mesh renderer which in the LODGroup.
            List<MeshRenderer> meshRenderers = GetComponentsInChildren<MeshRenderer>(root).ToList();

            //Remove low meshes.
            meshRenderers.RemoveAll(r => r.GetComponent<LowMeshHolder>() != null);
            

            for (int i = 0; i < lodGroups.Count; ++i)
            {
                LOD[] lods = lodGroups[i].GetLODs();
                targets.Add(lodGroups[i].gameObject);

                for (int li = 0; li < lods.Length; ++li)
                {
                    meshRenderers.RemoveAll(r => lods[li].renderers.Contains(r));
                }
            }

            //Combine renderer which in the LODGroup and renderer which without the LODGroup.
            targets.AddRange(meshRenderers.Select(r => r.gameObject));

            return targets;
        }

    }

}