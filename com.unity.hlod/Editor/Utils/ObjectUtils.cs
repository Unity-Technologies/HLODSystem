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

        
        public static HLODMesh SaveHLODMesh(string path, string name, WorkingObject gameObject)
        {
            path = Path.GetDirectoryName(path) + "/";
            path = path + name;
            
            HLODMesh hlodMesh = ScriptableObject.CreateInstance<HLODMesh>();
            hlodMesh.FromMesh(gameObject.Mesh.ToMesh());

            for (int i = 0; i < gameObject.Materials.Count; ++i)
            {
                Material mat = gameObject.Materials[i].ToMaterial();
                hlodMesh.Materials.Add(mat);
            }

            string meshName = path;
            AssetDatabase.CreateAsset(hlodMesh, meshName + ".asset");

            return hlodMesh;
        }
        public static List<GameObject> HLODTargets(GameObject root)
        {
            List<GameObject> targets = new List<GameObject>();

            List<LODGroup> lodGroups = GetComponentsInChildren<LODGroup>(root);
            //This contains all of the mesh renderers, so we need to remove the duplicated mesh renderer which in the LODGroup.
            List<MeshRenderer> meshRenderers = GetComponentsInChildren<MeshRenderer>(root).ToList();
            
            for (int i = 0; i < lodGroups.Count; ++i)
            {
                LOD[] lods = lodGroups[i].GetLODs();
                targets.Add(lodGroups[i].gameObject);

                var childMeshRenderers = lodGroups[i].GetComponentsInChildren<MeshRenderer>();
                for (int ri = 0; ri < childMeshRenderers.Length; ++ri)
                {
                    meshRenderers.Remove(childMeshRenderers[ri]);
                }
            }

            //Combine renderer which in the LODGroup and renderer which without the LODGroup.
            targets.AddRange(meshRenderers.Select(r => r.gameObject));

            return targets;
        }

    }

}