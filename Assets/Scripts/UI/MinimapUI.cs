using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TheAlchemistsCrypt.AI;

namespace TheAlchemistsCrypt.UI
{
    public class MinimapUI : MonoBehaviour, IPointerDownHandler
    {
        public static MinimapUI Instance { get; private set; }

        [Header("Settings")]
        public float scaleNormal = 1.5f; // Normal zoom (1.5 pixels per meter)
        public float scaleZoomedOut = 0.6f; // Zoomed out zoom (0.6 pixels per meter)
        private float currentScale = 1.5f;
        private bool isZoomedOut = false;

        private Transform playerTransform;
        private RectTransform mapContent;
        private RectTransform minimapFrame;
        private RectTransform compassRing;
        
        // Icon assets generated procedurally
        private Sprite obsidianSprite;
        private Sprite goldBorderSprite;
        private Sprite playerArrowSprite;
        private Sprite greenDotSprite;
        private Sprite brownDotSprite;
        private Sprite buildingSprite;

        // Track dynamic indicators
        private List<ZombieIndicator> zombieIndicators = new List<ZombieIndicator>();
        private List<StaticElementIndicator> staticIndicators = new List<StaticElementIndicator>();

        private class ZombieIndicator
        {
            public GameObject zombieGo;
            public RectTransform iconRect;
            public Image iconImage;
        }

        private class StaticElementIndicator
        {
            public Vector3 worldPos;
            public RectTransform iconRect;
        }

        private void Awake()
        {
            Instance = this;
            GenerateSprites();
        }

        private void Start()
        {
            // Auto-detect player
            var movement = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Movement>();
            if (movement != null) playerTransform = movement.transform;

            BuildMinimapUI();
            CacheStaticElements();
        }

        private void GenerateSprites()
        {
            obsidianSprite = CreateCircleSprite(240, new Color(0.04f, 0.04f, 0.04f, 0.9f));
            goldBorderSprite = CreateRingSprite(240, 6, new Color(0.95f, 0.8f, 0.2f, 0.95f));
            
            // Player Arrow (64x64 for gorgeous detail)
            playerArrowSprite = CreatePlayerArrowSprite(64, 64, new Color(0.95f, 0.8f, 0.2f, 0.95f));
            
            // Dynamic Indicators
            greenDotSprite = CreateSolidCircleSprite(16, new Color(0.1f, 0.9f, 0.1f, 0.95f)); // Glowing Green Zombie
            brownDotSprite = CreateSolidCircleSprite(12, new Color(0.6f, 0.4f, 0.2f, 0.8f)); // Crate/Barrel
            
            // Building block sprite
            buildingSprite = CreateSolidBarSprite(32, 32, new Color(0.85f, 0.7f, 0.4f, 0.65f)); // Sandy Yellow
        }

        private void BuildMinimapUI()
        {
            // Frame container positioned top-right
            minimapFrame = gameObject.GetComponent<RectTransform>();
            if (minimapFrame == null) minimapFrame = gameObject.AddComponent<RectTransform>();
            
            minimapFrame.anchorMin = minimapFrame.anchorMax = new Vector2(1, 1);
            minimapFrame.anchoredPosition = new Vector2(-160, -220); // Safe distance below top-right settings button
            minimapFrame.sizeDelta = new Vector2(240, 240);

            // Frame Image (Black Obsidian Base)
            var frameImage = gameObject.AddComponent<Image>();
            frameImage.sprite = obsidianSprite;
            frameImage.color = Color.white;
            frameImage.raycastTarget = true; // To receive clicks/taps for zooming!

            // Add Masking Container
            var maskGo = new GameObject("MaskContainer", typeof(RectTransform), typeof(Image), typeof(Mask));
            var maskRect = maskGo.GetComponent<RectTransform>();
            maskRect.SetParent(minimapFrame, false);
            maskRect.anchorMin = Vector2.zero; maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = maskRect.offsetMax = new Vector2(6, 6); // Offset slightly for border
            
            var maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = CreateCircleSprite(228, Color.white);
            maskGo.GetComponent<Mask>().showMaskGraphic = false;

            // Map Content (Holds all translating & rotating indicators)
            var mapContentGo = new GameObject("MapContent", typeof(RectTransform));
            mapContent = mapContentGo.GetComponent<RectTransform>();
            mapContent.SetParent(maskRect, false);
            mapContent.anchorMin = mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            mapContent.anchoredPosition = Vector2.zero;
            mapContent.sizeDelta = new Vector2(2000, 2000); // Massive coordinate space

            // Compass Ring (rotates, but does not translate)
            var compassRingGo = new GameObject("CompassRing", typeof(RectTransform));
            compassRing = compassRingGo.GetComponent<RectTransform>();
            compassRing.SetParent(maskRect, false);
            compassRing.anchorMin = compassRing.anchorMax = new Vector2(0.5f, 0.5f);
            compassRing.anchoredPosition = Vector2.zero;
            compassRing.sizeDelta = new Vector2(220, 220);

            // Add N, E, S, W text labels
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
                txt.fontSize = 14;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = directions[i] == "N" ? new Color(1f, 0.3f, 0.3f, 0.95f) : new Color(0.95f, 0.8f, 0.2f, 0.9f);
                txt.raycastTarget = false;
            }

            // Frame Gold Border Ring (layered on top)
            var borderGo = new GameObject("BorderRing", typeof(RectTransform), typeof(Image));
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.SetParent(minimapFrame, false);
            borderRect.anchorMin = Vector2.zero; borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
            
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.sprite = goldBorderSprite;
            borderImg.color = Color.white;
            borderImg.raycastTarget = false;

