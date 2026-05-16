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

        private Text healthText;
        private Text ammoText;
        private Text weaponText;

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

        private void LoadSprites()
        {
            reloadIcon = Resources.Load<Sprite>("UI/Icons/Inspiration/reload");
            
            // Try loading bullet.png as requested by user
            fireIcon = Resources.Load<Sprite>("UI/Icons/Inspiration/bullet");
            if (fireIcon == null) fireIcon = Resources.Load<Sprite>("UI/Icons/icon_crouch");
            if (fireIcon == null) fireIcon = Resources.Load<Sprite>("UI/Icons/icon_attack");
            
            swapIcon = Resources.Load<Sprite>("UI/Icons/icon_swap");
            sprintIcon = Resources.Load<Sprite>("UI/Icons/icon_sprint");
        }

        private void GenerateProceduralSprites()
        {
            obsidianSprite = CreateObsidianSprite();
            goldGradientSprite = CreateGoldenGradientSprite();
            joystickRingSprite = CreateRingSprite();
            joystickKnobSprite = CreateKnobSprite();
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

            // --- NATIVE JOYSTICK UI GENERATION (DOUBLE SCALED) ---
            var joystickBg = new GameObject("NativeJoystick_Bg", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            joystickBg.SetParent(moveZone, false);
            joystickBg.anchorMin = joystickBg.anchorMax = new Vector2(0.5f, 0.35f); 
            joystickBg.anchoredPosition = Vector2.zero;
            joystickBg.sizeDelta = new Vector2(500, 500); 

            var bgImage = joystickBg.GetComponent<Image>();
            bgImage.color = Color.white;
            if (joystickRingSprite != null) bgImage.sprite = joystickRingSprite;

            var joystickHandle = new GameObject("HandleTarget", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            joystickHandle.SetParent(joystickBg, false);
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickHandle.sizeDelta = new Vector2(500, 500); 

            var targetImage = joystickHandle.GetComponent<Image>();
            targetImage.color = new Color(0, 0, 0, 0); 
            targetImage.raycastTarget = true;

            var knobVisual = new GameObject("KnobVisual", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            knobVisual.SetParent(joystickHandle, false);
            knobVisual.anchoredPosition = Vector2.zero;
            knobVisual.sizeDelta = new Vector2(180, 180); 

            var visualImage = knobVisual.GetComponent<Image>();
            visualImage.color = Color.white;
            visualImage.raycastTarget = false;
            if (joystickKnobSprite != null) visualImage.sprite = joystickKnobSprite;

            var onScreenStick = joystickHandle.gameObject.AddComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
            onScreenStick.movementRange = 200f; 
            onScreenStick.controlPath = "<Gamepad>/leftStick"; 

            // 3. BUTTONS (CLUSTERED BOTTOM RIGHT)
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0); // BOTTOM RIGHT
            btnContainer.anchoredPosition = Vector2.zero;

            CreateButton(btnContainer, "FIRE", new Vector2(-300, 300), 320, fireIcon, true, () => SetFire(true), () => SetFire(false));
            CreateButton(btnContainer, "RELOAD", new Vector2(-650, 150), 180, reloadIcon, false, () => Reload());
            CreateButton(btnContainer, "SWAP", new Vector2(-450, 600), 180, swapIcon, false, () => Swap());
            CreateButton(btnContainer, "SPRINT", new Vector2(-650, 450), 180, sprintIcon, false, () => SetSprint(true), () => SetSprint(false));

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

        private void CreateButton(Transform p, string n, Vector2 pos, float s, Sprite icon, bool isFire, System.Action onDown, System.Action onUp = null)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            go.SetParent(p, false);
            go.anchoredPosition = pos;
            go.sizeDelta = new Vector2(s, s);
            
            var img = go.GetComponent<Image>();
            img.sprite = isFire ? obsidianSprite : goldGradientSprite;
            img.color = Color.white;
            img.raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false);
            iconGo.sizeDelta = go.sizeDelta * 0.55f;
            var iImg = iconGo.GetComponent<Image>();
            
            iImg.color = isFire ? Color.white : Color.black; 
            if (icon) iImg.sprite = icon;
            iImg.raycastTarget = false;

            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => {
                go.localScale = new Vector3(0.9f, 0.9f, 1f); 
                img.color = isFire ? new Color(0.7f, 0.7f, 0.7f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);
                onDown?.Invoke();
            };
            helper.onUp = () => {
                go.localScale = new Vector3(1f, 1f, 1f);
                img.color = Color.white;
                onUp?.Invoke();
            };
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
        public float sensitivity = 0.25f; 
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
