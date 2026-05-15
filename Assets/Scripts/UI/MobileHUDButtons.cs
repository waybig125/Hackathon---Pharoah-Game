using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        private void Start()
        {
            if (!Application.isMobilePlatform && !Application.isEditor) return;

            var inputManager = FindAnyObjectByType<TheAlchemistsCrypt.Input.MobileInputManager>();
            if (inputManager == null) return;

            var canvasObj = GameObject.Find("MobileHUD");
            if (canvasObj == null) return;

            // Ensure GraphicRaycaster exists so buttons are clickable!
            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Load Font and Icons
            Font medievalFont = Resources.Load<Font>("UI/Fonts/MedievalSharp");
            if (medievalFont == null) medievalFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Load icons (fallback to null if not imported yet)
            Sprite sprintIcon = Resources.Load<Sprite>("UI/Icons/icon_sprint");
            Sprite jumpIcon = Resources.Load<Sprite>("UI/Icons/icon_jump");
            Sprite crouchIcon = Resources.Load<Sprite>("UI/Icons/icon_crouch");
            Sprite swapIcon = Resources.Load<Sprite>("UI/Icons/icon_swap");

            // Procedural background sprite
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            Sprite defaultBg = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

            void CreateHoldButton(string name, Vector2 anchoredPos, Vector2 size, string textStr, Sprite iconSprite, UnityEngine.Events.UnityAction<BaseEventData> onDown, UnityEngine.Events.UnityAction<BaseEventData> onUp)
            {
                var go = new GameObject(name);
                go.transform.SetParent(canvasObj.transform, false);
                
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = size;
                
                var img = go.AddComponent<Image>();
                img.sprite = defaultBg;
                img.color = new Color(0.1f, 0.1f, 0.1f, 0.6f); // Darker background for contrast
                
                // Add Icon if exists, otherwise text
                if (iconSprite != null)
                {
                    var iconGo = new GameObject("Icon");
                    iconGo.transform.SetParent(go.transform, false);
                    var iconRect = iconGo.AddComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0.2f, 0.2f);
                    iconRect.anchorMax = new Vector2(0.8f, 0.8f);
                    iconRect.offsetMin = Vector2.zero; iconRect.offsetMax = Vector2.zero;
                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.sprite = iconSprite;
                    iconImg.color = Color.white;
                }
                else
                {
                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(go.transform, false);
                    var textRect = textGo.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
                    var text = textGo.AddComponent<Text>();
                    text.text = textStr;
                    text.font = medievalFont;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = Color.white;
                    text.fontSize = 45;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 20;
                    text.resizeTextMaxSize = 80;
                }
                
                var trigger = go.AddComponent<EventTrigger>();
                
                // Visual Feedback Callbacks
                var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => {
                    img.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                    rect.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                    onDown?.Invoke(data);
                });
                trigger.triggers.Add(entryDown);
                
                var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => {
                    img.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                    rect.localScale = Vector3.one;
                    onUp?.Invoke(data);
                });
                trigger.triggers.Add(entryUp);
            }

            // Curved Ergonomic Layout for Thumb
            // Jump (Bottom Right, closest to thumb rest)
            CreateHoldButton("JumpButton", new Vector2(-200, 200), new Vector2(140, 140), "JUMP", jumpIcon,
                data => inputManager.SetJumping(true), 
                data => inputManager.SetJumping(false));

            // Sprint (Above Jump)
            CreateHoldButton("SprintButton", new Vector2(-250, 400), new Vector2(120, 120), "SPRINT", sprintIcon,
                data => inputManager.SetSprinting(true), 
                data => inputManager.SetSprinting(false));

            // Crouch (Left of Jump)
            CreateHoldButton("CrouchButton", new Vector2(-400, 250), new Vector2(120, 120), "CROUCH", crouchIcon,
                data => inputManager.SetCrouching(true), 
                data => inputManager.SetCrouching(false));

            // Swap Weapon (Top Right)
            CreateHoldButton("SwapButton", new Vector2(-100, 900), new Vector2(120, 120), "SWAP", swapIcon,
                data => inputManager.SetSwappingWeapon(), 
                null);

            // Invisible Touch Zone for Camera Look (Right half of the screen)
            var touchZoneGo = new GameObject("LookTouchZone");
            touchZoneGo.transform.SetParent(canvasObj.transform, false);
            // Ensure the touch zone is behind the buttons
            touchZoneGo.transform.SetAsFirstSibling();
            
            var touchRect = touchZoneGo.AddComponent<RectTransform>();
            touchRect.anchorMin = new Vector2(0.5f, 0); // Start from middle
            touchRect.anchorMax = new Vector2(1, 1); // Full height, right side
            touchRect.offsetMin = Vector2.zero;
            touchRect.offsetMax = Vector2.zero;

            var touchImg = touchZoneGo.AddComponent<Image>();
            touchImg.color = new Color(0, 0, 0, 0); // Completely transparent
            touchImg.raycastTarget = true;

            var touchTrigger = touchZoneGo.AddComponent<EventTrigger>();

            // Drag event - Increased sensitivity
            var entryDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            entryDrag.callback.AddListener((data) => {
                var pointerData = (PointerEventData)data;
                // Increased multiplier from 2.0f to 10.0f for responsive sensitivity
                inputManager.SetLook(pointerData.delta * 10.0f);
            });
            touchTrigger.triggers.Add(entryDrag);

            // Pointer Up event (reset look to zero when finger lifted)
            var entryUpLook = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUpLook.callback.AddListener((data) => {
                inputManager.SetLook(Vector2.zero);
            });
            touchTrigger.triggers.Add(entryUpLook);
        }
    }
}
