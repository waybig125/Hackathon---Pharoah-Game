using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TheAlchemistsCrypt.AI;
using TheAlchemistsCrypt.Gameplay;

namespace TheAlchemistsCrypt.UI
{
    public class MinimapUI : MonoBehaviour, IPointerDownHandler
    {
        public static MinimapUI Instance { get; private set; }

        [Header("Radar Settings (Default)")]
        public float radarWorldRadius = 120f;
        public float radarPixelRadius = 110f;
        private float radarScale;

        [Header("Expanded Settings")]
        public float expandedScale;
        private bool isExpanded = false;
        private RectTransform expandedPanel;
        private GameObject legendPanel;

        [Header("Animation Settings")]
        public float animDuration = 0.25f;
        private Coroutine transitionCoroutine;

        private Transform playerTransform;
        private RectTransform mapRotator;
        private RectTransform mapContent;
        private RectTransform minimapFrame;
        private RectTransform maskContainer;
        private RectTransform compassRing;
        private RectTransform playerIndicator;
        private RectTransform radarSweep;
        
        // Icon assets generated procedurally
        private Sprite obsidianCircleSprite;
        private Sprite obsidianRectSprite;
        private Sprite goldBorderCircleSprite;
        private Sprite goldBorderRectSprite;
        private Image borderImage;
        private Sprite playerArrowSprite;
        private Sprite brownDotSprite;
        
        public Sprite keySprite;
        public Sprite boatSprite;
private Sprite buildingSprite;
        
        private Sprite minimapBgSprite;
        private Sprite minimapBgRectSprite;
        private UnityEngine.UI.Image bgGridImage;
        private Sprite radarSweepSprite;
        private Sprite zombieDotSprite;
        private Sprite pharaohDotSprite;
        private Sprite medicineDotSprite;

        // Fog of War
        private Texture2D fowTexture;
        private UnityEngine.UI.RawImage fowImage;
        private Coroutine fowCoroutine;

        // Track dynamic indicators
        private List<ZombieIndicator> zombieIndicators = new List<ZombieIndicator>();
        private List<MedicineIndicator> medicineIndicators = new List<MedicineIndicator>();
        private List<StaticElementIndicator> staticIndicators = new List<StaticElementIndicator>();

        // Colors for differentiation
        private static readonly Color ColorSea = new Color(0.1f, 0.48f, 0.68f, 0.8f);
        private static readonly Color ColorBeach = new Color(0.94f, 0.82f, 0.5f, 0.8f);
        private static readonly Color ColorPharaoh = new Color(1.0f, 0.4f, 0.0f, 1.0f);

        private class ZombieIndicator
        {
            public GameObject zombieGo;
            public RectTransform iconRect;
            public Image iconImage;
            public bool isPharaoh;
        }

        private class MedicineIndicator
        {
            public GameObject medicineGo;
            public RectTransform iconRect;
            public Image iconImage;
        }

        private class StaticElementIndicator
        {
            public GameObject sourceGo;
            public string name;
            public Vector3 worldPos;
            public Vector2 worldSize;
            public RectTransform iconRect;
            public bool isZone;
        }

        private void Awake()
        {
            Instance = this;
            radarWorldRadius = 45f;
            radarScale = radarPixelRadius / radarWorldRadius;
            expandedScale = (Screen.width * 0.45f) / 500f;
            GenerateSprites();
            InitFogOfWar();
            BuildMinimapUI();
        }

