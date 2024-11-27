using UnityEngine;
using System.Collections.Generic;
using System;

namespace Wugou
{
    /// <summary>
    /// 基于贝塞尔曲线生成管道模型，即通过点生成圆，然后生成圆柱，最后圆柱连接为管道
    /// 1.因为是实时生成的mesh，每一帧都更新顶点信息，数量多了之后，性能可能是个问题;
    /// 2.没有模拟重力；
    /// </summary>
    public class BezierPipe : MonoBehaviour
    {
        [Tooltip("管道弯曲程度")]
        public float cornerScale = 1f;

        [Tooltip("管道横向细分（圆环数量），越大越平滑")]
        [Range(1, 100)]
        public int cornerStep = 10;

        [Tooltip("管道切面细分点数（组成圆环的点），越大越平滑")]
        [Range(2, 100)]
        public int circleStep = 10;

        [Tooltip("管道半径")]
        public float radius = 0.1f;

        public Mesh mesh { get; private set; }

        private Material _material;
        public Material material
        {
            get
            {
                return _material;
            }
            set
            {
                _material = value;
                GetComponent<MeshRenderer>().material = _material;
            }
        }

        public Vector3 startPoint {  get; private set; }
        public Vector3 endPoint { get; private set; }
        public Vector3 startTangent { get; private set; }
        public Vector3 endTangent { get; private set; }


        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        //void Update()
        //{
        //}

        /// <summary>
        /// https://www.cnblogs.com/guangzhiruijie/p/16692759.html
        /// 1. 定义两个点和其切线:s点(pS,tS),e点(pE,tE)；
        /// 2. 控制点的计算方式：c.pos = s.pos + s.tan * len;
        /// 3. 利用2的方式计算出起始两个点的控制点，sc和ec；
        /// 4. 计算sc和ec的中点mc；
        /// 5. 继续利用2的方式计算sc和ec的控制点；
        /// 6. 总共的7个点：s,sc,scc,mc,ecc,ec,e;
        /// 7. 分为3条线段：(s,sc,scc),(scc,mc,ecc),(ecc,ec,e)
        /// 8. 利用贝塞尔曲线公式或其它的曲线公式获取插值点；
        /// 8. 每个点扩展为一个圆环，再把圆环连接起来变为管道
        /// </summary>
        /// <param name="start"></param>
        /// <param name="startTangent">切线方向</param>
        /// <param name="end"></param>
        /// <param name="endTangent">切线方向</param>
        /// <returns></returns>
        public void Build(Vector3 start, Vector3 startTangent, Vector3 end, Vector3 endTangent)
        {
            if(mesh == null)
            {
                // 生成mesh
                mesh = new Mesh();
                mesh.name = $"{name} Pipe";

                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                gameObject.AddComponent<MeshRenderer>();

                var collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.enabled = false;
            }

            startPoint = start;
            endPoint = end;
            this.startTangent = startTangent;
            this.endTangent = endTangent;

            // 计算管道弯曲部分的小长短
            float scale = cornerScale;
            float length = (start - end).magnitude / 4;
            if (scale > length)
            {
                scale = length;
            }

            // 计算控制点
            var s = start;
            var sc = start + startTangent * scale;

            var e = end;
            var ec = end + endTangent * scale;

            // 中间片段的控制点是前后控制点的中值
            var mc = (sc + ec) / 2;
            var scc = sc + (mc - sc).normalized * scale;
            var ecc = ec + (mc - ec).normalized * scale;

            // 生成3段管道
            float pipeLen = 0;
            List<Vertex> vertices = new List<Vertex>();
            vertices.AddRange(MakePipe(s, sc, scc, ref pipeLen));
            vertices.AddRange(MakePipe(scc, mc, ecc, ref pipeLen));
            vertices.AddRange(MakePipe(ecc, ec, e, ref pipeLen, true));

            mesh.Clear();

            Vector3[] vertArr = new Vector3[vertices.Count];
            Vector2[] uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                var vt = vertices[i];
                vertArr[i] = new Vector3(vt.x, vt.y, vt.z);
                uvs[i] = new Vector2(vt.u, vt.v); 
            }

