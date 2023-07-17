using System.Collections;
using MovementController.Level;
using MovementController.Player.States;
using UnityEngine;

namespace MovementController.Player
{
    public class Player : Entity.Entity
    {
        public PlayerInput Input { get; private set; }
        public PlayerAudio Audio { get; private set; }
        public PlayerInventory Inventory { get; private set; }

        public Collider2D whipCollider;

        [Header("States")]
        public GroundedState groundedState;
        public InAirState inAirState;
        public HangingState hangingState;
        public ClimbingState climbingState;
        public CrawlToHangState crawlToHangState;
        public EnterDoorState enterDoorState;
        public SplatState splatState;

        [Header("Level setup")]
        public LayerMask edgeGrabLayerMask;
        public CameraFollow cam;
        public Exit _exitDoor;

        [Header("Movement")]
        public float maxJumpHeight;
        public float minJumpHeight;
        public float timeToJumpApex;
        public float accelerationTime;
        public float climbSpeed;
        public float crawlSpeed;
        public float runSpeed;
        public float sprintSpeed;
        public float pushBlockSpeed;

        [HideInInspector] public bool sprinting;

        [Tooltip("The time in seconds that we are considered grounded after leaving a platform. Allows us to easier time jumps.")]
        public float groundedGracePeriod = 0.05f;

        public float jumpBufferTime = 0.05f;

        [HideInInspector] public float groundedGraceTimer;

        private float _gravity;
        [HideInInspector] public float _maxJumpVelocity;

        [HideInInspector] public float _minJumpVelocity;

        // TODO: Make this private. Currently the jump logic in State.cs is the only place we set this, but I'm not
        // entirely sure how to refactor that so that.
        [HideInInspector] public Vector2 velocity;
        private float _velocityXSmoothing;
        [HideInInspector] public Vector2 directionalInput;
        private float _speed;

        [HideInInspector] public bool recentlyJumped;
        [HideInInspector] public float _lastJumpTimer;

        [HideInInspector] public float _lookTimer;
        [HideInInspector] public float _timeBeforeLook = 1f;

        public StateMachine stateMachine = new();

        public override void Awake()
        {
            base.Awake();

            UpdatePhysics();

            Input = GetComponent<PlayerInput>();
            Audio = GetComponent<PlayerAudio>();
            Inventory = GetComponent<PlayerInventory>();

            Health.HealthChangedEvent.AddListener(OnHealthChanged);
        }

        private void UpdatePhysics()
        {
            _gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
            _maxJumpVelocity = Mathf.Abs(_gravity) * timeToJumpApex;
            _minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(_gravity) * minJumpHeight);
        }

        private void Start()
        {
            stateMachine.AttemptToChangeState(groundedState);
        }

        private void Update()
        {
            if (stateMachine.CurrentState == null)
            {
                return;
            }

            UpdatePhysics();
            stateMachine.CurrentState.UpdateState();

            SetPlayerSpeed();
            CalculateVelocity();

            // Used for giving ourselves a small amount of time before we grab
            // a ladder again. Can probably be solved cleaner. Without this at
            // the moment we would be unable to jump up or down ladders if we
            // were holding up or down at the same time.
            if (recentlyJumped)
            {
                _lastJumpTimer += Time.deltaTime;
                if (_lastJumpTimer > 0.35f)
                {
                    _lastJumpTimer = 0;
                    recentlyJumped = false;
                }
            }

            Physics.Move(velocity * Time.deltaTime);

            stateMachine.CurrentState.ChangePlayerVelocityAfterMove(ref velocity);
        }

        private void SetPlayerSpeed()
        {
            if (directionalInput.x != 0)
            {
                if (directionalInput.y < 0)
                {
                    _speed = crawlSpeed;
                }
                else if (sprinting)
                {
                    _speed = sprintSpeed;
                }
                else
                {
                    _speed = runSpeed;
                }
            }
        }

        private void CalculateVelocity()
        {
            float targetVelocityX = directionalInput.x * _speed;
            // TODO: This means we have a horizontal velocity for many seconds after letting go of the input. This tiny
            // velocity apparently can cause us to get dragged after enemies. It's of course the collision detection
            // that needs to be fixed and not the fact that we have deceleration on our movement, but I don't think I
            // understand what's going on here so I should try to understand it. Currently it doesn't really do what I
            // want. I don't want us to have a lingering velocity for many seconds after we stop moving. I want this to
            // just simulate some slight acceleration and deceleration, maybe over a second or something? And then we
            // also need to be able to affect this when we introduce ice which should be slippery.
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref _velocityXSmoothing, accelerationTime);

            velocity.y += _gravity * Time.deltaTime;

            stateMachine.CurrentState.ChangePlayerVelocity(ref velocity);
        }

        public void Use()
        {
            if (_exitDoor == null)
            {
                return;
            }

            stateMachine.AttemptToChangeState(enterDoorState);
        }

        private bool _isAttacking;

        public void Attack()
        {
            if (_isAttacking)
            {
                return;
            }

            StartCoroutine(DoAttack());
        }

        private IEnumerator DoAttack()
        {
            _isAttacking = true;
            whipCollider.enabled = true;

            Visuals.animator.PlayOnceUninterrupted("AttackWithWhip");
            Audio.Play(Audio.whipClip, 0.7f);

            yield return new WaitForSeconds(Visuals.animator.GetAnimationLength("AttackWithWhip"));

            _isAttacking = false;
            whipCollider.enabled = false;
        }

        public void EnteredDoorway(Exit door)
        {
            _exitDoor = door;
        }

        public void ExitedDoorway(Exit door)
        {
            _exitDoor = null;
        }

        public void Splat()
        {
            stateMachine.AttemptToChangeState(splatState);
        }

        private void OnHealthChanged()
        {
            if (Health.CurrentHealth <= 0)
            {
                stateMachine.AttemptToChangeState(splatState);
            }
        }

        // TODO: Make it so that we can show debug info for whatever entity we select.
        private void OnGUI()
        {
            // string[] debugInfo = {
            //     "--- Player info ---",
            //     "State: " + stateMachine.CurrentState.GetType().Name,
            //     "--- Physics info --- ",
            //     "Velocity X: " + Physics.Velocity.x,
            //     "Velocity Y: " + Physics.Velocity.y,
            //     "--- Physics Collision info --- ",
            //     "Down: " + Physics.collisionInfo.down,
            //     "Left: " + Physics.collisionInfo.left,
            //     "Right: " + Physics.collisionInfo.right,
            //     "Up: " + Physics.collisionInfo.up,
            //     "Collider horizontal: " + Physics.collisionInfo.colliderHorizontal,
            //     "Collider vertical: " + Physics.collisionInfo.colliderVertical,
            //     "Falling through platform: " + Physics.collisionInfo.fallingThroughPlatform
            // };
            // for (int i = 0; i < debugInfo.Length; i++) {
            //     GUI.Label(new Rect(8, 52 + 16 * i, 300, 22), debugInfo[i]);
            // }
        }
    }
}