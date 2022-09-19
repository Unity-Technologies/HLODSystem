using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public static List<GameObject> HLODTargets(GameObject root)
        {
            List<GameObject> targets = new List<GameObject>();

            List<HLODMeshSetter> meshSetters = GetComponentsInChildren<HLODMeshSetter>(root);
            List<LODGroup> lodGroups = GetComponentsInChildren<LODGroup>(root);
            //This contains all of the mesh renderers, so we need to remove the duplicated mesh renderer which in the LODGroup.
            List<MeshRenderer> meshRenderers = GetComponentsInChildren<MeshRenderer>(root);

            for (int mi = 0; mi < meshSetters.Count; ++mi)
            {
                if (meshSetters[mi].enabled == false)
                    continue;
                if (meshSetters[mi].gameObject.activeInHierarchy == false)
                    continue;
                
                targets.Add(meshSetters[mi].gameObject);

                lodGroups.RemoveAll(meshSetters[mi].GetComponentsInChildren<LODGroup>());
                meshRenderers.RemoveAll(meshSetters[mi].GetComponentsInChildren<MeshRenderer>());
            }

            for (int i = 0; i < lodGroups.Count; ++i)
            {
                if ( lodGroups[i].enabled == false )
                    continue;
                if (lodGroups[i].gameObject.activeInHierarchy == false)
                    continue;

                targets.Add(lodGroups[i].gameObject);

                meshRenderers.RemoveAll(lodGroups[i].GetComponentsInChildren<MeshRenderer>());
            }

            //Combine renderer which in the LODGroup and renderer which without the LODGroup.
            for (int ri = 0; ri < meshRenderers.Count; ++ri)
            {
                if (meshRenderers[ri].enabled == false)
                    continue;
                if (meshRenderers[ri].gameObject.activeInHierarchy == false)
                    continue;

                targets.Add(meshRenderers[ri].gameObject);
            }
            
            //Combine several LODGroups and MeshRenderers belonging to Prefab into one.
            //Since the minimum unit of streaming is Prefab, it must be set to the minimum unit.
            HashSet<GameObject> targetsByPrefab = new HashSet<GameObject>();
            for (int ti = 0; ti < targets.Count; ++ti)
            {
                var targetPrefab = GetCandidatePrefabRoot(root, targets[ti]);
                targetsByPrefab.Add(targetPrefab);
            }

            return targetsByPrefab.ToList();
        }

        //This is finding nearest prefab root from the HLODRoot.
        public static GameObject GetCandidatePrefabRoot(GameObject hlodRoot, GameObject target)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(target) == false)
                return target;

            GameObject candidate = target;
            GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(target);

            while (Equals(target,outermost) == false && 
                   Equals(GetParent(target), hlodRoot) == false)    //< HLOD root should not be included.
            {
                target = GetParent(target);
                if (PrefabUtility.IsAnyPrefabInstanceRoot(target))
                {
                    candidate = target;
                }
            }

            return candidate;
        }

        private static GameObject GetParent(GameObject go)
        {
            return go.transform.parent.gameObject;
        }
        
        
        
        
        
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        public static void CopyValues<T>(T source, T target)
        {
            System.Type type = source.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(target, field.GetValue(source));
            }
        }

        public static string ObjectToPath(Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return "";

            if (AssetDatabase.IsMainAsset(obj) == false)
            {
                path += "[" + obj.name + "]";
            }

            return path;
        }

        public static void ParseObjectPath(string path, out string mainPath, out string subAssetName)
        {
            string[] splittedStr = path.Split('[', ']');
            mainPath = splittedStr[0];
            if (splittedStr.Length > 1)
            {
                subAssetName = splittedStr[1];
            }
            else
            {
                subAssetName = null;
            }
        }


    }

}