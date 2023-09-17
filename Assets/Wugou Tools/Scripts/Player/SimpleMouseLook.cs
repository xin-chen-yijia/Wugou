using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 要求摄像机有父物体
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SimpleMouseLook : MonoBehaviour
    {
        private EMouseLook mouseLook_;

        // Use this for initialization
        void Start()
        {
            mouseLook_ = new EMouseLook();
            mouseLook_.Init(transform.parent, transform);
            mouseLook_.SetCursorLock(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                mouseLook_.LookRotation(transform.parent, transform);
            }
        }
    }

}

