using SmartRooms.Levels;
using UnityEngine;

namespace SmartRooms.Generator
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundGenerator : MonoBehaviour
    {
        [SerializeField] private Vector2Int _padding;

        private SpriteRenderer BackgroundSpriteRenderer => _backgroundSpriteRenderer == null ? _backgroundSpriteRenderer = GetComponent<SpriteRenderer>() : _backgroundSpriteRenderer;

        // Internals
        private SpriteRenderer _backgroundSpriteRenderer;
        private LevelStyle _levelStyle;
        
        public void GenerateBackground(LevelStyle levelStyle, Vector2Int tilemapSize)
        {
            _levelStyle = levelStyle;
            
            BackgroundSpriteRenderer.sprite = _levelStyle.BackgroundSprite;
            BackgroundSpriteRenderer.size = new Vector2(tilemapSize.x + _padding.x * 2, tilemapSize.y + _padding.y * 2);
            BackgroundSpriteRenderer.color = _levelStyle.BackgroundColor;

            Vector2 size = BackgroundSpriteRenderer.size;
            BackgroundSpriteRenderer.transform.position = new Vector3(size.x / 2f - _padding.x, size.y / 2f - _padding.y, 0);
        }
    }
}

