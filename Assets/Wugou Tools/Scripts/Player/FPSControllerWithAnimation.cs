using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 第一人称视角控制，支持Animation动画
    /// 1. run
    /// 2. idle
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSControllerWithAnimation : MonoBehaviour
    {
        private bool jumpFlag_;
        private bool isJumping_;
        private bool isWalking_;
        private bool previouslyGrounded_;

        [SerializeField]
        private float walkSpeed_ = 5;
        [SerializeField]
        private float runSpeed_ = 10;
        [SerializeField]
        private float jumpHeight_ = 10;

        [SerializeField]
        private float stickToGroundForce_ = 10;

        [SerializeField]
        private float gravityMultiplier_ = 2;

        private Vector3 moveDir_;

        private CharacterController characterController_;

        private CollisionFlags collisionFlags_;

        private EMouseLook mouseLook_;
        private Transform camera_;

        private Animation animation_;

        private bool m_isIdle = true;
        private bool m_isRunning = false;

        // Use this for initialization
        void Start()
        {
            jumpFlag_ = false;
            characterController_ = GetComponent<CharacterController>();
            camera_ = transform.GetComponentInChildren<Camera>().transform;

            mouseLook_ = new EMouseLook();
            mouseLook_.Init(transform, camera_);
            mouseLook_.SetCursorLock(false);

            animation_ = GetComponentInChildren<Animation>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!jumpFlag_)
            {
                jumpFlag_ = Input.GetKeyDown(KeyCode.Space);
            }

            if (!previouslyGrounded_ && characterController_.isGrounded)
            {
                moveDir_.y = 0;
                isJumping_ = false;
            }

            if (!characterController_.isGrounded && !isJumping_ && previouslyGrounded_)
            {
                moveDir_.y = 0;
            }

            previouslyGrounded_ = characterController_.isGrounded;

            if (Input.GetMouseButton(0))
            {
                mouseLook_.LookRotation(transform, camera_);
            }
        }

        private void FixedUpdate()
        {
            float speed;
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            isWalking_ = !Input.GetKey(KeyCode.LeftShift);
            speed = isWalking_ ? walkSpeed_ : runSpeed_;
            Vector2 move = new Vector2(horizontal, vertical);
            move.Normalize();

            Vector3 desiredMove = transform.forward * move.y + transform.right * move.x;

            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, characterController_.radius, Vector3.down, out hitInfo,
                characterController_.height / 2, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            moveDir_.x = desiredMove.x * speed;
            moveDir_.z = desiredMove.z * speed;

            if (characterController_.isGrounded)
            {
                moveDir_.y = -stickToGroundForce_;
                if (jumpFlag_)
                {
                    moveDir_.y = jumpHeight_;
                    jumpFlag_ = false;
                    isJumping_ = true;
                }
            }
            else
            {
                moveDir_ += Physics.gravity * gravityMultiplier_ * Time.fixedDeltaTime;
            }

            UpdateAnimation(moveDir_);
            collisionFlags_ = characterController_.Move(moveDir_ * Time.fixedDeltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (collisionFlags_ == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(characterController_.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }

        void UpdateAnimation(Vector3 move)
        {
            if(animation_ == null)
            {
                return;
            }
            // update the animator parameters
            if(Mathf.Abs(move.z) > 0 || Mathf.Abs(move.x) > 0)
            {
                if(m_isIdle)
                {
                    animation_.CrossFade("run");
                    m_isRunning = true;
                    m_isIdle = false;
                }
            }
            else
            {
                if(m_isRunning)
                {
                    animation_.CrossFade("idle");
                    m_isRunning = false;
                    m_isIdle = true;
                }
            }
        }
    }

}
