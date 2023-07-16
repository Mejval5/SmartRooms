using System.Collections.Generic;
using UnityEngine;

namespace SmartRooms.Utils
{
    /// <summary>
    /// Helper class for Direction enum
    /// </summary>
    public static class VectorUtils
    {
        public static Vector3 ToVector3(this Vector2Int vector2Int)
        {
            return new Vector3(vector2Int.x, vector2Int.y, 0);
        }
        
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }
    }
}