using System;
using System.Collections.Generic;
using SmartRooms.Palette;
using SmartRooms.Utils;
using UnityEngine.Tilemaps;

namespace SmartRooms.Generator.Tiles
{
    /// <summary>
    /// A tile that can be used in the level generator.
    /// </summary>
    public class GenTile
    {
        public Structure.StructureData structureData { get; }
        
        public Dictionary<Direction, List<RoomTile>> roomTiles = new();
        public Dictionary<Direction, List<RoomTile>> endRoomTiles = new();
        public Dictionary<Direction, List<SpecialTile>> specialRoomTiles = new();

        public readonly Dictionary<Direction, int> freeTilesSide = new();
        protected readonly Dictionary<Direction, bool> _enabledSides = new()
        {
            {Direction.Right, true},
            {Direction.Up, true},
            {Direction.Left, true},
            {Direction.Down, true}
        };

        public GenTile(Structure.StructureData structureData)
        {
            this.structureData = structureData;
        }

        public void GenerateFreeTilesSide()
        {
            IEnumerable<Direction> allDirs = DirectionUtils.GetAllDirs;

            foreach (Direction side in allDirs)
            {
                freeTilesSide[side] = 0;
                
                if (_enabledSides[side] == false)
                {
                    continue;
                }
                
                switch (side)
                {
                    case Direction.Right:
                        for (int y = 0; y < structureData.size.y - 1; y++)
                        {
                            int pos = structureData.size.x - 1 + y * structureData.size.x;
                            TileBase firstTile = structureData.layout[pos];
                            int firstTileEmpty = Convert.ToInt32(firstTile == null);
                            freeTilesSide[side] |= firstTileEmpty << y;
                        }

                        break;
                    case Direction.Up:
                        for (int x = 0; x < structureData.size.x - 1; x++)
                        {
                            int pos = x + structureData.size.x * (structureData.size.y - 1);
                            TileBase firstTile = structureData.layout[pos];
                            int firstTileEmpty = Convert.ToInt32(firstTile == null);
                            freeTilesSide[side] |= firstTileEmpty << x;
                        }

                        break;
                    case Direction.Left:
                        for (int y = 0; y < structureData.size.y - 1; y++)
                        {
                            int pos = y * structureData.size.x;
                            TileBase firstTile = structureData.layout[pos];
                            int firstTileEmpty = Convert.ToInt32(firstTile == null);
                            freeTilesSide[side] |= firstTileEmpty << y;
                        }
                        break;
                    case Direction.Down:
                        for (int x = 0; x < structureData.size.x - 1; x++)
                        {
                            TileBase firstTile = structureData.layout[x];
                            int firstTileEmpty = Convert.ToInt32(firstTile == null);
                            freeTilesSide[side] |= firstTileEmpty << x;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}