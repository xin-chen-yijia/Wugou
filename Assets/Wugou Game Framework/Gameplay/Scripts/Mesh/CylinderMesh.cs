using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 圆柱，也可用用于生成圆锥
    /// </summary>
    public class CylinderMesh
    {
        float radiusTop_;
        float radiusBottom_;
        float height_;
        int radialSegments_;
        int heightSegments_;
        bool openEnded_;
        float thetaStart_;
        float thetaLength_;

        float halfHeight;
        float slope;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> indices = new List<int>();
        int[,] indexArray;
        int index_ = 0;

        private CylinderMesh(float radiusTop = 1, float radiusBottom = 1, float height = 1, int radialSegments = 32, int heightSegments = 1, bool openEnded = false, float thetaStart = 0, float thetaLength = Mathf.PI * 2)
        {
            radiusTop_ = radiusTop;
            radiusBottom_ = radiusBottom;
            height_ = height;
            radialSegments_ = radialSegments;
            heightSegments_ = heightSegments;
            openEnded_ = openEnded;
            thetaStart_ = thetaStart;
            thetaLength_ = thetaLength;

            halfHeight = height / 2;
            slope = (radiusBottom - radiusTop) / height;
            indexArray = new int[heightSegments + 1, radialSegments + 1];
        }

        private Mesh Generate()
        {
            // generateTorso
            generateTorso();

            // generateCap
            if (openEnded_ == false)
            {
                if (radiusTop_ > 0) generateCap(true);
                if (radiusBottom_ > 0) generateCap(false);

            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = indices.ToArray();
            //mesh.RecalculateNormals();

            return mesh;
        }

        void generateTorso()
        {

            for (int y = 0; y <= heightSegments_; y++)
            {
                List<int> indexRow = new List<int>();

                float v = y * 1.0f / heightSegments_;

                // calculate the radius of the current row
                float radius = v * (radiusBottom_ - radiusTop_) + radiusTop_;

                for (int x = 0; x <= radialSegments_; x++)
                {

                    float u = (float)x / radialSegments_;

                    float theta = u * thetaLength_ + thetaStart_;

                    float sinTheta = Mathf.Sin(theta);
                    float cosTheta = Mathf.Cos(theta);

                    // vertex
                    vertices.Add(new Vector3(radius * sinTheta, -v * height_ + halfHeight, radius * cosTheta));

                    // normal
                    normals.Add(new Vector3(sinTheta, slope, cosTheta).normalized);

                    // uv
                    uvs.Add(new Vector2(u, 1 - v));

                    // save index of vertex in respective row
                    // now save vertices of the row in our index array

                    indexArray[y, x] = index_++;
                }
            }

            // generate indices
            for (int x = 0; x < radialSegments_; x++)
            {
                for (int y = 0; y < heightSegments_; y++)
                {

                    // we use the index array to access the correct indices

                    int a = indexArray[y, x];
                    var b = indexArray[y + 1, x];
                    var c = indexArray[y + 1, x + 1];
                    var d = indexArray[y, x + 1];

                    // faces

                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(d);
                    indices.Add(b);
                    indices.Add(c);
                    indices.Add(d);
                }

            }
        }

        void generateCap(bool top)
        {
            float radius = (top) ? radiusTop_ : radiusBottom_;
            float sign = (top) ? 1 : -1;

            int centerIndexStart = index_;

            // first we generate the center vertex data of the cap.
            // because the geometry needs one set of uvs per face,
            // we must generate a center vertex per face/segment

            for (int x = 1; x <= radialSegments_; x++)
            {

                // vertex

                vertices.Add(new Vector3(0, halfHeight * sign, 0));

                // normal

                normals.Add(new Vector3(0, sign, 0));

                // uv

                uvs.Add(new Vector2(0.5f, 0.5f));

                // increase index

                index_++;

            }

            // save the index of the last center vertex
            int centerIndexEnd = index_;

            // now we generate the surrounding vertices, normals and uvs

            for (int x = 0; x <= radialSegments_; x++)
            {

                float u = (float)x / radialSegments_;
                float theta = u * thetaLength_ + thetaStart_;

                float cosTheta = Mathf.Cos(theta);
                float sinTheta = Mathf.Sin(theta);

                // vertex
                vertices.Add(new Vector3(radius * sinTheta, halfHeight * sign, radius * cosTheta));

                // normal

                normals.Add(new Vector3(0, sign, 0));

                // uv

                uvs.Add(new Vector2((cosTheta * 0.5f) + 0.5f, (sinTheta * 0.5f * sign) + 0.5f));

                // increase index

                index_++;

            }

            // generate indices

            for (int x = 0; x < radialSegments_; x++)
            {

                int c = centerIndexStart + x;
                int i = centerIndexEnd + x;

                if (top)
                {
                    // face top
                    indices.Add(i);
                    indices.Add(i + 1);
                    indices.Add(c);

                }
                else
                {

                    // face bottom
                    indices.Add(i + 1);
                    indices.Add(i);
                    indices.Add(c);

                }
            }
        }

        /// <summary>
        /// 创建一个圆柱
        /// </summary>
        /// <param name="radiusTop"></param>
        /// <param name="radiusBottom"></param>
        /// <param name="height"></param>
        /// <param name="radialSegments"></param>
        /// <param name="heightSegments"></param>
        /// <param name="openEnded"></param>
        /// <param name="thetaStart"></param>
        /// <param name="thetaLength"></param>
        /// <returns></returns>
        public static Mesh Create(float radiusTop = 1, float radiusBottom = 1, float height = 1, int radialSegments = 32, int heightSegments = 1, bool openEnded = false, float thetaStart = 0, float thetaLength = Mathf.PI * 2)
        {
            var t = new CylinderMesh(radiusTop, radiusBottom, height, radialSegments, heightSegments, openEnded, thetaStart, thetaLength);
            return t.Generate();
        }
    }

}