            // Player indicator (Static in the very center, pointing up)
            var playerGo = new GameObject("PlayerIndicator", typeof(RectTransform), typeof(Image));
            var playerRect = playerGo.GetComponent<RectTransform>();
            playerRect.SetParent(minimapFrame, false); // Directly on frame, does not translate/rotate!
            playerRect.anchorMin = playerRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerRect.anchoredPosition = Vector2.zero;
            playerRect.sizeDelta = new Vector2(32, 32);
            
            var playerImg = playerGo.GetComponent<Image>();
            playerImg.sprite = playerArrowSprite;
            playerImg.color = Color.white;
            playerImg.raycastTarget = false;
        }

        private void CacheStaticElements()
        {
            // Clear existing
            foreach (var s in staticIndicators)
            {
                if (s.iconRect != null) Destroy(s.iconRect.gameObject);
            }
            staticIndicators.Clear();

            currentScale = isZoomedOut ? scaleZoomedOut : scaleNormal;

            // 1. Scan for Pyramids and Buildings
            var allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo)
            {
                if (go == null) continue;
                string lowerName = go.name.ToLower();

                // Skip editor templates or temporary visual items
                if (go.transform.parent != null && go.transform.parent.name.Contains("Player")) continue;

                if (lowerName.Contains("pyramid"))
                {
                    CreateStaticIcon(go.transform.position, new Vector2(80, 80), buildingSprite);
                }
                else if (lowerName.Contains("house") || lowerName.Contains("building") || lowerName.Contains("temple"))
                {
                    CreateStaticIcon(go.transform.position, new Vector2(40, 40), buildingSprite);
                }
                else if (lowerName.Contains("tree") || lowerName.Contains("palm"))
                {
                    CreateStaticIcon(go.transform.position, new Vector2(16, 16), CreateSolidCircleSprite(16, new Color(0.1f, 0.5f, 0.15f, 0.8f)));
                }
                else if (lowerName.Contains("crate") || lowerName.Contains("barrel"))
                {
                    CreateStaticIcon(go.transform.position, new Vector2(8, 8), brownDotSprite);
                }
            }
        }

        private void CreateStaticIcon(Vector3 pos, Vector2 size, Sprite sprite)
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

            // Map world pos to icon position
            iconRect.anchoredPosition = new Vector2(pos.x * currentScale, pos.z * currentScale);

            var indicator = new StaticElementIndicator
            {
                worldPos = pos,
                iconRect = iconRect
            };
            staticIndicators.Add(indicator);
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var movement = Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Movement>();
                if (movement != null) playerTransform = movement.transform;
                if (playerTransform == null) return;
            }

            // Update Map translation & rotation relative to the player
            Vector3 playerPos = playerTransform.position;
            Vector3 playerRot = playerTransform.eulerAngles;

            mapContent.anchoredPosition = new Vector2(-playerPos.x * currentScale, -playerPos.z * currentScale);
            mapContent.localEulerAngles = new Vector3(0, 0, playerRot.y);
            
            if (compassRing != null)
            {
                compassRing.localEulerAngles = new Vector3(0, 0, playerRot.y);
            }

            // Update dynamic zombie indicators
            UpdateZombieIndicators();
        }

        private void UpdateZombieIndicators()
        {
            // Find active zombies
            var zombies = Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude);
            
            // Match indicator pool
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

                // Find or create indicator
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
                    indicator.iconRect.sizeDelta = new Vector2(16, 16);
                    
                    indicator.iconImage = go.GetComponent<Image>();
                    indicator.iconImage.sprite = greenDotSprite;
                    indicator.iconImage.color = Color.white;
                    indicator.iconImage.raycastTarget = false;

                    zombieIndicators.Add(indicator);
                }

                indicator.zombieGo = z.gameObject;
                indicator.iconRect.gameObject.SetActive(true);
                
                // Position relative to mapContent (world position)
                Vector3 zPos = z.transform.position;
                indicator.iconRect.anchoredPosition = new Vector2(zPos.x * currentScale, zPos.z * currentScale);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Toggle zoom on tap/click!
            isZoomedOut = !isZoomedOut;
            currentScale = isZoomedOut ? scaleZoomedOut : scaleNormal;
            
            Debug.Log($"MinimapUI: Toggled Zoom! ZoomedOut={isZoomedOut}, CurrentScale={currentScale}");

            if (minimapFrame != null)
            {
                minimapFrame.sizeDelta = isZoomedOut ? new Vector2(480, 480) : new Vector2(240, 240);
                minimapFrame.anchoredPosition = isZoomedOut ? new Vector2(-280, -340) : new Vector2(-160, -220);
            }

            if (compassRing != null)
            {
                compassRing.sizeDelta = isZoomedOut ? new Vector2(440, 440) : new Vector2(220, 220);
                float labelRadius = isZoomedOut ? 170f : 85f;
                foreach (Transform child in compassRing)
                {
                    var labelRect = child.GetComponent<RectTransform>();
                    if (labelRect != null)
                    {
                        if (child.name == "Label_N") labelRect.anchoredPosition = new Vector2(0, labelRadius);
                        else if (child.name == "Label_E") labelRect.anchoredPosition = new Vector2(labelRadius, 0);
                        else if (child.name == "Label_S") labelRect.anchoredPosition = new Vector2(0, -labelRadius);
                        else if (child.name == "Label_W") labelRect.anchoredPosition = new Vector2(-labelRadius, 0);
                    }
                }
            }

            // Instantly update static indicators' positions
            foreach (var s in staticIndicators)
            {
                if (s.iconRect != null)
                {
                    s.iconRect.anchoredPosition = new Vector2(s.worldPos.x * currentScale, s.worldPos.z * currentScale);
                }
            }
        }

        #region Procedural Sprite Utilities

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