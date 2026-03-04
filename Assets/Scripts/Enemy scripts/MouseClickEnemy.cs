using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MouseClickEnemy : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent ClickEvent;
    
    void IPointerClickHandler.OnPointerClick(PointerEventData clickData)
    {
        Debug.Log(name + " Game Object Clicked!");
        
        ClickEvent.Invoke();
    }
}