        private void InitFogOfWar()
        {
            int texSize = 256;
            fowTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            fowTexture.filterMode = FilterMode.Bilinear;
            Color32 black = new Color32(0, 0, 0, 255);
            Color32[] pixels = new Color32[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = black;
            fowTexture.SetPixels32(pixels);
            fowTexture.Apply();
        }

        private void Start()
        {
            // Auto-detect player
            var movement = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Movement>();
            if (movement != null) playerTransform = movement.transform;

            expandedScale = (Screen.width * 0.45f) / 500f;
            BuildMinimapUI();
            CacheStaticElements();

            // Disable Fog of War on mobile for massive performance win (texture manipulation is slow)
#if !UNITY_ANDROID && !UNITY_IOS
            fowCoroutine = StartCoroutine(UpdateFogOfWarRoutine());
#else
            if (fowImage != null) fowImage.gameObject.SetActive(false);
#endif
        }

        private void GenerateSprites()
        {
            obsidianCircleSprite = CreateCircleSprite(240, new Color(0.04f, 0.04f, 0.04f, 0.93f));
            obsidianRectSprite = CreateRectSprite(300, 200, new Color(0.04f, 0.04f, 0.04f, 0.93f));
            goldBorderCircleSprite = CreateCircleRingSprite(240, 6, new Color(0.95f, 0.8f, 0.2f, 0.95f));
            goldBorderRectSprite = CreateRectRingSprite(400, 400, 6, new Color(0.95f, 0.8f, 0.2f, 0.95f));
            
            playerArrowSprite = CreatePlayerArrowSprite(64, 64, new Color(0.95f, 0.8f, 0.2f, 0.95f));
            brownDotSprite = CreateSolidCircleSprite(12, new Color(0.6f, 0.4f, 0.2f, 0.8f));
            buildingSprite = CreateSolidBarSprite(32, 32, new Color(0.85f, 0.7f, 0.4f, 0.65f));

            minimapBgSprite = CreateMinimapBackgroundSprite(240);
            minimapBgRectSprite = CreateMinimapBackgroundRectSprite(400, 400);
            radarSweepSprite = CreateRadarSweepSprite(220, new Color(0.95f, 0.8f, 0.2f, 0.15f));
            zombieDotSprite = CreateWarningTriangleSprite(20, new Color(0.9f, 0.1f, 0.1f, 0.95f));
            pharaohDotSprite = CreateWarningTriangleSprite(24, ColorPharaoh);
            
            keySprite = CreateSolidCircleSprite(16, new Color(1f, 0.9f, 0.2f, 0.95f));
            boatSprite = CreateSolidBarSprite(24, 12, new Color(0.1f, 0.4f, 0.9f, 0.95f));
            medicineDotSprite = CreateMedicineIconSprite(20, new Color(0.1f, 0.9f, 0.3f, 0.95f));
        }

        private void BuildMinimapUI()
        {
            minimapFrame = gameObject.GetComponent<RectTransform>();
            if (minimapFrame == null) minimapFrame = gameObject.AddComponent<RectTransform>();
            
            minimapFrame.anchorMin = minimapFrame.anchorMax = new Vector2(1, 1);
            minimapFrame.anchoredPosition = new Vector2(-130, -130); 
            minimapFrame.sizeDelta = new Vector2(220, 220);

            var frameImage = gameObject.GetComponent<Image>();
            if (frameImage == null) frameImage = gameObject.AddComponent<Image>();
            frameImage.sprite = obsidianCircleSprite;
            frameImage.color = Color.white;
            frameImage.raycastTarget = true; 

            var maskGo = new GameObject("MaskContainer", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskContainer = maskGo.GetComponent<RectTransform>();
            maskContainer.SetParent(minimapFrame, false);
            maskContainer.anchorMin = Vector2.zero; maskContainer.anchorMax = Vector2.one;
            maskContainer.offsetMin = maskContainer.offsetMax = new Vector2(6, 6); 
            
            var maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = obsidianCircleSprite; 
            maskImg.color = Color.white; 
            maskImg.raycastTarget = false;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;

            var mapRotatorGo = new GameObject("MapRotator", typeof(RectTransform));
            mapRotator = mapRotatorGo.GetComponent<RectTransform>();
            mapRotator.SetParent(maskContainer, false);
            mapRotator.anchorMin = mapRotator.anchorMax = new Vector2(0.5f, 0.5f);
            mapRotator.anchoredPosition = Vector2.zero;
            mapRotator.sizeDelta = new Vector2(400, 400);

            var bgGridGo = new GameObject("MinimapBackgroundGrid", typeof(RectTransform), typeof(Image));
            var bgGridRect = bgGridGo.GetComponent<RectTransform>();
            bgGridRect.SetParent(mapRotator, false);
            bgGridRect.anchorMin = bgGridRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgGridRect.anchoredPosition = Vector2.zero;
            bgGridRect.sizeDelta = new Vector2(2000, 2000); 
            var bgImg = bgGridGo.GetComponent<Image>();
            bgImg.sprite = minimapBgSprite;
            bgImg.type = Image.Type.Tiled;
            bgImg.color = Color.white;
            bgGridImage = bgImg;

            var mapContentGo = new GameObject("MapContent", typeof(RectTransform));
            mapContent = mapContentGo.GetComponent<RectTransform>();
            mapContent.SetParent(mapRotator, false);
            mapContent.anchorMin = mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            mapContent.anchoredPosition = Vector2.zero;
            mapContent.sizeDelta = new Vector2(2000, 2000); 

            // FOW Image (Overlay on background grid, under icons)
            var fowGo = new GameObject("FogOfWar", typeof(RectTransform), typeof(RawImage));
            var fowRect = fowGo.GetComponent<RectTransform>();
            fowRect.SetParent(mapContent, false);
            fowRect.anchorMin = fowRect.anchorMax = new Vector2(0.5f, 0.5f);
            fowRect.anchoredPosition = Vector2.zero;
            fowRect.sizeDelta = new Vector2(2000, 2000); // Must match map bounds exactly
            fowImage = fowGo.GetComponent<RawImage>();
            fowImage.texture = fowTexture;
            fowImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Opacity of the fog
            
            // FOW Material blending to multiply with background (requires custom shader, so we just use UI overlay with soft black/clear)
            // Wait, RawImage uses standard UI blend, so a texture with alpha 255 is solid.
            // We want the clear parts to be alpha 0. So black = opaque, white = transparent.

            var compassRingGo = new GameObject("CompassRing", typeof(RectTransform));
            compassRing = compassRingGo.GetComponent<RectTransform>();
            compassRing.SetParent(mapRotator, false);
            compassRing.anchorMin = compassRing.anchorMax = new Vector2(0.5f, 0.5f);
            compassRing.anchoredPosition = Vector2.zero;
            compassRing.sizeDelta = new Vector2(200, 200);

            string[] directions = { "N", "E", "S", "W" };
            Vector2[] positions = { new Vector2(0, 85), new Vector2(85, 0), new Vector2(0, -85), new Vector2(-85, 0) };
            for (int i = 0; i < 4; i++)
            {
                var dirGo = new GameObject("Label_" + directions[i], typeof(RectTransform), typeof(Text));
                var dirRect = dirGo.GetComponent<RectTransform>();
                dirRect.SetParent(compassRing, false);
                dirRect.anchorMin = dirRect.anchorMax = new Vector2(0.5f, 0.5f);
                dirRect.anchoredPosition = positions[i];
                dirRect.sizeDelta = new Vector2(24, 24);
                var txt = dirGo.GetComponent<Text>();
                txt.text = directions[i];
                txt.font = GetRobustFont();
                txt.fontSize = 15;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = directions[i] == "N" ? new Color(1f, 0.3f, 0.3f, 0.95f) : new Color(0.95f, 0.8f, 0.2f, 0.9f);
            }

            var sweepGo = new GameObject("RadarSweep", typeof(RectTransform), typeof(Image));
            radarSweep = sweepGo.GetComponent<RectTransform>();
            radarSweep.SetParent(maskContainer, false);
            radarSweep.anchorMin = radarSweep.anchorMax = new Vector2(0.5f, 0.5f);
            radarSweep.anchoredPosition = Vector2.zero;
            radarSweep.sizeDelta = new Vector2(400, 400);
            var sweepImg = sweepGo.GetComponent<Image>();
            sweepImg.sprite = radarSweepSprite;
            sweepImg.color = Color.white;

            var borderGo = new GameObject("BorderRing", typeof(RectTransform), typeof(Image));
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.SetParent(minimapFrame, false);
            borderRect.anchorMin = Vector2.zero; borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.sprite = goldBorderCircleSprite;
            borderImg.color = Color.white;
            borderImage = borderImg;

            var playerGo = new GameObject("PlayerIndicator", typeof(RectTransform), typeof(Image));
            playerIndicator = playerGo.GetComponent<RectTransform>();
            playerIndicator.SetParent(minimapFrame, false); 
            playerIndicator.anchorMin = playerIndicator.anchorMax = new Vector2(0.5f, 0.5f);
            playerIndicator.anchoredPosition = Vector2.zero;
            playerIndicator.sizeDelta = new Vector2(32, 32);
            var playerImg = playerGo.GetComponent<Image>();
            playerImg.sprite = playerArrowSprite;
            playerImg.color = Color.white;

            CreateLegendPanel();
        }

        private void CreateLegendPanel()
        {
            legendPanel = new GameObject("LegendPanel", typeof(RectTransform), typeof(Image));
            var legendRect = legendPanel.GetComponent<RectTransform>();
            legendRect.SetParent(minimapFrame, false);
            legendRect.anchorMin = new Vector2(0, 0);
            legendRect.anchorMax = new Vector2(0, 0);
            legendRect.pivot = new Vector2(0, 0);
            legendRect.anchoredPosition = new Vector2(40, 40);
            legendRect.sizeDelta = new Vector2(260, 210);
            
            var img = legendPanel.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.85f); // Slightly darker for better readability
            legendPanel.SetActive(false);

            string[] labels = { "Player", "Mummy", "Pharaoh", "Medicine", "Papyrus", "Boat", "Building", "Sea" };
            Color[] colors = { Color.yellow, Color.red, ColorPharaoh, Color.green, new Color(1f, 0.9f, 0.2f), Color.blue, new Color(0.85f, 0.7f, 0.4f), ColorSea };

            for (int i = 0; i < labels.Length; i++)
            {
                var item = new GameObject("LegendItem_" + labels[i], typeof(RectTransform), typeof(Text));
                var itemRect = item.GetComponent<RectTransform>();
                itemRect.SetParent(legendRect, false);
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(0, 1);
                itemRect.pivot = new Vector2(0, 1);
                itemRect.anchoredPosition = new Vector2(25, -20 - (i * 22)); // Tighter spacing with 25px offset from left
                itemRect.sizeDelta = new Vector2(210, 25);
                
                var txt = item.GetComponent<Text>();
                txt.text = "■ " + labels[i];
                txt.font = GetRobustFont();
                txt.fontSize = 16;
                txt.color = colors[i];
            }

            // Close Label
            var closeGo = new GameObject("CloseLabel", typeof(RectTransform), typeof(Text));
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.SetParent(minimapFrame, false);
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-20, -20);
            closeRect.sizeDelta = new Vector2(200, 40);
            var closeTxt = closeGo.GetComponent<Text>();
            closeTxt.text = "TAP TO CLOSE [X]";
            closeTxt.font = GetRobustFont();
            closeTxt.fontSize = 20;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleRight;
            closeGo.name = "CloseLabel";
            closeGo.SetActive(false);
        }

