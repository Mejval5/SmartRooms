using UnityEngine;

namespace SmartRooms.Utils
{
    public static class PoolingUtils
    {
        public static GameObject CreateObject(GameObject objectPrefab)
        {
            GameObject gameObject = Object.Instantiate(objectPrefab);
            return gameObject;
        }
        
        // Invoked when returning an item to the object pool
        public static void OnReleaseToPool(GameObject pooledObject)
        {
            pooledObject.SetActive(false);
        }
        
        // Invoked when retrieving the next item from the object pool
        public static void OnGetFromPool(GameObject pooledObject)
        {
        }

        // Invoked when we exceed the maximum number of pooled items (i.e. destroy the pooled object)
        public static void OnDestroyPooledObject(GameObject pooledObject)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(pooledObject);
            }
            else
            {
                Object.DestroyImmediate(pooledObject);
            }
        }
    }
}