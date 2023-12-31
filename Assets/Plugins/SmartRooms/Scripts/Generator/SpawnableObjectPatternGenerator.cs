﻿using System;
using SmartRooms.Levels;
using System.Collections.Generic;
using System.Diagnostics;
using SmartRooms.Generator.Patterns;
using SmartRooms.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

namespace SmartRooms.Generator
{
    [ExecuteAlways]
    public class SpawnableObjectPatternGenerator : MonoBehaviour
    {
        [SerializeField] private SmartLevelGenerator _levelGenerator;
        [SerializeField] private int _seed = -1;

        [SerializeField] private Transform _objectHolder;
        [SerializeField] private Texture2D _perlinTexture;
        [SerializeField] private Vector2 _defaultOffset = new(10, 16);

        [SerializeField] private bool _dynamicDensity;
        [SerializeField] private float _customDensityOverride = 1f;
        
        [Header("Debug features")]
        [SerializeField] private bool _logGenerationTime;

        private LevelStyle _levelStyle;
        private Tilemap _map;
        private Vector2Int _flipAxis;
        private Vector2Int _perlinNoiseOffset;
        private BlockState[,] _cachedMap;
        private Vector2Int _cachedMapSize;
        private Dictionary<SpawnableObject, int> _spawnedItems { get; set; } = new();
        private List<ValidLocation> ValidLocations { get; set; } = new();
        
        private bool _generateNextFrame;
        private Unity.Mathematics.Random _random;

        private enum BlockState
        {
            NULL,
            Air,
            Block
        }

        private struct ValidLocation
        {
            public Vector2Int FlipDir;
            public Vector2Int Position;
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                _objectHolder.DestroyAllChildren();
            }
        }

        protected void OnEnable()
        {
            if (_levelGenerator == null)
            {
                return;
            }
            
            if (Application.isPlaying)
            {
                _objectHolder.DestroyAllChildren();
            }
            
            _levelGenerator.LevelGenerated += GenerateWithLevelGeneratorNextFrame;
        }

        protected void OnDisable()
        {
            if (_levelGenerator == null)
            {
                return;
            }
            
            _levelGenerator.LevelGenerated -= GenerateWithLevelGeneratorNextFrame;
        }

        protected void Update()
        {
            if (_generateNextFrame)
            {
                _generateNextFrame = false;
                GenerateWithLevelGenerator();
            }
        }

        public void GenerateWithLevelGeneratorNextFrame()
        {
            _generateNextFrame = true;
        }

        private void GenerateWithLevelGenerator()
        {
            GenerateForMap(_levelGenerator.TargetTilemap, _levelGenerator.CurrentLevelStyle, _levelGenerator.Seed);
        }

        public void GenerateForMap(Tilemap map, LevelStyle levelStyle, int seed = -1)
        {
            _seed = seed;
            _levelStyle = levelStyle;
            _map = map;
            
            // Initialize the level generation seed.
            _random = _seed == -1 ? new Unity.Mathematics.Random((uint)DateTime.Now.Ticks) : new Unity.Mathematics.Random((uint)_seed);
            
            Generate();
        }

        private void Generate()
        {
            Stopwatch watch = new();
            watch.Restart();

            if (_dynamicDensity)
            {
                CalculateDensityOverride();
            }

            DisablePreviousItems();
            CacheMap();
            GenerateByStyle();
            TryToLogGenerationTime("[SpawnableObjectPatternGenerator] Spawning objects by patterns took: " + watch.ElapsedMilliseconds + "ms");
        }

        private void CacheMap()
        {
            _cachedMap = new BlockState[_map.size.x, _map.size.y];
            _cachedMapSize = new Vector2Int(_map.size.x, _map.size.y);
            
            for (int x = 0; x < _map.size.x; x++)
            for (int y = 0; y < _map.size.y; y++)
            {
                GameObject tile = _map.GetInstantiatedObject(new Vector3Int(x, y, 0));
                _cachedMap[x, y] = tile == null ? BlockState.Air : BlockState.Block;
            }
        }

        /// <summary>
        /// If the log generation time is true, log the message.
        /// </summary>
        /// <param name="message"></param>
        private void TryToLogGenerationTime(object message)
        {
            if (_logGenerationTime)
            {
                Debug.Log(message);
            }
        }

        private void CalculateDensityOverride()
        {
            float xScale = _map.size.x / 10f;
            float yScale = _map.size.y / 16f;
            _customDensityOverride = (xScale + yScale) / 2f;
        }

