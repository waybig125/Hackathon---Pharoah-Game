using UnityEngine;
using UnityEngine.AI;

namespace TheAlchemistsCrypt.AI
{
    public class ZombieAI : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Transform player;
        private float attackDistance = 2f;
        private float checkInterval = 0.5f;
        private float timer;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            
            agent.speed = 3.5f;
            agent.stoppingDistance = attackDistance;
            
            FindPlayer();
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else {
                // Fallback search
                var cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
        }

        private void Update()
        {
            if (player == null) {
                timer += Time.deltaTime;
                if (timer >= checkInterval) {
                    FindPlayer();
                    timer = 0;
                }
                return;
            }

            agent.SetDestination(player.position);

            if (Vector3.Distance(transform.position, player.position) <= attackDistance) {
                // Attack logic (Damage Player)
                var health = player.GetComponentInParent<TheAlchemistsCrypt.Player.PlayerHealth>();
                if (health != null) health.TakeDamage(10f * Time.deltaTime);
            }
        }
    }
}
