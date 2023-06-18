using System.Collections.Generic;
using UnityEngine;

namespace SmartRooms.Variables
{
    [CreateAssetMenu(menuName = "SmartRooms/SpriteListVariable")]
    public class SpriteListVariable : ScriptableObject
    {
#if UNITY_EDITOR
#pragma warning disable 414
        [Multiline] [SerializeField] private string _developerDescription = "";
#pragma warning restore 414
#endif
        
        [field: SerializeField] public List<Sprite> Value { get; private set; } = new();
    }
}
