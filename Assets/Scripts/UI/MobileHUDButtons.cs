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
                
                radius = background.sizeDelta.x * 0.45f;
                if (radius <= 0) radius = 100f; 

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
            if (canvasObj != null)
            {
                DestroyImmediate(canvasObj);
            }

            canvasObj = new GameObject("MobileHUD");
            var cv = canvasObj.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 100;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; 
            canvasObj.AddComponent<GraphicRaycaster>();

            // Robust Icon Loading - Prefer Inspiration Assets
            Sprite GetIcon(string name, string fallback = null)
            {
                // Try Inspiration folder first
                var s = Resources.Load<Sprite>("UI/Icons/Inspiration/" + name);
                if (s == null && fallback != null)
                {
                    s = Resources.Load<Sprite>("UI/Icons/" + fallback);
                }
                
                if (s == null)
                {
                    // Try Texture2D fallback
                    var t = Resources.Load<Texture2D>("UI/Icons/Inspiration/" + name);
                    if (t == null && fallback != null) t = Resources.Load<Texture2D>("UI/Icons/" + fallback);
                    if (t != null) s = Sprite.Create(t, new Rect(0,0,t.width,t.height), new Vector2(0.5f,0.5f));
                }
                return s;
            }

            // Load Sprites
            Sprite icoJump = GetIcon("jump", "icon_jump");
            Sprite icoAttack = GetIcon("bullet", "icon_attack");
            Sprite icoAim = GetIcon("aim");
            Sprite icoReload = GetIcon("reload", "icon_swap");
            Sprite icoCrouch = GetIcon("Button", "icon_crouch");
            Sprite icoSprint = GetIcon("Button", "icon_sprint");
            
            Sprite joyBg = GetIcon("Joystick");
            Sprite joyKnob = GetIcon("Controller");
            Sprite btnBg = GetIcon("Button");

            // Look Zone
            var lookObj = new GameObject("LookZone");
            lookObj.transform.SetParent(canvasObj.transform, false);
            var lookRect = lookObj.AddComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.3f, 0f); 
            lookRect.anchorMax = new Vector2(1f, 1f);
            lookRect.offsetMin = lookRect.offsetMax = Vector2.zero;
            var lookImg = lookObj.AddComponent<Image>();
            lookImg.color = new Color(1,1,1,0.01f); 
            var lookZone = lookObj.AddComponent<LookTouchZone>();
            lookZone.inputManager = inputManager;

            // Colors
            var goldColor = new Color(1.0f, 0.84f, 0.0f, 0.85f); // Pharaoh Gold
            var darkGold = new Color(0.6f, 0.5f, 0.1f, 0.75f);

            // Joystick
            var joyBgObj = new GameObject("JoystickBg");
            joyBgObj.transform.SetParent(canvasObj.transform, false);
            var joyBgRect = joyBgObj.AddComponent<RectTransform>();
            joyBgRect.anchorMin = joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0f, 0f);
            joyBgRect.anchoredPosition = new Vector2(150f, 150f); 
            joyBgRect.sizeDelta = new Vector2(350f, 350f);
            var joyBgImg = joyBgObj.AddComponent<Image>();
            joyBgImg.sprite = joyBg;
            joyBgImg.color = darkGold;

            var joyKnobObj = new GameObject("JoystickKnob");
            joyKnobObj.transform.SetParent(joyBgObj.transform, false);
            var joyKnobRect = joyKnobObj.AddComponent<RectTransform>();
            joyKnobRect.anchorMin = joyKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
            joyKnobRect.sizeDelta = new Vector2(150f, 150f);
            var joyKnobImg = joyKnobObj.AddComponent<Image>();
            joyKnobImg.sprite = joyKnob;
            joyKnobImg.color = goldColor;

            var joyScript = joyBgObj.AddComponent<CustomJoystick>();
            joyScript.background = joyBgRect;
            joyScript.handle = joyKnobRect;
            joyScript.inputManager = inputManager;

            // BUTTON CLUSTER
            float btnSz = 180f;
            float margin = 100f;

            // JUMP - Top Right of Cluster
            MakeBtn("JumpBtn", new Vector2(-margin - btnSz * 0.5f, margin + btnSz * 1.5f), btnSz, icoJump, btnBg, goldColor, canvasObj.transform, () => inputManager.SetJumping(true), () => inputManager.SetJumping(false));
            
            // ATTACK - Main Action (Bottom Center of Cluster)
            MakeBtn("AttackBtn", new Vector2(-margin - btnSz * 1.5f - 40f, margin + btnSz * 0.5f), btnSz + 60f, icoAttack, btnBg, goldColor, canvasObj.transform, () => inputManager.SetFiring(true), () => inputManager.SetFiring(false));

            // AIM - Top Left of Cluster
            MakeBtn("AimBtn", new Vector2(-margin - btnSz * 2.5f - 80f, margin + btnSz * 1.5f), btnSz, icoAim, btnBg, goldColor, canvasObj.transform, () => inputManager.SetAiming(true), () => inputManager.SetAiming(false));

            // RELOAD/SWAP - Top Center
            MakeBtn("ReloadBtn", new Vector2(-margin - btnSz * 1.5f - 40f, margin + btnSz * 1.5f + 40f), btnSz, icoReload, btnBg, goldColor, canvasObj.transform, () => inputManager.SetSwappingWeapon(), null);

            // SPRINT - Bottom Left
            MakeBtn("SprintBtn", new Vector2(-margin - btnSz * 2.5f - 80f, margin + btnSz * 0.5f), btnSz, icoSprint, btnBg, goldColor, canvasObj.transform, () => inputManager.SetSprinting(true), () => inputManager.SetSprinting(false));

            // CROUCH - Bottom Right
            MakeBtn("CrouchBtn", new Vector2(-margin - btnSz * 0.5f, margin + btnSz * 0.5f), btnSz, icoCrouch, btnBg, goldColor, canvasObj.transform, () => inputManager.SetCrouching(true), () => inputManager.SetCrouching(false));
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
                ir.offsetMin = new Vector2(sz * 0.2f, sz * 0.2f);
                ir.offsetMax = new Vector2(-sz * 0.2f, -sz * 0.2f);
                var iimg = iconGo.AddComponent<Image>();
                iimg.sprite = icon;
                iimg.color = Color.white;
                iimg.raycastTarget = false;
            }
            else
            {
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
            btn.normalColor = bgColor;
            btn.onDown = down;
            btn.onUp = up;
            return go;
        }
    }
}
