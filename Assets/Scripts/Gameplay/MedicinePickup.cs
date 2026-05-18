using UnityEngine;

namespace TheAlchemistsCrypt.Gameplay
{
    public class MedicinePickup : MonoBehaviour
    {
        [Header("Healing Settings")]
        public float healAmount = 10f;

        [Header("Movement")]
        public float rotationSpeed = 60f;
        public float hoverAmplitude = 0.2f;
        public float hoverFrequency = 1.5f;

        private Vector3 startPos;
        private Light glowLight;
        private GameObject crystalVisual;

        private void Start()
        {
            startPos = transform.position;

            // Procedurally build a beautiful glowing Emerald Crystal diamond if no custom mesh exists
            crystalVisual = new GameObject("CrystalVisual");
            crystalVisual.transform.SetParent(transform, false);
            crystalVisual.transform.localPosition = Vector3.zero;

            // Generate an elegant double-pyramid crystal mesh
            MeshFilter filter = crystalVisual.AddComponent<MeshFilter>();
            MeshRenderer renderer = crystalVisual.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = "AlchemicalCrystal";

            // 6 vertices: 4 around the equator, 1 top tip, 1 bottom tip
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(0, 0.4f, 0),    // 0: Top tip
                new Vector3(0.2f, 0, 0.2f),  // 1: Equator front-right
                new Vector3(-0.2f, 0, 0.2f), // 2: Equator front-left
                new Vector3(-0.2f, 0, -0.2f),// 3: Equator back-left
                new Vector3(0.2f, 0, -0.2f), // 4: Equator back-right
                new Vector3(0, -0.4f, 0)    // 5: Bottom tip
            };

            // Triangles for double pyramid (8 faces)
            int[] triangles = new int[]
            {
                // Top pyramid
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 1,
                // Bottom pyramid
                5, 2, 1,
                5, 3, 2,
                5, 4, 3,
                5, 1, 4
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.mesh = mesh;

            // Setup custom glowing alchemical material (Emerald/Jade)
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.1f, 0.9f, 0.3f, 1f));
            mat.SetColor("_EmissionColor", new Color(0.05f, 0.6f, 0.15f, 1f) * 2f);
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Smoothness", 0.9f);
            renderer.material = mat;

            // Add clean procedural collider
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.8f, 1.2f, 0.8f);

            // Add smooth green alchemical light glow
            var ltGo = new GameObject("GlowLight");
            ltGo.transform.SetParent(transform, false);
            ltGo.transform.localPosition = Vector3.zero;
            glowLight = ltGo.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0.1f, 0.9f, 0.2f);
            glowLight.intensity = 6.0f;
            glowLight.range = 5.0f;
            glowLight.shadows = LightShadows.None;
        }

        private void Update()
        {
            // Floating & rotating motion
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
            float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            transform.position = new Vector3(startPos.x, startPos.y + hoverOffset, startPos.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Detect player
            var player = other.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
            if (player == null) player = other.GetComponentInParent<TheAlchemistsCrypt.Player.PlayerHealth>();

            if (player != null)
            {
                player.Heal(healAmount);

                // Play custom procedural emerald spark particle burst
                var sparkGo = new GameObject("SparkBurst");
                sparkGo.transform.position = transform.position;
                var system = sparkGo.AddComponent<ParticleSystem>();
                
                var main = system.main;
                main.startColor = new Color(0.2f, 1f, 0.4f);
                main.startSize = 0.15f;
                main.startSpeed = 3f;
                main.duration = 0.5f;
                main.loop = false;
                
                var emission = system.emission;
                emission.rateOverTime = 0f;
                emission.burstCount = 1;
                emission.SetBurst(0, new ParticleSystem.Burst(0f, 25));

                system.Play();
                Destroy(sparkGo, 1.0f);

                // Destroy the pickup
                Destroy(gameObject);
            }
        }
    }
}
