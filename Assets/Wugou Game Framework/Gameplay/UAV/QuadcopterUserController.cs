using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public class QuadcopterUserController : MonoBehaviour
    {
        public QuadcopterController quadcopterController;

        // Start is called before the first frame update
        void Start()
        {
            if (!quadcopterController)
            {
                quadcopterController = GetComponent<QuadcopterController>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            float throttle = 0.0f;
            if (Input.GetKey(KeyCode.Space))
            {
                throttle += 1;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                throttle -= 1;
            }

            float forward = 0.0f;
            if (Input.GetKey(KeyCode.W))
            {
                forward += 1;
            }

            if (Input.GetKey(KeyCode.S))
            {
                forward -= 1;
            }

            var dir = QuadcopterController.Direction.kForward;
            if (Input.GetKey(KeyCode.A))
            {
                dir = QuadcopterController.Direction.kLeft;
            }

            if (Input.GetKey(KeyCode.D))
            {
                dir = QuadcopterController.Direction.kRight;
            }
            quadcopterController.Move(forward, throttle, dir);
        }
    }
}
