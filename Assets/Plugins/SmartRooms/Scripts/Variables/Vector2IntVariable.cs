using UnityEngine;

namespace SmartRooms.Variables
{
    [CreateAssetMenu(menuName = "SmartRooms/Vector2IntVariable")]
    public class Vector2IntVariable : ScriptableObject
    {
#if UNITY_EDITOR
#pragma warning disable 414
        [Multiline] [SerializeField] private string _developerDescription = "";
#pragma warning restore 414
#endif

        [field: SerializeField] public Vector2Int Value { get; private set; } = new();
    }
}