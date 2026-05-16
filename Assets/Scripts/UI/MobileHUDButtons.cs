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
        
        private Sprite btnSprite;
        private Sprite reloadIcon;
        private Sprite fireIcon;
        private Sprite swapIcon;

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
            // Use Resources or fallback to simple primitives if sprites missing
            btnSprite = Resources.Load<Sprite>("UI/Button"); 
            reloadIcon = Resources.Load<Sprite>("UI/Reload");
            fireIcon = Resources.Load<Sprite>("UI/Fire");
            swapIcon = Resources.Load<Sprite>("UI/Swap");
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
                jRect.anchorMin = jRect.anchorMax = new Vector2(0.25f, 0.25f);
                jRect.anchoredPosition = Vector2.zero;
                jRect.sizeDelta = new Vector2(350, 350);
                
                var j = joyObj.GetComponent<Joystick>();
                if (j != null) StartCoroutine(JoystickLoop(j));
            }

            // 3. BUTTONS (CLUSTERED BOTTOM RIGHT)
            var btnContainer = new GameObject("ButtonContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            btnContainer.SetParent(root, false);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(1, 0); // BOTTOM RIGHT
            btnContainer.anchoredPosition = Vector2.zero;

            // Fire (Main)
            CreateButton(btnContainer, "FIRE", new Vector2(-280, 280), 280, fireIcon, new Color(1, 0.3f, 0.2f), () => SetFire(true), () => SetFire(false));
            // Reload
            CreateButton(btnContainer, "RELOAD", new Vector2(-580, 180), 180, reloadIcon, goldColor, () => Reload());
            // Swap
            CreateButton(btnContainer, "SWAP", new Vector2(-380, 580), 180, swapIcon, new Color(0.3f, 0.8f, 1f), () => Swap());

            // 4. STATS (TOP LEFT)
            var stats = new GameObject("Stats", typeof(RectTransform)).GetComponent<RectTransform>();
            stats.SetParent(root, false);
            stats.anchorMin = stats.anchorMax = new Vector2(0, 1);
            stats.anchoredPosition = new Vector2(100, -100);

            healthText = CreateStatsText(stats, "Health", "100", Vector2.zero, new Color(1, 0.4f, 0.4f));
            ammoText = CreateStatsText(stats, "Ammo", "30 / 90", new Vector2(0, -70), goldColor);

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
            img.color = darkColor;
            if (btnSprite) img.sprite = btnSprite;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            iconGo.SetParent(go, false);
            iconGo.sizeDelta = go.sizeDelta * 0.6f;
            var iImg = iconGo.GetComponent<Image>();
            iImg.color = tint;
            if (icon) iImg.sprite = icon;

            var trigger = go.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((d) => { img.color = tint * 0.5f; onDown?.Invoke(); });
            trigger.triggers.Add(down);
            
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((d) => { img.color = darkColor; onUp?.Invoke(); });
            trigger.triggers.Add(up);
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
                    // Normalize and fix direction if needed
                    Vector2 dir = j.Direction;
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.SetMovement(dir);
                }
                yield return null;
            }
        }

        private void SetFire(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(s);
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
            float dpiScale = Screen.dpi > 0 ? (160f / Screen.dpi) : 1f;
            Vector2 delta = data.delta / Time.deltaTime;
            Vector2 lookVel = delta * sensitivity * dpiScale * 0.006f;
            TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(lookVel);
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
