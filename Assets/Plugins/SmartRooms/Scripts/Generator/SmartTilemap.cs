using UnityEngine;
using UnityEngine.Tilemaps;

namespace SmartRooms.Generator
{
    /// <summary>
    /// Component attached to a tilemap which makes sure the smart tiles work as intended.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]
    public class SmartTilemap : MonoBehaviour
    {
        private Tilemap _tilemap;
        private TilemapRenderer _tilemapRenderer;
        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
            _tilemapRenderer = GetComponent<TilemapRenderer>();
        }
    }
}