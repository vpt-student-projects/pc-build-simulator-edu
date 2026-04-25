using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;
using UnityEngine;
using UnityEngine.UI;

public class PCComponentModelViewer : MonoBehaviour
{
    public static bool IsViewerOpen { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject viewerRoot;
    [SerializeField] private Button closeButton;

    [Header("Scene")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private Demo_FirstPersonCamera firstPersonCamera;
    [SerializeField] private Demo_FirstPersonController firstPersonController;
    [SerializeField] private Camera previewCamera;
    [Tooltip("Если включено, всегда используется Camera.main (игровая MainCamera). Поле previewCamera тогда только как запасной вариант, если main не найден.")]
    [SerializeField] private bool useMainCamera = true;
    [SerializeField] private Vector3 previewLocalOffset = new Vector3(0f, 0f, 2.2f);

    [Header("Model fit")]
    [Tooltip("После подгонки самый большой полуразмер модели по осям (мир), чтобы превью не заполняло весь экран и не вылезало в сцену.")]
    [SerializeField] private float targetHalfExtentMeters = 0.32f;
    [Tooltip("Если 0–31: все объекты превью переводятся на этот слой (удобно отключить коллизии/кастом в проекте). -1 = не менять.")]
    [SerializeField] private int previewLayer = -1;

    [Header("Controls")]
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minDistance = 0.45f;
    [SerializeField] private float maxDistance = 2.8f;
    [SerializeField] private float smoothTime = 0.08f;

    private GameObject currentInstance;
    private bool isOpen;
    private float yaw;
    private float pitch = 15f;
    private float targetDistance = 2.2f;
    private float currentDistance = 2.2f;
    private float distanceVelocity;
    private float boundsRadius = 0.5f;

    private Transform savedCameraParent;
    private Vector3 savedCameraLocalPosition;
    private Quaternion savedCameraLocalRotation;
    private Vector3 savedCameraLocalScale;
    private bool hasSavedCameraPose;

    private Vector3 savedAnchorLocalPosition;
    private Quaternion savedAnchorLocalRotation;

    private void Awake()
    {
        if (firstPersonCamera == null) firstPersonCamera = FindFirstObjectByType<Demo_FirstPersonCamera>();
        if (firstPersonController == null) firstPersonController = FindFirstObjectByType<Demo_FirstPersonController>();
        ResolvePreviewCamera();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseViewer);
            closeButton.onClick.AddListener(CloseViewer);
        }

        if (viewerRoot != null)
        {
            viewerRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isOpen || currentInstance == null || previewAnchor == null)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed * Time.unscaledDeltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed * Time.unscaledDeltaTime;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        UpdateModelPose();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseViewer();
        }
    }

    public void OpenViewer(GameObject sourcePrefab)
    {
        if (sourcePrefab == null)
        {
            return;
        }

        ResolvePreviewCamera();
        if (previewCamera == null)
        {
            Debug.LogWarning("PCComponentModelViewer: не найдена камера (Camera.main / previewCamera).");
            return;
        }

        CloseCurrentInstance();
        EnsurePreviewAnchor();
        SaveCameraPose();

        currentInstance = Instantiate(sourcePrefab, previewAnchor);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one;
        currentInstance.name = "ModelPreviewRuntime";

        DisableBehaviourScripts(currentInstance);
        FitModelScaleAndCenter(currentInstance);
        ApplyPreviewLayer(currentInstance);

        targetDistance = Mathf.Clamp(boundsRadius * 2.4f, minDistance, maxDistance);
        currentDistance = targetDistance;
        yaw = 0f;
        pitch = 15f;

        isOpen = true;
        IsViewerOpen = true;
        if (viewerRoot != null)
        {
            viewerRoot.SetActive(true);
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(true);
        }
        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UpdateModelPose();
    }

    public void CloseViewer()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        IsViewerOpen = false;
        CloseCurrentInstance();

        if (viewerRoot != null)
        {
            viewerRoot.SetActive(false);
        }

        RestoreCameraPose();
        RestorePreviewAnchorPose();

        if (firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(false);
        }
        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ResolvePreviewCamera()
    {
        if (useMainCamera && Camera.main != null)
        {
            previewCamera = Camera.main;
            return;
        }

        if (previewCamera == null)
        {
            previewCamera = Camera.main;
        }
    }

    private void EnsurePreviewAnchor()
    {
        if (previewAnchor != null)
        {
            if (previewCamera != null && previewAnchor.parent != previewCamera.transform)
            {
                previewAnchor.SetParent(previewCamera.transform, false);
            }
            return;
        }

        GameObject go = new GameObject("PreviewAnchor");
        previewAnchor = go.transform;
        ResolvePreviewCamera();
        previewAnchor.SetParent(previewCamera != null ? previewCamera.transform : transform, false);
    }

    private void SaveCameraPose()
    {
        Transform cam = previewCamera != null ? previewCamera.transform : null;
        if (cam == null)
        {
            return;
        }

        savedCameraParent = cam.parent;
        savedCameraLocalPosition = cam.localPosition;
        savedCameraLocalRotation = cam.localRotation;
        savedCameraLocalScale = cam.localScale;
        hasSavedCameraPose = true;

        if (previewAnchor != null)
        {
            savedAnchorLocalPosition = previewAnchor.localPosition;
            savedAnchorLocalRotation = previewAnchor.localRotation;
        }
    }

    private void RestoreCameraPose()
    {
        if (!hasSavedCameraPose || previewCamera == null)
        {
            return;
        }

        Transform cam = previewCamera.transform;
        cam.SetParent(savedCameraParent, false);
        cam.localPosition = savedCameraLocalPosition;
        cam.localRotation = savedCameraLocalRotation;
        cam.localScale = savedCameraLocalScale;
        hasSavedCameraPose = false;
    }

    private void RestorePreviewAnchorPose()
    {
        if (previewAnchor == null)
        {
            return;
        }

        previewAnchor.localPosition = savedAnchorLocalPosition;
        previewAnchor.localRotation = savedAnchorLocalRotation;
    }

    private void UpdateModelPose()
    {
        previewAnchor.localPosition = new Vector3(previewLocalOffset.x, previewLocalOffset.y, currentDistance);
        if (currentInstance != null)
        {
            currentInstance.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    private static void DisableBehaviourScripts(GameObject root)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
            {
                behaviours[i].enabled = false;
            }
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = true;
            bodies[i].detectCollisions = false;
        }
    }

    private void FitModelScaleAndCenter(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            boundsRadius = 0.25f;
            return;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        float maxExtent = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
        if (maxExtent < 1e-5f)
        {
            boundsRadius = 0.12f;
            return;
        }

        float target = Mathf.Max(0.05f, targetHalfExtentMeters);
        float scaleFactor = target / maxExtent;
        root.transform.localScale = root.transform.localScale * scaleFactor;

        b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        float halfAfter = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
        boundsRadius = Mathf.Clamp(halfAfter, 0.06f, target * 1.05f);

        Vector3 centerLocal = previewAnchor.InverseTransformPoint(b.center);
        root.transform.localPosition -= centerLocal;
    }

    private void ApplyPreviewLayer(GameObject root)
    {
        if (previewLayer < 0 || previewLayer > 31)
        {
            return;
        }

        SetLayerRecursively(root.transform, previewLayer);
    }

    private static void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
        {
            SetLayerRecursively(t.GetChild(i), layer);
        }
    }

    private void CloseCurrentInstance()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }
    }
}
