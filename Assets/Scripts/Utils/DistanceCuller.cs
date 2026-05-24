using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Utils
{
    public class DistanceCuller : MonoBehaviour
    {
        [System.Serializable]
        public class CullableObject
        {
            public Transform transform;
            public Renderer[] renderers;
            public Collider[] colliders;
            public bool isCurrentlyVisible = true;
        }

        public float cullDistance = 180f;
        public float checkInterval = 0.3f;

        private Transform playerTransform;
        private List<CullableObject> cullables = new List<CullableObject>();
        private bool isRunning = true;

        private void Start()
        {
            FindPlayer();
            InitializeCullables();
            StartCoroutine(CullingLoop());
        }

        private void FindPlayer()
        {
            var movement = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Movement>();
            if (movement != null)
            {
                playerTransform = movement.transform;
            }
        }

        private void InitializeCullables()
        {
            cullables.Clear();
            
            // Find the "City" root object
            var cityRoot = GameObject.Find("City");
            if (cityRoot == null)
            {
                Debug.LogWarning("[DistanceCuller] Could not find 'City' root GameObject. Culler is inactive.");
                return;
            }

            // Find all renderers under City
            Renderer[] allRenderers = cityRoot.GetComponentsInChildren<Renderer>(true);
            
            // We want to group renderers and colliders by their immediate child of City (e.g. house object, pillar folder, palm tree)
            // so we don't have thousands of separate cullable items. Grouping by their top-level city sub-hierarchy is more performant.
            Dictionary<Transform, List<Renderer>> groupedRenderers = new Dictionary<Transform, List<Renderer>>();
            Dictionary<Transform, List<Collider>> groupedColliders = new Dictionary<Transform, List<Collider>>();

            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                string nameLower = r.gameObject.name.ToLower();

                // Skip background backdrops, pyramids, sea, terrain, bounds, and anything related to player/enemies
                if (nameLower.Contains("pyramid") || 
                    nameLower.Contains("terrain") || 
                    nameLower.Contains("sea") || 
                    nameLower.Contains("water") || 
                    nameLower.Contains("bounds") ||
                    nameLower.Contains("player") ||
                    nameLower.Contains("weapon") ||
                    nameLower.Contains("zombie") ||
                    nameLower.Contains("mummy") ||
                    nameLower.Contains("pharaoh"))
                {
                    continue;
                }

                // Determine the group root (first child under cityRoot)
                Transform groupRoot = r.transform;
                while (groupRoot.parent != null && groupRoot.parent != cityRoot.transform)
                {
                    groupRoot = groupRoot.parent;
                }

                // Skip if groupRoot is one of the excluded large world objects
                string rootNameLower = groupRoot.gameObject.name.ToLower();
                if (rootNameLower.Contains("pyramid") || 
                    rootNameLower.Contains("terrain") || 
                    rootNameLower.Contains("sea") || 
                    rootNameLower.Contains("water") || 
                    rootNameLower.Contains("bounds"))
                {
                    continue;
                }

                if (!groupedRenderers.ContainsKey(groupRoot))
                {
                    groupedRenderers[groupRoot] = new List<Renderer>();
                    groupedColliders[groupRoot] = new List<Collider>();
                }
                groupedRenderers[groupRoot].Add(r);
            }

            // Collect colliders for those group roots
            Collider[] allColliders = cityRoot.GetComponentsInChildren<Collider>(true);
            foreach (var c in allColliders)
            {
                if (c == null) continue;
                Transform groupRoot = c.transform;
                while (groupRoot.parent != null && groupRoot.parent != cityRoot.transform)
                {
                    groupRoot = groupRoot.parent;
                }

                if (groupedColliders.ContainsKey(groupRoot))
                {
                    groupedColliders[groupRoot].Add(c);
                }
            }

            // Build our cullables list
            foreach (var kvp in groupedRenderers)
            {
                var groupRoot = kvp.Key;
                var renderersList = kvp.Value;
                var collidersList = groupedColliders[groupRoot];

                cullables.Add(new CullableObject
                {
                    transform = groupRoot,
                    renderers = renderersList.ToArray(),
                    colliders = collidersList.ToArray(),
                    isCurrentlyVisible = true
                });
            }

            Debug.Log($"[DistanceCuller] Initialized with {cullables.Count} cullable groups under City.");
        }

        private IEnumerator CullingLoop()
        {
            var wait = new WaitForSecondsRealtime(checkInterval);
            while (isRunning)
            {
                if (playerTransform == null)
                {
                    FindPlayer();
                }

                if (playerTransform != null && cullables.Count > 0)
                {
                    Vector3 playerPos = playerTransform.position;
                    float sqrCullDist = cullDistance * cullDistance;

                    for (int i = 0; i < cullables.Count; i++)
                    {
                        var cullable = cullables[i];
                        if (cullable.transform == null) continue;

                        float sqrDist = (cullable.transform.position - playerPos).sqrMagnitude;
                        bool shouldBeVisible = sqrDist <= sqrCullDist;

                        if (shouldBeVisible != cullable.isCurrentlyVisible)
                        {
                            cullable.isCurrentlyVisible = shouldBeVisible;
                            
                            // Enable/disable all renderers in group
                            for (int r = 0; r < cullable.renderers.Length; r++)
                            {
                                if (cullable.renderers[r] != null)
                                {
                                    cullable.renderers[r].enabled = shouldBeVisible;
                                }
                            }

                            // Enable/disable all colliders in group
                            for (int c = 0; c < cullable.colliders.Length; c++)
                            {
                                if (cullable.colliders[c] != null)
                                {
                                    cullable.colliders[c].enabled = shouldBeVisible;
                                }
                            }
                        }
                    }
                }

                yield return wait;
            }
        }

        private void OnDestroy()
        {
            isRunning = false;
        }
    }
}
