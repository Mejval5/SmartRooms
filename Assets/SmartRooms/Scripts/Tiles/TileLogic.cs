using UnityEditor;
using UnityEngine;

namespace SmartRooms.Tiles
{
    /// <summary>
    /// Internal logic of a tile.
    /// </summary>
    [RequireComponent(typeof(TileStyleLogic))]
    public class TileLogic : MonoBehaviour
    {
        // Internals
        private TileStyleLogic _tileStyleLogic;

        private void InitTileStyleLogic()
        {
            if (_tileStyleLogic == null)
            {
                _tileStyleLogic = GetComponent<TileStyleLogic>();
            }
        }
        
        protected void Start()
        {
            InitTileStyleLogic();
        }
    }
        
#if UNITY_EDITOR
    [CustomEditor(typeof(TileLogic), true)]
    [CanEditMultipleObjects]
    internal class TileLogicEditor: UnityEditor.Editor
    {
    }
#endif
}