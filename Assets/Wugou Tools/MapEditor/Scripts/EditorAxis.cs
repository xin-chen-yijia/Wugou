using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public class EditorAxis
    {
        public GameObject red;
        public GameObject green;
        public GameObject blue;

        public GameObject editorCameraObj;
        public Camera editorCamera => editorCameraObj.GetComponent<Camera>();
        public float factor = 0.06f;    // 校正系数

        private float maxRayDistance = 1000;

        private string mapToolLayerName = "MapTool";
        public int mapToolLayer => LayerMask.NameToLayer(mapToolLayerName);   // 工具物体所在层，如坐标轴

        private GameObject attachedObject_ = null;
        // 坐标轴当前附着的物体
        public GameObject attachedObject { 
            get { return attachedObject_; }
            set { 
                attachedObject_ = value;
                if (attachedObject_ == null)
                {
                    processDragAxis = null;
                    Hide();
                }
                else
                {
                    Show();
                }
            }
        }

        public bool isDragging => processDragAxis != null;

        public enum Mode
        {
            kNone =0,
            kTranslate,
            kRotate,
            kScale,
        }
        private Mode mode_ = Mode.kNone;

        // 方法
        System.Action processDragAxis = null;
        System.Action releaseAixs = null;

        private GameObject gameObject_ = null;
        private Transform transform =>gameObject_.transform;
        public EditorAxis(GameObject gameObject)
        {
            gameObject_ = gameObject;
            gameObject_.SetLayerRecursively(mapToolLayer);
        }

        // Update is called once per frame
        public void Update()
        {
            if (Input.GetMouseButtonDown(0)) {
                RaycastHit hit;
                Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
                // 选择坐标轴等工具
                if (Physics.Raycast(ray, out hit, maxRayDistance, 1 << mapToolLayer))
                {
                    // TODO:高亮物体
                    if (hit.collider != null)
                    {
                        var selectedAxisObj = hit.collider.gameObject;
                        var mat = selectedAxisObj.GetComponentInChildren<Renderer>().material;
                        var originColor = mat.color;
                        mat.color = Color.yellow;

                        Vector3 objPos = attachedObject.transform.position;

                        // 计算鼠标拖动参数
                        // 平面参数
                        var moveDir = selectedAxisObj.transform.parent.forward;
                        var planeNormal = Vector3.Cross(Vector3.up, moveDir).normalized;
                        // 判断共线
                        if (Mathf.Abs(planeNormal.x) < 0.0001f && Mathf.Abs(planeNormal.y) < 0.0001f && Mathf.Abs(planeNormal.z) < 0.0001f)
                        {
                            planeNormal = Vector3.forward;
                        }
                        // 第一次计算,用于校正
                        float disToPlane = -Vector3.Dot(Vector3.zero - objPos, planeNormal);

                        // haha
                        var intersectionRes = MathUtils.IntersectionPoints(ray.origin, ray.direction, planeNormal, disToPlane);
                        Vector3 v = intersectionRes.Item1 - objPos;
                        float len0 = Vector3.Dot(v, moveDir);

                        if (mode_ == Mode.kTranslate)
                        {                              
                            processDragAxis = () =>
                            {
                                #region 根据鼠标位置的摄像和坐标轴求交点，不可行，因为移动过程中，鼠标可能不在坐标轴上
                                //// 向量计算，计算机的点为O点，鼠标点击的轴上的点为P点，近平面上的的为PA，鼠标移动后在近平面上的点为PB，坐标轴的方向dir已知，
                                //// 也就是求O + (PB-O)*d1 = P + dir * d2的解
                                //Vector3 PO = editorCamera.transform.position;
                                //float nearPlanHeight = Mathf.Tan(editorCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * editorCamera.nearClipPlane * 2;
                                //float nearPlanWidth = nearPlanHeight * editorCamera.aspect;
                                //Vector3 PB = new Vector3((Input.mousePosition.x / Screen.width - 0.5f) * nearPlanWidth, (Input.mousePosition.y / Screen.height - 0.5f) * nearPlanHeight, editorCamera.nearClipPlane);
                                //PB = editorCamera.transform.TransformPoint(PB);

                                //Vector3 PP = hit.point;
                                //Vector3 dir = selectedAxisObj.transform.up; // 根据模型朝向

                                //Vector3 OB = PB - PO;
                                //print("OB:" + OB.normalized);
                                //print("ray:" + editorCamera.ScreenPointToRay(Input.mousePosition).direction.normalized);
                                //print("cross:" + Vector3.Cross(editorCamera.ScreenPointToRay(Input.mousePosition).direction, OB));

                                //cube.transform.position = PO + OB * 10;

                                //// Ox + OBx * d1 = Px + dir-x * d2
                                //// Oy + OBy * d1 = Py + dir-y * d2
                                //float a1 = OB.x;
                                //float b1 = -dir.x;
                                //float c1 = PO.x - PP.x;
                                //float a2 = OB.y;
                                //float b2 = -dir.y;
                                //float c2 = PO.y - PP.y;
                                //float d1 = 0, d2 = 0;
                                //if (MathUtils.BinaryLinearEquation(a1, b1, c1, a2, b2, c2, out d1, out d2))
                                //{
                                //    print("hah:" + (a1 * d1 + b1 * d2 + c1));
                                //    print("hah2:" + (a2 * d1 + b2 * d2 + c2));
                                //    Debug.Assert(a1 * d1 + b1 * d1 + c1 == a2 * d2 + b2 * d2 + c2);
                                //    Vector3 v = PO + OB * d1;
                                //    currentSelectObj_.transform.position = objPos + dir * d2;
                                //}
                                #endregion

                                // 计算射线与平面的交点，然后求投影，把物体移到投影点
                                //
                                Ray ray2 = editorCamera.ScreenPointToRay(Input.mousePosition);
                                var intersectionRes2 = MathUtils.IntersectionPoints(ray2.origin, ray2.direction, planeNormal, disToPlane);
                                Vector3 v2 = intersectionRes2.Item1 - objPos;
                                float len = Vector3.Dot(v2, moveDir); //len 表示要向目标方向移动的距离
                                attachedObject.transform.position = objPos + moveDir * (len - len0);
                            };
                        }
                        else if(mode_ == Mode.kRotate)
                        {
                            var objRot = attachedObject.transform.rotation;
                            var rotAxis = selectedAxisObj.transform.parent.forward;
                            var originMousePos = Input.mousePosition;

                            // 计算旋转平面法线远点在屏幕上的投影，然后计算该点和两个鼠标位置向量的夹角作为旋转量
                            //float nearPlanHeight = Mathf.Tan(editorCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * editorCamera.nearClipPlane * 2;
                            //float nearPlanWidth = nearPlanHeight * editorCamera.aspect;
                            //PO *= editorCamera.nearClipPlane / PO.z;
                            //PA *= editorCamera.nearClipPlane / PA.z;

                            Vector3 PO = editorCamera.WorldToViewportPoint(selectedAxisObj.transform.parent.position);
                            Vector3 PA = editorCamera.WorldToViewportPoint(selectedAxisObj.transform.parent.position + selectedAxisObj.transform.parent.forward * selectedAxisObj.transform.parent.localScale.x);

                            Vector3 norm = PA - PO; 
                            Vector2 norm2 = new Vector2(norm.y,norm.x); // 旋转90度
                            if(Mathf.Abs(norm2.x) < 0.01f &&Mathf.Abs(norm2.y) < 0.01f)     // 注意旋转轴朝里的情况
                            {
                                norm2 = new Vector2(0, 1.0f);
                            }

                            processDragAxis = () =>
                            {
                                var dir = selectedAxisObj.transform.parent.forward;
                                //Vector3 PB = new Vector3((Input.mousePosition.x / Screen.width - 0.5f) * nearPlanWidth, (Input.mousePosition.y / Screen.height - 0.5f) * nearPlanHeight, editorCamera.nearClipPlane);
                                //PB = editorCamera.transform.TransformPoint(PB);
                                //Debug.Log("PB:" + PB * 100);

                                //Debug.Log(Vector3.Angle((PB - PO), (PA - PO)));

                                Vector2 v = Input.mousePosition - originMousePos;
                                float f = Vector2.Dot(v, norm2) * 2.0f;
                                if(selectedAxisObj.transform.parent.name == "Green")
                                {
                                    f = -f;
                                }

                                attachedObject.transform.rotation = Quaternion.AngleAxis(f, rotAxis) * objRot;

                            };
                        }
                        else if(mode_ == Mode.kScale)
                        {
                            Vector3 objScale = attachedObject.transform.localScale;

                            processDragAxis = () =>
                            {
                                Ray ray2 = editorCamera.ScreenPointToRay(Input.mousePosition);
                                var intersectionRes2 = MathUtils.IntersectionPoints(ray2.origin, ray2.direction, planeNormal, disToPlane);
                                Vector3 v2 = intersectionRes2.Item1 - objPos;
                                float len = Vector3.Dot(v2, moveDir);   // len 表示要向目标方向移动的距离

                                float f = len - len0;
                                f = 1.0f + f;  // 不移动鼠标时是原始大小
                                Vector3 v = objScale;
                                switch (selectedAxisObj.transform.parent.name)
                                {
                                    case "Red":
                                        v.x *= f;
                                        break;
                                    case "Green":
                                        v.y *= f;
                                        break;
                                    case "Blue":
                                        v.z *= f;
                                        break;
                                    default:
                                        break;
                                }

                                attachedObject.transform.localScale = v;

                                // 坐标轴也拉长
                                selectedAxisObj.transform.parent.localScale = new Vector3(1, 1, f);
                            };

                        }
                        else
                        {
                            Debug.LogWarning("EditorAxis mode error....");
                        }

                        releaseAixs = () =>
                        {
                            selectedAxisObj.transform.parent.localScale = Vector3.one;
                            selectedAxisObj.GetComponentInChildren<Renderer>().material.color = originColor;
                            selectedAxisObj = null;
                        };
                    }


                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if(releaseAixs != null)
                {
                    releaseAixs.Invoke();
                    releaseAixs = null;

                    processDragAxis = null;
                }
            }

            #region 保持大小不变
            // 透视投影，物体近大远小，这个效果是通过透视除法实现的
            // 坐标轴放在物体上的，所以物体越远，坐标轴越小，
            // 为了坐标轴不随摄像机的远近缩放，通过脚本把这个变换移除

            // 物体离摄像机的垂直距离 
            float distance = Vector3.Dot(transform.position - editorCameraObj.transform.position, editorCameraObj.transform.forward);
            float scale = distance * factor;
            //scale = Mathf.Clamp(scale, 0.01f, 0.1f);//限定一个最大值和最小值
            transform.localScale = new Vector3(scale, scale, scale);
            #endregion

            processDragAxis?.Invoke();

            UpdatePosition();
        }

        /// <summary>
        /// 显示坐标轴
        /// </summary>
        /// <param name="mode"></param>
        public void SetMode(Mode mode)
        {
            mode_ = mode;
            transform.Find("Translate").gameObject.SetActive(mode == Mode.kTranslate);
            transform.Find("Rotation").gameObject.SetActive(mode == Mode.kRotate);
            transform.Find("Scale").gameObject.SetActive(mode == Mode.kScale);
        }

        public void Show()
        {
            gameObject_.SetActive(true);
        }

        public void Hide()
        {
            gameObject_.SetActive(false);
        }

        public void UpdatePosition()
        {
            if (attachedObject)
            {
                transform.position = attachedObject.transform.position;
                transform.rotation = attachedObject.transform.rotation;
            }
        }

        public void OnDrawGizmos()
        {

        }
    }


}
