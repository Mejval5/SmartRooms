using System.Collections.Generic;
using System.Linq;
using SmartRooms.Editor;
using SmartRooms.Rooms;
using SmartRooms.Variables;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SmartRooms.Palette
{
    /// <summary>
    /// This is a structure of tiles and game objects which the level generator can spawn in.
    /// It can contain substructures.
    /// It has an entrance by default.
    /// </summary>
#if UNITY_EDITOR
    [HideScriptField]
#endif
    [ExecuteAlways]
    [RequireComponent(typeof(Grid))]
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]
    public class Structure : MonoBehaviour
    {
        public struct StructureData
        {
            public GameObject prefab;
            public string name;
            public string id;
            public List<Structure> substructures;
            public float substructuresChance;
            public Vector2Int offset;
            public float weight;
            public Vector2 scaleModifier;
            public bool flipChildren;
            public TileBase[] layout;
            public bool verticallyFlippable;
            public bool horizontallyFlippable;
            public Vector2Int size;
        }
        
        // ID for the entrance handle
        private const int HandleControlID = 9;
        
        // Size of the visual border
        private const float StructureBorderVisualSize = 500f;

        [Header("Editing setup")]
        [SerializeField] private bool _drawBorder = true;
        [SerializeField] private Color _borderColor = new (0.902f, 0.659f, 0.753f, 0.35f);
        [SerializeField] private bool _drawPositionHandle = true;
        [SerializeField] private Color _positionHandleColor = new(0.22745098f, 0.47843137f, 0.972549f, 0.93f);
        [SerializeField] private float _positionHandleSize = 1f;
        [SerializeField] private Vector2Int _pos = Vector2Int.zero;

        [Header("Structure Setup")]
        [SerializeField] private bool _verticallyFlippable;

        [SerializeField] private bool _horizontallyFlippable = true;
        [SerializeField] public bool _flipChildrenAsWell = true;

        [SerializeField] private Vector2IntVariable _size;
        [SerializeField] private float _substructuresChance = 50f;
        [SerializeField] private float _weight = 1f;

        [Header("Saved layout")]
        [SerializeField] private TileBase[] _layout;
        
        [Header("Sub structures setup")]
        [SerializeField] private List<Structure> _substructures;

        // Internals
        [HideInInspector] [SerializeField] private Tilemap _tilemap;
        [HideInInspector] [SerializeField] private GameObject _cachedGameObject;
        [HideInInspector] [SerializeField] private string _cachedName;
        [HideInInspector] [SerializeField] private string _cachedId;
        
        // Properties
        private Tilemap Tilemap => _tilemap != null ? _tilemap : _tilemap = GetComponent<Tilemap>();
        public Vector2Int Size => _size != null ? _size.Value : Vector2Int.zero;
        public bool SkipDrawingBorderForFrame { private get; set; }
        public float Weight => _weight;

        public Vector2Int Offset {private get; set; } = Vector2Int.zero;

        /// <summary>
        /// Return true if the structure has no parent with structure component.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return GetComponentsInParent<Structure>().All(parentStructure => parentStructure.gameObject.Equals(gameObject));
            }
        }
                
        /// <summary>
        /// Returns direct parent's structure size.
        /// Returns zero no parent structure exists.
        /// </summary>
        private Vector2Int ParentStructureSize
        {
            get
            {
                Structure structure = transform.parent.GetComponent<Structure>();
                return structure == null ? Vector2Int.zero : structure.Size;
            }
        }
        /// <summary>
        /// Creates structure data and returns it.
        /// </summary>
        /// <returns></returns>
        public StructureData GetStructureData()
        {
            return new StructureData()
            {
                prefab = _cachedGameObject,
                name = _cachedName,
                id = _cachedId,
                substructures = _substructures,
                substructuresChance = _substructuresChance,
                offset = _pos,
                weight = _weight,
                scaleModifier = Vector2.one,
                flipChildren = _flipChildrenAsWell,
                layout = _layout,
                verticallyFlippable = _verticallyFlippable,
                horizontallyFlippable = _horizontallyFlippable,
                size = _size.Value
            };
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
        
        /// <summary>
        /// Locks the structure in place to prevent moving it in the editor.
        /// </summary>
        private void LockTransform()
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            _pos = IsRoot ? Offset : ClampSubstructurePosition(_pos);

            transform.position = new Vector3(_pos.x, _pos.y, 0);
        }

        protected void OnValidate()
        {
            CacheProperties();
        }

        /// <summary>
        /// Caches game object properties which cannot be retrieved in background thread
        /// </summary>
        private void CacheProperties()
        {
            _cachedGameObject = gameObject;
            _cachedName = gameObject.name;
            _cachedId = gameObject.GetInstanceID().ToString();
        }
        
        private void OnEditorUpdate()
        {
            if (this == null)
            {
                EditorApplication.update -= OnEditorUpdate;
                return;
            }

            // Cache game object properties which cannot be retrieved in background thread
            CacheProperties();
            
            // Lock the transform of this game object to a specific location
            LockTransform();

            if (_size == null)
            {
                return;
            }
            
            TileBase[] previousLayout = _layout;

            // Clean up the tilemap and save it
            ClearTilesOutsideStructure();
            SaveCurrentLayout();

            // Cache any substructure
            CacheSubStructures();
            
            // Only mark the game object to be saved when the layout changes
            if (_layout != null && previousLayout != null && previousLayout.SequenceEqual(_layout) == false)
            {
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Finds all structures in children game objects and caches them
        /// </summary>
        private void CacheSubStructures()
        {
            _substructures = new List<Structure>();
            
            foreach (Structure structure in GetComponentsInChildren<Structure>(true))
            {
                if (structure.gameObject.Equals(gameObject))
                {
                    continue;
                }

                if (structure.GetComponent<SpecialRoom>() != null)
                {
                    continue;
                }

                _substructures.Add(structure);
            }
        }

        /// <summary>
        /// Serializes the current tile layout
        /// </summary>
        private void SaveCurrentLayout()
        {
            BoundsInt roomBounds = GetStructureBounds();

            // Initialize the layout
            _layout = new TileBase[roomBounds.size.x * roomBounds.size.y];

            int k = 0;
            for (int y = roomBounds.yMin; y < roomBounds.yMax; y++)
            for (int x = roomBounds.xMin; x < roomBounds.xMax; x++)
            {
                // Save a tile to the layout
                Vector3Int tilePos = new (x, y, 0);
                TileBase tileBase = Tilemap.GetTile(tilePos);
                _layout[k] = tileBase;

                k++;
            }
        }

        /// <summary>
        /// Returns the bounds of the structure
        /// </summary>
        private BoundsInt GetStructureBounds()
        {
            int minX = -Mathf.FloorToInt(_size.Value.x / 2f);
            int minY = -Mathf.FloorToInt(_size.Value.y / 2f);

            BoundsInt roomBounds = new (minX, minY, 0, _size.Value.x, _size.Value.y, 0);
            return roomBounds;
        }

        /// <summary>
        /// Remove all tiles which are outside of the bounds of the structure
        /// </summary>
        private void ClearTilesOutsideStructure()
        {
            BoundsInt structureBounds = GetStructureBounds();
            Tilemap.CompressBounds();

            foreach (Vector3Int position in Tilemap.cellBounds.allPositionsWithin)
            {
                int x = position.x;
                int y = position.y;

                if (x >= structureBounds.xMin && x < structureBounds.xMax && y >= structureBounds.yMin && y < structureBounds.yMax)
                {
                    continue;
                }

                Vector3Int tilePos = new (x, y, 0);
                Tilemap.SetTile(tilePos, null);
            }

            Tilemap.CompressBounds();
        }

        /// <summary>
        /// Draws a visual blocking border around the structure to indicate that you cannot edit tiles outside of it
        /// </summary>
        private void DrawBoundsGizmo()
        {
            if (_drawBorder == false)
            {
                return;
            }

            // Draw a visual border around the structure on all sides
            Gizmos.color = _borderColor;
            BoundsInt structureBounds = GetStructureBounds();
            
            Gizmos.DrawCube(transform.position + (structureBounds.xMin - StructureBorderVisualSize / 2f) * Vector3.right, new Vector3(StructureBorderVisualSize, StructureBorderVisualSize * 2f, 0));
            Gizmos.DrawCube(transform.position + (structureBounds.xMax + StructureBorderVisualSize / 2f) * Vector3.right, new Vector3(StructureBorderVisualSize, StructureBorderVisualSize * 2f, 0));
            
            Vector3 upOffset = (structureBounds.yMax + StructureBorderVisualSize / 2f) * Vector3.up;
            Vector3 downOffset =( structureBounds.yMin - StructureBorderVisualSize / 2f) * Vector3.up;
            Vector3 rightOffset = structureBounds.size.x % 2 / 2f * Vector3.right;
            Gizmos.DrawCube(transform.position + upOffset + rightOffset, new Vector3(structureBounds.size.x, StructureBorderVisualSize, 0));
            Gizmos.DrawCube(transform.position + downOffset + rightOffset, new Vector3(structureBounds.size.x, StructureBorderVisualSize, 0));
        }

        protected void OnDrawGizmos()
        {
            // Don't draw border if another structure wants to draw it
            if (SkipDrawingBorderForFrame)
            {
                SkipDrawingBorderForFrame = false;
                return;
            }
            
            if (IsRoot || IsSelected())
            {
                DrawBoundsGizmo();
            }
        }

        protected void OnDrawGizmosSelected()
        {
            if (IsRoot == false && IsSelected())
            {
                DrawBoundsGizmo();
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
        
        public void ProcessStructureHandleInput()
        {
            if (_drawPositionHandle == false)
            {
                return;
            }
            
            // Creates a block of code which will detect user input in the GUI
            EditorGUI.BeginChangeCheck();
            Handles.color = _positionHandleColor;

            // Creates a free move handle with specified color and position of the structure. Handles input by returning new handle position
            Vector3 position = Handles.FreeMoveHandle(
                HandleControlID, 
                (Vector2)_pos + new Vector2(0.5f, - Size.y / 2f - 0.5f), 
                _positionHandleSize, 
                Vector3.one, 
                Handles.SphereHandleCap
            );

            // False for all GUI repaints and only triggers in the GUI callback which is responsible for processing user input
            if (EditorGUI.EndChangeCheck() == false)
            {
                return;
            }

            // Allows for undoing any change to the entrance position
            Undo.RecordObject(this, "Change structure position");
            
            // Calculate new position of the structure from the handle and clamp it to the room bounds
            Vector3 handlePos = position - new Vector3(0.5f, - Size.y / 2f - 0.5f, 0f);
            Vector2Int newPos = new (Mathf.RoundToInt(handlePos.x), Mathf.RoundToInt(handlePos.y));
            _pos = ClampSubstructurePosition(newPos);
        }

        /// <summary>
        /// Clamps provided position of the structure to the bounds of the parent structure
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2Int ClampSubstructurePosition(Vector2Int position)
        {
            position.x = Mathf.Clamp(position.x, Offset.x - ParentStructureSize.x / 2 + Size.x / 2, Offset.x + ParentStructureSize.x / 2 + ParentStructureSize.x % 2 - Size.x / 2 - Size.x % 2);
            position.y = Mathf.Clamp(position.y, Offset.y - ParentStructureSize.y / 2 + Size.y / 2, Offset.y + ParentStructureSize.y / 2 + ParentStructureSize.y % 2 - Size.y / 2 - Size.y % 2);
            return position;
        }
#endif
    }
    
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Structure))]
    public class StructureEditor : UnityEditor.Editor
    {
        protected void OnSceneGUI()
        {
            Structure structure = (Structure)target;

            // Allow moving a of child structure inside a parent structure
            if (structure.IsRoot == false)
            {
                structure.ProcessStructureHandleInput();
            }
        }
    }
    
#endif
}
