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

        // Cached procedural sprites to avoid recreation GC pressure
        private Sprite obsidianSprite;
        private Sprite goldGradientSprite;
        private Sprite joystickRingSprite;
        private Sprite joystickKnobSprite;

        // Sprint Toggle references
        private bool sprintToggleState = false;
        private Image sprintIconImage;
        private Image sprintShadowImage;

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
            joystickBg.anchorMin = joystickBg.anchorMax = new Vector2(0.35f, 0.3f); 
            joystickBg.anchoredPosition = Vector2.zero;
            joystickBg.sizeDelta = new Vector2(300, 300); 

            var bgImage = joystickBg.GetComponent<Image>();
            bgImage.color = Color.white;
            if (joystickRingSprite != null) bgImage.sprite = joystickRingSprite;

            var joystickHandle = new GameObject("HandleTarget", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            joystickHandle.SetParent(joystickBg, false);
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickHandle.sizeDelta = new Vector2(300, 300); 

            var targetImage = joystickHandle.GetComponent<Image>();
            targetImage.color = new Color(0, 0, 0, 0); 
            targetImage.raycastTarget = true;

            var knobVisual = new GameObject("KnobVisual", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            knobVisual.SetParent(joystickHandle, false);
            knobVisual.anchoredPosition = Vector2.zero;
            knobVisual.sizeDelta = new Vector2(110, 110); 

            var visualImage = knobVisual.GetComponent<Image>();
            visualImage.color = Color.white;
            visualImage.raycastTarget = false;
            if (joystickKnobSprite != null) visualImage.sprite = joystickKnobSprite;

            var onScreenStick = joystickHandle.gameObject.AddComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
            onScreenStick.movementRange = 100f; 
            onScreenStick.controlPath = "<Gamepad>/leftStick"; 

            // 3. BUTTONS (CLUSTERED AND SCALED DOWN FOR SWIPE SPACE)
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0); // BOTTOM RIGHT
            btnContainer.anchoredPosition = Vector2.zero;

            CreateButton(btnContainer, "FIRE", new Vector2(-180, 180), 200, fireIcon, () => SetFire(true), () => SetFire(false));
            CreateButton(btnContainer, "RELOAD", new Vector2(-380, 90), 120, reloadIcon, () => Reload());
            CreateButton(btnContainer, "SWAP", new Vector2(-270, 360), 120, swapIcon, () => Swap());
            CreateSprintButton(btnContainer, new Vector2(-380, 270), 120);
            CreateButton(btnContainer, "JUMP", new Vector2(-90, 360), 120, jumpIcon, () => SetJump(true), () => SetJump(false));

            HideDebugLabels();

            // 4. STATS (TOP LEFT)
            var stats = new GameObject("Stats", typeof(RectTransform)).GetComponent<RectTransform>();
            stats.SetParent(root, false);
            stats.anchorMin = stats.anchorMax = new Vector2(0, 1);
            stats.anchoredPosition = new Vector2(100, -100);

            healthText = CreateStatsText(stats, "Health", "100", Vector2.zero, new Color(1, 0.4f, 0.4f));
            weaponText = CreateStatsText(stats, "Weapon", "NONE", new Vector2(0, -70), new Color(1f, 0.85f, 0.4f));

            // Ensure look zone is BEHIND buttons so it doesn't intercept clicks
            lookZone.SetAsFirstSibling();
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
                        weaponName = weapon.name.Replace("(Clone)", "").Trim().ToUpper();
                    }

                    if (weaponText != null)
                    {
                        weaponText.text = $"WEAPON: {weaponName}";
                    }
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

        public void UpdateHealth(float h) { if (healthText) healthText.text = $"HEALTH: {Mathf.CeilToInt(h)}"; }
        public void UpdateAmmo(int c, int t) { if (ammoText) ammoText.text = $"AMMO: {c} / {t}"; }
    }

    public class LookSwipeZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public float sensitivity = 0.025f; 
        private int trackedPointerId = -1;

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
