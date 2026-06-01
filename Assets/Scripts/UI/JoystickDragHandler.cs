using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheAlchemistsCrypt.UI
{
    public class JoystickDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform backgroundRing;
        public RectTransform knobVisual;
        public float movementRange = 180f;
        private int trackedPointerId = -1;

        // ── Opacity: 80% idle, 100% when being used ──────────────────────────
        private Image ringImage;
        private Image knobImage;
        private const float IdleAlpha   = 0.8f;
        private const float ActiveAlpha = 1.0f;

        private void Start()
        {
            // Cache Image components
            if (backgroundRing != null) ringImage = backgroundRing.GetComponent<Image>();
            if (knobVisual     != null) knobImage  = knobVisual.GetComponent<Image>();
            // Set idle (80%) opacity immediately
            SetJoystickAlpha(IdleAlpha);
        }

        private void SetJoystickAlpha(float alpha)
        {
            if (ringImage != null)
            {
                var c = ringImage.color; c.a = alpha; ringImage.color = c;
            }
            if (knobImage != null)
            {
                var c = knobImage.color; c.a = alpha; knobImage.color = c;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.IsCustomizingHUD) return;

            if (trackedPointerId == -1)
            {
                trackedPointerId = eventData.pointerId;
                SetJoystickAlpha(ActiveAlpha); // Full opacity while touching
                OnDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.IsCustomizingHUD)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null && backgroundRing != null)
                {
                    Vector2 localPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        backgroundRing.parent as RectTransform,
                        eventData.position,
                        canvas.worldCamera,
                        out localPos
                    );
                    backgroundRing.localPosition = new Vector3(localPos.x, localPos.y, 0f);
                    var finalAnchored = backgroundRing.anchoredPosition;
                    PlayerPrefs.SetFloat("ButtonPos_NativeJoystick_Bg_X", finalAnchored.x);
                    PlayerPrefs.SetFloat("ButtonPos_NativeJoystick_Bg_Y", finalAnchored.y);
                    PlayerPrefs.Save();
                }
                return;
            }

            if (eventData.pointerId != trackedPointerId) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRing, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                float dist = localPoint.magnitude;
                if (dist > movementRange)
                {
                    localPoint = localPoint.normalized * movementRange;
                }
                knobVisual.anchoredPosition = localPoint;

                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.VirtualJoystickInput = localPoint / movementRange;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.IsCustomizingHUD) return;

            if (eventData.pointerId == trackedPointerId)
            {
                trackedPointerId = -1;
                knobVisual.anchoredPosition = Vector2.zero;
                SetJoystickAlpha(IdleAlpha); // Back to 80% opacity
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.VirtualJoystickInput = Vector2.zero;
                }
            }
        }
    }
}
