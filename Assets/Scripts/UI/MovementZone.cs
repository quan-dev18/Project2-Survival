using UnityEngine;
using UnityEngine.EventSystems;

public class MovementZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private JoystickController joystick;

    private void Awake()
    {
        if(joystick == null)
            joystick = GetComponent<JoystickController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (joystick != null)
            joystick.ShowAt(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (joystick != null)
            joystick.MoveHandle(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (joystick != null)
            joystick.Hide();
    }
}