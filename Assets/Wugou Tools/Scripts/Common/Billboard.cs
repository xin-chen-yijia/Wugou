using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
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
                    cam = cameras[0];
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
