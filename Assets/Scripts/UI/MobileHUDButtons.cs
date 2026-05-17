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

        private Image healthBarFill;
        private Image ammoBarFill;
        private Sprite sulfurBarSprite;
        private Sprite mercuryBarSprite;
        private Sprite saltBarSprite;
        private Sprite punchBarSprite;

        private GameObject settingsModalInstance = null;

        private bool sprintToggleState = false;
        private Image sprintIconImage;
        private Image sprintShadowImage;

        private Sprite obsidianSprite;
        private Sprite goldGradientSprite;
        private Sprite joystickRingSprite;
        private Sprite joystickKnobSprite;
        private Sprite settingsIconSprite;

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
            if (result == null) result = Resources.Load<Sprite>(fallbackResourcePath);
            return result;
        }

        private void LoadSprites()
        {
            joystickRingSprite = LoadThemedSprite("joystick_outer", "UI/Icons/joystick_ring_fallback");
            joystickKnobSprite = LoadThemedSprite("joystick_knob", "UI/Icons/joystick_knob_fallback");
            
            fireIcon = LoadThemedSprite("fire", "UI/Icons/Inspiration/bullet");
            reloadIcon = LoadThemedSprite("reload_ammo", "UI/Icons/Inspiration/reload");
            swapIcon = LoadThemedSprite("swap_weapon", "UI/Icons/icon_swap");
            sprintIcon = LoadThemedSprite("sprint", "UI/Icons/icon_sprint");
            jumpIcon = LoadThemedSprite("jump", "UI/Icons/icon_jump");
            settingsIconSprite = Resources.Load<Sprite>("egyptian_items/settings_icon");
        }

        private void GenerateProceduralSprites()
        {
            obsidianSprite = CreateObsidianSprite();
            goldGradientSprite = CreateGoldenGradientSprite();
            if (joystickRingSprite == null) joystickRingSprite = CreateRingSprite();
            if (joystickKnobSprite == null) joystickKnobSprite = CreateKnobSprite();

            sulfurBarSprite = CreateAlchemicalBarSprite(new Color(0.95f, 0.55f, 0.05f), new Color(1f, 0.85f, 0.1f));
            mercuryBarSprite = CreateAlchemicalBarSprite(new Color(0.1f, 0.5f, 0.8f), new Color(0.4f, 0.9f, 0.95f));
            saltBarSprite = CreateAlchemicalBarSprite(new Color(0.9f, 0.7f, 0.2f), new Color(1f, 1f, 1f));
            punchBarSprite = CreateAlchemicalBarSprite(new Color(0.4f, 0.02f, 0.02f), new Color(0.8f, 0.05f, 0.05f));
        }

        private Sprite CreateAlchemicalBarSprite(Color startCol, Color endCol)
        {
            int w = 420, h = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    float t = (float)x / w;
                    Color col = Color.Lerp(startCol, endCol, t);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, 0.95f));
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateBorderSprite(int w, int h, int thickness, Color borderCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    if (x < thickness || x >= w - thickness || y < thickness || y >= h - thickness) tex.SetPixel(x, y, borderCol);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateSolidCircleSprite(int s, Color col)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++) {
                for (int x = 0; x < s; x++) {
                    float dx = (float)(x - s / 2) / (s / 2); float dy = (float)(y - s / 2) / (s / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f) {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * alpha));
                    } else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateHealthBarFillSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    float t = (float)x / w;
                    Color col = Color.Lerp(new Color(0.05f, 0.85f, 0.25f, 0.95f), new Color(0.95f, 0.85f, 0.05f, 0.95f), t);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateSolidBarSprite(int w, int h, Color c)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateObsidianSprite()
        {
            int width = 128, height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) {
                float t = (float)y / height;
                Color obsColor = Color.Lerp(new Color(0.08f, 0.08f, 0.08f, 0.95f), new Color(0.2f, 0.2f, 0.2f, 0.95f), t);
                for (int x = 0; x < width; x++) {
                    float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f) {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        tex.SetPixel(x, y, new Color(obsColor.r, obsColor.g, obsColor.b, obsColor.a * alpha));
                    } else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateGoldenGradientSprite()
        {
            int width = 128, height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) {
                float t = (float)y / height;
                Color goldColor = Color.Lerp(new Color(0.85f, 0.6f, 0.1f, 0.95f), new Color(1f, 0.85f, 0.3f, 0.95f), t);
                for (int x = 0; x < width; x++) {
                    float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f) {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        tex.SetPixel(x, y, new Color(goldColor.r, goldColor.g, goldColor.b, goldColor.a * alpha));
                    } else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRingSprite()
        {
            int width = 256, height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= 0.85f && dist <= 1f) {
                        float alpha = (dist > 0.95f) ? (1f - dist) / 0.05f : ((dist < 0.9f) ? (dist - 0.85f) / 0.05f : 1f);
                        tex.SetPixel(x, y, new Color(0.95f, 0.8f, 0.2f, alpha * 0.45f));
                    } else if (dist < 0.85f) tex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, 0.15f));
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateKnobSprite()
        {
            int width = 128, height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) {
                float t = (float)y / height;
                Color goldColor = Color.Lerp(new Color(0.95f, 0.8f, 0.2f, 0.95f), new Color(1f, 0.95f, 0.6f, 0.95f), t);
                for (int x = 0; x < width; x++) {
                    float dx = (float)(x - width / 2) / (width / 2); float dy = (float)(y - height / 2) / (height / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1f) {
                        float alpha = Mathf.Clamp01((1f - dist) * 10f);
                        if (dist >= 0.4f && dist <= 0.5f) tex.SetPixel(x, y, new Color(0.6f, 0.45f, 0.1f, alpha * 0.95f));
                        else tex.SetPixel(x, y, new Color(goldColor.r, goldColor.g, goldColor.b, goldColor.a * alpha));
                    } else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateSettingsMedallionSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    float dx = (float)(x - w / 2) / (w / 2); float dy = (float)(y - h / 2) / (h / 2);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 1.0f) {
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        float gearTeeth = Mathf.Sin(angle * 8 * Mathf.Deg2Rad);
                        Color medallionCol = new Color(0.95f, 0.8f, 0.2f, 0.95f);
                        if (dist > 0.85f && gearTeeth > 0.1f) tex.SetPixel(x, y, medallionCol);
                        else if (dist <= 0.85f && dist > 0.75f) tex.SetPixel(x, y, new Color(0.6f, 0.45f, 0.1f, 0.95f));
                        else if (dist <= 0.75f && dist > 0.25f) tex.SetPixel(x, y, new Color(0.08f, 0.08f, 0.08f, 0.9f));
                        else if (dist <= 0.25f) tex.SetPixel(x, y, medallionCol);
                        else tex.SetPixel(x, y, Color.clear);
                    } else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
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
            scaler.matchWidthOrHeight = 1.0f; // Force match height for consistent mobile look

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) eventSystem = GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            GameObject eventSystemGo = (eventSystem == null) ? new GameObject("EventSystem") : eventSystem.gameObject;
            if (eventSystem == null) eventSystem = eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            var modernModule = eventSystemGo.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (modernModule != null) { if (Application.isPlaying) Destroy(modernModule); else DestroyImmediate(modernModule); }
            
            if (eventSystemGo.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null) eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        public void BuildHUD()
        {
            foreach (Transform t in transform) Destroy(t.gameObject);

            var root = new GameObject("HUD_Root", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            var lookZone = new GameObject("LookZone", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            lookZone.SetParent(root, false);
            lookZone.anchorMin = new Vector2(0.4f, 0f); lookZone.anchorMax = Vector2.one;
            lookZone.offsetMin = lookZone.offsetMax = Vector2.zero;
            lookZone.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            lookZone.gameObject.AddComponent<LookSwipeZone>();

            var moveZone = new GameObject("MoveZone", typeof(RectTransform)).GetComponent<RectTransform>();
            moveZone.SetParent(root, false);
            moveZone.anchorMin = Vector2.zero; moveZone.anchorMax = new Vector2(0.4f, 1f);
            moveZone.offsetMin = moveZone.offsetMax = Vector2.zero;

            // --- MASSIVE JOYSTICK (2.5x original scale) ---
            var joystickBg = new GameObject("NativeJoystick_Bg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            joystickBg.SetParent(moveZone, false);
            joystickBg.anchorMin = joystickBg.anchorMax = new Vector2(0.4f, 0.4f); 
            joystickBg.anchoredPosition = Vector2.zero;
            joystickBg.sizeDelta = new Vector2(550, 550); 

            var bgImage = joystickBg.GetComponent<Image>();
            bgImage.color = Color.white;
            if (joystickRingSprite != null) bgImage.sprite = joystickRingSprite;

            var joystickHandle = new GameObject("HandleTarget", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            joystickHandle.SetParent(joystickBg, false);
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickHandle.sizeDelta = new Vector2(550, 550); 

            var targetImage = joystickHandle.GetComponent<Image>();
            targetImage.color = new Color(0, 0, 0, 0); targetImage.raycastTarget = true;

            var knobVisual = new GameObject("KnobVisual", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            knobVisual.SetParent(joystickHandle, false);
            knobVisual.anchoredPosition = Vector2.zero;
            knobVisual.sizeDelta = new Vector2(200, 200); 

            var visualImage = knobVisual.GetComponent<Image>();
            visualImage.color = Color.white; visualImage.raycastTarget = false;
            if (joystickKnobSprite != null) visualImage.sprite = joystickKnobSprite;

            var onScreenStick = joystickHandle.gameObject.AddComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
            onScreenStick.movementRange = 180f; 
            onScreenStick.controlPath = "<Gamepad>/leftStick"; 

            // --- ACTION BUTTONS (Scaled & Spaced for ergonomics with Notch Safety Padding) ---
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0);
            btnContainer.anchoredPosition = new Vector2(-50, 50); // Safe bezel padding

            CreateButton(btnContainer, "FIRE", new Vector2(-280, 280), 380, fireIcon, () => SetFire(true), () => SetFire(false));
            CreateButton(btnContainer, "RELOAD", new Vector2(-620, 150), 200, reloadIcon, () => Reload());
            CreateButton(btnContainer, "SWAP", new Vector2(-480, 520), 200, swapIcon, () => Swap());
            CreateSprintButton(btnContainer, new Vector2(-680, 350), 200);
            CreateButton(btnContainer, "JUMP", new Vector2(-150, 580), 220, jumpIcon, () => SetJump(true), () => SetJump(false));

            HideDebugLabels();

            // --- REFINED HEALTH PANEL ---
            var healthPanel = new GameObject("CustomHealthPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            healthPanel.SetParent(root, false);
            healthPanel.anchorMin = healthPanel.anchorMax = new Vector2(0, 1);
            healthPanel.pivot = new Vector2(0f, 1f); // Correct Pivot to Top-Left to prevent clipping
            healthPanel.anchoredPosition = new Vector2(60, -60);
            healthPanel.sizeDelta = new Vector2(400, 80);
            healthPanel.GetComponent<Image>().sprite = obsidianSprite;
            
            var hpBorder = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            hpBorder.SetParent(healthPanel, false); hpBorder.anchorMin = Vector2.zero; hpBorder.anchorMax = Vector2.one; hpBorder.offsetMin = hpBorder.offsetMax = Vector2.zero;
            hpBorder.GetComponent<Image>().sprite = CreateBorderSprite(400, 80, 4, new Color(0.95f, 0.8f, 0.2f, 0.9f));
            
            var healthTxtGo = new GameObject("HealthText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            healthTxtGo.SetParent(healthPanel, false);
            healthTxtGo.anchorMin = new Vector2(0, 0.5f); healthTxtGo.anchorMax = new Vector2(1, 0.5f);
            healthTxtGo.anchoredPosition = new Vector2(0, 0);
            healthText = healthTxtGo.GetComponent<Text>();
            healthText.font = GetRobustFont(); // Prevent crashes
            healthText.fontSize = 32; healthText.fontStyle = FontStyle.Bold; healthText.alignment = TextAnchor.MiddleCenter; healthText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            healthText.text = "100";

            // --- REFINED AMMO PANEL ---
            var ammoPanel = new GameObject("CustomAmmoPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ammoPanel.SetParent(root, false);
            ammoPanel.anchorMin = ammoPanel.anchorMax = new Vector2(0, 0);
            ammoPanel.pivot = new Vector2(0f, 0f); // Correct Pivot to Bottom-Left to prevent clipping
            ammoPanel.anchoredPosition = new Vector2(60, 60);
            ammoPanel.sizeDelta = new Vector2(400, 80);
            ammoPanel.GetComponent<Image>().sprite = obsidianSprite;
            
            var apBorder = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            apBorder.SetParent(ammoPanel, false); apBorder.anchorMin = Vector2.zero; apBorder.anchorMax = Vector2.one; apBorder.offsetMin = apBorder.offsetMax = Vector2.zero;
            apBorder.GetComponent<Image>().sprite = CreateBorderSprite(400, 80, 4, new Color(0.95f, 0.8f, 0.2f, 0.9f));
            
            var ammoTxtGo = new GameObject("AmmoText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            ammoTxtGo.SetParent(ammoPanel, false);
            ammoTxtGo.anchorMin = new Vector2(0, 0.5f); ammoTxtGo.anchorMax = new Vector2(1, 0.5f);
            ammoTxtGo.anchoredPosition = new Vector2(0, 0);
            ammoText = ammoTxtGo.GetComponent<Text>();
            ammoText.font = GetRobustFont(); // Prevent crashes
            ammoText.fontSize = 32; ammoText.fontStyle = FontStyle.Bold; ammoText.alignment = TextAnchor.MiddleCenter; ammoText.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            ammoText.text = "0 / 0";

            // --- SETTINGS BUTTON (Repositioned to the left of minimap with high-res sprite) ---
            var settingsBtnGo = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            settingsBtnGo.SetParent(root, false);
            settingsBtnGo.anchorMin = settingsBtnGo.anchorMax = new Vector2(1, 1);
            settingsBtnGo.pivot = new Vector2(1, 1);
            settingsBtnGo.anchoredPosition = new Vector2(-320, -70);
            settingsBtnGo.sizeDelta = new Vector2(80, 80);
            var settingsImg = settingsBtnGo.GetComponent<Image>();
            if (settingsIconSprite != null) settingsImg.sprite = settingsIconSprite;
            else settingsImg.sprite = CreateSettingsMedallionSprite(80, 80);
            
            var sHelper = settingsBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            sHelper.onUp = () => OpenSettingsModal(root);

            // --- TARGETING RETICLE ---
            var reticleGo = new GameObject("TargetingReticle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            reticleGo.SetParent(root, false);
            reticleGo.anchorMin = reticleGo.anchorMax = new Vector2(0.5f, 0.5f);
            reticleGo.anchoredPosition = Vector2.zero;
            reticleGo.sizeDelta = new Vector2(80, 80);
            var reticleImg = reticleGo.GetComponent<Image>();
            reticleImg.sprite = CreateTargetingReticleSprite(128);
            reticleImg.raycastTarget = false;

            new GameObject("MinimapCanvasContainer", typeof(RectTransform), typeof(MinimapUI)).transform.SetParent(root, false);
        }

        private void CreateButton(Transform p, string n, Vector2 pos, float s, Sprite icon, System.Action onDown, System.Action onUp = null)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            go.SetParent(p, false); go.anchoredPosition = pos; go.sizeDelta = new Vector2(s, s);
            var img = go.GetComponent<Image>(); img.color = new Color(0, 0, 0, 0); img.raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false); iconGo.anchorMin = Vector2.zero; iconGo.anchorMax = Vector2.one; iconGo.offsetMin = iconGo.offsetMax = Vector2.zero;
            var iImg = iconGo.GetComponent<Image>(); iImg.sprite = icon; iImg.color = Color.white; iImg.raycastTarget = false;
            iImg.preserveAspect = true; // FIX STRETCHING

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => { go.localScale = new Vector3(0.9f, 0.9f, 1f); iImg.color = new Color(0.8f, 0.8f, 0.8f, 1f); onDown?.Invoke(); };
            helper.onUp = () => { go.localScale = new Vector3(1f, 1f, 1f); iImg.color = Color.white; onUp?.Invoke(); };
        }

        private void CreateSprintButton(Transform p, Vector2 pos, float s)
        {
            var go = new GameObject("SPRINT", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            go.SetParent(p, false); go.anchoredPosition = pos; go.sizeDelta = new Vector2(s, s);
            var img = go.GetComponent<Image>(); img.color = new Color(0, 0, 0, 0); img.raycastTarget = true;

            var shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            shadowGo.SetParent(go, false); shadowGo.anchorMin = Vector2.zero; shadowGo.anchorMax = Vector2.one; shadowGo.offsetMin = shadowGo.offsetMax = Vector2.zero;
            sprintShadowImage = shadowGo.GetComponent<Image>(); sprintShadowImage.sprite = obsidianSprite; sprintShadowImage.raycastTarget = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false); iconGo.anchorMin = Vector2.zero; iconGo.anchorMax = Vector2.one; iconGo.offsetMin = iconGo.offsetMax = Vector2.zero;
            sprintIconImage = iconGo.GetComponent<Image>(); sprintIconImage.sprite = sprintIcon; sprintIconImage.raycastTarget = false;
            sprintIconImage.preserveAspect = true;

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => { sprintToggleState = !sprintToggleState; UpdateSprintVisuals(); SetSprint(sprintToggleState); };
        }

        private void UpdateSprintVisuals() {
            if (sprintShadowImage && sprintIconImage) {
                sprintShadowImage.sprite = sprintToggleState ? goldGradientSprite : obsidianSprite;
                sprintIconImage.color = sprintToggleState ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        private class ButtonInputHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
            public System.Action onDown; public System.Action onUp;
            public void OnPointerDown(PointerEventData data) => onDown?.Invoke();
            public void OnPointerUp(PointerEventData data) => onUp?.Invoke();
        }

        private class SliderDragHelper : MonoBehaviour, IDragHandler {
            public System.Action<Vector2> onDrag;
            public void OnDrag(PointerEventData data) => onDrag?.Invoke(data.position);
        }

        private void HideDebugLabels() {
            string[] names = { "Text Timescale", "Text Cursor Lock", "Text Tutorial", "Text Tutorial Text", "Text Tutorial Prompt", "Version Text", "Mouse Lock" };
            foreach (var n in names) { var l = GameObject.Find(n); if (l != null) l.SetActive(false); }
        }

        private void Update()
        {
            // Aggressively disable competing canvases, including clones and weapon UI
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in canvases) {
                if (c.gameObject.name == "MobileHUD_Root" || 
                    c.gameObject.name == "StartScreenOverlay" || 
                    c.gameObject.name == "DeathCanvas" || 
                    (c.gameObject.name == "Canvas" && c.gameObject.GetComponent<MobileHUDButtons>() != null)) continue;
                string nameLower = c.gameObject.name.ToLower();
                if (nameLower.Contains("lpsp") || nameLower.Contains("weaponui") || nameLower.Contains("hud") || nameLower.Contains("canvas") || nameLower.Contains("joystick")) {
                    if (c.gameObject != gameObject && c.gameObject.name != "MobileHUD_Root" && c.gameObject.name != "StartScreenOverlay" && c.gameObject.name != "DeathCanvas") {
                        c.gameObject.SetActive(false);
                    }
                }
            }

            var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
            if (character != null) {
                var weapon = character.GetEquippedWeapon();
                if (weapon != null) UpdateAmmo(weapon.GetAmmunitionCurrent(), weapon.GetAmmunitionTotal());
            }
            var health = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Player.PlayerHealth>();
            if (health != null) UpdateHealth(health.currentHealth);
        }

        private void SetFire(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(s);
        private void SetSprint(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetSprinting(s);
        private void SetJump(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetJumping(s);
        private void Reload() { if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsReloading = true; }
        private void Swap() { if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsSwappingWeapon = true; }

        private void OpenSettingsModal(RectTransform parentCanvas)
        {
            if (settingsModalInstance != null) return;
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            var modalBg = new GameObject("SettingsModal", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            modalBg.SetParent(parentCanvas, false); modalBg.anchorMin = Vector2.zero; modalBg.anchorMax = Vector2.one; modalBg.offsetMin = modalBg.offsetMax = Vector2.zero;
            modalBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f); settingsModalInstance = modalBg.gameObject;
            
            var dialog = new GameObject("Dialog", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            dialog.SetParent(modalBg, false); dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f); dialog.sizeDelta = new Vector2(600, 400);
            dialog.GetComponent<Image>().sprite = obsidianSprite;

            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            closeGo.SetParent(dialog, false); closeGo.anchorMin = closeGo.anchorMax = new Vector2(0.5f, 0.2f); closeGo.sizeDelta = new Vector2(200, 60);
            closeGo.GetComponent<Image>().sprite = CreateSolidBarSprite(200, 60, new Color(0.05f, 0.05f, 0.05f, 0.8f));
            closeGo.gameObject.AddComponent<ButtonInputHelper>().onUp = () => { Destroy(modalBg.gameObject); settingsModalInstance = null; if (TheAlchemistsCrypt.Input.MobileInputManager.Instance) TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true; };
        }

        public void UpdateHealth(float h) { if (healthText) healthText.text = $"VITALITY: {Mathf.CeilToInt(h)}"; }
        public void UpdateAmmo(int c, int t) { if (ammoText) ammoText.text = $"ESSENCE: {c} / {t}"; }

        public static bool HasStartedGame = false;

        private void Start()
        {
            if (!HasStartedGame)
            {
                CreateStartScreen();
            }
        }

        private void CreateStartScreen()
        {
            // Set scale to 0 to pause game
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Disable mobile input manager temporarily
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
            {
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = false;
            }

            // Create Canvas
            var startCanvasGo = new GameObject("StartScreenOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = startCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = startCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1.0f;

            // Background
            var bgGo = new GameObject("StartBackground", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bgGo.SetParent(startCanvasGo.transform, false);
            bgGo.anchorMin = Vector2.zero; bgGo.anchorMax = Vector2.one;
            bgGo.offsetMin = bgGo.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = Resources.Load<Sprite>("egyptian_items/GameStartImage");
            bgImg.color = Color.white;
            bgImg.preserveAspect = false;

            // Elegant right Obsidian Menu Panel
            var menuPanelGo = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            menuPanelGo.SetParent(startCanvasGo.transform, false);
            menuPanelGo.anchorMin = menuPanelGo.anchorMax = new Vector2(1f, 0.5f);
            menuPanelGo.anchoredPosition = new Vector2(-280, 0); // Offset to be perfectly readable on right
            menuPanelGo.sizeDelta = new Vector2(500, 700);

            var menuImg = menuPanelGo.GetComponent<Image>();
            menuImg.sprite = obsidianSprite;

            var menuBorderGo = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            menuBorderGo.SetParent(menuPanelGo, false);
            menuBorderGo.anchorMin = Vector2.zero; menuBorderGo.anchorMax = Vector2.one;
            menuBorderGo.offsetMin = menuBorderGo.offsetMax = Vector2.zero;
            menuBorderGo.GetComponent<Image>().sprite = CreateBorderSprite(500, 700, 5, new Color(0.95f, 0.8f, 0.2f, 0.95f));

            // Title Text
            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            titleGo.SetParent(menuPanelGo, false);
            titleGo.anchorMin = titleGo.anchorMax = new Vector2(0.5f, 1f);
            titleGo.anchoredPosition = new Vector2(0, -100);
            titleGo.sizeDelta = new Vector2(440, 150);
            var titleTxt = titleGo.GetComponent<Text>();
            titleTxt.font = GetRobustFont();
            titleTxt.fontSize = 44;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            titleTxt.text = "THE ALCHEMIST'S\nCRYPT";

            // Subtitle
            var subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            subGo.SetParent(menuPanelGo, false);
            subGo.anchorMin = subGo.anchorMax = new Vector2(0.5f, 1f);
            subGo.anchoredPosition = new Vector2(0, -260);
            subGo.sizeDelta = new Vector2(440, 60);
            var subTxt = subGo.GetComponent<Text>();
            subTxt.font = GetRobustFont();
            subTxt.fontSize = 20;
            subTxt.fontStyle = FontStyle.Italic;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
            subTxt.text = "Unravel the Pharaoh's secrets...";

            // START Button
            var startBtnGo = new GameObject("StartButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            startBtnGo.SetParent(menuPanelGo, false);
            startBtnGo.anchorMin = startBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            startBtnGo.anchoredPosition = new Vector2(0, -60);
            startBtnGo.sizeDelta = new Vector2(360, 80);
            var startBtnImg = startBtnGo.GetComponent<Image>();
            startBtnImg.sprite = goldGradientSprite;

            var startBtnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            startBtnTextGo.SetParent(startBtnGo, false);
            startBtnTextGo.anchorMin = Vector2.zero; startBtnTextGo.anchorMax = Vector2.one;
            startBtnTextGo.offsetMin = startBtnTextGo.offsetMax = Vector2.zero;
            var startBtnTxt = startBtnTextGo.GetComponent<Text>();
            startBtnTxt.font = GetRobustFont();
            startBtnTxt.fontSize = 28;
            startBtnTxt.fontStyle = FontStyle.Bold;
            startBtnTxt.alignment = TextAnchor.MiddleCenter;
            startBtnTxt.color = new Color(0.12f, 0.06f, 0f, 0.95f);
            startBtnTxt.text = "START VOYAGE";

            var startHelper = startBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            startHelper.onDown = () =>
            {
                startBtnGo.localScale = new Vector3(0.95f, 0.95f, 1f);
            };
            startHelper.onUp = () =>
            {
                startBtnGo.localScale = new Vector3(1f, 1f, 1f);
                HasStartedGame = true;
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.enabled = true;
                }
                Destroy(startCanvasGo);
            };

            // QUIT Button
            var quitBtnGo = new GameObject("QuitButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            quitBtnGo.SetParent(menuPanelGo, false);
            quitBtnGo.anchorMin = quitBtnGo.anchorMax = new Vector2(0.5f, 0.5f);
            quitBtnGo.anchoredPosition = new Vector2(0, -170);
            quitBtnGo.sizeDelta = new Vector2(360, 80);
            var quitBtnImg = quitBtnGo.GetComponent<Image>();
            quitBtnImg.sprite = obsidianSprite;

            var quitBorderGo = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            quitBorderGo.SetParent(quitBtnGo, false);
            quitBorderGo.anchorMin = Vector2.zero; quitBorderGo.anchorMax = Vector2.one;
            quitBorderGo.offsetMin = quitBorderGo.offsetMax = Vector2.zero;
            quitBorderGo.GetComponent<Image>().sprite = CreateBorderSprite(360, 80, 3, new Color(0.95f, 0.8f, 0.2f, 0.9f));

            var quitBtnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            quitBtnTextGo.SetParent(quitBtnGo, false);
            quitBtnTextGo.anchorMin = Vector2.zero; quitBtnTextGo.anchorMax = Vector2.one;
            quitBtnTextGo.offsetMin = quitBtnTextGo.offsetMax = Vector2.zero;
            var quitBtnTxt = quitBtnTextGo.GetComponent<Text>();
            quitBtnTxt.font = GetRobustFont();
            quitBtnTxt.fontSize = 28;
            quitBtnTxt.fontStyle = FontStyle.Bold;
            quitBtnTxt.alignment = TextAnchor.MiddleCenter;
            quitBtnTxt.color = new Color(0.95f, 0.8f, 0.2f, 0.95f);
            quitBtnTxt.text = "QUIT GAME";

            var quitHelper = quitBtnGo.gameObject.AddComponent<ButtonInputHelper>();
            quitHelper.onDown = () =>
            {
                quitBtnGo.localScale = new Vector3(0.95f, 0.95f, 1f);
            };
            quitHelper.onUp = () =>
            {
                quitBtnGo.localScale = new Vector3(1f, 1f, 1f);
                Application.Quit();
            };

            // Set UI layer
            SetLayerRecursively(startCanvasGo, 5);
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

        private Sprite CreateTargetingReticleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            Color gold = new Color(0.95f, 0.8f, 0.2f, 0.9f);
            Color ruby = new Color(0.85f, 0.1f, 0.1f, 0.95f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Concentric outer ring
                    if (dist >= 0.78f && dist <= 0.82f)
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    // Inner ring
                    else if (dist >= 0.38f && dist <= 0.42f)
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    // Concentric tick marks
                    else if (dist >= 0.45f && dist <= 0.75f && (Mathf.Abs(dx) < 0.03f || Mathf.Abs(dy) < 0.03f))
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    // Glowing Ruby Center Point
                    else if (dist <= 0.08f)
                    {
                        float alpha = Mathf.Clamp01((1f - dist / 0.08f) * 2f);
                        tex.SetPixel(x, y, new Color(ruby.r, ruby.g, ruby.b, ruby.a * alpha));
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
    }

    public class LookSwipeZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public float sensitivity = 0.08f; 
        private int trackedPointerId = -1;
        private void Start() => sensitivity = PlayerPrefs.GetFloat("MobileSensitivity", 0.08f);
        public void OnPointerDown(PointerEventData data) { if (trackedPointerId == -1) trackedPointerId = data.pointerId; }
        public void OnDrag(PointerEventData data) {
            if (data.pointerId != trackedPointerId) return;
            float deviceDpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            Vector2 delta = data.delta * sensitivity * (160f / deviceDpi);
            if (delta.sqrMagnitude > 0.0001f) TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(delta);
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == trackedPointerId) { trackedPointerId = -1; TheAlchemistsCrypt.Input.MobileInputManager.Instance?.ConsumeLook(); } }
    }
}
