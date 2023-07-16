using UnityEngine;

namespace MovementController.Level
{
    public class Exit : MonoBehaviour
    {
        public GameObject buttonPromptObject;

        private void Awake()
        {
            buttonPromptObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Player.Player player = other.GetComponent<Player.Player>();
            if (player != null)
            {
                buttonPromptObject.SetActive(true);
                player.EnteredDoorway(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Player.Player player = other.GetComponent<Player.Player>();
            if (player != null)
            {
                buttonPromptObject.SetActive(false);
                player.ExitedDoorway(this);
            }
        }
    }
}