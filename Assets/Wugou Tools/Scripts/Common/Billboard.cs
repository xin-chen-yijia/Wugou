using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 广告牌
    /// 注意：target使用的是transform，所以对使用了rigidbody，同时启用了interplote的物体表现会有延迟（帧率越低延迟越大）
    /// 解决方案：
    /// 1. target使用rigidbody的位置；
    /// 2. 关闭rigidbody的插值；
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        public Transform target;
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
                target = cam?.transform;
            }

        }

        // Update is called once per frame
        void Update()
        {
            if (freezeXZ)
            {
                var cache = transform.localEulerAngles;
                transform.LookAt(target);
                cache.y = transform.localEulerAngles.y;
                transform.localEulerAngles = cache;
            }
            else
            {
                transform.LookAt(target);
            }
        }
    }
}
