using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Wugou
{
    public class FlyCamera : MonoBehaviour
    {
        [Header("UI")]
        public bool enableOnUI = false;

        [Header("摄像机移动")]
        public float moveSpeed = 5.0f;

        [Header("摄像机旋转")]
        public float xRotSpeed = 20.0f;
        public float yRotSpeed = 20.0f;

        // 视线焦点
        public float viewCenterDistance = 10.0f;

        // 旋转是用父物体旋转实现
        private Transform parentTrans_ = null;

        // 控制鼠标移动视角
        public bool moveEnable { get; set; } = true;

        // 视角中心
        public Vector3 viewCenter { 
            get
            {
                return parentTrans_.position;
            }

            set
            {
                parentTrans_.position = value;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            parentTrans_ = transform.parent;
            if (!parentTrans_)
            {
                GameObject parentObj = new GameObject("FlyCamera");
                parentTrans_ = parentObj.transform;
                parentObj.tag = "Player";

                // camera view parent,so parent at camera forward
                parentTrans_.position = transform.position + transform.forward * viewCenterDistance;

                // camera parent's origin rotation same with camera's rotation
                parentTrans_.rotation = transform.rotation;
                transform.SetParent(parentTrans_);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!enableOnUI && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            float hx = 0.0f;
            float hy = 0.0f;
            float hz = 0.0f;
            hx += Input.GetAxis("Horizontal");
            hz += Input.GetAxis("Vertical");

            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");

            float tSpeed = moveSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                tSpeed *= 3.0f;
            }

            // 鼠标左键控制移动
            if (Input.GetMouseButton(0) && moveEnable)
            {
                hx += -dx * 10;
                hy += -dy * 10;
                //parentTrans_.Translate((-hx) * tSpeed, (-hy) * tSpeed, dy * tSpeed, Space.Self);       
            }

            parentTrans_.Translate(hx * tSpeed, hy * tSpeed, hz * tSpeed, Space.Self);

            if (Input.GetKey(KeyCode.Q))
            {
                parentTrans_.Translate(0.0f, -tSpeed * 0.35f, 0.0f, Space.Self);
            }

            if (Input.GetKey(KeyCode.E))
            {
                parentTrans_.Translate(0.0f, tSpeed * 0.35f, 0.0f, Space.Self);
            }


            if (Input.GetMouseButton(1))
            {
                float xAngle = xRotSpeed * dx * Time.deltaTime;
                float yAngle = -yRotSpeed * dy * Time.deltaTime;
                //Vector3 v = Quaternion.Euler(yAngle, xAngle, 0) * transform.forward * viewCenterDis_;
                //transform.position = transform.position + v - transform.forward * viewCenterDis_;
                //transform.rotation = transform.rotation * Quaternion.Euler(yAngle, -xAngle, 0);

                xAngle = Mathf.Clamp(xAngle, -10, 30);
                yAngle = Mathf.Clamp(yAngle, -10, 10);

                parentTrans_.rotation = Quaternion.Euler(0, xAngle, 0) * parentTrans_.rotation;
                parentTrans_.Rotate(yAngle, 0, 0, Space.Self);
            }
        }
    }
}
