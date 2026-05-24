using System.Collections;
using UnityEngine;

namespace TheAlchemistsCrypt.AI
{
    public class MummySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int maxMummies = 20;
        [SerializeField] private float spawnInterval = 25.0f;
        [SerializeField] private int initialSpawnCount = 3;

        private void Start()
        {
            // Spawn initial mummies at runtime
            for (int i = 0; i < initialSpawnCount; i++)
            {
                SpawnSingleMummy(i + 1);
            }

            // Auto-spawn HiveMindManager at runtime if not present
            if (GameObject.FindAnyObjectByType<HiveMindManager>() == null)
            {
                var hmGo = new GameObject("HiveMindManager");
                hmGo.AddComponent<HiveMindManager>();
            }

            // Start dynamic spawner loop
            StartCoroutine(AutoSpawnRoutine());
        }

        [SerializeField] private int pharaohSpawnInterval = 4; // every 4th wave
        private int waveCounter = 0;

        private IEnumerator AutoSpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                waveCounter++;

                var activeZombies = GameObject.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude);
                int aliveCount = 0;
                int maxExistingId = 0;
                
                foreach (var z in activeZombies)
                {
                    if (z != null && !z.IsDead)
                    {
                        aliveCount++;
                        if (z.mummyId > maxExistingId)
                        {
                            maxExistingId = z.mummyId;
                        }
                    }
                }

                if (aliveCount < maxMummies)
                {
                    if (waveCounter % pharaohSpawnInterval == 0)
                    {
                        // Boss Wave! Spawn Pharaoh
                        SpawnPharaoh(maxExistingId + 1);
                        aliveCount++; // Increment for the Pharaoh
                        
                        // Spawn guards only if we have less than 15 mummies present at the same time
                        if (aliveCount < 16)
                        {
                            int guardsToSpawn = Mathf.Min(6, maxMummies - aliveCount);
                            for (int i = 0; i < guardsToSpawn; i++)
                            {
                                SpawnSingleMummy(maxExistingId + 2 + i);
                                aliveCount++;
                            }
                        }
                    }
                    else
                    {
                        SpawnSingleMummy(maxExistingId + 1);
                        aliveCount++;
                    }
                }
            }
        }

        private void SpawnSingleMummy(int id)
        {
            // Find player or camera to center spawn around
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.gameObject;
            }

            Vector3 spawnCenter = player != null ? player.transform.position : Vector3.zero;

            // Load prefab directly from Resources
            GameObject prefab = Resources.Load<GameObject>("Mummy_Dynamic_Prefab");
            if (prefab == null) 
            {
                Debug.LogWarning("[MummySpawner] Mummy_Dynamic_Prefab not found in Resources! Please run Tools > Generate AI Prefabs.");
                return;
            }

            // Choose a random spawn position around the player at a tactical distance of 35-55 units
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(35f, 55f);
            Vector3 spawnPos = spawnCenter + new Vector3(Mathf.Cos(angle) * distance, 0.5f, Mathf.Sin(angle) * distance);

            // Snap to NavMesh, then reject positions south of the beach barrier (Z < -50)
            // This prevents mummies from spawning on the sea/beach area.
            UnityEngine.AI.NavMeshHit hit;
            int attempts = 0;
            while (attempts < 10)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 50f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    if (hit.position.z >= -50f) // Valid city-side position
                    {
                        spawnPos = hit.position;
                        break;
                    }
                }
                // Retry with a new random direction (bias north: clamp angle away from south)
                angle = Random.Range(30f, 330f) * Mathf.Deg2Rad; // avoid pure south angles
                distance = Random.Range(30f, 45f);
                spawnPos = spawnCenter + new Vector3(Mathf.Cos(angle) * distance, 0.5f, Mathf.Sin(angle) * distance);
                attempts++;
            }

            GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
            go.name = "Mummy_Dynamic_" + id;

            var ai = go.GetComponent<ZombieAI>();
            if (ai != null) ai.mummyId = id;

            Debug.Log($"[MummySpawner] Successfully spawned active dynamic mummy with ID {id} at {spawnPos}");

        }

        private void SpawnPharaoh(int id)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.gameObject;
            }

            Vector3 spawnCenter = player != null ? player.transform.position : Vector3.zero;

            GameObject prefab = Resources.Load<GameObject>("Pharaoh_Prefab");
            if (prefab == null) 
            {
                Debug.LogWarning("[MummySpawner] Pharaoh_Prefab not found in Resources! Please run Tools > Generate AI Prefabs.");
                return;
            }

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(30f, 45f);
            Vector3 spawnPos = spawnCenter + new Vector3(Mathf.Cos(angle) * distance, 0.5f, Mathf.Sin(angle) * distance);

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 50f, UnityEngine.AI.NavMesh.AllAreas)) {
                spawnPos = hit.position;
            }

            GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
            go.name = "Pharaoh_Prefab";

            var ai = go.GetComponent<ZombieAI>();
            if (ai != null) ai.mummyId = id;

            Debug.Log($"[MummySpawner] Boss Spawned: Pharaoh with ID {id} at {spawnPos}");
        }
    }
}
