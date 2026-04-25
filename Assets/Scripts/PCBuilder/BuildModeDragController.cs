using EasyBuildSystem.Features.Runtime.Buildings.Part;
using EasyBuildSystem.Features.Runtime.Buildings.Placer;
using EasyBuildSystem.Features.Runtime.Buildings.Placer.InputHandler;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;
using UnityEngine;
using TMPro;
using System.Collections;

public class BuildModeDragController : MonoBehaviour
{
    [SerializeField] private BuildingPlacer buildingPlacer;
    [SerializeField] private LayerMask slotLayerMask = ~0;
    [SerializeField] private float slotScanRadius = 5f;
    [SerializeField] private BuildMenuController menuController;
    [SerializeField] private Demo_FirstPersonCamera firstPersonCamera;
    [SerializeField] private Demo_FirstPersonController firstPersonController;
    [SerializeField] private bool disableDefaultPlacerInputWhileDragging = true;
    [SerializeField] private LayerMask casePlacementMask = ~0;
    [SerializeField] private float caseSurfaceOffset = 0.02f;
    [SerializeField] private PcPrefabCatalogMap prefabCatalogMap;
    [Header("Placement feedback")]
    [SerializeField] private TMP_Text placementFeedbackText;
    [SerializeField] private CanvasGroup placementFeedbackGroup;
    [SerializeField] private float placementFeedbackDuration = 3f;

    private bool isDragging;
    private PCComponent draggingComponent;
    private BuildingPart draggingPartPrefab;
    private Camera mainCamera;
    private BaseInputHandler cachedInputHandler;
    private BuildingPlacer.RaycastSettings.RaycastType cachedRaycastType;
    private BuildSlot currentTargetSlot;
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (buildingPlacer == null)
        {
            buildingPlacer = BuildingPlacer.Instance;
        }

        if (menuController == null)
        {
            menuController = FindFirstObjectByType<BuildMenuController>();
        }

        if (firstPersonCamera == null)
        {
            firstPersonCamera = FindFirstObjectByType<Demo_FirstPersonCamera>();
        }

        if (firstPersonController == null)
        {
            firstPersonController = FindFirstObjectByType<Demo_FirstPersonController>();
        }

