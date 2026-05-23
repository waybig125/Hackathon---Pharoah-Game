using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Gameplay
{
    public class EscapeManager : MonoBehaviour
    {
        public static EscapeManager Instance;
        
        [Header("State")]
        public bool hasKey = false;
        public bool hasEscaped = false;
        private bool isEscaping = false;
        private float escapeTimer = 0f;
        private float papyrusCollectFeedbackTimer = 0f;
        
        [Header("UI References")]
        private GameObject promptUiGo;
        private Text promptText;
        private GameObject victoryUiGo;
        private GameObject deathUiGo;

        public GameObject keyObj;
        public GameObject boatObj;
        private bool nearKey = false;
        private bool nearBoat = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            SpawnKey();
            SpawnBoat();
            SetupUI();
        }

        private void SetupUI()
        {
            var canvas = GameObject.Find("P_LPSP_UI_Canvas");
            if (canvas == null) canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            promptUiGo = new GameObject("ProximityPrompt");
            promptUiGo.transform.SetParent(canvas.transform, false);
            var rect = promptUiGo.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 100f);
            rect.sizeDelta = new Vector2(600, 50);

            promptText = promptUiGo.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 24;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            var outline = promptUiGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            promptUiGo.SetActive(false);
        }

        private void SpawnKey()
        {
            Vector3 chosenLoc;
            var plazas = new List<GameObject>();
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo) if (go.name.Contains("Plaza")) plazas.Add(go);

            if (plazas.Count > 0)
            {
                var targetPlaza = plazas[Random.Range(0, plazas.Count)];
                chosenLoc = targetPlaza.transform.position;
                Debug.Log($"[EscapeManager] Spawning Papyrus at random Plaza: {targetPlaza.name} at {chosenLoc}");
            }
            else
            {
                Vector3[] spawnLocations = new Vector3[]
                {
                    new Vector3(250f, 0f, 350f), 
                    new Vector3(-280f, 0f, 250f), 
                    new Vector3(80f, 0f, 450f)    
                };
                chosenLoc = spawnLocations[Random.Range(0, spawnLocations.Length)];
                Debug.Log($"[EscapeManager] No Plazas found, spawning Papyrus at fallback location: {chosenLoc}");
            }
            
            var terrain = Terrain.activeTerrain;
            if (terrain != null) chosenLoc.y = terrain.SampleHeight(chosenLoc) + 0.8f;

            // Load custom model
            GameObject prefab = Resources.Load<GameObject>("papyrus");
            if (prefab != null)
            {
                keyObj = Instantiate(prefab, chosenLoc, Quaternion.identity);
                keyObj.transform.localScale = Vector3.one * 0.4f; 
            }
            else
            {
                keyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); 
                keyObj.transform.position = chosenLoc;
                keyObj.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
                keyObj.transform.rotation = Quaternion.Euler(0, 0, 90f); 
            }
            keyObj.name = "AncientPapyrus";
            keyObj.SetActive(true);

            // Add a point light to the papyrus so it glows and is visible
            var lightGo = new GameObject("PapyrusLight", typeof(Light));
            lightGo.transform.SetParent(keyObj.transform);
            lightGo.transform.localPosition = Vector3.up * 0.5f;
            var l = lightGo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.85f, 0.3f);
            l.intensity = 20f;
            l.range = 30f;

            // Create particle beam GameObject
            var beamGo = new GameObject("PapyrusBeaconBeam");
            beamGo.transform.SetParent(keyObj.transform);
            beamGo.transform.localPosition = Vector3.zero;
            beamGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Point straight up relative to parent
            beamGo.transform.localScale = Vector3.one;

            var ps = beamGo.AddComponent<ParticleSystem>();
            var psr = beamGo.GetComponent<ParticleSystemRenderer>();

            // Stop the particle system before configuration to avoid Duration warnings
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 4.0f;
            main.startSpeed = 25f;
            main.startSize = 0.6f;
            main.startColor = new Color(1.0f, 0.85f, 0.3f, 0.8f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // Makes particles rise nicely in world space as the parent floats/moves

            var emission = ps.emission;
            emission.rateOverTime = 80f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.4f;
            shape.angle = 1.0f; // Very narrow cone to create a beam-like effect

            // Setup fade out over lifetime so particles disappear smoothly
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0.0f), new GradientColorKey(new Color(1f, 0.6f, 0.1f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader == null) particleShader = Shader.Find("Lit");
            
            Material particleMat = new Material(particleShader);
            particleMat.name = "PapyrusBeamParticleMat";

            // Generate a simple soft circle texture for particles to prevent hard square blocks
            int pTexSize = 16;
            Texture2D circleTex = new Texture2D(pTexSize, pTexSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < pTexSize; y++)
            {
                for (int x = 0; x < pTexSize; x++)
                {
                    float dx = (x - (pTexSize - 1) * 0.5f) / ((pTexSize - 1) * 0.5f);
                    float dy = (y - (pTexSize - 1) * 0.5f) / ((pTexSize - 1) * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1.0f - dist);
                    alpha = alpha * alpha; // Soft edge
                    circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            circleTex.Apply();

            Color particleColor = new Color(1f, 0.85f, 0.3f, 0.8f);
            if (particleShader.name != null && particleShader.name.Contains("Particles"))
            {
                particleMat.SetTexture("_BaseMap", circleTex);
                particleMat.SetColor("_BaseColor", particleColor);
                particleMat.SetFloat("_Surface", 1f); // Transparent
                particleMat.SetFloat("_Blend", 0f); // Alpha blend
                particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive glow
                particleMat.SetInt("_ZWrite", 0);
                particleMat.DisableKeyword("_ALPHATEST_ON");
                particleMat.EnableKeyword("_ALPHABLEND_ON");
                particleMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.3f) * 12f);
                particleMat.EnableKeyword("_EMISSION");
                particleMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                if (particleMat.HasProperty("_MainTex")) particleMat.SetTexture("_MainTex", circleTex);
                if (particleMat.HasProperty("_Color")) particleMat.SetColor("_Color", particleColor);
                particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                particleMat.SetInt("_ZWrite", 0);
                particleMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            
            psr.sharedMaterial = particleMat;

            // Start the system after setup
            ps.Play();

            // Register with Minimap
            if (TheAlchemistsCrypt.UI.MinimapUI.Instance != null)
            {
                TheAlchemistsCrypt.UI.MinimapUI.Instance.RegisterDynamicStaticIcon(keyObj, new Vector2(16, 16), TheAlchemistsCrypt.UI.MinimapUI.Instance.keySprite);
            }
            
            var col = keyObj.GetComponent<Collider>();
            if (col == null) col = keyObj.AddComponent<MeshCollider>();
            if (col is MeshCollider mc) mc.convex = true;
            col.isTrigger = true;
            keyObj.AddComponent<Floater>();
        }

        private void ConvertBoatMaterials(GameObject boat)
        {
            var renderers = boat.GetComponentsInChildren<Renderer>(true);
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) return;

            foreach (var r in renderers)
            {
                var mats = r.materials;
                if (mats == null) continue;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var newMat = new Material(urpShader);
                    newMat.name = mats[i].name + "_URP";
                    newMat.SetColor("_BaseColor", new Color(0.22f, 0.15f, 0.09f)); // Dark Wood
                    newMat.SetFloat("_Smoothness", 0.1f);
                    newMat.SetFloat("_Metallic", 0.0f);
                    mats[i] = newMat;
                }
                r.materials = mats;
            }
        }

        private void SpawnBoat()
        {
            Vector3 spawnPos = new Vector3(0f, 1.8f, -104f);
            
            GameObject prefab = Resources.Load<GameObject>("boat");
            if (prefab != null)
            {
                // Set the boat rotation so it is upright, and height so it floats neatly
                boatObj = Instantiate(prefab, spawnPos, Quaternion.Euler(-90f, 180f, 0f));
                boatObj.transform.localScale = Vector3.one * 0.18f; 
                ConvertBoatMaterials(boatObj);
            }
            else
            {
                boatObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boatObj.transform.position = spawnPos;
                boatObj.transform.localScale = new Vector3(2f, 1f, 4f);
            }
            boatObj.name = "EscapeBoat";
            boatObj.SetActive(true);

            var boatLightGo = new GameObject("BoatBeacon", typeof(Light));
            boatLightGo.transform.SetParent(boatObj.transform);
            boatLightGo.transform.localPosition = Vector3.up * 3f;
            var bl = boatLightGo.GetComponent<Light>();
            bl.type = LightType.Point;
            bl.color = new Color(1f, 0.6f, 0.2f); // Warm sunset glow
            bl.intensity = 15f;
            bl.range = 25f;

            if (TheAlchemistsCrypt.UI.MinimapUI.Instance != null)
            {
                TheAlchemistsCrypt.UI.MinimapUI.Instance.RegisterDynamicStaticIcon(boatObj, new Vector2(24, 24), TheAlchemistsCrypt.UI.MinimapUI.Instance.boatSprite);
            }
            
            var col = boatObj.GetComponent<Collider>();
            if (col == null) col = boatObj.AddComponent<MeshCollider>();
            if (col is MeshCollider mc) mc.convex = true;
            col.isTrigger = true;
        }

        private void StartEscapeSequence(GameObject player)
        {
            hasEscaped = true;
            isEscaping = true;
            escapeTimer = 0f;

            // Disable player controller movement to stop inputs
            var pc = player.GetComponent<TheAlchemistsCrypt.Player.PlayerController>();
            if (pc != null) pc.enabled = false;

            var ph = player.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
            if (ph != null) ph.enabled = false;

            var punch = player.GetComponent<PunchCombat>();
            if (punch != null) punch.enabled = false;

            var focus = player.GetComponentInChildren<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
            if (focus != null) focus.enabled = false;

            // Disable all Infima Games behaviours (movement, inputs, shooting) to prevent physical overrides
            var infimaBehaviors = player.GetComponentsInChildren<MonoBehaviour>();
            foreach (var comp in infimaBehaviors)
            {
                if (comp.GetType().Namespace != null && comp.GetType().Namespace.StartsWith("InfimaGames"))
                {
                    comp.enabled = false;
                }
            }

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
            }

            // Snugly seat the player on the escape boat
            player.transform.position = boatObj.transform.position + new Vector3(0f, 1.2f, 0f);
            
            // Point the player to look straight back at the city/coast
            player.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f)); // Face +Z (inland)

            var playerCam = player.GetComponentInChildren<Camera>();
            if (playerCam != null)
            {
                playerCam.transform.localRotation = Quaternion.Euler(10f, 0f, 0f); // Cinematic slight downward tilt
            }

            if (promptUiGo != null)
            {
                promptUiGo.SetActive(true);
                if (promptText != null)
                {
                    promptText.color = new Color(0.2f, 1f, 1f); // Cyan
                    promptText.text = "ESCAPING THE CRYPT...";
                }
            }

            TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_taunt_01");
            TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_pickup", false, 1.0f);
        }

        private void Update()
        {
            if (hasEscaped)
            {
                if (isEscaping)
                {
                    escapeTimer += Time.deltaTime;
                    
                    // Smoothly sail out to sea (Z decreases)
                    if (boatObj != null)
                    {
                        boatObj.transform.position += new Vector3(0f, 0f, -4f * Time.deltaTime);
                    }
                    
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        player.transform.position = boatObj.transform.position + new Vector3(0f, 1.2f, 0f);
                    }

                    if (escapeTimer >= 5.0f)
                    {
                        isEscaping = false;
                        WinGame();
                    }
                }
                return;
            }

            if (!TheAlchemistsCrypt.UI.MobileHUDButtons.HasStartedGame) return;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;

            float distToKey = keyObj != null ? Vector3.Distance(playerObj.transform.position, keyObj.transform.position) : 9999f;
            float distToBoat = boatObj != null ? Vector3.Distance(playerObj.transform.position, boatObj.transform.position) : 9999f;

            nearKey = !hasKey && distToKey < 15f; 
            nearBoat = distToBoat < 18f;

            if (nearKey)
            {
                if (promptUiGo != null)
                {
                    promptUiGo.SetActive(true);
                    promptText.color = Color.white;
                }
                if (distToKey < 3.5f)
                {
                    hasKey = true;
                    if (keyObj != null) Destroy(keyObj);
                    papyrusCollectFeedbackTimer = 5.0f; // Keep showing explicit pickup banner for 5 seconds
                    
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_taunt_01");
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_pickup", false, 0.8f);
                }
                else
                {
                    if (promptText != null) promptText.text = "ANCIENT PAPYRUS DETECTED! MOVE CLOSER TO AUTO-PICKUP";
                }
            }
            else if (nearBoat)
            {
                if (promptUiGo != null) promptUiGo.SetActive(true);
                if (hasKey)
                { 
                    if (distToBoat < 15.0f)
                    {
                        StartEscapeSequence(playerObj);
                    }
                    else
                    {
                        if (promptText != null)
                        {
                            promptText.color = new Color(0.2f, 1f, 0.2f); // Green
                            promptText.text = "REACH THE BOAT TO ESCAPE!";
                        }
                    }
                }
                else
                {
                    if (promptText != null)
                    {
                        promptText.color = new Color(1f, 0.3f, 0.3f); // Red
                        promptText.text = "THE ANCIENT BOAT. YOU NEED THE PAPYRUS TO DEPART.";
                    }
                }
            }
            else
            {
                if (papyrusCollectFeedbackTimer > 0f)
                {
                    papyrusCollectFeedbackTimer -= Time.deltaTime;
                    if (promptUiGo != null)
                    {
                        promptUiGo.SetActive(true);
                        if (promptText != null)
                        {
                            promptText.color = new Color(1f, 0.8f, 0.2f); // Gold feedback
                            promptText.text = "ANCIENT PAPYRUS COLLECTED! RUN TO THE BOAT!";
                        }
                    }
                }
                else if (hasKey)
                {
                    if (promptUiGo != null)
                    {
                        promptUiGo.SetActive(true);
                        if (promptText != null)
                        {
                            promptText.color = new Color(1f, 0.8f, 0.2f); // Gold objective
                            promptText.text = "OBJECTIVE: ESCAPE VIA THE BOAT AT THE COAST!";
                        }
                    }
                }
                else
                {
                    if (promptUiGo != null) promptUiGo.SetActive(false);
                }
            }
        }

        private void WinGame()
        {
            hasEscaped = true;
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
                TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.ShowVictoryScreen();
        }
    }

    public class Floater : MonoBehaviour {
        private float startY;
        private void Start() { startY = transform.position.y; }
        private void Update() {
            transform.position = new Vector3(transform.position.x, startY + Mathf.Sin(Time.time * 2f) * 0.5f, transform.position.z);
            transform.Rotate(0, 45f * Time.deltaTime, 0);
        }
    }
}
