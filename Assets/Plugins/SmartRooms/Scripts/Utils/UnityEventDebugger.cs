using System;
using UnityEngine;

namespace SmartRooms.Utils
{
    public class UnityEventDebugger : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("Awake");
        }

        private void Start()
        {
            Debug.Log("Start");
        }
        
        private void OnEnable()
        {
            Debug.Log("OnEnable");
        }
        
        private void OnDisable()
        {
            Debug.Log("OnDisable");
        }
        
        private void OnDestroy()
        {
            Debug.Log("OnDestroy");
        }
    }
}