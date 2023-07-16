using UnityEngine;

namespace MovementController.Utils
{
    public static class MovementUtils
    {
        public const int TileWidth = 1;
        public const int TileHeight = 1;

        /// <summary>
        /// Reset a transform the same way you can in the inspector.
        /// </summary>
        /// <param name="transform"></param>
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(1, 1, 1);
        }


        public static Vector3 GetPositionOfCenterOfNearestTile(Vector3 position)
        {
            int x = Mathf.FloorToInt(Mathf.Abs(position.x) / TileWidth) * TileWidth + Mathf.RoundToInt(TileWidth / 2f);
            int y = Mathf.FloorToInt(Mathf.Abs(position.y) / TileHeight) * TileHeight + Mathf.RoundToInt(TileHeight / 2f);
            return new Vector3(x, y, 0);
        }

        public static Vector3 GetPositionOfLowerLeftOfNearestTile(Vector3 position)
        {
            int x = Mathf.FloorToInt(Mathf.Abs(position.x) / TileWidth) * TileWidth;
            int y = Mathf.FloorToInt(Mathf.Abs(position.y) / TileHeight) * TileHeight;
            return new Vector3(x, y, 0);
        }

        /// <summary>
        /// Remaps a value from a current minimum a1 and maximum a2 to a new minimum b1 and maximum b2.
        ///
        /// Let's say you have a value going from 5 to 20 and you want it to go from 0 to 1 so you can use the value in
        /// a slider or whatnot, or the reverse of that. This method handles such use cases for you.
        /// </summary>
        /// <param name="value">The value to remap.</param>
        /// <param name="a1">The current minimum</param>
        /// <param name="a2">The current maximum</param>
        /// <param name="b1">The new minimum</param>
        /// <param name="b2">The new maximum</param>
        /// <returns></returns>
        public static float Remap(this float value, float a1, float a2, float b1, float b2)
        {
            return b1 + (value - a1) * (b2 - b1) / (a2 - a1);
        }
    }
}