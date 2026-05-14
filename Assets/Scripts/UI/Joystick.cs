using UnityEngine;
using UnityEngine.EventSystems;
using TheAlchemistsCrypt.Input;

namespace TheAlchemistsCrypt.UI
{
    public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float range = 100f;

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 pos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out pos))
            {
                pos.x = (pos.x / background.sizeDelta.x);
                pos.y = (pos.y / background.sizeDelta.y);

                Vector2 input = new Vector2(pos.x * 2, pos.y * 2);
                input = (input.magnitude > 1.0f) ? input.normalized : input;

                handle.anchoredPosition = new Vector2(input.x * (background.sizeDelta.x / 2), input.y * (background.sizeDelta.y / 2));
                MobileInputManager.Instance.SetMovement(input);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            handle.anchoredPosition = Vector2.zero;
            MobileInputManager.Instance.SetMovement(Vector2.zero);
        }
    }
}
