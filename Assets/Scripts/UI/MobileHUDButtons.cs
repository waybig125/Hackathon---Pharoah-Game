using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        public static MobileHUDButtons Instance { get; private set; }

        private Text healthText;
        private Text ammoText;
        private Image damageOverlay;
        
        private Color goldColor = new Color(0.9f, 0.75f, 0.2f);
        private Color darkColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);

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
            
            // Allow mouse to still move if in editor for dev, but mobile is unlocked
            #if !UNITY_EDITOR
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            #endif
        }

        public void BuildHUD()
        {
            foreach (Transform t in transform) Destroy(t.gameObject);

            var root = new GameObject("HUD_Root");
            root.transform.SetParent(transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            // 1. DIVIDE SCREEN (Left 50% Move, Right 50% Look)
            
            // JOYSTICK ZONE (Left)
            var joyZone = new GameObject("JoystickZone");
            joyZone.transform.SetParent(root.transform, false);
            var jzRect = joyZone.AddComponent<RectTransform>();
            jzRect.anchorMin = Vector2.zero; jzRect.anchorMax = new Vector2(0.5f, 1f);
            jzRect.offsetMin = jzRect.offsetMax = Vector2.zero;

            // Instantiate Variable Joystick from Pack
            var joyPrefab = Resources.Load<GameObject>("Joystick Pack/Prefabs/Variable Joystick");
            if (joyPrefab == null) {
                // Fallback search via AssetDatabase if runtime load fails in editor
                #if UNITY_EDITOR
                joyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Joystick Pack/Prefabs/Variable Joystick.prefab");
                #endif
            }

            if (joyPrefab != null) {
                var joyObj = Instantiate(joyPrefab, joyZone.transform);
                var j = joyObj.GetComponent<Joystick>();
                StartCoroutine(InputBridgeRoutine(j));
            }

            // LOOK ZONE (Right)
            var lookZone = new GameObject("LookZone");
            lookZone.transform.SetParent(root.transform, false);
            var lzRect = lookZone.AddComponent<RectTransform>();
            lzRect.anchorMin = new Vector2(0.5f, 0f); lzRect.anchorMax = Vector2.one;
            lzRect.offsetMin = lzRect.offsetMax = Vector2.zero;
            var lzImg = lookZone.AddComponent<Image>();
            lzImg.color = new Color(0, 0, 0, 0.01f); // Transparent but raycastable
            lookZone.AddComponent<LookTouchZone>();

            // 2. STATS & BUTTONS
            Font mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var stats = new GameObject("Stats");
            stats.transform.SetParent(root.transform, false);
            var statsRect = stats.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1); statsRect.anchorMax = new Vector2(0, 1);
            statsRect.anchoredPosition = new Vector2(80, -80);
            statsRect.sizeDelta = new Vector2(600, 200);

            healthText = CreateText(stats.transform, "H", "HEALTH: 100", mainFont, new Color(1f, 0.3f, 0.3f), 60, Vector2.zero);
            ammoText = CreateText(stats.transform, "A", "AMMO: --", mainFont, goldColor, 60, new Vector2(0, -80));

            // Buttons
            CreateButton(root.transform, "FIRE", new Vector2(-250, 250), new Color(0.9f, 0.2f, 0.1f), () => SetFire(true), () => SetFire(false), true);
            CreateButton(root.transform, "SWAP", new Vector2(-550, 250), new Color(0.2f, 0.8f, 1f), () => {
                var inv = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>()?.GetPlayerCharacter()?.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>();
                if (inv != null) inv.Equip(inv.GetNextIndex());
            });
            CreateButton(root.transform, "RELOAD", new Vector2(-250, 550), goldColor, () => {
                var charSvc = InfimaGames.LowPolyShooterPack.ServiceLocator.Current.Get<InfimaGames.LowPolyShooterPack.IGameModeService>();
                charSvc?.GetPlayerCharacter()?.GetComponent<InfimaGames.LowPolyShooterPack.Inventory>()?.GetEquipped()?.Reload();
            });

            // Crosshair
            var cross = new GameObject("Crosshair");
            cross.transform.SetParent(root.transform, false);
            cross.AddComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
            var cImg = cross.AddComponent<Image>();
            cImg.color = new Color(1, 0.9f, 0, 0.5f);

            // Damage Overlay
            var overlay = new GameObject("DamageOverlay");
            overlay.transform.SetParent(root.transform, false);
            overlay.transform.SetAsFirstSibling();
            var ovRect = overlay.AddComponent<RectTransform>();
            ovRect.anchorMin = Vector2.zero; ovRect.anchorMax = Vector2.one;
            damageOverlay = overlay.AddComponent<Image>();
            damageOverlay.color = Color.clear;
            damageOverlay.raycastTarget = false;
        }

        private IEnumerator InputBridgeRoutine(Joystick j)
        {
            while (true) {
                if (j != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance != null) {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.SetMovement(j.Direction);
                }
                yield return null;
            }
        }

        private void SetFire(bool s) => TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetFiring(s);

        private Text CreateText(Transform p, string n, string v, Font f, Color c, int s, Vector2 pos)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1); r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(600, 100);
            var t = go.AddComponent<Text>();
            t.font = f; t.fontSize = s; t.color = c; t.text = v; t.fontStyle = FontStyle.Bold;
            return t;
        }

        private void CreateButton(Transform p, string l, Vector2 pos, Color c, System.Action onDown, System.Action onUp = null, bool isBig = false)
        {
            var go = new GameObject(l); go.transform.SetParent(p, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = Vector2.one; r.anchoredPosition = pos;
            float s = isBig ? 260f : 200f;
            r.sizeDelta = new Vector2(s, s);
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

            var border = new GameObject("B"); border.transform.SetParent(go.transform, false);
            var bImg = border.AddComponent<Image>(); bImg.color = c;
            var bRect = border.GetComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
            bRect.offsetMin = new Vector2(-4, -4); bRect.offsetMax = new Vector2(4, 4);
            border.transform.SetAsFirstSibling();

            var txt = new GameObject("T"); txt.transform.SetParent(go.transform, false);
            var t = txt.AddComponent<Text>(); t.text = l; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = goldColor; t.fontSize = isBig ? 45 : 35; t.alignment = TextAnchor.MiddleCenter;
            txt.GetComponent<RectTransform>().sizeDelta = r.sizeDelta;
        }

        public void UpdateHealth(float h) { if (healthText) healthText.text = $"HEALTH: {Mathf.CeilToInt(h)}"; }
        public void UpdateAmmo(int c, int t) { if (ammoText) ammoText.text = $"AMMO: {c} / {t}"; }
        public void TriggerDamage() { if (damageOverlay) StartCoroutine(Flash()); }
        private IEnumerator Flash() { damageOverlay.color = new Color(1, 0, 0, 0.4f); yield return new WaitForSeconds(0.1f); damageOverlay.color = Color.clear; }

        // --- SUB CLASSES ---

        private class LookTouchZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
        {
            private int pointerId = -1;
            public float sensitivity = 1.8f;

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
                // Frame-rate independent velocity as per video guidelines
                Vector2 lookVelocity = (data.delta / Time.deltaTime) * sensitivity * dpiScale * 0.005f;
                
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(lookVelocity);
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
}