            mesh.vertices = vertArr;
            int[] triangles = new int[(vertices.Count - circleStep - 2) * 6];
            for (int i = 0; i < vertices.Count - circleStep - 2; i++)
            {
                triangles[i * 6 + 0] = (i);
                triangles[i * 6 + 1] = (i + 1);
                triangles[i * 6 + 2] = (i + circleStep + 1);
                triangles[i * 6 + 3] = (i + circleStep + 1);
                triangles[i * 6 + 4] = (i + 1);
                triangles[i * 6 + 5] = (i + circleStep + 2);
            }
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // 计算整体管道的坐标轴位置和角度
            //transform.position = midSeg.controlPoint;
            //transform.rotation = midSeg.fromDir;

            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// 重新构建管道
        /// </summary>
        public void Rebuild()
        {
            Build(startPoint, startTangent, endPoint, endTangent);
        }

        /// <summary>
        /// 顶点数据
        /// </summary>
        private class Vertex
        {
            public float x, y, z;
            public float u, v;
        }

        /// <summary>
        /// 计算管道的顶点
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="dir"></param>
        /// <param name="u">uv的横向坐标</param>
        /// <returns></returns>
        List<Vertex> CalcCircleVectices(Vector3 pos, Quaternion dir, float u)
        {
            List<Vertex> points = new List<Vertex>();
            for (int i = 0; i <= circleStep; i++)
            {
                float p = 2 * Mathf.PI * i / circleStep;
                Vector3 cp = new Vector3(radius * Mathf.Cos(p), radius * Mathf.Sin(p), 0);
                cp = pos + dir * cp;
                cp = transform.worldToLocalMatrix.MultiplyPoint(cp);
                points.Add(new Vertex() { x = cp.x, y = cp.y, z = cp.z, u = u / 1, v = i * 1.0f / circleStep }); // 横向坐标根据长度计算
            }

            return points;
        }

        /// <summary>
        /// 制作管道
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <param name="e"></param>
        /// <param name="len">记录长度，用于后续计算uv</param>
        /// <param name="generateEnd"></param>
        private List<Vertex> MakePipe(Vector3 s, Vector3 c, Vector3 e, ref float len, bool generateEnd = false)
        {
            var v1 = c - s;
            var v2 = e - c;
            bool isStrait = Vector3.Angle(v1, v2) < 5;

            var dir1 = Quaternion.FromToRotation(Vector3.forward, v1);
            var dir2 = Quaternion.FromToRotation(Vector3.forward, v2);

            List<Vertex> vertices = new List<Vertex>();
            vertices.AddRange(CalcCircleVectices(s, dir1, len));     // 第一个圆环

            var lastPoint = s;
            // 生成中间圆环
            if (isStrait)
            {
                len += Vector3.Distance(lastPoint, c);
                vertices.AddRange(CalcCircleVectices(c, dir1, len));
                lastPoint = c;
            }
            else
            {
                for (int i = 1; i < cornerStep; i++)
                {
                    float t = (float)i / cornerStep;
                    Vector3 p2 = BezierUtils.CalculateCubicBezierPoint(t, s, c, e);
                    Quaternion dir = Quaternion.Lerp(dir1, dir2, t);
                    len += Vector3.Distance(lastPoint, p2);
                    vertices.AddRange(CalcCircleVectices(p2, dir, len));
                    lastPoint = p2;
                }
            }

            len += Vector3.Distance(lastPoint, e);
            if (generateEnd)
            {
                vertices.AddRange(CalcCircleVectices(e, dir2, len)); // 最后一个圆环
            }

            return vertices;
        }

        public void SetCollidable(bool isCollidable)
        {
            GetComponent<MeshCollider>().enabled = isCollidable;
        }

        /// <summary>
        /// 创建一个新的贝塞尔曲线管道
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static BezierPipe Create(string name)
        {
            GameObject pipeObj = new GameObject(name);

            return pipeObj.AddComponent<BezierPipe>();
        }


    }
}
