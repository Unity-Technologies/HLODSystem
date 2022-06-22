using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        public struct SerializableMesh
        {
            [SerializeField] private string m_name;
            [SerializeField] private byte[] m_vertices;
            [SerializeField] private byte[] m_normals;
            [SerializeField] private byte[] m_tangents;
            [SerializeField] private byte[] m_uvs;
            [SerializeField] private byte[] m_uvs2;
            [SerializeField] private byte[] m_uvs3;
            [SerializeField] private byte[] m_uvs4;
            [SerializeField] private byte[] m_colors;
            [SerializeField] private List<int[]> m_indices;

            public string Name
            {
                set { m_name = value; }
                get { return m_name; }
            }

            private static byte[] ArrayToBytes<T>(T[] arr)
                where T : struct
            {
                int dataSize = Marshal.SizeOf<T>();
                byte[] buffer = new byte[dataSize * arr.Length];

                IntPtr ptr = Marshal.AllocHGlobal(dataSize);
                for (int i = 0; i < arr.Length; ++i)
                {
                    Marshal.StructureToPtr(arr[i], ptr, false);
                    Marshal.Copy(ptr, buffer, i * dataSize, dataSize);
                }

                Marshal.FreeHGlobal(ptr);

                return buffer;
            }

            private T[] BytesToArray<T>(Byte[] bytes)
                where T : struct
            {
                int dataSize = Marshal.SizeOf<T>();
                T[] array = new T[bytes.Length / dataSize];

                IntPtr ptr = Marshal.AllocHGlobal(dataSize);
                for (int i = 0; i < array.Length; ++i)
                {
                    Marshal.Copy(bytes, i * dataSize, ptr, dataSize);
                    array[i] = Marshal.PtrToStructure<T>(ptr);
                }

                Marshal.FreeHGlobal(ptr);

                return array;
            }

            public void From(WorkingMesh mesh)
            {
                m_name = mesh.name;

                m_vertices = ArrayToBytes(mesh.vertices);
                m_normals = ArrayToBytes(mesh.normals);
                m_tangents = ArrayToBytes(mesh.tangents);
                m_uvs = ArrayToBytes(mesh.uv);
                m_uvs2 = ArrayToBytes(mesh.uv2);
                m_uvs3 = ArrayToBytes(mesh.uv3);
                m_uvs4 = ArrayToBytes(mesh.uv4);
                m_colors = ArrayToBytes(mesh.colors);
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

                mesh.vertices = BytesToArray<Vector3>(m_vertices);
                mesh.normals = BytesToArray<Vector3>(m_normals);
                mesh.tangents = BytesToArray<Vector4>(m_tangents);
                mesh.uv = BytesToArray<Vector2>(m_uvs);
                mesh.uv2 = BytesToArray<Vector2>(m_uvs2);
                mesh.uv3 = BytesToArray<Vector2>(m_uvs3);
                mesh.uv4 = BytesToArray<Vector2>(m_uvs4);
                mesh.colors = BytesToArray<Color>(m_colors);

                mesh.subMeshCount = m_indices.Count;
                for (int i = 0; i < m_indices.Count; ++i)
                {
                    mesh.SetTriangles(m_indices[i], i);
                }

                return mesh;
            }

            public int GetSpaceUsage()
            {
                int usage = 0;
                usage += m_vertices.Length;
                usage += m_normals.Length;
                usage += m_tangents.Length;
                usage += m_uvs.Length;
                usage += m_uvs2.Length;
                usage += m_uvs3.Length;
                usage += m_uvs4.Length;
                usage += m_colors.Length;
                for ( int i = 0; i < m_indices.Count; ++i )
                {
                    usage += m_indices[i].Length * sizeof(int);
                }

                return usage;
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

            public string TextureName
            {
                get { return m_textureName; }
            }

            public int Height
            {
                get { return m_height; }
            }

            public int Width
            {
                get { return m_width; }
            }

            public GraphicsFormat GraphicsFormat
            {
                get { return m_format; }
            }

            public TextureWrapMode WrapMode
            {
                get { return m_wrapMode; }
            }
            public int BytesLength
            {
                get 
                { 
                    if (m_bytes == null)
                        return 0;
                    return m_bytes.Length; 
                }
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
            [SerializeField] private string m_assetGuid;
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
                if (needWrite)
                {
                    Material mat = material.ToMaterial();
                    m_jsonData = EditorJsonUtility.ToJson(mat);
                    m_assetGuid = "";
                }
                else
                {
                    m_jsonData = "";
                    string path  = AssetDatabase.GetAssetPath(material.InstanceID);
                    m_assetGuid = AssetDatabase.AssetPathToGUID(path);
                }

                m_id = material.Guid;
            }

            public Material To()
            {
                if (string.IsNullOrEmpty(m_assetGuid))
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    EditorJsonUtility.FromJsonOverwrite(m_jsonData, mat);
                    mat.name = m_name;
                    return mat;
                }
                else
                {
                    string path = AssetDatabase.GUIDToAssetPath(m_assetGuid);
                    var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                    for (int i = 0; i < objects.Length; ++i)
                    {
                        Material mat = objects[i] as Material;
                        
                        if (mat == null)
                            continue;

                        if (mat.name == m_name)
                            return mat;
                    }
                    
                    return AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
        }

        [Serializable]
        public class SerializableObject
        {
            [SerializeField] private string m_name;
            [SerializeField] private SerializableMesh m_mesh;
            [SerializeField] private List<string> m_materialIds = new List<string>();
            [SerializeField] private List<string> m_materialNames = new List<string>();
            [SerializeField] private LightProbeUsage m_lightProbeUsage;

            public string Name
            {
                set { m_name = value; }
                get { return m_name; }
            }

            public LightProbeUsage LightProbeUsage => m_lightProbeUsage;

            public SerializableMesh GetMesh()
            {
                return m_mesh;
            }

            public List<string> GetMaterialIds()
            {
                return m_materialIds;
            }

            public List<string> GetMaterialNames()
            {
                return m_materialNames;
            }

            public void From(WorkingObject obj)
            {
                Name = obj.Name;
                m_mesh.From(obj.Mesh);
                m_materialIds = new List<string>();
                m_materialNames = new List<string>();
                m_lightProbeUsage = obj.LightProbeUsage;
                for (int i = 0; i < obj.Materials.Count; ++i)
                {
                    m_materialIds.Add(obj.Materials[i].Guid);
                    m_materialNames.Add(obj.Materials[i].Name);
                }
            }
        }

        [Serializable]
        public struct SerializableVector3
        {
            [SerializeField]
            public float X;
            [SerializeField]
            public float Y;
            [SerializeField]
            public float Z;

            public SerializableVector3(Vector3 vector3)
            {
                X = vector3.x;
                Y = vector3.y;
                Z = vector3.z;
            }

            public Vector3 To()
            {
                return new Vector3(X, Y, Z);
            }
        }

        [Serializable]
        public struct SerializableQuaternion
        {
            [SerializeField]
            public float X;
            [SerializeField]
            public float Y;
            [SerializeField]
            public float Z;
            [SerializeField]
            public float W;

            public SerializableQuaternion(Quaternion quaternion)
            {
                X = quaternion.x;
                Y = quaternion.y;
                Z = quaternion.z;
                W = quaternion.w;
            }

            public Quaternion To()
            {
                return new Quaternion(X, Y, Z, W);
            }
        }

        [Serializable]
        public class SerializableCollider
        {
            [SerializeField]
            string m_name;
            [SerializeField]
            string m_type;
            [SerializeField]
            SerializableVector3 m_position;
            [SerializeField]
            SerializableQuaternion m_rotation;
            [SerializeField]
            SerializableVector3 m_scale;
            
            [SerializeField]
            SerializableDynamicObject m_parameters;

            public string Name
            {
                get => m_name;
                set => m_name = value;
            }

            public void From(WorkingCollider collider)
            {
                m_type = collider.Type;
                m_position = new SerializableVector3(collider.Position);
                m_rotation = new SerializableQuaternion(collider.Rotation);
                m_scale = new SerializableVector3(collider.Scale);
                m_parameters = collider.Parameters;
            }

            public GameObject CreateGameObject()
            {
                if (m_type == typeof(BoxCollider).Name)
                {
                    return CreateBoxCollider();
                }

                if (m_type == typeof(MeshCollider).Name)
                {
                    return CreateMeshCollider();
                }

                if (m_type == typeof(SphereCollider).Name)
                {
                    return CreateSphereCollider();
                }

                if (m_type == typeof(CapsuleCollider).Name)
                {
                    return CreateCapsuleCollider();
                }

                return null;
            }

            private GameObject CreateBoxCollider()
            {
                dynamic param = m_parameters;
                GameObject go = new GameObject("Collider");
                var col = go.AddComponent<BoxCollider>();

                go.transform.position = m_position.To();
                go.transform.rotation = m_rotation.To();
                go.transform.localScale = m_scale.To();

                Vector3 size;
                Vector3 center;
                size.x = param.SizeX;
                size.y = param.SizeY;
                size.z = param.SizeZ;
                center.x = param.CenterX;
                center.y = param.CenterY;
                center.z = param.CenterZ;

                col.size = size;
                col.center = center;

                return go;
            }

            private GameObject CreateMeshCollider()
            {
                dynamic param = m_parameters;
                string sharedMeshPath = param.SharedMeshPath;
                string mainAssetPath = "";
                string subAssetName = "";
                ObjectUtils.ParseObjectPath(sharedMeshPath, out mainAssetPath, out subAssetName);
                
                if (string.IsNullOrEmpty(mainAssetPath) == true)
                    return null;

                GameObject go = new GameObject("Collider");
                var col = go.AddComponent<MeshCollider>();

                go.transform.position = m_position.To();
                go.transform.rotation = m_rotation.To();
                go.transform.localScale = m_scale.To();

                if (string.IsNullOrEmpty(subAssetName) == true)
                {
                    col.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(mainAssetPath);
                }
                else
                {
                    UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(mainAssetPath);
                    for (int oi = 0; oi < objects.Length; ++oi)
                    {
                        if (objects[oi].name == subAssetName)
                        {
                            col.sharedMesh = objects[oi] as Mesh;
                            if (col.sharedMesh != null)
                            {
                                break;
                            }
                        }
                    }
                }
                
                col.convex = param.Convex;

                return go;
            }

            private GameObject CreateSphereCollider()
            {
                dynamic param = m_parameters;
                GameObject go = new GameObject("Collider");
                var col = go.AddComponent<SphereCollider>();

                go.transform.position = m_position.To();
                go.transform.rotation = m_rotation.To();
                go.transform.localScale = m_scale.To();

                Vector3 center;
                center.x = param.CenterX;
                center.y = param.CenterY;
                center.z = param.CenterZ;

                col.center = center;
                col.radius = param.Radius;

                return go;
            }

            private GameObject CreateCapsuleCollider()
            {
                dynamic param = m_parameters;
                GameObject go = new GameObject("Collider");
                var col = go.AddComponent<CapsuleCollider>();

                go.transform.position = m_position.To();
                go.transform.rotation = m_rotation.To();
                go.transform.localScale = m_scale.To();

                Vector3 center;
                center.x = param.CenterX;
                center.y = param.CenterY;
                center.z = param.CenterZ;

                col.center = center;
                col.radius = param.Radius;
                col.height = param.Height;
                col.direction = param.Direction;

                return go;
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
        [SerializeField] private List<SerializableCollider> m_colliders = new List<SerializableCollider>();

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

        public void AddFromWorkingColliders(string name, IList<WorkingCollider> wcList)
        {
            for (int i = 0; i < wcList.Count; ++i)
            {
                WorkingCollider wc = wcList[i];
                SerializableCollider sc = new SerializableCollider();
                sc.From(wc);
                sc.Name = name;
                m_colliders.Add(sc);
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

                    //Prevent duplication
                    if (GetMaterial(wm.Guid) != null)
                        continue;
                        
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
                if (GetMaterial(wm.Guid) != null)
                    continue;

                string path = AssetDatabase.GUIDToAssetPath(wm.Guid);
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                    return;

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

        public List<SerializableCollider> GetColliders()
        {
            return m_colliders;
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