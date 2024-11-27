using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Wugou.Editor
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
        public bool selfFindTarget = true;

        private float maxRaycastDistance = float.MaxValue;  // 最大射线距离

        private int toolLayer = 0;
        private int gizmosLayer = 0;

        private Vector3 positionStart_;
        private Quaternion quaternionStart_;
        private Vector3 scaleStart_;

        private Vector3 pointStart_;
        private Vector3 pointEnd_;

        /// <summary>
        /// 坐标轴所用的坐标系
        /// </summary>
        public Space space { get; set; } = Space.World;

        public GameObject selectedObject { get; private set; }
        public bool isDragging { get; private set; }

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
        public string activeAxisName { get; private set; } = kEmptyAxisName;
        private string lastActiveAxisName_ = kEmptyAxisName;

        // 排除了坐标轴之类的物体的层
        public int layer { get; set; } = 1; // 默认就default

        /// <summary>
        /// 看向物体的视线方向
        /// </summary>
        public Vector3 eyeForward { get; private set; }

        private Dictionary<GameObject, Color> oldAxisColors_ = new Dictionary<GameObject, Color>();  // 缓存轴的颜色

        public Transform translateParent;// => axis.transform.Find("Translate");
        public Transform rotateParent;// => axis.transform.Find("Rotate");
        public Transform scaleParent;// => axis.transform.Find("Scale");

        /// <summary>
        /// 创建坐标轴
        /// </summary>
        private class AxisMaker
        {
            public GameObject gameObject { get; private set; }
            public AxisMaker(string axisName, Transform parent = null)
            {
                gameObject = new GameObject(axisName);
                gameObject.transform.SetParent(parent, false);
                gameObject.transform.localRotation = Quaternion.identity;
            }

            public GameObject CreateRenderObject(string name, Mesh mesh, Material material, Vector3 localPosition, Vector3 localAngles)
            {
                GameObject go = new GameObject(name, new System.Type[] { typeof(MeshRenderer), typeof(MeshFilter) });
                go.GetComponent<MeshRenderer>().material = material;
                go.GetComponent<MeshFilter>().mesh = mesh;

                go.transform.SetParent(gameObject.transform);
                go.transform.localPosition = localPosition;
                go.transform.localRotation = Quaternion.Euler(localAngles);

                return go;
            }

            public GameObject CreatePickerObject(string name, Mesh mesh, Vector3 localPosition, Vector3 localAngles)
            {
                var colliderObj = new GameObject(name);
                colliderObj.AddComponent<MeshFilter>().mesh = mesh;
                colliderObj.AddComponent<MeshCollider>().convex = true;
                colliderObj.transform.SetParent(gameObject.transform);
                colliderObj.transform.localPosition = localPosition;
                colliderObj.transform.localRotation = Quaternion.Euler(localAngles);

                return colliderObj;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            toolLayer = LayerMask.NameToLayer("MapTool");
            gizmosLayer = LayerMask.NameToLayer("MapGizmos");

            var translateParentObj = new GameObject("Translate");
            translateParent = translateParentObj.transform;
            translateParent.SetParent(axis.transform, false);
            CreateTranslateAxis(translateParent);
            Utils.SetLayerRecursively(translateParentObj, toolLayer);

            var rotateParentObj = new GameObject("Rotate");
            rotateParent = rotateParentObj.transform;
            rotateParent.SetParent(axis.transform, false);
            CreateRotateAxes(rotateParent);
            Utils.SetLayerRecursively(rotateParentObj, toolLayer);

            var scaleParentObj = new GameObject("Scale");
            scaleParent = scaleParentObj.transform;
            scaleParent.SetParent(axis.transform, false);
            CreateScaleAxes(scaleParent);
            Utils.SetLayerRecursively(scaleParentObj, toolLayer);
        }

        /// <summary>
        /// 创建移动轴
        /// </summary>
        /// <param name="parentTrans"></param>
        void CreateTranslateAxis(Transform parentTrans)
        {
            var axisShader = Shader.Find("Wugou/Axis");
            var matRed = new Material(axisShader);
            matRed.color = Color.red;
            var matGreen = new Material(axisShader);
            matGreen.color = Color.green;
            var matBlue = new Material(axisShader);
            matBlue.color = Color.blue;

            var axisTransparentShader = Shader.Find("Wugou/AxisTransparent");
            var matTransparent = new Material(axisTransparentShader);
            matTransparent.color = new Color(1, 1, 1, 0.25f);

            var arrowMesh = CylinderMesh.Create(0, 0.04f, 0.1f, 12);
            var lineMesh = CylinderMesh.Create(0.0075f, 0.0075f, 0.5f, 3);
            var pickerMesh = CylinderMesh.Create(0.2f, 0, 0.6f, 4);

            var boxMesh = BoxMesh.Create(0.15f, 0.15f, 0.01f);
            var boxPickerMesh = BoxMesh.Create(0.2f, 0.2f, 0.01f);

            AxisMaker xMaker = new AxisMaker("X", parentTrans);
            xMaker.CreateRenderObject("Arrow", arrowMesh, matRed, new Vector3(0.5f, 0, 0), new Vector3(0, 0, -90));
            xMaker.CreateRenderObject("Line", lineMesh, matRed, new Vector3(0.25f, 0, 0), new Vector3(0, 0, -90));
            xMaker.CreatePickerObject("Picker", pickerMesh, new Vector3(0.3f, 0, 0), new Vector3(0, 0, -90));

            AxisMaker yMaker = new AxisMaker("Y", parentTrans);
            yMaker.CreateRenderObject("Arrow", arrowMesh, matGreen, new Vector3(0.0f, 0.5f, 0), new Vector3(0, 0, 0));
            yMaker.CreateRenderObject("Line", lineMesh, matGreen, new Vector3(0.0f, 0.25f, 0), new Vector3(0, 0, 0));
            yMaker.CreatePickerObject("Picker", pickerMesh, new Vector3(0.0f, 0.3f, 0), new Vector3(0, 0, 0));

            AxisMaker zMaker = new AxisMaker("Z", parentTrans);
            zMaker.CreateRenderObject("Arrow", arrowMesh, matBlue, new Vector3(0.0f, 0.0f, 0.5f), new Vector3(90, 0, 0));
            zMaker.CreateRenderObject("Line", lineMesh, matBlue, new Vector3(0.0f, 0, 0.25f), new Vector3(90, 0, 0));
            zMaker.CreatePickerObject("Picker", pickerMesh, new Vector3(0.0f, 0, 0.3f), new Vector3(90, 0, 0));

            AxisMaker xyMaker = new AxisMaker("XY", parentTrans);
            xyMaker.CreateRenderObject("Plane", boxMesh, matRed, new Vector3(0.15f, 0.15f, 0), new Vector3(0, 0, 0));
            xyMaker.CreatePickerObject("Picker", boxPickerMesh, new Vector3(0.15f, 0.15f, 0), new Vector3(0, 0, 0));

            AxisMaker yzMaker = new AxisMaker("YZ", parentTrans);
            yzMaker.CreateRenderObject("Plane", boxMesh, matGreen, new Vector3(0, 0.15f, 0.15f), new Vector3(0, 90, 0));
            yzMaker.CreatePickerObject("Picker", boxPickerMesh, new Vector3(0, 0.15f, 0.15f), new Vector3(0, 90, 0));

            AxisMaker xzMaker = new AxisMaker("XZ", parentTrans);
            xzMaker.CreateRenderObject("Plane", boxMesh, matBlue, new Vector3(0.15f, 0, 0.15f), new Vector3(-90, 0, 0));
            xzMaker.CreatePickerObject("Picker", boxPickerMesh, new Vector3(0.15f, 0, 0.15f), new Vector3(-90, 0, 0));

            AxisMaker xyzMaker = new AxisMaker("XYZ", parentTrans);
            xyzMaker.CreateRenderObject("Plane", OctahedronMesh.Create(0.1f, 0), matTransparent, Vector3.zero, Vector3.zero);
            xyzMaker.CreatePickerObject("Picker", OctahedronMesh.Create(0.2f, 0), Vector3.zero, Vector3.zero);
        }

        /// <summary>
        /// 创建旋转轴
        /// </summary>
        void CreateRotateAxes(Transform parentTrans)
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

            var xMaker = new AxisMaker("X", parentTrans);
            xMaker.CreateRenderObject("Tube", TorusMesh.Create(0.5f, 0.0075f, 3, 64, 0.5f * Mathf.PI * 2), matRed, Vector3.zero, new Vector3(0, 0, 0));
            xMaker.CreatePickerObject("Picker", TorusMesh.Create(0.5f, 0.1f, 4, 24), Vector3.zero, new Vector3(0, 0, 0));
            xMaker.gameObject.transform.localRotation = Quaternion.Euler(0, 90, 0);

            var yMaker = new AxisMaker("Y", parentTrans);
            yMaker.CreateRenderObject("Tube", TorusMesh.Create(0.5f, 0.0075f, 3, 64, 0.5f * Mathf.PI * 2), matGreen, Vector3.zero, new Vector3(0, 0, 0));
            yMaker.CreatePickerObject("Picker", TorusMesh.Create(0.5f, 0.1f, 4, 24), Vector3.zero, new Vector3(0, 0, 0));
            yMaker.gameObject.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var zMaker = new AxisMaker("Z", parentTrans);
            zMaker.CreateRenderObject("Tube", TorusMesh.Create(0.5f, 0.0075f, 3, 64, 0.5f * Mathf.PI * 2), matBlue, Vector3.zero, new Vector3(0, 0, 0));
            zMaker.CreatePickerObject("Picker", TorusMesh.Create(0.5f, 0.1f, 4, 24), Vector3.zero, new Vector3(0, 0, 0));
            zMaker.gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);

            var eMaker = new AxisMaker("E", parentTrans);
            eMaker.CreateRenderObject("Tube", TorusMesh.Create(0.75f, 0.0075f, 3, 64, Mathf.PI * 2), matYellowTransparent, Vector3.zero, new Vector3(0, 0, 0));
            eMaker.CreatePickerObject("Picker", TorusMesh.Create(0.75f, 0.1f, 2, 24), Vector3.zero, new Vector3(0, 0, 0));

            var xyzeMaker = new AxisMaker("XYZE", parentTrans);
            xyzeMaker.CreateRenderObject("Tube", TorusMesh.Create(0.5f, 0.0075f, 3, 64, Mathf.PI * 2), matGray, Vector3.zero, new Vector3(0, 0, 0));
            xyzeMaker.CreatePickerObject("Picker", SphereMesh.Create(0.25f, 10, 8), Vector3.zero, new Vector3(0, 0, 0));

            //var x = Circle.Create(0.5f, 0.0075f, 3, 64, 0.5f * Mathf.PI * 2, matRed);
            //x.transform.localEulerAngles = new Vector3(0, 90, 0);
            //x.AddComponent<MeshCollider>();
            //x.name = "X";
            //x.layer = toolLayer;

            //float width = 0.01f;
            //var y = Circle.Create(0.5f, width, 3, 64, 0.5f * Mathf.PI * 2, matGreen);
            //y.transform.localEulerAngles = new Vector3(90, 0, 0);
            //y.AddComponent<MeshCollider>();
            //y.name = "Y";
            //y.layer = toolLayer;

            //var z = Circle.Create(0.5f, width, 3, 64, 0.5f * Mathf.PI * 2, matBlue);
            //z.transform.localEulerAngles = new Vector3(0, 0, 0);
            //z.AddComponent<MeshCollider>();
            //z.name = "Z";
            //z.layer = toolLayer;

            //var e = Circle.Create(0.75f, width, 3, 64, Mathf.PI * 2, matYellowTransparent);
            //e.AddComponent<MeshCollider>();
            //e.name = "E";
            //e.layer = toolLayer;

            //var xyze = Circle.Create(0.5f, width, 3, 64, Mathf.PI * 2, matGray);
            ////xyze.AddComponent<MeshCollider>();
            //xyze.name = "XYZE";
            //xyze.layer = toolLayer;

            //var rotParent = rotateParent;
            //x.transform.SetParent(rotParent);
            //x.transform.localPosition = Vector3.zero;

            //y.transform.SetParent(rotParent);
            //y.transform.localPosition = Vector3.zero;

            //z.transform.SetParent(rotParent);
            //z.transform.localPosition = Vector3.zero;

            //xyze.transform.SetParent(rotParent);
            //xyze.transform.localPosition = Vector3.zero;

            //e.transform.SetParent(rotParent);
            //e.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 创建缩放轴
        /// </summary>
        /// <param name="parentTrans"></param>
        void CreateScaleAxes(Transform parentTrans)
        {
            var axisShader = Shader.Find("Wugou/Axis");
            var matRed = new Material(axisShader);
            matRed.color = Color.red;
            var matGreen = new Material(axisShader);
            matGreen.color = Color.green;
            var matBlue = new Material(axisShader);
            matBlue.color = Color.blue;

            var axisTransparentShader = Shader.Find("Wugou/AxisTransparent");
            var matTransparent = new Material(axisTransparentShader);
            matTransparent.color = new Color(1, 1, 1, 0.25f);

            var lineGeometry2 = CylinderMesh.Create(0.0075f, 0.0075f, 0.5f, 3);
            var scaleHandleMesh = BoxMesh.Create(0.08f, 0.08f, 0.08f);
            var linePickerMesh = CylinderMesh.Create(0.2f, 0, 0.6f, 4);

            var boxMesh = BoxMesh.Create(0.15f, 0.15f, 0.01f);
            var boxPickerMesh = BoxMesh.Create(0.2f, 0.2f, 0.01f);

            AxisMaker xMaker = new AxisMaker("X", parentTrans);
            xMaker.CreateRenderObject("Handle", scaleHandleMesh, matRed, new Vector3(0.5f, 0, 0), new Vector3(0, 0, -90));
            xMaker.CreateRenderObject("Line", lineGeometry2, matRed, new Vector3(0.25f, 0, 0), new Vector3(0, 0, -90));
            xMaker.CreatePickerObject("Picker", linePickerMesh, new Vector3(0.3f, 0, 0), new Vector3(0, 0, -90));

            AxisMaker yMaker = new AxisMaker("Y", parentTrans);
            yMaker.CreateRenderObject("Arrow", scaleHandleMesh, matGreen, new Vector3(0.0f, 0.5f, 0), new Vector3(0, 0, 0));
            yMaker.CreateRenderObject("Line", lineGeometry2, matGreen, new Vector3(0.0f, 0.25f, 0), new Vector3(0, 0, 0));
            yMaker.CreatePickerObject("Picker", linePickerMesh, new Vector3(0.0f, 0.3f, 0), new Vector3(0, 0, 0));

            AxisMaker zMaker = new AxisMaker("Z", parentTrans);
            zMaker.CreateRenderObject("Arrow", scaleHandleMesh, matBlue, new Vector3(0.0f, 0.0f, 0.5f), new Vector3(90, 0, 0));
            zMaker.CreateRenderObject("Line", lineGeometry2, matBlue, new Vector3(0.0f, 0, 0.25f), new Vector3(90, 0, 0));
            zMaker.CreatePickerObject("Picker", linePickerMesh, new Vector3(0.0f, 0, 0.3f), new Vector3(90, 0, 0));

            AxisMaker xyMaker = new AxisMaker("XY", parentTrans);
            xyMaker.CreateRenderObject("Plane", boxMesh, matRed, new Vector3(0.15f, 0.15f, 0), new Vector3(0, 0, 0));
            xyMaker.CreatePickerObject("Picker", boxPickerMesh, new Vector3(0.15f, 0.15f, 0), new Vector3(0, 0, 0));

            AxisMaker yzMaker = new AxisMaker("YZ", parentTrans);
            yzMaker.CreateRenderObject("Plane", boxMesh, matGreen, new Vector3(0, 0.15f, 0.15f), new Vector3(0, 90, 0));
            yzMaker.CreatePickerObject("Picker", boxPickerMesh, new Vector3(0, 0.15f, 0.15f), new Vector3(0, 90, 0));

            AxisMaker xzMaker = new AxisMaker("XZ", parentTrans);
            xzMaker.CreateRenderObject("Plane", boxMesh, matBlue, new Vector3(0.15f, 0, 0.15f), new Vector3(-90, 0, 0));
            xzMaker.CreatePickerObject("Picker", boxPickerMesh, new Vector3(0.15f, 0, 0.15f), new Vector3(-90, 0, 0));

            AxisMaker xyzMaker = new AxisMaker("XYZ", parentTrans);
            xyzMaker.CreateRenderObject("Plane", BoxMesh.Create(0.1f, 0.1f, 0.1f), matTransparent, Vector3.zero, Vector3.zero);
            xyzMaker.CreatePickerObject("Picker", BoxMesh.Create(0.2f, 0.2f, 0.2f), Vector3.zero, Vector3.zero);
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

            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (selfFindTarget && Physics.Raycast(ray, out hit, maxRaycastDistance, layer))
            {
                if (Input.GetMouseButtonDown(0) )
                {
                    selectedObject = hit.collider.gameObject;
                    axis.transform.position = selectedObject.transform.position;
                }
            }

            if (selectedObject)
            {
                UpdateAxisTransform();

                // 在辅助板上点击
                if (Input.GetMouseButtonDown(0) && !isDragging && Physics.Raycast(ray, out hit, maxRaycastDistance, 1 << gizmosLayer))
                {
                    positionStart_ = selectedObject.transform.position;
                    quaternionStart_ = selectedObject.transform.rotation;
                    scaleStart_ = selectedObject.transform.localScale;
                    pointStart_ = hit.point - selectedObject.transform.position;

                    isDragging = true;
                }

                if (!isDragging)
                {
                    if (Physics.Raycast(ray, out hit, maxRaycastDistance, 1 << toolLayer))
                    {
                        activeAxisName = hit.collider.transform.parent.name;
                        UpdateControlPlane();   // 实时更新辅助板，TODO：每次轴变化时更新
                    }
                    else
                    {
                        activeAxisName = kEmptyAxisName;
                    }
                }

                // apply option: translate, rotate, scale
                if (isDragging)
                {
                    if (Physics.Raycast(ray, out hit, maxRaycastDistance, 1 << gizmosLayer))
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

                                if (!activeAxisName.Contains("X")) offset.x = 0;
                                if (!activeAxisName.Contains("Y")) offset.y = 0;
                                if (!activeAxisName.Contains("Z")) offset.z = 0;

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
                                if (activeAxisName == "XYZE")
                                {
                                    rotateAixs = Vector3.Cross(eyeForward, rv).normalized;
                                    rotateAngle = Vector3.Dot(rv, Vector3.Cross(eyeForward, rotateAixs)) * ROTATION_SPEED;

                                }
                                else if (activeAxisName == "X" || activeAxisName == "Y" || activeAxisName == "Z")
                                {
                                    rotateAixs = activeAxisName == "X" ? Vector3.right : activeAxisName == "Y" ? Vector3.up : Vector3.forward;
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

                                if (activeAxisName == "E" || inPlaneRotation)
                                {
                                    rotateAixs = eyeForward;
                                    rotateAngle = Vector3.Angle(pointStart_, pointEnd_);
                                    rotateAngle *= (Vector3.Dot(Vector3.Cross(pointStart_.normalized, pointEnd_.normalized), eyeForward) < 0 ? -1 : 1);
                                }

                                // apply roation
                                if (space == Space.Self && activeAxisName != "E" && activeAxisName != "XYZE")
                                {
                                    selectedObject.transform.rotation = (quaternionStart_ * Quaternion.AngleAxis(rotateAngle, rotateAixs)).normalized;
                                }
                                else
                                {
                                    var tmpAixs = selectedObject.transform.parent ? Quaternion.Inverse(selectedObject.transform.parent.rotation) * rotateAixs : rotateAixs;
                                    selectedObject.transform.rotation = (Quaternion.AngleAxis(rotateAngle, tmpAixs) * quaternionStart_).normalized;
                                }

                                break;
                            case Mode.kScale:
                                if (activeAxisName == "XYZ")
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
                                    if (!activeAxisName.Contains("X"))
                                    {
                                        v.x = 1;
                                    }
                                    if (!activeAxisName.Contains("Y"))
                                    {
                                        v.y = 1;
                                    }
                                    if (!activeAxisName.Contains("Z"))
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
            else
            {
                // 隐藏
                translateParent.gameObject.SetActive(false);
                rotateParent.gameObject.SetActive(false);
                scaleParent.gameObject.SetActive(false);
            }


            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                activeAxisName = kEmptyAxisName;
            }

            // axis highlight
            if (lastActiveAxisName_ != activeAxisName)
            {
                lastActiveAxisName_ = activeAxisName;
                var yellow = new Color(1, 1, 0, 0.5f);
                foreach (var t in axis.transform.GetComponentsInChildren<Renderer>())
                {
                    if (t.transform.parent.name == activeAxisName
                       || t.transform.parent.name.ToList().All((c) => { return activeAxisName.Contains(c); }))
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
                    switch (activeAxisName)
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
            var tmpSpace = this.optionMode == Mode.kScale ? Space.Self : space;
            // rotation
            var qua = tmpSpace == Space.Self ? selectedObject.transform.rotation : Quaternion.identity;

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

            // rotation
            axis.transform.rotation = qua;

            translateParent.gameObject.SetActive(optionMode == Mode.kTranslate);
            rotateParent.gameObject.SetActive(optionMode == Mode.kRotate);
            scaleParent.gameObject.SetActive(optionMode == Mode.kScale);


            if (optionMode == Mode.kRotate)
            {
                var tmp = qua;
                var alignVec = Quaternion.Inverse(qua) * eyeForward;
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
                        //axisObj.transform.rotation = q * Quaternion.Euler(0, 90, 0);
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
#if UNITY_EDITOR    
            if (selfFindTarget)
            {
                Logger.Warning("selfFindTarget is true, but use outside set selected object...");
            }
#endif

            selectedObject = go;

            if (selectedObject)
            {
                positionStart_ = selectedObject.transform.position;
                quaternionStart_ = selectedObject.transform.rotation;
                scaleStart_ = selectedObject.transform.localScale;
            }
        }

        private void OnDisable()
        {
            translateParent.gameObject.SetActive(false);
            rotateParent.gameObject.SetActive(false);
            scaleParent.gameObject.SetActive(false);

            isDragging = false;
        }
    }
}
