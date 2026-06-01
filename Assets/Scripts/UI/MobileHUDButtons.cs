using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

namespace TheAlchemistsCrypt.UI
{
    public partial class MobileHUDButtons : MonoBehaviour
    {
        public static MobileHUDButtons Instance { get; private set; }
        public static bool IsCustomizingHUD = false;

        private Sprite reloadIcon;
        private Sprite fireIcon;
        private Sprite swapIcon;
        private Sprite sprintIcon;
        private Sprite jumpIcon;
        private Sprite focusIcon;

        private TextMeshProUGUI healthText;
        private TextMeshProUGUI ammoText;
        private TextMeshProUGUI weaponText;
        private TextMeshProUGUI healthValueText;
        private TextMeshProUGUI ammoValueText;
        private TextMeshProUGUI killsText;
        private TextMeshProUGUI elementText;

        private Image healthBarFill;
        private Image ammoBarFill;

        private Image gameplayBloodVignette;
        private Sprite sulfurBarSprite;
        private Sprite mercuryBarSprite;
        private Sprite saltBarSprite;
        private Sprite punchBarSprite;

        private GameObject settingsModalInstance = null;
        private GameObject hudRootGo;
        private GameObject deathPanelInstance = null;

        private bool sprintToggleState = false;
        private Image sprintButtonIconImg;
        private Image sprintIndicatorImg;
        private Image sprintShadowImage;

        private Sprite obsidianSprite;
        private Sprite charcoalSprite;
        private Sprite goldGradientSprite;
        private Sprite joystickRingSprite;
        private Sprite joystickKnobSprite;
        private Sprite sandstoneFrameSprite;
        private Sprite goldTrimmedButtonSprite;
        private Sprite orangeGlowSprite;
        private Sprite cyanGlowSprite;
        
        private Sprite healthIconSprite;
        private Sprite sulphurIconSprite;
        private Sprite mercuryIconSprite;
        private Sprite saltIconSprite;
        private Sprite welcomeBgSprite;

        private Image ammoIconImage;
        private TextMeshProUGUI sprintButtonText;

        private RectTransform guideArrowRect;
        private CanvasGroup guideArrowCanvasGroup;
        private Sprite guideArrowSprite;
        private TextMeshProUGUI guideArrowText;
        private Image guideArrowImage;
        private Image guideArrowOutlineImage;

        private GameObject bootFader = null;
        private GameObject startScreenCanvasInstance = null;
        private GameObject startScreenBgGo = null;
        private GameObject startScreenBottomPanelGo = null;
        private GameObject activeDifficultyDropdown = null;

        private void Awake()
        {
            Instance = this;
            
            // Create a temporary black overlay immediately on Awake to prevent city flash
            if (!HasStartedGame)
            {
                var canvas = GetComponent<Canvas>();
                if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9998; // right under BootCanvas (9999) but above game camera

                var scaler = GetComponent<CanvasScaler>();
                if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1.0f;

                bootFader = new GameObject("BootFader", typeof(RectTransform), typeof(Image));
                bootFader.transform.SetParent(transform, false);
                var r = bootFader.GetComponent<RectTransform>();
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
                bootFader.GetComponent<Image>().color = Color.black;
            }
        }



        private class ButtonInputHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler {
            public System.Action onDown; public System.Action onUp; public System.Action onClick;
            public GameObject glowObject;
            public bool allowClicksDuringCustomization = false;
            public bool isDraggable = false;
            private RectTransform rectTransform;

            private void Awake()
            {
                rectTransform = GetComponent<RectTransform>();
            }

            public void OnPointerDown(PointerEventData data)
            {
                if (IsCustomizingHUD && !allowClicksDuringCustomization) return;
                transform.localScale = Vector3.one * 0.95f; // tactile scale squeeze feedback
                if (glowObject != null) glowObject.SetActive(true);
                onDown?.Invoke();
            }

            public void OnPointerUp(PointerEventData data)
            {
                if (IsCustomizingHUD && !allowClicksDuringCustomization) return;
                transform.localScale = Vector3.one; // restore scale
                if (glowObject != null) glowObject.SetActive(false);
                if (onClick == null) onUp?.Invoke();
            }

            public void OnPointerClick(PointerEventData data)
            {
                if (IsCustomizingHUD && !allowClicksDuringCustomization) return;
                if (onClick != null) onClick.Invoke();
            }

            public void OnPointerEnter(PointerEventData data)
            {
                if (IsCustomizingHUD && !allowClicksDuringCustomization) return;
                if (glowObject != null) glowObject.SetActive(true);
            }

            public void OnPointerExit(PointerEventData data)
            {
                if (IsCustomizingHUD && !allowClicksDuringCustomization) return;
                if (glowObject != null) glowObject.SetActive(false);
            }

            public void OnDrag(PointerEventData data)
            {
                if (!IsCustomizingHUD || !isDraggable || rectTransform == null) return;
                
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Vector2 localPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rectTransform.parent as RectTransform,
                        data.position,
                        canvas.worldCamera,
                        out localPos
                    );
                    rectTransform.anchoredPosition = localPos;
                    
