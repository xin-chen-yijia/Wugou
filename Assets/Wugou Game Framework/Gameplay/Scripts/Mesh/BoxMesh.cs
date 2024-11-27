using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// Box Mesh
    /// </summary>
    public class BoxMesh
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        int numberOfVertices = 0;

        private BoxMesh(float width = 1, float height = 1, float depth = 1, int widthSegments = 1, int heightSegments = 1, int depthSegments = 1)
        {
            buildPlane(2, 1, 0, -1, -1, depth, height, width, depthSegments, heightSegments, 0); // px
            buildPlane(2, 1, 0, 1, -1, depth, height, -width, depthSegments, heightSegments, 1); // nx
            buildPlane(0, 2, 1, 1, 1, width, depth, height, widthSegments, depthSegments, 2); // py
            buildPlane(0, 2, 1, 1, -1, width, depth, -height, widthSegments, depthSegments, 3); // ny
            buildPlane(0, 1, 2, 1, -1, width, height, depth, widthSegments, heightSegments, 4); // pz
            buildPlane(0, 1, 2, -1, -1, width, height, -depth, widthSegments, heightSegments, 5); // nz
        }

        void buildPlane(int u, int v, int w, int udir, int vdir, float width, float height, float depth, int gridX, int gridY, int materialIndex)
        {

            float segmentWidth = width / gridX;
            float segmentHeight = height / gridY;

            float widthHalf = width / 2;
            float heightHalf = height / 2;
            float depthHalf = depth / 2;

            var gridX1 = gridX + 1;
            var gridY1 = gridY + 1;

            int vertexCounter = 0;

            float[] tmpVec = new float[3];
            // generate vertices, normals and uvs

            for (int iy = 0; iy < gridY1; iy++)
            {

                float y = iy * segmentHeight - heightHalf;

                for (int ix = 0; ix < gridX1; ix++)
                {

                    float x = ix * segmentWidth - widthHalf;

                    // set values to correct vector component
                    tmpVec[u] = x * udir;
                    tmpVec[v] = y * vdir;
                    tmpVec[w] = depthHalf;

                    // now apply vector to vertex buffer
                    vertices.Add(new Vector3(tmpVec[0], tmpVec[1], tmpVec[2]));

                    // set values to correct vector component
                    tmpVec[u] = 0;
                    tmpVec[v] = 0;
                    tmpVec[w] = depth > 0 ? 1 : -1;

                    // now apply vector to normal buffer

                    normals.Add(new Vector3(tmpVec[0], tmpVec[1], tmpVec[2]));

                    // uvs

                    uvs.Add(new Vector2((float)ix / gridX, 1 - ((float)iy / gridY)));

                    // counters

                    vertexCounter += 1;

                }

            }

            // indices

            // 1. you need three indices to draw a single face
            // 2. a single segment consists of two faces
            // 3. so we need to generate six (2*3) indices per segment

            for (int iy = 0; iy < gridY; iy++)
            {

                for (int ix = 0; ix < gridX; ix++)
                {

                    int a = numberOfVertices + ix + gridX1 * iy;
                    int b = numberOfVertices + ix + gridX1 * (iy + 1);
                    int c = numberOfVertices + (ix + 1) + gridX1 * (iy + 1);
                    int d = numberOfVertices + (ix + 1) + gridX1 * iy;

                    // faces

                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(d);
                    indices.Add(b);
                    indices.Add(c);
                    indices.Add(d);
                }
            }

            // update total number of vertices
            numberOfVertices += vertexCounter;
        }

        private Mesh Generate()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.uv = uvs.ToArray();

            return mesh;
        }

        public static Mesh Create(float width = 1, float height = 1, float depth = 1, int widthSegments = 1, int heightSegments = 1, int depthSegments = 1)
        {
            var box = new BoxMesh(width, height, depth, widthSegments, heightSegments, depthSegments);
            return box.Generate();
        }
    }
}
