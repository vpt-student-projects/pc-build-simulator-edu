using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PCComponent))]
public class PCComponentInfoClickTarget : MonoBehaviour
{
    private PCComponent component;

    private void Awake()
    {
        component = GetComponent<PCComponent>();
    }

    private void OnMouseOver()
    {
        if (component == null)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (ComponentRemovalDialog.Instance != null)
            {
                ComponentRemovalDialog.Instance.RequestRemove(component);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0) && ComponentInfoPopup.Instance != null)
        {
            ComponentInfoPopup.Instance.Show(component);
        }
    }
}
