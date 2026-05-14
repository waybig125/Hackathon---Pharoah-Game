using UnityEngine;
using UnityEngine.EventSystems;
using TheAlchemistsCrypt.Input;

namespace TheAlchemistsCrypt.UI
{
    public class TouchZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        private Vector2 lastTouchPos;
        private bool isDragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            lastTouchPos = eventData.position;
            isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDragging)
            {
                Vector2 delta = eventData.position - lastTouchPos;
                MobileInputManager.Instance.SetLook(delta);
                lastTouchPos = eventData.position;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            MobileInputManager.Instance.SetLook(Vector2.zero);
        }

        private void Update()
        {
            // Reset look input if not dragging to prevent continuous spinning
            if (!isDragging)
            {
                MobileInputManager.Instance.SetLook(Vector2.zero);
            }
        }
    }
}
