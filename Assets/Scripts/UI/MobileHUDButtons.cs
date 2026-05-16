using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        public static MobileHUDButtons Instance { get; private set; }

        private Color goldColor = new Color(1f, 0.85f, 0.4f);
        private Color darkColor = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        
        private Sprite circleSprite;
        private Sprite reloadIcon;
        private Sprite fireIcon;
        private Sprite swapIcon;
        private Sprite sprintIcon;

        private Text healthText;
        private Text ammoText;

        private void Awake()
        {
            Instance = this;
            LoadSprites();
            SetupCanvas();
            BuildHUD();
        }

        private void LoadSprites()
        {
            // Use built-in knob for circle or try to find a resource
            circleSprite = Resources.Load<Sprite>("UI/Circle"); 
            #if UNITY_EDITOR
            if (circleSprite == null) circleSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            #endif

            reloadIcon = Resources.Load<Sprite>("UI/Icons/Inspiration/reload");
            // User requested Assets/Resources/UI/Icons/icon_crouch.png for fire
            fireIcon = Resources.Load<Sprite>("UI/Icons/icon_crouch");
            if (fireIcon == null) fireIcon = Resources.Load<Sprite>("UI/Icons/icon_attack");
            
            swapIcon = Resources.Load<Sprite>("UI/Icons/icon_swap");
            sprintIcon = Resources.Load<Sprite>("UI/Icons/icon_sprint");
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

            // Variable Joystick
            var joyPrefab = Resources.Load<GameObject>("Joystick Pack/Prefabs/Variable Joystick");
#if UNITY_EDITOR
            if (joyPrefab == null) joyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Joystick Pack/Prefabs/Variable Joystick.prefab");
#endif

            if (joyPrefab != null) {
                var joyObj = Instantiate(joyPrefab, moveZone);
                var jRect = joyObj.GetComponent<RectTransform>();
                jRect.anchorMin = jRect.anchorMax = new Vector2(0.5f, 0.3f);
                jRect.anchoredPosition = Vector2.zero;
                jRect.sizeDelta = new Vector2(500, 500); // Increased size
                
                var j = joyObj.GetComponent<Joystick>();
                if (j != null) {
                    // Try to force Variable/Dynamic mode if available via reflection or cast
                    // VariableJoystick is the common type for this prefab
                    var vj = j as VariableJoystick;
                    if (vj != null) vj.SetMode(JoystickType.Dynamic);
                    
                    StartCoroutine(JoystickLoop(j));
                }
            }

            // 3. BUTTONS (CLUSTERED BOTTOM RIGHT)
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0); // BOTTOM RIGHT
            btnContainer.anchoredPosition = Vector2.zero;

            // Updated Fire Button with larger size and explicit single-fire handling if needed
            CreateButton(btnContainer, "FIRE", new Vector2(-300, 300), 320, fireIcon, Color.white, () => SetFire(true), () => SetFire(false));
            CreateButton(btnContainer, "RELOAD", new Vector2(-650, 150), 180, reloadIcon, Color.white, () => Reload());
            CreateButton(btnContainer, "SWAP", new Vector2(-450, 600), 180, swapIcon, Color.white, () => Swap());
            CreateButton(btnContainer, "SPRINT", new Vector2(-650, 450), 180, sprintIcon, Color.white, () => SetSprint(true), () => SetSprint(false));

            HideDebugLabels();

            // 4. STATS (TOP LEFT)
            var stats = new GameObject("Stats", typeof(RectTransform)).GetComponent<RectTransform>();
            stats.SetParent(root, false);
            stats.anchorMin = stats.anchorMax = new Vector2(0, 1);
            stats.anchoredPosition = new Vector2(100, -100);

            healthText = CreateStatsText(stats, "Health", "100", Vector2.zero, new Color(1, 0.4f, 0.4f));

            // Ensure look zone is below buttons for raycasting
            lookZone.SetAsFirstSibling();
        }

        private void CreateButton(Transform p, string n, Vector2 pos, float s, Sprite icon, Color tint, System.Action onDown, System.Action onUp = null)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            go.SetParent(p, false);
            go.anchoredPosition = pos;
            go.sizeDelta = new Vector2(s, s);
            
            var img = go.GetComponent<Image>();
            img.color = Color.white; // White button background
            if (circleSprite) img.sprite = circleSprite;
            img.raycastTarget = true; // Crucial for input

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false);
            iconGo.sizeDelta = go.sizeDelta * 0.55f;
            var iImg = iconGo.GetComponent<Image>();
            iImg.color = Color.black; // Black icon
            if (icon) iImg.sprite = icon;
            iImg.raycastTarget = false; // Don't block parent button

            // Using a dedicated component for better reliability than EventTrigger entries
            var helper = go.gameObject.AddComponent<ButtonInputHelper>();
            helper.onDown = () => { img.color = new Color(0.8f, 0.8f, 0.8f, 1f); onDown?.Invoke(); };
            helper.onUp = () => { img.color = Color.white; onUp?.Invoke(); };
        }

        // Dedicated helper class to ensure clean pointer events
        private class ButtonInputHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public System.Action onDown;
            public System.Action onUp;
            public void OnPointerDown(PointerEventData eventData) => onDown?.Invoke();
            public void OnPointerUp(PointerEventData eventData) => onUp?.Invoke();
        }

        private void HideDebugLabels()
        {
            // Find common debug labels in the P_LPSP_UI_Canvas and children
            string[] names = { "Text Timescale", "Text Cursor Lock", "Text Tutorial", "Text Tutorial Text", "Text Tutorial Prompt", "Version Text", "Mouse Lock" };
            foreach (var n in names)
            {
                var label = GameObject.Find(n);
                if (label != null) label.SetActive(false);
                
                // Fallback: search by partial name in all objects
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

        private IEnumerator JoystickLoop(Joystick j)
        {
            while (true) {
                if (j != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) {
                    // Use Horizontal and Vertical directly to avoid any weirdness with Direction property
                    Vector2 dir = new Vector2(j.Horizontal, j.Vertical);
                    // If the joystick is behaving inverted, we can fix it here, but let's first ensure it's not a deadzone issue
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.SetMovement(dir);
                }
                yield return null;
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
        public float sensitivity = 1.2f; // Increased baseline for DPI-scaled input
        private int trackedPointerId = -1;

        public void OnPointerDown(PointerEventData data)
        {
            if (trackedPointerId != -1) return;
            trackedPointerId = data.pointerId;
            // NotifyTouchActive is now a no-op as the Manager polls globally, 
            // but we keep the call for architecture consistency.
            TheAlchemistsCrypt.Input.MobileInputManager.Instance?.NotifyTouchActive(true);
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != trackedPointerId) return;

            // DPI Scaling: Normalize pixels to physical distance (Inches) 
            // baseDpi 160 (Standard MDPI) ensures consistent feel across devices.
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
