using System.Linq;
using SmartRooms.Editor;
using SmartRooms.Palette;
using SmartRooms.Utils;
using UnityEditor;
using UnityEngine;

namespace SmartRooms.Rooms
{
    /// <summary>
    /// This represents a room which can be inserted into the generated layout after main generation occurs.
    /// It can be composed of two rooms if it contains a child room.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Structure))]
    [HideScriptField]
    public class SpecialRoom : MonoBehaviour
    {    
        public struct SpecialRoomData
        {
            public Structure.StructureData structureData;
            public bool hasChildStructure;
            public Direction childStructureDirection;
            public Structure.StructureData childStructure;
            public bool mandatory;
            public float spawnChance;
        }
        
        // Cache structure, but don't show it in inspector
        [HideInInspector] [SerializeField] private Structure _structure;
        
        [SerializeField] private Direction _childSpecialRoomDirection = Direction.Right;
        [SerializeField] private SpecialRoom _childSpecialRoom;

        // Properties
        private Structure Structure => _structure;
        
        /// <summary>
        /// Return true if the special room has no parent with special room component.
        /// </summary>
        private bool IsRoot
        {
            get
            {
                return GetComponentsInParent<SpecialRoom>().All(parentSpecialRoom => parentSpecialRoom.gameObject.Equals(gameObject));
            }
        }
        
        /// <summary>
        /// Returns direct parent's structure
        /// </summary>
        private Structure GetParentStructure => transform.parent.GetComponent<Structure>();

        /// <summary>
        /// Creates special room data. Updates it with the provided variables and returns it.
        /// </summary>
        /// <param name="mandatory"></param>
        /// <param name="id"></param>
        /// <param name="spawnChance"></param>
        /// <returns></returns>
        public SpecialRoomData GetSpecialRoomData(bool mandatory = false, string id = null, float spawnChance = 100f)
        {
            SpecialRoomData specialRoomData = new ()
            {
                structureData = _structure.GetStructureData(),
                mandatory = mandatory,
                spawnChance = spawnChance,
                childStructureDirection = _childSpecialRoomDirection,
                hasChildStructure = _childSpecialRoom != null,
            };
            
            if (_childSpecialRoom != null)
            {
                specialRoomData.childStructure = _childSpecialRoom.Structure.GetStructureData();
            }
            
            if (string.IsNullOrEmpty(id) == false)
            {
                specialRoomData.structureData.id = id;
            }

            return specialRoomData;
        }
        
#if UNITY_EDITOR
        
        protected void OnEnable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        protected void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        private void OnEditorUpdate()
        {
            if (this == null)
            {
                EditorApplication.update -= OnEditorUpdate;
                return;
            }

            // Make sure we still have the correct structure after script compilation
            GetStructure();

            // Move the special room to its place
            OffsetChildSpecialRoom();
        }

        /// <summary>
        /// Moves the child special room to the side it belongs to.
        /// </summary>
        private void OffsetChildSpecialRoom()
        {
            if (_childSpecialRoom == null)
            {
                return;
            }

            _childSpecialRoom.Structure.Offset = _childSpecialRoomDirection.ToVector2Int() * _structure.Size;
        }
        
        protected void Awake()
        {
            GetStructure();
        }
        
        protected void OnValidate()
        {
            // Make sure we still have the correct structure after script compilation
            GetStructure();
        }

        private void GetStructure()
        {
            _structure = GetComponent<Structure>();
        }

        protected void OnDrawGizmos()
        {
            if (IsRoot == false && IsSelected())
            {
                // Tell parent to not draw its border while this is selected
                GetParentStructure.SkipDrawingBorderForFrame = true;
            }
        }

        /// <summary>
        /// Return true of the game object is selected or any of its children is selected
        /// </summary>
        /// <returns></returns>
        private bool IsSelected()
        {
            if (Selection.Contains(gameObject))
            {
                return true;
            }
            
            foreach (Transform childTransform in GetComponentInChildren<Transform>())
            {
                if (Selection.Contains(childTransform.gameObject))
                {
                    return true;
                }
            }
            
            return false;
        }
#endif
    }
    
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpecialRoom), true)]
    public class SpecialRoomEditor : UnityEditor.Editor
    {
    }
    
#endif
}