using System.Collections;
using UnityEngine;

namespace TheAlchemistsCrypt.AI
{
    public class MummySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int maxMummies = 40;
        [SerializeField] private float spawnInterval = 5.0f;
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

        private IEnumerator AutoSpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

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
                    SpawnSingleMummy(maxExistingId + 1);
                }
            }
        }

        private void SpawnSingleMummy(int id)
        {
            string fbxPath = "Assets/Mummy_Assets/mummy_base.fbx";
            
            // Find player or camera to center spawn around
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.gameObject;
            }

            Vector3 spawnCenter = player != null ? player.transform.position : Vector3.zero;

            // Load animator controller
            RuntimeAnimatorController controller = null;
#if UNITY_EDITOR
            controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Mummy_Assets/MummyTestController.controller");
#endif

            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
#endif
            if (prefab == null) return;

            // Choose a random spawn position around the player at a tactical distance of 30-45 units
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(30f, 45f);
            Vector3 spawnPos = spawnCenter + new Vector3(Mathf.Cos(angle) * distance, 0.5f, Mathf.Sin(angle) * distance);

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 50f, UnityEngine.AI.NavMesh.AllAreas)) {
                spawnPos = hit.position;
            }

            GameObject go = Instantiate(prefab);
            go.name = "Mummy_Dynamic_" + id;
            go.transform.position = spawnPos;
            go.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

            // Programmatically assign high-fidelity URP Lit materials to ensure they are 100% visible and beautiful
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            Texture2D diffTex = null;
            Texture2D normTex = null;
#if UNITY_EDITOR
            diffTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mummy_Assets/texture/texture_diffuse.png");
            normTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mummy_Assets/texture/texture_normal.png");
#endif
            foreach (Renderer r in renderers) {
                if (r == null) continue;
                Material[] sharedMats = r.sharedMaterials;
                for (int j = 0; j < sharedMats.Length; j++) {
                    if (urpShader != null) {
                        Material uMat = new Material(urpShader);
                        if (diffTex != null) uMat.SetTexture("_BaseMap", diffTex);
                        if (normTex != null) {
                            uMat.SetTexture("_BumpMap", normTex);
                            uMat.EnableKeyword("_NORMALMAP");
                        }
                        sharedMats[j] = uMat;
                    }
                }
                r.sharedMaterials = sharedMats;
            }

            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            Avatar avatar = null;
#if UNITY_EDITOR
            var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in subAssets) {
                if (asset is Avatar av) {
                    avatar = av;
                    break;
                }
            }
#endif
            if (avatar != null) anim.avatar = avatar;

            var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null) agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.8f;
            agent.stoppingDistance = 3.2f; // Adjusted for 1.6x scale
            agent.height = 2.0f;
            agent.radius = 0.4f;

            var col = go.GetComponent<CapsuleCollider>();
            if (col == null) col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.0f, 0); 
            col.height = 2.0f;
            col.radius = 0.4f;

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var ai = go.GetComponent<ZombieAI>();
            if (ai == null) ai = go.AddComponent<ZombieAI>();
            ai.mummyId = id;

            Debug.Log($"MummySpawner: Successfully spawned active dynamic mummy with ID {id} at {spawnPos}");
        }
    }
}
