using System;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

#pragma warning disable 618, 649
namespace Wugou
{
    [Serializable]
    public class FOVKickW
    {
        public Camera Camera;                           // optional camera setup, if null the main camera will be used
        [HideInInspector] public float originalFov;     // the original fov
        public float FOVIncrease = 3f;                  // the amount the field of view increases when going into a run
        public float TimeToIncrease = 1f;               // the amount of time the field of view will increase over
        public float TimeToDecrease = 1f;               // the amount of time the field of view will take to return to its original size
        public AnimationCurve IncreaseCurve = new AnimationCurve();


        public void Setup(Camera camera)
        {
            CheckStatus(camera);

            Camera = camera;
            originalFov = camera.fieldOfView;
        }


        private void CheckStatus(Camera camera)
        {
            if (camera == null)
            {
                throw new Exception("FOVKick camera is null, please supply the camera to the constructor");
            }

            if (IncreaseCurve == null)
            {
                throw new Exception(
                    "FOVKick Increase curve is null, please define the curve for the field of view kicks");
            }
        }


        public void ChangeCamera(Camera camera)
        {
            Camera = camera;
        }


        public IEnumerator FOVKickUp()
        {
            float t = Mathf.Abs((Camera.fieldOfView - originalFov) / FOVIncrease);
            while (t < TimeToIncrease)
            {
                Camera.fieldOfView = originalFov + (IncreaseCurve.Evaluate(t / TimeToIncrease) * FOVIncrease);
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }


        public IEnumerator FOVKickDown()
        {
            float t = Mathf.Abs((Camera.fieldOfView - originalFov) / FOVIncrease);
            while (t > 0)
            {
                Camera.fieldOfView = originalFov + (IncreaseCurve.Evaluate(t / TimeToDecrease) * FOVIncrease);
                t -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            //make sure that fov returns to the original size
            Camera.fieldOfView = originalFov;
        }
    }

    [Serializable]
    public class CurveControlledBobW
    {
        public float HorizontalBobRange = 0.33f;
        public float VerticalBobRange = 0.33f;
        public AnimationCurve Bobcurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                                                            new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
                                                            new Keyframe(2f, 0f)); // sin curve for head bob
        public float VerticaltoHorizontalRatio = 1f;

        private float m_CyclePositionX;
        private float m_CyclePositionY;
        private float m_BobBaseInterval;
        private Vector3 m_OriginalCameraPosition;
        private float m_Time;


        public void Setup(Camera camera, float bobBaseInterval)
        {
            m_BobBaseInterval = bobBaseInterval;
            m_OriginalCameraPosition = camera.transform.localPosition;

            // get the length of the curve in time
            m_Time = Bobcurve[Bobcurve.length - 1].time;
        }


        public Vector3 DoHeadBob(float speed)
        {
            float xPos = m_OriginalCameraPosition.x + (Bobcurve.Evaluate(m_CyclePositionX) * HorizontalBobRange);
            float yPos = m_OriginalCameraPosition.y + (Bobcurve.Evaluate(m_CyclePositionY) * VerticalBobRange);

            m_CyclePositionX += (speed * Time.deltaTime) / m_BobBaseInterval;
            m_CyclePositionY += ((speed * Time.deltaTime) / m_BobBaseInterval) * VerticaltoHorizontalRatio;

            if (m_CyclePositionX > m_Time)
            {
                m_CyclePositionX = m_CyclePositionX - m_Time;
            }
            if (m_CyclePositionY > m_Time)
            {
                m_CyclePositionY = m_CyclePositionY - m_Time;
            }

            return new Vector3(xPos, yPos, 0f);
        }
    }

    [Serializable]
    public class LerpControlledBobW
    {
        public float BobDuration;
        public float BobAmount;

        private float m_Offset = 0f;


        // provides the offset that can be used
        public float Offset()
        {
            return m_Offset;
        }


        public IEnumerator DoBobCycle()
        {
            // make the camera move down slightly
            float t = 0f;
            while (t < BobDuration)
            {
                m_Offset = Mathf.Lerp(0f, BobAmount, t / BobDuration);
                t += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            // make it move back to neutral
            t = 0f;
            while (t < BobDuration)
            {
                m_Offset = Mathf.Lerp(BobAmount, 0f, t / BobDuration);
                t += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }
            m_Offset = 0f;
        }
    }


    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonControllerW : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] public float walkSpeed;
        [SerializeField] public float runSpeed;
        [SerializeField][Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] public float jumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] public float gravityMultiplier;
        [SerializeField] public MouseLookW mouseLook = new MouseLookW();
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKickW m_FovKick = new FOVKickW();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBobW m_HeadBob = new CurveControlledBobW();
        [SerializeField] private LerpControlledBobW m_JumpBob = new LerpControlledBobW();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
        private Animator m_Animator;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
            mouseLook.Init(transform, m_Camera.transform);

            m_Animator = GetComponent<Animator>();
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();

            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = Input.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = jumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            //// update the animator parameters
            //var turn = Mathf.Atan2(m_MoveDir.x, m_MoveDir.z);
            //var forward = new Vector3(m_MoveDir.x, turn, m_MoveDir.z).magnitude;
            //m_Animator.SetFloat("Forward", forward, 0.1f, Time.deltaTime);
            //m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
            //m_Animator.SetBool("Crouch", m_Crouching);
            //m_Animator.SetBool("OnGround", m_IsGrounded);
            //if (!m_IsGrounded)
            //{
            //    m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
            //}

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            if (m_FootstepSounds == null || m_FootstepSounds.Length == 0)
            {
                return;
            }

            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? walkSpeed : runSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            mouseLook.LookRotation(transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
