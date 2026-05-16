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
                "Assets/Mummy_Assets/base.fbx",
                "Assets/Mummy_Assets/base_basic_pbr.fbx",
                "Assets/Mummy_Assets/base_basic_shaded.fbx"
            };

            string[] names = { "Mummy_Base_Active", "Mummy_PBR_Active", "Mummy_Shaded_Active" };

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
                Vector3 spawnPos = spawnCenter + new Vector3(Mathf.Cos(angle) * 15f, 0.5f, Mathf.Sin(angle) * 15f);

                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 20f, UnityEngine.AI.NavMesh.AllAreas)) {
                    spawnPos = hit.position;
                }

                GameObject go = Instantiate(prefab);
                go.name = names[i];
                go.transform.position = spawnPos;
                go.transform.localScale = new Vector3(250f, 250f, 250f);

                var anim = go.GetComponent<Animator>();
                if (anim == null) anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;

                var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent == null) agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = 3.8f;
                agent.stoppingDistance = 2.5f;

                var col = go.GetComponent<CapsuleCollider>();
                if (col == null) col = go.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.0036f, 0); 
                col.height = 0.0072f;
                col.radius = 0.0016f;

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
