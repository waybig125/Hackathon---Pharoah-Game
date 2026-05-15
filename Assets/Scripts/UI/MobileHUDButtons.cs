using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        private class CustomJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public RectTransform background;
            public RectTransform handle;
            public TheAlchemistsCrypt.Input.MobileInputManager inputManager;
            private float radius;

            private void Awake()
            {
                UpdateRadius();
            }

            private void UpdateRadius()
            {
                if (background != null)
                    radius = background.sizeDelta.x * 0.4f;
                if (radius <= 0) radius = 100f;
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
                
                UpdateRadius();

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(background, data.position, data.pressEventCamera, out localPoint);
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
                handle.anchoredPosition = clamped;
                Vector2 input = clamped / radius;
                
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
                transform.localScale = Vector3.one * 0.85f;
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

        private class LookTouchZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
        {
            public TheAlchemistsCrypt.Input.MobileInputManager inputManager;
            // Sensitivity tuned for CameraLook.cs (which uses 2800)
            // data.delta is in pixels. We want to pass a value that feels like Mouse X/Y.
            public float sensitivity = 0.05f; 

            public void OnPointerDown(PointerEventData data) { }

            public void OnDrag(PointerEventData data)
            {
                if (inputManager != null)
                {
                    // Normalize by screen width to keep sensitivity consistent across devices
                    Vector2 input = new Vector2(data.delta.x / Screen.width, data.delta.y / Screen.height);
                    inputManager.SetLook(input * 1500f * sensitivity);
                }
            }

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
            if (canvasObj != null) DestroyImmediate(canvasObj);

            canvasObj = new GameObject("MobileHUD");
            var cv = canvasObj.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 100;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; 
            canvasObj.AddComponent<GraphicRaycaster>();

            Sprite GetIcon(string name, string fallback = null)
            {
                var s = Resources.Load<Sprite>("UI/Icons/Inspiration/" + name);
                if (s == null && fallback != null) s = Resources.Load<Sprite>("UI/Icons/" + fallback);
                return s;
            }

            Sprite icoJump = GetIcon("jump", "icon_jump");
            Sprite icoAttack = GetIcon("bullet", "icon_attack");
            Sprite icoAim = GetIcon("aim");
            Sprite icoReload = GetIcon("reload", "icon_swap");
            Sprite icoCrouch = GetIcon("Button", "icon_crouch");
            Sprite icoSprint = GetIcon("Button", "icon_sprint");
            
            Sprite joyBg = GetIcon("Joystick");
            Sprite joyKnob = GetIcon("Controller");
            Sprite btnBg = GetIcon("Button");

            // LOOK ZONE (Full Right Side)
            var lookObj = new GameObject("LookZone");
            lookObj.transform.SetParent(canvasObj.transform, false);
            var lookRect = lookObj.AddComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.4f, 0f); 
            lookRect.anchorMax = new Vector2(1f, 1f);
            lookRect.offsetMin = lookRect.offsetMax = Vector2.zero;
            var lookImg = lookObj.AddComponent<Image>();
            lookImg.color = new Color(1,1,1,0.005f); 
            var lookZone = lookObj.AddComponent<LookTouchZone>();
            lookZone.inputManager = inputManager;

            var goldColor = new Color(1.0f, 0.84f, 0.0f, 0.8f); 
            var darkGold = new Color(0.4f, 0.35f, 0.1f, 0.6f);

            // JOYSTICK (Left Side)
            var joyBgObj = new GameObject("JoystickBg");
            joyBgObj.transform.SetParent(canvasObj.transform, false);
            var joyBgRect = joyBgObj.AddComponent<RectTransform>();
            joyBgRect.anchorMin = joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0.5f, 0.5f);
            joyBgRect.anchoredPosition = new Vector2(300f, 300f); 
            joyBgRect.sizeDelta = new Vector2(350f, 350f);
            var joyBgImg = joyBgObj.AddComponent<Image>();
            joyBgImg.sprite = joyBg;
            joyBgImg.color = darkGold;

            var joyKnobObj = new GameObject("JoystickKnob");
            joyKnobObj.transform.SetParent(joyBgObj.transform, false);
            var joyKnobRect = joyKnobObj.AddComponent<RectTransform>();
            joyKnobRect.anchorMin = joyKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
            joyKnobRect.sizeDelta = new Vector2(160f, 160f);
            var joyKnobImg = joyKnobObj.AddComponent<Image>();
            joyKnobImg.sprite = joyKnob;
            joyKnobImg.color = goldColor;

            var joyScript = joyBgObj.AddComponent<CustomJoystick>();
            joyScript.background = joyBgRect;
            joyScript.handle = joyKnobRect;
            joyScript.inputManager = inputManager;

            // ACTION BUTTONS (Right Side)
            float btnSz = 180f;
            float margin = 120f;

            // ATTACK - Largest, bottom center-right
            MakeBtn("AttackBtn", new Vector2(-margin - 250f, margin + 120f), 260f, icoAttack, btnBg, goldColor, canvasObj.transform, () => inputManager.SetFiring(true), () => inputManager.SetFiring(false));

            // AIM - Above Attack
            MakeBtn("AimBtn", new Vector2(-margin - 250f, margin + 420f), 200f, icoAim, btnBg, goldColor, canvasObj.transform, () => inputManager.SetAiming(true), () => inputManager.SetAiming(false));

            // JUMP - Right of Attack
            MakeBtn("JumpBtn", new Vector2(-margin, margin + 300f), 180f, icoJump, btnBg, goldColor, canvasObj.transform, () => inputManager.SetJumping(true), () => inputManager.SetJumping(false));

            // RELOAD/SWAP - Above Aim
            MakeBtn("ReloadBtn", new Vector2(-margin - 480f, margin + 480f), 160f, icoReload, btnBg, goldColor, canvasObj.transform, () => inputManager.SetSwappingWeapon(), null);

            // SPRINT - Left of Attack
            MakeBtn("SprintBtn", new Vector2(-margin - 500f, margin + 250f), 160f, icoSprint, btnBg, goldColor, canvasObj.transform, () => inputManager.SetSprinting(true), () => inputManager.SetSprinting(false));

            // CROUCH - Below Sprint
            MakeBtn("CrouchBtn", new Vector2(-margin - 500f, margin + 80f), 160f, icoCrouch, btnBg, goldColor, canvasObj.transform, () => inputManager.SetCrouching(true), () => inputManager.SetCrouching(false));
        }

        private GameObject MakeBtn(string label, Vector2 pos, float sz, Sprite icon, Sprite bgSprite, Color bgColor, Transform parent, System.Action down, System.Action up)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(1f, 0f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(sz, sz);

            var img = go.AddComponent<Image>();
            img.sprite = bgSprite;
            img.color = bgColor;

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

            var btn = go.AddComponent<HUDButton>();
            btn.background = img;
            btn.normalColor = bgColor;
            btn.onDown = down;
            btn.onUp = up;
            return go;
        }
    }
}
