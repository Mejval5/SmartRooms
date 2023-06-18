using System;
using System.Collections.Generic;
using SmartRooms.Generator;
using SmartRooms.Generator.Patterns;
using SmartRooms.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SmartRooms.Levels
{
    /// <summary>
    /// Object which holds information about level style, size, rooms it contains, and its build mode.
    /// </summary>
    [CreateAssetMenu(menuName = "SmartRooms/LevelStyle")]
    public class LevelStyle : ScriptableObject
    {
        /// <summary>
        /// Build mode enum
        /// </summary>
        public enum BuildMode
        {
            TopToBottom,
            BottomUp,
            LeftToRight,
            RightToLeft
        }

        [field: SerializeField] public TileBase DefaultGroundTile { get; private set; }
        [field: SerializeField] public Sprite BackgroundSprite { get; private set; }
        [field: SerializeField] public Color BackgroundColor { get; private set; } = new (0.15f,0.15f,0.15f,1f);
        [field: SerializeField] public TileBase SurroundingTile { get; private set; }
        [field: SerializeField] public Material SurroundingMaterial { get; private set; }
        [field: SerializeField] public List<ObjectPattern> FoliagePatterns { get; private set; } 
        [field: SerializeField] public Vector2Int LevelTileSize { get; private set; } = new (4, 4);
        [field: SerializeField] public BuildMode BuildModeDirection { get; private set; } = BuildMode.TopToBottom;
        [field: SerializeField] public List<SmartRoom> Rooms { get; private set; }
        [field: SerializeField] public List<SmartRoom> StartRooms { get; private set; }
        [field: SerializeField] public List<SmartRoom> EndRooms { get; private set; }
        [field: SerializeField] public List<SpecialRoomConfig> SpecialRooms { get; private set; } 
    }

    /// <summary>
    /// Struct which holds information about special room
    /// </summary>
    [Serializable]
    public struct SpecialRoomConfig
    {
        public string specialRoomGroupID;
        public bool mandatory;
        public float spawnChance;
        public SpecialRoom room;
    }
}