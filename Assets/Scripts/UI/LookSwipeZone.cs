using UnityEngine;
using UnityEngine.EventSystems;

namespace TheAlchemistsCrypt.UI
{
    public class LookSwipeZone : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public float sensitivity = 0.08f; 
        private int trackedPointerId = -1;
        
        private void Start() => sensitivity = PlayerPrefs.GetFloat("MobileSensitivity", 0.08f);
        
        public void OnPointerDown(PointerEventData data) 
        { 
            if (trackedPointerId == -1) trackedPointerId = data.pointerId; 
        }
        
        public void OnDrag(PointerEventData data) 
        {
            if (data.pointerId != trackedPointerId) return;
            float deviceDpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            Vector2 delta = data.delta * sensitivity * (160f / deviceDpi);
            if (delta.sqrMagnitude > 0.0001f) TheAlchemistsCrypt.Input.MobileInputManager.Instance?.SetLook(delta);
        }
        
        public void OnPointerUp(PointerEventData data) 
        { 
            if (data.pointerId == trackedPointerId) 
            { 
                trackedPointerId = -1; 
                TheAlchemistsCrypt.Input.MobileInputManager.Instance?.ConsumeLook(); 
            } 
        }
    }
}
