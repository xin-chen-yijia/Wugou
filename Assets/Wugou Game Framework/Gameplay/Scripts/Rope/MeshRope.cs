using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 使用Mesh构建Rope，静态的，不会动
    /// </summary>
    public class MeshRope
    {
        public GameObject gameObject { get; private set; }

        public Mesh mesh { get; private set; }

        public Material material
        {
            get
            {
                return gameObject.GetComponent<MeshRenderer>().material;
            }

            set
            {
                gameObject.GetComponent<MeshRenderer>().material = value;
            }
        }

        public int sides { get; set; } = 12;
        public float radius { get; set; } = 0.05f;

        List<Vector3> points_ = new List<Vector3>();

        // 记录mesh的数据，用于后续更新
        private Vector3[] vertices_ = new Vector3[0];
        private Vector2[] uvs_ = new Vector2[0];
        private int[] triangles_ = new int[0];

        private MeshFilter meshFilter_ = null;

        /// <summary>
        /// 切面的三角形数量
        /// </summary>
        public int sectionTriangles => (sides - 2);

        public MeshRope(string name, Material material)
        {
            gameObject = new GameObject(name);
            meshFilter_ = gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();

            mesh = new Mesh();
            meshFilter_.mesh = mesh;

            this.material = material;
        }

        /// <summary>
        /// 计算以索引点为中心的圆周顶点数据
        /// </summary>
        /// <param name="point"></param>
        /// <param name="quaternion"></param>
        /// <param name="vertices"></param>
        /// <param name="startIndex"></param>
        void FillVertices(Vector3 point, Quaternion quaternion, Vector3[] vertices, int startIndex)
        {
            for (int j = 0; j < sides; ++j)
            {
                int vertexIndex = startIndex + j;

                Vector3 vertex = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (j * 60)) * radius, Mathf.Sin(Mathf.Deg2Rad * (j * 60)) * radius, 0);
                vertex = quaternion * vertex;  // rotation
                vertex += point;   // translate
                vertex = gameObject.transform.InverseTransformPoint(vertex);

                vertices[vertexIndex] = vertex;
            }
        }

        /// <summary>
        /// 设置顶点的uv
        /// </summary>
        /// <param name="uvs"></param>
        /// <param name="u">该圆周的顶点的u值</param>
        /// <param name="startIndex"></param>
        void FillVertexUV(Vector2[] uvs, float u, int startIndex)
        {
            for (int j = 0; j < sides; ++j)
            {
                int vertexIndex = startIndex + j;
                uvs[vertexIndex] = new Vector2(u, (float)j / sides);
            }
        }

        private void SetVertexNormal(Vector3[] normals, int startIndex)
        {
            for (int j = 0; j < sides; ++j)
            {
                int vertexIndex = startIndex + j;
                //normals[vertexIndex] = new Vector3(u, (float)j / sides);
            }
        }

        private enum SectionFrontFace
        {
            kClockwise = 0,
            kCounterClockwise
        }

        /// <summary>
        /// 用于添加分段切面
        /// </summary>
        /// <param name="index"></param>
        /// <param name="face"></param>
        /// <param name="triangles"></param>
        /// <param name="trianglesStartIndex"></param>
        /// <returns>返回切面所有三角形索引的数量</returns>
        private int FillSectionTriangles(int index, SectionFrontFace face, int[] triangles, int trianglesStartIndex)
        {
            if (face == SectionFrontFace.kClockwise)
            {
                int first = index * sides;
                for (int i = 0; i < sides - 2; ++i)
                {
                    triangles[trianglesStartIndex] = first + 0;
                    triangles[trianglesStartIndex + 1] = first + 2 + i;
                    triangles[trianglesStartIndex + 2] = first + 1 + i;

                    trianglesStartIndex += 3;
                }
            }
            else
            {
                int first = index * sides;
                for (int i = 0; i < sides - 2; ++i)
                {
                    triangles[trianglesStartIndex] = first + 0;
                    triangles[trianglesStartIndex + 1] = first + 1 + i;
                    triangles[trianglesStartIndex + 2] = first + 2 + i;

                    trianglesStartIndex += 3;
                }
            }

            return (sides - 2) * 3;
        }

        public void UpdateMesh()
        {
            // 更新mesh
            //mesh.vertices = vertices_;
            //mesh.uv = uvs_;
            //mesh.triangles = triangles_;

            if(vertices_.Length > 0)
            {
                mesh.SetVertices(vertices_);
                mesh.SetUVs(0, uvs_);
                mesh.SetTriangles(triangles_, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
            else
            {
                mesh.Clear();
            }

            meshFilter_.mesh = mesh;
        }


        public int pointCount => points_.Count;

        /// <summary>
        /// 设置绳子数据
        /// </summary>
        /// <param name="points"></param>
        public void SetPoints(List<Vector3> points)
        {
            points_ = new List<Vector3>(points);

            if (points_.Count < 2)
            {
                return;
            }

            // 更新顶点和uv数据
            vertices_ = new Vector3[points_.Count * sides];
            uvs_ = new Vector2[vertices_.Length];

            // 第一个点
            FillVertices(points_[0], Quaternion.FromToRotation(Vector3.forward, points_[1] - points_[0]), vertices_, 0);
            FillVertexUV(uvs_, 0, 0);

            // 中间的点
            for (int i = 1; i < points_.Count - 1; i++)
            {
                // 朝向
                Quaternion quat = Quaternion.Slerp(Quaternion.FromToRotation(Vector3.forward, points_[i] - points_[i - 1]), Quaternion.FromToRotation(Vector3.forward, points_[i + 1] - points_[i]), 0.5f);

                FillVertices(points_[i], quat, vertices_, i * sides);
                FillVertexUV(uvs_, i, i * sides);
            }

            // 最后一个点
            FillVertices(points_[points_.Count - 1], Quaternion.FromToRotation(Vector3.forward, points_[points_.Count - 1] - points_[points_.Count - 2]), vertices_, (points_.Count - 1) * sides);
            FillVertexUV(uvs_, 0, (points_.Count - 1) * sides);

            // 三角形索引
            int sideTriangles = (points_.Count - 1) * sides * 2 * 3; // 分段（圆柱）的三角形
            triangles_ = new int[sideTriangles + sectionTriangles * 3 * 2];    // 分段（圆柱）的三角形+首尾切面三角形

            // 两端切面, 放在前面，方便后续更新
            FillSectionTriangles(0, SectionFrontFace.kClockwise, triangles_, 0);
            FillSectionTriangles(pointCount - 1, SectionFrontFace.kCounterClockwise, triangles_, sectionTriangles * 3);

            int triangleIndex = sectionTriangles * 3 * 2;
            for (int i = 0; i < points_.Count - 1; ++i)
            {
                for (int j = 0; j < sides; ++j)
                {
                    //
                    int baseIndex = i * sides;
                    triangles_[triangleIndex] = baseIndex + j;
                    triangles_[triangleIndex + 1] = baseIndex + sides + (j + 1) % sides;
                    triangles_[triangleIndex + 2] = baseIndex + sides + j;
                    triangles_[triangleIndex + 3] = baseIndex + (j + 1) % sides;
                    triangles_[triangleIndex + 4] = baseIndex + sides + (j + 1) % sides;
                    triangles_[triangleIndex + 5] = baseIndex + j;

                    triangleIndex += 6;
                }
            }
        }

        /// <summary>
        /// 绳子分为一段一段，每段为由两个圆组成。AddSegment添加一个圆，从第二圆开始生成一段一段的圆柱模拟绳子
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(Vector3 point)
        {
            points_.Add(point);

            if (points_.Count < 2)
            {
                return;
            }
            
            if(points_.Count == 2)
            {
                SetPoints(points_);
                return;
            }

            // 拷贝原有的数据
            var oldVertices = vertices_;
            vertices_ = new Vector3[points_.Count * sides];
            Array.Copy(oldVertices, vertices_, oldVertices.Length);

            var oldUvs = uvs_;
            uvs_ = new Vector2[vertices_.Length];
            Array.Copy(oldUvs, uvs_, oldUvs.Length);

            // 最后一个点
            FillVertices(points_[points_.Count - 1], Quaternion.FromToRotation(Vector3.forward, points_[points_.Count - 1] - points_[points_.Count - 2]), vertices_, (points_.Count - 1) * sides);
            FillVertexUV(uvs_, 0, (points_.Count - 1) * sides);

            // 三角形索引
            var oldTriangles = triangles_;
            int sideTriangles = (points_.Count - 1) * sides * 2 * 3; // 分段（圆柱）的三角形
            triangles_ = new int[sideTriangles + sectionTriangles * 3 * 2];    // 分段（圆柱）的三角形+首尾切面三角形
            Array.Copy(oldTriangles, triangles_, oldTriangles.Length);

            // 只更新最后的切面
            FillSectionTriangles(pointCount - 1, SectionFrontFace.kCounterClockwise, triangles_, sectionTriangles * 3);

            // 更新最后一段
            int triangleIndex = oldTriangles.Length;
            for (int j = 0; j < sides; ++j)
            {
                //
                int baseIndex = (points_.Count - 2) * sides;
                triangles_[triangleIndex] = baseIndex + j;
                triangles_[triangleIndex + 1] = baseIndex + sides + (j + 1) % sides;
                triangles_[triangleIndex + 2] = baseIndex + sides + j;
                triangles_[triangleIndex + 3] = baseIndex + (j + 1) % sides;
                triangles_[triangleIndex + 4] = baseIndex + sides + (j + 1) % sides;
                triangles_[triangleIndex + 5] = baseIndex + j;

                triangleIndex += 6;
            }
        }

        /// <summary>
        /// 删除一个点
        /// </summary>
        /// <param name="index"></param>
        public void RemovePoint(int index)
        {
            if (index > -1 && index < points_.Count)
            {
                points_.RemoveAt(index);

                // 不构成段
                if (points_.Count < 2)
                {
                    triangles_ = new int[3];
                    return;
                }

                // 直接移动数据
                for (int i=(index + 1)* sides; i < vertices_.Length; ++i)
                {
                    vertices_[i - sides] = vertices_[i];
                }
                for (int i = (index + 1) * sides; i < uvs_.Length; ++i)
                {
                    uvs_[i - sides] = uvs_[i];
                }

                // 删除第一个点
                if(index == 0)
                {
                    // 更新切面
                    FillSectionTriangles(0, SectionFrontFace.kCounterClockwise, triangles_, sectionTriangles * 3);

                    var indicesCountOfSegment = sides * 2 * 3;
                    int baseIndex = sectionTriangles * 3 * 2;
                    for (int i = (index + 1) * indicesCountOfSegment; i < triangles_.Length; ++i)
                    {
                        triangles_[baseIndex + i - indicesCountOfSegment] = triangles_[baseIndex + i];
                    }
                }
                // 最后一个点
                else if(index == points_.Count)
                {
                    // 三角形索引
                    var oldTriangles = triangles_;
                    int sideTriangles = (points_.Count - 1) * sides * 2 * 3; // 分段（圆柱）的三角形
                    triangles_ = new int[sideTriangles + sectionTriangles * 3 * 2];    // 分段（圆柱）的三角形+首尾切面三角形
                    Array.Copy(oldTriangles, triangles_, oldTriangles.Length - sides * 3 * 2);  // 减少一段

                    // 更新切面
                    FillSectionTriangles(points_.Count - 1, SectionFrontFace.kCounterClockwise, triangles_, sectionTriangles * 3);
                }
                // 中间一段，顶点和UV已经更新，索引不需要更新，但最后一段变成了多出来的，去掉最后一段就可以了
                else
                {
                    // 三角形索引
                    var oldTriangles = triangles_;
                    int sideTriangles = (points_.Count - 1) * sides * 2 * 3; // 分段（圆柱）的三角形
                    triangles_ = new int[sideTriangles + sectionTriangles * 3 * 2];    // 分段（圆柱）的三角形+首尾切面三角形
                    Array.Copy(oldTriangles, triangles_, oldTriangles.Length - sides * 3 * 2);  // 减少一段
                }


            }
        }

        /// <summary>
        /// 清除所有的点
        /// </summary>
        public void RemoveAllPoints()
        {
            points_.Clear();
            vertices_ = new Vector3[0];
            uvs_ = new Vector2[0];
            triangles_ = new int[0];
        }

        /// <summary>
        /// 更新顶点点数据
        /// </summary>
        /// <param name="point"></param>
        public void SetPoint(int index, Vector3 point)
        {
            if (index > -1 && index < points_.Count)
            {
                points_[index] = point;

                if (index == 0 && index + 1 < points_.Count)
                {
                    FillVertices(points_[0], Quaternion.FromToRotation(Vector3.forward, points_[1] - points_[0]), vertices_, index * sides);
                    FillVertexUV(uvs_, index, index * sides);
                }
                else if (index == pointCount - 1 && pointCount - 2 > -1)
                {
                    // 第一个点的圆周顶点需要根据第二点更新角度
                    if(index == 1)
                    {
                        FillVertices(points_[0], Quaternion.FromToRotation(Vector3.forward, points_[1] - points_[0]), vertices_, 0);
                    }
                    FillVertices(points_[points_.Count - 1], Quaternion.FromToRotation(Vector3.forward, points_[points_.Count - 1] - points_[points_.Count - 2]), vertices_, (points_.Count - 1) * sides);
                    FillVertexUV(uvs_, 0, (points_.Count - 1) * sides);
                }
                else if (index - 1 > -1 && index + 1 < points_.Count)
                {
                    Quaternion quat = Quaternion.Slerp(Quaternion.FromToRotation(Vector3.forward, points_[index] - points_[index - 1]), Quaternion.FromToRotation(Vector3.forward, points_[index + 1] - points_[index]), 0.5f);

                    FillVertices(points_[index], quat, vertices_, index * sides);
                    FillVertexUV(uvs_, index, index * sides);
                }
            }
        }

        /// <summary>
        /// 获取点数据
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector3 GetPoint(int index)
        {
            return points_[index];
        }

        /// <summary>
        /// 添加碰撞
        /// </summary>
        /// <param name="isTrigger"></param>
        public void AddCollider(bool isTrigger=false)
        {
            if(pointCount < 2)
            {
                return;
            }

            var collider = gameObject.GetComponent<Collider>();
            if (collider)
            {
                GameObject.Destroy(collider);
            }

            // TODO: 考虑共线的点合并，可以少一些collider
            for(int i=1; i<points_.Count; i++)
            {
                var go = CreateBoxCollider(points_[i], points_[i-1], isTrigger);
                go.transform.SetParent(gameObject.transform);
            }
        }

        private GameObject CreateBoxCollider(Vector3 left,  Vector3 right, bool isTrigger)
        {
            GameObject go = new GameObject("Collider");
            go.transform.position = (left + right) * 0.5f;
            go.transform.right = (right - left).normalized;
            go.transform.localScale = new Vector3(Vector3.Distance(left, right), radius * 2, radius * 2);

            go.AddComponent<BoxCollider>().isTrigger = isTrigger;

            return go;
        }


    }
}
