using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Wugou
{
    /// <summary>
    /// ÇòÐÎÍø¸ñ
    /// </summary>
    public class SphereMesh
    {
        public static Mesh Create(float radius = 1, int widthSegments = 32, int heightSegments = 16, int phiStart = 0, float phiLength = Mathf.PI * 2, float thetaStart = 0, float thetaLength = Mathf.PI)
        {
			widthSegments = Math.Max(3, widthSegments);
			heightSegments = Math.Max(2, heightSegments);

			float thetaEnd = Mathf.Min(thetaStart + thetaLength, Mathf.PI);

			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<Vector2> uvs = new List<Vector2>();
			List<int> indices = new List<int>();

			List<List<int>> grid = new List<List<int>>();
			int index = 0;

			// generate vertices, normals and uvs

			for (int iy = 0; iy <= heightSegments; iy++)
			{

				List<int> verticesRow = new List<int>();

				float v = iy * 1.0f / heightSegments;

				// special case for the poles

				float uOffset = 0;

				if (iy == 0 && thetaStart == 0)
				{

					uOffset = 0.5f / widthSegments;

				}
				else if (iy == heightSegments && thetaEnd == Math.PI)
				{

					uOffset = -0.5f / widthSegments;

				}

				for (int ix = 0; ix <= widthSegments; ix++)
				{

					float u = ix * 1.0f / widthSegments;

					// vertex
					var vertex = new Vector3(
						-radius * Mathf.Cos(phiStart + u * phiLength) * Mathf.Sin(thetaStart + v * thetaLength),
						radius * Mathf.Cos(thetaStart + v * thetaLength),
						radius * Mathf.Sin(phiStart + u * phiLength) * Mathf.Sin(thetaStart + v * thetaLength)
					);

					vertices.Add(vertex);

					// normal

					normals.Add(vertex.normalized);

					// uv

					uvs.Add(new Vector2(u + uOffset, 1 - v));

					verticesRow.Add(index++);

				}

				grid.Add(verticesRow);

			}

			// indices

			for (int iy = 0; iy < heightSegments; iy++)
			{

				for (int ix = 0; ix < widthSegments; ix++)
				{

					var a = grid[iy][ix + 1];
					var b = grid[iy][ix];
					var c = grid[iy + 1][ix];
					var d = grid[iy + 1][ix + 1];

					if (iy != 0 || thetaStart > 0)
					{
						indices.Add(a);
						indices.Add(b);
						indices.Add(d);
					}

					if (iy != heightSegments - 1 || thetaEnd < Math.PI)
					{
						indices.Add(b);
						indices.Add(c);
						indices.Add(d);
					}

				}

			}

			Mesh mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.normals = normals.ToArray();
			mesh.triangles = indices.ToArray();
			mesh.uv = uvs.ToArray();

			return mesh;
		}
    }
}
