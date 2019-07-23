using System.Collections.Generic;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace Unity.HLODSystem
{
    public class MeshDataBuilder : IProcessSceneWithReport
    {
        public int callbackOrder
        {
            get { return 0; }
        }
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            //this case is enter the playmode.
            if (report == null)
                return;
            if (report == null)
                return;
            
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int oi = 0; oi < rootObjects.Length; ++oi)
            {
                List<MeshDataRenderer> renderers = new List<MeshDataRenderer>();
                List<HLOD> hlods = new List<HLOD>();
                List<TerrainHLOD> terrainHlods = new List<TerrainHLOD>();
                
                FindComponentsInChild(rootObjects[oi], ref renderers);
                FindComponentsInChild(rootObjects[oi], ref hlods);
                FindComponentsInChild(rootObjects[oi], ref terrainHlods);

                for (int hi = 0; hi < hlods.Count; ++hi)
                {
                    Object.DestroyImmediate(hlods[hi]);
                }
                for (int hi = 0; hi < terrainHlods.Count; ++hi)
                {
                    Object.DestroyImmediate(terrainHlods[hi]);
                }

                for (int ri = 0; ri < renderers.Count; ++ri)
                {
                    ReplaceRenderer(renderers[ri]);
                }
            }
        }

        private void FindComponentsInChild<T>(GameObject target, ref List<T> components)
        {
            var component = target.GetComponent<T>();
            if (component != null)
                components.Add(component);

            foreach (Transform child in target.transform)
            {
                FindComponentsInChild(child.gameObject, ref components);
            }
        }

        private void ReplaceRenderer(MeshDataRenderer renderer)
        {
            MeshData data = renderer.Data;
            GameObject go = renderer.gameObject;
            
            Object.DestroyImmediate(renderer);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            using (WorkingMesh wm = data.Mesh.ToWorkingMesh(Allocator.Temp))
            {
                mf.sharedMesh = wm.ToMesh();
            }
            
            List<Material> materials = new List<Material>();
            for (int i = 0; i < data.GetMaterialDataCount(); ++i)
            {
                var md = data.GetMaterialData(i);
                Material mat = new Material(md.Material);

                for (int ti = 0; ti < md.Textures.Count; ++ti)
                {
                    var td = md.Textures[ti]; 
                    Texture2D tex = new Texture2D(
                        td.Width, 
                        td.Height, 
                        GraphicsFormatUtility.GetTextureFormat(td.Format), 
                        false,
                        !GraphicsFormatUtility.IsSRGBFormat(td.Format));
                    tex.LoadRawTextureData(td.Bytes);
                    tex.Apply();
                    //EditorUtility.CompressTexture(tex, TextureFormat.BC7, TextureCompressionQuality.Normal);
                    
                    mat.SetTexture(td.Name,tex);
                }
                
                mat.EnableKeyword("_NORMALMAP");
                materials.Add(mat);
            }

            mr.sharedMaterials = materials.ToArray();
        }
 
    }
}