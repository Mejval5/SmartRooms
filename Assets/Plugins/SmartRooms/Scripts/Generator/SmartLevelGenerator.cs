using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SmartRooms.Rooms;
using SmartRooms.Editor;
using SmartRooms.Generator.Tiles;
using SmartRooms.Levels;
using SmartRooms.Palette;
using SmartRooms.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

namespace SmartRooms.Generator
{
    /// <summary>
    /// A class that generates a level based on a LevelStyle.
    /// </summary>
#if UNITY_EDITOR
    [HideScriptField]
#endif
    [ExecuteAlways]
    public class SmartLevelGenerator : MonoBehaviour
    {
        private const int maxBuildIterations = 5000;

        [Header("Level setup")]
        [SerializeField] private LevelStyle _levelStyle;

        [Header("Scene setup")]
        [SerializeField] private Transform _roomGameObjectsHolder;
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _exit;
        [SerializeField] private BorderGenerator _borderGenerator;
        [SerializeField] private BackgroundGenerator _backgroundGenerator;

        [Header("Additional options")]
        [SerializeField] private bool _sameTileNeighbor = true;
        [SerializeField] [Range(0, 100)] private int _randomStopChance = 25;
        [SerializeField] private int _seed = -1;

        [Header("Debug features")]
        [SerializeField] private bool _logGenerationTime;
        [SerializeField] private bool _logWarnings = true;
        [SerializeField] private int _maxAttempts = 10;
        [SerializeField] private bool _tryToFindError;

        [Header("Visualization")]

        [Header("Output")]
        public LevelLayout finalLevelLayout;

        // Usable rooms
        private readonly List<SmartRoom.RoomData> _usableRooms = new();
        private readonly List<SmartRoom.RoomData> _usableStartRooms = new();
        private readonly List<SmartRoom.RoomData> _usableEndRooms = new();
        private readonly List<SpecialRoom.SpecialRoomData> _usableSpecialRooms = new();

        // Flipped rooms
        private List<SmartRoom.RoomData> _flippedRooms = new();
        private List<SmartRoom.RoomData> _flippedStartRooms = new();
        private List<SmartRoom.RoomData> _flippedEndRooms = new();
        private List<SpecialRoom.SpecialRoomData> _flippedSpecialRooms = new();

        // Room tiles
        private readonly List<RoomTile> _roomTiles = new();
        private readonly List<RoomTile> _startRoomTiles = new();
        private readonly List<RoomTile> _endRoomTiles = new();
        private readonly List<SpecialTile> _specialRoomTiles = new();

        // A* path finding
        private List<Vector2Int> _closedList;
        private List<APoint> _openList;
        private Vector2Int _startPos;
        private Vector2Int _endPos;
        
        // Shortest path visualization
        private IEnumerator _showShortestPathCoroutineIEnumerator;
        private Coroutine _showShortestPathCoroutine;
        
        // Generation order visualization
        private List<Vector2Int> _mainGeneratedPath = new ();
        private IEnumerator _mainGeneratedPathCoroutineIEnumerator;
        private Coroutine _mainGeneratedPathCoroutine;

        // Internals
        private Random _random;
        private IEnumerator _generateCoroutineIEnumerator;
        private Coroutine _generateCoroutine;
        private bool _generationFinished;
        private Thread _t;
        
        // Events
        public event Action LevelGenerated = delegate { };
        
        // Properties
        public Tilemap TargetTilemap => _tilemap;
        public LevelStyle CurrentLevelStyle => _levelStyle;

        /// <summary>
        /// A* path point
        /// </summary>
        public class APoint
        {
            public Vector2Int Pos;
            public APoint Parent;
            public int Cost;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            EditorApplication.update -= UpdateEditorCoroutines;
            EditorApplication.update += UpdateEditorCoroutines;
        }
        private void OnDisable()
        {
            EditorApplication.update -= UpdateEditorCoroutines;
        }

        private void UpdateEditorCoroutines()
        {
            // This is needed because the coroutine doesn't work in the editor.
            _generateCoroutineIEnumerator?.MoveNext();
            _showShortestPathCoroutineIEnumerator?.MoveNext();
        }
#endif

        /// <summary>
        /// Starts the generation of the level.
        /// Everything should be setup before calling this method.
        /// </summary>
        public void StartGeneration()
        {
            // Don't run generation if the background generation thread is still running.
            if (_t is { IsAlive: true })
            {
                return;
            }
            
            // Stop previous generating coroutine if it is still running.
            if (_generateCoroutine != null)
            {
                StopCoroutine(_generateCoroutine);
            }

            // Initialize the level generation seed.
            _random = _seed == -1 ? new Random((uint)DateTime.Now.Ticks) : new Random((uint)_seed);

            // Start the generation in a background thread.
            _generationFinished = false;
            _t = new Thread(Generate);
            _t.Start();

            // Start the coroutine that checks if the generation is finished.
            _generateCoroutineIEnumerator = CheckGenerationFinished();
            _generateCoroutine = StartCoroutine(_generateCoroutineIEnumerator);
        }

