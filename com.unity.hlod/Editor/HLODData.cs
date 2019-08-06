using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Unity.HLODSystem
{
    [Serializable]
    public class HLODData
    {
        [Serializable]
        public struct TextureCompressionData
        {
            [SerializeField] public TextureFormat PCTextureFormat;
            [SerializeField] public TextureFormat WebGLTextureFormat;
            [SerializeField] public TextureFormat AndroidTextureFormat;
            [SerializeField] public TextureFormat iOSTextureFormat;
            [SerializeField] public TextureFormat tvOSTextureFormat;
        }

        [Serializable]
        public struct SerializableVector2
        {
            [SerializeField] private float x;
            [SerializeField] private float y;

            public void From(Vector2 v)
            {
                x = v.x;
                y = v.y;
            }

            public Vector2 To()
            {
                Vector2 v = new Vector2();
                v.x = x;
                v.y = y;
                return v;
            }

            public static SerializableVector2[] ConvertArrayFrom(Vector2[] arr)
            {
                SerializableVector2[] vectors = new SerializableVector2[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i].From(arr[i]);
                }

                return vectors;
            }

            public static Vector2[] ConvertArrayTo(SerializableVector2[] arr)
            {
                Vector2[] vectors = new Vector2[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i] = arr[i].To();
                }

                return vectors;
            }
        }
        [Serializable]
        public struct SerializableVector3
        {
            [SerializeField] private float x;
            [SerializeField] private float y;
            [SerializeField] private float z;

            public void From(Vector3 v)
            {
                x = v.x;
                y = v.y;
                z = v.z;
            }

            public Vector3 To()
            {
                Vector3 v = new Vector3();
                v.x = x;
                v.y = y;
                v.z = z;
                return v;
            }

            public static SerializableVector3[] ConvertArrayFrom(Vector3[] arr)
            {
                SerializableVector3[] vectors = new SerializableVector3[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i].From(arr[i]);
                }

                return vectors;
            }

            public static Vector3[] ConvertArrayTo(SerializableVector3[] arr)
            {
                Vector3[] vectors = new Vector3[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i] = arr[i].To();
                }

                return vectors;
            }
        }
        [Serializable]
        public struct SerializableVector4
        {
            [SerializeField] private float x;
            [SerializeField] private float y;
            [SerializeField] private float z;
            [SerializeField] private float w;

            public void From(Vector4 v)
            {
                x = v.x;
                y = v.y;
                z = v.z;
                w = v.w;
            }

            public Vector4 To()
            {
                Vector4 v = new Vector4();
                v.x = x;
                v.y = y;
                v.z = z;
                v.w = w;
                return v;
            }

            public static SerializableVector4[] ConvertArrayFrom(Vector4[] arr)
            {
                SerializableVector4[] vectors = new SerializableVector4[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i].From(arr[i]);
                }

                return vectors;
            }

            public static Vector4[] ConvertArrayTo(SerializableVector4[] arr)
            {
                Vector4[] vectors = new Vector4[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i] = arr[i].To();
                }

                return vectors;
            }
        }
        [Serializable]
        public struct SerializableColor
        {
            [SerializeField] private float r;
            [SerializeField] private float g;
            [SerializeField] private float b;

            public void From(Color c)
            {
                r = c.r;
                g = c.g;
                b = c.b;
            }

            public Color To()
            {
                Color c = new Color();
                c.r = r;
                c.g = g;
                c.b = b;
                return c;
            }

            public static SerializableColor[] ConvertArrayFrom(Color[] arr)
            {
                SerializableColor[] vectors = new SerializableColor[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i].From(arr[i]);
                }

                return vectors;
            }

            public static Color[] ConvertArrayTo(SerializableColor[] arr)
            {
                Color[] vectors = new Color[arr.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    vectors[i] = arr[i].To();
                }

                return vectors;
            }
        }
        
        [Serializable]
        public struct SerializableMesh
        {
            [SerializeField] private string m_name;
            [SerializeField] private SerializableVector3[] m_vertices;
            [SerializeField] private SerializableVector3[] m_normals;
            [SerializeField] private SerializableVector4[] m_tangents;
            [SerializeField] private SerializableVector2[] m_uvs;
            [SerializeField] private SerializableVector2[] m_uvs2;
            [SerializeField] private SerializableVector2[] m_uvs3;
            [SerializeField] private SerializableVector2[] m_uvs4;
            [SerializeField] private SerializableColor[] m_colors;
            [SerializeField] private List<int[]> m_indices;

            public void From(WorkingMesh mesh)
            {
                m_name = mesh.name;
                m_vertices = SerializableVector3.ConvertArrayFrom(mesh.vertices);
                m_normals = SerializableVector3.ConvertArrayFrom(mesh.normals);
                m_tangents = SerializableVector4.ConvertArrayFrom(mesh.tangents);
                m_uvs = SerializableVector2.ConvertArrayFrom(mesh.uv);
                m_uvs2 = SerializableVector2.ConvertArrayFrom(mesh.uv2);
                m_uvs3 = SerializableVector2.ConvertArrayFrom(mesh.uv3);
                m_uvs4 = SerializableVector2.ConvertArrayFrom(mesh.uv4);
                m_colors = SerializableColor.ConvertArrayFrom(mesh.colors);
                m_indices = new List<int[]>();
                for (int i = 0; i < mesh.subMeshCount; ++i)
                {
                    m_indices.Add(mesh.GetTriangles(i));
                }
            }

            public Mesh To()
            {
                Mesh mesh = new Mesh();
                mesh.name = m_name;
                if (m_vertices.Length > 65535)
                    mesh.indexFormat = IndexFormat.UInt32;
                
                mesh.vertices = SerializableVector3.ConvertArrayTo(m_vertices);
                mesh.normals = SerializableVector3.ConvertArrayTo(m_normals);
                mesh.tangents = SerializableVector4.ConvertArrayTo(m_tangents);
                mesh.uv = SerializableVector2.ConvertArrayTo(m_uvs);
                mesh.uv2 = SerializableVector2.ConvertArrayTo(m_uvs2);
                mesh.uv3 = SerializableVector2.ConvertArrayTo(m_uvs3);
                mesh.uv4 = SerializableVector2.ConvertArrayTo(m_uvs4);
                mesh.colors = SerializableColor.ConvertArrayTo(m_colors);

                mesh.subMeshCount = m_indices.Count;
                for (int i = 0; i < m_indices.Count; ++i)
                {
                    mesh.SetTriangles(m_indices[i], i);
                }
                
                return mesh;
            }
        }

        [Serializable]
        public struct SerializableTexture
        {
            [SerializeField] private string m_name;
            [SerializeField] private string m_textureName;
            [SerializeField] private GraphicsFormat m_format;
            [SerializeField] private TextureWrapMode m_wrapMode;
            [SerializeField] private int m_width;
            [SerializeField] private int m_height;
            [SerializeField] private byte[] m_bytes;

            public string Name
            {
                set { m_name = value; }
                get { return m_name; }
            }

            public void From(Texture2D texture)
            {
                m_textureName = texture.name;
                m_format = texture.graphicsFormat;
                m_wrapMode = texture.wrapMode;
                m_width = texture.width;
                m_height = texture.height;
                m_bytes = texture.EncodeToPNG();
            }

            public Texture2D To()
            {
                var textureFormat = GraphicsFormatUtility.GetTextureFormat(m_format);
                var srgb = GraphicsFormatUtility.IsSRGBFormat(m_format);
                Texture2D texture = new Texture2D(m_width, m_height, textureFormat, true, !srgb);
                texture.name = m_textureName;
                texture.LoadImage(m_bytes);
                texture.wrapMode = m_wrapMode;
                texture.Apply();
                return texture;
            }
        }

        [Serializable]
        public class SerializableMaterial
        {
            [SerializeField] private string m_name;
            [SerializeField] private string m_id;
            [SerializeField] private string m_assetPath;
            [SerializeField] private string m_jsonData;
            [SerializeField] private List<SerializableTexture> m_textures;
            
            public string ID
            {
                get { return m_id; }
            }
            public void AddTexture(SerializableTexture texture)
            {
                if (m_textures == null) 
                    m_textures = new List<SerializableTexture>();
                
                m_textures.Add(texture);
            }

            public int GetTextureCount()
            {
                if (m_textures == null)
                    return 0;
                return m_textures.Count;
            }

            public SerializableTexture GetTexture(int index)
            {
                return m_textures[index];
            }
            public void From(WorkingMaterial material)
            {
                m_name = material.Name;
                bool needWrite = material.NeedWrite(); 
                if ( needWrite )
                {
                    Material mat = material.ToMaterial();
                    m_jsonData = EditorJsonUtility.ToJson(mat);
                    m_assetPath = "";
                }
                else
                {
                    m_jsonData = "";
                    m_assetPath = AssetDatabase.GetAssetPath(material.InstanceID);
                }

                m_id = material.Guid;
            }

            public Material To()
            {
                if (string.IsNullOrEmpty(m_assetPath))
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    EditorJsonUtility.FromJsonOverwrite(m_jsonData, mat);
                    mat.name = m_name;
                    return mat;
                }
                else
                {
                    return AssetDatabase.LoadAssetAtPath<Material>(m_assetPath);
                }
            }
        }

        [Serializable]
        public class SerializableObject
        {
            [SerializeField] private string m_name;
            [SerializeField] private SerializableMesh m_mesh;
            [SerializeField] private List<string> m_materialIds = new List<string>();

            public string Name
            {
                set { m_name = value; }
                get { return m_name; }
            }

            public Mesh GetMesh()
            {
                return m_mesh.To();
            }
            public List<string> GetMaterialIds()
            {
                return m_materialIds;
            }
            
            public void From(WorkingObject obj)
            {
                Name = obj.Name;
                m_mesh.From(obj.Mesh);
                m_materialIds = new List<string>();
                for (int i = 0; i < obj.Materials.Count; ++i)
                {
                    m_materialIds.Add(obj.Materials[i].Guid);
                }
            }
        }


        public string Name
        {
            set { m_name = value; }
            get { return m_name; }
        }
        public TextureCompressionData CompressionData
        {
            set { m_compressionData = value; }
            get { return m_compressionData; }
        }

        [SerializeField] private string m_name;
        [SerializeField] private TextureCompressionData m_compressionData;

        [SerializeField] private List<SerializableObject> m_objects = new List<SerializableObject>();
        [SerializeField] private List<SerializableMaterial> m_materials = new List<SerializableMaterial>();

        public void AddFromWokringObjects(string name, IList<WorkingObject> woList)
        {
            for (int i = 0; i < woList.Count; ++i)
            {
                WorkingObject wo = woList[i];
                SerializableObject so = new SerializableObject();
                so.From(wo);
                m_objects.Add(so);
                
                AddFromWorkingMaterials(wo.Materials);
            }
        }

        public void AddFromGameObject(GameObject go)
        {
            using (WorkingObject wo = new WorkingObject(Allocator.Temp))
            {

                var mr = go.GetComponent<MeshRenderer>();
                if (mr == null)
                    return;
                
                wo.FromRenderer(mr);
                wo.Name = go.name;
                
                SerializableObject so = new SerializableObject();
                so.From(wo);

                for (int mi = 0; mi < wo.Materials.Count; ++mi)
                {
                    WorkingMaterial wm = wo.Materials[mi];
                    string[] textureNames = wm.GetTextureNames();
                    
                    SerializableMaterial sm = new SerializableMaterial();
                    sm.From(wm);

                    for (int ti = 0; ti < textureNames.Length; ++ti)
                    {
                        WorkingTexture tex = wm.GetTexture(textureNames[ti]);
                        if (tex == null)
                            continue;
                        
                        SerializableTexture st = new SerializableTexture();
                        st.From(tex.ToTexture());
                        st.Name = textureNames[ti];

                        sm.AddTexture(st);
                    }

                    m_materials.Add(sm);

                }
                
                m_objects.Add(so);
            }
        }

        private void AddFromWorkingMaterials(IList<WorkingMaterial> wmList)
        {
            for (int i = 0; i < wmList.Count; ++i)
            {
                WorkingMaterial wm = wmList[i];

                //Prevent duplication
                if (wm.NeedWrite() == false && GetMaterial(wm.Guid) != null)
                    continue;
                
                SerializableMaterial sm = new SerializableMaterial();
                sm.From(wmList[i]);
                
                string[] textureNames = wm.GetTextureNames();
                for (int ti = 0; ti < textureNames.Length; ++ti)
                {
                    WorkingTexture wt = wm.GetTexture(textureNames[ti]);
                    SerializableTexture st = new SerializableTexture();
                    st.From(wt.ToTexture());
                    st.Name = textureNames[ti];
                    
                    sm.AddTexture(st);
                }
                
                m_materials.Add(sm);
            }
        }

        public List<SerializableMaterial> GetMaterials()
        {
            return m_materials;
        }

        public List<SerializableObject> GetObjects()
        {
            return m_objects;
        }
        
        public int GetMaterialCount()
        {
            if (m_materials == null)
                return 0;

            return m_materials.Count;
        }

        private SerializableMaterial GetMaterial(string id)
        {
            for (int i = 0; i < m_materials.Count; ++i)
            {
                if (m_materials[i].ID == id)
                    return m_materials[i];
            }

            return null;
        }
        
        

    }
}