using SmartRooms.Palette;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SmartRooms.Utils
{
    public static class StructureUtils
    {
        /// <summary>
        /// Flip structure's layout horizontally. Returns only layout.
        /// </summary>
        /// <param name="structureData"></param>
        /// <returns></returns>
        public static TileBase[] FlipStructureHorizontally(Structure.StructureData structureData)
        {
            TileBase[] newTiles = new TileBase[structureData.layout.Length];
            for (int i = 0; i < structureData.layout.Length; i++)
            {
                Vector2Int coords = new(structureData.size.x - 1 - i % structureData.size.x, i / structureData.size.x);
                int pos = coords.x + coords.y * structureData.size.x;
                newTiles[pos] = structureData.layout[i];
            }

            return newTiles;
        }

        /// <summary>
        /// Flip structure's layout vertically. Returns only layout.
        /// </summary>
        /// <param name="structureData"></param>
        /// <returns></returns>
        public static TileBase[] FlipStructureVertically(Structure.StructureData structureData)
        {
            TileBase[] newTiles = new TileBase[structureData.layout.Length];
            for (int i = 0; i < structureData.layout.Length; i++)
            {
                Vector2Int coords = new(i % structureData.size.x, structureData.size.y - 1 - i / structureData.size.x);
                int pos = coords.x + coords.y * structureData.size.x;
                newTiles[pos] = structureData.layout[i];
            }

            return newTiles;
        }
    }
}