using MovementController.Collision;
using MovementController.Entity;
using UnityEngine;

namespace MovementController.Player.States
{
    /// <summary>
    /// The state for whenever we're in the air, whether it's from jumping, falling or anything else.
    /// </summary>
    public class InAirState : State
    {
        public float hangRayCastLengthX = 0.25f;
        public float hangRayCastLengthY = 0.15f;
        private bool _hitHead;
        private bool _bouncedOnEnemy;
        public LayerMask solidBlockMask;

        private Collider2D _lastColliderToSee;

        [HideInInspector] public float lastJumpAttemptTime = float.MinValue;

        public override void EnterState()
        {
            player.Physics.OnCollisionEnterEvent.AddListener(OnEntityPhysicsCollisionEnter);
        }

        public override void ExitState()
        {
            player.Physics.OnCollisionEnterEvent.RemoveListener(OnEntityPhysicsCollisionEnter);
        }

        public override void UpdateState()
        {
            player.groundedGraceTimer += Time.deltaTime;

            // TODO: We currently only have a single sprite for anything "air" related. Later on we would probably
            // created animations for jumping, falling etc. It's especially important to have something for when we're
            // ragdolled.
            player.Visuals.animator.Play("Jump");

            HandleEdgeGrabbing();
        }

        public override void OnDirectionalInput(Vector2 input)
        {
            base.OnDirectionalInput(input);

            if (player.directionalInput.y > 0)
            {
                player.stateMachine.AttemptToChangeState(player.climbingState);
            }
        }

        public override void OnJumpInputDown()
        {
            if (player.groundedGraceTimer > player.groundedGracePeriod)
            {
                lastJumpAttemptTime = Time.time;
                return;
            }

            base.OnJumpInputDown();
        }

        public override void ChangePlayerVelocity(ref Vector2 velocity)
        {
            if (_hitHead)
            {
                velocity.y = 0;
                _hitHead = false;
            }

            if (_bouncedOnEnemy)
            {
                // TODO: This should not be a full jump. Maybe half height or something.
                velocity.y = player._maxJumpVelocity;
                _bouncedOnEnemy = false;
            }
        }

        private void HandleEdgeGrabbing()
        {
            Vector2 direction = Vector2.right * player.Visuals.facingDirection;

            // This was just what felt right.
            // TODO: Maybe this isn't the best suited for when we're grabbing with the glove. Investigate this.
            const float yOffset = 0.75f;
            // This should only stick 1 "pixel" out from our collider so that we can grab the tiniest of ledges.
            // TODO: This doesn't currently work. We're able to stand on any width of ledge, but we're not able to grab
            // very tiny ledges. We need a better check here. There's also a bug with CrawlToHang if we're on really
            // tiny ledges where it will hang on the ledge above us instead of the one below us.
            Vector2 rayOrigin = transform.position + Vector3.up * yOffset;
            float rayLength = hangRayCastLengthX;
            Vector2 rayTip = rayOrigin + direction * rayLength;
            Vector2 playerPointCheck = rayOrigin + Vector2.up * hangRayCastLengthY;
            Vector2 grabPointCheck = rayTip + Vector2.up * hangRayCastLengthY;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, rayLength, player.edgeGrabLayerMask);
            Debug.DrawRay(rayOrigin, direction * rayLength, Color.cyan);


            Debug.DrawLine(rayTip, grabPointCheck, Color.red);

            // We're currently trying to move into a wall either on the left or on the right.
            bool movingIntoWallOnTheLeft = player.Physics.collisionInfo.left && player.directionalInput.x < 0 && !player.Visuals.isFacingRight;
            bool movingIntoWallOnTheRight = player.Physics.collisionInfo.right && player.directionalInput.x > 0 && player.Visuals.isFacingRight;

            if (player.velocity.y > 0 || hit.collider == null)
            {
                _lastColliderToSee = hit.collider;
                return;
            }

            // If we have the glove we can grab anything.
            if ((movingIntoWallOnTheLeft || movingIntoWallOnTheRight) && player.Inventory.hasClimbingGlove)
            {
                // TODO: How do we pass data to a state?
                player.hangingState.colliderToHangFrom = hit.collider;
                player.hangingState.grabbedWallUsingGlove = true;
                player.stateMachine.AttemptToChangeState(player.hangingState);
                _lastColliderToSee = hit.collider;
                return;
            }

            // Otherwise we can only grab ledges (tile corners).
            // lastEdgeGrabRayCastHit.collider == null ensures we'll only grab
            // an actual ledge with air above it and only when we're falling downwards.
            if (CanGrab(playerPointCheck, grabPointCheck) && _lastColliderToSee == null)
            {
                player.hangingState.colliderToHangFrom = hit.collider;
                player.stateMachine.AttemptToChangeState(player.hangingState);
            }

            _lastColliderToSee = hit.collider;
        }

        private bool CanGrab(Vector2 playerPointCheck, Vector2 grabPointCheck)
        {
            Collider2D hitBlockPlayer = Physics2D.OverlapPoint(playerPointCheck, player.edgeGrabLayerMask | solidBlockMask);
            Collider2D hitBlockBlock = Physics2D.OverlapPoint(grabPointCheck, player.edgeGrabLayerMask | solidBlockMask);
            return hitBlockPlayer == null && hitBlockBlock == null;
        }

        private void OnEntityPhysicsCollisionEnter(CollisionInfo collisionInfo)
        {
            if (collisionInfo.becameGroundedThisFrame)
            {
                player.stateMachine.AttemptToChangeState(player.groundedState);
            }

            if (collisionInfo.up)
            {
                // Same as _bouncedOnEnemy.
                _hitHead = true;
            }
        }
    }
}