                    string btnName = gameObject.name;
                    PlayerPrefs.SetFloat("ButtonPos_" + btnName + "_X", localPos.x);
                    PlayerPrefs.SetFloat("ButtonPos_" + btnName + "_Y", localPos.y);
                    PlayerPrefs.Save();
                }
            }
        }

        private class SliderDragHelper : MonoBehaviour, IDragHandler {
            public System.Action<Vector2> onDrag;
            public void OnDrag(PointerEventData data) => onDrag?.Invoke(data.position);
        }

        private TheAlchemistsCrypt.Weapons.AlchemicalFocus cachedFocus;
        private InfimaGames.LowPolyShooterPack.Character cachedCharacter;
        private TheAlchemistsCrypt.Player.PlayerHealth cachedHealth;
        private bool isCacheInitialized = false;
        private float lastEscTime = 0f;

        private void Update()
        {
            if (!HasStartedGame || settingsModalInstance != null || deathPanelInstance != null)
            {
                if (narrationPanel != null && narrationPanel.activeSelf) narrationPanel.SetActive(false);
            }

            if (settingsModalInstance != null) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            }

            // ON desktop, escape should trigger settings toggling using modern Input System API.
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.isPressed) {
                if (Time.unscaledTime - lastEscTime > 0.3f) {
                    lastEscTime = Time.unscaledTime;
                    ToggleSettingsFromEscape();
                }
            }

            // Optimize: Initialize cache periodically if not fully found, instead of every frame
            if (!isCacheInitialized || Time.frameCount % 60 == 0)
            {
                TryInitializeCache();
            }

            // Update alchemical mode icon in Ammo panel
            Sprite activeElementIcon = sulphurIconSprite;
            if (cachedFocus != null)
            {
                switch (cachedFocus.CurrentMode)
                {
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Sulfur:
                        activeElementIcon = sulphurIconSprite;
                        break;
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Mercury:
                        activeElementIcon = mercuryIconSprite;
                        break;
                    case TheAlchemistsCrypt.Weapons.AlchemicalFocus.FireMode.Salt:
                        activeElementIcon = saltIconSprite;
                        break;
                }
            }
            else if (cachedCharacter != null)
            {
                var weapon = cachedCharacter.GetEquippedWeapon();
                if (weapon != null)
                {
                    string wName = weapon.name.ToLower();
                    if (wName.Contains("sulfur")) activeElementIcon = sulphurIconSprite;
                    else if (wName.Contains("mercury")) activeElementIcon = mercuryIconSprite;
                    else if (wName.Contains("salt")) activeElementIcon = saltIconSprite;
                }
            }
            
            if (ammoIconImage != null && activeElementIcon != null)
            {
                ammoIconImage.sprite = activeElementIcon;
            }

            int current = 30;
            int total = 30;
            if (cachedFocus != null)
            {
                current = cachedFocus.CurrentAmmo;
                total = cachedFocus.MaxAmmo;
            }
            else if (cachedCharacter != null)
            {
                var weapon = cachedCharacter.GetEquippedWeapon();
                if (weapon != null) {
                    current = weapon.GetAmmunitionCurrent();
                    total = weapon.GetAmmunitionTotal();
                }
            }

            UpdateAmmo(current, total);
            if (cachedHealth != null) UpdateHealth(cachedHealth.currentHealth);
            
            if (killsText != null && TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null)
            {
                int currentKills = TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.currentKills;
                int reqKills = TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.requiredKills;
                if (currentKills >= reqKills)
                {
                    killsText.text = $"KILLS: {currentKills}/{reqKills} (ESCAPE READY)";
                    killsText.color = new Color(0.2f, 1.0f, 0.4f, 1f);
                }
                else
                {
                    killsText.text = $"KILLS: {currentKills}/{reqKills}";
                    killsText.color = new Color(1.0f, 0.8f, 0.2f, 1f);
                }
            }

            UpdateGuideArrow();
            UpdateAnimations();
        }

        public void ToggleSettingsFromEscape()
        {
            if (deathPanelInstance != null) return;
            
            // If settings modal is open, Escape can always close it
            if (settingsModalInstance != null)
            {
                var bg = settingsModalInstance;
                Destroy(bg);
                settingsModalInstance = null;
                
                if (HasStartedGame)
                {
                    Time.timeScale = 1f; // RESUME THE GAME!
                    if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                    
                    if (cachedCharacter != null)
                    {
                        cachedCharacter.SetCursorLocked(true);
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
                else
                {
                    // Start screen settings cleanup: show start screen background/buttons again!
                    if (startScreenBgGo != null) startScreenBgGo.SetActive(true);
                    if (startScreenBottomPanelGo != null) startScreenBottomPanelGo.SetActive(true);
                }
                return;
            }

            if (!HasStartedGame) return; // Escape does not open settings on start screen
            
            if (Time.unscaledTime - lastEscTime > 0.3f)
            {
                lastEscTime = Time.unscaledTime;
                var canvas = GetComponent<Canvas>();
                if (canvas != null)
                {
                    OpenSettingsModal(canvas.GetComponent<RectTransform>());
                }
                
                if (cachedCharacter != null)
                {
                    cachedCharacter.SetCursorLocked(false);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private Sprite CreateProceduralArrowSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float px = (x - half) / half;
                    float py = (y - half) / half;

                    // Double chevron pointing UP
                    // Upper chevron
                    float val1 = py + Mathf.Abs(px) * 0.8f;
                    bool c1 = val1 <= 0.5f && val1 >= 0.25f && py >= -0.1f && py <= 0.5f && Mathf.Abs(px) <= 0.5f;

                    // Lower chevron
                    float val2 = py + Mathf.Abs(px) * 0.8f;
                    bool c2 = val2 <= 0.0f && val2 >= -0.25f && py >= -0.6f && py <= 0.0f && Mathf.Abs(px) <= 0.5f;

                    if (c1 || c2) tex.SetPixel(x, y, Color.white);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateProceduralRingSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= 0.88f && dist <= 0.98f) {
                        float alpha = 1f;
                        if (dist < 0.91f) alpha = (dist - 0.88f) / 0.03f;
                        else if (dist > 0.95f) alpha = (0.98f - dist) / 0.03f;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    } else {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void UpdateGuideArrow()
        {
            if (guideArrowCanvasGroup == null || guideArrowRect == null) return;

            // Target Priority: EscapeManager's active task target
            GameObject target = null;
            bool isBoat = false;

            if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null)
            {
                var em = TheAlchemistsCrypt.Gameplay.EscapeManager.Instance;
                
                if (!em.papyrusSpawned)
                {
                    // Do not show any chevron if the papyrus hasn't even spawned yet! (User kills < 20)
                    target = null;
                }
                else if (!em.hasKey)
                {
                    target = em.keyObj;
                    isBoat = false;
                }
                else
                {
                    target = em.boatObj;
                    isBoat = true;
                }
            }

            // Fallback if EscapeManager is not initialized or null
            if (target == null)
            {
                if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null)
                {
                    if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.papyrusSpawned)
                    {
                        target = GameObject.Find("AncientPapyrus");
                        isBoat = false;
                        if (target == null)
                        {
                            target = GameObject.Find("EscapeBoat");
                            isBoat = true;
                        }
                    }
                }
                else
                {
                    // If EscapeManager is null, only point to AncientPapyrus if it is explicitly found in the scene
                    target = GameObject.Find("AncientPapyrus");
                    isBoat = false;
                }
            }

            if (target == null) {
                guideArrowCanvasGroup.alpha = Mathf.MoveTowards(guideArrowCanvasGroup.alpha, 0f, Time.deltaTime * 3f);
                return;
            }

            GameObject player = null;
            if (cachedCharacter != null) player = cachedCharacter.gameObject;

            if (player == null)
            {
                guideArrowCanvasGroup.alpha = Mathf.MoveTowards(guideArrowCanvasGroup.alpha, 0f, Time.deltaTime * 3f);
                return;
            }

            // Dynamic Styling
            if (guideArrowText != null) {
                guideArrowText.text = isBoat ? "ESCAPE TO BOAT" : "FIND PAPYRUS";
                Color indicatorCol = isBoat ? new Color(1f, 0.75f, 0.1f) : new Color(0.2f, 0.9f, 1f); // Gold vs Cyan
                guideArrowText.color = indicatorCol;
                if (guideArrowImage != null) guideArrowImage.color = indicatorCol;
                if (guideArrowOutlineImage != null) guideArrowOutlineImage.color = indicatorCol;
            }

            // Determine if the player is moving
            bool isMoving = false;
            
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null &&
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.MovementInput.sqrMagnitude > 0.01f)
            {
                isMoving = true;
            }
            else if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var k = UnityEngine.InputSystem.Keyboard.current;
                if (k.wKey.isPressed || k.sKey.isPressed || k.aKey.isPressed || k.dKey.isPressed ||
                    k.upArrowKey.isPressed || k.downArrowKey.isPressed || k.leftArrowKey.isPressed || k.rightArrowKey.isPressed)
                {
                    isMoving = true;
                }
            }

            // Fade in if moving, fade out if stationary
            float targetAlpha = isMoving ? 1f : 0f;
            guideArrowCanvasGroup.alpha = Mathf.MoveTowards(guideArrowCanvasGroup.alpha, targetAlpha, Time.deltaTime * 3f);

            if (guideArrowCanvasGroup.alpha > 0.01f)
            {
                // Rotation
                Vector3 dir = (target.transform.position - player.transform.position);
                dir.y = 0; dir.Normalize();
                float angle = Vector3.SignedAngle(player.transform.forward, dir, Vector3.up);
                guideArrowRect.localRotation = Quaternion.Euler(0, 0, -angle);

                // Bobbing & Pulsing Animations
                float bob = Mathf.Sin(Time.time * 6f) * 10f;
                guideArrowRect.anchoredPosition = new Vector2(0, 25f + bob);
                
                float pulse = 1f + Mathf.PingPong(Time.time * 0.8f, 0.15f);
                guideArrowRect.localScale = Vector3.one * pulse;
            }
        }

        private void SetAiming(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetAiming(s);
        private void SetFire(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(s);
        private void SetSprint(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetSprinting(s);
        private void SetJump(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetJumping(s);
        private void Reload() { if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsReloading = true; }
        private void Swap() { if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsSwappingWeapon = true; }

        private Vector2 GetButtonPosition(string btnName, Vector2 defaultPos)
        {
            float x = PlayerPrefs.GetFloat("ButtonPos_" + btnName + "_X", defaultPos.x);
            float y = PlayerPrefs.GetFloat("ButtonPos_" + btnName + "_Y", defaultPos.y);
            return new Vector2(x, y);
        }

        private void SaveButtonPos(string btnName, Vector2 pos)
        {
            PlayerPrefs.SetFloat("ButtonPos_" + btnName + "_X", pos.x);
            PlayerPrefs.SetFloat("ButtonPos_" + btnName + "_Y", pos.y);
        }

        private void ApplyPreset(string presetName)
        {
            PlayerPrefs.SetString("HUD_Preset", presetName);
            
            if (presetName == "DEFAULT")
            {
                // Positions scaled 0.75x from original
                SaveButtonPos("FIRE", new Vector2(-165, 165));
                SaveButtonPos("RELOAD", new Vector2(-390, 112));
                SaveButtonPos("SWAP", new Vector2(-270, 465));
                SaveButtonPos("SPRINT", new Vector2(-487, 225));
                SaveButtonPos("FOCUS", new Vector2(-337, 315));
                SaveButtonPos("JUMP", new Vector2(-112, 390));
                SaveButtonPos("NativeJoystick_Bg", new Vector2(300, 300));
            }
            else if (presetName == "COMPACT")
            {
                SaveButtonPos("FIRE", new Vector2(-135, 135));
                SaveButtonPos("RELOAD", new Vector2(-315, 90));
                SaveButtonPos("SWAP", new Vector2(-217, 375));
                SaveButtonPos("SPRINT", new Vector2(-390, 180));
                SaveButtonPos("FOCUS", new Vector2(-270, 255));
                SaveButtonPos("JUMP", new Vector2(-90, 315));
                SaveButtonPos("NativeJoystick_Bg", new Vector2(250, 250));
            }
            else if (presetName == "LEFTY")
            {
                SaveButtonPos("FIRE", new Vector2(165, 165));
                SaveButtonPos("RELOAD", new Vector2(390, 112));
                SaveButtonPos("SWAP", new Vector2(270, 465));
                SaveButtonPos("SPRINT", new Vector2(487, 225));
                SaveButtonPos("FOCUS", new Vector2(337, 315));
                SaveButtonPos("JUMP", new Vector2(112, 390));
                SaveButtonPos("NativeJoystick_Bg", new Vector2(1620, 300));
            }
            PlayerPrefs.Save();

            if (IsCustomizingHUD)
            {
                UpdateHUDButtonPositionsOnScreen();
            }
            else
            {
                BuildHUD();
            }
        }

        private void ResetToFactoryDefaults()
        {
            PlayerPrefs.DeleteKey("HUD_Preset");
            PlayerPrefs.DeleteKey("ButtonPos_FIRE_X");
            PlayerPrefs.DeleteKey("ButtonPos_FIRE_Y");
            PlayerPrefs.DeleteKey("ButtonPos_RELOAD_X");
            PlayerPrefs.DeleteKey("ButtonPos_RELOAD_Y");
            PlayerPrefs.DeleteKey("ButtonPos_SWAP_X");
            PlayerPrefs.DeleteKey("ButtonPos_SWAP_Y");
            PlayerPrefs.DeleteKey("ButtonPos_SPRINT_X");
            PlayerPrefs.DeleteKey("ButtonPos_SPRINT_Y");
            PlayerPrefs.DeleteKey("ButtonPos_FOCUS_X");
            PlayerPrefs.DeleteKey("ButtonPos_FOCUS_Y");
            PlayerPrefs.DeleteKey("ButtonPos_JUMP_X");
            PlayerPrefs.DeleteKey("ButtonPos_JUMP_Y");
            PlayerPrefs.DeleteKey("ButtonPos_NativeJoystick_Bg_X");
            PlayerPrefs.DeleteKey("ButtonPos_NativeJoystick_Bg_Y");
            PlayerPrefs.Save();

            if (IsCustomizingHUD)
            {
                UpdateHUDButtonPositionsOnScreen();
            }
            else
            {
                BuildHUD();
            }
        }

        private void StartHUDCustomization()
        {
            if (settingsModalInstance != null)
            {
                Destroy(settingsModalInstance);
                settingsModalInstance = null;
            }

            IsCustomizingHUD = true;
            Time.timeScale = 0f;

            if (!HasStartedGame && startScreenCanvasInstance != null)
            {
                startScreenCanvasInstance.SetActive(false);
            }

            var canvas = transform.GetComponent<RectTransform>();
            
            // Add customRoot to HUD_Root so it blocks under-layers (joystick, looking) but sits behind the buttons
            var customRoot = new GameObject("HUDCustomizerOverlay", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            customRoot.SetParent(hudRootGo != null ? hudRootGo.transform : canvas, false);
            customRoot.anchorMin = Vector2.zero; customRoot.anchorMax = Vector2.one;
            customRoot.offsetMin = customRoot.offsetMax = Vector2.zero;
            customRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            // Put button container in front of the overlay so they can be dragged
            var btnContainer = hudRootGo != null ? hudRootGo.transform.Find("ButtonContainer") : null;
            if (btnContainer != null)
            {
                btnContainer.SetAsLastSibling();
            }

            var joystickBg = hudRootGo != null ? hudRootGo.transform.Find("NativeJoystick_Bg") : null;
            if (joystickBg != null)
            {
                joystickBg.SetAsLastSibling();
                var sHelper = joystickBg.gameObject.AddComponent<ButtonInputHelper>();
                sHelper.isDraggable = true;
                sHelper.allowClicksDuringCustomization = true;

                var handleTarget = joystickBg.Find("HandleTarget");
                if (handleTarget != null)
                {
                    var dragHandler = handleTarget.GetComponent<JoystickDragHandler>();
                    if (dragHandler != null) dragHandler.enabled = false;
                    var img = handleTarget.GetComponent<Image>();
                    if (img != null) img.raycastTarget = false;
                }
            }

            // Create a premium, full-width top horizontal menu bar of height 85
            var panelGo = new GameObject("CustomizerPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panelGo.SetParent(customRoot, false);
            panelGo.anchorMin = new Vector2(0f, 1f); // Top left
            panelGo.anchorMax = new Vector2(1f, 1f); // Top right
            panelGo.pivot = new Vector2(0.5f, 1f);
            panelGo.anchoredPosition = Vector2.zero; // Touches the top of the screen
            panelGo.sizeDelta = new Vector2(0f, 85f); // Height 85
            
            var panelImg = panelGo.GetComponent<Image>();
            panelImg.sprite = null; // Clean obsidian top bar
            panelImg.color = new Color(0.06f, 0.05f, 0.05f, 0.9f); // Dark semi-transparent
            
            // Add Canvas override sorting to panelGo so it always sits on top of standard buttons and receives clicks
            var panelCanvas = panelGo.gameObject.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 1005; // Drawn above ButtonContainer (999)
            panelGo.gameObject.AddComponent<GraphicRaycaster>();

            // Thin gold border line at the bottom
            var borderGo = new GameObject("BottomBorder", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            borderGo.SetParent(panelGo, false);
            borderGo.anchorMin = new Vector2(0f, 0f);
            borderGo.anchorMax = new Vector2(1f, 0f);
            borderGo.pivot = new Vector2(0.5f, 0f);
            borderGo.anchoredPosition = Vector2.zero;
            borderGo.sizeDelta = new Vector2(0f, 3f); // 3px height
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.color = new Color(0.95f, 0.8f, 0.2f, 0.9f); // Egyptian gold border line

            // Left-aligned instructions text
            var textGo = new GameObject("Instructions", typeof(RectTransform)).GetComponent<RectTransform>();
            textGo.SetParent(panelGo, false);
            textGo.anchorMin = new Vector2(0f, 0.5f);
            textGo.anchorMax = new Vector2(0f, 0.5f);
            textGo.pivot = new Vector2(0f, 0.5f);
            textGo.anchoredPosition = new Vector2(40f, 0f);
            textGo.sizeDelta = new Vector2(400, 60);
            var txt = textGo.gameObject.AddComponent<TextMeshProUGUI>();
            txt.font = GetRobustFont(); txt.fontSize = 18; txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Left;
            txt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            txt.text = "HUD CUSTOMIZER\n<size=12><color=#cccccc>Drag any action button to place.</color></size>";
            txt.richText = true;

            // Preset Selection Buttons centered in a row
            CreateSettingsActionButton(panelGo, "DEFAULT PRESET", new Vector2(-160, 0), new Vector2(140, 40),
                () => ApplyPreset("DEFAULT"), new Color(0.95f, 0.8f, 0.2f, 0.15f));

            CreateSettingsActionButton(panelGo, "COMPACT PRESET", new Vector2(0, 0), new Vector2(140, 40),
                () => ApplyPreset("COMPACT"), new Color(0.95f, 0.8f, 0.2f, 0.15f));

            CreateSettingsActionButton(panelGo, "LEFTY PRESET", new Vector2(160, 0), new Vector2(140, 40),
                () => ApplyPreset("LEFTY"), new Color(0.95f, 0.8f, 0.2f, 0.15f));

            // Reset and Save Buttons anchored to the right side
            var resetBtn = CreateSettingsActionButton(panelGo, "RESET", new Vector2(-220, 0), new Vector2(130, 40),
                () => ResetToFactoryDefaults(), new Color(0.9f, 0.2f, 0.2f, 0.15f));
            var resetRect = resetBtn.GetComponent<RectTransform>();
            resetRect.anchorMin = resetRect.anchorMax = new Vector2(1f, 0.5f);
            resetRect.pivot = new Vector2(1f, 0.5f);

            var saveBtn = CreateSettingsActionButton(panelGo, "SAVE & EXIT", new Vector2(-40, 0), new Vector2(150, 40),
                () => {
                    IsCustomizingHUD = false;
                    Destroy(customRoot.gameObject);
                    BuildHUD();
                    
                    if (!HasStartedGame)
                    {
                        // Return to settings modal on Start Screen
                        if (startScreenCanvasInstance != null)
                        {
                            startScreenCanvasInstance.SetActive(true);
                            if (startScreenBgGo != null) startScreenBgGo.SetActive(true);
                            if (startScreenBottomPanelGo != null) startScreenBottomPanelGo.SetActive(true);
                            OpenSettingsModal(startScreenCanvasInstance.GetComponent<RectTransform>());
                        }
                    }
                    else
                    {
                        if (settingsModalInstance != null)
                        {
                            Destroy(settingsModalInstance);
                            settingsModalInstance = null;
                        }
                        Time.timeScale = 1f; // RESUME THE GAME!
                        if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }, new Color(0.1f, 0.9f, 0.3f, 0.2f));
            var saveRect = saveBtn.GetComponent<RectTransform>();
            saveRect.anchorMin = saveRect.anchorMax = new Vector2(1f, 0.5f);
            saveRect.pivot = new Vector2(1f, 0.5f);
        }

        private void UpdateHUDButtonPositionsOnScreen()
        {
            if (hudRootGo == null) return;
            string currentPreset = PlayerPrefs.GetString("HUD_Preset", "DEFAULT");
            bool isLefty = (currentPreset == "LEFTY");
            
            var btnContainer = hudRootGo.transform.Find("ButtonContainer") as RectTransform;
            if (btnContainer != null)
            {
                if (isLefty)
                {
                    btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(0, 0);
                    btnContainer.anchoredPosition = new Vector2(50, 50);
                }
                else
                {
                    btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0);
                    btnContainer.anchoredPosition = new Vector2(-50, 50);
                }

                foreach (Transform btn in btnContainer)
                {
                    Vector2 defaultPos = Vector2.zero;
                    // Positions scaled 0.75x from original
                    if (btn.name == "FIRE") defaultPos = isLefty ? new Vector2(165, 165) : new Vector2(-165, 165);
                    else if (btn.name == "RELOAD") defaultPos = isLefty ? new Vector2(390, 112) : new Vector2(-390, 112);
                    else if (btn.name == "SWAP") defaultPos = isLefty ? new Vector2(270, 465) : new Vector2(-270, 465);
                    else if (btn.name == "SPRINT") defaultPos = isLefty ? new Vector2(487, 225) : new Vector2(-487, 225);
                    else if (btn.name == "FOCUS") defaultPos = isLefty ? new Vector2(337, 315) : new Vector2(-337, 315);
                    else if (btn.name == "JUMP") defaultPos = isLefty ? new Vector2(112, 390) : new Vector2(-112, 390);

                    Vector2 savedPos = GetButtonPosition(btn.name, defaultPos);
                    var rect = btn.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = savedPos;
                    }
                }
            }

            var joystickBg = hudRootGo.transform.Find("NativeJoystick_Bg") as RectTransform;
            if (joystickBg != null)
            {
                Vector2 defaultPos = isLefty ? new Vector2(1620, 300) : new Vector2(300, 300);
                Vector2 savedPos = GetButtonPosition("NativeJoystick_Bg", defaultPos);
                joystickBg.anchoredPosition = savedPos;
            }
        }

        private GameObject CreateSettingsActionButton(RectTransform parent, string labelText, Vector2 pos, Vector2 size, System.Action onClick, Color highlightColor)
        {
            var btnGo = new GameObject(labelText, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            btnGo.SetParent(parent, false); btnGo.anchoredPosition = pos; btnGo.sizeDelta = size;
            
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.sprite = goldTrimmedButtonSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.95f, 0.8f, 0.2f, 1.0f);
            
            var highlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            highlight.SetParent(btnGo, false); highlight.anchorMin = Vector2.zero; highlight.anchorMax = Vector2.one;
            highlight.offsetMin = highlight.offsetMax = Vector2.zero;
            
            var hlImg = highlight.GetComponent<Image>();
            bool isOrange = highlightColor.r > highlightColor.b;
            hlImg.sprite = isOrange ? orangeGlowSprite : cyanGlowSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = Color.white;
            highlight.gameObject.SetActive(false);
            
            var txtGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            txtGo.SetParent(btnGo, false);
            txtGo.anchorMin = Vector2.zero; txtGo.anchorMax = Vector2.one;
            txtGo.offsetMin = txtGo.offsetMax = Vector2.zero;
            var txt = txtGo.gameObject.AddComponent<TextMeshProUGUI>();
            txt.font = GetRobustFont(); txt.fontSize = 15; txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center; txt.color = Color.black;
            txt.text = labelText;
            txt.raycastTarget = false;

            var helper = btnGo.gameObject.AddComponent<ButtonInputHelper>();
            helper.onClick = onClick;
            helper.glowObject = highlight.gameObject;
            helper.allowClicksDuringCustomization = true;
            helper.isDraggable = false;
            return btnGo.gameObject;
        }

        private void OpenSettingsModal(RectTransform parentCanvas)
        {
            if (settingsModalInstance != null) return;
            Time.timeScale = 0f; // PAUSE THE GAME!
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            
            // Hide Start Screen background and bottom panel when settings modal is open
            if (!HasStartedGame)
            {
                if (startScreenBgGo != null) startScreenBgGo.SetActive(false);
                if (startScreenBottomPanelGo != null) startScreenBottomPanelGo.SetActive(false);
            }
            
            // Background blur overlay
            var modalBg = new GameObject("SettingsModal", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            modalBg.SetParent(parentCanvas, false); modalBg.SetAsLastSibling(); modalBg.anchorMin = Vector2.zero; modalBg.anchorMax = Vector2.one; modalBg.offsetMin = modalBg.offsetMax = Vector2.zero;
            modalBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f); settingsModalInstance = modalBg.gameObject;
            
            // Dialog box
            var dialog = new GameObject("Dialog", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            dialog.SetParent(modalBg, false); dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f); dialog.sizeDelta = new Vector2(850, 640);
            var dialogImg = dialog.GetComponent<Image>();
            dialogImg.sprite = null;
            dialogImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            
            // Title Text
            var titleGo = new GameObject("Title", typeof(RectTransform)).GetComponent<RectTransform>();
            titleGo.SetParent(dialog, false); titleGo.anchoredPosition = new Vector2(0, 260); titleGo.sizeDelta = new Vector2(700, 60);
            var titleTxt = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            titleTxt.font = GetRobustFont(); titleTxt.fontSize = 28; titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center; titleTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            titleTxt.text = "THE PHARAOH'S VAULT - SETTINGS";

            // Row 1: Swipe Sensitivity (SLIDER)
            float currentSens = PlayerPrefs.GetFloat("MobileSensitivity", 0.08f);
            var sensRow = CreateSettingsSliderRow(dialog, "TOUCH SENSITIVITY", new Vector2(0, 170), 0.02f, 0.30f, currentSens,
                (val) => {
                    PlayerPrefs.SetFloat("MobileSensitivity", val); PlayerPrefs.Save();
                    var sz = GameObject.FindAnyObjectByType<LookSwipeZone>();
                    if (sz != null) sz.sensitivity = val;
                },
                (val) => val.ToString("F2")
            );

            // Row 2: Master Volume (SLIDER)
            float currentVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            AudioListener.volume = currentVol;
            var volRow = CreateSettingsSliderRow(dialog, "MASTER VOLUME", new Vector2(0, 100), 0f, 1f, currentVol,
                (val) => {
                    PlayerPrefs.SetFloat("MasterVolume", val); PlayerPrefs.Save();
                    AudioListener.volume = val;
                },
                (val) => Mathf.RoundToInt(val * 100f) + "%"
            );

            // Row 3: Hive Narration Toggle (CHECKBOX TOGGLE)
            int showNar = PlayerPrefs.GetInt("ShowNarration", 1);
            var narrationRow = CreateSettingsToggleRow(dialog, "HIVE NARRATION", new Vector2(0, 30), showNar == 1,
                (val) => {
                    int nextVal = val ? 1 : 0;
                    PlayerPrefs.SetInt("ShowNarration", nextVal); PlayerPrefs.Save();
                    if (nextVal == 0 && narrationPanel != null) narrationPanel.SetActive(false);
                }
            );

            // Row 4: Visual Fidelity (SELECTOR)
            int currentQualityIdx = PlayerPrefs.GetInt("VisualQualityIdx", 1); // 0: LOW, 1: MEDIUM, 2: HIGH, 3: ULTRA (default 1)
            string[] qualityNames = { "LOW", "MEDIUM", "HIGH", "ULTRA" };
            int[] unityQualityLevels = { 1, 2, 3, 5 }; // Map user options to Low, Medium, High, Ultra in Unity Settings
            var qualRow = CreateSettingsRow(dialog, "VISUAL QUALITY", new Vector2(0, -40), qualityNames[Mathf.Clamp(currentQualityIdx, 0, 3)],
                () => {
                    currentQualityIdx = Mathf.Clamp(currentQualityIdx - 1, 0, 3);
                    PlayerPrefs.SetInt("VisualQualityIdx", currentQualityIdx);
                    PlayerPrefs.Save();
                    QualitySettings.SetQualityLevel(unityQualityLevels[currentQualityIdx], true);
                    return qualityNames[currentQualityIdx];
                },
                () => {
                    currentQualityIdx = Mathf.Clamp(currentQualityIdx + 1, 0, 3);
                    PlayerPrefs.SetInt("VisualQualityIdx", currentQualityIdx);
                    PlayerPrefs.Save();
                    QualitySettings.SetQualityLevel(unityQualityLevels[currentQualityIdx], true);
                    return qualityNames[currentQualityIdx];
                }
            );

            // Row 5: HUD Layout Preset (SELECTOR)
            string currentPreset = PlayerPrefs.GetString("HUD_Preset", "DEFAULT");
            var presetRow = CreateSettingsRow(dialog, "HUD LAYOUT PRESET", new Vector2(0, -110), currentPreset,
                () => {
                    string next = "DEFAULT";
                    if (currentPreset == "DEFAULT") next = "LEFTY";
                    else if (currentPreset == "LEFTY") next = "COMPACT";
                    currentPreset = next;
                    ApplyPreset(currentPreset);
                    return currentPreset;
                },
                () => {
                    string next = "DEFAULT";
                    if (currentPreset == "DEFAULT") next = "COMPACT";
                    else if (currentPreset == "COMPACT") next = "LEFTY";
                    currentPreset = next;
                    ApplyPreset(currentPreset);
                    return currentPreset;
                }
            );

            // Row 6: Custom Layout Action Buttons (CUSTOMIZE / RESET)
            CreateSettingsActionButton(dialog, "CUSTOMIZE HUD LAYOUT", new Vector2(-160, -180), new Vector2(300, 50),
                () => {
                    StartHUDCustomization();
                },
                new Color(0.95f, 0.8f, 0.2f, 0.15f)
            );

            CreateSettingsActionButton(dialog, "RESET TO DEFAULT", new Vector2(160, -180), new Vector2(300, 50),
                () => {
                    ResetToFactoryDefaults();
                    if (settingsModalInstance != null) {
                        Destroy(modalBg.gameObject);
                        settingsModalInstance = null;
                        OpenSettingsModal(parentCanvas);
                    }
                },
                new Color(0.9f, 0.2f, 0.2f, 0.15f)
            );

            if (HasStartedGame)
            {
                CreateSettingsActionButton(dialog, "MAIN MENU / HOME", new Vector2(-160, -250), new Vector2(300, 50),
                    () => {
                        Time.timeScale = 1f;
                        HasStartedGame = false;
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    },
                    new Color(0.2f, 0.5f, 0.8f, 0.15f)
                );

                CreateSettingsActionButton(dialog, "RETURN TO GAME", new Vector2(160, -250), new Vector2(300, 50),
                    () => {
                        Destroy(modalBg.gameObject);
                        settingsModalInstance = null;
                        Time.timeScale = 1f; // RESUME THE GAME!
                        if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    },
                    new Color(0.95f, 0.8f, 0.2f, 0.15f)
                );
            }
            else
            {
                CreateSettingsActionButton(dialog, "BACK TO MENU", new Vector2(0, -250), new Vector2(300, 50),
                    () => {
                        Destroy(modalBg.gameObject);
                        settingsModalInstance = null;
                        if (!HasStartedGame)
                        {
                            if (startScreenBgGo != null) startScreenBgGo.SetActive(true);
                            if (startScreenBottomPanelGo != null) startScreenBottomPanelGo.SetActive(true);
                        }
                    },
                    new Color(0.95f, 0.8f, 0.2f, 0.15f)
                );
            }
        }

        private GameObject CreateSettingsSliderRow(RectTransform parent, string labelText, Vector2 pos, float minVal, float maxVal, float initialVal, System.Action<float> onValueChange, System.Func<float, string> formatFunc)
        {
            var row = new GameObject("Row_" + labelText.Replace(" ", ""), typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false); row.anchoredPosition = pos; row.sizeDelta = new Vector2(700, 70);

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            lblGo.SetParent(row, false); lblGo.anchorMin = new Vector2(0, 0.5f); lblGo.anchorMax = new Vector2(0.4f, 0.5f);
            lblGo.pivot = new Vector2(0, 0.5f); lblGo.anchoredPosition = new Vector2(20, 0); lblGo.sizeDelta = new Vector2(250, 50);
            var lblTxt = lblGo.gameObject.AddComponent<TextMeshProUGUI>();
            lblTxt.font = GetRobustFont(); lblTxt.fontSize = 20; lblTxt.fontStyle = FontStyles.Bold;
            lblTxt.alignment = TextAlignmentOptions.Left; lblTxt.color = new Color(0.95f, 0.85f, 0.6f, 0.95f);
            lblTxt.text = labelText;

            // Slider Background track
            var sliderBgGo = new GameObject("SliderBg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sliderBgGo.SetParent(row, false); sliderBgGo.anchoredPosition = new Vector2(210, 0); sliderBgGo.sizeDelta = new Vector2(270, 14);
            sliderBgGo.GetComponent<Image>().sprite = charcoalSprite;
            sliderBgGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Slider Fill track (golden/crimson)
            var sliderFillGo = new GameObject("SliderFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sliderFillGo.SetParent(sliderBgGo, false);
            sliderFillGo.anchorMin = new Vector2(0, 0);
            sliderFillGo.anchorMax = new Vector2((initialVal - minVal) / (maxVal - minVal), 1);
            sliderFillGo.offsetMin = sliderFillGo.offsetMax = Vector2.zero;
            sliderFillGo.GetComponent<Image>().sprite = charcoalSprite;
            sliderFillGo.GetComponent<Image>().color = new Color(0.95f, 0.8f, 0.2f, 0.9f);

            // Handle knob
            var knobGo = new GameObject("SliderHandle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            knobGo.SetParent(sliderBgGo, false);
            knobGo.anchorMin = knobGo.anchorMax = new Vector2((initialVal - minVal) / (maxVal - minVal), 0.5f);
            knobGo.anchoredPosition = Vector2.zero;
            knobGo.sizeDelta = new Vector2(26, 26);
            knobGo.GetComponent<Image>().sprite = CreateSettingsMedallionSprite(32, 32);

            // Value text label at the right
            var valGo = new GameObject("SliderValueText", typeof(RectTransform)).GetComponent<RectTransform>();
            valGo.SetParent(row, false); valGo.anchoredPosition = new Vector2(400, 0); valGo.sizeDelta = new Vector2(100, 50);
            var valTxt = valGo.gameObject.AddComponent<TextMeshProUGUI>();
            valTxt.font = GetRobustFont(); valTxt.fontSize = 20; valTxt.fontStyle = FontStyles.Bold;
            valTxt.alignment = TextAlignmentOptions.Left; valTxt.color = new Color(1f, 0.95f, 0.8f, 0.95f);
            valTxt.text = formatFunc != null ? formatFunc(initialVal) : initialVal.ToString("F2");

            // Direct interactive drag listener!
            var sliderHelper = sliderBgGo.gameObject.AddComponent<ButtonInputHelper>();
            System.Action<Vector2> updateSliderVal = (screenPos) => {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderBgGo, screenPos, null, out Vector2 localPoint);
                float width = sliderBgGo.rect.width;
                float pct = Mathf.Clamp01((localPoint.x + width * 0.5f) / width);
                float val = Mathf.Lerp(minVal, maxVal, pct);
                sliderFillGo.anchorMax = new Vector2(pct, 1);
                knobGo.anchorMin = knobGo.anchorMax = new Vector2(pct, 0.5f);
                valTxt.text = formatFunc != null ? formatFunc(val) : val.ToString("F2");
                onValueChange?.Invoke(val);
            };

            sliderHelper.onDown = () => {
                Vector2 mousePos = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : 
                                   (UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero);
                updateSliderVal(mousePos);
            };

            var dragHelper = sliderBgGo.gameObject.AddComponent<SliderDragHelper>();
            dragHelper.onDrag = (screenPos) => {
                updateSliderVal(screenPos);
            };

            return row.gameObject;
        }

        private GameObject CreateSettingsRow(RectTransform parent, string labelText, Vector2 pos, string initialVal, System.Func<string> onDec, System.Func<string> onInc)
        {
            var row = new GameObject("Row_" + labelText.Replace(" ", ""), typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false); row.anchoredPosition = pos; row.sizeDelta = new Vector2(700, 70);

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            lblGo.SetParent(row, false); lblGo.anchorMin = new Vector2(0, 0.5f); lblGo.anchorMax = new Vector2(0.4f, 0.5f);
            lblGo.pivot = new Vector2(0, 0.5f); lblGo.anchoredPosition = new Vector2(20, 0); lblGo.sizeDelta = new Vector2(250, 50);
            var lblTxt = lblGo.gameObject.AddComponent<TextMeshProUGUI>();
            lblTxt.font = GetRobustFont(); lblTxt.fontSize = 20; lblTxt.fontStyle = FontStyles.Bold;
            lblTxt.alignment = TextAlignmentOptions.Left; lblTxt.color = new Color(0.95f, 0.85f, 0.6f, 0.95f);
            lblTxt.text = labelText;

            // Dec Button [-]
            var decGo = new GameObject("DecBtn", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            decGo.SetParent(row, false); decGo.anchoredPosition = new Vector2(100, 0); decGo.sizeDelta = new Vector2(50, 50);
            decGo.GetComponent<Image>().sprite = charcoalSprite;
            var decHighlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            decHighlight.SetParent(decGo, false); decHighlight.anchorMin = Vector2.zero; decHighlight.anchorMax = Vector2.one; decHighlight.offsetMin = decHighlight.offsetMax = Vector2.zero;
            decHighlight.GetComponent<Image>().color = new Color(0.95f, 0.8f, 0.2f, 0.15f);
            var decTxtGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            decTxtGo.SetParent(decGo, false); decTxtGo.anchorMin = Vector2.zero; decTxtGo.anchorMax = Vector2.one; decTxtGo.offsetMin = decTxtGo.offsetMax = Vector2.zero;
            var decTxt = decTxtGo.gameObject.AddComponent<TextMeshProUGUI>();
            decTxt.font = GetRobustFont(); decTxt.fontSize = 24; decTxt.fontStyle = FontStyles.Bold;
            decTxt.alignment = TextAlignmentOptions.Center; decTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); decTxt.text = "-";

            // Value text box
            var valGo = new GameObject("ValBtn", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            valGo.SetParent(row, false); valGo.anchoredPosition = new Vector2(210, 0); valGo.sizeDelta = new Vector2(150, 50);
            valGo.GetComponent<Image>().sprite = charcoalSprite;
            var valTxtGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            valTxtGo.SetParent(valGo, false); valTxtGo.anchorMin = Vector2.zero; valTxtGo.anchorMax = Vector2.one; valTxtGo.offsetMin = valTxtGo.offsetMax = Vector2.zero;
            var valTxt = valTxtGo.gameObject.AddComponent<TextMeshProUGUI>();
            valTxt.font = GetRobustFont(); valTxt.fontSize = 20; valTxt.fontStyle = FontStyles.Bold;
            valTxt.alignment = TextAlignmentOptions.Center; valTxt.color = new Color(1f, 0.95f, 0.8f, 0.95f); valTxt.text = initialVal;

            // Inc Button [+]
            var incGo = new GameObject("IncBtn", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            incGo.SetParent(row, false); incGo.anchoredPosition = new Vector2(320, 0); incGo.sizeDelta = new Vector2(50, 50);
            incGo.GetComponent<Image>().sprite = charcoalSprite;
            var incHighlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            incHighlight.SetParent(incGo, false); incHighlight.anchorMin = Vector2.zero; incHighlight.anchorMax = Vector2.one; incHighlight.offsetMin = incHighlight.offsetMax = Vector2.zero;
            incHighlight.GetComponent<Image>().color = new Color(0.95f, 0.8f, 0.2f, 0.15f);
            var incTxtGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            incTxtGo.SetParent(incGo, false); incTxtGo.anchorMin = Vector2.zero; incTxtGo.anchorMax = Vector2.one; incTxtGo.offsetMin = incTxtGo.offsetMax = Vector2.zero;
            var incTxt = incTxtGo.gameObject.AddComponent<TextMeshProUGUI>();
            incTxt.font = GetRobustFont(); incTxt.fontSize = 24; incTxt.fontStyle = FontStyles.Bold;
            incTxt.alignment = TextAlignmentOptions.Center; incTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f); incTxt.text = "+";

            // Position controls to the right
            decGo.anchorMin = decGo.anchorMax = new Vector2(1, 0.5f); decGo.anchoredPosition = new Vector2(-270, 0);
            valGo.anchorMin = valGo.anchorMax = new Vector2(1, 0.5f); valGo.anchoredPosition = new Vector2(-160, 0);
            incGo.anchorMin = incGo.anchorMax = new Vector2(1, 0.5f); incGo.anchoredPosition = new Vector2(-50, 0);

            decGo.gameObject.AddComponent<ButtonInputHelper>().onUp = () => { valTxt.text = onDec(); };
            incGo.gameObject.AddComponent<ButtonInputHelper>().onUp = () => { valTxt.text = onInc(); };

            return row.gameObject;
        }

        private GameObject narrationPanel = null;
        private TextMeshProUGUI narrationText = null;
        private Coroutine narrationFadeRoutine = null;
        private GameObject orbTooltipPanel = null;
        private TextMeshProUGUI orbTooltipText = null;
        private Coroutine orbTooltipFadeRoutine = null;

        public void ShowNarration(string message)
        {
            if (PlayerPrefs.GetInt("ShowNarration", 1) == 0)
            {
                if (narrationPanel != null) narrationPanel.SetActive(false);
                return;
            }

            if (narrationPanel == null)
            {
                var canvas = GetComponent<Canvas>();
                if (canvas == null) return;
                var root = canvas.GetComponent<RectTransform>();

                // Sleek, completely transparent container for subtitles
                var panelGo = new GameObject("NarrationPanel", typeof(RectTransform)).GetComponent<RectTransform>();
                panelGo.SetParent(root, false);
                panelGo.anchorMin = panelGo.anchorMax = new Vector2(0.5f, 0f);
                panelGo.pivot = new Vector2(0.5f, 0f);
                panelGo.anchoredPosition = new Vector2(0, 100); // Moved lower to prevent button conflict
                panelGo.sizeDelta = new Vector2(900, 75);
                narrationPanel = panelGo.gameObject;

                // Text (No golden border, clean modern look, pure white and MedievalSharp)
                var txtGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
                txtGo.SetParent(panelGo, false);
                txtGo.anchorMin = Vector2.zero; txtGo.anchorMax = Vector2.one;
                txtGo.offsetMin = new Vector2(30, 5); txtGo.offsetMax = new Vector2(-30, -5);
                narrationText = txtGo.gameObject.AddComponent<TextMeshProUGUI>();
                narrationText.font = GetTitleFont();
                narrationText.fontSize = 21;
                narrationText.fontStyle = FontStyles.Normal;
                narrationText.textWrappingMode = TextWrappingModes.Normal;
                narrationText.overflowMode = TextOverflowModes.Truncate;
            }

            narrationPanel.SetActive(true);
            narrationText.text = message;

            // Subtitle Z-index hierarchy management: Keep Narration below settings modal, but above buttons
            narrationPanel.transform.SetAsLastSibling();
            if (settingsModalInstance != null)
            {
                settingsModalInstance.transform.SetAsLastSibling();
            }

            if (narrationFadeRoutine != null) StopCoroutine(narrationFadeRoutine);
            narrationFadeRoutine = StartCoroutine(NarrationFadeOutSequence());
        }

        private IEnumerator NarrationFadeOutSequence()
        {
            float duration = 6f;
            float elapsed = 0f;
            var cg = narrationPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = narrationPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            float fadeTime = 1.5f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }

            narrationPanel.SetActive(false);
        }

        private IEnumerator OrbTooltipFadeOutSequence()
        {
            float duration = 3.5f;
            float elapsed = 0f;
            var cg = orbTooltipPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = orbTooltipPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            float fadeTime = 0.8f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }

            orbTooltipPanel.SetActive(false);
        }

        public static bool HasStartedGame = false;

        private IEnumerator Start()
        {
            // 1. Defer heavy procedural generation to prevent startup freeze
            LoadSprites();
            yield return null;
            GenerateProceduralSprites();
            yield return null;
            SetupCanvas();
            yield return null;
            BuildHUD();
            yield return null;

            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 5f;
            }

            // Force immersive full screen mode (hides navigation and status bars)
            Screen.fullScreen = true;

            // Apply volume on boot
            float currentVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            AudioListener.volume = currentVol;

            // Apply visual quality setting on boot
            int currentQualityIdxOnBoot = PlayerPrefs.GetInt("VisualQualityIdx", 1); // default to MEDIUM
            int[] unityQualityLevelsOnBoot = { 1, 2, 3, 5 };
            QualitySettings.SetQualityLevel(unityQualityLevelsOnBoot[Mathf.Clamp(currentQualityIdxOnBoot, 0, 3)], true);

            // Limit framerate to 30 FPS for battery and performance optimization on mobile devices
            Application.targetFrameRate = 30;

            if (!HasStartedGame)
            {
                CreateStartScreen();
            }
            yield return null;
        }

        private void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void CreateStartScreen()
        {
            if (bootFader != null)
            {
                Destroy(bootFader);
                bootFader = null;
            }

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
            {
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            }

            var startCanvasGo = new GameObject("StartScreenOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            startScreenCanvasInstance = startCanvasGo;
            var canvas = startCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = startCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1.0f;

            var bgGo = new GameObject("StartBackground", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            startScreenBgGo = bgGo.gameObject;
            bgGo.SetParent(startCanvasGo.transform, false);
            bgGo.anchorMin = Vector2.zero; bgGo.anchorMax = Vector2.one;
            bgGo.offsetMin = bgGo.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            var bgSprite = Resources.Load<Sprite>("egyptian_items/GameStartImage");
            if (bgSprite != null) bgImg.sprite = bgSprite;
            else bgImg.sprite = CreateProceduralGradientSprite(1920, 1080, new Color(0.08f, 0.04f, 0f, 1f), new Color(0.02f, 0.01f, 0f, 1f));
            bgImg.color = Color.white;

            // --- HACKATHON: Mystic Dust Particles ---
            // Positioned above the background image
            StartCoroutine(MysticDustRoutine(startCanvasGo.transform));

            // Lightning overlay
            var lightningGo = new GameObject("LightningOverlay", typeof(RectTransform), typeof(Image));
            lightningGo.transform.SetParent(startCanvasGo.transform, false);
            var lRect = lightningGo.GetComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero; lRect.anchorMax = Vector2.one;
            lRect.offsetMin = lRect.offsetMax = Vector2.zero;
            var lImg = lightningGo.GetComponent<Image>();
            lImg.color = new Color(1f, 1f, 1f, 0f);
            lImg.raycastTarget = false;

            StartCoroutine(LightningFlashesRoutine(lImg));

            var bottomActionGo = new GameObject("BottomActionPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            startScreenBottomPanelGo = bottomActionGo.gameObject;
            bottomActionGo.SetParent(startCanvasGo.transform, false);
            bottomActionGo.anchorMin = bottomActionGo.anchorMax = new Vector2(0.5f, 0f);
            bottomActionGo.pivot = new Vector2(0.5f, 0f);
            bottomActionGo.anchoredPosition = new Vector2(0, 100);
            bottomActionGo.sizeDelta = new Vector2(1000, 240); // slightly taller bottom action panel for 2 rows of 80px buttons
            bottomActionGo.GetComponent<Image>().color = Color.clear;

            var startBtnGo = new GameObject("StartButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            startBtnGo.SetParent(bottomActionGo, false);
            startBtnGo.anchorMin = startBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            startBtnGo.anchoredPosition = new Vector2(-200, 50);
            startBtnGo.sizeDelta = new Vector2(380, 80);
            var startBtnImg = startBtnGo.GetComponent<Image>();
            startBtnImg.color = new Color(0.95f, 0.8f, 0.2f, 1f);

            var startBtnTextGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            startBtnTextGo.SetParent(startBtnGo, false);
            startBtnTextGo.anchorMin = Vector2.zero; startBtnTextGo.anchorMax = Vector2.one;
            var startBtnTxt = startBtnTextGo.gameObject.AddComponent<TextMeshProUGUI>();
            startBtnTxt.font = GetTitleFont();
            startBtnTxt.fontSize = 24;
            startBtnTxt.fontStyle = FontStyles.Bold;
            startBtnTxt.alignment = TextAlignmentOptions.Center;
            startBtnTxt.color = Color.black;
            startBtnTxt.text = "START VOYAGE";

            var startHelper = startBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            startHelper.onClick = () =>
            {
                HasStartedGame = true;
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                
                if (activeDifficultyDropdown != null)
                {
                    Destroy(activeDifficultyDropdown);
                    activeDifficultyDropdown = null;
                }
                
                Destroy(startCanvasGo);
                startScreenCanvasInstance = null;
                startScreenBgGo = null;
                startScreenBottomPanelGo = null;
                DisableCompetingCanvases();
            };

            var quitBtnGo = new GameObject("QuitButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            quitBtnGo.SetParent(bottomActionGo, false);
            quitBtnGo.anchorMin = quitBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            quitBtnGo.anchoredPosition = new Vector2(200, 50);
            quitBtnGo.sizeDelta = new Vector2(380, 80);
            var quitBtnImg = quitBtnGo.GetComponent<Image>();
            quitBtnImg.color = new Color(0.95f, 0.8f, 0.2f, 1f);

            var quitBtnTextGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            quitBtnTextGo.SetParent(quitBtnGo, false);
            quitBtnTextGo.anchorMin = Vector2.zero; quitBtnTextGo.anchorMax = Vector2.one;
            var quitBtnTxt = quitBtnTextGo.gameObject.AddComponent<TextMeshProUGUI>();
            quitBtnTxt.font = GetTitleFont();
            quitBtnTxt.fontSize = 24;
            quitBtnTxt.fontStyle = FontStyles.Bold;
            quitBtnTxt.alignment = TextAlignmentOptions.Center;
            quitBtnTxt.color = Color.black;
            quitBtnTxt.text = "ABANDON SHIP";

            var quitHelper = quitBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            quitHelper.onClick = () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            };

            // --- DIFFICULTY BUTTON ---
            var diffBtnGo = new GameObject("DifficultyButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            diffBtnGo.SetParent(bottomActionGo, false);
            diffBtnGo.anchorMin = diffBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            diffBtnGo.anchoredPosition = new Vector2(-200, -50);
            diffBtnGo.sizeDelta = new Vector2(380, 80);
            var diffBtnImg = diffBtnGo.GetComponent<Image>();
            diffBtnImg.color = new Color(0.95f, 0.8f, 0.2f, 1f);

            var diffBtnTextGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            diffBtnTextGo.SetParent(diffBtnGo, false);
            diffBtnTextGo.anchorMin = Vector2.zero; diffBtnTextGo.anchorMax = Vector2.one;
            var diffBtnTxt = diffBtnTextGo.gameObject.AddComponent<TextMeshProUGUI>();
            diffBtnTxt.font = GetTitleFont();
            diffBtnTxt.fontSize = 18; // Smaller font size to prevent clipping
            diffBtnTxt.enableAutoSizing = true;
            diffBtnTxt.fontSizeMin = 12;
            diffBtnTxt.fontSizeMax = 20;
            diffBtnTxt.fontStyle = FontStyles.Bold;
            diffBtnTxt.alignment = TextAlignmentOptions.Center;
            diffBtnTxt.color = Color.black;

            int currentDiff = PlayerPrefs.GetInt("DifficultyLevel", 1); // default NORMAL
            string[] diffNames = { "EASY", "NORMAL", "HARD", "NIGHTMARE" };
            int[] diffKills = { 5, 10, 20, 35 };
            diffBtnTxt.text = $"DIFFICULTY: {diffNames[currentDiff]} ({diffKills[currentDiff]} KILLS)";

            var diffHelper = diffBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            diffHelper.onClick = () =>
            {
                ToggleDifficultyDropdown(bottomActionGo, diffBtnTxt);
            };

            // --- SETTINGS BUTTON ---
            var settingsBtnGo = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            settingsBtnGo.SetParent(bottomActionGo, false);
            settingsBtnGo.anchorMin = settingsBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            settingsBtnGo.anchoredPosition = new Vector2(200, -50);
            settingsBtnGo.sizeDelta = new Vector2(380, 80);
            var settingsBtnImg = settingsBtnGo.GetComponent<Image>();
            settingsBtnImg.color = new Color(0.95f, 0.8f, 0.2f, 1f);

            var settingsBtnTextGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            settingsBtnTextGo.SetParent(settingsBtnGo, false);
            settingsBtnTextGo.anchorMin = Vector2.zero; settingsBtnTextGo.anchorMax = Vector2.one;
            var settingsBtnTxt = settingsBtnTextGo.gameObject.AddComponent<TextMeshProUGUI>();
            settingsBtnTxt.font = GetTitleFont();
            settingsBtnTxt.fontSize = 24;
            settingsBtnTxt.fontStyle = FontStyles.Bold;
            settingsBtnTxt.alignment = TextAlignmentOptions.Center;
            settingsBtnTxt.color = Color.black;
            settingsBtnTxt.text = "SETTINGS";

            var settingsHelper = settingsBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            settingsHelper.onClick = () =>
            {
                if (activeDifficultyDropdown != null)
                {
                    Destroy(activeDifficultyDropdown);
                    activeDifficultyDropdown = null;
                }
                OpenSettingsModal(startCanvasGo.GetComponent<RectTransform>());
            };

            SetLayerRecursively(startCanvasGo, 5);
        }

        private void ToggleDifficultyDropdown(Transform parent, TextMeshProUGUI buttonText)
        {
            if (activeDifficultyDropdown != null)
            {
                Destroy(activeDifficultyDropdown);
                activeDifficultyDropdown = null;
                return;
            }

            // Create dropdown container (using outline-offset pattern)
            var outlineGo = new GameObject("DifficultyDropdownOutline", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            outlineGo.SetParent(parent, false);
            outlineGo.anchorMin = outlineGo.anchorMax = new Vector2(0.5f, 0.5f);
            outlineGo.pivot = new Vector2(0.5f, 0f);
            outlineGo.anchoredPosition = new Vector2(-200, -10); // Grow upwards starting right above the button
            outlineGo.sizeDelta = new Vector2(384, 244);
            var outlineImg = outlineGo.GetComponent<Image>();
            outlineImg.color = new Color(0.95f, 0.8f, 0.2f, 1f); // Egyptian gold border line
            
            var dropdownGo = new GameObject("DifficultyDropdown", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            dropdownGo.SetParent(outlineGo, false);
            dropdownGo.anchorMin = Vector2.zero; dropdownGo.anchorMax = Vector2.one;
            dropdownGo.offsetMin = new Vector2(2, 2); dropdownGo.offsetMax = new Vector2(-2, -2);
            var img = dropdownGo.GetComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.08f, 0.98f);
            
            outlineGo.SetAsLastSibling();
            activeDifficultyDropdown = outlineGo.gameObject;

            string[] diffNames = { "EASY", "NORMAL", "HARD", "NIGHTMARE" };
            int[] diffKills = { 5, 10, 20, 35 };

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var optionGo = new GameObject("Option_" + diffNames[i], typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                optionGo.SetParent(dropdownGo, false);
                optionGo.anchorMin = optionGo.anchorMax = new Vector2(0.5f, 0f);
                optionGo.pivot = new Vector2(0.5f, 0f);
                optionGo.anchoredPosition = new Vector2(0, index * 60);
                optionGo.sizeDelta = new Vector2(376, 58);
                
                var optImg = optionGo.GetComponent<Image>();
                optImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                
                // Option Text
                var optTextGo = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
                optTextGo.SetParent(optionGo, false);
                optTextGo.anchorMin = Vector2.zero; optTextGo.anchorMax = Vector2.one;
                var optTxt = optTextGo.gameObject.AddComponent<TextMeshProUGUI>();
                optTxt.font = GetTitleFont();
                optTxt.fontSize = 18;
                optTxt.fontStyle = FontStyles.Bold;
                optTxt.alignment = TextAlignmentOptions.Center;
                optTxt.color = new Color(0.95f, 0.85f, 0.6f, 1f);
                optTxt.text = $"{diffNames[index]} ({diffKills[index]} KILLS)";
                
                // Highlight currently selected difficulty
                int currentSelected = PlayerPrefs.GetInt("DifficultyLevel", 1);
                if (index == currentSelected)
                {
                    optImg.color = new Color(0.95f, 0.8f, 0.2f, 0.2f);
                    optTxt.color = new Color(0.95f, 0.8f, 0.2f, 1f);
                }

                var helper = optionGo.gameObject.AddComponent<ButtonInputHelper>();
                helper.allowClicksDuringCustomization = true;
                helper.onClick = () =>
                {
                    PlayerPrefs.SetInt("DifficultyLevel", index);
                    PlayerPrefs.Save();
                    buttonText.text = $"DIFFICULTY: {diffNames[index]} ({diffKills[index]} KILLS)";
                    
                    if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null)
                    {
                        TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.SetDifficulty(index);
                    }
                    
                    Destroy(activeDifficultyDropdown);
                    activeDifficultyDropdown = null;
                };
            }
        }

        private TMP_FontAsset GetTitleFont()
        {
            TMP_FontAsset f = Resources.Load<TMP_FontAsset>("Fonts/MedievalSharp SDF");
            if (f == null) f = GetRobustFont();
            return f;
        }

        private GameObject CreateSettingsToggleRow(RectTransform parent, string labelText, Vector2 pos, bool initialVal, System.Action<bool> onToggle)
        {
            var row = new GameObject("Row_" + labelText.Replace(" ", ""), typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false); row.anchoredPosition = pos; row.sizeDelta = new Vector2(700, 70);

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            lblGo.SetParent(row, false); lblGo.anchorMin = new Vector2(0, 0.5f); lblGo.anchorMax = new Vector2(0.4f, 0.5f);
            lblGo.pivot = new Vector2(0, 0.5f); lblGo.anchoredPosition = new Vector2(20, 0); lblGo.sizeDelta = new Vector2(250, 50);
            var lblTxt = lblGo.gameObject.AddComponent<TextMeshProUGUI>();
            lblTxt.font = GetRobustFont(); lblTxt.fontSize = 20; lblTxt.fontStyle = FontStyles.Bold;
            lblTxt.alignment = TextAlignmentOptions.Left; lblTxt.color = new Color(0.95f, 0.85f, 0.6f, 0.95f);
            lblTxt.text = labelText;

            // Checkbox Outline (Outer Gold Frame)
            var outlineGo = new GameObject("Outline", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            outlineGo.SetParent(row, false);
            outlineGo.anchorMin = outlineGo.anchorMax = new Vector2(1f, 0.5f);
            outlineGo.anchoredPosition = new Vector2(-160, 0);
            outlineGo.sizeDelta = new Vector2(54, 54);
            var outImg = outlineGo.GetComponent<Image>();
            outImg.sprite = null;
            outImg.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);

            // Checkbox Backing (Inner Card)
            var boxGo = new GameObject("Checkbox", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            boxGo.SetParent(outlineGo, false);
            boxGo.anchorMin = Vector2.zero; boxGo.anchorMax = Vector2.one;
            boxGo.offsetMin = new Vector2(2, 2); boxGo.offsetMax = new Vector2(-2, -2);
            
            var boxImg = boxGo.GetComponent<Image>();
            boxImg.sprite = charcoalSprite;
            boxImg.color = Color.white;

            // Checkmark
            var markGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            markGo.SetParent(boxGo, false);
            markGo.anchorMin = markGo.anchorMax = new Vector2(0.5f, 0.5f);
            markGo.sizeDelta = new Vector2(30, 30);
            var markImg = markGo.GetComponent<Image>();
            markImg.sprite = null;
            markImg.color = new Color(1.0f, 0.78f, 0.0f, 0.95f);

            bool currentVal = initialVal;
            markGo.gameObject.SetActive(currentVal);

            var helper = boxGo.gameObject.AddComponent<ButtonInputHelper>();
            helper.onUp = () =>
            {
                currentVal = !currentVal;
                markGo.gameObject.SetActive(currentVal);
                onToggle(currentVal);
            };

            return row.gameObject;
        }

        private TMP_FontAsset GetRobustFont()
        {
            TMP_FontAsset f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (f == null) f = TMP_Settings.defaultFontAsset;
            return f;
        }

        private Sprite CreateTargetingReticleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            Color reticleColor = new Color(1.0f, 1.0f, 1.0f, 0.95f);
            Color glowColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Crosshair lines (outer)
                    bool isCross = (Mathf.Abs(dx) < 0.02f && Mathf.Abs(dy) > 0.4f && Mathf.Abs(dy) < 0.8f) ||
                                   (Mathf.Abs(dy) < 0.02f && Mathf.Abs(dx) > 0.4f && Mathf.Abs(dx) < 0.8f);

                    // Inner focus dots
                    bool isInner = (dist > 0.15f && dist < 0.22f) && 
                                   (Mathf.Abs(dx) < 0.04f || Mathf.Abs(dy) < 0.04f);

                    if (isCross)
                    {
                        tex.SetPixel(x, y, reticleColor);
                    }
                    else if (isInner)
                    {
                        tex.SetPixel(x, y, glowColor);
                    }
                    // Precise center dot with glow
                    else if (dist <= 0.06f)
                    {
                        float alpha = Mathf.Clamp01((1f - dist / 0.06f) * 2f);
                        tex.SetPixel(x, y, new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * alpha));
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

        private IEnumerator FadeInDeathScreen(Image bgImg, CanvasGroup cardGroup, Image vigImg)
        {
            float duration = 0.8f;
            float elapsed = 0f;
            bgImg.color = new Color(1f, 1f, 1f, 0f);
            vigImg.color = new Color(1f, 1f, 1f, 0f);
            cardGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); // Smooth ease-out curve

                bgImg.color = new Color(1f, 1f, 1f, smoothT);
                vigImg.color = new Color(1f, 1f, 1f, smoothT * 0.8f);
                cardGroup.alpha = smoothT;
                yield return null;
            }
            bgImg.color = Color.white;
            cardGroup.alpha = 1f;

            // Dangerous breathing/pulsing effect for the blood vignette
            float pulseTimer = 0f;
            while (vigImg != null)
            {
                pulseTimer += Time.unscaledDeltaTime;
                float pulse = 0.5f + Mathf.PingPong(pulseTimer * 1.5f, 0.45f); // oscillates between 0.5 and 0.95
                vigImg.color = new Color(1f, 1f, 1f, pulse);
                yield return null;
            }
        }

        public void ShowVictoryScreen()
        {
            if (deathPanelInstance != null)
            {
                Destroy(deathPanelInstance);
                deathPanelInstance = null;
            }
            if (hudRootGo != null)
            {
                var cg = hudRootGo.GetComponent<CanvasGroup>();
                if (cg == null) cg = hudRootGo.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;

            var victoryCanvasGo = new GameObject("VictoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var victoryCanvas = victoryCanvasGo.GetComponent<Canvas>();
            victoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            victoryCanvas.sortingOrder = 1100;

            var scaler = victoryCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            deathPanelInstance = victoryCanvasGo;

            var panelGo = new GameObject("VictoryPanelOverlay", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panelGo.SetParent(victoryCanvasGo.transform, false);
            panelGo.anchorMin = Vector2.zero; panelGo.anchorMax = Vector2.one;
            panelGo.offsetMin = panelGo.offsetMax = Vector2.zero;
            
            var bgImg = panelGo.GetComponent<Image>();
            bgImg.sprite = CreateProceduralGradientSprite(1920, 1080, new Color(0.24f, 0.18f, 0.04f, 0.0f), new Color(0.04f, 0.03f, 0.0f, 0.98f));
            bgImg.color = new Color(1f, 1f, 1f, 0f); // Set alpha to 0 for fade-in

            // Triumphant golden glowing vignette
            var vignetteGo = new GameObject("VictoryVignette", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            vignetteGo.SetParent(panelGo, false);
            vignetteGo.anchorMin = Vector2.zero; vignetteGo.anchorMax = Vector2.one;
            vignetteGo.offsetMin = vignetteGo.offsetMax = Vector2.zero;
            var vigImg = vignetteGo.GetComponent<Image>();
            vigImg.sprite = CreateProceduralGradientSprite(1920, 1080, new Color(0.95f, 0.8f, 0.2f, 0.0f), new Color(0.45f, 0.35f, 0.05f, 0.95f));
            vigImg.color = new Color(1f, 1f, 1f, 0f);

            // Container for all content that will fade in smoothly (without background/modal card)
            var contentContainerGo = new GameObject("VictoryContent", typeof(RectTransform)).GetComponent<RectTransform>();
            contentContainerGo.SetParent(panelGo, false);
            contentContainerGo.anchorMin = contentContainerGo.anchorMax = new Vector2(0.5f, 0.5f);
            contentContainerGo.anchoredPosition = Vector2.zero;
            contentContainerGo.sizeDelta = new Vector2(850, 640);
            
            var cardGroup = contentContainerGo.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;

            var titleGo = new GameObject("TitleText", typeof(RectTransform)).GetComponent<RectTransform>();
            titleGo.SetParent(contentContainerGo, false);
            titleGo.anchoredPosition = new Vector2(0, 100); titleGo.sizeDelta = new Vector2(900, 150);
            var titleText = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            titleText.font = GetTitleFont();
            titleText.fontSize = 130; // Massive Impact Title
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.95f, 0.8f, 0.2f, 0.98f); // Golden Victory Color
            titleText.text = "ESCAPED!";
            titleText.outlineColor = new Color(0.25f, 0.18f, 0.02f, 0.8f);
            titleText.outlineWidth = 0.25f;

            var btnRestart = CreateSettingsActionButton(contentContainerGo, "PLAY AGAIN", new Vector2(-180, -80), new Vector2(320, 80), () => {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }, new Color(0.95f, 0.8f, 0.2f, 0.20f));

            var btnMenu = CreateSettingsActionButton(contentContainerGo, "MAIN MENU", new Vector2(180, -80), new Vector2(320, 80), () => {
                Time.timeScale = 1f;
                HasStartedGame = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }, new Color(0.95f, 0.8f, 0.2f, 0.20f));

            // Style buttons to be yellow/gold with dark charcoal text for high contrast legibility
            var restartImg = btnRestart.GetComponent<Image>();
            if (restartImg != null) restartImg.color = new Color(0.85f, 0.65f, 0.05f, 1.0f);
            var restartTxt = btnRestart.GetComponentInChildren<TextMeshProUGUI>();
            if (restartTxt != null) {
                restartTxt.color = new Color(0.08f, 0.08f, 0.08f, 1.0f);
                restartTxt.fontSize = 20;
            }

            var menuImg = btnMenu.GetComponent<Image>();
            if (menuImg != null) menuImg.color = new Color(0.85f, 0.65f, 0.05f, 1.0f);
            var menuTxt = btnMenu.GetComponentInChildren<TextMeshProUGUI>();
            if (menuTxt != null) {
                menuTxt.color = new Color(0.08f, 0.08f, 0.08f, 1.0f);
                menuTxt.fontSize = 20;
            }

            // Play celebratory audio on victory!
            TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_tactical_vision", true, true);
            TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_pickup", false, 1.0f);

            SetLayerRecursively(victoryCanvasGo, 5);

            StartCoroutine(FadeInVictoryScreen(bgImg, cardGroup, vigImg));
        }

        private IEnumerator FadeInVictoryScreen(Image bgImg, CanvasGroup cardGroup, Image vigImg)
        {
            float duration = 0.8f;
            float elapsed = 0f;
            bgImg.color = new Color(1f, 1f, 1f, 0f);
            vigImg.color = new Color(1f, 1f, 1f, 0f);
            cardGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);

                bgImg.color = new Color(1f, 1f, 1f, smoothT);
                vigImg.color = new Color(1f, 1f, 1f, smoothT * 0.8f);
                cardGroup.alpha = smoothT;
                yield return null;
            }
            bgImg.color = Color.white;
            cardGroup.alpha = 1f;

            // Pulsing golden light effect for victory card overlay
            float pulseTimer = 0f;
            while (vigImg != null)
            {
                pulseTimer += Time.unscaledDeltaTime;
                float pulse = 0.4f + Mathf.PingPong(pulseTimer * 1.0f, 0.4f); // oscillates between 0.4 and 0.8
                vigImg.color = new Color(1f, 1f, 1f, pulse);
                yield return null;
            }
        }

        public void SetHUDVisible(bool visible)
        {
            if (hudRootGo != null)
            {
                hudRootGo.SetActive(visible);
            }
        }

        // ── Procedural lightning bolt texture generator ───────────────────────
        private Sprite CreateProceduralLightningSprite(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Vector2 start = new Vector2(width * 0.5f, height - 1);
            DrawBoltSegment(pixels, width, height, start, 0, height, 8, true);

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private void DrawBoltSegment(Color[] pixels, int width, int height, Vector2 pos, float angle, float remaining, float thickness, bool canBranch)
        {
            if (remaining < 5f) return;
            float segLen = Random.Range(8f, 25f);
            float newAngle = angle + Random.Range(-45f, 45f);
            float rad = newAngle * Mathf.Deg2Rad;
            Vector2 end = pos + new Vector2(Mathf.Sin(rad) * segLen, -Mathf.Cos(rad) * segLen);
            end.x = Mathf.Clamp(end.x, 0, width - 1);
            end.y = Mathf.Clamp(end.y, 0, height - 1);

            int steps = Mathf.Max(1, Mathf.RoundToInt(segLen));
            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                int px = Mathf.RoundToInt(Mathf.Lerp(pos.x, end.x, t));
                int py = Mathf.RoundToInt(Mathf.Lerp(pos.y, end.y, t));
                float bright = Mathf.Lerp(1f, 0.3f, t);
                
                // Draw outer cyan/blue glow (Additive-like blending)
                int glowRad = Mathf.RoundToInt(thickness * 2.5f);
                for (int dy = -glowRad; dy <= glowRad; dy++)
                    for (int dx = -glowRad; dx <= glowRad; dx++)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist > glowRad) continue;
                        int nx = Mathf.Clamp(px + dx, 0, width - 1);
                        int ny = Mathf.Clamp(py + dy, 0, height - 1);
                        float falloff = Mathf.Pow(1f - (dist / glowRad), 1.5f);
                        Color existing = pixels[ny * width + nx];
                        Color glow = new Color(0.1f, 0.5f, 1f, bright * 0.7f * falloff);
                        pixels[ny * width + nx] = new Color(
                            Mathf.Min(1f, existing.r + glow.r * glow.a),
                            Mathf.Min(1f, existing.g + glow.g * glow.a),
                            Mathf.Min(1f, existing.b + glow.b * glow.a),
                            Mathf.Max(existing.a, glow.a)
                        );
                    }

                // Draw sharp pure white core
                int coreRad = Mathf.Max(1, Mathf.RoundToInt(thickness * 0.3f));
                for (int dy = -coreRad; dy <= coreRad; dy++)
                    for (int dx = -coreRad; dx <= coreRad; dx++)
                    {
                        if (dx * dx + dy * dy > coreRad * coreRad) continue;
                        int nx = Mathf.Clamp(px + dx, 0, width - 1);
                        int ny = Mathf.Clamp(py + dy, 0, height - 1);
                        pixels[ny * width + nx] = new Color(1f, 1f, 1f, 1f);
                    }
            }

            DrawBoltSegment(pixels, width, height, end, newAngle, remaining - segLen, Mathf.Max(1f, thickness * 0.85f), canBranch);
            if (canBranch && remaining > 20f && Random.value < 0.65f)
            {
                float branchAngle = newAngle + Random.Range(25f, 70f) * (Random.value > 0.5f ? 1 : -1);
                DrawBoltSegment(pixels, width, height, end, branchAngle, remaining * Random.Range(0.3f, 0.6f), thickness * 0.5f, false);
            }
        }

        private IEnumerator MysticDustRoutine(Transform parent)
        {
            var dustSprite = CreateCircleSprite(16);
            var dustContainer = new GameObject("MysticDustContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            dustContainer.SetParent(parent, false);
            // Move behind UI elements but in front of background (Background is sibling 0)
            dustContainer.SetSiblingIndex(1); 
            dustContainer.anchorMin = Vector2.zero; dustContainer.anchorMax = Vector2.one;
            dustContainer.offsetMin = dustContainer.offsetMax = Vector2.zero;

            while (parent != null)
            {
                var dustGo = new GameObject("Dust", typeof(RectTransform), typeof(Image));
                dustGo.transform.SetParent(dustContainer, false);
                var rt = dustGo.GetComponent<RectTransform>();
                
                float size = Random.Range(6f, 16f); // Larger size
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = new Vector2(Random.Range(-960f, 960f), -600f);
                
                var img = dustGo.GetComponent<Image>();
                img.sprite = dustSprite;
                img.color = new Color(0.95f, 0.85f, 0.5f, 0.9f); // Gorgeous premium gold dust
                
                StartCoroutine(AnimateDust(rt, img));
                yield return new WaitForSeconds(Random.Range(0.1f, 0.4f));
            }
        }

        private IEnumerator AnimateDust(RectTransform rt, Image img)
        {
            float speed = Random.Range(100f, 250f);
            float drift = Random.Range(-40f, 40f);
            float lifetime = Random.Range(4f, 8f);
            float elapsed = 0f;

            while (elapsed < lifetime && rt != null)
            {
                elapsed += Time.deltaTime;
                rt.anchoredPosition += new Vector2(drift * Time.deltaTime, speed * Time.deltaTime);
                
                // Fade in and out beautifully
                float alpha = Mathf.PingPong(elapsed * 2f / lifetime, 1.0f);
                if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, alpha * 0.8f);
                
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private IEnumerator LightningFlashesRoutine(Image img)
        {
            // Create a bolt Image sibling to the flash overlay
            var boltGo = new GameObject("LightningBolt", typeof(RectTransform), typeof(Image));
            boltGo.transform.SetParent(img.transform.parent, false);
            var boltRect = boltGo.GetComponent<RectTransform>();
            boltRect.anchorMin = boltRect.anchorMax = new Vector2(0.5f, 0.5f);
            boltRect.sizeDelta = new Vector2(200, 500);
            var boltImg = boltGo.GetComponent<Image>();
            boltImg.raycastTarget = false;
            boltImg.color = new Color(1f, 1f, 1f, 0f); // Fix: Start transparent to avoid white box on game start
            
            while (img != null)
            {
                yield return new WaitForSecondsRealtime(Random.Range(2.5f, 6.5f));
                if (img == null) break;
 
                // Spawn fresh bolt at random screen position
                if (boltImg != null)
                {
                    boltImg.sprite = CreateProceduralLightningSprite(200, 500);
                    // Truly random positioning across the whole screen and 360 degree rotation
                    float rx = Random.Range(-800f, 800f);
                    float ry = Random.Range(-400f, 400f);
                    boltRect.anchoredPosition = new Vector2(rx, ry);
                    boltRect.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                    float boltScale = Random.Range(1.5f, 3f);
                    boltRect.localScale = new Vector3(boltScale, boltScale, 1f);
                }

                float flashIntensity = Random.Range(0.4f, 0.75f);
                // img.color = new Color(1f, 0.95f, 0.85f, flashIntensity); // Removed background flash
                if (boltImg != null) boltImg.color = new Color(0.85f, 0.95f, 1f, flashIntensity * 1.2f);
 
                float elapsed = 0f;
                float duration = Random.Range(0.08f, 0.15f);
                while (elapsed < duration && img != null)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float ft = elapsed / duration;
                    // img.color = new Color(1f, 0.95f, 0.85f, Mathf.Lerp(flashIntensity, 0f, ft));
                    if (boltImg != null) boltImg.color = new Color(0.85f, 0.95f, 1f, Mathf.Lerp(flashIntensity * 1.2f, 0f, ft));
                    yield return null;
                }
 
                if (img != null && Random.value < 0.6f)
                {
                    yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.12f));
                    if (img == null) break;
 
                    flashIntensity = Random.Range(0.2f, 0.45f);
                    // img.color = new Color(1f, 0.95f, 0.85f, flashIntensity);
                    if (boltImg != null) boltImg.color = new Color(0.85f, 0.95f, 1f, flashIntensity);
 
                    elapsed = 0f;
                    duration = Random.Range(0.12f, 0.25f);
                    while (elapsed < duration && img != null)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float ft = elapsed / duration;
                        // img.color = new Color(1f, 0.95f, 0.85f, Mathf.Lerp(flashIntensity, 0f, ft));
                        if (boltImg != null) boltImg.color = new Color(0.85f, 0.95f, 1f, Mathf.Lerp(flashIntensity, 0f, ft));
                        yield return null;
                    }
                }
 
                if (img != null) img.color = new Color(1f, 1f, 1f, 0f);
                if (boltImg != null) boltImg.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private void OnDestroy()
        {
            // PERFORMANCE: Explicitly kill all tweens to prevent GC handle leaks on domain reload/scene switch
            DG.Tweening.DOTween.KillAll(false);
        }
    }
}
