using UnityEngine;

namespace TheAlchemistsCrypt.AI
{
    public class MummySpawner : MonoBehaviour
    {
        private void Start()
        {
            // Spawn the active mummies at runtime!
            SpawnMummies();
        }

        private void SpawnMummies()
        {
            // Check if they are already in the scene (avoid double spawning)
            if (GameObject.Find("Mummy_Base_Active") != null) return;

            string[] fbxPaths = {
                "Assets/Mummy_Assets/mummy_base.fbx",
                "Assets/Mummy_Assets/mummy_base.fbx",
                "Assets/Mummy_Assets/mummy_base.fbx"
            };

            string[] names = { "Mummy_Alpha", "Mummy_Beta", "Mummy_Gamma" };

            // Find player or camera
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

            for (int i = 0; i < fbxPaths.Length; i++) {
                GameObject prefab = null;
#if UNITY_EDITOR
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPaths[i]);
#endif
                if (prefab == null) continue;

                float angle = i * (360f / fbxPaths.Length) * Mathf.Deg2Rad;
                Vector3 spawnPos = spawnCenter + new Vector3(Mathf.Cos(angle) * 40f, 0.5f, Mathf.Sin(angle) * 40f);

                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 50f, UnityEngine.AI.NavMesh.AllAreas)) {
                    spawnPos = hit.position;
                }

                GameObject go = Instantiate(prefab);
                go.name = names[i];
                go.transform.position = spawnPos;
                go.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

                var anim = go.GetComponent<Animator>();
                if (anim == null) anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;

                Avatar avatar = null;
#if UNITY_EDITOR
                var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbxPaths[i]);
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
            }

            Debug.Log("MummySpawner: Successfully spawned active mummies dynamically at runtime!");
        }
    }
}
