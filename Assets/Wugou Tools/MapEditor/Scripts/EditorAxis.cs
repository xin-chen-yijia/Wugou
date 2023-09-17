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
        public float factor = 0.06f;    // У��ϵ��

        private float maxRayDistance = 1000;

        private string mapToolLayerName = "MapTool";
        public int mapToolLayer => LayerMask.NameToLayer(mapToolLayerName);   // �����������ڲ㣬��������

        private GameObject attachedObject_ = null;
        // �����ᵱǰ���ŵ�����
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

        // ����
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
                // ѡ��������ȹ���
                if (Physics.Raycast(ray, out hit, maxRayDistance, 1 << mapToolLayer))
                {
                    // TODO:��������
                    if (hit.collider != null)
                    {
                        var selectedAxisObj = hit.collider.gameObject;
                        var mat = selectedAxisObj.GetComponentInChildren<Renderer>().material;
                        var originColor = mat.color;
                        mat.color = Color.yellow;

                        Vector3 objPos = attachedObject.transform.position;

                        // ��������϶�����
                        // ƽ�����
                        var moveDir = selectedAxisObj.transform.parent.forward;
                        var planeNormal = Vector3.Cross(Vector3.up, moveDir).normalized;
                        // �жϹ���
                        if (Mathf.Abs(planeNormal.x) < 0.0001f && Mathf.Abs(planeNormal.y) < 0.0001f && Mathf.Abs(planeNormal.z) < 0.0001f)
                        {
                            planeNormal = Vector3.forward;
                        }
                        // ��һ�μ���,����У��
                        float disToPlane = -Vector3.Dot(Vector3.zero - objPos, planeNormal);

                        // haha
                        var intersectionRes = MathUtils.IntersectionPoints(ray.origin, ray.direction, planeNormal, disToPlane);
                        Vector3 v = intersectionRes.Item1 - objPos;
                        float len0 = Vector3.Dot(v, moveDir);

                        if (mode_ == Mode.kTranslate)
                        {                              
                            processDragAxis = () =>
                            {
                                #region �������λ�õ�������������󽻵㣬�����У���Ϊ�ƶ������У������ܲ�����������
                                //// �������㣬������ĵ�ΪO�㣬����������ϵĵ�ΪP�㣬��ƽ���ϵĵ�ΪPA������ƶ����ڽ�ƽ���ϵĵ�ΪPB��������ķ���dir��֪��
                                //// Ҳ������O + (PB-O)*d1 = P + dir * d2�Ľ�
                                //Vector3 PO = editorCamera.transform.position;
                                //float nearPlanHeight = Mathf.Tan(editorCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * editorCamera.nearClipPlane * 2;
                                //float nearPlanWidth = nearPlanHeight * editorCamera.aspect;
                                //Vector3 PB = new Vector3((Input.mousePosition.x / Screen.width - 0.5f) * nearPlanWidth, (Input.mousePosition.y / Screen.height - 0.5f) * nearPlanHeight, editorCamera.nearClipPlane);
                                //PB = editorCamera.transform.TransformPoint(PB);

                                //Vector3 PP = hit.point;
                                //Vector3 dir = selectedAxisObj.transform.up; // ����ģ�ͳ���

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

                                // ����������ƽ��Ľ��㣬Ȼ����ͶӰ���������Ƶ�ͶӰ��
                                //
                                Ray ray2 = editorCamera.ScreenPointToRay(Input.mousePosition);
                                var intersectionRes2 = MathUtils.IntersectionPoints(ray2.origin, ray2.direction, planeNormal, disToPlane);
                                Vector3 v2 = intersectionRes2.Item1 - objPos;
                                float len = Vector3.Dot(v2, moveDir); //len ��ʾҪ��Ŀ�귽���ƶ��ľ���
                                attachedObject.transform.position = objPos + moveDir * (len - len0);
                            };
                        }
                        else if(mode_ == Mode.kRotate)
                        {
                            var objRot = attachedObject.transform.rotation;
                            var rotAxis = selectedAxisObj.transform.parent.forward;
                            var originMousePos = Input.mousePosition;

                            // ������תƽ�淨��Զ������Ļ�ϵ�ͶӰ��Ȼ�����õ���������λ�������ļн���Ϊ��ת��
                            //float nearPlanHeight = Mathf.Tan(editorCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * editorCamera.nearClipPlane * 2;
                            //float nearPlanWidth = nearPlanHeight * editorCamera.aspect;
                            //PO *= editorCamera.nearClipPlane / PO.z;
                            //PA *= editorCamera.nearClipPlane / PA.z;

                            Vector3 PO = editorCamera.WorldToViewportPoint(selectedAxisObj.transform.parent.position);
                            Vector3 PA = editorCamera.WorldToViewportPoint(selectedAxisObj.transform.parent.position + selectedAxisObj.transform.parent.forward * selectedAxisObj.transform.parent.localScale.x);

                            Vector3 norm = PA - PO; 
                            Vector2 norm2 = new Vector2(norm.y,norm.x); // ��ת90��
                            if(Mathf.Abs(norm2.x) < 0.01f &&Mathf.Abs(norm2.y) < 0.01f)     // ע����ת�ᳯ������
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
                                float len = Vector3.Dot(v2, moveDir);   // len ��ʾҪ��Ŀ�귽���ƶ��ľ���

                                float f = len - len0;
                                f = 1.0f + f;  // ���ƶ����ʱ��ԭʼ��С
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

                                // ������Ҳ����
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

            #region ���ִ�С����
            // ͸��ͶӰ���������ԶС�����Ч����ͨ��͸�ӳ���ʵ�ֵ�
            // ��������������ϵģ���������ԽԶ��������ԽС��
            // Ϊ�������᲻���������Զ�����ţ�ͨ���ű�������任�Ƴ�

            // ������������Ĵ�ֱ���� 
            float distance = Vector3.Dot(transform.position - editorCameraObj.transform.position, editorCameraObj.transform.forward);
            float scale = distance * factor;
            //scale = Mathf.Clamp(scale, 0.01f, 0.1f);//�޶�һ�����ֵ����Сֵ
            transform.localScale = new Vector3(scale, scale, scale);
            #endregion

            processDragAxis?.Invoke();

            UpdatePosition();
        }

        /// <summary>
        /// ��ʾ������
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
