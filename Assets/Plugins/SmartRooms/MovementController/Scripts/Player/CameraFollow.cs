using Cinemachine;
using System;
using UnityEngine;

namespace MovementController.Player
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CameraFollow : MonoBehaviour
    {
        public float verticalOffset = 4;
        public float lerpValue = 0.99f;

        private float _targetOffset;

        private CinemachineVirtualCamera _camera;
        private CinemachineTransposer _transposer;

        private void Start()
        {
            _camera = GetComponent<CinemachineVirtualCamera>();
            _transposer = _camera.GetCinemachineComponent<CinemachineTransposer>();
        }

        public void SetVerticalOffset(float offset)
        {
            _targetOffset = offset;
        }

        private void LateUpdate()
        {
            _transposer.m_FollowOffset.y = Mathf.Lerp(_transposer.m_FollowOffset.y, verticalOffset * _targetOffset, lerpValue);
        }
    }
}