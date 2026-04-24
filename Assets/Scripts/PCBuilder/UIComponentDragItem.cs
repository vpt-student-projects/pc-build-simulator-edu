using EasyBuildSystem.Features.Runtime.Buildings.Part;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIComponentDragItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private BuildModeDragController dragController;
    [SerializeField] private PCComponent componentData;
    [SerializeField] private BuildingPart buildingPartPrefab;

    public void InitializeFromMenu(BuildModeDragController controller, PCComponent component, BuildingPart partPrefab)
    {
        dragController = controller;
        componentData = component;
        buildingPartPrefab = partPrefab;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (dragController == null)
        {
            dragController = FindFirstObjectByType<BuildModeDragController>();
        }

        if (dragController == null || componentData == null || buildingPartPrefab == null)
        {
            return;
        }

        dragController.BeginDrag(componentData, buildingPartPrefab);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (dragController == null)
        {
            return;
        }

        dragController.EndDrag();
    }

    private void OnDisable()
    {
        // Card gets disabled when menu closes; do not cancel drag here.
    }
}
