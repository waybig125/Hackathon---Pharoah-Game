using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

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
        private float initialBoatY = 3.2f;

        [Header("Progression")]
        public int requiredKills = 20;
        public int currentKills = 0;
        public bool papyrusSpawned = false;
        
        [Header("UI References")]
        private GameObject promptUiGo;
        private TextMeshProUGUI promptText;
        private GameObject victoryUiGo;
        private GameObject deathUiGo;

        public GameObject keyObj;
        public GameObject boatObj;
        [HideInInspector] public bool nearKey = false;
        [HideInInspector] public bool nearBoat = false;
        [HideInInspector] public bool canPickupKey = false;
        [HideInInspector] public bool canEscape = false;

        // PERFORMANCE: Cache player reference — FindGameObjectWithTag is O(n) over all scene objects.
        // Previously this was called every single frame in Update(), which is ~60 O(n) searches/second.
        private GameObject cachedPlayer;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            SpawnBoat();
            SetupUI();
            // Cache player once at start — see Update() for null-check fallback
            cachedPlayer = GameObject.FindGameObjectWithTag("Player");
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

            promptText = promptUiGo.AddComponent<TextMeshProUGUI>();
            promptText.font = TMP_Settings.defaultFontAsset;
            promptText.fontSize = 24;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;
            promptText.outlineColor = Color.black;
            promptText.outlineWidth = 0.2f;
            promptUiGo.SetActive(false);
        }

        public void AddKill()
        {
            if (papyrusSpawned) return;
            
            currentKills++;
            if (currentKills >= requiredKills)
            {
                papyrusSpawned = true;
                SpawnKey();
                ShowNotification("The Ancient Papyrus has revealed itself!");
            }
        }

        private void ShowNotification(string message)
        {
            if (promptUiGo != null && promptText != null)
            {
                promptText.text = message;
                promptUiGo.SetActive(true);
                papyrusCollectFeedbackTimer = 4f; // Show for 4 seconds
            }
        }

        private void SpawnKey()
        {
            Vector3 chosenLoc;
            var plazas = new List<GameObject>();
            var allGo = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo)
            {
                if (go.name.Contains("Plaza"))
                {
                    float distToCentral = Vector3.Distance(new Vector3(go.transform.position.x, 0f, go.transform.position.z), new Vector3(16f, 0f, 48f));
                    float distToBoat = Vector3.Distance(new Vector3(go.transform.position.x, 0f, go.transform.position.z), new Vector3(0f, 0f, -74f));
                    if (distToCentral > 10f && distToBoat > 120f)
                    {
                        plazas.Add(go);
                    }
                }
            }

            if (plazas.Count > 0)
            {
                // Sort by Z descending to keep plazas farthest from the boat (Z = -104) at the front of the list
                plazas.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
                // Choose randomly from the top 30% of farthest plazas (or at least the first 2 if list is small)
                int rangeLimit = Mathf.Max(2, Mathf.RoundToInt(plazas.Count * 0.3f));
                rangeLimit = Mathf.Min(rangeLimit, plazas.Count);
                var targetPlaza = plazas[Random.Range(0, rangeLimit)];
                chosenLoc = targetPlaza.transform.position;
                Debug.Log($"[EscapeManager] Spawning Papyrus at far-end Plaza: {targetPlaza.name} at {chosenLoc}");
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

            // 1. Volumetric Light Shaft (Semi-transparent glowing cylinder)
            var beamGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beamGo.name = "PapyrusVolumetricBeam";
            beamGo.transform.SetParent(keyObj.transform);
            beamGo.transform.localPosition = Vector3.up * 150f;
            beamGo.transform.localRotation = Quaternion.identity;
            beamGo.transform.localScale = new Vector3(2.5f, 375f, 2.5f); // scaled to go high up (key is 0.4f scale)
            DestroyImmediate(beamGo.GetComponent<Collider>());

            Shader beamShader = Shader.Find("Custom/VolumetricBeam");
            if (beamShader == null) beamShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (beamShader == null) beamShader = Shader.Find("Universal Render Pipeline/Lit");
            if (beamShader == null) beamShader = Shader.Find("Lit");
            Material beamMat = new Material(beamShader);
            beamMat.name = "VolumetricBeamMat";

            Color beamColor = new Color(0.96f, 0.75f, 0.5f, 0.12f); // Soft glowing translucent sunset gold
            if (beamShader != null && beamShader.name == "Custom/VolumetricBeam")
            {
                beamMat.SetColor("_Color", beamColor);
                beamMat.SetFloat("_GlowIntensity", 3.0f);
                beamMat.SetFloat("_FresnelPower", 2.0f);
                beamMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else if (beamShader != null && beamShader.name != null && beamShader.name.Contains("Universal Render Pipeline"))
            {
                beamMat.SetFloat("_Surface", 1f); // Transparent
                beamMat.SetFloat("_Blend", 0f); // Alpha blend
                beamMat.SetColor("_BaseColor", beamColor);
                beamMat.SetColor("_EmissionColor", new Color(0.96f, 0.75f, 0.5f) * 3f);
                beamMat.EnableKeyword("_EMISSION");
                beamMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                beamMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                beamMat.SetInt("_ZWrite", 0);
                beamMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // Enable URP transparency keywords so it actually blends instead of rendering opaque
                beamMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                beamMat.DisableKeyword("_ALPHATEST_ON");
                beamMat.EnableKeyword("_ALPHABLEND_ON");
            }
            else
            {
                beamMat.SetColor("_Color", beamColor);
            }
            beamGo.GetComponent<Renderer>().sharedMaterial = beamMat;

            // 2. Slow-floating tiny dust particles inside the light shaft
            var dustGo = new GameObject("PapyrusDustParticles");
            dustGo.transform.SetParent(keyObj.transform);
            dustGo.transform.localPosition = Vector3.zero;
            dustGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Point straight up
            dustGo.transform.localScale = Vector3.one;

            var ps = dustGo.AddComponent<ParticleSystem>();
            var psr = dustGo.GetComponent<ParticleSystemRenderer>();

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 10.0f; // Extra long lifetime for suspended feel
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f); // Very slow upward drift (floating suspended dust)
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.06f); // Extremely tiny dust motes (realistic)
            main.startColor = new Color(0.96f, 0.85f, 0.6f, 0.45f); // Subtly glowing gold dust
            main.maxParticles = 600; // More particles since they are smaller
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 120f; // Higher rate to populate the shaft volume beautifully

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f; // 0 degree angle makes the cone a cylinder!
            shape.radius = 1.2f; // Spawn in the cylinder radius
            shape.length = 150f; // Spawn along the height of the beam!

            // Add simple noise to make particles wander realistically like dust in air
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.2f;

            // Setup fade out over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.96f, 0.85f, 0.6f), 0.0f), new GradientColorKey(new Color(0.96f, 0.85f, 0.6f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.5f, 0.2f), new GradientAlphaKey(0.5f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            // Create soft circular material for dust particles
            Shader dustShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (dustShader == null) dustShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (dustShader == null) dustShader = Shader.Find("Particles/Standard Unlit");
            if (dustShader == null) dustShader = Shader.Find("Lit");

            Material dustMat = new Material(dustShader);
            dustMat.name = "PapyrusDustMat";

            int dTexSize = 16;
            Texture2D circleTex = new Texture2D(dTexSize, dTexSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < dTexSize; y++)
            {
                for (int x = 0; x < dTexSize; x++)
                {
                    float dx = (x - (dTexSize - 1) * 0.5f) / ((dTexSize - 1) * 0.5f);
                    float dy = (y - (dTexSize - 1) * 0.5f) / ((dTexSize - 1) * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1.0f - dist);
                    alpha = alpha * alpha;
                    circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            circleTex.Apply();

            Color pColor = new Color(0.96f, 0.85f, 0.6f, 0.5f);
            if (dustShader.name != null && dustShader.name.Contains("Particles"))
            {
                dustMat.SetTexture("_BaseMap", circleTex);
                dustMat.SetColor("_BaseColor", pColor);
                dustMat.SetFloat("_Surface", 1f);
                dustMat.SetFloat("_Blend", 0f);
                dustMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                dustMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive glowing dust
                dustMat.SetInt("_ZWrite", 0);
                dustMat.DisableKeyword("_ALPHATEST_ON");
                dustMat.EnableKeyword("_ALPHABLEND_ON");
                dustMat.SetColor("_EmissionColor", new Color(0.96f, 0.85f, 0.6f) * 8f);
                dustMat.EnableKeyword("_EMISSION");
                dustMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                if (dustMat.HasProperty("_MainTex")) dustMat.SetTexture("_MainTex", circleTex);
                if (dustMat.HasProperty("_Color")) dustMat.SetColor("_Color", pColor);
                dustMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                dustMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                dustMat.SetInt("_ZWrite", 0);
                dustMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            psr.sharedMaterial = dustMat;
            ps.Play();

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
                    
                    Color originalColor = mats[i].HasProperty("_Color") ? mats[i].color : 
                                         (mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : Color.white);
                    newMat.SetColor("_BaseColor", originalColor);
                    
                    Texture mainTex = mats[i].HasProperty("_MainTex") ? mats[i].mainTexture : 
                                      (mats[i].HasProperty("_BaseMap") ? mats[i].GetTexture("_BaseMap") : null);
                    if (mainTex != null)
                    {
                        newMat.SetTexture("_BaseMap", mainTex);
                    }
                    
                    newMat.SetFloat("_Smoothness", mats[i].HasProperty("_Glossiness") ? mats[i].GetFloat("_Glossiness") : 
                                                 (mats[i].HasProperty("_Smoothness") ? mats[i].GetFloat("_Smoothness") : 0.1f));
                    newMat.SetFloat("_Metallic", mats[i].HasProperty("_Metallic") ? mats[i].GetFloat("_Metallic") : 0.0f);
                    mats[i] = newMat;
                }
                r.materials = mats;
            }
        }

        private void SpawnBoat()
        {
            float groundY = 0f;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(0f, 250f, -74f), Vector3.down, out hit, 300f))
            {
                groundY = hit.point.y;
            }
            else
            {
                var terrain = Terrain.activeTerrain;
                if (terrain != null) groundY = terrain.SampleHeight(new Vector3(0f, 0f, -74f));
            }
            
            // Raised by +1.6f so the hull sits on top of the sand instead of being buried in it.
            Vector3 spawnPos = new Vector3(0f, groundY + 1.6f, -74f); // Sits on beach sand
            
            GameObject prefab = Resources.Load<GameObject>("boat");
            if (prefab != null)
            {
                // Set the boat rotation so it is upright, and height so it floats neatly
                boatObj = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 180f, 0f));
                boatObj.transform.localScale = Vector3.one * 1.5f; 
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

            initialBoatY = boatObj.transform.position.y;

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
            
            // Compute combined bounds of all children renderers in world space to set up a clean trigger BoxCollider
            Bounds worldBounds = new Bounds(boatObj.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (var r in boatObj.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasBounds)
                {
                    worldBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(r.bounds);
                }
            }

            var boxCol = boatObj.GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = boatObj.AddComponent<BoxCollider>();
            // isTrigger = false so the boat is solid and the player can't walk through it
            boxCol.isTrigger = false;
            if (hasBounds)
            {
                boxCol.center = boatObj.transform.InverseTransformPoint(worldBounds.center);
                Vector3 lossyScale = boatObj.transform.lossyScale;
                boxCol.size = new Vector3(
                    worldBounds.size.x / (lossyScale.x > 0 ? lossyScale.x : 1f),
                    worldBounds.size.y / (lossyScale.y > 0 ? lossyScale.y : 1f),
                    worldBounds.size.z / (lossyScale.z > 0 ? lossyScale.z : 1f)
                );
            }
            else
            {
                boxCol.size = new Vector3(20f, 10f, 40f);
            }

            // Ensure any existing colliders in children are also solid (not triggers)
            foreach (var c in boatObj.GetComponentsInChildren<Collider>(true))
            {
                c.isTrigger = false;
            }
        }

        public void StartEscapeSequence(GameObject player)
        {
            hasEscaped = true;
            isEscaping = true;
            escapeTimer = 0f;

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

            // Disable character controller and immersive body script to prevent physical updates and shadow animation sync overrides
            var charController = player.GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;

            var immersiveBody = player.GetComponent<TheAlchemistsCrypt.Player.PlayerImmersiveBody>();
            if (immersiveBody != null) immersiveBody.enabled = false;

            // Reset all animators under the player to stop the running animations
            var animators = player.GetComponentsInChildren<Animator>();
            foreach (var anim in animators)
            {
                if (anim == null) continue;
                for (int i = 0; i < anim.parameterCount; i++)
                {
                    var param = anim.GetParameter(i);
                    if (param.type == AnimatorControllerParameterType.Float)
                    {
                        anim.SetFloat(param.nameHash, 0f);
                    }
                    else if (param.type == AnimatorControllerParameterType.Bool)
                    {
                        anim.SetBool(param.nameHash, false);
                    }
                    else if (param.type == AnimatorControllerParameterType.Int)
                    {
                        anim.SetInteger(param.nameHash, 0);
                    }
                }
                anim.Play("Idle", 0, 0f);
                anim.Play("idle", 0, 0f);
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
                    
                    // Smoothly sail out to sea (Z decreases) with gentle wave bobbing
                    if (boatObj != null)
                    {
                        float bob = Mathf.Sin(Time.time * 1.2f) * 0.08f;
                        float newZ = boatObj.transform.position.z - 4f * Time.deltaTime;
                        
                        // Lerp Y from initialBoatY on beach (Z = -74) to 3.2f floating in sea (Z = -104)
                        float t = Mathf.InverseLerp(-74f, -104f, newZ);
                        float targetY = Mathf.Lerp(initialBoatY, 3.2f, t);
                        
                        boatObj.transform.position = new Vector3(0f, targetY + bob, newZ);
                    }

                    // Use cached player — no per-frame scene search
                    if (cachedPlayer != null)
                    {
                        cachedPlayer.transform.position = boatObj.transform.position + new Vector3(0f, 1.2f, 0f);
                    }

                    if (escapeTimer >= 5.0f)
                    {
                        isEscaping = false;
                        WinGame();
                    }
                }
                return;
            }

            // Bob the escape boat gently at all times on the beach/shore to make it look floaty
            if (boatObj != null)
            {
                float bob = Mathf.Sin(Time.time * 1.2f) * 0.08f;
                boatObj.transform.position = new Vector3(0f, initialBoatY + bob, -74f);
            }

            if (!TheAlchemistsCrypt.UI.MobileHUDButtons.HasStartedGame) return;

            var playerObj = cachedPlayer;
            // Null-check: re-find if player was respawned or scene reloaded
            if (playerObj == null)
            {
                playerObj = GameObject.FindGameObjectWithTag("Player");
                cachedPlayer = playerObj;
            }
            if (playerObj == null) return;

            // Proximity/Pickup detection via direct distance calculations
            if (keyObj != null)
            {
                float distToKey = Vector3.Distance(playerObj.transform.position, keyObj.transform.position);
                nearKey = (distToKey <= 15f);
                canPickupKey = (distToKey <= 3.5f);
            }
            else
            {
                nearKey = false;
                canPickupKey = false;
            }

            if (boatObj != null)
            {
                float distToBoat = Vector3.Distance(playerObj.transform.position, boatObj.transform.position);
                nearBoat = (distToBoat <= 30f);
                canEscape = (distToBoat <= 18f);
            }
            else
            {
                nearBoat = false;
                canEscape = false;
            }

            if (nearKey && !hasKey)
            {
                if (promptUiGo != null)
                {
                    promptUiGo.SetActive(true);
                    promptText.color = Color.white;
                }
                
                if (canPickupKey)
                {
                    hasKey = true;
                    if (keyObj != null) Destroy(keyObj);
                    papyrusCollectFeedbackTimer = 5.0f; // Keep showing explicit pickup banner for 5 seconds
                    nearKey = false;
                    canPickupKey = false;
                    
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
                    if (canEscape)
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
            var hud = TheAlchemistsCrypt.UI.MobileHUDButtons.Instance;
            if (hud == null) hud = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.UI.MobileHUDButtons>();
            if (hud != null)
            {
                Debug.Log("[EscapeManager] WinGame: HUD found! Displaying Victory Screen.");
                hud.ShowVictoryScreen();
            }
            else
            {
                Debug.LogError("[EscapeManager] WinGame: MobileHUDButtons Instance not found!");
            }
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
