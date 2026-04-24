using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildSlot : MonoBehaviour
{
    [Header("Slot Setup")]
    [SerializeField] private PCSlotType slotType = PCSlotType.None;
    [SerializeField] private List<PCComponentType> allowedComponentTypes = new List<PCComponentType>();
    [SerializeField] private Transform snapTransform;
    [SerializeField] private Transform animationStartTransform;
    [SerializeField] private Renderer highlightMesh;

    [Header("Highlight Colors")]
    [SerializeField] private Color idleColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Install Animation")]
    [SerializeField, Range(0.3f, 0.6f)] private float installDuration = 0.4f;
    [SerializeField] private bool enablePlacementAnimation = true;

    private PCComponent currentComponent;
    private MaterialPropertyBlock propertyBlock;

    public PCSlotType SlotType => slotType;
    public bool IsOccupied => currentComponent != null;
    public IReadOnlyList<PCComponentType> AllowedComponentTypes => allowedComponentTypes;
    public Transform SnapTransform => snapTransform != null ? snapTransform : transform;
    public Transform AnimationStartTransform => animationStartTransform != null ? animationStartTransform : SnapTransform;

    public bool CanPlace(PCComponent component)
    {
        if (component == null || IsOccupied)
        {
            return false;
        }

        if (!allowedComponentTypes.Contains(component.ComponentType))
        {
            if (allowedComponentTypes.Count > 0)
            {
                return false;
            }
        }

        if (!component.IsCompatibleWith(slotType))
        {
            return false;
        }

        return PCCompatibilityService.CanPlaceComponent(component, this);
    }

    public bool Place(PCComponent component)
    {
        if (!CanPlace(component))
        {
            return false;
        }

        currentComponent = component;
        component.MarkInstalled(this);
        component.transform.SetParent(transform, true);
        PreparePlacedComponent(component);
        PCAssemblyState assembly = FindAssemblyState();
        if (assembly != null)
        {
            assembly.RegisterInstalled(component);
        }

        if (enablePlacementAnimation)
        {
            StartCoroutine(AnimatePlacement(component.transform));
        }
        else
        {
            Snap(component.transform);
        }

        return true;
    }

    public void Remove()
    {
        if (currentComponent == null)
        {
            return;
        }

        PCAssemblyState assembly = FindAssemblyState();
        if (assembly != null)
        {
            assembly.RegisterRemoved(currentComponent);
        }

        currentComponent.MarkInstalled(null);
        currentComponent = null;
    }

    public void SetHighlightState(PCComponent component)
    {
        if (highlightMesh == null)
        {
            return;
        }

        if (component == null)
        {
            SetHighlightColor(idleColor);
            return;
        }

        SetHighlightColor(CanPlace(component) ? validColor : invalidColor);
    }

    private IEnumerator AnimatePlacement(Transform componentTransform)
    {
        Transform target = SnapTransform;
        Transform start = AnimationStartTransform;
        Vector3 startPos = start.position;
        Quaternion startRot = start.rotation;
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        componentTransform.position = startPos;
        componentTransform.rotation = startRot;

        float elapsed = 0f;
        while (elapsed < installDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / installDuration);
            componentTransform.position = Vector3.Lerp(startPos, endPos, t);
            componentTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        Snap(componentTransform);
    }

    private void Snap(Transform componentTransform)
    {
        Transform target = SnapTransform;
        componentTransform.position = target.position;
        componentTransform.rotation = target.rotation;
        componentTransform.SetParent(transform, true);
    }

    private void SetHighlightColor(Color color)
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        highlightMesh.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", color);
        highlightMesh.SetPropertyBlock(propertyBlock);
    }

    private void PreparePlacedComponent(PCComponent component)
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

    private PCAssemblyState FindAssemblyState()
    {
        return Object.FindFirstObjectByType<PCAssemblyState>();
    }
}
