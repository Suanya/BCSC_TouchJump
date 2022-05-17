using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/
        public Collider2D collider2d;
        /*internal new*/
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;


#if UNITY_ANDROID || UNITY_IOS

        [SerializeField] private Vector2 fingerStartPosition;
        [SerializeField] private Vector2 fingerEndPosition;

        [SerializeField] private float distanceForSwipe = 50f;

        private bool swipeStart = false;
        private bool swipeEnd = false;




#endif




        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

#if UNITY_STANDALONE

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

#endif

#if UNITY_ANDROID || UNITY_IOS

        protected override void Update()
        {

            float touchMove = 0f;
            swipeStart = false;
            swipeEnd = false;

            if (Input.touchCount > 0)
            {
                //get a touch!
                Touch touch = Input.GetTouch(0);

                //figure out if its a left or right touch

                if (touch.position.x > Screen.width / 2)
                {
                    Debug.Log("Right touch!");
                    touchMove = 1f;
                }
                else if (touch.position.x < Screen.width / 2)
                {
                    Debug.Log("Left touch!");
                    touchMove = -1f;
                }


                //determine if a swipe has occured

                //recording the finger position for each phase
                switch (touch.phase)
                {

                    case TouchPhase.Began:
                        Debug.Log("touch began!");
                        fingerStartPosition = touch.position;
                        break;

                    case TouchPhase.Moved:
                        Debug.Log("touch moved!");
                        fingerEndPosition = touch.position;
                        break;

                    case TouchPhase.Ended:
                        Debug.Log("touch ended!");
                        fingerEndPosition = touch.position;
                        break;

                }

                float swipeMagnitude = Mathf.Abs(fingerEndPosition.y - fingerStartPosition.y);


                if (swipeMagnitude > distanceForSwipe)
                {
                    Debug.Log("jump!");
                    if (touch.phase == TouchPhase.Began)
                    {
                        swipeStart = true;
                    }

                    else if (touch.phase == TouchPhase.Ended)
                    {
                        swipeEnd = true;
                        fingerStartPosition = new Vector2(0, 0);
                        fingerEndPosition = new Vector2(0, 0);
                    }

                }

            }

            if (controlEnabled)
            {
                move.x = touchMove;
                if (jumpState == JumpState.Grounded && swipeStart == true)
                    jumpState = JumpState.PrepareToJump;
                else if (swipeEnd == true)
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

#endif

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}