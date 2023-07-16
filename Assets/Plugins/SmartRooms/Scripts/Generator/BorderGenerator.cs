using System.Collections.Generic;
using SmartRooms.Levels;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace SmartRooms.Generator
{
    public class BorderGenerator: MonoBehaviour
    {
        [SerializeField] private int _floorAndCeilingThickness = 1;
        [SerializeField] private int _borderThickness = 20;
        [SerializeField] private float _borderOffset = 0.5f;
        
        [SerializeField] private SpriteRenderer _leftBorder;
        [SerializeField] private SpriteRenderer _rightBorder;
        [SerializeField] private SpriteRenderer _topBorder;
        [SerializeField] private SpriteRenderer _bottomBorder;

        // Internals
        private Tilemap _tilemap;
        private LevelStyle _levelStyle;

        public void UpdateSettings(Tilemap tilemap, LevelStyle levelStyle)
        {
            _tilemap = tilemap;
            _levelStyle = levelStyle;
        }
        
        /// <summary>
        /// Creates border around a tilemap given bounds.
        /// <see cref="UpdateSettings"/> must be called before this method.
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="maxX"></param>
        /// <param name="minY"></param>
        /// <param name="maxY"></param>
        public void GenerateBorder(int minX, int maxX, int minY, int maxY)
        {
            if (_tilemap == null || _levelStyle == null)
            {
                Debug.LogError("UpdateSettings() must be called before GenerateBorder()");
                return;
            }

            // Adjust for ground tile padding thickness
            minY -= _floorAndCeilingThickness;
            maxY += _floorAndCeilingThickness;

            GenerateFloorAndCeiling(minX, maxX, minY, maxY);

            GenerateTilemapBorder(minX, maxX, minY, maxY);
            
            PositionBorders(minX, maxX, minY, maxY);

            UpdateBorderMaterial();
        }

        private void GenerateFloorAndCeiling(int minX, int maxX, int minY, int maxY)
        {
            BoundsInt topTilemapBounds = new(minX , maxY - _floorAndCeilingThickness, 0, maxX - minX, 1, 1);
            List<TileBase> topTiles = new();
            foreach (Vector3 _ in topTilemapBounds.allPositionsWithin)
            {
                topTiles.Add(_levelStyle.DefaultGroundTile);
            }

            _tilemap.SetTilesBlock(topTilemapBounds, topTiles.ToArray());

            BoundsInt bottomTilemapBounds = new(minX, minY - 1 + _floorAndCeilingThickness, 0, maxX - minX, 1, 1);
            List<TileBase> bottomTiles = new();
            foreach (Vector3 _ in bottomTilemapBounds.allPositionsWithin)
            {
                bottomTiles.Add(_levelStyle.DefaultGroundTile);
            }

            _tilemap.SetTilesBlock(bottomTilemapBounds, bottomTiles.ToArray());
        }
        
        private void UpdateBorderMaterial()
        {
            _leftBorder.material = _levelStyle.SurroundingMaterial;
            _rightBorder.material = _levelStyle.SurroundingMaterial;
            _topBorder.material = _levelStyle.SurroundingMaterial;
            _bottomBorder.material = _levelStyle.SurroundingMaterial;
        }

        private void PositionBorders(int minX, int maxX, int minY, int maxY)
        {
            float borderOffset = _borderThickness / 2f + _borderOffset;
            
            _leftBorder.transform.localScale = new Vector2(_borderThickness, maxY - minY + _borderThickness * 2);
            _leftBorder.transform.position = new Vector3(minX - borderOffset, (maxY + minY) / 2f, 0);

            _rightBorder.transform.localScale = new Vector2(_borderThickness, maxY - minY + _borderThickness * 2);
            _rightBorder.transform.position = new Vector3(maxX + borderOffset, (maxY + minY) / 2f, 0);

            _topBorder.transform.localScale = new Vector2(maxX - minX + _borderOffset * 2, _borderThickness - _borderOffset);
            _topBorder.transform.position = new Vector3((maxX + minX) / 2f, maxY + borderOffset - _borderOffset / 2f, 0);

            _bottomBorder.transform.localScale = new Vector2(maxX - minX + _borderOffset * 2, _borderThickness - _borderOffset);
            _bottomBorder.transform.position = new Vector3((maxX + minX) / 2f, minY - borderOffset + _borderOffset / 2f, 0);
        }

        private void GenerateTilemapBorder(int minX, int maxX, int minY, int maxY)
        {
            BoundsInt leftTilemapBounds = new(minX - 1, minY, 0, 1, maxY - minY, 1);
            List<TileBase> leftTiles = new();
            foreach (Vector3 _ in leftTilemapBounds.allPositionsWithin)
            {
                leftTiles.Add(_levelStyle.SurroundingTile);
            }

            _tilemap.SetTilesBlock(leftTilemapBounds, leftTiles.ToArray());

            BoundsInt rightTilemapBounds = new(maxX, minY, 0, 1, maxY - minY, 1);
            List<TileBase> rightTiles = new();
            foreach (Vector3 _ in rightTilemapBounds.allPositionsWithin)
            {
                rightTiles.Add(_levelStyle.SurroundingTile);
            }

            _tilemap.SetTilesBlock(rightTilemapBounds, rightTiles.ToArray());

            BoundsInt topTilemapBounds = new(minX - 1, maxY, 0, maxX - minX + 2, 1, 1);
            List<TileBase> topTiles = new();
            foreach (Vector3 _ in topTilemapBounds.allPositionsWithin)
            {
                topTiles.Add(_levelStyle.SurroundingTile);
            }

            _tilemap.SetTilesBlock(topTilemapBounds, topTiles.ToArray());

            BoundsInt bottomTilemapBounds = new(minX - 1, minY - 1, 0, maxX - minX + 2, 1, 1);
            List<TileBase> bottomTiles = new();
            foreach (Vector3 _ in bottomTilemapBounds.allPositionsWithin)
            {
                bottomTiles.Add(_levelStyle.SurroundingTile);
            }

            _tilemap.SetTilesBlock(bottomTilemapBounds, bottomTiles.ToArray());
        }
    }
}