using SmartRooms.Rooms;
using SmartRooms.Utils;

namespace SmartRooms.Generator.Tiles
{
    /// <summary>
    /// A tile that has a special room attached to it.
    /// </summary>
    public class SpecialTile: GenTile
    {
        public SpecialRoom.SpecialRoomData SpecialRoomData { get; }

        public SpecialTile(SpecialRoom.SpecialRoomData specialRoomData) : base(specialRoomData.structureData)
        {
            if (specialRoomData.hasChildStructure == false)
            {
                SpecialRoomData = specialRoomData;
                return;
            }

            if (structureData.scaleModifier.x < 0)
            {
                specialRoomData.childStructure.layout = StructureUtils.FlipStructureHorizontally(specialRoomData.childStructure);
                specialRoomData.childStructure.scaleModifier.x = -1;
                
                if (specialRoomData.childStructureDirection is Direction.Left or Direction.Right)
                {
                    specialRoomData.childStructureDirection = specialRoomData.childStructureDirection.GetOpposite();
                }
            }
            
            if (structureData.scaleModifier.y < 0)
            {
                specialRoomData.childStructure.layout = StructureUtils.FlipStructureVertically(specialRoomData.childStructure);
                specialRoomData.childStructure.scaleModifier.y = -1;

                if (specialRoomData.childStructureDirection is Direction.Up or Direction.Down)
                {
                    specialRoomData.childStructureDirection = specialRoomData.childStructureDirection.GetOpposite();
                }
            }

            _enabledSides[specialRoomData.childStructureDirection] = false;
            
            SpecialRoomData = specialRoomData;
        }
    }
}