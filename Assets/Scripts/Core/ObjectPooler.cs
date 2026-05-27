using System.Collections.Generic;
using UnityEngine;

namespace TheAlchemistsCrypt.Core
{
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance;

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        }

        public List<Pool> pools;
        public Dictionary<string, Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();

            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(pool.tag, objectPool);
            }
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
                return null;
            }

            Queue<GameObject> queue = poolDictionary[tag];
            GameObject objectToSpawn = null;

            // Dequeue until we find a valid non-destroyed GameObject, or the queue runs dry
            int initialCount = queue.Count;
            for (int i = 0; i < initialCount; i++)
            {
                if (queue.Count == 0) break;
                objectToSpawn = queue.Dequeue();
                if (objectToSpawn != null)
                {
                    break;
                }
            }

            // If all objects were destroyed or the queue is empty, instantiate a new one
            if (objectToSpawn == null)
            {
                Pool pool = null;
                foreach (Pool p in pools)
                {
                    if (p.tag == tag)
                    {
                        pool = p;
                        break;
                    }
                }
                if (pool != null && pool.prefab != null)
                {
                    objectToSpawn = Instantiate(pool.prefab);
                }
                else
                {
                    Debug.LogError("Pool with tag " + tag + " has no prefab or is missing.");
                    return null;
                }
            }

            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;

            queue.Enqueue(objectToSpawn);

            return objectToSpawn;
        }
    }
}
