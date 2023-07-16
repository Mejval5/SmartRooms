using System;
using System.Collections.Generic;
using SmartRooms.Generator.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SmartRooms.Generator
{
    /// <summary>
    /// A class that holds all the data for a level. Used by the SmartRoomGenerator to generate a level.
    /// </summary>
    [Serializable]
    public class LevelLayout
    {
        public TileBase[] levelLayout;

        public GenTile[,] roomTiles;

        public Vector2Int levelTileSize;
        
        public Vector2Int levelSize;
        
        public int[,] spawnedSubStructureIndex;
        
        public List<SpecialTile> spawnedSpecialTiles = new ();

        /// <summary>
        /// Constructor for LevelLayout
        /// </summary>
        /// <param name="levelTileSize"></param>
        /// <param name="levelSize"></param>
        public LevelLayout(Vector2Int levelTileSize, Vector2Int levelSize)
        {
            this.levelTileSize = levelTileSize;
            roomTiles = new GenTile[levelTileSize.x, levelTileSize.y];
            spawnedSubStructureIndex = new int[levelTileSize.x, levelTileSize.y];
            this.levelSize = levelSize;
            levelLayout = new TileBase[this.levelSize.x * this.levelSize.y];
        }
        
        /// <summary>
        /// Returns a tile at a given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TileBase GetTile(int x, int y)
        {
            int pos = x + y * levelSize.x;
            return levelLayout[pos];
        }
    }
}