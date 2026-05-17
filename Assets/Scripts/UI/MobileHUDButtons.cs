using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        public static MobileHUDButtons Instance { get; private set; }

        private Sprite reloadIcon;
        private Sprite fireIcon;
        private Sprite swapIcon;
        private Sprite sprintIcon;
        private Sprite jumpIcon;

        private Text healthText;
        private Text ammoText;
        private Text weaponText;

        // Dynamic Bar Fills & Alchemical Sprites
        private Image healthBarFill;
        private Image ammoBarFill;
        private Sprite sulfurBarSprite;
        private Sprite mercuryBarSprite;
        private Sprite saltBarSprite;
        private Sprite punchBarSprite;

        // Settings Modal Instance tracking
        private GameObject settingsModalInstance = null;

        // Sprint Toggle references
        private bool sprintToggleState = false;
        private Image sprintIconImage;
        private Image sprintShadowImage;

        // Cached procedural sprites to avoid recreation GC pressure
        private Sprite obsidianSprite;
        private Sprite goldGradientSprite;
        private Sprite joystickRingSprite;
        private Sprite joystickKnobSprite;

        private void Awake()
        {
            Instance = this;
            LoadSprites();
            GenerateProceduralSprites();
            SetupCanvas();
            BuildHUD();
        }

        private Sprite LoadThemedSprite(string spriteName, string fallbackResourcePath)
        {
            Sprite result = Resources.Load<Sprite>("egypt_themed_icons/" + spriteName);
            if (result == null)
            {
                result = Resources.Load<Sprite>(fallbackResourcePath);
            }
            return result;
        }

        private void LoadSprites()
        {
            // Load custom Egypt-themed sprites from Resources
            joystickRingSprite = LoadThemedSprite("joystick_outer", "UI/Icons/joystick_ring_fallback");
            joystickKnobSprite = LoadThemedSprite("joystick_knob", "UI/Icons/joystick_knob_fallback");
            
            fireIcon = LoadThemedSprite("fire", "UI/Icons/Inspiration/bullet");
            reloadIcon = LoadThemedSprite("reload_ammo", "UI/Icons/Inspiration/reload");
            swapIcon = LoadThemedSprite("swap_weapon", "UI/Icons/icon_swap");
            sprintIcon = LoadThemedSprite("sprint", "UI/Icons/icon_sprint");
            jumpIcon = LoadThemedSprite("jump", "UI/Icons/icon_jump");
        }

        private void GenerateProceduralSprites()
        {
            obsidianSprite = CreateObsidianSprite();
            goldGradientSprite = CreateGoldenGradientSprite();
            
            if (joystickRingSprite == null) joystickRingSprite = CreateRingSprite();
            if (joystickKnobSprite == null) joystickKnobSprite = CreateKnobSprite();

            // Dynamic Alchemical bar sprites
            sulfurBarSprite = CreateAlchemicalBarSprite(new Color(0.95f, 0.55f, 0.05f), new Color(1f, 0.85f, 0.1f)); // Orange to Yellow
            mercuryBarSprite = CreateAlchemicalBarSprite(new Color(0.1f, 0.5f, 0.8f), new Color(0.4f, 0.9f, 0.95f)); // Cyan to Silver
            saltBarSprite = CreateAlchemicalBarSprite(new Color(0.9f, 0.7f, 0.2f), new Color(1f, 1f, 1f)); // Amber to White
            punchBarSprite = CreateAlchemicalBarSprite(new Color(0.4f, 0.02f, 0.02f), new Color(0.8f, 0.05f, 0.05f)); // Dark Crimson to Red
        }

        private Sprite CreateAlchemicalBarSprite(Color startCol, Color endCol)
        {
            int w = 420;
            int h = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (float)x / w;
                    Color col = Color.Lerp(startCol, endCol, t);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, 0.95f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateBorderSprite(int w, int h, int thickness, Color borderCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < thickness || x >= w - thickness || y < thickness || y >= h - thickness)
                    {
                        tex.SetPixel(x, y, borderCol);
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

        private Sprite CreateSolidCircleSprite(int s, Color col)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (float)(x - s / 2) / (s / 2);
                    float dy = (float)(y - s / 2) / (s / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f)
                    {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateHealthBarFillSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (float)x / w;
                    // Pure Emerald Green to Bright Golden Yellow
                    Color col = Color.Lerp(new Color(0.05f, 0.85f, 0.25f, 0.95f), new Color(0.95f, 0.85f, 0.05f, 0.95f), t);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateSolidBarSprite(int w, int h, Color c)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateObsidianSprite()
        {
            int width = 128;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color obsColor = Color.Lerp(new Color(0.08f, 0.08f, 0.08f, 0.95f), new Color(0.2f, 0.2f, 0.2f, 0.95f), t);
                for (int x = 0; x < width; x++)
                {
                    float dx = (float)(x - width / 2) / (width / 2);
                    float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f)
                    {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        tex.SetPixel(x, y, new Color(obsColor.r, obsColor.g, obsColor.b, obsColor.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateGoldenGradientSprite()
        {
            int width = 128;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color goldColor = Color.Lerp(new Color(0.85f, 0.6f, 0.1f, 0.95f), new Color(1f, 0.85f, 0.3f, 0.95f), t);
                for (int x = 0; x < width; x++)
                {
                    float dx = (float)(x - width / 2) / (width / 2);
                    float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f)
                    {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        tex.SetPixel(x, y, new Color(goldColor.r, goldColor.g, goldColor.b, goldColor.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRingSprite()
        {
            int width = 256;
            int height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (float)(x - width / 2) / (width / 2);
                    float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist >= 0.85f && dist <= 1f)
                    {
                        float alpha = 1f;
                        if (dist > 0.95f) alpha = (1f - dist) / 0.05f;
                        else if (dist < 0.9f) alpha = (dist - 0.85f) / 0.05f;
                        
                        tex.SetPixel(x, y, new Color(0.95f, 0.8f, 0.2f, alpha * 0.45f));
                    }
                    else if (dist < 0.85f)
                    {
                        tex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, 0.15f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateKnobSprite()
        {
            int width = 128;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color goldColor = Color.Lerp(new Color(0.95f, 0.8f, 0.2f, 0.95f), new Color(1f, 0.95f, 0.6f, 0.95f), t);
                for (int x = 0; x < width; x++)
                {
                    float dx = (float)(x - width / 2) / (width / 2);
                    float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f)
                    {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        if (dist >= 0.4f && dist <= 0.5f)
                        {
                            tex.SetPixel(x, y, new Color(0.6f, 0.45f, 0.1f, alpha * 0.95f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(goldColor.r, goldColor.g, goldColor.b, goldColor.a * alpha));
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateSettingsMedallionSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (float)(x - w / 2) / (w / 2);
                    float dy = (float)(y - h / 2) / (h / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist <= 1.0f)
                    {
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        float gearTeeth = Mathf.Sin(angle * 8 * Mathf.Deg2Rad); // 8-tooth gear pattern
                        
                        Color medallionCol = new Color(0.95f, 0.8f, 0.2f, 0.95f); // Rich gold
                        if (dist > 0.85f && gearTeeth > 0.1f)
                        {
                            tex.SetPixel(x, y, medallionCol);
                        }
                        else if (dist <= 0.85f && dist > 0.75f)
                        {
                            tex.SetPixel(x, y, new Color(0.6f, 0.45f, 0.1f, 0.95f)); // Darker gold ring border
                        }
                        else if (dist <= 0.75f && dist > 0.25f)
                        {
                            tex.SetPixel(x, y, new Color(0.08f, 0.08f, 0.08f, 0.9f)); // Obsidian core
                        }
                        else if (dist <= 0.25f)
                        {
                            tex.SetPixel(x, y, medallionCol); // Gold center pin
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
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

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // Auto-inject missing EventSystem if not present in the scene
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            }
            GameObject eventSystemGo;
            if (eventSystem == null)
            {
                eventSystemGo = new GameObject("EventSystem");
                eventSystem = eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }
            else
            {
                eventSystemGo = eventSystem.gameObject;
            }
            
            var legacyModule = eventSystemGo.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (legacyModule != null)
            {
                if (Application.isPlaying) Destroy(legacyModule);
                else DestroyImmediate(legacyModule);
            }
            
            var modernModule = eventSystemGo.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (modernModule == null)
            {
                modernModule = eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                modernModule.AssignDefaultActions();
            }
            Debug.Log("MobileHUDButtons: Injected modern EventSystem with InputSystemUIInputModule for reliable UI clicks!");
        }

        public void BuildHUD()
        {
            foreach (Transform t in transform) Destroy(t.gameObject);

            var root = new GameObject("HUD_Root", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            // 1. LOOK ZONE (Strictly Right 50% of screen - Zero Overlap)
            var lookZone = new GameObject("LookZone", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            lookZone.SetParent(root, false);
            lookZone.anchorMin = new Vector2(0.5f, 0f); lookZone.anchorMax = Vector2.one;
            lookZone.offsetMin = lookZone.offsetMax = Vector2.zero;
            lookZone.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            lookZone.gameObject.AddComponent<LookSwipeZone>();

            // 2. MOVEMENT ZONE (Strictly Left 50% of screen - Zero Overlap)
            var moveZone = new GameObject("MoveZone", typeof(RectTransform)).GetComponent<RectTransform>();
            moveZone.SetParent(root, false);
            moveZone.anchorMin = Vector2.zero; moveZone.anchorMax = new Vector2(0.5f, 1f);
            moveZone.offsetMin = moveZone.offsetMax = Vector2.zero;

            // --- NATIVE JOYSTICK UI GENERATION (SCALED DOWN FOR SWIPE SPACE) ---
            var joystickBg = new GameObject("NativeJoystick_Bg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            joystickBg.SetParent(moveZone, false);
            joystickBg.anchorMin = joystickBg.anchorMax = new Vector2(0.3f, 0.3f); 
            joystickBg.anchoredPosition = Vector2.zero;
            joystickBg.sizeDelta = new Vector2(220, 220); 

            var bgImage = joystickBg.GetComponent<Image>();
            bgImage.color = Color.white;
            if (joystickRingSprite != null) bgImage.sprite = joystickRingSprite;

            var joystickHandle = new GameObject("HandleTarget", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            joystickHandle.SetParent(joystickBg, false);
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickHandle.sizeDelta = new Vector2(220, 220); 

            var targetImage = joystickHandle.GetComponent<Image>();
            targetImage.color = new Color(0, 0, 0, 0); 
            targetImage.raycastTarget = true;

            var knobVisual = new GameObject("KnobVisual", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            knobVisual.SetParent(joystickHandle, false);
            knobVisual.anchoredPosition = Vector2.zero;
            knobVisual.sizeDelta = new Vector2(80, 80); 

            var visualImage = knobVisual.GetComponent<Image>();
            visualImage.color = Color.white;
            visualImage.raycastTarget = false;
            if (joystickKnobSprite != null) visualImage.sprite = joystickKnobSprite;

            var onScreenStick = joystickHandle.gameObject.AddComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
            onScreenStick.movementRange = 70f; 
            onScreenStick.controlPath = "<Gamepad>/leftStick"; 

            // 3. BUTTONS (CLUSTERED AND SCALED DOWN FOR SWIPE SPACE)
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0); // BOTTOM RIGHT
            btnContainer.anchoredPosition = Vector2.zero;

            CreateButton(btnContainer, "FIRE", new Vector2(-150, 150), 160, fireIcon, () => SetFire(true), () => SetFire(false));
            CreateButton(btnContainer, "RELOAD", new Vector2(-310, 80), 96, reloadIcon, () => Reload());
            CreateButton(btnContainer, "SWAP", new Vector2(-230, 300), 96, swapIcon, () => Swap());
            CreateSprintButton(btnContainer, new Vector2(-310, 220), 96);
            CreateButton(btnContainer, "JUMP", new Vector2(-80, 300), 96, jumpIcon, () => SetJump(true), () => SetJump(false));

            HideDebugLabels();

            // Disable default Low Poly Shooter Pack weapon UI panel canvas at runtime
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                if (canvas.gameObject.name.Contains("p_lpsp_ui_canvas") || canvas.gameObject.name.Contains("WeaponUI") || canvas.gameObject.name.Contains("LPSP"))
                {
                    canvas.gameObject.SetActive(false);
                }
            }

            // 4. CUSTOM TOP LEFT HEALTH PANEL (OBSIDIAN CARD WITH GOLD BORDER)
            var healthPanel = new GameObject("CustomHealthPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            healthPanel.SetParent(root, false);
            healthPanel.anchorMin = healthPanel.anchorMax = new Vector2(0, 1); // TOP LEFT
            healthPanel.anchoredPosition = new Vector2(40, -40);
            healthPanel.sizeDelta = new Vector2(360, 70);
            
            var healthPanelImg = healthPanel.GetComponent<Image>();
            healthPanelImg.sprite = obsidianSprite;
            healthPanelImg.color = Color.white;
            
            var healthPanelBorder = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            healthPanelBorder.SetParent(healthPanel, false);
            healthPanelBorder.anchorMin = Vector2.zero; healthPanelBorder.anchorMax = Vector2.one;
            healthPanelBorder.offsetMin = healthPanelBorder.offsetMax = Vector2.zero;
            var healthBorderImg = healthPanelBorder.GetComponent<Image>();
            healthBorderImg.sprite = CreateBorderSprite(360, 70, 3, new Color(0.95f, 0.8f, 0.2f, 0.9f));
            healthBorderImg.color = Color.white;
            
            // Health Bar Label
            var healthLabel = new GameObject("HealthLabel", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            healthLabel.rectTransform.SetParent(healthPanel, false);
            healthLabel.rectTransform.anchorMin = healthLabel.rectTransform.anchorMax = new Vector2(0, 0.5f);
            healthLabel.rectTransform.pivot = new Vector2(0, 0.5f);
            healthLabel.rectTransform.anchoredPosition = new Vector2(20, 0);
            healthLabel.rectTransform.sizeDelta = new Vector2(85, 30);
            healthLabel.text = "VITALITY";
            healthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthLabel.fontSize = 13;
            healthLabel.fontStyle = FontStyle.Bold;
            healthLabel.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); // Gold
            healthLabel.alignment = TextAnchor.MiddleLeft;

            // Health Text Display inside Card
            var healthTxtGo = new GameObject("HealthText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            healthTxtGo.SetParent(healthPanel, false);
            healthTxtGo.anchorMin = healthTxtGo.anchorMax = new Vector2(0, 0.5f);
            healthTxtGo.pivot = new Vector2(0, 0.5f);
            healthTxtGo.anchoredPosition = new Vector2(110, 0);
            healthTxtGo.sizeDelta = new Vector2(50, 30);
            healthText = healthTxtGo.GetComponent<Text>();
            healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthText.fontSize = 18;
            healthText.fontStyle = FontStyle.Bold;
            healthText.alignment = TextAnchor.MiddleCenter;
            healthText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            healthText.text = "100";

            // Health Bar Background
            var healthBarBg = new GameObject("HealthBarBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            healthBarBg.SetParent(healthPanel, false);
            healthBarBg.anchorMin = healthBarBg.anchorMax = new Vector2(0, 0.5f);
            healthBarBg.pivot = new Vector2(0, 0.5f);
            healthBarBg.anchoredPosition = new Vector2(170, 0);
            healthBarBg.sizeDelta = new Vector2(170, 20);
            var hbBgImg = healthBarBg.GetComponent<Image>();
            hbBgImg.sprite = CreateSolidBarSprite(170, 20, new Color(0.05f, 0.05f, 0.05f, 0.6f)); // Deep dark fill
            
            // Health Bar Fill
            var healthBarFillGo = new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            healthBarFillGo.SetParent(healthBarBg, false);
            healthBarFillGo.anchorMin = new Vector2(0, 0.5f);
            healthBarFillGo.anchorMax = new Vector2(0, 0.5f);
            healthBarFillGo.pivot = new Vector2(0, 0.5f);
            healthBarFillGo.anchoredPosition = Vector2.zero;
            healthBarFillGo.sizeDelta = new Vector2(170, 20);
            healthBarFill = healthBarFillGo.GetComponent<Image>();
            healthBarFill.sprite = CreateHealthBarFillSprite(170, 20);
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillAmount = 1f;

            // 4b. CUSTOM BOTTOM LEFT AMMO PANEL (OBSIDIAN CARD WITH GOLD BORDER)
            var ammoPanel = new GameObject("CustomAmmoPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ammoPanel.SetParent(root, false);
            ammoPanel.anchorMin = ammoPanel.anchorMax = new Vector2(0, 0); // BOTTOM LEFT
            ammoPanel.anchoredPosition = new Vector2(40, 40);
            ammoPanel.sizeDelta = new Vector2(360, 70);
            
            var ammoPanelImg = ammoPanel.GetComponent<Image>();
            ammoPanelImg.sprite = obsidianSprite;
            ammoPanelImg.color = Color.white;
            
            var ammoPanelBorder = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ammoPanelBorder.SetParent(ammoPanel, false);
            ammoPanelBorder.anchorMin = Vector2.zero; ammoPanelBorder.anchorMax = Vector2.one;
            ammoPanelBorder.offsetMin = ammoPanelBorder.offsetMax = Vector2.zero;
            var ammoBorderImg = ammoPanelBorder.GetComponent<Image>();
            ammoBorderImg.sprite = CreateBorderSprite(360, 70, 3, new Color(0.95f, 0.8f, 0.2f, 0.9f));
            ammoBorderImg.color = Color.white;
            
            // Ammo Bar Label
            var ammoLabel = new GameObject("AmmoLabel", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            ammoLabel.rectTransform.SetParent(ammoPanel, false);
            ammoLabel.rectTransform.anchorMin = ammoLabel.rectTransform.anchorMax = new Vector2(0, 0.5f);
            ammoLabel.rectTransform.pivot = new Vector2(0, 0.5f);
            ammoLabel.rectTransform.anchoredPosition = new Vector2(20, 0);
            ammoLabel.rectTransform.sizeDelta = new Vector2(85, 30);
            ammoLabel.text = "ESSENCE";
            ammoLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ammoLabel.fontSize = 13;
            ammoLabel.fontStyle = FontStyle.Bold;
            ammoLabel.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); // Gold
            ammoLabel.alignment = TextAnchor.MiddleLeft;

            // Ammo Text Display inside Card
            var ammoTxtGo = new GameObject("AmmoText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            ammoTxtGo.SetParent(ammoPanel, false);
            ammoTxtGo.anchorMin = ammoTxtGo.anchorMax = new Vector2(0, 0.5f);
            ammoTxtGo.pivot = new Vector2(0, 0.5f);
            ammoTxtGo.anchoredPosition = new Vector2(110, 0);
            ammoTxtGo.sizeDelta = new Vector2(50, 30);
            ammoText = ammoTxtGo.GetComponent<Text>();
            ammoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ammoText.fontSize = 18;
            ammoText.fontStyle = FontStyle.Bold;
            ammoText.alignment = TextAnchor.MiddleCenter;
            ammoText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            ammoText.text = "0 / 0";

            // Ammo Bar Background
            var ammoBarBg = new GameObject("AmmoBarBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ammoBarBg.SetParent(ammoPanel, false);
            ammoBarBg.anchorMin = ammoBarBg.anchorMax = new Vector2(0, 0.5f);
            ammoBarBg.pivot = new Vector2(0, 0.5f);
            ammoBarBg.anchoredPosition = new Vector2(170, 0);
            ammoBarBg.sizeDelta = new Vector2(170, 20);
            var abBgImg = ammoBarBg.GetComponent<Image>();
            abBgImg.sprite = CreateSolidBarSprite(170, 20, new Color(0.05f, 0.05f, 0.05f, 0.6f)); // Deep dark fill

            // Ammo Bar Fill
            var ammoBarFillGo = new GameObject("AmmoBarFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ammoBarFillGo.SetParent(ammoBarBg, false);
            ammoBarFillGo.anchorMin = new Vector2(0, 0.5f);
            ammoBarFillGo.anchorMax = new Vector2(0, 0.5f);
            ammoBarFillGo.pivot = new Vector2(0, 0.5f);
            ammoBarFillGo.anchoredPosition = Vector2.zero;
            ammoBarFillGo.sizeDelta = new Vector2(170, 20);
            ammoBarFill = ammoBarFillGo.GetComponent<Image>();
            ammoBarFill.sprite = punchBarSprite;
            ammoBarFill.type = Image.Type.Filled;
            ammoBarFill.fillMethod = Image.FillMethod.Horizontal;
            ammoBarFill.fillAmount = 1f;
            
            // Element/Weapon Label (Mini overlay on top of Ammo panel)
            var weaponTxtGo = new GameObject("WeaponText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            weaponTxtGo.SetParent(ammoPanel, false);
            weaponTxtGo.anchorMin = weaponTxtGo.anchorMax = new Vector2(0.5f, 1);
            weaponTxtGo.anchoredPosition = new Vector2(0, -5);
            weaponTxtGo.sizeDelta = new Vector2(200, 16);
            weaponText = weaponTxtGo.GetComponent<Text>();
            weaponText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            weaponText.fontSize = 10;
            weaponText.fontStyle = FontStyle.Normal;
            weaponText.alignment = TextAnchor.MiddleCenter;
            weaponText.color = new Color(1f, 0.85f, 0.4f, 0.8f);
            weaponText.text = "FOCUS: PUNCH";

            // 5. SPOOKY MINIMAP & COMPASS (TOP RIGHT - layered on HUD root)
            var minimapGo = new GameObject("MinimapCanvasContainer", typeof(RectTransform), typeof(MinimapUI));
            minimapGo.transform.SetParent(root, false);

            // 6. SETTINGS MEDALLION BUTTON (TOP RIGHT)
            var settingsBtnGo = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            settingsBtnGo.SetParent(root, false);
            settingsBtnGo.anchorMin = settingsBtnGo.anchorMax = new Vector2(1, 1); // TOP RIGHT
            settingsBtnGo.anchoredPosition = new Vector2(-60, -60);
            settingsBtnGo.sizeDelta = new Vector2(70, 70);
            
            var settingsImg = settingsBtnGo.GetComponent<Image>();
            settingsImg.sprite = CreateSettingsMedallionSprite(70, 70);
            settingsImg.color = Color.white;
            settingsImg.raycastTarget = true;
            
            var sHelper = settingsBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            sHelper.onDown = () => {
                settingsBtnGo.localScale = new Vector3(0.9f, 0.9f, 1f);
            };
            sHelper.onUp = () => {
                settingsBtnGo.localScale = new Vector3(1f, 1f, 1f);
                OpenSettingsModal(root);
            };
        }

        private void CreateButton(Transform p, string n, Vector2 pos, float s, Sprite icon, System.Action onDown, System.Action onUp = null)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            go.SetParent(p, false);
            go.anchoredPosition = pos;
            go.sizeDelta = new Vector2(s, s);
            
            var img = go.GetComponent<Image>();
            img.sprite = null;
            img.color = new Color(0, 0, 0, 0); // Make parent completely transparent but raycastable
            img.raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false);
            iconGo.anchorMin = Vector2.zero;
            iconGo.anchorMax = Vector2.one;
            iconGo.offsetMin = iconGo.offsetMax = Vector2.zero; // Stretch completely to match button size

            var iImg = iconGo.GetComponent<Image>();
            iImg.sprite = icon;
            iImg.color = Color.white; 
            iImg.raycastTarget = false;

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => {
                go.localScale = new Vector3(0.9f, 0.9f, 1f); 
                iImg.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Darken for feedback
                onDown?.Invoke();
            };
            helper.onUp = () => {
                go.localScale = new Vector3(1f, 1f, 1f);
                iImg.color = Color.white;
                onUp?.Invoke();
            };
        }

        private void CreateSprintButton(Transform p, Vector2 pos, float s)
        {
            var go = new GameObject("SPRINT", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            go.SetParent(p, false);
            go.anchoredPosition = pos;
            go.sizeDelta = new Vector2(s, s);
            
            var img = go.GetComponent<Image>();
            img.sprite = null;
            img.color = new Color(0, 0, 0, 0); // Transparent but raycastable parent
            img.raycastTarget = true;

            // 1. SHADOW / GLOW LAYER
            var shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            shadowGo.SetParent(go, false);
            shadowGo.anchorMin = Vector2.zero; shadowGo.anchorMax = Vector2.one;
            shadowGo.offsetMin = shadowGo.offsetMax = Vector2.zero;
            sprintShadowImage = shadowGo.GetComponent<Image>();
            sprintShadowImage.sprite = obsidianSprite; // Start with Obsidian shadow
            sprintShadowImage.color = Color.white;
            sprintShadowImage.raycastTarget = false;

            // 2. ICON LAYER
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false);
            iconGo.anchorMin = Vector2.zero; iconGo.anchorMax = Vector2.one;
            iconGo.offsetMin = iconGo.offsetMax = Vector2.zero;
            sprintIconImage = iconGo.GetComponent<Image>();
            sprintIconImage.sprite = sprintIcon;
            sprintIconImage.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Dimmed gold icon when off
            sprintIconImage.raycastTarget = false;

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => {
                sprintToggleState = !sprintToggleState; // Toggle state on click!
                
                // Audio or scale feedback
                go.localScale = new Vector3(0.9f, 0.9f, 1f);
                
                // Update visuals and state
                UpdateSprintVisuals();
                SetSprint(sprintToggleState);
            };
            helper.onUp = () => {
                go.localScale = new Vector3(1f, 1f, 1f);
            };
        }

        private void UpdateSprintVisuals()
        {
            if (sprintShadowImage == null || sprintIconImage == null) return;
            
            if (sprintToggleState)
            {
                // Active State: Gold glowing background, bright white/gold icon!
                sprintShadowImage.sprite = goldGradientSprite;
                sprintIconImage.color = Color.white;
            }
            else
            {
                // Inactive State: Obsidian dark shadow backing, slightly dimmed icon
                sprintShadowImage.sprite = obsidianSprite;
                sprintIconImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        private class ButtonInputHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public System.Action onDown;
            public System.Action onUp;
            public void OnPointerDown(PointerEventData eventData) => onDown?.Invoke();
            public void OnPointerUp(PointerEventData eventData) => onUp?.Invoke();
        }

        private class SliderDragHelper : MonoBehaviour, IDragHandler
        {
            public System.Action<Vector2> onDrag;
            public void OnDrag(PointerEventData eventData) => onDrag?.Invoke(eventData.position);
        }

        private void HideDebugLabels()
        {
            string[] names = { "Text Timescale", "Text Cursor Lock", "Text Tutorial", "Text Tutorial Text", "Text Tutorial Prompt", "Version Text", "Mouse Lock" };
            foreach (var n in names)
            {
                var label = GameObject.Find(n);
                if (label != null) label.SetActive(false);
                
                var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                foreach(var go in all) {
                    if (go.name.Contains(n)) go.SetActive(false);
                }
            }
        }

        private Text CreateStatsText(Transform p, string n, string v, Vector2 pos, Color c)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            go.SetParent(p, false);
            go.anchorMin = go.anchorMax = new Vector2(0, 1);
            go.pivot = new Vector2(0, 1); go.anchoredPosition = pos;
            go.sizeDelta = new Vector2(500, 60);
            var t = go.GetComponent<Text>();
            t.text = $"{n.ToUpper()}: {v}";
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 45; t.fontStyle = FontStyle.Bold; t.color = c;
            return t;
        }

        private void Update()
        {
            // Keep disabling LPSP UI if it is active
            var lpsp = GameObject.Find("p_lpsp_ui_canvas");
            if (lpsp != null && lpsp.activeSelf) lpsp.SetActive(false);

            // 1. Update active weapon indicator and Ammo
            var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
            if (character != null)
            {
                var weapon = character.GetEquippedWeapon();
                if (weapon != null)
                {
                    int currentAmmo = weapon.GetAmmunitionCurrent();
                    int totalAmmo = weapon.GetAmmunitionTotal();
                    UpdateAmmo(currentAmmo, totalAmmo);

                    // Dynamic ammo bar fill amount
                    if (ammoBarFill != null)
                    {
                        if (totalAmmo > 0)
                            ammoBarFill.fillAmount = Mathf.Clamp01((float)currentAmmo / (float)totalAmmo);
                        else
                            ammoBarFill.fillAmount = 0f;
                    }

                    string weaponName = "PUNCH";
                    
                    var focus = character.GetComponentInChildren<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
                    if (focus != null)
                    {
                        weaponName = focus.CurrentMode.ToString().ToUpper();
                    }
                    else if (weapon.name.Contains("Sulfur") || weapon.name.Contains("sulfur"))
                    {
                        weaponName = "SULFUR";
                    }
                    else if (weapon.name.Contains("Mercury") || weapon.name.Contains("mercury"))
                    {
                        weaponName = "MERCURY";
                    }
                    else if (weapon.name.Contains("Salt") || weapon.name.Contains("salt"))
                    {
                        weaponName = "SALT";
                    }
                    else
                    {
                        string cleanName = weapon.name.Replace("(Clone)", "").Trim().ToUpper();
                        if (cleanName.Contains("SULFUR")) weaponName = "SULFUR";
                        else if (cleanName.Contains("MERCURY")) weaponName = "MERCURY";
                        else if (cleanName.Contains("SALT")) weaponName = "SALT";
                        else weaponName = cleanName;
                    }

                    // Dynamically set Ammo Bar Sprite based on element
                    if (ammoBarFill != null)
                    {
                        if (weaponName == "SULFUR")
                            ammoBarFill.sprite = sulfurBarSprite;
                        else if (weaponName == "MERCURY")
                            ammoBarFill.sprite = mercuryBarSprite;
                        else if (weaponName == "SALT")
                            ammoBarFill.sprite = saltBarSprite;
                        else
                            ammoBarFill.sprite = punchBarSprite;
                    }

                    if (weaponText != null)
                    {
                        weaponText.text = $"FOCUS: {weaponName}";
                    }
                }
                else
                {
                    // No weapon active -> Punch/Spooky Dark Crimson
                    if (ammoBarFill != null)
                    {
                        ammoBarFill.sprite = punchBarSprite;
                        ammoBarFill.fillAmount = 0f;
                    }
                    if (weaponText != null)
                    {
                        weaponText.text = "FOCUS: PUNCH";
                    }
                    UpdateAmmo(0, 0);
                }
            }

            // 2. Update health
            var health = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Player.PlayerHealth>();
            if (health != null)
            {
                UpdateHealth(health.currentHealth);
            }
        }

        private void SetFire(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(s);
        private void SetSprint(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetSprinting(s);
        private void SetJump(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetJumping(s);
        private void Reload() {
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsReloading = true;
        }
        private void Swap() {
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsSwappingWeapon = true;
        }

        private void OpenSettingsModal(RectTransform parentCanvas)
        {
            if (settingsModalInstance != null) return;
            
            // Disable mobile inputs while settings are open
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
            {
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            }

            // Create Modal Background (Dimmer overlay)
            var modalBg = new GameObject("SettingsModal", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            modalBg.SetParent(parentCanvas, false);
            modalBg.anchorMin = Vector2.zero; modalBg.anchorMax = Vector2.one;
            modalBg.offsetMin = modalBg.offsetMax = Vector2.zero;
            
            var bgImg = modalBg.GetComponent<Image>();
            bgImg.sprite = null;
            bgImg.color = new Color(0, 0, 0, 0.75f); // Dim screen beautifully
            bgImg.raycastTarget = true; // Blocks events underneath
            
            settingsModalInstance = modalBg.gameObject;
            
            // Settings Dialog Box (Obsidian card with Gold outline)
            var dialog = new GameObject("Dialog", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            dialog.SetParent(modalBg, false);
            dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f);
            dialog.sizeDelta = new Vector2(500, 320);
            dialog.anchoredPosition = Vector2.zero;
            
            var dialImg = dialog.GetComponent<Image>();
            dialImg.sprite = obsidianSprite;
            dialImg.color = Color.white;
            
            // Gold border
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            border.SetParent(dialog, false);
            border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
            border.offsetMin = border.offsetMax = Vector2.zero;
            var borderImg = border.GetComponent<Image>();
            borderImg.sprite = CreateBorderSprite(500, 320, 5, new Color(0.95f, 0.8f, 0.2f, 0.95f));
            borderImg.color = Color.white;
            
            // Title text
            var title = new GameObject("TitleText", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            title.rectTransform.SetParent(dialog, false);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -35);
            title.rectTransform.sizeDelta = new Vector2(400, 40);
            title.text = "ALCHEMY SETTINGS";
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 28;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            
            // Sensitivity Slider Background
            var sliderBg = new GameObject("SliderBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sliderBg.SetParent(dialog, false);
            sliderBg.anchorMin = sliderBg.anchorMax = new Vector2(0.5f, 0.5f);
            sliderBg.anchoredPosition = new Vector2(0, -10);
            sliderBg.sizeDelta = new Vector2(360, 20);
            var sliderBgImg = sliderBg.GetComponent<Image>();
            sliderBgImg.sprite = CreateSolidBarSprite(360, 20, new Color(0.05f, 0.05f, 0.05f, 0.8f));
            
            // Gold fill of the slider (representing active level)
            var sliderFill = new GameObject("SliderFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sliderFill.SetParent(sliderBg, false);
            sliderFill.anchorMin = new Vector2(0, 0.5f); sliderFill.anchorMax = new Vector2(0, 0.5f);
            sliderFill.pivot = new Vector2(0, 0.5f);
            sliderFill.anchoredPosition = Vector2.zero;
            sliderFill.sizeDelta = new Vector2(360, 20);
            var fillImg = sliderFill.GetComponent<Image>();
            fillImg.sprite = CreateSolidBarSprite(360, 20, new Color(0.95f, 0.8f, 0.2f, 0.9f));
            fillImg.type = Image.Type.Filled;
            fillImg.fillAmount = 0.5f;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            
            // Slider Handle (Golden Medallion Circle)
            var sliderHandle = new GameObject("SliderHandle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sliderHandle.SetParent(sliderBg, false);
            sliderHandle.anchorMin = sliderHandle.anchorMax = new Vector2(0, 0.5f);
            sliderHandle.sizeDelta = new Vector2(40, 40);
            var handleImg = sliderHandle.GetComponent<Image>();
            handleImg.sprite = CreateSolidCircleSprite(40, new Color(0.95f, 0.8f, 0.2f, 0.95f));
            handleImg.color = Color.white;
            handleImg.raycastTarget = true;
            
            // Slider value text label
            var valText = new GameObject("ValueText", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            valText.rectTransform.SetParent(dialog, false);
            valText.rectTransform.anchorMin = valText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            valText.rectTransform.anchoredPosition = new Vector2(0, 40);
            valText.rectTransform.sizeDelta = new Vector2(400, 30);
            valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valText.fontSize = 20;
            valText.fontStyle = FontStyle.Bold;
            valText.alignment = TextAnchor.MiddleCenter;
            valText.color = new Color(1f, 0.85f, 0.4f, 0.95f);

            // Drag behavior for Slider
            float currentSensitivity = PlayerPrefs.GetFloat("MobileSensitivity", 0.04f);
            float minSens = 0.01f;
            float maxSens = 0.10f;
            
            System.Action<float> updateSliderValue = (sens) => {
                sens = Mathf.Clamp(sens, minSens, maxSens);
                PlayerPrefs.SetFloat("MobileSensitivity", sens);
                PlayerPrefs.Save();
                
                // Update LookSwipeZone instances if any
                var zones = FindObjectsByType<LookSwipeZone>(FindObjectsSortMode.None);
                foreach (var z in zones) z.sensitivity = sens;
                
                float pct = (sens - minSens) / (maxSens - minSens);
                fillImg.fillAmount = pct;
                sliderHandle.anchoredPosition = new Vector2(pct * 360f, 0);
                
                float mult = sens / 0.025f;
                valText.text = $"LOOK SENSITIVITY: {mult:F2}x ({sens:F3})";
            };
            
            // Initialize slider visuals
            updateSliderValue(currentSensitivity);
            
            // Wire Drag handler on handle
            var dragHelper = sliderHandle.gameObject.AddComponent<SliderDragHelper>();
            dragHelper.onDrag = (pointerPos) => {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderBg, pointerPos, null, out Vector2 localPt);
                float pct = Mathf.Clamp01(localPt.x / 360f);
                float newSens = minSens + pct * (maxSens - minSens);
                updateSliderValue(newSens);
            };

            // Close button (Gold outlines)
            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            closeGo.SetParent(dialog, false);
            closeGo.anchorMin = closeGo.anchorMax = new Vector2(0.5f, 0);
            closeGo.anchoredPosition = new Vector2(0, 45);
            closeGo.sizeDelta = new Vector2(180, 50);
            
            var closeImg = closeGo.GetComponent<Image>();
            closeImg.sprite = CreateSolidBarSprite(180, 50, new Color(0.05f, 0.05f, 0.05f, 0.8f));
            closeImg.color = Color.white;
            closeImg.raycastTarget = true;
            
            var closeBorder = new GameObject("CloseBorder", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            closeBorder.SetParent(closeGo, false);
            closeBorder.anchorMin = Vector2.zero; closeBorder.anchorMax = Vector2.one;
            closeBorder.offsetMin = closeBorder.offsetMax = Vector2.zero;
            closeBorder.GetComponent<Image>().sprite = CreateBorderSprite(180, 50, 3, new Color(0.95f, 0.8f, 0.2f, 0.9f));
            
            var closeTxt = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            closeTxt.rectTransform.SetParent(closeGo, false);
            closeTxt.rectTransform.anchorMin = closeTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            closeTxt.rectTransform.anchoredPosition = Vector2.zero;
            closeTxt.rectTransform.sizeDelta = new Vector2(160, 40);
            closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeTxt.fontSize = 20;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            closeTxt.text = "APPLY";
            closeTxt.raycastTarget = false;
            
            var closeHelper = closeGo.gameObject.AddComponent<ButtonInputHelper>();
            closeHelper.onDown = () => {
                closeGo.localScale = new Vector3(0.9f, 0.9f, 1f);
            };
            closeHelper.onUp = () => {
                closeGo.localScale = new Vector3(1f, 1f, 1f);
                Destroy(modalBg.gameObject);
                settingsModalInstance = null;
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                }
            };
        }

        public void UpdateHealth(float h) {
            if (healthText) healthText.text = $"{Mathf.CeilToInt(h)}";
            if (healthBarFill) healthBarFill.fillAmount = Mathf.Clamp01(h / 100f);
        }
        
        public void UpdateAmmo(int c, int t) {
            if (ammoText) ammoText.text = $"{c} / {t}";
            if (ammoBarFill && t > 0) ammoBarFill.fillAmount = Mathf.Clamp01((float)c / t);
        }
    }

    public class LookSwipeZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public float sensitivity = 0.04f; 
        private int trackedPointerId = -1;

        private void Start()
        {
            sensitivity = PlayerPrefs.GetFloat("MobileSensitivity", 0.04f);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (trackedPointerId != -1) return;
            trackedPointerId = data.pointerId;
            TheAlchemistsCrypt.Input.MobileInputManager.Instance?.NotifyTouchActive(true);
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != trackedPointerId) return;

            float baseDpi = 160f;
            float deviceDpi = Screen.dpi > 0 ? Screen.dpi : baseDpi;
            float dpiScale = baseDpi / deviceDpi;
            
            Vector2 delta = data.delta * sensitivity * dpiScale;

            if (delta.sqrMagnitude > 0.0001f)
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(delta);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId == trackedPointerId) {
                trackedPointerId = -1;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.ConsumeLook(); 
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.NotifyTouchActive(false);
            }
        }
    }
}
