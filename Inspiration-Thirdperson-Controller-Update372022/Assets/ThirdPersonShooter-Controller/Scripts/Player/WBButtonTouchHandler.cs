using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.CrossPlatformInput;

public class WBButtonTouchHandler : MonoBehaviour,IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private GameManager _gameManager;
    private ButtonHandler _handler;

    private bool _isPressed = false;

    private void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        _handler = GetComponent<ButtonHandler>();
    }

    private void Update()
    {
        if (_handler == null)
            return;

        if (!_gameManager.mobileControl)
            return;

        if (CrossPlatformInputManager.GetButtonDown(_handler.Name)) 
        {
            _isPressed = true;
        }
        else if (CrossPlatformInputManager.GetButtonUp(_handler.Name)) 
        {
            _isPressed = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isPressed)
            return;

        TouchLook.TouchDist.x = eventData.delta.x;
        TouchLook.TouchDist.y = eventData.delta.y;
    }

    public void OnPointerDown(PointerEventData eventData)
    {     
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressed)
            return;
        TouchLook.TouchDist = Vector2.zero;
    }
}