        private void DisablePreviousItems()
        {
            if (Application.isPlaying)
            {
                foreach (KeyValuePair<SpawnableObject, int> spawnedItem in _spawnedItems)
                {
                    if (spawnedItem.Value > 0)
                    {
                        spawnedItem.Key.ReleaseAllObjects();
                    }
                }
            }
            else
            {
                _objectHolder.DestroyAllChildren();
            }
            
            _spawnedItems = new Dictionary<SpawnableObject, int>();
        }

        private void GenerateByStyle()
        {
            foreach (ObjectPattern objectPattern in _levelStyle.SpawnableObjectPatterns)
            {
                objectPattern.Initialize();
                GenerateByPattern(objectPattern);
            }
        }

        private void GenerateByPattern(ObjectPattern objectPattern)
        {
            ValidLocations = new List<ValidLocation>();

            int randomStartIndexX = 2;
            int randomStartIndexY = 2;
            int xOffset = 0;
            int yOffset = 0;

            if (objectPattern.Pattern.RuleTransform is SpawningPattern.Transform.MirrorX or SpawningPattern.Transform.MirrorXY)
            {
                randomStartIndexX = 1;
                xOffset = _random.NextInt(2);
            }

            if (objectPattern.Pattern.RuleTransform is SpawningPattern.Transform.MirrorY or SpawningPattern.Transform.MirrorXY)
            {
                randomStartIndexY = 1;
                yOffset = _random.NextInt(2);
            }

            Stopwatch watch = new();
            watch.Restart();
            
            for (int i = randomStartIndexX; i <= 2; i++)
            for (int j = randomStartIndexY; j <= 2; j++)
            {
                int _x = (int)Mathf.Pow(-1, i + xOffset);
                int _y = (int)Mathf.Pow(-1, j + yOffset);

                _flipAxis = new Vector2Int(_x, _y);
                IterateThroughMapWithPattern(objectPattern);
            }
            
            TryToLogGenerationTime("[SpawnableObjectPatternGenerator] Generate Per Axis took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();

            foreach (SpawnableObject spawnableObject in objectPattern.SpawnableObjects.ShuffleList(_random))
            {
                SpawningCycle(spawnableObject);
            }
            
            TryToLogGenerationTime("[SpawnableObjectPatternGenerator] Object Spawning Cycle took: " + watch.ElapsedMilliseconds + "ms");
        }

        protected virtual void SpawningCycle(SpawnableObject spawnableObject)
        {
            if (!_spawnedItems.ContainsKey(spawnableObject))
            {
                _spawnedItems.Add(spawnableObject, 0);
            }

            GeneratePerlinNoiseOffset();
            float threshold = spawnableObject.SpawnChance;
            IEnumerable<ValidLocation> validLocations = ValidLocations.ShuffleList(_random);
            foreach (ValidLocation location in validLocations)
            {
                _flipAxis = location.FlipDir;
                bool perlinValid = ReadPerlinAtPosition(location.Position.x, location.Position.y, threshold);
                if (perlinValid && _spawnedItems[spawnableObject] < spawnableObject.MaxSpawned * _customDensityOverride)
                {
                    bool success = SpawnObjectAtPosition(location.Position.x, location.Position.y, spawnableObject);
                    if (success)
                    {
                        _spawnedItems[spawnableObject] += 1;
                    }
                }
                
                ValidLocations.RemoveAll(x => x.Position == location.Position && x.FlipDir == location.FlipDir);
            }
        }

        protected virtual bool SpawnObjectAtPosition(int x, int y, SpawnableObject spawnableObject)
        {
            if (spawnableObject.DontSpawnOnEdge && (x < 0 || x > _map.size.x - 1 || y < 0 || y > _map.size.y - 1))
            {
                return false;
            }

            float rand = _random.NextFloat(100f);
            if (rand < spawnableObject.FailChance)
            {
                return false;
            }
            
            GameObject spawnedItem = spawnableObject.SpawnObject(_objectHolder);
            if (spawnedItem == null)
            {
                return false;
            }

            Vector2 spawnPos = MapCoordsToSpace(x, y);
            spawnPos = OffsetSpawnPos(spawnPos, spawnableObject);
            spawnedItem.transform.position = new Vector3(spawnPos.x, spawnPos.y, spawnedItem.transform.position.z);

            Vector3 origScale = spawnableObject.ObjectPrefab.transform.localScale;
            spawnedItem.transform.localScale = new Vector3(_flipAxis.x * origScale.x, _flipAxis.y * origScale.y, origScale.z);

            var angle = spawnableObject.RotationOffset;
            if (spawnableObject.FlipOnX)
            {
                angle *= _flipAxis.x;
            }

            if (spawnableObject.FlipOnY)
            {
                angle *= _flipAxis.y;
            }

            spawnedItem.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            spawnedItem.SetActive(true);
            return true;
        }

        private Vector2 OffsetSpawnPos(Vector2 spawnPos, SpawnableObject spawnableObject)
        {
            float xOff = _random.NextFloat(spawnableObject.RandomOffsetRangeX.x, spawnableObject.RandomOffsetRangeX.y);
            float yOff = _random.NextFloat(spawnableObject.RandomOffsetRangeY.x, spawnableObject.RandomOffsetRangeY.y);
            xOff += spawnableObject.PositionOffset.x * _flipAxis.x;
            yOff += spawnableObject.PositionOffset.y * _flipAxis.y;
            return spawnPos + new Vector2(xOff, yOff);
        }

        private Vector2 MapCoordsToSpace(int x, int y)
        {
            float _x = x + _defaultOffset.x;
            float _y = y + _defaultOffset.y;
            return new Vector2(_x, _y);
        }

        private void IterateThroughMapWithPattern(ObjectPattern objectPattern)
        {
            SpawningPattern spawningPattern = objectPattern.Pattern;
            if (objectPattern == null)
            {
                Debug.Log(name);
            }
            
            for (int x = 0; x < _map.size.x; x++)
            for (int y = 0; y < _map.size.y; y++)
                if (CheckMapPosAtCoords(x , y, spawningPattern))
                {
                    Vector2Int validPoint = new(x, y);
                    ValidLocation location = new ()
                    {
                        Position = validPoint,
                        FlipDir = _flipAxis
                    };
                    
                    bool isUnique = true;
                    if (objectPattern.UniquePositions)
                    {
                        foreach (ValidLocation loc in ValidLocations)
                        {
                            if (location.Position.x == loc.Position.x && location.Position.y == loc.Position.y)
                            {
                                isUnique = false;
                            }
                        }
                    }

                    if (isUnique)
                    {
                        ValidLocations.Add(location);
                    }
                }
        }

        private bool CheckMapPosAtCoords(int x, int y, SpawningPattern pattern)
        {
            Dictionary<Vector3Int, int> neighbors = pattern.Neighbors;

            foreach (KeyValuePair<Vector3Int, int> neighbor in neighbors)
            {
                int _x = neighbor.Key.x;
                int _y = neighbor.Key.y;
                if (_flipAxis.x == -1)
                {
                    _x = -_x;
                }

                if (_flipAxis.y == -1)
                {
                    _y = -_y;
                }

                BlockState patternState = ConvertPatternToBlockState(neighbor.Value);
                if (patternState == BlockState.NULL)
                {
                    continue;
                }

                BlockState mapState = ReadMapAtPosition(x + _x, y + _y);

                if (mapState != patternState)
                {
                    return false;
                }
                
            }

            return true;
        }

        private static BlockState ConvertPatternToBlockState(int patternValue)
        {
            return patternValue switch
            {
                SpawningPattern.Neighbor.Air => BlockState.Air,
                SpawningPattern.Neighbor.Tile => BlockState.Block,
                _ => BlockState.NULL
            };
        }

        private BlockState ReadMapAtPosition(int x, int y)
        {
            if (x >= 0 && x < _cachedMapSize.x && y >= 0 && y < _cachedMapSize.y)
            {
                return _cachedMap[x, y];
            }

            return BlockState.Block;
        }

        private void GeneratePerlinNoiseOffset()
        {
            _perlinNoiseOffset = new Vector2Int(_random.NextInt(_perlinTexture.width), _random.NextInt(_perlinTexture.height));
        }

        private bool ReadPerlinAtPosition(int x, int y, float threshold)
        {
            x += _perlinNoiseOffset.x;
            y += _perlinNoiseOffset.y;
            Color pixel = _perlinTexture.GetPixel(x, y);

            return pixel.maxColorComponent < threshold / 100f;
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(SpawnableObjectPatternGenerator))]
    internal class SpawnableObjectPatternGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Generate Pattern"))
            {
                SpawnableObjectPatternGenerator generator = (SpawnableObjectPatternGenerator) target;
                generator.GenerateWithLevelGeneratorNextFrame();
            }
        }
    }
#endif
}