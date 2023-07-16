using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmartRooms.Generator.Patterns
{
    [Serializable]
    public class SpawningPattern
    {
        /// <summary>
        /// The enumeration for matching Neighbors when matching Rule Tiles
        /// </summary>
        public class Neighbor
        {
            /// <summary>
            /// The Rule Tile will check if the contents of the cell in that direction is an instance of this Rule Tile.
            /// If not, the rule will fail.
            /// </summary>
            public const int Tile = 1;
            /// <summary>
            /// The Rule Tile will check if the contents of the cell in that direction is not an instance of this Rule Tile.
            /// If it is, the rule will fail.
            /// </summary>
            public const int Air = 2;
        }
        
        public virtual Type m_NeighborType => typeof(Neighbor);
        /// <summary>
        /// The transform matching Rule for this Rule.
        /// </summary>
        public Transform RuleTransform;
        
        /// <summary>
        /// The matching Rule conditions for each of its neighboring Tiles.
        /// </summary>
        public List<int> NeighborStates = new List<int>();
        
        /// <summary>
        /// * Preset this list to RuleTile backward compatible, but not support for HexagonalRuleTile backward compatible.
        /// </summary>
        public List<Vector3Int> NeighborPositions = new List<Vector3Int>()
        {
            new Vector3Int(-1, 1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, -1, 0),
        };

        private Dictionary<Vector3Int, int> _neighbors;
        
        public Dictionary<Vector3Int, int> Neighbors {
            get
            {
                if (_neighbors != null)
                {
                    return _neighbors;
                }
                
                _neighbors = GetNeighbors();
                return _neighbors;
            }
        }
        
        /// <summary>
        /// Returns all neighbors of this Tile as a dictionary. Expensive method.
        /// </summary>
        /// <returns>A dictionary of neighbors for this Tile</returns>
        public Dictionary<Vector3Int, int> GetNeighbors()
        {
            Dictionary<Vector3Int, int> dict = new Dictionary<Vector3Int, int>();

            for (int i = 0; i < NeighborStates.Count && i < NeighborPositions.Count; i++)
                dict.Add(NeighborPositions[i], NeighborStates[i]);

            return dict;
        }

        public void Clear()
        {
            NeighborPositions = new List<Vector3Int>();
            NeighborStates = new List<int>();
            RuleTransform = 0;
        }

        /// <summary>
        /// Applies the values from the given dictionary as this Tile's neighbors
        /// </summary>
        /// <param name="dict">Dictionary to apply values from</param>
        public void ApplyNeighbors(Dictionary<Vector3Int, int> dict)
        {
            NeighborPositions = dict.Keys.ToList();
            NeighborStates = dict.Values.ToList();
        }

        private BoundsInt? _bounds;
        
        public BoundsInt Bounds {
            get
            {
                if (_bounds.HasValue)
                {
                    return _bounds.Value;
                }
                
                _bounds = GetBounds();
                return _bounds.Value;
            }
            set => _bounds = value;
        }

        public void Initialize()
        {
            _bounds = null;
            _neighbors = null;
        }

        /// <summary>
        /// Gets the cell bounds of the TilingRule. Expensive method.
        /// </summary>
        /// <returns>Returns the cell bounds of the TilingRule.</returns>
        public BoundsInt GetBounds()
        {
            BoundsInt bounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);
            foreach (var neighbor in GetNeighbors())
            {
                bounds.xMin = Mathf.Min(bounds.xMin, neighbor.Key.x);
                bounds.yMin = Mathf.Min(bounds.yMin, neighbor.Key.y);
                bounds.xMax = Mathf.Max(bounds.xMax, neighbor.Key.x + 1);
                bounds.yMax = Mathf.Max(bounds.yMax, neighbor.Key.y + 1);
            }
            return bounds;
        }
        
        /// <summary>
        /// The enumeration for the transform rule used when matching Rule Tiles.
        /// </summary>
        public enum Transform
        {
            /// <summary>
            /// The Rule Tile will match Tiles exactly as laid out in its neighbors.
            /// </summary>
            Fixed,
            // /// <summary>
            // /// The Rule Tile will rotate and match its neighbors.
            // /// </summary>
            // Rotated,
            /// <summary>
            /// The Rule Tile will mirror in the X axis and match its neighbors.
            /// </summary>
            MirrorX,
            /// <summary>
            /// The Rule Tile will mirror in the Y axis and match its neighbors.
            /// </summary>
            MirrorY,
            /// <summary>
            /// The Rule Tile will mirror in the X or Y axis and match its neighbors.
            /// </summary>
            MirrorXY
        }    
    }
}