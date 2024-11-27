using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 圆环网格
    /// </summary>
    public class TorusMesh
    {
        /// <summary>
        /// 创建一个圆环
        /// </summary>
        /// <param name="radius">半径</param>
        /// <param name="tube">管半径</param>
        /// <param name="radialSegments">管分割数</param>
        /// <param name="tubularSegments">圆分割数</param>
        /// <param name="arc">角度</param>
        public static Mesh Create(float radius = 1, float tube = 0.4f, int radialSegments = 12, int tubularSegments = 48, float arc = Mathf.PI * 2, Material mat = null)
        {
            Vector3[] vertices = new Vector3[(radialSegments + 1) * (tubularSegments + 1)];
            Vector3[] normals = new Vector3[(radialSegments + 1) * (tubularSegments + 1)];
            Vector2[] uvs = new Vector2[(radialSegments + 1) * (tubularSegments + 1)];

            // generate vertices, normals and uvs
            for (int j = 0; j <= radialSegments; j++)
            {
                for (int i = 0; i <= tubularSegments; i++)
                {
                    int index = j * (tubularSegments + 1) + i;

                    float u = (float)i / tubularSegments * arc;
                    float v = (float)j / radialSegments * Mathf.PI * 2;

                    // vertex
                    Vector3 vertex = new Vector3();
                    vertex.x = (radius + tube * Mathf.Cos(v)) * Mathf.Cos(u);
                    vertex.y = (radius + tube * Mathf.Cos(v)) * Mathf.Sin(u);
                    vertex.z = tube * Mathf.Sin(v);

                    vertices[index] = vertex;

                    // normal
                    Vector3 center = new Vector3();
                    center.x = radius * Mathf.Cos(u);
                    center.y = radius * Mathf.Sin(u);
                    var normal = (vertex - center).normalized;

                    normals[index] = (normal);

                    // uv
                    uvs[index] = new Vector2((float)i / tubularSegments, (float)j / radialSegments);
                }

            }

            // generate indices
            int[] triangles = new int[radialSegments * tubularSegments * 6];
            for (int j = 1; j <= radialSegments; j++)
            {
                for (int i = 1; i <= tubularSegments; i++)
                {
                    // triangles
                    int a = (tubularSegments + 1) * j + i - 1;
                    int b = (tubularSegments + 1) * (j - 1) + i - 1;
                    int c = (tubularSegments + 1) * (j - 1) + i;
                    int d = (tubularSegments + 1) * j + i;

                    // faces
                    int index = ((j - 1) * (tubularSegments) + i - 1) * 6;
                    triangles[index + 0] = a;
                    triangles[index + 1] = b;
                    triangles[index + 2] = d;
                    triangles[index + 3] = b;
                    triangles[index + 4] = c;
                    triangles[index + 5] = d;
                }

            }

            //负载属性与mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            //mesh.normals = normals;
            mesh.uv = uvs;

            return mesh;
        }
    }
}
