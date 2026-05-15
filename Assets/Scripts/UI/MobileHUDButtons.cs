using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.UI
{
    /// <summary>
    /// Mobile HUD: Custom joystick, icon buttons with press feedback, look touch zone.
    /// Cleans up pre-existing HUD children before rebuilding.
    /// </summary>
    public class MobileHUDButtons : MonoBehaviour
    {
        // ── Procedural circle sprite ─────────────────────────────────────────────
        private static Sprite CreateCircleSprite(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float c = size * 0.5f, r = c - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float a = Mathf.Clamp01((r - d) / 1.5f);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // ── Custom Joystick ──────────────────────────────────────────────────────
        private class CustomJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public RectTransform background;
            public RectTransform handle;
            public TheAlchemistsCrypt.Input.MobileInputManager inputManager;
            private float radius;
            private Canvas canvas;

            private void Awake()
            {
                canvas = GetComponentInParent<Canvas>();
                if (background != null)
                    radius = background.sizeDelta.x * 0.5f;
            }

            public void OnPointerDown(PointerEventData data)  => UpdateJoystick(data);
            public void OnDrag(PointerEventData data)          => UpdateJoystick(data);
            public void OnPointerUp(PointerEventData data)
            {
                if (handle != null) handle.anchoredPosition = Vector2.zero;
                inputManager?.SetMovement(Vector2.zero);
            }

            private void UpdateJoystick(PointerEventData data)
            {
                if (background == null || handle == null) return;

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background, data.position, data.pressEventCamera, out localPoint);

                // Clamp within radius
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
                handle.anchoredPosition = clamped;

                // Normalise to [-1, 1]
                Vector2 input = clamped / radius;
                inputManager?.SetMovement(new Vector2(input.x, input.y));
            }
        }

        // ── Button with icon + active-state feedback ─────────────────────────────
        private class HUDButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Image background;
            public Color normalColor;
            public Color pressedColor = new Color(1f, 0.85f, 0.3f, 0.95f);   // golden glow when pressed
            public System.Action onDown;
            public System.Action onUp;
            private bool held;

            public void OnPointerDown(PointerEventData data)
            {
                held = true;
                if (background) background.color = pressedColor;
                transform.localScale = new Vector3(0.88f, 0.88f, 0.88f);
                onDown?.Invoke();
            }

            public void OnPointerUp(PointerEventData data)
            {
                held = false;
                if (background) background.color = normalColor;
                transform.localScale = Vector3.one;
                onUp?.Invoke();
            }

            // Keep visual in sync while dragging off
            private void Update()
            {
                if (background == null) return;
                background.color = Color.Lerp(background.color,
                    held ? pressedColor : normalColor, Time.deltaTime * 12f);
            }
        }

        // ── Look Touch Zone (right half of screen) ───────────────────────────────
        private class LookTouchZone : MonoBehaviour, IDragHandler, IPointerUpHandler
        {
            public TheAlchemistsCrypt.Input.MobileInputManager inputManager;
            private const float SENSITIVITY = 0.08f;

            public void OnDrag(PointerEventData data)
                => inputManager?.SetLook(data.delta * SENSITIVITY);

            public void OnPointerUp(PointerEventData data)
                => inputManager?.SetLook(Vector2.zero);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void Start()
        {
            // Build HUD even in editor so we can preview
            BuildHUD();
        }

        private void BuildHUD()
        {
            var inputManager = FindAnyObjectByType<TheAlchemistsCrypt.Input.MobileInputManager>();
            if (inputManager == null) return;

            // ── Find/Create the Canvas ──
            GameObject canvasObj = GameObject.Find("MobileHUD");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("MobileHUD");
                var cv = canvasObj.AddComponent<Canvas>();
                cv.renderMode = RenderMode.ScreenSpaceOverlay;
                cv.sortingOrder = 10;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // ── Clean up ALL old children (removes stray red/black buttons) ──
            var toDestroy = new List<GameObject>();
            foreach (Transform child in canvasObj.transform)
                toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy)
                Destroy(go);

            // ── Sprites ──
            var circleSprite  = CreateCircleSprite(256, Color.white);
            var joyBgSprite   = CreateCircleSprite(256, new Color(0.05f, 0.05f, 0.05f, 0.65f));
            var joyKnobSprite = CreateCircleSprite(256, new Color(0.85f, 0.75f, 0.45f, 0.95f));

            // ── Load icons (Resources/UI/Icons/) ──
            Sprite icoJump   = Resources.Load<Sprite>("UI/Icons/icon_jump");
            Sprite icoAttack = Resources.Load<Sprite>("UI/Icons/icon_attack");
            Sprite icoSprint = Resources.Load<Sprite>("UI/Icons/icon_sprint");
            Sprite icoCrouch = Resources.Load<Sprite>("UI/Icons/icon_crouch");
            Sprite icoSwap   = Resources.Load<Sprite>("UI/Icons/icon_swap");

            // ─────────────────────────────────────────────────────────────────────
            // LOOK TOUCH ZONE — right 55 % of screen, behind everything else
            // ─────────────────────────────────────────────────────────────────────
            var lookObj = new GameObject("LookZone");
            lookObj.transform.SetParent(canvasObj.transform, false);
            lookObj.transform.SetAsFirstSibling();
            var lookRect = lookObj.AddComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.4f, 0f);
            lookRect.anchorMax = new Vector2(1f,   1f);
            lookRect.offsetMin = lookRect.offsetMax = Vector2.zero;
            var lookImg = lookObj.AddComponent<Image>();
            lookImg.color = Color.clear;
            var lookZone = lookObj.AddComponent<LookTouchZone>();
            lookZone.inputManager = inputManager;

            // ─────────────────────────────────────────────────────────────────────
            // JOYSTICK — bottom-left
            // ─────────────────────────────────────────────────────────────────────
            var joyBgObj = new GameObject("JoystickBg");
            joyBgObj.transform.SetParent(canvasObj.transform, false);
            var joyBgRect = joyBgObj.AddComponent<RectTransform>();
            joyBgRect.anchorMin = joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0f, 0f);
            joyBgRect.anchoredPosition = new Vector2(60f, 60f);
            joyBgRect.sizeDelta = new Vector2(260f, 260f);
            var joyBgImg = joyBgObj.AddComponent<Image>();
            joyBgImg.sprite = joyBgSprite;
            joyBgImg.color = new Color(0.06f, 0.06f, 0.06f, 0.7f);

            var joyKnobObj = new GameObject("JoystickKnob");
            joyKnobObj.transform.SetParent(joyBgObj.transform, false);
            var joyKnobRect = joyKnobObj.AddComponent<RectTransform>();
            joyKnobRect.anchorMin = joyKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
            joyKnobRect.pivot = new Vector2(0.5f, 0.5f);
            joyKnobRect.anchoredPosition = Vector2.zero;
            joyKnobRect.sizeDelta = new Vector2(110f, 110f);
            var joyKnobImg = joyKnobObj.AddComponent<Image>();
            joyKnobImg.sprite = joyKnobSprite;
            joyKnobImg.color = new Color(0.9f, 0.8f, 0.5f, 0.95f);

            var joyScript = joyBgObj.AddComponent<CustomJoystick>();
            joyScript.background    = joyBgRect;
            joyScript.handle        = joyKnobRect;
            joyScript.inputManager  = inputManager;

            // ─────────────────────────────────────────────────────────────────────
            // ACTION BUTTONS — bottom-right cluster (D-pad-like)
            //   Layout (screen-space, anchored bottom-right):
            //       [SPRINT]
            //  [CROUCH] [JUMP]
            //      [ATTACK]
            // ─────────────────────────────────────────────────────────────────────
            float btnSize   = 140f;
            float btnGap    = 20f;
            float baseX     = -80f;   // offset from right edge
            float baseY     =  80f;   // offset from bottom

            // Helper: creates one round icon button anchored bottom-right
            HUDButton MakeBtn(string label, Vector2 pos, float sz, Sprite icon,
                              Color bg, System.Action down, System.Action up)
            {
                var go = new GameObject(label);
                go.transform.SetParent(canvasObj.transform, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(1f, 0f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = pos;
                r.sizeDelta = new Vector2(sz, sz);

                var img = go.AddComponent<Image>();
                img.sprite = circleSprite;
                img.color  = bg;

                // Icon child
                if (icon != null)
                {
                    var iconGo = new GameObject("Icon");
                    iconGo.transform.SetParent(go.transform, false);
                    var ir = iconGo.AddComponent<RectTransform>();
                    ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
                    ir.offsetMin = new Vector2(sz * 0.2f, sz * 0.2f);
                    ir.offsetMax = new Vector2(-sz * 0.2f, -sz * 0.2f);
                    var iimg = iconGo.AddComponent<Image>();
                    iimg.sprite = icon;
                    iimg.color = Color.white;
                    iimg.raycastTarget = false;
                }

                var btn = go.AddComponent<HUDButton>();
                btn.background   = img;
                btn.normalColor  = bg;
                btn.onDown = down;
                btn.onUp   = up;
                return btn;
            }

            // Stone/gold color theme
            var stoneColor  = new Color(0.18f, 0.15f, 0.12f, 0.80f);
            var attackColor = new Color(0.65f, 0.15f, 0.05f, 0.85f);

            // JUMP — right of center
            MakeBtn("JumpBtn",
                new Vector2(baseX, baseY + btnSize * 0.5f + btnGap * 0.5f),
                btnSize, icoJump, stoneColor,
                () => inputManager.SetJumping(true),
                () => inputManager.SetJumping(false));

            // ATTACK — center (big)
            MakeBtn("AttackBtn",
                new Vector2(baseX - btnSize - btnGap, baseY + btnSize * 0.5f + btnGap * 0.5f),
                btnSize + 20f, icoAttack, attackColor,
                () => inputManager.SetFiring(true),
                () => inputManager.SetFiring(false));

            // SPRINT — above attack
            MakeBtn("SprintBtn",
                new Vector2(baseX - btnSize - btnGap, baseY + btnSize * 1.5f + btnGap * 1.5f),
                btnSize - 20f, icoSprint, stoneColor,
                () => inputManager.SetSprinting(true),
                () => inputManager.SetSprinting(false));

            // CROUCH — below attack
            MakeBtn("CrouchBtn",
                new Vector2(baseX - btnSize - btnGap, baseY - btnSize * 0.5f + btnGap * 0.5f),
                btnSize - 20f, icoCrouch, stoneColor,
                () => inputManager.SetCrouching(true),
                () => inputManager.SetCrouching(false));

            // SWAP — top-right corner
            MakeBtn("SwapBtn",
                new Vector2(-60f, 0f),   // anchored top-right via different anchor
                100f, icoSwap, new Color(0.25f, 0.2f, 0.35f, 0.8f),
                () => inputManager.SetSwappingWeapon(),
                null);

            // Fix SWAP anchor to top-right
            var swapRect = canvasObj.transform.Find("SwapBtn")?.GetComponent<RectTransform>();
            if (swapRect != null)
            {
                swapRect.anchorMin = swapRect.anchorMax = new Vector2(1f, 1f);
                swapRect.anchoredPosition = new Vector2(-80f, -80f);
            }
        }
    }
}
