using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// °ËÃæÌå£¬²Î¿¼ThreeJS
    /// </summary>
    public class OctahedronMesh
    {
        public static Mesh Create(float radius = 1, int detail = 0)
        {
            float[] vertices = new float[] {
                1, 0, 0, -1, 0, 0, 0, 1, 0,
                0, -1, 0, 0, 0, 1, 0, 0, -1
            };

            int[] indices = new int[] {
                0, 2, 4, 0, 4, 3, 0, 3, 5,
                0, 5, 2, 1, 2, 5, 1, 5, 3,
                1, 3, 4, 1, 4, 2
            };


            var octahedron = new OctahedronMesh(vertices, indices, radius, detail);
            return octahedron.Generate();
        }

        int[] indices_;
        float[] vertices_;

        List<Vector3> vertexBuffer_ = new List<Vector3>();
        List<Vector2> uvs_ = new List<Vector2>();

        private OctahedronMesh(float[] vertices, int[] indices, float radius, int detail)
        {
            vertices_ = vertices;
            indices_ = indices;

            // the subdivision creates the vertex buffer data

            Subdivide(detail);

            // all vertices should lie on a conceptual sphere with a given radius

            ApplyRadius(radius);

            // finally, create the uv data

            GenerateUVs();
        }

        void Subdivide(int detail)
        {
            // iterate over all faces and apply a subdivision with the given detail value

            for (int i = 0; i < indices_.Length; i += 3)
            {

                // get the vertices of the face
                // perform subdivision

                SubdivideFace(GetVertexByIndex(indices_[i + 0]), GetVertexByIndex(indices_[i + 1]), GetVertexByIndex(indices_[i + 2]), detail);

            }

        }

        void SubdivideFace(Vector3 a, Vector3 b, Vector3 c, int detail)
        {

            int cols = detail + 1;

            // we use this multidimensional array as a data structure for creating the subdivision

            Vector3[,] v = new Vector3[cols + 1, cols + 1];

            // construct all of the vertices for this subdivision

            for (int i = 0; i <= cols; i++)
            {
                var aj = Vector3.Lerp(a, c, i * 1.0f / cols);
                var bj = Vector3.Lerp(b, c, i * 1.0f / cols);

                int rows = cols - i;

                for (int j = 0; j <= rows; j++)
                {

                    if (j == 0 && i == cols)
                    {

                        v[i, j] = aj;

                    }
                    else
                    {

                        v[i, j] = Vector3.Lerp(aj, bj, j / rows);

                    }

                }

            }

            // construct all of the faces

            for (int i = 0; i < cols; i++)
            {

                for (int j = 0; j < 2 * (cols - i) - 1; j++)
                {

                    int k = (int)Mathf.Floor(j / 2.0f);

                    if (j % 2 == 0)
                    {

                        PushVertex(v[i, k + 1]);
                        PushVertex(v[i + 1, k]);
                        PushVertex(v[i, k]);

                    }
                    else
                    {

                        PushVertex(v[i, k + 1]);
                        PushVertex(v[i + 1, k + 1]);
                        PushVertex(v[i + 1, k]);

                    }

                }

            }

        }

        Vector3 GetVertexByIndex(int index)
        {
            int stride = index * 3;
            return new Vector3(vertices_[stride + 0], vertices_[stride + 1], vertices_[stride + 2]);

        }

        void PushVertex(Vector3 vertex)
        {
            vertexBuffer_.Add(vertex);
        }

        void ApplyRadius(float radius)
        {
            // iterate over the entire buffer and apply the radius to each vertex

            for (int i = 0; i < vertexBuffer_.Count; i++)
            {
                vertexBuffer_[i] = vertexBuffer_[i].normalized * radius;
            }

        }

        void GenerateUVs()
        {
            for (int i = 0; i < vertexBuffer_.Count; i++)
            {
                var vertex = vertexBuffer_[i];

                float u = Azimuth(vertex) / 2 / Mathf.PI + 0.5f;
                float v = Inclination(vertex) / Mathf.PI + 0.5f;
                uvs_.Add(new Vector2(u, 1 - v));

            }

            CorrectUVs();

            CorrectSeam();

        }

        void CorrectSeam()
        {

            // handle case when face straddles the seam, see #3269

            for (int i = 0; i < uvs_.Count; i += 3)
            {

                // uv data of a single face

                float x0 = uvs_[i + 0].x;
                float x1 = uvs_[i + 1].x;
                float x2 = uvs_[i + 2].x;

                float max = Mathf.Max(x0, x1, x2);
                float min = Mathf.Min(x0, x1, x2);

                // 0.9 is somewhat arbitrary

                if (max > 0.9 && min < 0.1)
                {

                    if (x0 < 0.2) uvs_[i + 0].Set(uvs_[i + 0].x + 1, uvs_[i + 0].y);
                    if (x1 < 0.2) uvs_[i + 1].Set(uvs_[i + 1].x + 1, uvs_[i + 1].y);
                    if (x2 < 0.2) uvs_[i + 2].Set(uvs_[i + 2].x + 1, uvs_[i + 2].y);

                }

            }

        }

        void CorrectUVs()
        {
            for (int i = 0, j = 0; i < vertexBuffer_.Count; i += 3, j += 3)
            {
                var centroid = (vertexBuffer_[i] + vertexBuffer_[i + 1] + vertexBuffer_[i + 2]) / 3.0f;
                float azi = Azimuth(centroid);

                correctUV(uvs_[i], j + 0, vertexBuffer_[i], azi);
                correctUV(uvs_[i + 1], j + 2, vertexBuffer_[i + 1], azi);
                correctUV(uvs_[i + 2], j + 4, vertexBuffer_[i + 2], azi);
            }

        }

        void correctUV(Vector2 uv, int stride, Vector3 vector, float azimuth)
        {

            if ((azimuth < 0) && (uv.x == 1))
            {
                var tmp = uvs_[stride];
                tmp.x = uv.x - 1;
                uvs_[stride] = tmp;

            }

            if ((vector.x == 0) && (vector.z == 0))
            {
                var tmp = uvs_[stride];
                tmp.x = azimuth / 2 / Mathf.PI + 0.5f;
                uvs_[stride] = tmp;
            }

        }

        // Angle around the Y axis, counter-clockwise when looking from above.

        float Azimuth(Vector3 vector)
        {
            return Mathf.Atan2(vector.z, -vector.x);
        }


        // Angle above the XZ plane.

        float Inclination(Vector3 vector)
        {

            return Mathf.Atan2(-vector.y, Mathf.Sqrt((vector.x * vector.x) + (vector.z * vector.z)));

        }

        public Mesh Generate()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertexBuffer_.ToArray();
            mesh.uv = uvs_.ToArray();
            //mesh.normals = normals.ToArray();
            int[] triangles = new int[vertexBuffer_.Count];
            for(int i = 0; i < triangles.Length; ++i)
            {
                triangles[i] = i;
            }
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }
    }

}