        private void CacheStaticElements()
        {
            // Clear existing
            foreach (var s in staticIndicators)
            {
                if (s.iconRect != null) Destroy(s.iconRect.gameObject);
            }
            staticIndicators.Clear();

            // Scan for Pyramids, Buildings, and Zones
            var allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo)
            {
                if (go == null) continue;
                string lowerName = go.name.ToLower();

                if (go.transform.parent != null && go.transform.parent.name.Contains("Player")) continue;

                if (lowerName == "seazone")
                {
                    CreateSeaZoneIcon(go.transform.position, new Vector2(go.transform.localScale.x, go.transform.localScale.y), ColorSea);
                }
                else if (lowerName == "seazone_shallow")
                {
                    Color shallowCol = new Color(0.18f, 0.60f, 0.80f, 0.75f);
                    CreateSeaZoneIcon(go.transform.position, new Vector2(go.transform.localScale.x, go.transform.localScale.y), shallowCol);
                }
                else if (lowerName == "seazone_surf")
                {
                    Color surfCol = new Color(0.55f, 0.82f, 0.92f, 0.8f);
                    CreateSeaZoneIcon(go.transform.position, new Vector2(go.transform.localScale.x, go.transform.localScale.y), surfCol);
                }
                else if (lowerName == "beachzone")
                {
                    CreateSeaZoneIcon(go.transform.position, new Vector2(go.transform.localScale.x, go.transform.localScale.y), ColorBeach);
                }

                else if (lowerName.Contains("house") || lowerName.Contains("building") || lowerName.Contains("temple"))
                {
                    CreateStaticIcon(go, new Vector2(40, 40), buildingSprite);
                }
                else if (lowerName.Contains("tree") || lowerName.Contains("palm"))
                {
                    CreateStaticIcon(go, new Vector2(16, 16), CreateSolidCircleSprite(16, new Color(0.1f, 0.5f, 0.15f, 0.8f)));
                }
                else if (lowerName.Contains("crate") || lowerName.Contains("barrel"))
                {
                    CreateStaticIcon(go, new Vector2(8, 8), brownDotSprite);
                }
            }
        }

