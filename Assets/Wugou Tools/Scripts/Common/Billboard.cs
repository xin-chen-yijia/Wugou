using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public class Billboard : MonoBehaviour
    {
        public Transform target;
        // Start is called before the first frame update
        void Start()
        {
            if(target == null)
            {
                Debug.LogWarning($"{name}'s billboard's target is null, use Camera.main");
                target = Camera.main.transform;
            }
        }

        // Update is called once per frame
        void Update()
        {
            transform.LookAt(target);
        }
    }
}
