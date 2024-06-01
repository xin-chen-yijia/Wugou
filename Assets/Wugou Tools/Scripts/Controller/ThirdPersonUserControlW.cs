using System;
using UnityEngine;

//#if CROSS_PLATFORM_INPUT
//using EInput = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager;
//#else
//using EInput = UnityEngine.Input;
//#endif
using EInput = UnityEngine.Input;

namespace Wugou
{
    [RequireComponent(typeof (ThirdPersonCharacterW))]
    public class ThirdPersonUserControlW : MonoBehaviour
    {
        private ThirdPersonCharacterW m_Character; // A reference to the ThirdPersonCharacter on the object
        public Transform lookCamera;                  // A reference to the main camera in the scenes transform
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.

        
        private void Start()
        {
            // get the transform of the main camera
            if (!lookCamera && Camera.main != null)
            {
                lookCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning(
                    "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
                // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
            }

            // get the third person character ( this should never be null due to require component )
            m_Character = GetComponent<ThirdPersonCharacterW>();
        }


        private void Update()
        {
            if (!m_Jump)
            {
                m_Jump = EInput.GetButtonDown("Jump");
            }
        }


        // Fixed update is called in sync with physics
        private void FixedUpdate()
        {
            // read inputs
            float h = EInput.GetAxis("Horizontal");
            float v = EInput.GetAxis("Vertical");
            bool crouch = Input.GetKey(KeyCode.C);

            // calculate move direction to pass to character
            if (lookCamera != null)
            {
                // calculate camera relative direction to move:
                m_CamForward = Vector3.Scale(lookCamera.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = v*m_CamForward + h*lookCamera.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = v*Vector3.forward + h*Vector3.right;
            }
#if !MOBILE_INPUT
			// walk speed multiplier
	        if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

            // pass all parameters to the character control script
            m_Character.Move(m_Move, crouch, m_Jump);
            m_Jump = false;
            m_Move = Vector3.zero;
        }

        private void OnDisable()
        {
            m_Character.StopMoveAndIdle();
        }
    }
}
