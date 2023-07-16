using System;
using UnityEngine.Events;

namespace MovementController.Collision
{
    [Serializable]
    public class CollisionInfoEvent : UnityEvent<CollisionInfo>
    {
    }
}