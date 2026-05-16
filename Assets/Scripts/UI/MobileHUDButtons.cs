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

            // 1. LOOK ZONE (Fullscreen but lower priority than buttons)
            var lookZone = new GameObject("LookZone", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            lookZone.SetParent(root, false);
            lookZone.anchorMin = new Vector2(0.5f, 0f); lookZone.anchorMax = Vector2.one;
            lookZone.offsetMin = lookZone.offsetMax = Vector2.zero;
            lookZone.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            lookZone.gameObject.AddComponent<LookSwipeZone>();

            // 2. MOVEMENT ZONE
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
                jRect.anchorMin = jRect.anchorMax = new Vector2(0.3f, 0.3f);
                jRect.anchoredPosition = Vector2.zero;
                jRect.sizeDelta = new Vector2(450, 450); // Bigger joystick
                
                var j = joyObj.GetComponent<Joystick>();
                if (j != null) StartCoroutine(JoystickLoop(j));
            }

            // 3. BUTTONS (CLUSTERED BOTTOM RIGHT)
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0); // BOTTOM RIGHT
            btnContainer.anchoredPosition = Vector2.zero;

            // Use white icons on black semi-transparent circular buttons
            // Fire (Main)
            CreateButton(btnContainer, "FIRE", new Vector2(-300, 300), 300, fireIcon, Color.white, () => SetFire(true), () => SetFire(false));
            // Reload
            CreateButton(btnContainer, "RELOAD", new Vector2(-650, 150), 180, reloadIcon, Color.white, () => Reload());
            // Swap
            CreateButton(btnContainer, "SWAP", new Vector2(-450, 600), 180, swapIcon, Color.white, () => Swap());
            // Sprint
            CreateButton(btnContainer, "SPRINT", new Vector2(-650, 450), 180, sprintIcon, Color.white, () => SetSprint(true), () => SetSprint(false));

            // Hide debug labels from asset pack
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
            img.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
            if (circleSprite) img.sprite = circleSprite;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false);
            iconGo.sizeDelta = go.sizeDelta * 0.55f;
            var iImg = iconGo.GetComponent<Image>();
            iImg.color = tint; // White as passed
            if (icon) iImg.sprite = icon;

            var trigger = go.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((d) => { img.color = new Color(1, 1, 1, 0.3f); onDown?.Invoke(); });
            trigger.triggers.Add(down);
            
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((d) => { img.color = new Color(0, 0, 0, 0.5f); onUp?.Invoke(); });
            trigger.triggers.Add(up);
        }

        private void HideDebugLabels()
        {
            // Find common debug labels in the P_LPSP_UI_Canvas
            var timescale = GameObject.Find("Timescale");
            if (timescale) timescale.SetActive(false);
            var mouseLock = GameObject.Find("Mouse Lock");
            if (mouseLock) mouseLock.SetActive(false);
            var tutorial = GameObject.Find("Tutorial");
            if (tutorial) tutorial.SetActive(false);
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
        private int pointerId = -1;
        public float sensitivity = 1.2f;

        public void OnPointerDown(PointerEventData data)
        {
            if (pointerId == -1 && data.position.x >= Screen.width * 0.5f) {
                pointerId = data.pointerId;
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointerId) return;
            // Use pixels directly for more consistent movement across frame rates
            Vector2 delta = data.delta;
            // Sensitivity check
            if (Mathf.Abs(delta.x) > 0.01f || Mathf.Abs(delta.y) > 0.01f)
            {
                Vector2 lookVel = delta * sensitivity * 0.12f;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(lookVel);
            }
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId == pointerId) {
                pointerId = -1;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(Vector2.zero);
            }
        }
    }
}
