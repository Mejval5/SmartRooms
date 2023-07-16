using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SmartRooms.Palette
{
    /// <summary>
    /// A tile which can contain a game object and has proper flags selected.
    /// </summary>
    [ExecuteAlways]
    [CreateAssetMenu(menuName = "SmartRooms/SmartTile", fileName = "SmartTile")]
    public class SmartTile : Tile
    {
        // Cache this field, but do not show it
        [HideInInspector] [SerializeField] private bool _initialized;

        [SerializeField] private bool _showSpriteAtRuntime = false;

        protected virtual void Reset()
        {
            TryToInitialize();
        }
        
        /// <summary>
        /// Gets the tile data for this tile. Randomizes which game object it returns.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="tilemap"></param>
        /// <param name="tileData"></param>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);

            if (Application.isPlaying && _showSpriteAtRuntime == false)
            {
                tileData.sprite = null;
            }
        }

        public void TryToInitialize()
        {
            if (_initialized)
            {
                return;
            }
#if UNITY_EDITOR
            EditorApplication.delayCall -= FirstInitialize;
            EditorApplication.delayCall += FirstInitialize;
#endif
        }

        protected void OnValidate()
        {
            if (sprite != null)
            {
                return;
            }
            
            if (gameObject == null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            sprite = spriteRenderer.sprite;
        }

        private void FirstInitialize()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= FirstInitialize;
#endif
            
            flags = TileFlags.InstantiateGameObjectRuntimeOnly | TileFlags.LockColor;
            colliderType = ColliderType.None;
            
            _initialized = true;
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(SmartTile))]
    public class SmartTileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SmartTile smartTile = (SmartTile)target;

            smartTile.TryToInitialize();
        }
    }
#endif
}
