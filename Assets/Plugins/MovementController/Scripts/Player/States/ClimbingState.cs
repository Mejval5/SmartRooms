using System.Collections.Generic;
using UnityEngine;

namespace MovementController
{
    /// <summary>
    /// The state we're in when we're climbing a ladder or a rope.
    /// </summary>
    public class ClimbingState : State
    {
        public ContactFilter2D ladderFilter;
        public LayerMask ladderLayerMask;

        private Collider2D _closestCollider;

        public override bool CanEnterState()
        {
            if (player.directionalInput.y == 0)
            {
                return false;
            }

            if (Mathf.Abs(player.directionalInput.y) * 3 < Mathf.Abs(player.directionalInput.x) * 2)
            {
                return false;
            }

            if (player.recentlyJumped)
            {
                return false;
            }

            // Find any nearby ladder colliders.
            _closestCollider = FindClosestOverlappedLadder();
            if (_closestCollider == null)
            {
                return false;
            }

            // Raycast ahead of us and set our velocity to 0 if there is no ladder (or rope) in front of us, effectively
            // disallowing us from climbing off and falling. We have to manually jump to leave a ladder.
            Vector2 direction = Vector2.down;
            Vector3 position = transform.position + Vector3.up * 0.5f;
            if (player.directionalInput.y > 0)
            {
                direction = Vector2.up;
            }

            RaycastHit2D hit = Physics2D.Raycast(position, direction, 9 / 16f, ladderLayerMask);
            Debug.DrawRay(position, direction * 9 / 16f, Color.magenta);
            if (hit.collider == null)
            {
                return false;
            }

            return true;
        }

        public override void EnterState()
        {
            player.Physics.collisionInfo.fallingThroughPlatform = true;
            float xPos = _closestCollider.transform.position.x;
            if (_closestCollider.CompareTag("Rope"))
            {
                player.Visuals.animator.Play("ClimbRope");
            }
            else
            {
                player.Visuals.animator.Play("ClimbLadder");
            }

            transform.position = new Vector3(xPos, transform.position.y, 0);
            player.Audio.Play(player.Audio.grabClip);
        }

        public override void UpdateState()
        {
            if (player.directionalInput.y < 0 && player.Physics.collisionInfo.down && !player.Physics.collisionInfo.colliderVertical.CompareTag("OneWayPlatform"))
            {
                player.stateMachine.AttemptToChangeState(player.groundedState);
            }

            // Continously look for a ladder collider so that we can react accordingly.
            _closestCollider = FindClosestOverlappedLadder();
            if (_closestCollider)
            {
                if (_closestCollider.CompareTag("Rope"))
                {
                    player.Visuals.animator.Play("ClimbRope");
                }
                else
                {
                    player.Visuals.animator.Play("ClimbLadder");
                }
            }

            // Set the framerate of the climbing animation dynamically based on our climbing speed.
            if (player.directionalInput.y != 0)
            {
                player.Visuals.animator.fps = Mathf.RoundToInt(Mathf.Abs(player.directionalInput.y).Remap(0.1f, 1.0f, 4, 18));
            }
            else
            {
                player.Visuals.animator.fps = 0;
            }
        }

        public override void ChangePlayerVelocity(ref Vector2 velocity)
        {
            // Allow us to move freely up and down, but disallow any horizontal movement.
            velocity.y = player.directionalInput.y * player.climbSpeed;
            velocity.x = 0;

            // Raycast ahead of us and set our velocity to 0 if there is no ladder (or rope) in front of us, effectively
            // disallowing us from climbing off and falling. We have to manually jump to leave a ladder.
            Vector2 direction = Vector2.down;
            Vector3 position = transform.position + Vector3.up * 0.5f;
            if (player.directionalInput.y > 0)
            {
                direction = Vector2.up;
            }

            RaycastHit2D hit = Physics2D.Raycast(position, direction, 9 / 16f, ladderLayerMask);
            Debug.DrawRay(position, direction * 9 / 16f, Color.magenta);
            if (hit.collider == null)
            {
                velocity.y = 0;
            }
        }

        /// <summary>
        /// Find the closest ladder, in the horizontal direction, that we're currently overlapping.
        ///
        /// If we're standing between two ladders we want to grab the closet one, not the first one
        /// in the list as that one could be the furthest one away.
        /// </summary>
        private Collider2D FindClosestOverlappedLadder()
        {
            List<Collider2D> ladderColliders = new();
            player.Physics.Collider.OverlapCollider(ladderFilter, ladderColliders);
            if (ladderColliders.Count <= 0)
            {
                return null;
            }

            float closestDistance = Mathf.Infinity;
            Collider2D closestCollider = null;
            foreach (Collider2D ladderCollider in ladderColliders)
            {
                float xPos = ladderCollider.transform.position.x;
                if (ladderCollider.CompareTag("Ladder"))
                {
                    xPos += ExtensionMethods.TileWidth / 2f;
                }

                float currentDistance = Mathf.Abs(transform.position.x - xPos);
                if (currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closestCollider = ladderCollider;
                }

                // Prioritize ropes over ladders due to the sprite sort order.
                if (currentDistance == closestDistance)
                {
                    if (ladderCollider.CompareTag("Rope"))
                    {
                        closestCollider = ladderCollider;
                    }
                }
            }

            return closestCollider;
        }
    }
}