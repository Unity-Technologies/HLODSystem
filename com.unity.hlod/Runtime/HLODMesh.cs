using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace Unity.HLODSystem
{

    [PreferBinarySerialization]
    public class HLODMesh : ScriptableObject
    {
        [SerializeField]
        private Vector3[] m_vertices;
        [SerializeField]
        private Vector3[] m_normals;
        [SerializeField]
        private Vector2[] m_uv;
        [SerializeField]
        private int[] m_triangles;
        [SerializeField]
        private Material m_material;

        public Material Material
        {
            set { m_material = value; }
            get { return m_material; }
        }

        public void FromMesh(Mesh mesh)
        {
            m_vertices = new Vector3[mesh.vertexCount];
            m_normals = new Vector3[mesh.vertexCount];
            m_uv = new Vector2[mesh.vertexCount];
            

            m_triangles = new int[mesh.triangles.Length];

            System.Array.Copy(mesh.vertices, m_vertices, mesh.vertexCount);
            System.Array.Copy(mesh.normals, m_normals, mesh.vertexCount);
            System.Array.Copy(mesh.uv, m_uv, mesh.vertexCount);
            System.Array.Copy(mesh.triangles, m_triangles, mesh.triangles.Length);
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();
            if (m_vertices.Length > 0xffff)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            else
            {
                mesh.indexFormat = IndexFormat.UInt16;
            }

            mesh.vertices = m_vertices;
            mesh.normals = m_normals;
            mesh.uv = m_uv;
            mesh.triangles = m_triangles;

            return mesh;
        }
    }
}