        /// <summary>
        /// We need to finish the final steps of the generation in the main thread.
        /// A coroutine that checks if the generation is finished.
        /// If it is finished, it will place the start and exit GOs and update the tilemap.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckGenerationFinished()
        {
            // Wait until the background thread is dead.
            while (_t.IsAlive)
            {
                yield return null;
            }

            // If the generation failed to generate a level, don't continue.
            if (_generationFinished == false)
            {
                yield break;
            }
            
            // Start timer.
            Stopwatch watch = new();
            watch.Restart();
            
            // Place the start and exit GOs.
            PlaceStartAndExitGO();
            TryToLogGenerationTime("[PlaceStartAndExitGO] Placing start and exit game objects took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();

            // Update the tilemap with level and add border.
            UpdateTilemapAndBorder();
            TryToLogGenerationTime("[UpdateTilemap] Setting tiles in tilemap took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();

            // Spawn game objects in the rooms.
            GenerateRoomGameObjects();
            TryToLogGenerationTime("[GenerateRoomGameObjects] Generating level Game Objects took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();
            
            // Tell subscribers that the generation has finished.
            LevelGenerated();
        }

        /// <summary>
        /// Place the start and exit GOs.
        /// </summary>
        private void PlaceStartAndExitGO()
        {
            // Place the start GO.
            Vector2 startPos = _startPos + 0.5f * Vector2.one;
            _start.position = new Vector3(startPos.x, startPos.y, _start.position.z);

            // Place the exit GO.
            Vector2 endPos = _endPos + 0.5f * Vector2.one;
            _exit.position = new Vector3(endPos.x, endPos.y, _start.position.z);
        }

        /// <summary>
        /// Generate the level layout. Use several attempts if needed.
        /// </summary>
        private void Generate()
        {
            // Run generation algorithm to find a level layout.
            int attempts = 0;
            while (attempts < _maxAttempts)
            {
                attempts += 1;

                // If the generation is finished, stop the loop.
                if (GenerateLoop(attempts) != _tryToFindError)
                {
                    break;
                }
            }

            // If the generation didn't succeed on the first try, log it.
            if (attempts > 1)
            {
                LogWarning("Finished generating in " + attempts + " tries.");
            }

            // If the generation managed to generate a level, don't continue.
            if (attempts < _maxAttempts)
            {
                _generationFinished = true;
                return;
            }
            
            // Log error if the generation failed to generate a level.
            if (_tryToFindError)
            {
                _generationFinished = true;
                Debug.LogError("Couldn't find error in " + attempts + " tries.");
            }
            else
            {
                Debug.LogError("Couldn't generate level in " + attempts + " tries.");
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

        /// <summary>
        /// Run the generation algorithm single time.
        /// </summary>
        /// <param name="attempts"></param>
        /// <returns></returns>
        private bool GenerateLoop(int attempts)
        {
            Stopwatch watch = new();
            watch.Restart();

            // Get all room data
            GetRooms();
            TryToLogGenerationTime("Getting rooms took: " + watch.ElapsedMilliseconds + "ms");
            
            // Create more rooms by flipping the existing ones.
            FlipRooms();
            TryToLogGenerationTime("Flipping rooms took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();
            
            // Convert all rooms into tiles.
            CreateAvailableTiles();
            TryToLogGenerationTime("Creating available rooms took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();

            // Attempt to build the level layout.
            bool builtLevel = BuildLevel(attempts);
            TryToLogGenerationTime("Building level took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();

            // If the level layout failed to build, return false.
            if (builtLevel == false)
            {
                LogWarning("Level building failed on try: " + attempts);
                return false;
            }

            // Place the start and exit tiles.
            CalculateStartAndExitLocations();
            TryToLogGenerationTime("Placing start and exit took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();

            bool levelCompletable = TryToCompleteLevel();

            if (levelCompletable == false)
            {
                LogWarning("Unfinishable level");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculate the start and exit tile locations.
        /// </summary>
        private void CalculateStartAndExitLocations()
        {
            int startCount = 0;
            int endCount = 0;
            for (int x = 0; x < _levelStyle.LevelTileSize.x; x++)
            for (int y = 0; y < _levelStyle.LevelTileSize.y; y++)
            {
                RoomTile tile = finalLevelLayout.roomTiles[x, y] as RoomTile;
                if (tile == null)
                {
                    continue;
                }
                
                // Calculate which tile is the entrance tile.
                Vector2Int entranceTileLocation = tile.GetEntrance + tile.roomData.structureData.size * new Vector2Int(x, y);
                switch (tile.Type)
                {
                    case RoomTile.RoomTileType.StartRoom:
                    {
                        _startPos = entranceTileLocation;
                        startCount += 1;
                        break;
                    }
                    case RoomTile.RoomTileType.EndRoom:
                    {
                        _endPos = entranceTileLocation;
                        endCount += 1;
                        break;
                    }
                    case RoomTile.RoomTileType.Default:
                    default:
                        continue;
                }
            }

            // Log warnings if the start or end tile is missing or if there are multiple start or end tiles.
            if (startCount == 0)
            {
                LogWarning("No start");
            }

            if (endCount == 0)
            {
                LogWarning("No end");
            }

            if (startCount > 1)
            {
                LogWarning("Start not unique");
            }

            if (endCount > 1)
            {
                LogWarning("End not unique");
            }
        }

        /// <summary>
        /// Generate the tilemap from the layout.
        /// Must run on main thread.
        /// </summary>
        private void UpdateTilemapAndBorder()
        {
            if (finalLevelLayout.levelLayout == null)
            {
                return;
            }

            // Create the bounds of the tilemap.
            const int minX = 0;
            int maxX = finalLevelLayout.levelSize.x;
            const int minY = 0;
            int maxY = finalLevelLayout.levelSize.y;
            BoundsInt tilemapBounds = new(minX, minY, 0, maxX - minX, maxY - minY, 1);

            // Set main tiles
            _tilemap.ClearAllTiles();
            _tilemap.SetTilesBlock(tilemapBounds, finalLevelLayout.levelLayout);

            // Generate border around the map
            _borderGenerator.UpdateSettings(_tilemap, _levelStyle);
            _borderGenerator.GenerateBorder(minX, maxX, minY, maxY);
            
            // Generate the background
            _backgroundGenerator.GenerateBackground(_levelStyle, finalLevelLayout.levelSize);

            // Compress the tilemap.
            _tilemap.CompressBounds();
        }

        /// <summary>
        /// Gets random position for the start tile.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Vector2Int GetStartTilePosition()
        {
            int randomStartTileX = _random.NextInt(_levelStyle.LevelTileSize.x);
            int randomStartTileY = _random.NextInt(_levelStyle.LevelTileSize.y);

            return _levelStyle.BuildModeDirection switch
            {
                LevelStyle.BuildMode.TopToBottom => new Vector2Int(randomStartTileX, _levelStyle.LevelTileSize.y - 1),
                LevelStyle.BuildMode.BottomUp => new Vector2Int(randomStartTileX, 0),
                LevelStyle.BuildMode.LeftToRight => new Vector2Int(0, randomStartTileY),
                LevelStyle.BuildMode.RightToLeft => new Vector2Int(_levelStyle.LevelTileSize.x - 1, randomStartTileY),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Is the current tile in the final layer of the level.
        /// </summary>
        /// <param name="prevCoords"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private bool InFinalLayer(Vector2Int prevCoords)
        {
            return _levelStyle.BuildModeDirection switch
            {
                LevelStyle.BuildMode.TopToBottom => prevCoords.y == 0,
                LevelStyle.BuildMode.BottomUp => prevCoords.y == _levelStyle.LevelTileSize.y - 1,
                LevelStyle.BuildMode.LeftToRight => prevCoords.x == _levelStyle.LevelTileSize.x - 1,
                LevelStyle.BuildMode.RightToLeft => prevCoords.x == 0,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

         /// <summary>
         /// Builds the whole the level layout.
         /// </summary>
         /// <param name="attempts"></param>
         /// <returns></returns>
        private bool BuildLevel(int attempts)
        {
            InitializeLevelLayout();

            if (BuildMainPath() == false)
            {
                return false;
            }

            // Attempts to spawn special rooms. If it fails, restart the level generation.
            // If we are at last attempt, skip any mandatory rooms.
            if (SpawnSpecialRooms() == false && attempts < _maxAttempts)
            {
                LogWarning("Mandatory special room could not spawn. Restarting.");
                return false;
            }
            
            // Fill the empty tiles with random rooms.
            FillEmptyTilesWithRandom();

            // Run through the level layout, write each tile in GenTile to layout, and spawn substructures.
            for (int x = 0; x < _levelStyle.LevelTileSize.x; x++)
            for (int y = 0; y < _levelStyle.LevelTileSize.y; y++)
            {
                WriteGenTileToLayout(x, y);
                SpawnSubstructure(x, y);
            }

            return true;
        }

         private void InitializeLevelLayout()
         {
             Vector2Int levelSize = _usableRooms[0].structureData.size * _levelStyle.LevelTileSize;
             finalLevelLayout = new LevelLayout(_levelStyle.LevelTileSize, levelSize);
         }

         private bool BuildMainPath()
        {
            // Initialize generation
            RoomTile previousTile = null;
            _mainGeneratedPath = new List<Vector2Int> { GetStartTilePosition() };
            Direction newDirection = Direction.Down;
            Direction lastDirection = Direction.Down;

            int iteration = 0;
            while (iteration < maxBuildIterations)
            {
                // Calculate which directions we can take from the current tile.
                List<Direction> possibleDirections = GetAvailableSides(_mainGeneratedPath.LastOrDefault());
                
                // Get the next tile. It should connect to the previous tile, and be in the list of possible directions.
                RoomTile newTile = GetTile(previousTile, newDirection, possibleDirections);

                bool inFinalLayer = previousTile != null && _mainGeneratedPath.Count >= 2 && InFinalLayer(_mainGeneratedPath[^2]);
                bool randomStop = _random.NextInt(100) < _randomStopChance;
                bool noTileStop = newTile == null;

                // If we are in the final layer, and we have no place to go or we have a random stop, stop.
                if (inFinalLayer && (noTileStop || randomStop))
                {
                    break;
                }

                // If we have no place to go and we are not in the final layer then the level is not possible.
                if (noTileStop)
                {
                    LogWarning("A tile had no possible successor: " + iteration);
                    return false;
                }

                // Mark first tile as the start tile.
                if (previousTile == null)
                {
                    newTile.Type = RoomTile.RoomTileType.StartRoom;
                }
                else
                {
                    lastDirection = newDirection;
                }

                // Save the tile to the layout.
                finalLevelLayout.roomTiles[_mainGeneratedPath.LastOrDefault().x, _mainGeneratedPath.LastOrDefault().y] = newTile;

                // Calculate new direction and position.
                List<Direction> validDirections = GetValidDirections(newTile, possibleDirections);
                int index = _random.NextInt(validDirections.Count);
                newDirection = validDirections[index];
                _mainGeneratedPath.Add(_mainGeneratedPath.LastOrDefault() + newDirection.ToVector2Int());

                // Save the tile for the next iteration.
                previousTile = newTile;
                
                // Iterate
                iteration += 1;
            }

            if (iteration == maxBuildIterations)
            {
                LogWarning("Hit max iteration, probably a bug: " + iteration);
                return false;
            }
            
            // One before current tile position is an exit tile.
            if (GenerateExit(_mainGeneratedPath[^3], _mainGeneratedPath[^2], lastDirection) == false)
            {
                LogWarning("Exit generation failed");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Replaces the last tile with an exit tile.
        /// </summary>
        /// <param name="previousTileCoords"></param>
        /// <param name="lastDirection"></param>
        /// <param name="exitPosition"></param>
        /// <returns></returns>
        private bool GenerateExit(Vector2Int previousTileCoords, Vector2Int exitPosition, Direction lastDirection)
        {
            RoomTile lastRoomTile = finalLevelLayout.roomTiles[exitPosition.x, exitPosition.y] as RoomTile;

            if (lastRoomTile == null)
            {
                LogWarning("Last room tile was null");
                return false;
            }

            RoomTile tileBeforeEnd = finalLevelLayout.roomTiles[previousTileCoords.x, previousTileCoords.y] as RoomTile;

            if (tileBeforeEnd == null)
            {
                LogWarning("Tile before end was null");
                return false;
            }

            RoomTile replacementTile = GetEndTile(tileBeforeEnd, lastDirection);
            if (replacementTile != null)
            {
                lastRoomTile = replacementTile;
            }
            else
            {
                LogWarning("No end tile found as replacement");
            }

            lastRoomTile.Type = RoomTile.RoomTileType.EndRoom;
            finalLevelLayout.roomTiles[exitPosition.x, exitPosition.y] = lastRoomTile;
            return true;
        }

        private void SpawnSubstructure(int x, int y)
        {
            GenTile tile = finalLevelLayout.roomTiles[x, y];

            bool willHaveSubstructures = _random.NextInt(100) < tile.structureData.substructuresChance;
            if (willHaveSubstructures == false || tile.structureData.substructures.Count == 0)
            {
                finalLevelLayout.spawnedSubStructureIndex[x, y] = -1;
                return;
            }

            // Select one randomly chosen substructure based on its weight and assign the index to the structure data
            float totalWeight = 0;
            for (int i = 0; i < tile.structureData.substructures.Count; i++)
            {
                totalWeight += tile.structureData.substructures[i].Weight;
            }

            float randomValue = _random.NextFloat() * totalWeight;
            float currentWeight = 0;
            int substructureIndex = 0;
            for (int i = 0; i < tile.structureData.substructures.Count; i++)
            {
                currentWeight += tile.structureData.substructures[i].Weight;
                if (randomValue < currentWeight)
                {
                    substructureIndex = i;
                    break;
                }
            }

            finalLevelLayout.spawnedSubStructureIndex[x, y] = substructureIndex;
            Structure substructure = tile.structureData.substructures[substructureIndex];

            Structure.StructureData substructureData = substructure.GetStructureData();
            for (int i = 0; i < substructureData.layout.Length; i++)
            {
                int xLevel = i % substructureData.size.x + tile.structureData.size.x / 2 + substructureData.offset.x - substructureData.size.x / 2;
                int yLevel = i / substructureData.size.x + tile.structureData.size.y / 2 + substructureData.offset.y - substructureData.size.y / 2;

                if (Mathf.Approximately(tile.structureData.scaleModifier.x, -1))
                {
                    xLevel = tile.structureData.size.x - xLevel - 1;
                }

                if (Mathf.Approximately(tile.structureData.scaleModifier.y, -1))
                {
                    yLevel = tile.structureData.size.y - yLevel - 1;
                }

                xLevel += x * tile.structureData.size.x;
                yLevel += y * tile.structureData.size.y;
                int pos = xLevel + yLevel * finalLevelLayout.levelSize.x;
                finalLevelLayout.levelLayout[pos] = substructureData.layout[i];
            }
        }

        private void WriteGenTileToLayout(int x, int y)
        {
            GenTile tile = finalLevelLayout.roomTiles[x, y];
            
            for (int i = 0; i < tile.structureData.layout.Length; i++)
            {
                int xLevel = i % tile.structureData.size.x + x * tile.structureData.size.x;
                int yLevel = i / tile.structureData.size.x + y * tile.structureData.size.y;
                int pos = xLevel + yLevel * finalLevelLayout.levelSize.x;
                finalLevelLayout.levelLayout[pos] = tile.structureData.layout[i];
            }
        }

        private void FillEmptyTilesWithRandom()
        {
            for (int x = 0; x < _levelStyle.LevelTileSize.x; x++)
            for (int y = 0; y < _levelStyle.LevelTileSize.y; y++)
            {
                GenTile tile = finalLevelLayout.roomTiles[x, y];

                if (tile == null)
                {
                    int index = _random.NextInt(_roomTiles.Count);
                    RoomTile randomTile = _roomTiles[index];
                    tile = randomTile;
                    finalLevelLayout.roomTiles[x, y] = tile;
                }
            }
        }

        private bool SpawnSpecialRooms()
        {
            if (_levelStyle.SpecialRooms.Count > 0)
            {
                List<Vector2Int> possibleCoords = new ();
                for (int x = 0; x < _levelStyle.LevelTileSize.x; x++)
                for (int y = 0; y < _levelStyle.LevelTileSize.y; y++)
                {
                    possibleCoords.Add(new Vector2Int(x, y));
                }
                possibleCoords = possibleCoords.OrderBy(_ => _random.NextFloat()).ToList();

                foreach (Vector2Int coords in possibleCoords)
                {
                    int x = coords.x;
                    int y = coords.y;
                    
                    if (finalLevelLayout.roomTiles[x, y] == null)
                    {
                        continue;
                    }

                    Vector2Int startCoords = new (x, y);
                    
                    List<Direction> possibleDirections = GetAvailableSides(startCoords);
                    GenTile tile = finalLevelLayout.roomTiles[x, y];
                    
                    (SpecialTile specialTile, Vector2Int tilePos) = GetSpecialTile(tile, possibleDirections, startCoords);
                    if (specialTile == null)
                    {
                        continue;
                    }

                    bool shouldSpawnByChance = _random.NextFloat(0, 100f) < specialTile.SpecialRoomData.spawnChance;
                    if (shouldSpawnByChance == false && specialTile.SpecialRoomData.mandatory == false)
                    {
                        continue;
                    }

                    if (specialTile.SpecialRoomData.hasChildStructure)
                    {
                        Vector2Int childCoords = tilePos + specialTile.SpecialRoomData.childStructureDirection.ToVector2Int();
                        
                        if (childCoords.x < 0 || childCoords.x >= _levelStyle.LevelTileSize.x || childCoords.y < 0 || childCoords.y >= _levelStyle.LevelTileSize.y)
                        {
                            continue;
                        }
                        
                        if (finalLevelLayout.roomTiles[childCoords.x, childCoords.y] != null)
                        {
                            continue;
                        }

                        // Convert child structure to a tile
                        GenTile childTile = new(specialTile.SpecialRoomData.childStructure);
                        childTile.GenerateFreeTilesSide();
                        List<GenTile> childTiles = new() { childTile };
                        ConnectTiles(childTiles);

                        finalLevelLayout.roomTiles[childCoords.x, childCoords.y] = childTile;
                    }

                    finalLevelLayout.roomTiles[tilePos.x, tilePos.y] = specialTile;
                    finalLevelLayout.spawnedSpecialTiles.Add(specialTile);
                }
            }

            List<string> spawnedSpecialRooms = finalLevelLayout.spawnedSpecialTiles.Select(roomTile => roomTile.SpecialRoomData.structureData.id).ToList();
            
            return _usableSpecialRooms.TrueForAll(specialRoom => specialRoom.mandatory == false || spawnedSpecialRooms.Any(
                spawnedRoomID => spawnedRoomID == specialRoom.structureData.id)
            );
        }

        private RoomTile GetTile(GenTile prevTile, Direction lastDir, List<Direction> possibleDirections)
        {
            List<RoomTile> possibleRooms = GetPossibleTiles(prevTile, lastDir, possibleDirections);

            if (possibleRooms.Count == 0)
            {
                return null;
            }

            int index = _random.NextInt(possibleRooms.Count);
            return possibleRooms[index];
        }

        private RoomTile GetEndTile(GenTile prevTile, Direction lastDir)
        {
            List<RoomTile> possibleRooms = GetPossibleEndTiles(prevTile, lastDir);

            if (possibleRooms.Count == 0)
            {
                return null;
            }

            int index = _random.NextInt(possibleRooms.Count);
            return possibleRooms[index];
        }

        private List<RoomTile> GetPossibleTiles(GenTile prevTile, Direction lastDir, List<Direction> possibleDirections)
        {
            List<RoomTile> possibleRooms = new ();

            List<RoomTile> availableTiles = _startRoomTiles;
            if (prevTile != null)
            {
                availableTiles = prevTile.roomTiles[lastDir];
            }

            foreach (RoomTile roomTile in availableTiles)
            {
                foreach (Direction dir in possibleDirections)
                {
                    List<RoomTile> matchingRooms = roomTile.roomTiles[dir];

                    if (matchingRooms.Count > 0)
                    {
                        possibleRooms.Add(roomTile);
                    }
                }
            }

            return possibleRooms;
        }

        private List<RoomTile> GetPossibleEndTiles(GenTile prevTile, Direction lastDir)
        {
            List<RoomTile> availableTiles = _endRoomTiles;
            if (prevTile != null)
            {
                availableTiles = prevTile.endRoomTiles[lastDir];
            }

            return availableTiles;
        }
        
        private (SpecialTile, Vector2Int) GetSpecialTile(GenTile tile, List<Direction> possibleDirs, Vector2Int startPos)
        {
            (List<SpecialTile> possibleRooms, List<Vector2Int> positions) = GetPossibleSpecialTiles(tile, possibleDirs, startPos);

            if (possibleRooms.Count == 0)
            {
                return (null, Vector2Int.zero);
            }

            int index = _random.NextInt(possibleRooms.Count);
            return (possibleRooms[index], positions[index]);
        }
        
        private (List<SpecialTile>, List<Vector2Int>) GetPossibleSpecialTiles(GenTile tile, List<Direction> possibleDirections, Vector2Int startPos)
        {
            List<SpecialTile> possibleTiles = new ();
            List<Vector2Int> possiblePositions = new ();
            
            foreach (Direction dir in possibleDirections)
            {
                Vector2Int pos = startPos + dir.ToVector2Int();
                if (pos.x < 0 || pos.x >= _levelStyle.LevelTileSize.x || pos.y < 0 || pos.y >= _levelStyle.LevelTileSize.y)
                {
                    continue;
                }
                
                List<SpecialTile> directionTiles = tile.specialRoomTiles[dir];
                directionTiles = directionTiles.Where(possibleTile =>
                     finalLevelLayout.spawnedSpecialTiles.TrueForAll(specialTile => possibleTile.structureData.id != specialTile.SpecialRoomData.structureData.id)
                     ).ToList();

                if (directionTiles.Count > 0)
                {
                    possibleTiles.AddRange(directionTiles);
                    possiblePositions.AddRange(directionTiles.Select(_ => pos).ToList());
                }
            }

            List<SpecialTile> mandatoryTiles = new();
            List<Vector2Int> mandatoryTilesPositions = new();
            for (int i = 0; i < possibleTiles.Count; i++)
            {
                SpecialTile specialTile = possibleTiles[i];
                if (specialTile.SpecialRoomData.mandatory == false)
                {
                    continue;
                }

                mandatoryTiles.Add(specialTile);
                mandatoryTilesPositions.Add(possiblePositions[i]);
            }

            return mandatoryTiles.Count > 0 ? (mandatoryTiles, mandatoryTilesPositions) : (possibleTiles, possiblePositions);
        }

        private List<Direction> GetValidDirections(RoomTile newTile, List<Direction> possibleDirections)
        {
            List<Direction> validDirections = new List<Direction>();

            foreach (Direction dir in possibleDirections)
            {
                List<RoomTile> matchingRooms = newTile.roomTiles[dir];

                if (matchingRooms.Count > 0)
                {
                    if (validDirections.Contains(dir) == false)
                    {
                        validDirections.Add(dir);
                    }
                }
            }

            return validDirections;
        }

        private IEnumerable<Direction> GetPossibleDirections()
        {
            return _levelStyle.BuildModeDirection switch
            {
                LevelStyle.BuildMode.TopToBottom => new[] { Direction.Right, Direction.Down, Direction.Left },
                LevelStyle.BuildMode.BottomUp => new[] { Direction.Right, Direction.Up, Direction.Left },
                LevelStyle.BuildMode.LeftToRight => new[] { Direction.Right, Direction.Up, Direction.Down },
                LevelStyle.BuildMode.RightToLeft => new[] { Direction.Up, Direction.Down, Direction.Left },
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private List<Direction> GetAvailableSides(Vector2Int pos)
        {
            IEnumerable<Direction> possibleDirs = GetPossibleDirections();

            List<Direction> emptyDirs = new();

            foreach (Direction dir in possibleDirs)
            {
                Vector2Int posCheck = pos + dir.ToVector2Int();
                if (posCheck.x < 0 || posCheck.x >= _levelStyle.LevelTileSize.x || posCheck.y < 0 || posCheck.y >= _levelStyle.LevelTileSize.y)
                {
                    continue;
                }

                if (finalLevelLayout.roomTiles[posCheck.x, posCheck.y] != null)
                {
                    continue;
                }

                emptyDirs.Add(dir);
            }

            return emptyDirs;
        }

        private void CreateAvailableTiles()
        {
            Stopwatch watch = new();
            watch.Restart();

            InitializeTiles();
            TryToLogGenerationTime("[Creating available rooms] Initializing rooms took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();

            ConnectTiles(_roomTiles);
            ConnectTiles(_startRoomTiles);
            ConnectTiles(_endRoomTiles);
            ConnectTiles(_specialRoomTiles);
            TryToLogGenerationTime("[Creating available rooms] Connecting tiles took: " + watch.ElapsedMilliseconds + "ms");
            watch.Restart();
        }

        private void ConnectTiles<TGenTile>(List<TGenTile> tiles) where TGenTile : GenTile
        {
            foreach (TGenTile tile in tiles)
            {
                tile.roomTiles = GetConnectedTiles(tile, _roomTiles);
                tile.endRoomTiles = GetConnectedTiles(tile, _endRoomTiles);
                tile.specialRoomTiles = GetConnectedTiles(tile, _specialRoomTiles);
            }
        }

        private Dictionary<Direction, List<TGenTile>> GetConnectedTiles<TGenTile1, TGenTile>(TGenTile1 tile, List<TGenTile> opposingTiles)
            where TGenTile1 : GenTile
            where TGenTile : GenTile
        {
            Dictionary<Direction, List<TGenTile>> connectedTiles = new();

            foreach (Direction dir in DirectionUtils.GetAllDirs)
            {
                connectedTiles[dir] = new List<TGenTile>();
            }
            
            foreach (Direction dir in DirectionUtils.GetAllDirs)
            {
                // Add end tiles to which can be connected for the last tile
                foreach (TGenTile nextTile in opposingTiles)
                {
                    if (_sameTileNeighbor == false && tile.structureData.layout == nextTile.structureData.layout)
                    {
                        continue;
                    }

                    bool connecting = TilesConnect(tile, nextTile, dir);
                    if (connecting)
                    {
                        connectedTiles[dir].Add(nextTile);
                    }
                }
            }

            return connectedTiles;
        }
        
        private void InitializeTiles()
        {
            _roomTiles.Clear();

            foreach (SmartRoom.RoomData room in _flippedRooms)
            {
                RoomTile newTile = new(room);
                newTile.GenerateFreeTilesSide();

                _roomTiles.Add(newTile);
            }

            _startRoomTiles.Clear();
            foreach (SmartRoom.RoomData room in _flippedStartRooms)
            {
                RoomTile newTile = new(room);
                newTile.GenerateFreeTilesSide();

                _startRoomTiles.Add(newTile);
            }

            _endRoomTiles.Clear();
            foreach (SmartRoom.RoomData room in _flippedEndRooms)
            {
                RoomTile newTile = new(room);
                newTile.GenerateFreeTilesSide();

                _endRoomTiles.Add(newTile);
            }
            
            _specialRoomTiles.Clear();
            foreach (SpecialRoom.SpecialRoomData room in _flippedSpecialRooms)
            {
                SpecialTile newTile = new(room);
                newTile.GenerateFreeTilesSide();

                _specialRoomTiles.Add(newTile);
            }
        }

        private static bool TilesConnect(GenTile firstTile, GenTile secondTile, Direction side)
        {
            int firstSide = firstTile.freeTilesSide[side];

            Direction oppositeSide = (Direction)(((int)side + 2) % 4);
            int secondSide = secondTile.freeTilesSide[oppositeSide];

            int connects = firstSide & secondSide;
            return connects != 0;
        }

        private void GetRooms()
        {
            _usableRooms.Clear();
            foreach (SmartRoom room in _levelStyle.Rooms)
            {
                _usableRooms.Add(room.GetRoomData());
            }

            _usableStartRooms.Clear();
            foreach (SmartRoom startRoom in _levelStyle.StartRooms)
            {
                _usableStartRooms.Add(startRoom.GetRoomData());
            }

            _usableEndRooms.Clear();
            foreach (SmartRoom endRoom in _levelStyle.EndRooms)
            {
                _usableEndRooms.Add(endRoom.GetRoomData());
            }
            
            _usableSpecialRooms.Clear();
            foreach (SpecialRoomConfig specialRoomConfig in _levelStyle.SpecialRooms)
            {
                SpecialRoom.SpecialRoomData specialRoomData = specialRoomConfig.room.GetSpecialRoomData(
                    specialRoomConfig.mandatory, specialRoomConfig.specialRoomGroupID, specialRoomConfig.spawnChance
                    );
                _usableSpecialRooms.Add(specialRoomData);
            }
        }

        private void FlipRooms()
        {
            _flippedRooms = GenerateFlippedRooms(_usableRooms);
            _flippedStartRooms = GenerateFlippedRooms(_usableStartRooms);
            _flippedEndRooms = GenerateFlippedRooms(_usableEndRooms);
            
            _flippedSpecialRooms = GenerateFlippedSpecialRooms(_usableSpecialRooms);
        }
        
        private List<SpecialRoom.SpecialRoomData> GenerateFlippedSpecialRooms(List<SpecialRoom.SpecialRoomData> specialRoomsToFlip)
        {
            List<SpecialRoom.SpecialRoomData> flippedRooms = new();
            
            foreach (SpecialRoom.SpecialRoomData room in specialRoomsToFlip)
            {
                SpecialRoom.SpecialRoomData flippedRoom = room;
                List<Structure.StructureData> flippedStructures = FlipStructureData(flippedRoom.structureData);

                foreach (Structure.StructureData flippedStructure in flippedStructures)
                {
                    flippedRoom.structureData = flippedStructure;

                    flippedRooms.Add(flippedRoom);
                }
            }

            return flippedRooms;
        }

        private List<SmartRoom.RoomData> GenerateFlippedRooms(List<SmartRoom.RoomData> roomsToFlip)
        {
            List<SmartRoom.RoomData> flippedRooms = new();
            
            foreach (SmartRoom.RoomData room in roomsToFlip)
            {
                SmartRoom.RoomData flippedRoom = new();
                List<Structure.StructureData> flippedStructures = FlipStructureData(room.structureData);

                foreach (Structure.StructureData flippedStructure in flippedStructures)
                {
                    flippedRoom.structureData = flippedStructure;
                    flippedRoom.entrancePos = room.entrancePos;

                    if (Mathf.Approximately(flippedRoom.structureData.scaleModifier.x, -1))
                    {
                        flippedRoom.entrancePos.x = flippedRoom.structureData.size.x - 1 - flippedRoom.entrancePos.x;
                    }

                    if (Mathf.Approximately(flippedRoom.structureData.scaleModifier.y, -1))
                    {
                        flippedRoom.entrancePos.y = flippedRoom.structureData.size.y - 1 - flippedRoom.entrancePos.y;
                    }

                    flippedRooms.Add(flippedRoom);
                }
            }

            return flippedRooms;
        }

        private List<Structure.StructureData> FlipStructureData(Structure.StructureData structureDataToFlip)
        {
            List<Structure.StructureData> flippedStructures = new();
            
            Vector2Int[] flips = new Vector2Int[4]
            {
                new(1, 1),
                new(1, -1),
                new(-1, 1),
                new(-1, -1)
            };
            
            foreach (Vector2Int flip in flips)
            {
                if (structureDataToFlip.horizontallyFlippable == false && flip.x == -1)
                {
                    continue;
                }

                if (structureDataToFlip.verticallyFlippable == false && flip.y == -1)
                {
                    continue;
                }

                Structure.StructureData flippedStructure = structureDataToFlip;
                
                if (flip.x == -1)
                {
                    flippedStructure.layout = StructureUtils.FlipStructureHorizontally(flippedStructure);
                    flippedStructure.scaleModifier.x = -1;
                }

                if (flip.y == -1)
                {
                    flippedStructure.layout = StructureUtils.FlipStructureVertically(flippedStructure);
                    flippedStructure.scaleModifier.y = -1;
                }
                
                flippedStructures.Add(flippedStructure);
            }

            return flippedStructures;
        }

        private bool TryToCompleteLevel()
        {
            _openList = new List<APoint>();
            _closedList = new List<Vector2Int>();

            Vector2Int coords = _startPos;
            APoint firstPoint = new()
            {
                Pos = coords,
                Parent = null,
                Cost = TaxiDistance(coords, _endPos)
            };

            _openList.Add(firstPoint);

            int result = RunAStar(coords, firstPoint);

            return result >= 0;
        }

        private int RunAStar(Vector2Int coords, APoint firstPoint)
        {
            const int maxIter = 5000;
            int iter = 0;

            while (iter < maxIter)
            {
                iter += 1;
                List<Vector2Int> newPoints = FindReachableTiles(firstPoint.Pos);
                foreach (Vector2Int point in newPoints)
                {
                    APoint newAPoint = new()
                    {
                        Pos = point,
                        Parent = firstPoint
                    };

                    if (TaxiDistance(point, _endPos) == 0)
                    {
                        _openList.Add(newAPoint);
                        return DistanceToStart(newAPoint);
                    }

                    newAPoint.Cost = 1 + firstPoint.Cost + TaxiDistance(coords, _endPos);
                    _openList.Add(newAPoint);
                    _closedList.Add(newAPoint.Pos);
                }

                _openList.Remove(firstPoint);
                firstPoint = GetNextAPoint();
                if (firstPoint == null)
                {
                    return -1;
                }
            }

            return -2;
        }

        private int DistanceToStart(APoint endPoint)
        {
            const int maxIter = 5000;
            int iter = 0;
            APoint currentPoint = endPoint;
            int distance = 0;
            while (iter < maxIter)
            {
                iter += 1;
                if (currentPoint.Parent == null)
                {
                    return distance;
                }

                currentPoint = currentPoint.Parent;
                distance += 1;
            }

            Debug.Log(iter);
            return 0;
        }

        private APoint GetNextAPoint()
        {
            APoint returnPoint = null;
            int minCost = int.MaxValue;

            foreach (APoint point in _openList)
            {
                if (point.Cost < minCost)
                {
                    minCost = point.Cost;
                    returnPoint = point;
                }
            }

            return returnPoint;
        }

        private static int TaxiDistance(Vector2Int pos, Vector2Int oldPos)
        {
            return Mathf.Abs(pos.x - oldPos.x) + Mathf.Abs(pos.y - oldPos.y);
        }

        private List<Vector2Int> FindReachableTiles(Vector2Int pos)
        {
            List<Vector2Int> newPoints = new();
            Direction[] possibleDirs = new Direction[4] { Direction.Right, Direction.Up, Direction.Left, Direction.Down };
            foreach (Direction dir in possibleDirs)
            {
                Vector2Int vecDir = dir.ToVector2Int();
                Vector2Int newPos = pos + vecDir;
                if (_closedList.Contains(newPos))
                {
                    continue;
                }

                if (newPos.x < 0 || newPos.x >= finalLevelLayout.levelSize.x || newPos.y < 0 || newPos.y >= finalLevelLayout.levelSize.y)
                {
                    continue;
                }

                TileBase tile = finalLevelLayout.GetTile(newPos.x, newPos.y);
                if (tile == null)
                {
                    newPoints.Add(newPos);
                }
            }

            return newPoints;
        }

        private void LogWarning(string param)
        {
            if (_logWarnings)
            {
                Debug.LogWarning(param);
            }
        }

        private void GenerateRoomGameObjects()
        {
            if (_roomGameObjectsHolder == null)
            {
                Debug.LogError("Didn't set up room gameobject holder");
            }
            
            _roomGameObjectsHolder.DestroyAllChildren();

            for (int x = 0; x < _levelStyle.LevelTileSize.x; x++)
            for (int y = 0; y < _levelStyle.LevelTileSize.y; y++)
            {
                GenTile tile = finalLevelLayout.roomTiles[x, y];
                    
                GameObject tileParent = new GameObject($"Room[{x}, {y}]");
                tileParent.transform.parent = _roomGameObjectsHolder;

                // We have the room offset by 1 if the room size is odd, this offsets all in place
                float tileParentPosX = (x + 0.5f) * tile.structureData.size.x - (tile.structureData.size.x / 2f) % 1f * tile.structureData.scaleModifier.x;
                float tileParentPosY = (y + 0.5f) * tile.structureData.size.y - (tile.structureData.size.y / 2f) % 1f * tile.structureData.scaleModifier.y;

                tileParent.transform.position =  new Vector3(tileParentPosX, tileParentPosY, 0f);

                Dictionary<Transform, Vector3> originalScales = new();
                
                for (int i = 0; i < tile.structureData.prefab.transform.childCount; i++)
                {
                    Transform childTransform = tile.structureData.prefab.transform.GetChild(i);
                    
                    if (childTransform.gameObject.Equals(tile.structureData.prefab))
                    {
                        continue;
                    }

                    Structure structure = childTransform.GetComponent<Structure>();
                    if (structure == null)
                    {
                        Transform newGameObject = Instantiate(childTransform, tileParent.transform);
                        originalScales.Add(newGameObject, newGameObject.lossyScale);
                        continue;
                    }

                    if (finalLevelLayout.spawnedSubStructureIndex[x, y] == -1)
                    {
                        continue;
                    }

                    Structure chosenSubstructure = tile.structureData.substructures[finalLevelLayout.spawnedSubStructureIndex[x, y]];
                    Structure.StructureData chosenSubstructureData = chosenSubstructure.GetStructureData();

                    if (chosenSubstructure.gameObject.Equals(structure.gameObject) == false)
                    {
                        continue;
                    }
                    
                    GameObject substructureParent = new ($"Substructure: {structure.name}");
                    substructureParent.transform.parent = tileParent.transform;
                    substructureParent.transform.localPosition = new Vector3(chosenSubstructureData.offset.x, chosenSubstructureData.offset.y, 0f);
                    
                    for (int k = 0; k < chosenSubstructure.transform.childCount; k++)
                    {
                        Transform substructureChild = chosenSubstructure.transform.GetChild(k);
                        
                        if (substructureChild.gameObject.Equals(chosenSubstructure.gameObject))
                        {
                            continue;
                        }
                        
                        Structure subSubstructure = substructureChild.GetComponent<Structure>();
                        if (subSubstructure != null)
                        {
                            continue;
                        }

                        Transform newGameObject = Instantiate(substructureChild, substructureParent.transform);
                        originalScales.Add(newGameObject, newGameObject.lossyScale);
                    }
                }

                Vector3 tileParentLocalScale = new (tile.structureData.scaleModifier.x, tile.structureData.scaleModifier.y, 1f);
                tileParent.transform.localScale = tileParentLocalScale;

                if (tile.structureData.flipChildren)
                {
                    continue;
                }

                foreach (KeyValuePair<Transform, Vector3> originalScale in originalScales)
                {
                    originalScale.Key.SetGlobalScale(originalScale.Value);
                }
            }
        }

        public void ShowGenerationOrder()
        {
            if (_mainGeneratedPathCoroutine != null)
            {
                StopCoroutine(_mainGeneratedPathCoroutine);
            }

            _mainGeneratedPathCoroutineIEnumerator = ShowGenerationOrderCoroutine();
            _mainGeneratedPathCoroutine = StartCoroutine(_mainGeneratedPathCoroutineIEnumerator);
        }

        private IEnumerator ShowGenerationOrderCoroutine()
        {
            if (_mainGeneratedPath == null || _mainGeneratedPath.Count == 0)
            {
                yield break;
            }
            
            Vector2Int lastRoomPosition = _mainGeneratedPath[0];
            Vector3 roomSize = finalLevelLayout.roomTiles[0, 0].structureData.size.ToVector3();
            
            for (int i = 1; i < _mainGeneratedPath.Count - 1; i++)
            {
                Vector2Int newPos = _mainGeneratedPath[i];
                Vector3 startPos = Vector3.Scale(roomSize, lastRoomPosition.ToVector3() + Vector3.one / 2f);
                Vector3 endPos = Vector3.Scale(roomSize, newPos.ToVector3() + Vector3.one / 2f);
                Debug.DrawLine(startPos, endPos, new Color32	(164,197,234, 255), 5f, false);
                
                lastRoomPosition = newPos;
                
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
            }
        }

        public void ShowShortestPath()
        {
            if (_showShortestPathCoroutine != null)
            {
                StopCoroutine(_showShortestPathCoroutine);
            }

            _showShortestPathCoroutineIEnumerator = ShowShortestPathCoroutine();
            _showShortestPathCoroutine = StartCoroutine(_showShortestPathCoroutineIEnumerator);
        }

        private IEnumerator ShowShortestPathCoroutine()
        {
            if (_openList == null)
            {
                yield break;
            }
            
            APoint lastPoint = _openList.LastOrDefault();

            if (lastPoint == null)
            {
                yield break;
            }

            List<Vector2Int> path = new () { lastPoint.Pos };
            int maxIterations = 5000;
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                APoint newPoint = lastPoint.Parent;
                if (newPoint == null)
                {
                    break;
                }
                
                path.Add(newPoint.Pos);
                lastPoint = newPoint;
            }

            path.Reverse();
            Vector2Int prevPos = path[0];
            
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int newPos = path[i];
                Debug.DrawLine(prevPos.ToVector3() + Vector3.one / 2f, newPos.ToVector3() + Vector3.one / 2f, new Color32	(164,197,234, 255), 5f, false);
                
                prevPos = newPos;
                
                yield return null;
                yield return null;
                yield return null;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SmartLevelGenerator))]
    public class RoomGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(5);

            HandleGenerationButton();
            
            GUILayout.Space(15);

            GUILayout.Label("Visualization");
            
            HandleShortestPathButton();

            HandleGenerationOrderButton();
            
            GUILayout.Space(15);

            // if (GUILayout.Button("GenerateTest"))
            // {
            //     var smartLevelGenerator = target as SmartLevelGenerator;
            //
            //     if (smartLevelGenerator == null)
            //     {
            //         return;
            //     }
            //     
            //     const int tests = 10000;
            //     int successes = 0;
            //     int fails = 0;
            //     for (int i = 0; i < tests; i++)
            //     {
            //         var generated = smartLevelGenerator.GenerateLoop(i);
            //
            //         if (generated)
            //         {
            //             successes += 1;
            //         }
            //         else
            //         {
            //             fails += 1;
            //         }
            //     }
            //     
            //     Debug.Log($"Tested {tests} attempts. {successes} succeeded and {fails} failed.");
            // }
        }
        
        private void HandleShortestPathButton()
        {
            if (GUILayout.Button("Show shortest path") == false)
            {
                return;
            }

            SmartLevelGenerator smartLevelGenerator = target as SmartLevelGenerator;

            if (smartLevelGenerator != null)
            {
                smartLevelGenerator.ShowShortestPath();
            }
        }
        
        private void HandleGenerationOrderButton()
        {
            if (GUILayout.Button("Show generation order") == false)
            {
                return;
            }

            SmartLevelGenerator smartLevelGenerator = target as SmartLevelGenerator;

            if (smartLevelGenerator != null)
            {
                smartLevelGenerator.ShowGenerationOrder();
            }
        }

        private void HandleGenerationButton()
        {
            if (GUILayout.Button("Generate") == false)
            {
                return;
            }

            SmartLevelGenerator smartLevelGenerator = target as SmartLevelGenerator;

            if (smartLevelGenerator != null)
            {
                smartLevelGenerator.StartGeneration();
            }
        }
    }
#endif
}