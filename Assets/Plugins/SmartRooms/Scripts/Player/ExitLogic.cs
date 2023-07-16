using System;
using UnityEngine;

namespace SmartRooms.Player
{
    public class ExitLogic: MonoBehaviour
    {
        public event Action<GameObject> PlayerExited = delegate {  };

        private void OnTriggerEnter2D(Collider2D other)
        {
            bool isPlayer = other.CompareTag("Player");
            
            if (isPlayer)
            {
                PlayerExited(other.gameObject);
            }
        }
    }
}