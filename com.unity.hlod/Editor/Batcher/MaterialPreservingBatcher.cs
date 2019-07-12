using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.HLODSystem.Utils;
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


        public MaterialPreservingBatcher(SerializableDynamicObject batcherOptions)
        {
        }
  
        public void Batch(Vector3 rootPosition, DisposableList<HLODBuildInfo> targets, Action<float> onProgress)
        {
            for (int i = 0; i < targets.Count; ++i)
            {
                Combine(rootPosition, targets[i]);

                if (onProgress != null)
                    onProgress((float) i / (float)targets.Count);
            }

        }

        


       
        
        private void Combine(Vector3 rootPosition, HLODBuildInfo info)
        {
            var instancesTable = new Dictionary<Material, List<CombineInstance>>();
            var combineInfos = new Dictionary<Guid, List<MeshCombiner.CombineInfo>>();

            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                var materials = info.WorkingObjects[i].Materials;
                for (int m = 0; m < materials.Count; ++m)
                {
                    //var mat = materials[m];
                    MeshCombiner.CombineInfo combineInfo = new MeshCombiner.CombineInfo();

                    combineInfo.Transform = info.WorkingObjects[i].LocalToWorld;
                    combineInfo.Transform.m03 -= rootPosition.x;
                    combineInfo.Transform.m13 -= rootPosition.y;
                    combineInfo.Transform.m23 -= rootPosition.z;
                    combineInfo.Mesh = info.WorkingObjects[i].Mesh;
                    combineInfo.MeshIndex = m;

                    if (combineInfos.ContainsKey(materials[m].GUID) == false)
                    {
                        combineInfos.Add(materials[m].GUID, new List<MeshCombiner.CombineInfo>());
                    }
                    
                    combineInfos[materials[m].GUID].Add(combineInfo);
                }
            }

            DisposableList<WorkingObject> combinedObjects = new DisposableList<WorkingObject>();
            MeshCombiner combiner = new MeshCombiner();
            foreach (var pair in combineInfos)
            {
                WorkingMesh combinedMesh = combiner.CombineMesh(Allocator.Persistent, pair.Value);
                WorkingObject combinedObject = new WorkingObject(Allocator.Persistent);
                WorkingMaterial material = new WorkingMaterial(Allocator.Persistent, pair.Key);

                combinedObject.SetMesh(combinedMesh);
                combinedObject.AddMaterial(material);
                material.Dispose();
                
                combinedObjects.Add(combinedObject);
            }

            //release before change
            info.WorkingObjects.Dispose();
            info.WorkingObjects = combinedObjects;
        }

        static void OnGUI(HLOD hlod)
        {

        }

    }
}
