using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public static MobileHUDButtons Instance { get; private set; }

        private Text healthText;
        private Text ammoText;
        private Image damageOverlay;
        
        private Color goldColor = new Color(0.9f, 0.75f, 0.2f);
        private Color darkColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);

        // Joystick
        private RectTransform joystickBG;
        private RectTransform joystickHandle;
        private Vector2 joystickInput = Vector2.zero;
        private float joystickRange = 150f;

        // Touch Look
        private int lookFingerId = -1;
        private Vector2 lastLookPos;

        private void Awake()
        {
            Instance = this;
            SetupCanvas();
            BuildHUD();
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

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void BuildHUD()
        {
            foreach (Transform t in transform) Destroy(t.gameObject);

            var root = new GameObject("HUD_Root");
            root.transform.SetParent(transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;

            // Stats
            Font mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var stats = new GameObject("Stats");
            stats.transform.SetParent(root.transform, false);
            var statsRect = stats.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1); statsRect.anchorMax = new Vector2(0, 1);
            statsRect.anchoredPosition = new Vector2(80, -80);
            statsRect.sizeDelta = new Vector2(600, 200);

            healthText = CreateText(stats.transform, "H", "HEALTH: 100", mainFont, new Color(1f, 0.3f, 0.3f), 60, Vector2.zero);
            ammoText = CreateText(stats.transform, "A", "AMMO: --", mainFont, goldColor, 60, new Vector2(0, -80));

            // Joystick
            var joy = new GameObject("Joystick");
            joy.transform.SetParent(root.transform, false);
            joystickBG = joy.AddComponent<RectTransform>();
            joystickBG.anchorMin = Vector2.zero; joystickBG.anchorMax = Vector2.zero;
            joystickBG.anchoredPosition = new Vector2(250, 250);
            joystickBG.sizeDelta = new Vector2(400, 400);
            var bgImg = joy.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(joy.transform, false);
            joystickHandle = handle.AddComponent<RectTransform>();
            joystickHandle.sizeDelta = new Vector2(160, 160);
            var hImg = handle.AddComponent<Image>();
            hImg.color = goldColor;

            // Buttons (Fire, Switch, Reload)
            CreateButton(root.transform, "FIRE", new Vector2(-220, 220), new Color(0.8f, 0.1f, 0.1f), () => SetFire(true), () => SetFire(false));
            CreateButton(root.transform, "SWAP", new Vector2(-480, 220), new Color(0.1f, 0.7f, 0.8f), () => {
                var inv = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>()?.GetPlayerCharacter()?.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>();
                if (inv != null) inv.Equip(inv.GetNextIndex());
            });
            CreateButton(root.transform, "RELOAD", new Vector2(-220, 480), goldColor, () => {
                var charSvc = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>();
                charSvc?.GetPlayerCharacter()?.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>()?.GetEquipped()?.Reload();
            });

            // Crosshair
            var cross = new GameObject("Crosshair");
            cross.transform.SetParent(root.transform, false);
            cross.AddComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
            cross.AddComponent<Image>().color = new Color(1, 0.9f, 0, 0.5f);

            // Damage Overlay
            var overlay = new GameObject("Damage");
            overlay.transform.SetParent(root.transform, false);
            overlay.transform.SetAsFirstSibling();
            var ovRect = overlay.AddComponent<RectTransform>();
            ovRect.anchorMin = Vector2.zero; ovRect.anchorMax = Vector2.one;
            damageOverlay = overlay.AddComponent<Image>();
            damageOverlay.color = new Color(1, 0, 0, 0);
            damageOverlay.raycastTarget = false;
        }

        private Text CreateText(Transform p, string n, string v, Font f, Color c, int s, Vector2 pos)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1); r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(600, 100);
            var t = go.AddComponent<Text>();
            t.font = f; t.fontSize = s; t.color = c; t.text = v;
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        private void CreateButton(Transform p, string l, Vector2 pos, Color c, System.Action onDown, System.Action onUp = null)
        {
            var go = new GameObject(l); go.transform.SetParent(p, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = Vector2.one; r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(200, 200);
            var img = go.AddComponent<Image>(); img.color = darkColor;
            
            var trigger = go.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((data) => onDown?.Invoke());
            trigger.triggers.Add(down);
            
            if (onUp != null) {
                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener((data) => onUp?.Invoke());
                trigger.triggers.Add(up);
            }

            var txt = new GameObject("T"); txt.transform.SetParent(go.transform, false);
            var t = txt.AddComponent<Text>(); t.text = l; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = c; t.fontSize = 40; t.alignment = TextAnchor.MiddleCenter;
            txt.GetComponent<RectTransform>().sizeDelta = r.sizeDelta;
        }

        private void SetFire(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(s);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.position.x > Screen.width / 2) {
                lookFingerId = eventData.pointerId;
                lastLookPos = eventData.position;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == lookFingerId) {
                lookFingerId = -1;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(Vector2.zero);
            }
            
            if (Vector2.Distance(eventData.position, joystickBG.position) < 300f) {
                joystickInput = Vector2.zero;
                joystickHandle.anchoredPosition = Vector2.zero;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetMovement(Vector2.zero);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == lookFingerId) {
                Vector2 delta = eventData.position - lastLookPos;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(delta * 0.1f);
                lastLookPos = eventData.position;
            }

            if (Vector2.Distance(eventData.pressPosition, joystickBG.position) < 300f) {
                Vector2 dir = eventData.position - (Vector2)joystickBG.position;
                joystickInput = Vector2.ClampMagnitude(dir, joystickRange);
                joystickHandle.anchoredPosition = joystickInput;
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetMovement(joystickInput / joystickRange);
            }
        }

        public void UpdateHealth(float h) { if (healthText) healthText.text = $"HEALTH: {Mathf.CeilToInt(h)}"; }
        public void UpdateAmmo(int c, int t) { if (ammoText) ammoText.text = $"AMMO: {c} / {t}"; }
        public void TriggerDamage() { if (damageOverlay) StartCoroutine(Flash()); }
        private IEnumerator Flash() { damageOverlay.color = new Color(1, 0, 0, 0.4f); yield return new WaitForSeconds(0.1f); damageOverlay.color = Color.clear; }
    }
}
