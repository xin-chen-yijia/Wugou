using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 广告牌,针对rigidbody物体，注意rigidbody要真实的代表摄像机的方位；
    /// 比如这种情况：如摄像机是在某个Rigidbody下，同时有一定的位移和角度，这时rigidbody就不能真实的代表摄像机的位置
    /// </summary>
    public class BillboardRigidbody : MonoBehaviour
    {
        public Rigidbody target;
        public bool freezeXZ = false;

        // Start is called before the first frame update
        void Start()
        {
            if(target == null)
            {
                Debug.LogWarning($"{name}'s billboard's target is null, use Camera.main");
                var cam = Camera.main;
                if(!cam)
                {
                    var cameras = GameObject.FindObjectsOfType<Camera>();
                    if(cameras.Length > 0 )
                    {
                        cam = cameras[0];
                    }
                }
                target = cam.GetComponent<Rigidbody>();
            }

        }

        // Update is called once per frame
        void Update()
        {
            if (freezeXZ)
            {
                var cache = transform.localEulerAngles;
                transform.LookAt(target.position);
                cache.y = transform.localEulerAngles.y;
                transform.localEulerAngles = cache;
            }
            else
            {
                transform.LookAt(target.position);
            }
        }

        //void LookAt(Vector3 pos)
        //{
        //    var mat = Matrix4x4.LookAt(transform.position, pos, Vector3.up);
        //    transform.position = mat.GetPosition();
        //    transform.rotation = mat.rotation;
        //}
    }
}
