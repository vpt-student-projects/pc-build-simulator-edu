using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Универсальный слот установки. Поддерживает локальные поправки позы (как offset в EBS),
/// отдельную точку старта анимации и превью-префаб в редакторе для настройки без угадывания координат.
/// </summary>
[ExecuteAlways]
public class BuildSlot : MonoBehaviour
{
    private const string EditorPreviewChildName = "_PlacementPreview_";

    [Header("Slot Setup")]
    [SerializeField] private PCSlotType slotType = PCSlotType.None;
    [SerializeField] private List<PCComponentType> allowedComponentTypes = new List<PCComponentType>();
    [Tooltip("Точка привязки: origin + rotation задают систему координат для поправок ниже.")]
    [SerializeField] private Transform snapTransform;
    [Tooltip("Если задано — анимация начинается отсюда; иначе от позиции с учётом «подхода» от snap.")]
    [SerializeField] private Transform animationStartTransform;
    [SerializeField] private Renderer highlightMesh;

    [Header("Placement corrections (local to Snap)")]
    [Tooltip("Смещение финальной позиции в локальных осях Snap (как Position в EBS SnappingPoint).")]
    [SerializeField] private Vector3 installLocalPosition;
    [Tooltip("Дополнительный поворот в локальных осях Snap (Euler, как Rotation в EBS).")]
    [SerializeField] private Vector3 installLocalEulerAngles;

    [Header("Animation start (when Animation Start is empty)")]
    [Tooltip("Локальное смещение от финальной позы — откуда «прилетает» деталь (например чуть выше/назад).")]
    [SerializeField] private Vector3 animationApproachLocalOffset = new Vector3(0f, 0.08f, -0.12f);

    [Header("Editor placement preview")]
    [Tooltip("Префаб для визуальной подсказки в редакторе (не обязан совпадать с реальным BuildingPart).")]
    [SerializeField] private GameObject placementPreviewPrefab;
    [Tooltip("В Play Mode превью удаляется, чтобы не мешать игре.")]
    [SerializeField] private bool hidePlacementPreviewInPlayMode = true;

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

    public Transform AnimationStartTransform
    {
        get
        {
            if (animationStartTransform != null)
            {
                return animationStartTransform;
            }

            return SnapTransform;
        }
    }

    private void Awake()
    {
        if (Application.isPlaying && hidePlacementPreviewInPlayMode)
        {
            DestroyEditorPreviewInstance();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            RefreshEditorPlacementPreview();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RefreshEditorPlacementPreview();
        }
    }

    public bool CanPlace(PCComponent component)
    {
        return string.IsNullOrEmpty(GetPlacementError(component));
    }

    public string GetPlacementError(PCComponent component)
    {
        if (component == null || IsOccupied)
        {
            return component == null
                ? "Не удалось определить устанавливаемую деталь."
                : $"Слот {slotType} уже занят.";
        }

        if (!allowedComponentTypes.Contains(component.ComponentType))
        {
            if (allowedComponentTypes.Count > 0)
            {
                return $"Деталь {component.ComponentType} нельзя установить в слот {slotType}.";
            }
        }

        if (!component.IsCompatibleWith(slotType))
        {
            return $"Деталь {component.ComponentType} не подходит к слоту {slotType}.";
        }

        if (!PCCompatibilityService.TryGetCompatibilityError(component, this, out string compatibilityError))
        {
            return string.IsNullOrWhiteSpace(compatibilityError)
                ? "Компонент несовместим с текущей сборкой."
                : compatibilityError;
        }

        return string.Empty;
    }

    public bool Place(PCComponent component)
    {
        if (!CanPlace(component))
        {
            return false;
        }

        currentComponent = component;
        component.MarkInstalled(this);
        component.transform.SetParent(SnapTransform, true);
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

    /// <summary>
    /// Финальная мировая поза детали с учётом installLocal* относительно Snap.
    /// </summary>
    public void GetInstallWorldPose(out Vector3 worldPosition, out Quaternion worldRotation)
    {
        Transform anchor = SnapTransform;
        worldPosition = anchor.position + anchor.rotation * installLocalPosition;
        worldRotation = anchor.rotation * Quaternion.Euler(installLocalEulerAngles);
    }

    private IEnumerator AnimatePlacement(Transform componentTransform)
    {
        GetInstallWorldPose(out Vector3 endPos, out Quaternion endRot);

        Vector3 startPos;
        Quaternion startRot;

        if (animationStartTransform != null)
        {
            startPos = animationStartTransform.position;
            startRot = animationStartTransform.rotation;
        }
        else
        {
            Transform anchor = SnapTransform;
            startPos = endPos + anchor.rotation * animationApproachLocalOffset;
            startRot = endRot;
        }

        componentTransform.SetParent(SnapTransform, true);
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
        GetInstallWorldPose(out Vector3 worldPosition, out Quaternion worldRotation);
        componentTransform.SetPositionAndRotation(worldPosition, worldRotation);
        componentTransform.SetParent(SnapTransform, true);
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

    private void RefreshEditorPlacementPreview()
    {
        Transform anchor = SnapTransform;
        Transform existing = anchor.Find(EditorPreviewChildName);

        if (placementPreviewPrefab == null)
        {
            if (existing != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(existing.gameObject);
                }
                else
                {
                    Destroy(existing.gameObject);
                }
#else
                Destroy(existing.gameObject);
#endif
            }

            return;
        }

        GameObject previewInstance;
        if (existing == null)
        {
            previewInstance = Instantiate(placementPreviewPrefab, anchor);
            previewInstance.name = EditorPreviewChildName;
        }
        else
        {
            previewInstance = existing.gameObject;
        }

        previewInstance.transform.localPosition = installLocalPosition;
        previewInstance.transform.localRotation = Quaternion.Euler(installLocalEulerAngles);
        previewInstance.transform.localScale = Vector3.one;

        foreach (Collider c in previewInstance.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = false;
        }

        foreach (Renderer r in previewInstance.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = true;
        }
    }

    private void DestroyEditorPreviewInstance()
    {
        Transform anchor = SnapTransform;
        Transform existing = anchor.Find(EditorPreviewChildName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }
}
