using UnityEngine;
using UnityEngine.EventSystems;

public class MenuCardInfoHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PCComponent component;

    public void Initialize(PCComponent source)
    {
        component = source;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (component == null || ComponentInfoPopup.Instance == null)
        {
            return;
        }

        ComponentInfoPopup.Instance.Show(component);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Do not hide immediately: popup closes by its own timer in ComponentInfoPopup.
    }
}
