using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 使用mesh实现line
    /// Line 永远朝向上方，即顶点都在xz平面
    /// </summary>
    public class QuadMeshLine
    {
        public GameObject gameObject { get; private set; }

        private Mesh mesh_;

        private float _width = 1;
        public float width { 
            get
            {
                return _width;
            }
            set
            {
                _width = value;
                SetLineWidth(_width);
            }
        }
        public int positionCount => positions_.Count;
        private List<Vector3> positions_ = new List<Vector3>();

        private Material material_;
        public Material material
        {
            get
            {
                return material_;
            }

            set
            {
                material_ = value;
                gameObject.GetComponent<MeshRenderer>().material = value;
            }
        }
        public static QuadMeshLine Create(string name, List<Vector3> positions, float width)
        {
            Debug.Assert(positions.Count > 1);

            GameObject go = new GameObject(name);
            go.transform.position = positions[0];

            Vector3[] vertices = new Vector3[(positions.Count - 1) * 4];
            int[] triangles = new int[(positions.Count - 1) * 6];
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < positions.Count - 1; i++) 
            {
                var leftMiddle = go.transform.InverseTransformPoint(positions[i]);
                var rightMiddle = go.transform.InverseTransformPoint(positions[i + 1]);

                var forward = (rightMiddle - leftMiddle).normalized;
                var tangent = Vector3.Cross(forward, Vector3.up);

                vertices[i * 4 + 0] = leftMiddle + tangent * 0.5f * width;
                vertices[i * 4 + 1] = leftMiddle - tangent * 0.5f * width;
                vertices[i * 4 + 2] = rightMiddle + tangent * 0.5f * width;
                vertices[i * 4 + 3] = rightMiddle - tangent * 0.5f * width;

                float u0 = 0;
                float distance = Vector3.Distance(leftMiddle, rightMiddle);
                float u1 = distance;
                uvs[i * 4 + 0] = new Vector2(u0, 1);
                uvs[i * 4 + 1] = new Vector2(u0, 0);
                uvs[i * 4 + 2] = new Vector2(u1, 1);
                uvs[i * 4 + 3] = new Vector2(u1, 0);

                triangles[i * 6 + 0] = i * 4 + 0;
                triangles[i * 6 + 1] = i * 4 + 2;
                triangles[i * 6 + 2] = i * 4 + 1;
                triangles[i * 6 + 3] = i * 4 + 1;
                triangles[i * 6 + 4] = i * 4 + 2;
                triangles[i * 6 + 5] = i * 4 + 3;
            }

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();

            //
            var line = new QuadMeshLine();
            line.gameObject = go;
            line.mesh_ = mesh;
            line._width = width;
            line.positions_ = new List<Vector3>();
            for(int i = 0; i < positions.Count; ++i)
            {
                line.positions_.Add(positions[i]);
            }
            return line;
        }

        public static void Destroy(QuadMeshLine line)
        {
            if (line != null)
            {
                GameObject.Destroy(line.gameObject);
            }
        }

        /// <summary>
        /// 更新顶点
        /// </summary>
        /// <param name="index"></param>
        /// <param name="position"></param>
        public void SetPosition(int index,Vector3 position)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(mesh_ != null);

            List<Vector3> vertices = new List<Vector3>(mesh_.vertexCount);
            mesh_.GetVertices(vertices);

            List<Vector2> uvs = new List<Vector2>(mesh_.vertexCount);
            mesh_.GetUVs(0, uvs);

            Debug.Assert(vertices.Count % 4 == 0 && vertices.Count / 4 + 1 == positionCount);
            Debug.Assert(positionCount > 1);
            Debug.Assert(index < positionCount);

            if(index - 1 >= 0 && index - 1 < positionCount - 1)
            {
                // 前一段
                var leftMiddle = (vertices[(index - 1) * 4 + 0] + vertices[(index - 1) * 4 + 1]) * 0.5f;
                var rightMiddle = gameObject.transform.InverseTransformPoint(position);
                var forward = (rightMiddle - leftMiddle).normalized;
                var tangent = Vector3.Cross(forward, Vector3.up);

                vertices[(index - 1) * 4 + 0] = leftMiddle + tangent * 0.5f * width;
                vertices[(index - 1) * 4 + 1] = leftMiddle - tangent * 0.5f * width;
                vertices[(index - 1) * 4 + 2] = rightMiddle + tangent * 0.5f * width;
                vertices[(index - 1) * 4 + 3] = rightMiddle - tangent * 0.5f * width;

                float distance = Vector3.Distance(leftMiddle,rightMiddle);
                uvs[(index - 1) * 4 + 0] = new Vector2(0, 1);
                uvs[(index - 1) * 4 + 1] = new Vector2(0, 0);
                uvs[(index - 1) * 4 + 2] = new Vector2(distance, 1);
                uvs[(index - 1) * 4 + 3] = new Vector2(distance, 0);
            }

            if (index+1 < positionCount)
            {
                // 后一段
                var leftMiddle = gameObject.transform.InverseTransformPoint(position);
                var rightMiddle = (vertices[index * 4 + 2] + vertices[index * 4 + 3]) * 0.5f;
                var forward = (rightMiddle - leftMiddle).normalized;
                var tangent = Vector3.Cross(forward, Vector3.up);

                vertices[index * 4 + 0] = leftMiddle + tangent * 0.5f * width;
                vertices[index * 4 + 1] = leftMiddle - tangent * 0.5f * width;
                vertices[index * 4 + 2] = rightMiddle + tangent * 0.5f * width;
                vertices[index * 4 + 3] = rightMiddle - tangent * 0.5f * width;

                float distance = Vector3.Distance(leftMiddle, rightMiddle);
                uvs[index * 4 + 0] = new Vector2(0, 1);
                uvs[index * 4 + 1] = new Vector2(0, 0);
                uvs[index * 4 + 2] = new Vector2(distance, 1);
                uvs[index * 4 + 3] = new Vector2(distance, 0);
            }

            //
            positions_[index] = position;

            mesh_.SetVertices(vertices);
            mesh_.SetUVs(0, uvs);
            mesh_.RecalculateBounds();
            mesh_.RecalculateNormals();
        }

        public void AddPosition(Vector3 position)
        {
            Debug.Assert(positionCount > 1);
            Debug.Assert(mesh_ != null);

            List<Vector3> vertices = new List<Vector3>(mesh_.vertexCount);
            mesh_.GetVertices(vertices);
            Debug.Assert(positionCount == (vertices.Count / 4 + 1));

            var leftMiddle = (vertices[vertices.Count - 1] + vertices[vertices.Count - 2]) * 0.5f;
            var rightMiddle = gameObject.transform.InverseTransformPoint(position);
            var forward = (rightMiddle - leftMiddle).normalized;
            var tangent = Vector3.Cross(forward, Vector3.up);

            int c = vertices.Count;
            vertices.Add(leftMiddle + tangent * 0.5f * width);
            vertices.Add(leftMiddle - tangent * 0.5f * width);
            vertices.Add(rightMiddle + tangent * 0.5f * width);
            vertices.Add(rightMiddle - tangent * 0.5f * width);

            List<Vector2> uvs = new List<Vector2>();
            mesh_.GetUVs(0, uvs);

            float distance = Vector3.Distance(leftMiddle, rightMiddle);
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(distance, 1));
            uvs.Add(new Vector2(distance, 0));

            List<int> indices = new List<int>();
            mesh_.GetTriangles(indices, 0);
            indices.Add(c);
            indices.Add(c + 2);
            indices.Add(c + 1);
            indices.Add(c + 1);
            indices.Add(c + 2);
            indices.Add(c + 3);

            mesh_.SetVertices(vertices);
            mesh_.SetTriangles(indices, 0);
            mesh_.SetUVs(0, uvs);
            mesh_.RecalculateBounds();
            mesh_.RecalculateNormals();

            //
            positions_.Add(position);
        }

        public void RemovePosition(int index)
        {
            Debug.Assert(index >= 0 && index < positionCount);
            Debug.Assert(mesh_);

            List<Vector3> vertices = new List<Vector3>(mesh_.vertexCount);
            mesh_.GetVertices(vertices);

            List<Vector2> uvs = new List<Vector2>();
            mesh_.GetUVs(0, uvs);

            //删除顶点
            if (index == 0)
            {
                vertices.RemoveRange(0, 4);
                uvs.RemoveRange(0, 4);
            }
            else if (index == positionCount - 1)
            {
                vertices.RemoveRange(vertices.Count - 4, 4);
                uvs.RemoveRange(uvs.Count - 4, 4);
            }
            else
            {
                vertices.RemoveRange((index - 1) * 4 + 2, 4);
                uvs.RemoveRange((index - 1) * 4 + 2, 4);

                // 更新连接处的顶点
                var leftMiddle = (vertices[(index - 1) * 4 + 0] + vertices[(index - 1) * 4 + 1]) * 0.5f;
                var rightMiddle = (vertices[(index - 1) * 4 + 2] + vertices[(index - 1) * 4 + 3]) * 0.5f;
                var forward = (rightMiddle - leftMiddle).normalized;
                var tangent = Vector3.Cross(forward, Vector3.up);
                vertices[(index - 1) * 4 + 0] = leftMiddle + tangent * 0.5f * width;
                vertices[(index - 1) * 4 + 1] = leftMiddle - tangent * 0.5f * width;
                vertices[(index - 1) * 4 + 2] = rightMiddle + tangent * 0.5f * width;
                vertices[(index - 1) * 4 + 3] = rightMiddle - tangent * 0.5f * width;

                float distance = Vector3.Distance(leftMiddle, rightMiddle);
                uvs[(index - 1) * 4 + 0] = new Vector2(0, 1);
                uvs[(index - 1) * 4 + 1] = new Vector2(0, 0);
                uvs[(index - 1) * 4 + 2] = new Vector2(distance, 1);
                uvs[(index - 1) * 4 + 3] = new Vector2(distance, 0);
            }
            // 删除索引
            List<int> indices = new List<int>();
            mesh_.GetTriangles(indices, 0);
            indices.RemoveRange(indices.Count - 6, 6);

            mesh_.SetTriangles(indices, 0);
            mesh_.SetVertices(vertices);
            mesh_.SetUVs(0, uvs);
            mesh_.RecalculateBounds();
            mesh_.RecalculateNormals();

            //
            positions_.RemoveAt(index);
        }

        private void SetLineWidth(float width)
        {
            Debug.Assert(positionCount > 1);
            Debug.Assert(mesh_ != null);

            List<Vector3> vertices = new List<Vector3>(mesh_.vertexCount);
            mesh_.GetVertices(vertices);
            Debug.Assert(positionCount == (vertices.Count / 4 + 1));

            for(int i = 0; i < positionCount-1; i++)
            {
                var leftMiddle = (vertices[i * 4 + 0] + vertices[i * 4 + 1]) * 0.5f;
                var rightMiddle = (vertices[i * 4 + 2] + vertices[i * 4 + 3]) * 0.5f;

                var forward = (rightMiddle - leftMiddle).normalized;
                var tangent = Vector3.Cross(forward, Vector3.up);
                vertices[i * 4 + 0] = leftMiddle + tangent * 0.5f * width;
                vertices[i * 4 + 1] = leftMiddle - tangent * 0.5f * width;
                vertices[i * 4 + 2] = rightMiddle + tangent * 0.5f * width;
                vertices[i * 4 + 3] = rightMiddle - tangent * 0.5f * width;
            }

            mesh_.RecalculateBounds();
            mesh_.RecalculateNormals();
        }

        public void GetPositions(List<Vector3> positions)
        {
            if(positions == null)
            {
                Debug.LogError("positions must not be null.");
                return;
            }

            for (int i = 0; i < positions_.Count; i++)
            {
                positions.Add(positions_[i]);
            }
        }
    }
}

