using MovementController.Collision;
using MovementController.Player.States;
using MovementController.Utils;
using System;
using UnityEngine;

namespace MovementController.Level
{
    /// <summary>
    /// A pushable block.
    /// </summary>
    public class Block : Entity.Entity
    {
        public Vector2 gravity = new(0, -30f);
        public AudioClip landClip;

        private Vector3 _velocity;

        // References.
        private AudioSource _audioSource;
        private bool _initialized;

        public override void Awake()
        {
            base.Awake();
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            // If we're not grounded we snap our x position to the tile grid to avoid floating point inaccuraies in our
            // alignment and we zero out our x velocity to avoid any further movement on the horizontal axis.
            if (!Physics.collisionInfo.down)
            {
                Vector3 centerOfBlock = transform.position + (Vector3)Physics.Collider.offset;
                Vector3 lowerLeftCornerOfTileWeAreIn = MovementUtils.GetPositionOfLowerLeftOfNearestTile(centerOfBlock);
                transform.position = new Vector3(lowerLeftCornerOfTileWeAreIn.x + 0.5f - Physics.Collider.offset.x, transform.position.y, transform.position.z);
                _velocity.x = 0;
            }

            if (Physics.collisionInfo.down)
            {
                _velocity.y = 0;
            }
            else
            {
                _velocity.y += gravity.y * Time.deltaTime;
            }

            Physics.Move(_velocity * Time.deltaTime);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            // Not sure if hacky or not, but I added this to avoid the landClip from playing on level start for all
            // blocks in the level.
            if (!_initialized)
            {
                _initialized = true;
                return;
            }

            if (_velocity.y < 0)
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    Player.Player player = other.gameObject.GetComponent<Player.Player>();
                    if (player.stateMachine.CurrentState is GroundedState or CrawlToHangState)
                    {
                        player.Splat();
                    }
                    else if (player.stateMachine.CurrentState is ClimbingState)
                    {
                        player.stateMachine.AttemptToChangeState(player.inAirState);
                    }
                    else if (player.stateMachine.CurrentState is HangingState)
                    {
                        player.stateMachine.AttemptToChangeState(player.inAirState);
                    }
                }
                else
                {
                    _audioSource.clip = landClip;
                    _audioSource.Play();
                }
            }
        }

        public void Push(float pushSpeed)
        {
            _velocity.x = pushSpeed;
        }
    }
}