using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 一个简单的绳子实现，参考UltimateRope
    /// 
    /// 原理：主要基于ConfigurableJoint和SkinnedMeshRenderer，也就是ConfigurableJoint其实是SkinnedMeshRenderer的骨骼
    /// </summary>
    public class SkinnnedJointRope
    {
        public GameObject gameObject { get; private set; }

        public SkinnedMeshRenderer skinnedMeshRenderer => gameObject.GetComponent<SkinnedMeshRenderer>();

        public GameObject startPoint { get; set; }
        public GameObject endPoint { get; set; }

        /// <summary>
        /// 绳子片段长度
        /// </summary>
        public float segmentLength { get; set; }

        /// <summary>
        /// 圆的边数
        /// </summary>
        public int sides { get; set; } = 12;

        /// <summary>
        /// 绳子半径
        /// </summary>
        public float radius { get; set; }

        private bool hasNodes_ = false;

        /// <summary>
        /// 切面的三角形数量
        /// </summary>
        public int sectionTriangles => (sides - 2);

        public SkinnnedJointRope(string name)
        {
            gameObject = new GameObject(name);
            var skin = gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.updateWhenOffscreen = true;
        }

        /// <summary>
        /// 创建绳子的组成节点，其节点基于ConfigurableJoint
        /// 
        /// 1. ConfigurableJoint在有旋转的时候，
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        List<Transform> CreateRopeNodes(GameObject start, GameObject end, bool useBezier = false)
        {
            // controller points
            var dir = (end.transform.position - start.transform.position).normalized;
            var controllerPoint1 = start.transform.position + start.transform.forward * 1.2f;// Vector3.Slerp(start.forward, dir, 0.5f).normalized;
            var controllerPoint2 = end.transform.position + end.transform.forward * 1.2f;// (controllerPoint1 - end.transform.position).normalized * 0.2f;

            //var g1 = new GameObject("g1");
            //g1.transform.position = controllerPoint1;
            //var g2 = new GameObject("g2");
            //g2.transform.position = controllerPoint2;


            var joints = new List<Transform>();
            ConfigurableJoint lastJoint = null;
            GameObject lastJoitConnectTo = start.gameObject;

            if (useBezier)
            {
                // 不会复杂的贝塞尔曲线长度求法，简单的分割法
                float tmpLen = 0;
                float step = 0.01f;
                float t = 0;
                Vector3 lastJointPos = start.transform.position;
                Vector3 lastPoint = start.transform.position;
                int jointSeq = 0;

                while (t < 1.0f)
                {
                    t += step;
                    var point = BezierUtils.CalculateThreePowerBezierPoint(t, start.transform.position, controllerPoint1, controllerPoint2, end.transform.position);
                    var len = Vector3.Distance(point, lastPoint);
                    //Debug.Log($"{t}  {tmpLen} + {len}    {segmentLength}");
                    if (tmpLen + len > segmentLength)
                    {
                        //int dd = 0;
                        //while (tmpLen + len - segmentLength > 0.01f && dd++ < 1000)    // 尽量逼近
                        //{
                        //    t -= step * 0.0f;
                        //    point = BezierUtils.CalculateThreePowerBezierPoint(t, start.transform.position, controllerPoint1, controllerPoint2, end.transform.position);
                        //    len = Vector3.Distance(point, lastPoint);
                        //}

                        tmpLen -= segmentLength;

                        // 添加新点
                        var jointGo = new GameObject($"Node {jointSeq++}");
                        jointGo.transform.SetParent(gameObject.transform);
                        jointGo.transform.position = lastJointPos;
                        jointGo.transform.forward = (point - lastJointPos).normalized;
                        //
                        lastJointPos = point;

                        // 创建并配置joint
                        var joint = CreateJoint(jointGo, lastJoitConnectTo, jointGo.transform.position);
                        lastJoint = joint;
                        lastJoitConnectTo = jointGo;
                        joints.Add(joint.transform);
                    }

                    tmpLen += len;
                    lastPoint = point;
                }
            }
            else
            {
                // 数量
                int count = Mathf.CeilToInt(Vector3.Distance(start.transform.position, end.transform.position) / segmentLength);
                Vector3 direction = (end.transform.position - start.transform.position).normalized;
                Vector3 tmpPoint = start.transform.position;
                for (int i = 0; i < count; ++i)
                {
                    var jointGo = new GameObject($"Node {i}");
                    jointGo.transform.SetParent(gameObject.transform);
                    jointGo.transform.position = start.transform.position + i * segmentLength * dir;
                    jointGo.transform.forward = direction;

                    // 创建并配置joint
                    var joint = CreateJoint(jointGo, lastJoitConnectTo, jointGo.transform.position);
                    lastJoint = joint;
                    lastJoitConnectTo = jointGo;

                    joints.Add(joint.transform);
                }
            }

            // end joint
            CreateJoint(lastJoint.gameObject,end.gameObject,end.transform.position);

            return joints;
        }

        ConfigurableJoint CreateJoint(GameObject goObject, GameObject goConnectedTo, Vector3 v3Pivot)
        {
            ConfigurableJoint joint = goObject.AddComponent<ConfigurableJoint>();

            SetupJoint(joint);
            joint.connectedBody = goConnectedTo.GetComponent<Rigidbody>();
            joint.anchor = goObject.transform.InverseTransformPoint(v3Pivot);

            if (!goObject.GetComponent<CapsuleCollider>())
            {
                var capsule = goObject.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0, 0, segmentLength * 0.5f);
                capsule.radius = radius;
                capsule.height = segmentLength;
                capsule.direction = 2;
                //capsule.isTrigger = true;
            }

            return joint;
        }

        void SetupJoint(ConfigurableJoint joint)
        {
            //var go = new GameObject("Node");
            //var joint = go.AddComponent<ConfigurableJoint>();
            //joint.axis = new Vector3(1, 0, 0);
            //joint.angularXMotion = ConfigurableJointMotion.Limited;
            //joint.angularYMotion = ConfigurableJointMotion.Limited;
            //joint.angularZMotion = ConfigurableJointMotion.Limited;

            //joint.xMotion = ConfigurableJointMotion.Locked;
            //joint.yMotion = ConfigurableJointMotion.Locked;
            //joint.zMotion = ConfigurableJointMotion.Locked;

            SoftJointLimit jointLimit = new SoftJointLimit();
            jointLimit.contactDistance = 0.0f;
            jointLimit.bounciness = 0.0f;

            JointDrive jointDrive = new JointDrive();
            jointDrive.positionSpring = 20; ;
            jointDrive.positionDamper = 0.0f;
            jointDrive.maximumForce = 20;

            joint.axis = Vector3.right;
            joint.secondaryAxis = Vector3.up;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = Mathf.Approximately(90, 0.0f) == false ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
            joint.angularYMotion = Mathf.Approximately(90, 0.0f) == false ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
            joint.angularZMotion = Mathf.Approximately(90, 0.0f) == false ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;

            jointLimit.limit = -90;
            joint.lowAngularXLimit = jointLimit;

            jointLimit.limit = 90;
            joint.highAngularXLimit = jointLimit;

            jointLimit.limit = 90;
            joint.angularYLimit = jointLimit;

            jointLimit.limit = 90;
            joint.angularZLimit = jointLimit;

            joint.angularXDrive = jointDrive;
            joint.angularYZDrive = jointDrive;

            var rb = joint.GetComponent<Rigidbody>();
            rb.solverIterations = 100;
            //rb.isKinematic = true;
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

        /// <summary>
        /// 计算以索引点为中心的圆周顶点数据
        /// </summary>
        /// <param name="point"></param>
        /// <param name="quaternion"></param>
        /// <param name="vertices"></param>
        /// <param name="startIndex"></param>
        /// <param name="circleSides"></param>
        void FillVertices(Vector3 point, Transform bone, Vector3[] vertices, int startIndex, int circleSides)
        {
            for (int j = 0; j < circleSides; ++j)
            {
                int vertexIndex = startIndex + j;

                Vector3 vertex = point + new Vector3(Mathf.Cos(Mathf.Deg2Rad * (j * 60)) * radius, Mathf.Sin(Mathf.Deg2Rad * (j * 60)) * radius, 0);
                vertex = bone.TransformPoint(vertex); 
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
        /// <param name="circleSides"></param>
        void FillVertexUV(Vector2[] uvs, float u, int startIndex, int circleSides)
        {
            for (int j = 0; j < circleSides; ++j)
            {
                int vertexIndex = startIndex + j;
                uvs[vertexIndex] = new Vector2(u, (float)j / circleSides);
            }
        }

        /// <summary>
        /// 填充骨骼权重
        /// </summary>
        /// <param name="weights"></param>
        /// <param name="startIndex"></param>
        /// <param name="boneIndex"></param>
        void FillBoneWeights(BoneWeight[] weights, int startIndex, int boneIndex)
        {
            for(int i=0;i<sides; ++i)
            {
                float fWeight0 = 1.0f;
                float fWeight1 = 1.0f - fWeight0;
                weights[startIndex + i] = new BoneWeight()
                {
                    boneIndex0 = boneIndex,
                    weight0 = 1.0f,
                    boneIndex1 = boneIndex,
                    weight1 = 0.0f
                };
            }

        }

        /// <summary>
        /// 构建绳子的mesh，用于SkinnedMeshRenderer，注意bones和bindPoses的设置
        /// </summary>
        /// <returns></returns>
        Mesh BuildMesh1(Transform[] bones)
        {
            var pointCount = bones.Length + 1;
            // build mesh
            Vector3[] verices = new Vector3[bones.Length * sides];
            Vector2[] uvs = new Vector2[verices.Length];
            int[] triangles = new int[(bones.Length - 1) * sides * 2 * sides + (sides - 2) * 3 * 2];
            int triangleIndex = 0;

            var boneWeights = new BoneWeight[verices.Length];
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                //
                for (int j = 0; j < sides; ++j)
                {
                    int vertexIndex = i * sides + j;

                    Vector3 vertex = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (j * 60)) * radius, Mathf.Sin(Mathf.Deg2Rad * (j * 60)) * radius, 0);
                    vertex = bones[i].TransformPoint(vertex);
                    vertex = gameObject.transform.InverseTransformPoint(vertex);

                    verices[vertexIndex] = vertex;

                    uvs[vertexIndex] = new Vector2(i, (float)j / (float)sides);

                    float fWeight0 = 1.0f;
                    float fWeight1 = 1.0f - fWeight0;
                    boneWeights[vertexIndex] = new BoneWeight()
                    {
                        boneIndex0 = i,
                        weight0 = 1.0f,
                        boneIndex1 = i,
                        weight1 = 0.0f
                    };
                }

                if (i > 0)
                {
                    for (int k = 0; k < sides; ++k)
                    {
                        //
                        int baseIndex = (i - 1) * sides;
                        triangles[triangleIndex] = baseIndex + k;
                        triangles[triangleIndex + 1] = baseIndex + sides + (k + 1) % sides;
                        triangles[triangleIndex + 2] = baseIndex + sides + k;
                        triangles[triangleIndex + 3] = baseIndex + (k + 1) % sides;
                        triangles[triangleIndex + 4] = baseIndex + sides + (k + 1) % sides;
                        triangles[triangleIndex + 5] = baseIndex + k;

                        triangleIndex += 6;
                    }
                }

                bindPoses[i] = bones[i].worldToLocalMatrix * gameObject.transform.localToWorldMatrix;
            }

            // 两端切面, 放在前面，方便后续更新
            FillSectionTriangles(0, SectionFrontFace.kClockwise, triangles, 0);
            FillSectionTriangles(pointCount - 1, SectionFrontFace.kCounterClockwise, triangles, sectionTriangles * 3);
            //for (int i = 0; i < sides - 2; ++i)
            //{
            //    triangles[triangleIndex] = 0;
            //    triangles[triangleIndex + 1] = 2 + i;
            //    triangles[triangleIndex + 2] = 1 + i;
            Debug.Log(pointCount);

            //    triangleIndex += 3;
            //}

            //for (int i = 0; i < sides - 2; ++i)
            //{
            //    int first = verices.Length - sides;
            //    triangles[triangleIndex] = first;
            //    triangles[triangleIndex + 1] = first + 1 + i;
            //    triangles[triangleIndex + 2] = first + 2 + i;

            //    triangleIndex += 3;
            //}

            Mesh mesh = new Mesh();
            mesh.vertices = verices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.boneWeights = boneWeights;
            mesh.bindposes = bindPoses;

            mesh.RecalculateNormals();

            return mesh;
        }

        Mesh BuildMesh(Transform[] bones)
        {
            // 每个bone为一截，总共是bones.length+1个点
            var pointCount = bones.Length + 1;

            // build mesh
            Vector3[] verices = new Vector3[pointCount * sides];
            Vector2[] uvs = new Vector2[pointCount * sides];
            var boneWeights = new BoneWeight[pointCount * sides];
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                //
                FillVertices(Vector3.zero, bones[i], verices, i * sides, sides);
                FillVertexUV(uvs, i, i * sides, sides);
                FillBoneWeights(boneWeights, i * sides, i);

                bindPoses[i] = bones[i].worldToLocalMatrix * gameObject.transform.localToWorldMatrix;
            }

            // 最后一个点
            FillVertices(new Vector3(0, 0, segmentLength), bones[bones.Length - 1], verices, bones.Length * sides, sides);
            FillVertexUV(uvs, pointCount - 1, (pointCount - 1) * sides, sides);
            FillBoneWeights(boneWeights, (pointCount - 1) * sides, bones.Length - 1);

            // 圆柱的三角面+两侧切面的三角面
            int[] triangles = new int[(pointCount - 1) * sides * 2 * 3 + sectionTriangles * 3 * 2];

            // 两端切面, 放在前面，方便后续更新
            FillSectionTriangles(0, SectionFrontFace.kClockwise, triangles, 0);
            FillSectionTriangles(pointCount - 1, SectionFrontFace.kCounterClockwise, triangles, sectionTriangles * 3);

            int triangleIndex = sectionTriangles * 3 * 2;
            // 三角面
            for (int i = 0; i < pointCount - 1; ++i)
            {
                for(int j = 0; j < sides; ++j)
                {
                    //
                    int baseIndex = i * sides;
                    triangles[triangleIndex] = baseIndex + j;
                    triangles[triangleIndex + 1] = baseIndex + sides + (j + 1) % sides;
                    triangles[triangleIndex + 2] = baseIndex + sides + j;
                    triangles[triangleIndex + 3] = baseIndex + (j + 1) % sides;
                    triangles[triangleIndex + 4] = baseIndex + sides + (j + 1) % sides;
                    triangles[triangleIndex + 5] = baseIndex + j;

                    triangleIndex += 6;

                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.boneWeights = boneWeights;
            mesh.bindposes = bindPoses;

            mesh.RecalculateNormals();

            return mesh;
        }

        /// <summary>
        /// 生成绳子
        /// </summary>
        public void Generate()
        {
            if (!hasNodes_)
            {
                hasNodes_ = true;
                var bones = CreateRopeNodes(startPoint, endPoint).ToArray();
                skinnedMeshRenderer.sharedMesh = BuildMesh(bones);
                skinnedMeshRenderer.bones = bones;
            }
        }
    }
}
