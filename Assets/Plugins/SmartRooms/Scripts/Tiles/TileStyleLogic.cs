using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SmartRooms.Tiles
{
    /// <summary>
    /// Handles style updates of a tile based on its surrounding.
    /// </summary>
    [ExecuteAlways]
    public class TileStyleLogic : MonoBehaviour
    {
        [Header("Borders")]
        [SerializeField] private GameObject _topBorder;
        [SerializeField] private GameObject _bottomBorder;
        [SerializeField] private GameObject _leftBorder;
        [SerializeField] private GameObject _rightBorder;
        
        [Header("Shadow")]
        [SerializeField] private GameObject _rightShadow;
        [SerializeField] private GameObject _bottomShadow;
        [SerializeField] private GameObject _cornerShadow;

        [Header("Guid")]
        [SerializeField] private string _uniqueGuid = Guid.NewGuid().ToString();

        // Internal
        private Tilemap _tilemap;
        private Dictionary<Vector3Int, GameObject> SidesBorder = new();
        private Dictionary<Vector3Int, GameObject> SideShadows = new();
        
        // Events
        private string GUID => _uniqueGuid;
        
        // Properties
        public bool UpdateNeighborsOnDestroy { get; set; } = false;

        private void InitSides()
        {
            SidesBorder = new Dictionary<Vector3Int, GameObject>
            {
                { Vector3Int.up, _topBorder },
                { Vector3Int.down, _bottomBorder },
                { Vector3Int.left, _leftBorder },
                { Vector3Int.right, _rightBorder },
            };
            SideShadows = new Dictionary<Vector3Int, GameObject>
            {
                { Vector3Int.right, _rightShadow },
                { Vector3Int.down, _bottomShadow },
                { Vector3Int.right + Vector3Int.down, _cornerShadow },
            };
        }

        protected void Awake()
        {
            InitSides();
        }

        protected void OnValidate()
        {
            if (string.IsNullOrEmpty(GUID))
            {
                _uniqueGuid = Guid.NewGuid().ToString();
            }
        }

        protected void OnDestroy()
        {
            if (UpdateNeighborsOnDestroy)
            {
                UpdateNeighbors();
            }
        }

        public void UpdateNeighbors()
        {
            EnsureTilemap();

            if (_tilemap == null)
            {
                return;
            }

            // Gets tile position
            Vector3Int pos = _tilemap.WorldToCell(transform.position);
            
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                // Don't update itself
                if (x == 0 && y == 0)
                {
                    continue;
                }
                
                // If there a tile next to us, update it
                GameObject sideFoundGO = _tilemap.GetInstantiatedObject(pos + new Vector3Int(x, y, 0));
                if (sideFoundGO == null)
                {
                    continue;
                }

                TileStyleLogic tileStyleLogic = sideFoundGO.GetComponent<TileStyleLogic>();
                if (tileStyleLogic != null)
                {
                    tileStyleLogic.UpdateTile();
                }
            }
        }

        protected void Start()
        {
            UpdateTile();
        }

        private void DisableStyle()
        {
            foreach (GameObject sideBorder in SidesBorder.Values)
            {
                if (sideBorder == null)
                {
                    continue;
                }
                
                sideBorder.SetActive(false);
            }
            
            foreach (GameObject sideShadow in SideShadows.Values)
            {
                if (sideShadow == null)
                {
                    continue;
                }
                
                sideShadow.SetActive(false);
            }
        }

        private void EnsureTilemap()
        {
            if (_tilemap == null)
            {
                _tilemap = GetComponentInParent<Tilemap>();
            }
        }

        /// <summary>
        /// Updates tile style. The tile will refresh its state based on its surroundings.
        /// </summary>
        public void UpdateTile()
        {
            EnsureTilemap();

            if (_tilemap == null)
            {
                return;
            }

            if (Application.isPlaying == false)
            {
                DisableStyle();
                return;
            }

            // Gets tile position
            Vector3Int pos = _tilemap.WorldToCell(transform.position);

            // Enable side shadows
            foreach (KeyValuePair<Vector3Int, GameObject> sideShadow in SideShadows)
            {
                if (sideShadow.Value == null)
                {
                    continue;
                }
                
                // If there a tile next to us, show shadow at that side
                GameObject sideFoundGO = _tilemap.GetInstantiatedObject(pos + sideShadow.Key);
                if (sideFoundGO == null)
                {
                    sideShadow.Value.SetActive(true);
                    continue;
                }
                
                // No need to show shadow if a tile is there
                sideShadow.Value.SetActive(false);
            }
            
            // Enable corner shadow
            if (_rightShadow != null && _bottomShadow != null && _cornerShadow != null)
            {
                _cornerShadow.SetActive(_rightShadow.activeSelf && _bottomShadow.activeSelf);
            }
            
            // Update each side
            foreach (KeyValuePair<Vector3Int, GameObject> sideBorder in SidesBorder)
            {
                if (sideBorder.Value == null)
                {
                    continue;
                }
                
                // Show side object if tile next to it has no game object
                GameObject sideFoundGO = _tilemap.GetInstantiatedObject(pos + sideBorder.Key);
                if (sideFoundGO == false)
                {
                    sideBorder.Value.SetActive(true);
                    continue;
                }

                // Show side object if tile next to it is not using style logic
                TileStyleLogic sideTileStyle = sideFoundGO.GetComponent<TileStyleLogic>();
                if (sideTileStyle == false)
                {
                    sideBorder.Value.SetActive(true);
                    continue;
                }
                
                // Show side object if tile next to it is not the same GUID
                sideBorder.Value.SetActive(sideTileStyle.GUID != GUID);
            }
        }
    }
            
#if UNITY_EDITOR
    [CustomEditor(typeof(TileStyleLogic), true)]
    [CanEditMultipleObjects]
    internal class TileStyleLogicEditor: UnityEditor.Editor
    {
    }
#endif
}