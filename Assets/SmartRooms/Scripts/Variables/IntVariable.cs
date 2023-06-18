using UnityEngine;

namespace SmartRooms.Variables
{
    [CreateAssetMenu(menuName = "SmartRooms/IntVariable")]
    public class IntVariable : ScriptableObject
    {
#if UNITY_EDITOR
#pragma warning disable 414
        [Multiline] [SerializeField] private string _developerDescription = "";
#pragma warning restore 414
#endif

        [field: SerializeField] public int Value { get; private set; }
    }
}