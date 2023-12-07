using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    [RequireComponent(typeof(VehicleController))]
    public class VehicleUserController : MonoBehaviour
    {
        private VehicleController m_Car; // the car controller we want to use


        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<VehicleController>();
        }


        private void FixedUpdate()
        {
            // pass the input to the car!
            //            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            //            float v = CrossPlatformInputManager.GetAxis("Vertical");
            //#if !MOBILE_INPUT
            //            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            //            m_Car.Move(h, v, v, handbrake);
            //#else
            //            m_Car.Move(h, v, v, 0f);
            //#endif
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
#if !MOBILE_INPUT
            float handbrake = Input.GetAxis("Jump");
            m_Car.Move(h, v, v, handbrake);
#else
            m_Car.Move(h, v, v, 0f);
#endif
        }
    }
}
