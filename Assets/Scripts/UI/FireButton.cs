using UnityEngine;
using UnityEngine.EventSystems;
using TheAlchemistsCrypt.Input;

namespace TheAlchemistsCrypt.UI
{
    public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            MobileInputManager.Instance.SetFiring(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            MobileInputManager.Instance.SetFiring(false);
        }
    }
}