        mainCamera = Camera.main;
        if (buildingPlacer != null)
        {
            cachedInputHandler = buildingPlacer.GetInputHandler;
            cachedRaycastType = buildingPlacer.GetRaycastSettings.ViewType;
        }
    }

    private void Update()
    {
        if (!isDragging || draggingComponent == null)
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
            return;
        }

        if (buildingPlacer != null && buildingPlacer.GetBuildMode != BuildingPlacer.BuildMode.PLACE)
        {
            buildingPlacer.ChangeBuildMode(BuildingPlacer.BuildMode.PLACE, false);
        }

        if (buildingPlacer != null && buildingPlacer.GetCurrentPreview == null && draggingPartPrefab != null)
        {
            buildingPlacer.CreatePreview(draggingPartPrefab);
        }

        UpdateSlotsHighlight();
    }

    public void BeginDrag(PCComponent component, BuildingPart buildingPartPrefab)
    {
        if (isDragging || component == null || buildingPartPrefab == null)
        {
            return;
        }

        if (buildingPlacer == null)
        {
            return;
        }

        draggingComponent = component;
        draggingPartPrefab = buildingPartPrefab;
        isDragging = true;

        if (menuController != null)
        {
            menuController.HideMenuWithoutLock();
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(true);
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
        }

        if (buildingPlacer != null)
        {
            cachedRaycastType = buildingPlacer.GetRaycastSettings.ViewType;
            buildingPlacer.GetRaycastSettings.ViewType = BuildingPlacer.RaycastSettings.RaycastType.TOP_DOWN_VIEW;

            if (disableDefaultPlacerInputWhileDragging)
            {
                cachedInputHandler = buildingPlacer.GetInputHandler;
                if (cachedInputHandler != null)
                {
                    cachedInputHandler.enabled = false;
                }
            }
        }

        buildingPlacer.SelectBuildingPart(buildingPartPrefab);
        buildingPlacer.ChangeBuildMode(BuildingPlacer.BuildMode.PLACE);
        buildingPlacer.CreatePreview(buildingPartPrefab);
    }

    public void EndDrag()
    {
        if (!isDragging || buildingPlacer == null)
        {
            return;
        }

        bool placed = TryPlaceIntoBuildSlot();
        if (!placed)
        {
            buildingPlacer.CancelPreview();
            if (menuController != null)
            {
                menuController.ShowMenu();
            }
        }
        else if (firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(false);
        }

        if (placed && firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        RestorePlacerRuntimeState();
        buildingPlacer.ChangeBuildMode(BuildingPlacer.BuildMode.NONE, false);
        ClearHighlights();
        currentTargetSlot = null;

        draggingComponent = null;
        draggingPartPrefab = null;
        isDragging = false;
    }

    public bool IsDragging => isDragging;

    public void CancelDrag()
    {
        if (!isDragging)
        {
            return;
        }

        if (buildingPlacer != null)
        {
            buildingPlacer.CancelPreview();
            buildingPlacer.ChangeBuildMode(BuildingPlacer.BuildMode.NONE, false);
        }

        if (menuController != null)
        {
            menuController.ShowMenu();
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        RestorePlacerRuntimeState();
        ClearHighlights();
        currentTargetSlot = null;
        draggingComponent = null;
        draggingPartPrefab = null;
        isDragging = false;
    }

    private void UpdateSlotsHighlight()
    {
        BuildSlot[] slots = GetNearbySlots();
        BuildSlot bestSlot = FindSlotUnderCursor(false);

        for (int i = 0; i < slots.Length; i++)
        {
            BuildSlot slot = slots[i];
            slot.SetHighlightState(draggingComponent);
        }

        currentTargetSlot = bestSlot;
    }

    private void ClearHighlights()
    {
        BuildSlot[] slots = GetNearbySlots();
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetHighlightState(null);
        }
    }

    private BuildSlot[] GetNearbySlots()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return new BuildSlot[0];
        }

        Collider[] colliders = Physics.OverlapSphere(mainCamera.transform.position, slotScanRadius, slotLayerMask);
        BuildSlot[] slots = new BuildSlot[colliders.Length];
        int count = 0;

        for (int i = 0; i < colliders.Length; i++)
        {
            BuildSlot slot = colliders[i].GetComponentInParent<BuildSlot>();
            if (slot == null)
            {
                continue;
            }

            slots[count] = slot;
            count++;
        }

        if (count == slots.Length)
        {
            return slots;
        }

        BuildSlot[] compact = new BuildSlot[count];
        for (int i = 0; i < count; i++)
        {
            compact[i] = slots[i];
        }

        return compact;
    }

    private bool TryPlaceIntoBuildSlot()
    {
        if (draggingComponent == null || draggingPartPrefab == null || mainCamera == null)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        BuildSlot slot = FindSlotUnderCursor(false);
        if (slot == null)
        {
            slot = currentTargetSlot;
        }

        if (slot == null)
        {
            if (draggingComponent.ComponentType == PCComponentType.Case)
            {
                return TryPlaceCaseOnSurface(ray);
            }

            ShowPlacementFeedback("Наведитесь на подходящий слот для установки детали.");
            return false;
        }

        if (!slot.CanPlace(draggingComponent))
        {
            ShowPlacementFeedback(slot.GetPlacementError(draggingComponent));
            return false;
        }

        Vector3 startPosition = slot.AnimationStartTransform.position;
        Quaternion startRotation = slot.AnimationStartTransform.rotation;

        if (buildingPlacer.GetCurrentPreview != null)
        {
            startRotation = buildingPlacer.GetCurrentPreview.transform.rotation;
        }

        PCComponent instanceComponent = SpawnComponentInstance(startPosition, startRotation);
        if (instanceComponent == null)
        {
            return false;
        }

        buildingPlacer.CancelPreview();
        return slot.Place(instanceComponent);
    }

    private BuildSlot FindSlotUnderCursor()
    {
        return FindSlotUnderCursor(true);
    }

    private BuildSlot FindSlotUnderCursor(bool requireCanPlace)
    {
        if (mainCamera == null || draggingComponent == null)
        {
            return null;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, slotLayerMask);
        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        float bestDistance = float.MaxValue;
        BuildSlot bestSlot = null;

        for (int i = 0; i < hits.Length; i++)
        {
            BuildSlot slot = hits[i].collider.GetComponentInParent<BuildSlot>();
            if (slot == null)
            {
                continue;
            }

            if (requireCanPlace && !slot.CanPlace(draggingComponent))
            {
                continue;
            }

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestSlot = slot;
            }
        }

        return bestSlot;
    }

    private void RestorePlacerRuntimeState()
    {
        if (buildingPlacer == null)
        {
            return;
        }

        buildingPlacer.GetRaycastSettings.ViewType = cachedRaycastType;

        if (cachedInputHandler != null)
        {
            cachedInputHandler.enabled = true;
        }
    }

    private bool TryPlaceCaseOnSurface(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, casePlacementMask))
        {
            return false;
        }

        Vector3 placePosition = hit.point + hit.normal * caseSurfaceOffset;
        Quaternion placeRotation = Quaternion.Euler(0f, mainCamera.transform.eulerAngles.y, 0f);

        if (buildingPlacer.GetCurrentPreview != null)
        {
            placeRotation = Quaternion.Euler(0f, buildingPlacer.GetCurrentPreview.transform.eulerAngles.y, 0f);
        }

        PCComponent instanceComponent = SpawnComponentInstance(placePosition, placeRotation);
        if (instanceComponent == null)
        {
            return false;
        }

        PrepareLoosePlacedComponent(instanceComponent);

        PCAssemblyState assembly = Object.FindFirstObjectByType<PCAssemblyState>();
        if (assembly != null)
        {
            assembly.RegisterInstalled(instanceComponent);
        }

        buildingPlacer.CancelPreview();
        return true;
    }

    private PCComponent SpawnComponentInstance(Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate(draggingPartPrefab.gameObject, position, rotation);
        PCComponent instanceComponent = instance.GetComponent<PCComponent>();
        if (instanceComponent == null)
        {
            instanceComponent = instance.AddComponent<PCComponent>();
        }

        instanceComponent.CopyFrom(draggingComponent);
        if (prefabCatalogMap != null)
        {
            ComponentModelBinder.BindVisual(instance.transform, prefabCatalogMap, draggingComponent.ToData());
        }

        return instanceComponent;
    }

    private void PrepareLoosePlacedComponent(PCComponent component)
    {
        Rigidbody rb = component.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ShowPlacementFeedback(string message)
    {
        if (placementFeedbackText == null || placementFeedbackGroup == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(ShowPlacementFeedbackRoutine(message));
    }

    private IEnumerator ShowPlacementFeedbackRoutine(string message)
    {
        placementFeedbackText.text = message;
        placementFeedbackGroup.alpha = 1f;
        placementFeedbackGroup.interactable = false;
        placementFeedbackGroup.blocksRaycasts = false;

        float waitFor = Mathf.Max(0.75f, placementFeedbackDuration);
        yield return new WaitForSeconds(waitFor);

        placementFeedbackGroup.alpha = 0f;
        feedbackRoutine = null;
    }
}
