using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public class FlyCamera : MonoBehaviour
    {
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
        public bool freezeMouseMove { get; set; } = false;

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

        private bool isInit_ = false;

        /// <summary>
        /// 用于加载场景等情况下手动初始化
        /// </summary>
        public void Init()
        {
            if(!isInit_)
            {
                isInit_ = true;

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

                    // put camera under parentTrans_
                    transform.SetParent(parentTrans_);
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        // 飞行时间越久，速度越快
        private float moveMultiplier_ = 1;
        private float accruedTime = 0.0f;   // 时间累计，每一帧都提速太快了
        // Update is called once per frame
        void Update()
        {
            float hx = 0.0f;
            float hy = 0.0f;
            float hz = 0.0f;
            hx += Input.GetAxis("Horizontal");
            hz += Input.GetAxis("Vertical");

            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");

            // 常按加速
            bool isKeyDown = Mathf.Abs(hx) > 0.01f || Mathf.Abs(hz) > 0.01f || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);

            if(!isKeyDown)
            {
                moveMultiplier_ = 1.0f;
            }
            accruedTime += Time.deltaTime;
            if(accruedTime > 0.1f)
            {
                accruedTime = 0.0f;
                moveMultiplier_ *= 1.04f;
                moveMultiplier_ = Mathf.Clamp(moveMultiplier_, 1, 12);
            }

            float tSpeed = moveSpeed * moveMultiplier_ * Time.deltaTime;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                tSpeed *= 3.0f;
            }

            // 鼠标左键控制移动
            if (Input.GetMouseButton(0) && !freezeMouseMove)
            {
                hx += -dx * 10;
                hy += -dy * 10;
                parentTrans_.Translate((hx) * tSpeed, (hy) * tSpeed, dy * tSpeed, Space.Self);       
            }

            // 常按右键移动
            if (Input.GetMouseButton(1))
            {
                parentTrans_.Translate(hx * tSpeed, hy * tSpeed, hz * tSpeed, Space.Self);
            }

            if (Input.GetKey(KeyCode.Q))
            {
                parentTrans_.Translate(0.0f, -tSpeed * 0.35f, 0.0f, Space.Self);
            }

            if (Input.GetKey(KeyCode.E))
            {
                parentTrans_.Translate(0.0f, tSpeed * 0.35f, 0.0f, Space.Self);
            }

            // 旋转
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

        /// <summary>
        /// Camera Locate to target,  just as camera's transform locate to target
        /// </summary>
        /// <param name="target"></param>
        public void LocateCameraTo(Transform target)
        {
            if(parentTrans_ != null)
            {
                // camera view parent,so parent at camera forward
                parentTrans_.position = target.position + target.forward * viewCenterDistance;

                // camera parent's origin rotation same with camera's rotation
                parentTrans_.rotation = target.rotation;
            }
            else
            {
                transform.position = target.position;
                transform.rotation = target.rotation;
            }

        }
    }
}
