using SmartRooms.Rooms;
using UnityEngine;

namespace SmartRooms.Generator.Tiles
{
    /// <summary>
    /// A tile that has a room attached to it.
    /// </summary>
    public class RoomTile: GenTile
    {
        public enum RoomTileType
        {
            Default,
            StartRoom,
            EndRoom
        }
        
        public SmartRoom.RoomData roomData;
        public RoomTileType Type = RoomTileType.Default;

        public Vector2Int GetEntrance => roomData.entrancePos;

        public RoomTile(SmartRoom.RoomData roomData) : base(roomData.structureData)
        {
            this.roomData = roomData;
        }
    }
}