using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
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

        private class CustomJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public RectTransform background;
            public RectTransform handle;
            public TheAlchemistsCrypt.Input.MobileInputManager inputManager;
            private float radius;

            private void Awake()
            {
                if (background != null)
                    radius = background.sizeDelta.x * 0.45f;
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
                
                // Ensure radius is always valid even if screen size changed
                radius = background.sizeDelta.x * 0.45f;
                if (radius <= 0) radius = 100f; // Safety fallback

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(background, data.position, data.pressEventCamera, out localPoint);
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
                handle.anchoredPosition = clamped;
                Vector2 input = clamped / radius;
                
                // Final safety check for NaN
                if (float.IsNaN(input.x) || float.IsNaN(input.y)) input = Vector2.zero;
                
                inputManager?.SetMovement(input);
            }
        }

        private class HUDButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Image background;
            public Color normalColor;
            public Color pressedColor = new Color(1f, 0.9f, 0.4f, 1f);
            public System.Action onDown;
            public System.Action onUp;
            private bool held;

            public void OnPointerDown(PointerEventData data)
            {
                held = true;
                if (background) background.color = pressedColor;
                transform.localScale = Vector3.one * 0.9f;
                onDown?.Invoke();
            }

            public void OnPointerUp(PointerEventData data)
            {
                held = false;
                if (background) background.color = normalColor;
                transform.localScale = Vector3.one;
                onUp?.Invoke();
            }

            private void Update()
            {
                if (background == null) return;
                background.color = Color.Lerp(background.color, held ? pressedColor : normalColor, Time.deltaTime * 15f);
            }
        }

        private class LookTouchZone : MonoBehaviour, IDragHandler, IPointerUpHandler
        {
            public TheAlchemistsCrypt.Input.MobileInputManager inputManager;
            // INCREASED SENSITIVITY from 0.25 to 1.0
            public float sensitivity = 1.0f;

            public void OnDrag(PointerEventData data)
                => inputManager?.SetLook(data.delta * sensitivity);

            public void OnPointerUp(PointerEventData data)
                => inputManager?.SetLook(Vector2.zero);
        }

        private void Start()
        {
            BuildHUD();
        }

        private void BuildHUD()
        {
            var inputManager = FindAnyObjectByType<TheAlchemistsCrypt.Input.MobileInputManager>();
            if (inputManager == null) return;

            GameObject canvasObj = GameObject.Find("MobileHUD");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("MobileHUD");
                var cv = canvasObj.AddComponent<Canvas>();
                cv.renderMode = RenderMode.ScreenSpaceOverlay;
                cv.sortingOrder = 100;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f; // Height matching prevents stretching on ultrawide phones
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Robust Cleanup - use DestroyImmediate to prevent frame-lag stray buttons
            var toDestroy = new List<GameObject>();
            foreach (Transform child in canvasObj.transform) toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy) DestroyImmediate(go);

            var circleSprite = CreateCircleSprite(256, Color.white);

            // Robust Icon Loading
            Sprite GetIcon(string name)
            {
                var s = Resources.Load<Sprite>("UI/Icons/" + name);
                if (s == null)
                {
                    // Try Texture2D fallback
                    var t = Resources.Load<Texture2D>("UI/Icons/" + name);
                    if (t != null) s = Sprite.Create(t, new Rect(0,0,t.width,t.height), new Vector2(0.5f,0.5f));
                }
                return s;
            }

            Sprite icoJump = GetIcon("icon_jump");
            Sprite icoAttack = GetIcon("icon_attack");
            Sprite icoSprint = GetIcon("icon_sprint");
            Sprite icoCrouch = GetIcon("icon_crouch");
            Sprite icoSwap = GetIcon("icon_swap");

            // Look Zone
            var lookObj = new GameObject("LookZone");
            lookObj.transform.SetParent(canvasObj.transform, false);
            var lookRect = lookObj.AddComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.3f, 0f); // Larger zone
            lookRect.anchorMax = new Vector2(1f, 1f);
            lookRect.offsetMin = lookRect.offsetMax = Vector2.zero;
            var lookImg = lookObj.AddComponent<Image>();
            lookImg.color = new Color(1,1,1,0.01f); // Almost invisible but raycastable
            var lookZone = lookObj.AddComponent<LookTouchZone>();
            lookZone.inputManager = inputManager;

            // Joystick
            var joyBgObj = new GameObject("JoystickBg");
            joyBgObj.transform.SetParent(canvasObj.transform, false);
            var joyBgRect = joyBgObj.AddComponent<RectTransform>();
            joyBgRect.anchorMin = joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0f, 0f);
            joyBgRect.anchoredPosition = new Vector2(180f, 400f); // MOVED EVEN HIGHER to prevent bottom-edge overlap on curved screens
            joyBgRect.sizeDelta = new Vector2(300f, 300f);
            var joyBgImg = joyBgObj.AddComponent<Image>();
            joyBgImg.sprite = circleSprite;
            joyBgImg.color = new Color(0, 0, 0, 0.4f);

            var joyKnobObj = new GameObject("JoystickKnob");
            joyKnobObj.transform.SetParent(joyBgObj.transform, false);
            var joyKnobRect = joyKnobObj.AddComponent<RectTransform>();
            joyKnobRect.anchorMin = joyKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
            joyKnobRect.sizeDelta = new Vector2(120f, 120f);
            var joyKnobImg = joyKnobObj.AddComponent<Image>();
            joyKnobImg.sprite = circleSprite;
            joyKnobImg.color = new Color(1, 0.9f, 0.2f, 0.9f);

            var joyScript = joyBgObj.AddComponent<CustomJoystick>();
            joyScript.background = joyBgRect;
            joyScript.handle = joyKnobRect;
            joyScript.inputManager = inputManager;

            // BUTTON CLUSTER - EGYPTIAN THEME - MORE VIBRANT
            float btnSz = 170f;
            float margin = 90f;
            var themeColor = new Color(0.35f, 0.25f, 0.15f, 0.85f); // Richer Stone/Dark Brown
            var accentColor = new Color(1.0f, 0.84f, 0.0f, 0.95f); // TRUE GOLD (Vibrant)
            var attackColor = new Color(1.0f, 0.84f, 0.0f, 0.95f); // GOLD instead of Red

            // JUMP - Far Right
            MakeBtn("JumpBtn", new Vector2(-margin - btnSz * 0.5f, margin + btnSz * 1.5f), btnSz, icoJump, themeColor, canvasObj.transform, () => inputManager.SetJumping(true), () => inputManager.SetJumping(false));
            
            // ATTACK - Main Action
            MakeBtn("AttackBtn", new Vector2(-margin - btnSz * 1.5f - 40f, margin + btnSz * 0.5f), btnSz + 40f, icoAttack, attackColor, canvasObj.transform, () => inputManager.SetFiring(true), () => inputManager.SetFiring(false));

            // SPRINT - Left of Attack
            MakeBtn("SprintBtn", new Vector2(-margin - btnSz * 2.5f - 80f, margin + btnSz * 0.5f), btnSz, icoSprint, themeColor, canvasObj.transform, () => inputManager.SetSprinting(true), () => inputManager.SetSprinting(false));

            // CROUCH - Below Jump
            MakeBtn("CrouchBtn", new Vector2(-margin - btnSz * 0.5f, margin + btnSz * 0.5f), btnSz, icoCrouch, themeColor, canvasObj.transform, () => inputManager.SetCrouching(true), () => inputManager.SetCrouching(false));

            // SWAP - Top Right
            var swapBtn = MakeBtn("SwapBtn", new Vector2(-margin - 60f, -margin - 60f), 120f, icoSwap, themeColor, canvasObj.transform, () => inputManager.SetSwappingWeapon(), null);
            var swapRect = swapBtn.GetComponent<RectTransform>();
            swapRect.anchorMin = swapRect.anchorMax = new Vector2(1f, 1f);
        }

        private GameObject MakeBtn(string label, Vector2 pos, float sz, Sprite icon, Color bg, Transform parent, System.Action down, System.Action up)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(1f, 0f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(sz, sz);

            var img = go.AddComponent<Image>();
            img.sprite = CreateCircleSprite(128, Color.white);
            img.color = bg;

            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var ir = iconGo.AddComponent<RectTransform>();
                ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
                ir.offsetMin = new Vector2(sz * 0.25f, sz * 0.25f);
                ir.offsetMax = new Vector2(-sz * 0.25f, -sz * 0.25f);
                var iimg = iconGo.AddComponent<Image>();
                iimg.sprite = icon;
                iimg.color = Color.white;
                iimg.raycastTarget = false;
            }
            else
            {
                // Fallback text if icon fails
                var txtGo = new GameObject("Label");
                txtGo.transform.SetParent(go.transform, false);
                var t = txtGo.AddComponent<Text>();
                t.text = label.Replace("Btn", "");
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.alignment = TextAnchor.MiddleCenter;
                t.resizeTextForBestFit = true;
                t.color = Color.white;
            }

            var btn = go.AddComponent<HUDButton>();
            btn.background = img;
            btn.normalColor = bg;
            btn.onDown = down;
            btn.onUp = up;
            return go;
        }
    }
}
