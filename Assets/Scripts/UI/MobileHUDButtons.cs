using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        private Sprite circleSprite;
        private Sprite joystickBgSprite;
        private Sprite joystickHandleSprite;
        private Font medievalFont;

        private void Start()
        {
            if (!Application.isMobilePlatform && !Application.isEditor) return;

            var inputManager = FindAnyObjectByType<TheAlchemistsCrypt.Input.MobileInputManager>();
            if (inputManager == null) return;

            var canvasObj = GameObject.Find("MobileHUD");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("MobileHUD");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Load Font and Icons
            medievalFont = Resources.Load<Font>("UI/Fonts/MedievalSharp");
            if (medievalFont == null) medievalFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Sprite sprintIcon = Resources.Load<Sprite>("UI/Icons/icon_sprint");
            Sprite jumpIcon = Resources.Load<Sprite>("UI/Icons/icon_jump");
            Sprite crouchIcon = Resources.Load<Sprite>("UI/Icons/icon_crouch");
            Sprite swapIcon = Resources.Load<Sprite>("UI/Icons/icon_swap");
            Sprite attackIcon = Resources.Load<Sprite>("UI/Icons/icon_attack"); // Assuming we might have one or use a fallback

            // Create procedural circle sprite
            circleSprite = CreateCircleSprite(128, new Color(1, 1, 1, 1));
            joystickBgSprite = CreateCircleSprite(256, new Color(0.2f, 0.15f, 0.1f, 0.7f)); // Bronze/Stone color
            joystickHandleSprite = CreateCircleSprite(128, new Color(0.8f, 0.7f, 0.5f, 0.9f)); // Golden color

            void CreateRoundButton(string name, Vector2 anchoredPos, Vector2 size, Sprite iconSprite, string fallbackText, UnityEngine.Events.UnityAction<BaseEventData> onDown, UnityEngine.Events.UnityAction<BaseEventData> onUp, Color bgColor)
            {
                var go = new GameObject(name);
                go.transform.SetParent(canvasObj.transform, false);
                
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = size;
                
                var img = go.AddComponent<Image>();
                img.sprite = circleSprite;
                img.color = bgColor;
                
                if (iconSprite != null)
                {
                    var iconGo = new GameObject("Icon");
                    iconGo.transform.SetParent(go.transform, false);
                    var iconRect = iconGo.AddComponent<RectTransform>();
                    iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one;
                    iconRect.offsetMin = size * 0.2f; iconRect.offsetMax = -size * 0.2f;
                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.sprite = iconSprite;
                    iconImg.color = Color.white;
                    iconImg.raycastTarget = false;
                }
                else
                {
                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(go.transform, false);
                    var textRect = textGo.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
                    var text = textGo.AddComponent<Text>();
                    text.text = fallbackText;
                    text.font = medievalFont;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = Color.white;
                    text.fontSize = (int)(size.x * 0.3f);
                    text.resizeTextForBestFit = true;
                    text.raycastTarget = false;
                }
                
                var trigger = go.AddComponent<EventTrigger>();
                
                var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => {
                    img.color = new Color(bgColor.r * 1.5f, bgColor.g * 1.5f, bgColor.b * 1.5f, 0.9f);
                    rect.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                    onDown?.Invoke(data);
                });
                trigger.triggers.Add(entryDown);
                
                var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => {
                    img.color = bgColor;
                    rect.localScale = Vector3.one;
                    onUp?.Invoke(data);
                });
                trigger.triggers.Add(entryUp);
            }

            // Layout Settings (Curved for thumb)
            float radius = 400f;
            Vector2 center = new Vector2(-150, 150);

            // Jump (Bottom Right)
            CreateRoundButton("JumpButton", center + new Vector2(0, 0), new Vector2(180, 180), jumpIcon, "JUMP",
                data => inputManager.SetJumping(true), 
                data => inputManager.SetJumping(false), new Color(0.2f, 0.2f, 0.2f, 0.7f));

            // Attack / Fire (Repurposed Red Button)
            CreateRoundButton("AttackButton", center + new Vector2(-220, 100), new Vector2(200, 200), attackIcon, "ATTACK",
                data => inputManager.SetFiring(true), 
                data => inputManager.SetFiring(false), new Color(0.6f, 0.1f, 0.1f, 0.8f));

            // Sprint
            CreateRoundButton("SprintButton", center + new Vector2(-100, 250), new Vector2(140, 140), sprintIcon, "RUN",
                data => inputManager.SetSprinting(true), 
                data => inputManager.SetSprinting(false), new Color(0.2f, 0.2f, 0.2f, 0.7f));

            // Crouch
            CreateRoundButton("CrouchButton", center + new Vector2(-300, -50), new Vector2(140, 140), crouchIcon, "CROUCH",
                data => inputManager.SetCrouching(true), 
                data => inputManager.SetCrouching(false), new Color(0.2f, 0.2f, 0.2f, 0.7f));

            // Swap
            CreateRoundButton("SwapButton", new Vector2(-150, 850), new Vector2(130, 130), swapIcon, "SWAP",
                data => inputManager.SetSwappingWeapon(), 
                null, new Color(0.3f, 0.3f, 0.4f, 0.7f));

            // Create Themed Joystick
            CreateJoystick(canvasObj, inputManager);

            // Look Touch Zone (Right Half)
            var touchZoneGo = new GameObject("LookTouchZone");
            touchZoneGo.transform.SetParent(canvasObj.transform, false);
            touchZoneGo.transform.SetAsFirstSibling();
            var touchRect = touchZoneGo.AddComponent<RectTransform>();
            touchRect.anchorMin = new Vector2(0.4f, 0);
            touchRect.anchorMax = new Vector2(1, 1);
            touchRect.offsetMin = Vector2.zero; touchRect.offsetMax = Vector2.zero;
            var touchImg = touchZoneGo.AddComponent<Image>();
            touchImg.color = Color.clear;
            var touchTrigger = touchZoneGo.AddComponent<EventTrigger>();
            var entryDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            entryDrag.callback.AddListener((data) => {
                inputManager.SetLook(((PointerEventData)data).delta * 20.0f);
            });
            touchTrigger.triggers.Add(entryDrag);
            var entryUpLook = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUpLook.callback.AddListener((data) => inputManager.SetLook(Vector2.zero));
            touchTrigger.triggers.Add(entryUpLook);
        }

        private void CreateJoystick(GameObject canvas, TheAlchemistsCrypt.Input.MobileInputManager inputManager)
        {
            var joyGo = new GameObject("ThemedJoystick");
            joyGo.transform.SetParent(canvas.transform, false);
            var rect = joyGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(250, 250);
            rect.sizeDelta = new Vector2(300, 300);

            var bgImg = joyGo.AddComponent<Image>();
            bgImg.sprite = joystickBgSprite;

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(joyGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(120, 120);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = joystickHandleSprite;

            var joystick = joyGo.AddComponent<Joystick>();
            joystick.background = rect;
            joystick.handle = handleRect;
        }

        private Sprite CreateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size);
            float center = size / 2f;
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        float alpha = Mathf.SmoothStep(radius, radius - 2, dist);
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
