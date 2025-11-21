using System.Collections.Generic;
using UnityEngine;

namespace NavalCommand.Infrastructure
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
        private Dictionary<int, int> spawnedObjectsMap = new Dictionary<int, int>(); // Map InstanceID to PrefabID

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject obj = Spawn(prefab.gameObject, position, rotation);
            return obj.GetComponent<T>();
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            int poolKey = prefab.GetInstanceID();

            if (!poolDictionary.ContainsKey(poolKey))
            {
                poolDictionary.Add(poolKey, new Queue<GameObject>());
            }

            GameObject objectToSpawn;

            if (poolDictionary[poolKey].Count > 0)
            {
                objectToSpawn = poolDictionary[poolKey].Dequeue();
            }
            else
            {
                objectToSpawn = Instantiate(prefab);
                // Map the new object instance ID to the prefab ID so we know where to return it
                int instanceId = objectToSpawn.GetInstanceID();
                if (!spawnedObjectsMap.ContainsKey(instanceId))
                {
                    spawnedObjectsMap.Add(instanceId, poolKey);
                }
                else
                {
                    spawnedObjectsMap[instanceId] = poolKey;
                }
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            return objectToSpawn;
        }

        public void Despawn(GameObject obj)
        {
            int objId = obj.GetInstanceID();

            if (spawnedObjectsMap.TryGetValue(objId, out int poolKey))
            {
                obj.SetActive(false);
                
                if (!poolDictionary.ContainsKey(poolKey))
                {
                    poolDictionary[poolKey] = new Queue<GameObject>();
                }
                
                poolDictionary[poolKey].Enqueue(obj);
            }
            else
            {
                Debug.LogWarning($"PoolManager: Object {obj.name} was not spawned via PoolManager. Destroying it.");
                Destroy(obj);
            }
        }
    }
}
