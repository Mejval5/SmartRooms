using SmartRooms.Editor;
using SmartRooms.Palette;
using UnityEditor;
using UnityEngine;

namespace SmartRooms.Rooms
{
    /// <summary>
    /// This represents a room which is used for generating the level layout.
    /// It has an entrance by default.
    /// </summary>
#if UNITY_EDITOR
    [HideScriptField]
#endif
    [ExecuteAlways]
    [RequireComponent(typeof(Structure))]
    public class SmartRoom : MonoBehaviour
    {
        public struct RoomData
        {
            public Structure.StructureData structureData;
            public Vector2Int entrancePos;
        }
        
        // ID for the entrance handle
        private const int HandleControlID = 9;

        [Header("Entrance")]
        [SerializeField] private Vector2Int _entrancePosition = - Vector2Int.one;

        [Header("Entrance visual indicator")]
        [SerializeField] private bool _showEntranceVisual = true;
        [SerializeField] private Color _entranceVisualColor = new(0f, 1f, 0f, 0.5f);
        [SerializeField] private float _entranceVisualSize = 1f;
        
        [Header("Entrance handle")]
        [SerializeField] private bool _showEntranceHandle = true;
        [SerializeField] private Color _entranceHandleColor = new(0.22745098f, 0.47843137f, 0.972549f, 0.93f);
        [SerializeField] private float _entranceHandleSize = 1f;

        // Entrance handle variables
        private Vector2Int _startHandlePosition;
        private Vector2 _startMousePosition;
        private Vector2 _currentMousePosition;
        
        // Structure
        [HideInInspector] [SerializeField] private Structure _structure;

        /// <summary>
        /// Creates room data and returns it.
        /// </summary>
        /// <returns></returns>
        public RoomData GetRoomData()
        {
            RoomData roomData = new ()
            {
                entrancePos = _entrancePosition,
                structureData = _structure.GetStructureData(),
            };

            return roomData;
        }
        
#if UNITY_EDITOR
        protected void Awake()
        {
            GetStructure();
        }

        private void GetStructure()
        {
            _structure = GetComponent<Structure>();
        }

        protected void OnValidate()
        {
            GetStructure();
            
            // If entrance position is in the default position then we 
            if (_entrancePosition == - Vector2Int.one && _structure.Size.magnitude == 0)
            {
                // Can't calculate the default entrance position
                if (_structure.Size.magnitude == 0)
                {
                    return;
                }
                
                int x = Mathf.CeilToInt(_structure.Size.x / 2f);
                int y = Mathf.CeilToInt(_structure.Size.y / 2f);
                _entrancePosition = new Vector2Int(x, y);
            }

            // Clamp the entrance position to the size of the room
            _entrancePosition.x = Mathf.Clamp(_entrancePosition.x, 0, _structure.Size.x - 1);
            _entrancePosition.y = Mathf.Clamp(_entrancePosition.y, 0, _structure.Size.y - 1);
        }

        protected void OnDrawGizmos()
        {
            DrawEntranceGizmo();
        }
        
        /// <summary>
        /// Draws visual gizmo which represents the entrance to the room.
        /// </summary>
        private void DrawEntranceGizmo()
        {
            if (_showEntranceVisual == false)
            {
                return;
            }

            Gizmos.color = _entranceVisualColor;
            Gizmos.DrawSphere(_entrancePosition - (Vector2)_structure.Size / 2f + new Vector2(0.5f, 1f), _entranceVisualSize / 2f);
        }

        /// <summary>
        /// Moves the entrance handle based on the input of the user respecting the room bounds.
        /// </summary>
        public void ProcessEntranceHandleInput()
        {
            if (_showEntranceHandle == false)
            {
                return;
            }
            
            // Creates a block of code which will detect user input in the GUI
            EditorGUI.BeginChangeCheck();
            Handles.color = _entranceHandleColor;

            // Creates a free move handle with specified color and position of the entrance. Handles input by returning new handle position
            Vector3 newHandlePosition = Handles.FreeMoveHandle(
                HandleControlID, 
                _entrancePosition - (Vector2)_structure.Size / 2f + new Vector2(0.5f, 1f), 
                _entranceHandleSize, 
                Vector3.one, 
                Handles.SphereHandleCap
                );

            // False for all GUI repaints and only triggers in the GUI callback which is responsible for processing user input
            if (EditorGUI.EndChangeCheck() == false)
            {
                return;
            }

            // Allows for undoing any change to the entrance position
            Undo.RecordObject(this, "Change Entrance position");
            
            // Calculate new position of the entrance from the handle and clamp it to the room bounds
            Vector3 handlePos = newHandlePosition + (Vector3)((Vector2)_structure.Size / 2f - new Vector2(0.5f, 1f));
            Vector2Int newPos = new (Mathf.RoundToInt(handlePos.x), Mathf.RoundToInt(handlePos.y));
            newPos.x = Mathf.Clamp(newPos.x, 0, _structure.Size.x - 1);
            newPos.y = Mathf.Clamp(newPos.y, 0, _structure.Size.y - 1);
            _entrancePosition = newPos;
        }
#endif
    }
    
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SmartRoom), true)]
    public class SmartRoomEditor : UnityEditor.Editor
    {
        protected void OnSceneGUI()
        {
            SmartRoom smartRoom = (SmartRoom)target;

            // Display entrance handle and handle input
            smartRoom.ProcessEntranceHandleInput();
        }
    }
    
#endif
}