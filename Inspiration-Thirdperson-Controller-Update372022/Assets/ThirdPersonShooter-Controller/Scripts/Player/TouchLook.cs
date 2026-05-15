using UnityEngine;
using UnityEngine.EventSystems;
public class TouchLook : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [HideInInspector]
    public static Vector2 TouchDist;

    public void OnDrag(PointerEventData eventData)
    {
        TouchDist.x = eventData.delta.x;
        TouchDist.y = eventData.delta.y;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        TouchDist = Vector2.zero;
    }
}
