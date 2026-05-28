using UnityEngine;
using UnityEngine.EventSystems;

namespace TheAlchemistsCrypt.UI
{
    public class JoystickDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform backgroundRing;
        public RectTransform knobVisual;
        public float movementRange = 180f;
        private int trackedPointerId = -1;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (trackedPointerId == -1)
            {
                trackedPointerId = eventData.pointerId;
                OnDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
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
            if (eventData.pointerId == trackedPointerId)
            {
                trackedPointerId = -1;
                knobVisual.anchoredPosition = Vector2.zero;
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.VirtualJoystickInput = Vector2.zero;
                }
            }
        }
    }
}
