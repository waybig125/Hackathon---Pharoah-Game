using UnityEngine;
using UnityEngine.AI;

namespace TheAlchemistsCrypt.AI
{
    public class ZombieAI : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Transform player;
        private float attackDistance = 2.5f;
        private float checkInterval = 0.5f;
        private float timer;

        private Animator animator;
        private string currentAnimState = "";

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            agent.speed = 4.0f;
            agent.stoppingDistance = attackDistance;
            
            FindPlayer();
        }

        private void FindPlayer()
        {
            // Tag search
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else {
                // Component search - More robust
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.transform;
                else {
                    var cam = Camera.main;
                    if (cam != null) player = cam.transform;
                }
            }
        }

        private void Update()
        {
            if (player == null) {
                PlayAnimation("Idle");
                timer += Time.deltaTime;
                if (timer >= checkInterval) {
                    FindPlayer();
                    timer = 0;
                }
                return;
            }

            agent.SetDestination(player.position);

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackDistance) {
                PlayAnimation("Attack");
                // Attack logic (Damage Player)
                var health = player.GetComponentInParent<TheAlchemistsCrypt.Player.PlayerHealth>();
                if (health != null) health.TakeDamage(10f * Time.deltaTime);
            }
            else if (agent.velocity.sqrMagnitude > 0.1f) {
                PlayAnimation("Walk");
            }
            else {
                PlayAnimation("Idle");
            }
        }

        private void LateUpdate()
        {
            // Enforce upright rotation: local X must be 0 and local Z must be 0
            // This prevents them from falling or tilting during physics/agent movement
            Vector3 rot = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(0f, rot.y, 0f);
        }

        private void PlayAnimation(string stateName)
        {
            if (animator != null && currentAnimState != stateName) {
                currentAnimState = stateName;
                animator.CrossFadeInFixedTime(stateName, 0.2f);
            }
        }
    }
}
