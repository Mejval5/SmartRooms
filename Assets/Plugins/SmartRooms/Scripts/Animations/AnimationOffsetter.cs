using UnityEngine;

namespace SmartRooms.Animations
{
    [RequireComponent(typeof(Animator))]
    public class AnimationOffsetter : MonoBehaviour
    {
        void OnEnable()
        {
            float randomOffset = Random.Range(0f, 1f);
            GetComponent<Animator>().SetFloat("Offset", randomOffset);
        }
    }
}

