using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 第一人称视角，无动画支持
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFPSController : MonoBehaviour
    {

        private bool m_isJump;
        private bool m_isJumping;
        [SerializeField]
        private bool m_isWalking;
        private bool m_previouslyGrounded;

        [SerializeField]
        private float m_walkSpeed = 5;
        [SerializeField]
        private float m_runSpeed = 10;
        [SerializeField]
        private float m_jumpSpeed = 10;

        [SerializeField]
        private float m_gravityMultiplier = 9.0f;   //重力

        private Vector2 m_input;
        private Vector3 m_moveDir;

        private CharacterController m_characterController;

        private CollisionFlags m_collisionFlags;

        private EMouseLook m_mouseLook;
        private Transform m_camera;

        // Use this for initialization
        void Start()
        {
            m_isJump = false;
            m_characterController = GetComponent<CharacterController>();
            m_camera = transform.GetComponentInChildren<Camera>().transform;

            m_mouseLook = new EMouseLook();
            m_mouseLook.Init(transform, m_camera);
            m_mouseLook.SetCursorLock(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_isJump)
            {
                m_isJump = Input.GetButtonDown("Jump");
            }

            if (!m_previouslyGrounded && m_characterController.isGrounded)
            {
                m_moveDir.y = 0;
                m_isJumping = false;
            }

            if (!m_characterController.isGrounded && !m_isJumping && m_previouslyGrounded)
            {
                m_moveDir.y = 0;
            }

            m_previouslyGrounded = m_characterController.isGrounded;

            if (Input.GetMouseButton(0))
            {
                m_mouseLook.LookRotation(transform, m_camera);
            }
        }

        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);

            Vector3 desiredMove = transform.forward * m_input.y + transform.right * m_input.x;

            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_characterController.radius, Vector3.down, out hitInfo,
                m_characterController.height / 2, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_moveDir.x = desiredMove.x * speed;
            m_moveDir.z = desiredMove.z * speed;

            if (m_characterController.isGrounded)
            {
                if (m_isJump)
                {
                    m_moveDir.y = m_jumpSpeed;
                    m_isJump = false;
                    m_isJumping = true;
                }
            }
            else
            {
                m_moveDir += Physics.gravity * m_gravityMultiplier * Time.fixedDeltaTime;
            }

            m_collisionFlags = m_characterController.Move(m_moveDir * Time.fixedDeltaTime);
        }

        private void GetInput(out float speed)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            m_isWalking = !Input.GetKey(KeyCode.LeftShift);
            speed = m_isWalking ? m_walkSpeed : m_runSpeed;
            m_input = new Vector2(horizontal, vertical);

            if (m_input.sqrMagnitude > 1)
            {
                m_input.Normalize();
            }
        }
    }

}
