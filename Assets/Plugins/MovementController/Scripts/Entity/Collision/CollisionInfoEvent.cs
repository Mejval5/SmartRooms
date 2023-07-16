using System;
using UnityEngine.Events;

namespace MovementController
{
    [Serializable]
    public class CollisionInfoEvent : UnityEvent<CollisionInfo>
    {
    }
}