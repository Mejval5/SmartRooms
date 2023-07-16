using System.Collections.Generic;
using UnityEngine;

namespace SmartRooms.Utils
{
    /// <summary>
    /// Direction enum
    /// </summary>
    public enum Direction
    {
        Right,
        Up,
        Left,
        Down
    }
    
    /// <summary>
    /// Helper class for Direction enum
    /// </summary>
    public static class DirectionUtils
    {
        public static Direction GetOpposite(this Direction side)
        {
            return side switch
            {
                Direction.Right => Direction.Left,
                Direction.Up => Direction.Down,
                Direction.Left => Direction.Right,
                Direction.Down => Direction.Up,
                _ => Direction.Right
            };
        }

        public static Vector2Int ToVector2Int(this Direction dir)
        {
            return dir switch
            {
                Direction.Right => new Vector2Int(1, 0),
                Direction.Up => new Vector2Int(0, 1),
                Direction.Left => new Vector2Int(-1, 0),
                Direction.Down => new Vector2Int(0, -1),
                _ => new Vector2Int(1, 0)
            };
        }
        
        public static IEnumerable<Direction> GetAllDirs => new [] { Direction.Right, Direction.Up, Direction.Left, Direction.Down };
    }
}