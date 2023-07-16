using System;
using System.Collections.Generic;
using SmartRooms.Utils;
using UnityEngine.Pool;
using UnityEngine;

namespace SmartRooms.Generator.Patterns
{
    [CreateAssetMenu(menuName = "SmartRooms/Generation/SpawnableObject")]
    public class SpawnableObject : ScriptableObject
    {
        private const bool PoolingCollectionCheck = true;
        
        [field: SerializeField] public GameObject ObjectPrefab { get; private set; }
        [field: SerializeField] public  Vector2 PositionOffset { get; private set; }
        [field: SerializeField] public  Vector2 RandomOffsetRangeX { get; private set; }
        [field: SerializeField] public  Vector2 RandomOffsetRangeY { get; private set; }
        [field: SerializeField] public float RotationOffset { get; private set; } = 0f;
        [field: SerializeField] public  bool FlipOnX { get; private set; } = false;
        [field: SerializeField] public bool FlipOnY { get; private set; } = false;
        [Tooltip("In percentage, where 100 means it always spawns")]
        [field: SerializeField] public float SpawnChance { get; private set; } = 100f;
        [field: SerializeField] public int MaxSpawned { get; private set; } = 10;
        [field: SerializeField] public bool DontSpawnOnEdge { get; private set; } = false;
        [field: SerializeField] public bool IgnoreSafeAreas { get; private set; } = false;
        
        [Tooltip("0-100, where 100 means it never spawns")]
        [field: SerializeField] public float FailChance { get; private set; } = 0f;
        
        [field: Header("Object Pooling")]
        [field: SerializeField] public int DefaultPoolCapacity { get; private set; } = 20;
        [field: SerializeField] public int MaxPoolSize { get; private set; } = 100;

        private IObjectPool<GameObject> _objectPool;
        private List<GameObject> _spawnedObjects = new();

        private static List<IObjectPool<GameObject>> _activePools = new();
        private static List<IObjectPool<GameObject>> _activeSpawnedObjects = new();
        
        private void Initialize()
        {
            _objectPool = new ObjectPool<GameObject>(() => PoolingUtils.CreateObject(ObjectPrefab),
                PoolingUtils.OnGetFromPool, PoolingUtils.OnReleaseToPool, PoolingUtils.OnDestroyPooledObject,
                PoolingCollectionCheck, DefaultPoolCapacity, MaxPoolSize);
            
            _activePools.Add(_objectPool);

            _spawnedObjects = new List<GameObject>();
            
            _activeSpawnedObjects.Add(_objectPool);
        }

        private void TryToInitialize()
        {
            if (_objectPool != null)
            {
                return;
            }

            Initialize();
        }

        public GameObject SpawnObject(Transform parent)
        {
            GameObject gameObject = SpawnObject();
            gameObject.transform.parent = parent;
            return gameObject;
        }

        public GameObject SpawnObject()
        {
            TryToInitialize();
            
            GameObject gameObject = _objectPool.Get();
            _spawnedObjects.Add(gameObject);
            return gameObject;
        }

        public void ReleaseObject(GameObject gameObject)
        {
            TryToInitialize();
            
            if (_spawnedObjects.Contains(gameObject))
            {
                _spawnedObjects.Remove(gameObject);
            }
            
            _objectPool.Release(gameObject);
        }

        public void ReleaseAllObjects()
        {
            TryToInitialize();
            
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedObjects[i] == null)
                {
                    _spawnedObjects.RemoveAt(i);
                    continue;
                }
                
                _objectPool.Release(_spawnedObjects[i]);
            }

            _spawnedObjects = new List<GameObject>();
        }
        
        // Cleaning this object because we don't want to use domain reload
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools()
        {
            for (int i = _activePools.Count - 1; i >= 0; i--)
            {
                if (_activePools[i] == null)
                {
                    _activePools.RemoveAt(i);
                    continue;
                }

                _activePools[i].Clear();
            }

            for (int i = _activeSpawnedObjects.Count - 1; i >= 0; i--)
            {
                if (_activeSpawnedObjects[i] == null)
                {
                    _activeSpawnedObjects.RemoveAt(i);
                    continue;
                }
                
                _activeSpawnedObjects[i].Clear();
            }
        }
    }
}

