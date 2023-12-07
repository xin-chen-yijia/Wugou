using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Wugou.MapEditor
{
    /// <summary>
    /// 坐标轴操作，用于移动、旋转、缩放物体；
    /// 快捷键：
    /// w 移动
    /// e 旋转
    /// r 缩放
    /// </summary>
    public class EditorAxis : MonoBehaviour
    {
        public GameObject axis;
        public GameObject controllerPlane;
        public Camera editorCamera;
        [Tooltip("use internal selection logic")]
        public bool enableSelectObj = true;

        private int toolLayer = 0;
        private int gizmosLayer = 0;

        private Vector3 positionStart_;
        private Quaternion quaternionStart_;
        private Vector3 scaleStart_;

        private Vector3 pointStart_;
        private Vector3 pointEnd_;

        private Space space = Space.World;

        public GameObject selectedObject { get; private set; }
        public bool isDragging { get; private set; }
        public bool isDraggingAxis { get { return axisName != kEmptyAxisName;  } }

        public enum Mode
        {
            kNone = 0,
            kTranslate,
            kRotate,
            kScale,
        }
        private Mode optionMode = Mode.kNone;

        // 内部改变模式时调用
        public UnityEvent<Mode> onOptionModeChanged = new UnityEvent<Mode>();

        public const string kEmptyAxisName = "NULL";
        public string axisName { get; private set; } = kEmptyAxisName;
        private string lastAxisName_ = kEmptyAxisName;

        // 排除了坐标轴之类的物体的层
        public int mainLayerMask { get; set; } = 1; // 默认就default

        /// <summary>
        /// 看向物体的视线方向
        /// </summary>
        public Vector3 eyeForward { get; private set; }

        private Dictionary<GameObject, Color> oldAxisColors_ = new Dictionary<GameObject, Color>();  // 缓存轴的颜色

        public Transform translateParent => axis.transform.Find("Translate");
        public Transform rotateParent => axis.transform.Find("Rotate");
        public Transform scaleParent => axis.transform.Find("Scale");

        private class Circle
        {
            /// <summary>
            /// 创建一个圆环
            /// </summary>
            /// <param name="radius">半径</param>
            /// <param name="tube">管半径</param>
            /// <param name="radialSegments">管分割数</param>
            /// <param name="tubularSegments">圆分割数</param>
            /// <param name="arc">角度</param>
            /// <returns></returns>
            public static GameObject Create(float radius = 1, float tube = 0.4f, int radialSegments = 12, int tubularSegments = 48, float arc = Mathf.PI * 2, Material mat = null)
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

                GameObject go = new GameObject("Circle");
                var filter = go.AddComponent<MeshFilter>();
                filter.mesh = mesh;

                var renderComp = go.AddComponent<MeshRenderer>();
                renderComp.material = mat;

                return go;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            toolLayer = LayerMask.NameToLayer("MapTool");
            gizmosLayer = LayerMask.NameToLayer("MapGizmos");

            CreateRotateAxis();
        }

        void CreateRotateAxis()
        {
            var axisShader = Shader.Find("Wugou/Axis");
            var matRed = new Material(axisShader);
            matRed.color = Color.red;
            var matGreen = new Material(axisShader);
            matGreen.color = Color.green;
            var matBlue = new Material(axisShader);
            matBlue.color = Color.blue;

            var matGray = new Material(axisShader);
            matGray.color = Color.gray;

            var axisTransparentShader = Shader.Find("Wugou/AxisTransparent");
            var matYellowTransparent = new Material(axisTransparentShader);
            var cc = Color.yellow;
            cc.a = 0.25f;
            matYellowTransparent.color = cc;

            var x = Circle.Create(0.5f, 0.0075f, 3, 64, 0.5f * Mathf.PI * 2, matRed);
            x.transform.localEulerAngles = new Vector3(0, 90, 0);
            x.AddComponent<MeshCollider>();
            x.name = "X";
            x.layer = toolLayer;

            float width = 0.01f;
            var y = Circle.Create(0.5f, width, 3, 64, 0.5f * Mathf.PI * 2, matGreen);
            y.transform.localEulerAngles = new Vector3(90, 0, 0);
            y.AddComponent<MeshCollider>();
            y.name = "Y";
            y.layer = toolLayer;

            var z = Circle.Create(0.5f, width, 3, 64, 0.5f * Mathf.PI * 2, matBlue);
            z.transform.localEulerAngles = new Vector3(0, 0, 0);
            z.AddComponent<MeshCollider>();
            z.name = "Z";
            z.layer = toolLayer;

            var e = Circle.Create(0.75f, width, 3, 64, Mathf.PI * 2, matYellowTransparent);
            e.AddComponent<MeshCollider>();
            e.name = "E";
            e.layer = toolLayer;

            var xyze = Circle.Create(0.5f, width, 3, 64, Mathf.PI * 2, matGray);
            //xyze.AddComponent<MeshCollider>();
            xyze.name = "XYZE";
            xyze.layer = toolLayer;

            var rotParent = rotateParent;
            x.transform.SetParent(rotParent);
            x.transform.localPosition = Vector3.zero;

            y.transform.SetParent(rotParent);
            y.transform.localPosition = Vector3.zero;

            z.transform.SetParent(rotParent);
            z.transform.localPosition = Vector3.zero;

            xyze.transform.SetParent(rotParent);
            xyze.transform.localPosition = Vector3.zero;

            e.transform.SetParent(rotParent);
            e.transform.localPosition = Vector3.zero;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.P))
            {
                if (space == Space.World)
                {
                    space = Space.Self;
                }
                else
                {
                    space = Space.World;
                }

                print(space);
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                SetOptionMode(Mode.kTranslate);
            }

            if (Input.GetKey(KeyCode.E))
            {
                SetOptionMode(Mode.kRotate);
            }

            if (Input.GetKey(KeyCode.R))
            {
                SetOptionMode(Mode.kScale);
            }


            if (editorCamera.orthographic)
            {
                eyeForward = editorCamera.transform.forward;
            }
            else
            {
                if (selectedObject)
                {
                    eyeForward = (editorCamera.transform.position - selectedObject.transform.position).normalized;
                }
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (enableSelectObj && Physics.Raycast(ray, out hit, 1000, mainLayerMask))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    selectedObject = hit.collider.gameObject;
                    axis.transform.position = selectedObject.transform.position;
                }
            }

            if (selectedObject)
            {
                UpdateAxisTransform();

                // 在辅助板上点击
                if (Input.GetMouseButtonDown(0) && !isDragging && Physics.Raycast(ray, out hit, 1000, 1 << gizmosLayer))
                {
                    positionStart_ = selectedObject.transform.position;
                    quaternionStart_ = selectedObject.transform.rotation;
                    scaleStart_ = selectedObject.transform.localScale;
                    pointStart_ = hit.point - selectedObject.transform.position;

                    isDragging = true;
                }

                if (!isDragging)
                {
                    if (Physics.Raycast(ray, out hit, 10000, 1 << toolLayer))
                    {
                        axisName = hit.collider.name;
                        UpdateControlPlane();   // 实时更新辅助板，TODO：每次轴变化时更新
                    }
                    else
                    {
                        axisName = kEmptyAxisName;
                    }
                }

                // apply option: translate, rotate, scale
                if (isDragging)
                {
                    if (Physics.Raycast(ray, out hit, 1000, 1 << gizmosLayer))
                    {
                        pointEnd_ = hit.point - positionStart_;
                        switch (optionMode)
                        {
                            case Mode.kTranslate:
                                var offset = pointEnd_ - pointStart_;
                                if (space == Space.Self)
                                {
                                    offset = Quaternion.Inverse(selectedObject.transform.rotation) * offset;
                                }

                                if (!axisName.Contains("X")) offset.x = 0;
                                if (!axisName.Contains("Y")) offset.y = 0;
                                if (!axisName.Contains("Z")) offset.z = 0;

                                if (space == Space.Self)
                                {
                                    offset = quaternionStart_ * offset * 1.0f;
                                }

                                selectedObject.transform.position = positionStart_ + offset;
                                break;
                            case Mode.kRotate:
                                var rv = pointEnd_ - pointStart_;
                                float ROTATION_SPEED = 150 / Vector3.Distance(selectedObject.transform.position, editorCamera.transform.position);
                                bool inPlaneRotation = false;
                                float rotateAngle = 0;
                                Vector3 rotateAixs = Vector3.right;
                                if (axisName == "XYZE")
                                {
                                    rotateAixs = Vector3.Cross(eyeForward, rv).normalized;
                                    rotateAngle = Vector3.Dot(rv, Vector3.Cross(eyeForward, rotateAixs)) * ROTATION_SPEED;

                                }
                                else if (axisName == "X" || axisName == "Y" || axisName == "Z")
                                {
                                    rotateAixs = axisName == "X" ? Vector3.right : axisName == "Y" ? Vector3.up : Vector3.forward;
                                    var temp = rotateAixs;
                                    if (space == Space.Self)
                                    {
                                        temp = selectedObject.transform.rotation * temp;
                                    }

                                    temp = Vector3.Cross(eyeForward, temp);

                                    if (temp.magnitude < 0.000001f)
                                    {
                                        inPlaneRotation = true;
                                    }
                                    else
                                    {
                                        rotateAngle = -Vector3.Dot(rv, temp.normalized) * ROTATION_SPEED;
                                    }
                                }

                                if (axisName == "E" || inPlaneRotation)
                                {
                                    rotateAixs = eyeForward;
                                    rotateAngle = Vector3.Angle(pointStart_, pointEnd_);
                                    rotateAngle *= (Vector3.Dot(Vector3.Cross(pointStart_.normalized, pointEnd_.normalized), eyeForward) < 0 ? -1 : 1);
                                }

                                // apply roation
                                if (space == Space.Self && axisName != "E" && axisName != "XYZE")
                                {
                                    selectedObject.transform.rotation = quaternionStart_ * Quaternion.AngleAxis(rotateAngle, rotateAixs);
                                }
                                else
                                {
                                    selectedObject.transform.rotation = Quaternion.AngleAxis(rotateAngle, rotateAixs) * quaternionStart_;
                                }

                                break;
                            case Mode.kScale:
                                if (axisName == "XYZ")
                                {
                                    float d = pointEnd_.magnitude / pointStart_.magnitude;
                                    if (Vector3.Dot(pointStart_, pointEnd_) < 0)
                                    {
                                        d = -d;
                                    }
                                    selectedObject.transform.localScale = scaleStart_ * d;

                                }
                                else
                                {
                                    var v1 = pointStart_;
                                    var v2 = pointEnd_;

                                    var v = new Vector3(v2.x / v1.x, v2.y / v1.y, v2.z / v1.z);
                                    if (!axisName.Contains("X"))
                                    {
                                        v.x = 1;
                                    }
                                    if (!axisName.Contains("Y"))
                                    {
                                        v.y = 1;
                                    }
                                    if (!axisName.Contains("Z"))
                                    {
                                        v.z = 1;
                                    }

                                    var tmp = scaleStart_;
                                    tmp.x *= v.x;
                                    tmp.y *= v.y;
                                    tmp.z *= v.z;
                                    selectedObject.transform.localScale = tmp;
                                }

                                break;
                            default:
                                break;
                        }

                    }
                }

            }



            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                axisName = kEmptyAxisName;
            }

            // axis highlight
            if (lastAxisName_ != axisName)
            {
                lastAxisName_ = axisName;
                var yellow = new Color(1, 1, 0, 0.5f);
                var axisCharList = axisName.ToList();
                foreach (var t in axis.transform.GetComponentsInChildren<Renderer>())
                {
                    if (t.name == axisName
                       || t.name.ToList().All((c) => { return axisCharList.Contains(c); }))
                    {
                        if (!oldAxisColors_.ContainsKey(t.gameObject))
                        {
                            oldAxisColors_[t.gameObject] = t.material.color;
                        }
                        t.material.color = yellow;
                    }
                    else
                    {
                        if (oldAxisColors_.ContainsKey(t.gameObject))
                        {
                            t.material.color = oldAxisColors_[t.gameObject];
                        }
                    }
                }
            }

        }

        private void UpdateControlPlane()
        {
            // position
            controllerPlane.transform.position = selectedObject.transform.position;

            // for rotation
            var tmpSpace = optionMode == Mode.kScale ? Space.Self : space;
            var v1 = (tmpSpace == Space.Self ? selectedObject.transform.rotation : Quaternion.identity) * Vector3.right;
            var v2 = (tmpSpace == Space.Self ? selectedObject.transform.rotation : Quaternion.identity) * Vector3.up;
            var v3 = (tmpSpace == Space.Self ? selectedObject.transform.rotation : Quaternion.identity) * Vector3.forward;

            Vector3 alignVec = v2;
            Vector3 dirVec = Vector3.zero;
            switch (optionMode)
            {
                case Mode.kTranslate:
                case Mode.kScale:
                    switch (axisName)
                    {
                        case "X":
                            alignVec = Vector3.Cross(v1, eyeForward);
                            dirVec = Vector3.Cross(alignVec, v1);
                            break;
                        case "Y":
                            alignVec = Vector3.Cross(v2, eyeForward);
                            dirVec = Vector3.Cross(alignVec, v2);
                            break;
                        case "Z":
                            alignVec = Vector3.Cross(v3, eyeForward);
                            dirVec = Vector3.Cross(alignVec, v3);
                            break;
                        case "XY":
                            dirVec = v3;
                            break;
                        case "YZ":
                            dirVec = v1;
                            break;
                        case "XZ":
                            alignVec = v3;
                            dirVec = v2;
                            break;
                        case "XYZ":
                            break;
                        default:
                            break;
                    }

                    break;
                case Mode.kRotate:
                default:
                    break;

            }

            var mat = Matrix4x4.LookAt(Vector3.zero, dirVec, alignVec);
            controllerPlane.transform.rotation = mat.rotation;
        }

        private void UpdateAxisTransform()
        {
            // scale
            float factor = 1.0f;
            if (editorCamera.orthographic)
            {
                factor = editorCamera.orthographicSize;
            }
            else
            {
                factor = Vector3.Distance(editorCamera.transform.position, selectedObject.transform.position) * (float)Math.Min(1.9 * Math.Tan(Math.PI * editorCamera.fieldOfView / 360), 7);
            }
            factor *= 0.2f;
            axis.transform.localScale = new Vector3(factor, factor, factor);

            // position
            axis.transform.position = selectedObject.transform.position;

            translateParent.gameObject.SetActive(optionMode == Mode.kTranslate);
            rotateParent.gameObject.SetActive(optionMode == Mode.kRotate);
            scaleParent.gameObject.SetActive(optionMode == Mode.kScale);

            // rotation
            var qua = space == Space.Self ? selectedObject.transform.rotation : Quaternion.identity;

            if (optionMode == Mode.kRotate)
            {
                var tmp = qua;
                var alignVec = Quaternion.Inverse(qua) * eyeForward;
                //axis.transform.rotation = Quaternion.identity;
                for (int i = 0; i < rotateParent.childCount; ++i)
                {
                    var axisObj = rotateParent.GetChild(i);
                    if (axisObj.name.Contains("E"))
                    {
                        var mat = Matrix4x4.LookAt(eyeForward, Vector3.zero, Vector3.up);
                        axisObj.transform.rotation = mat.rotation;
                    }

                    if (axisObj.name == "X")
                    {
                        var q = Quaternion.AngleAxis(Mathf.Atan2(-alignVec.y, alignVec.z) * Mathf.Rad2Deg + 90, Vector3.right);
                        axisObj.transform.rotation = qua * q * Quaternion.Euler(0, 90, 0);
                    }

                    if (axisObj.name == "Y")
                    {
                        var q = Quaternion.AngleAxis(Mathf.Atan2(alignVec.x, alignVec.z) * Mathf.Rad2Deg - 180, Vector3.up);
                        axisObj.transform.rotation = qua * q * Quaternion.Euler(-90, 0, 0);
                    }

                    if (axisObj.name == "Z")
                    {
                        var q = Quaternion.AngleAxis(Mathf.Atan2(alignVec.y, alignVec.x) * Mathf.Rad2Deg - 90, Vector3.forward);
                        axisObj.transform.rotation = qua * q * Quaternion.Euler(0, 0, 0);
                    }
                }
            }
            else
            {
                // do nothing
            }
        }

        /// <summary>
        /// 更改坐标轴模式 
        /// </summary>
        /// <param name="mode"></param>
        public void SetOptionMode(Mode mode)
        {
            optionMode = mode;
            onOptionModeChanged.Invoke(optionMode);
        }

        /// <summary>
        /// 不触发回调事件
        /// </summary>
        /// <param name="mode"></param>
        public void SetOptionModeWithoutNotify(Mode mode)
        {
            optionMode = mode;
        }

        public void SetSelectedObject(GameObject go)
        {
            selectedObject = go;

            positionStart_ = selectedObject.transform.position;
            quaternionStart_ = selectedObject.transform.rotation;
            scaleStart_ = selectedObject.transform.localScale;
        }

        private void OnDisable()
        {
            translateParent.gameObject.SetActive(false);
            rotateParent.gameObject.SetActive(false);
            scaleParent.gameObject.SetActive(false);
        }
    }
}
