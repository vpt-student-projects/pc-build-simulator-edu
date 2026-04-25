using UnityEngine;

[RequireComponent(typeof(PCComponent))]
public class PCComponentInfoClickTarget : MonoBehaviour
{
    private PCComponent component;

    private void Awake()
    {
        component = GetComponent<PCComponent>();
    }

    private void OnMouseDown()
    {
        if (component == null || ComponentInfoPopup.Instance == null)
        {
            return;
        }

        ComponentInfoPopup.Instance.Show(component);
    }
}
