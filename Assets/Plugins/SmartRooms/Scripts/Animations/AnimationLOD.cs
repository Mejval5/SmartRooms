using SmartRooms.Utils;
using UnityEngine;

namespace SmartRooms.Animations
{
    public class AnimationLOD : MonoBehaviour
    {
        [Tooltip("If this is null, the script will use the transform of the object this script is attached to.")] [SerializeField]
        private Transform _objectOverride;

        [SerializeField] private float _cameraDistanceThresholdRatio = 1.1f;

        private Animator _animator;
        private Animation _animation;
        private ParticleSystem _particles;

        // Start is called before the first frame update
        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            _particles = GetComponentInChildren<ParticleSystem>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (_animator == null && _particles == null && _animation == null)
            {
                return;
            }

            Vector3 pos = _objectOverride != null ? _objectOverride.position : transform.position;

            if (Camera.main == null)
            {
                return;
            }

            float distanceToCamera = (Camera.main.transform.position - pos).ToVector2().magnitude;

            Vector2 cameraRect = new Vector2(Camera.main.orthographicSize * Camera.main.aspect, Camera.main.orthographicSize);
            float threshold = cameraRect.magnitude * _cameraDistanceThresholdRatio;

            if (threshold * 1.2f < distanceToCamera)
            {
                if (_animator != null)
                {
                    _animator.enabled = false;
                }

                if (_animation != null)
                {
                    _animation.enabled = false;
                }

                if (_particles != null)
                {
                    _particles.gameObject.SetActive(false);
                }
            }

            if (threshold > distanceToCamera)
            {
                if (_animator != null)
                {
                    _animator.enabled = true;
                }

                if (_animation != null)
                {
                    _animation.enabled = false;
                }

                if (_particles != null)
                {
                    _particles.gameObject.SetActive(true);
                }
            }
        }
    }
}