        private void CreateSeaZoneIcon(Vector3 worldPos, Vector2 worldSize, Color col)
        {
            var iconGo = new GameObject("SeaZoneIcon", typeof(RectTransform), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.SetParent(mapContent, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            
            var img = iconGo.GetComponent<Image>();
            img.color = col;
            img.raycastTarget = false;

            var indicator = new StaticElementIndicator
            {
                name = "Zone",
                worldPos = worldPos,
                worldSize = worldSize,
                iconRect = iconRect,
                isZone = true
            };
            staticIndicators.Add(indicator);
        }

        private void CreateStaticIcon(GameObject go, Vector2 size, Sprite sprite)
        {
            var iconGo = new GameObject("StaticIcon", typeof(RectTransform), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.SetParent(mapContent, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = size;
            
            var img = iconGo.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;

            var indicator = new StaticElementIndicator
            {
                name = go.name,
                worldPos = go.transform.position,
                iconRect = iconRect,
                sourceGo = go,
                isZone = false
            };
            staticIndicators.Add(indicator);
        }

        private Vector3 lastUpdatePlayerPos;
        private float movementThreshold = 0.2f;

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
            {
                if (transitionCoroutine == null)
                {
                    isExpanded = !isExpanded;
                    transitionCoroutine = StartCoroutine(AnimateMapTransition(isExpanded));
                }
            }

            if (playerTransform == null)
            {
                var movement = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Movement>();
                if (movement != null) playerTransform = movement.transform;
                if (playerTransform == null) return;
            }

            Vector3 playerPos = playerTransform.position;
            
            // OPTIMIZATION: Only update map background if the player has moved significantly
            if (Vector3.Distance(playerPos, lastUpdatePlayerPos) < movementThreshold && !isExpanded)
            {
                UpdateSmoothingRotations();
            }
            else
            {
                lastUpdatePlayerPos = playerPos;

                if (isExpanded)
                {
                    UpdateExpandedPositions();
                }
                else
                {
                    UpdateRadarPositions();
                }
            }

            // Animate dynamic radar sweep rotation
            if (radarSweep != null)
            {
                radarSweep.Rotate(Vector3.forward, -60f * Time.deltaTime);
            }

            // Update dynamic indicators (Throttled: every 2nd/3rd frame)
            if (Time.frameCount % 2 == 0) UpdateZombieIndicators();
            if (Time.frameCount % 3 == 0) UpdateMedicineIndicators();
        }

        private void UpdateSmoothingRotations()
        {
            // Keep the map and indicators aligned even when standing still but turning
            if (playerTransform == null) return;
            float playerYaw = playerTransform.eulerAngles.y;
            if (mapRotator != null) mapRotator.localEulerAngles = new Vector3(0, 0, playerYaw);
            if (playerIndicator != null) playerIndicator.localEulerAngles = new Vector3(0, 0, -playerYaw);
            if (compassRing != null) compassRing.localEulerAngles = new Vector3(0, 0, playerYaw);
            
            if (radarSweep != null)
            {
                radarSweep.Rotate(Vector3.forward, -60f * Time.deltaTime);
            }
        }

        private IEnumerator UpdateFogOfWarRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.25f);
                if (playerTransform == null) continue;

                Vector3 pPos = playerTransform.position;
                
                // Assuming the map area represented by the 2000x2000 UI is roughly 500x500 world units.
                // Center of map is 0,0. Range is -250 to 250 in world space.
                float worldMapSize = 2000f / radarScale; // Full size in world coordinates based on radarScale
                
                // Convert world position to FOW texture pixel coordinates (0 to 256)
                // mapContent's center is 0,0 world.
                float FOW_SIZE = 256f;
                int px = Mathf.RoundToInt((pPos.x / worldMapSize + 0.5f) * FOW_SIZE);
                int py = Mathf.RoundToInt((pPos.z / worldMapSize + 0.5f) * FOW_SIZE);

                // Paint a clear circle
                int radius = 18; // Reveal radius
                bool changed = false;
                Color32 clearColor = new Color32(0, 0, 0, 0);

                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y <= radius * radius)
                        {
                            int cx = px + x;
                            int cy = py + y;
                            if (cx >= 0 && cx < FOW_SIZE && cy >= 0 && cy < FOW_SIZE)
                            {
                                fowTexture.SetPixel(cx, cy, clearColor);
                                changed = true;
                            }
                        }
                    }
                }
                
                if (changed) fowTexture.Apply();
            }
        }

        private void UpdateRadarPositions()
        {
            Vector3 playerPos = playerTransform.position;
            Vector3 playerRot = playerTransform.eulerAngles;

            mapContent.anchoredPosition = new Vector2(-playerPos.x * radarScale, -playerPos.z * radarScale);
            mapContent.localEulerAngles = Vector3.zero;
            mapRotator.localEulerAngles = new Vector3(0, 0, playerRot.y);
            playerIndicator.localEulerAngles = Vector3.zero;
            playerIndicator.anchoredPosition = Vector2.zero;

            for (int i = staticIndicators.Count - 1; i >= 0; i--)
            {
                var s = staticIndicators[i];
                if (s.sourceGo == null && !s.isZone)
                {
                    if (s.iconRect != null) Destroy(s.iconRect.gameObject);
                    staticIndicators.RemoveAt(i);
                    continue;
                }

                if (s.isZone)
                {
                    s.iconRect.gameObject.SetActive(true);
                    s.iconRect.sizeDelta = new Vector2(s.worldSize.x * radarScale, s.worldSize.y * radarScale);
                    s.iconRect.anchoredPosition = new Vector2(s.worldPos.x * radarScale, s.worldPos.z * radarScale);
                }
                else
                {
                    s.worldPos = s.sourceGo.transform.position;
                    
                    if (s.name == "AncientPapyrus")
                    {
                        s.iconRect.gameObject.SetActive(true);
                        
                        // Calculate position relative to player
                        Vector3 relPos = s.worldPos - playerPos;
                        Vector2 localPos = new Vector2(relPos.x, relPos.z) * radarScale;
                        
                        // Clamp to radar radius minus a small margin to keep it inside the frame
                        float maxRad = radarPixelRadius - 12f;
                        if (localPos.magnitude > maxRad)
                        {
                            localPos = localPos.normalized * maxRad;
                        }
                        
                        // In mapContent coordinates (which is translated by -playerPos * radarScale):
                        // playerPos * radarScale + localPos resolves relative to center of radar
                        s.iconRect.anchoredPosition = new Vector2(playerPos.x * radarScale, playerPos.z * radarScale) + localPos;
                        
                        // Keep upright by compensating for player rotation
                        float mapRot = mapRotator != null ? mapRotator.localEulerAngles.z : 0f;
                        s.iconRect.localEulerAngles = new Vector3(0f, 0f, -mapRot);
                    }
                    else
                    {
                        float dist = Vector3.Distance(playerPos, s.worldPos);
                        s.iconRect.gameObject.SetActive(dist <= radarWorldRadius * 2f); 
                        s.iconRect.anchoredPosition = new Vector2(s.worldPos.x * radarScale, s.worldPos.z * radarScale);
                        s.iconRect.localEulerAngles = Vector3.zero;
                    }
                }
            }
        }

        private void UpdateExpandedPositions()
        {
            Vector3 playerPos = playerTransform.position;

            // Expanded mode: Map is fixed, player moves across it
            mapContent.anchoredPosition = Vector2.zero;
            mapContent.localEulerAngles = Vector3.zero;
            mapRotator.localEulerAngles = Vector3.zero;

            // Player dot position
            playerIndicator.localEulerAngles = Vector3.zero;
            playerIndicator.anchoredPosition = new Vector2(playerPos.x * expandedScale, playerPos.z * expandedScale);

            foreach (var s in staticIndicators)
            {
                s.iconRect.gameObject.SetActive(true);
                if (s.isZone)
                {
                    s.iconRect.sizeDelta = new Vector2(s.worldSize.x * expandedScale, s.worldSize.y * expandedScale);
                }
                s.iconRect.anchoredPosition = new Vector2(s.worldPos.x * expandedScale, s.worldPos.z * expandedScale);
                s.iconRect.localEulerAngles = Vector3.zero;
            }
        }

        private void UpdateZombieIndicators()
        {
            var zombies = Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude);
            Vector3 playerPos = playerTransform.position;
            float currentScale = isExpanded ? expandedScale : radarScale;

            // 1. Disable all indicators first
            foreach (var ind in zombieIndicators)
            {
                if (ind.iconRect != null) ind.iconRect.gameObject.SetActive(false);
            }

            // 2. Map active zombies to indicators
            for (int i = 0; i < zombies.Length; i++)
            {
                var z = zombies[i];
                if (z == null || !z.gameObject.activeInHierarchy) continue;

                // Radar filtering
                if (!isExpanded)
                {
                    float dist = Vector3.Distance(playerPos, z.transform.position);
                    if (dist > radarWorldRadius) continue;
                }

                ZombieIndicator indicator = null;
                if (i < zombieIndicators.Count)
                {
                    indicator = zombieIndicators[i];
                }
                else
                {
                    indicator = new ZombieIndicator();
                    var go = new GameObject("ZombieIndicator", typeof(RectTransform), typeof(Image));
                    indicator.iconRect = go.GetComponent<RectTransform>();
                    indicator.iconRect.SetParent(mapContent, false);
                    indicator.iconRect.anchorMin = indicator.iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    
                    indicator.iconImage = go.GetComponent<Image>();
                    indicator.iconImage.raycastTarget = false;

                    zombieIndicators.Add(indicator);
                }

                indicator.zombieGo = z.gameObject;
                indicator.iconRect.gameObject.SetActive(true);
                
                bool isPharaoh = z is PharaohAI;
                indicator.isPharaoh = isPharaoh;
                indicator.iconImage.sprite = isPharaoh ? pharaohDotSprite : zombieDotSprite;
                indicator.iconRect.sizeDelta = isPharaoh ? new Vector2(24, 24) : new Vector2(20, 20);

                Vector3 zPos = z.transform.position;
                indicator.iconRect.anchoredPosition = new Vector2(zPos.x * currentScale, zPos.z * currentScale);

                // Compensate for map rotation so triangles point straight up on screen
                float mapRot = mapRotator != null ? mapRotator.localEulerAngles.z : 0f;
                indicator.iconRect.localEulerAngles = new Vector3(0, 0, -mapRot);
            }
        }

        private void UpdateMedicineIndicators()
        {
            var medicines = Object.FindObjectsByType<MedicinePickup>(FindObjectsInactive.Exclude);
            Vector3 playerPos = playerTransform.position;
            float currentScale = isExpanded ? expandedScale : radarScale;
            
            // 1. Disable all indicators first
            foreach (var ind in medicineIndicators)
            {
                if (ind.iconRect != null) ind.iconRect.gameObject.SetActive(false);
            }

            // 2. Map active medicines to indicators
            for (int i = 0; i < medicines.Length; i++)
            {
                var m = medicines[i];
                if (m == null || !m.gameObject.activeInHierarchy) continue;

                // Radar filtering
                if (!isExpanded)
                {
                    float dist = Vector3.Distance(playerPos, m.transform.position);
                    if (dist > radarWorldRadius) continue;
                }

                MedicineIndicator indicator = null;
                if (i < medicineIndicators.Count)
                {
                    indicator = medicineIndicators[i];
                }
                else
                {
                    indicator = new MedicineIndicator();
                    var go = new GameObject("MedicineIndicator", typeof(RectTransform), typeof(Image));
                    indicator.iconRect = go.GetComponent<RectTransform>();
                    indicator.iconRect.SetParent(mapContent, false);
                    indicator.iconRect.anchorMin = indicator.iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    indicator.iconRect.sizeDelta = new Vector2(20, 20);
                    
                    indicator.iconImage = go.GetComponent<Image>();
                    indicator.iconImage.sprite = medicineDotSprite;
                    indicator.iconImage.color = Color.white;
                    indicator.iconImage.raycastTarget = false;

                    medicineIndicators.Add(indicator);
                }

                indicator.medicineGo = m.gameObject;
                indicator.iconRect.gameObject.SetActive(true);
                
                Vector3 mPos = m.transform.position;
                indicator.iconRect.anchoredPosition = new Vector2(mPos.x * currentScale, mPos.z * currentScale);

                // Compensate for map rotation so health crosses point straight up on screen
                float mapRot = mapRotator != null ? mapRotator.localEulerAngles.z : 0f;
                indicator.iconRect.localEulerAngles = new Vector3(0, 0, -mapRot);
            }
        }

        
        public void RegisterDynamicStaticIcon(GameObject go, Vector2 size, Sprite sprite)
        {
            var iconGo = new GameObject("DynamicStaticIcon", typeof(RectTransform), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.SetParent(mapContent, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = size;
            
            var img = iconGo.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;

            var indicator = new StaticElementIndicator
            {
                name = go.name,
                worldPos = go.transform.position,
                iconRect = iconRect,
                sourceGo = go,
                isZone = false
            };
            staticIndicators.Add(indicator);
        }
public void OnPointerDown(PointerEventData eventData)
        {
            if (transitionCoroutine != null) return;
            isExpanded = !isExpanded;
            transitionCoroutine = StartCoroutine(AnimateMapTransition(isExpanded));
        }

        private IEnumerator AnimateMapTransition(bool expanding)
        {
            float elapsed = 0f;
            Vector2 startPos = minimapFrame.anchoredPosition;
            Vector2 startSize = minimapFrame.sizeDelta;

            var canvasRT = minimapFrame.parent as RectTransform;
            float canvasW = canvasRT != null ? canvasRT.rect.width : Screen.width;
            float canvasH = canvasRT != null ? canvasRT.rect.height : Screen.height;

            Vector2 targetPos = expanding ? new Vector2(-canvasW * 0.5f, -canvasH * 0.5f) : new Vector2(-130, -130);
            Vector2 targetSize = expanding ? new Vector2(canvasW * 0.85f, canvasH * 0.8f) : new Vector2(220, 220);
            
            // Toggle UI overlays
            if (expanding)
            {
                float mapPanelMinDimension = Mathf.Min(targetSize.x, targetSize.y) * 0.85f;
                expandedScale = mapPanelMinDimension / 400f; // Center and scale within a 400-unit world radius box

                if (MobileHUDButtons.Instance != null)
                {
                    MobileHUDButtons.Instance.SetHUDVisible(false);
                }

                if (bgGridImage != null)
                {
                    bgGridImage.sprite = minimapBgRectSprite;
                    bgGridImage.type = Image.Type.Simple;
                }
                minimapFrame.GetComponent<Image>().sprite = obsidianRectSprite;
                if (borderImage != null)
                {
                    borderImage.sprite = goldBorderRectSprite;
                    borderImage.type = Image.Type.Sliced;
                }
                radarSweep.gameObject.SetActive(false);
                compassRing.gameObject.SetActive(false);
                
                // Switch mask to rectangular to match gold border bounds perfectly, but KEEP enabled to clip overflow!
                var maskImg = maskContainer.GetComponent<Image>();
                maskImg.sprite = obsidianRectSprite;
                maskImg.type = Image.Type.Simple;
                maskContainer.GetComponent<Mask>().enabled = true;
                maskImg.enabled = true;

                legendPanel.SetActive(true);
                minimapFrame.Find("CloseLabel")?.gameObject.SetActive(true);
            }
            else
            {
                if (MobileHUDButtons.Instance != null)
                {
                    MobileHUDButtons.Instance.SetHUDVisible(true);
                }
                legendPanel.SetActive(false);
                minimapFrame.Find("CloseLabel")?.gameObject.SetActive(false);
            }

            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / animDuration);
                
                minimapFrame.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                minimapFrame.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
                
                yield return null;
            }

            minimapFrame.anchoredPosition = targetPos;
            minimapFrame.sizeDelta = targetSize;

            if (!expanding)
            {
                if (bgGridImage != null)
                {
                    bgGridImage.sprite = minimapBgSprite;
                    bgGridImage.type = Image.Type.Tiled;
                }
                minimapFrame.GetComponent<Image>().sprite = obsidianCircleSprite;
                if (borderImage != null)
                {
                    borderImage.sprite = goldBorderCircleSprite;
                    borderImage.type = Image.Type.Simple;
                }
                radarSweep.gameObject.SetActive(true);
                compassRing.gameObject.SetActive(true);
                
                // Reset mask back to circular
                var maskImg = maskContainer.GetComponent<Image>();
                maskImg.sprite = obsidianCircleSprite;
                maskImg.type = Image.Type.Simple;
                maskContainer.GetComponent<Mask>().enabled = true;
                maskImg.enabled = true;
            }

            transitionCoroutine = null;
        }

        #region Procedural Sprite Utilities

        private Sprite CreateRectSprite(int w, int h, Color col)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateCircleSprite(int size, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (float)(x - size / 2) / (size / 2);
                    float dy = (float)(y - size / 2) / (size / 2);
                    float dist = dx * dx + dy * dy;
                    if (dist <= 1.0f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRingSprite(int size, int thickness, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (float)(x - size / 2) / (size / 2);
                    float dy = (float)(y - size / 2) / (size / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float innerRad = (float)(size / 2 - thickness) / (size / 2);
                    if (dist <= 1.0f && dist >= innerRad)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateSolidCircleSprite(int size, Color col)
        {
            return CreateCircleSprite(size, col);
        }

        
        private Sprite CreateRectRingSprite(int w, int h, int thickness, Color col)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                { 
                    bool isBorder = (x < thickness || x > w - thickness || y < thickness || y > h - thickness);
                    tex.SetPixel(x, y, isBorder ? col : Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(thickness, thickness, thickness, thickness));
        }

        private Sprite CreateCircleRingSprite(int size, int thickness, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (float)(x - size / 2) / (size / 2);
                    float dy = (float)(y - size / 2) / (size / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float innerRad = (float)(size / 2 - thickness) / (size / 2);
                    if (dist <= 1.0f && dist >= innerRad)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
private Sprite CreateSolidBarSprite(int w, int h, Color col)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateMinimapBackgroundSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color baseBg = new Color(0.04f, 0.04f, 0.04f, 0.85f);
            Color gridColor = new Color(0.95f, 0.8f, 0.2f, 0.12f); // Soft gold lines
            Color gridColorBright = new Color(0.95f, 0.8f, 0.2f, 0.28f);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (float)(x - size / 2) / (size / 2);
                    float dy = (float)(y - size / 2) / (size / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    
                    Color pixelColor = baseBg;
                    
                    // Concentric rings at 0.33, 0.66, 0.98
                    float epsilon = 0.012f;
                    if (Mathf.Abs(dist - 0.33f) < epsilon || Mathf.Abs(dist - 0.66f) < epsilon || Mathf.Abs(dist - 0.98f) < epsilon)
                    {
                        pixelColor = Color.Lerp(pixelColor, gridColorBright, 0.75f);
                    }
                    // Horizontal and vertical grid lines
                    else if (Mathf.Abs(dx) < 0.01f || Mathf.Abs(dy) < 0.01f)
                    {
                        pixelColor = Color.Lerp(pixelColor, gridColor, 0.55f);
                    }
                    // Diagonal lines
                    else if (Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) < 0.015f)
                    {
                        pixelColor = Color.Lerp(pixelColor, gridColor, 0.35f);
                    }
                    
                    tex.SetPixel(x, y, pixelColor);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateMinimapBackgroundRectSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color baseBg = new Color(0.04f, 0.04f, 0.04f, 0.90f);
            Color gridColor = new Color(0.95f, 0.8f, 0.2f, 0.08f);
            Color gridColorBright = new Color(0.95f, 0.8f, 0.2f, 0.20f);
            
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color pixelColor = baseBg;
                    if (x % 40 == 0 || y % 40 == 0)
                    {
                        pixelColor = Color.Lerp(pixelColor, gridColorBright, 0.6f);
                    }
                    else if (x % 10 == 0 || y % 10 == 0)
                    {
                        pixelColor = Color.Lerp(pixelColor, gridColor, 0.3f);
                    }
                    
                    if (x < 3 || x > w - 4 || y < 3 || y > h - 4)
                    {
                        pixelColor = new Color(0.95f, 0.8f, 0.2f, 0.7f);
                    }
                    tex.SetPixel(x, y, pixelColor);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRadarSweepSprite(int size, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (float)(x - size / 2) / (size / 2);
                    float dy = (float)(y - size / 2) / (size / 2);
                    float dist = dx * dx + dy * dy;
                    
                    if (dist <= 0.98f)
                    {
                        // Calculate angle in radians from -PI to PI, then map to 0 to 1
                        float angle = Mathf.Atan2(dy, dx); 
                        if (angle < 0) angle += 2f * Mathf.PI;
                        
                        float progress = angle / (2f * Mathf.PI);
                        float alpha = Mathf.Pow(progress, 3.5f); // Sharp fading sector
                        
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * alpha * (1f - dist * 0.25f)));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateWarningTriangleSprite(int size, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (float)(x - size / 2) / (size / 2);
                    float py = (float)(y - size / 2) / (size / 2);
                    
                    // Triangle bounds: py from -0.8 to 0.8
                    float absX = Mathf.Abs(px);
                    float width = (0.8f - py) * 0.6f;
                    
                    if (py > -0.8f && py < 0.8f && absX <= width)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateMedicineIconSprite(int size, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (float)(x - size / 2) / (size / 2);
                    float dy = (float)(y - size / 2) / (size / 2);
                    float dist = dx * dx + dy * dy;
                    
                    if (dist <= 0.9f)
                    {
                        float absX = Mathf.Abs(dx);
                        float absY = Mathf.Abs(dy);
                        bool isCross = (absX < 0.22f && absY < 0.65f) || (absY < 0.22f && absX < 0.65f);
                        if (isCross)
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                        else
                        {
                            tex.SetPixel(x, y, col);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreatePlayerArrowSprite(int w, int h, Color col)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = (float)(x - w / 2) / (w / 2);
                    float py = (float)(y - h / 2) / (h / 2);
                    
                    float absX = Mathf.Abs(px);
                    bool insideWings = false;
                    
                    float t = (py - -0.8f) / 1.7f; // 0 to 1
                    float outerWidth = (1f - t) * 0.85f;
                    float innerNotch = -0.4f + absX * 0.5f;
                    
                    if (py < 0.9f && py > -0.8f)
                    {
                        if (absX <= outerWidth && py >= innerNotch)
                        {
                            insideWings = true;
                        }
                    }
                    
                    if (insideWings)
                    {
                        // Glowing red Ruby gem in the arrow core
                        float distToRuby = Vector2.Distance(new Vector2(px, py - 0.2f), Vector2.zero);
                        if (distToRuby <= 0.16f)
                        {
                            if (distToRuby >= 0.13f)
                            {
                                // Golden bezel frame around the ruby
                                float shade = 0.6f + 0.4f * Mathf.Clamp01((px + py + 1.2f) / 2.4f);
                                tex.SetPixel(x, y, new Color(0.95f * shade, 0.8f * shade, 0.2f * shade, 1f));
                            }
                            else
                            {
                                // Specular-shaded Ruby Gemstone
                                float rubyShade = 0.5f + 0.5f * Mathf.Clamp01((px + (py - 0.2f) + 0.16f) / 0.32f);
                                float rubyGlint = Mathf.Pow(Mathf.Clamp01(1.05f - Vector2.Distance(new Vector2(px, py - 0.2f), new Vector2(-0.06f, 0.06f))), 6f);
                                Color rubyColor = Color.Lerp(new Color(0.4f * rubyShade, 0.02f, 0.05f, 1f), new Color(0.95f * rubyShade, 0.05f, 0.15f, 1f), (py - 0.2f + 0.16f) / 0.32f);
                                rubyColor += new Color(rubyGlint * 0.8f, rubyGlint * 0.3f, rubyGlint * 0.3f, 0f);
                                tex.SetPixel(x, y, rubyColor);
                            }
                        }
                        else
                        {
                            // Double-check border contour
                            bool isBorder = (absX > outerWidth - 0.12f) || (py > 0.75f) || (py < innerNotch + 0.12f) || (absX < 0.12f);
                            
                            if (isBorder)
                            {
                                // Gold border with 3D bevel shading
                                float shade = 0.6f + 0.4f * Mathf.Clamp01((px + py + 1.2f) / 2.4f);
                                Color goldColor = new Color(0.95f * shade, 0.8f * shade, 0.2f * shade, 1f);
                                
                                float distToEdge = Mathf.Min(
                                    Mathf.Abs(absX - (outerWidth - 0.12f)),
                                    Mathf.Abs(py - 0.75f),
                                    Mathf.Abs(py - (innerNotch + 0.12f)),
                                    Mathf.Abs(absX - 0.12f)
                                );
                                if (distToEdge < 0.04f)
                                {
                                    goldColor = Color.Lerp(goldColor, new Color(1f, 0.95f, 0.6f, 1f), 0.5f);
                                }
                                tex.SetPixel(x, y, goldColor);
                            }
                            else
                            {
                                // Deep glittering royal Lapis Lazuli inlay with Pyrite (gold dust) veins
                                float gemGlint = Mathf.Pow(Mathf.Clamp01(1.1f - Vector2.Distance(new Vector2(px, py), new Vector2(-0.2f, 0.3f))), 7f);
                                Color lapisColor = Color.Lerp(new Color(0.04f, 0.15f, 0.4f, 0.95f), new Color(0.12f, 0.35f, 0.75f, 0.95f), (py + 1f) / 2f);
                                lapisColor += new Color(gemGlint * 0.4f, gemGlint * 0.5f, gemGlint * 0.8f, 0f);
                                
                                float pyriteVeins = Mathf.Sin(x * 0.9f) * Mathf.Cos(y * 0.9f);
                                if (pyriteVeins > 0.75f)
                                {
                                    lapisColor = Color.Lerp(lapisColor, new Color(0.95f, 0.85f, 0.4f, 0.95f), 0.25f);
                                }
                                
                                tex.SetPixel(x, y, lapisColor);
                            }
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Font GetRobustFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) f = fonts[0];
            }
            return f;
        }

        #endregion
    }
}