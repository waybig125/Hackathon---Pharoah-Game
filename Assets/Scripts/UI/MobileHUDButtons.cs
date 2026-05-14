using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TheAlchemistsCrypt.UI
{
    public class MobileHUDButtons : MonoBehaviour
    {
        private void Start()
        {
            // Only create these buttons on mobile or editor
            if (!Application.isMobilePlatform && !Application.isEditor) return;

            var inputManager = FindFirstObjectByType<TheAlchemistsCrypt.Input.MobileInputManager>();
            if (inputManager == null) return;

            var canvasObj = GameObject.Find("MobileHUD");
            if (canvasObj == null) return;

            // Use the standard Knob sprite
            var knobSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            void CreateHoldButton(string name, Vector2 anchoredPos, Vector2 size, string textStr, UnityEngine.Events.UnityAction<BaseEventData> onDown, UnityEngine.Events.UnityAction<BaseEventData> onUp)
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
                img.sprite = knobSprite;
                img.color = new Color(0, 0, 0, 0.5f);
                
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
                var text = textGo.AddComponent<Text>();
                text.text = textStr;
                text.font = font;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.fontSize = 24;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 10;
                text.resizeTextMaxSize = 30;
                
                var trigger = go.AddComponent<EventTrigger>();
                
                var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener(onDown);
                trigger.triggers.Add(entryDown);
                
                if (onUp != null) {
                    var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                    entryUp.callback.AddListener(onUp);
                    trigger.triggers.Add(entryUp);
                }
            }

            // Sprint (Bottom Right, far left of group)
            CreateHoldButton("SprintButton", new Vector2(-600, 100), new Vector2(120, 120), "SPRINT", 
                data => inputManager.SetSprinting(true), 
                data => inputManager.SetSprinting(false));

            // Jump (Bottom Right, middle)
            CreateHoldButton("JumpButton", new Vector2(-450, 100), new Vector2(120, 120), "JUMP", 
                data => inputManager.SetJumping(true), 
                data => inputManager.SetJumping(false));

            // Crouch (Bottom Right, far right)
            CreateHoldButton("CrouchButton", new Vector2(-300, 100), new Vector2(120, 120), "CROUCH", 
                data => inputManager.SetCrouching(true), 
                data => inputManager.SetCrouching(false));

            // Swap Weapon (Top Right)
            CreateHoldButton("SwapButton", new Vector2(-100, 900), new Vector2(120, 120), "SWAP", 
                data => inputManager.SetSwappingWeapon(), 
                null);
        }
    }
}
