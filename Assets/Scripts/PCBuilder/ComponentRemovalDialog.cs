using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;

public class ComponentRemovalDialog : MonoBehaviour
{
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Demo_FirstPersonCamera firstPersonCamera;
    [SerializeField] private Demo_FirstPersonController firstPersonController;

    public static ComponentRemovalDialog Instance { get; private set; }

    private PCComponent pendingComponent;
    private bool dialogActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(ConfirmRemove);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(CancelRemove);
        }

        if (firstPersonCamera == null)
        {
            firstPersonCamera = FindFirstObjectByType<Demo_FirstPersonCamera>();
        }

        if (firstPersonController == null)
        {
            firstPersonController = FindFirstObjectByType<Demo_FirstPersonController>();
        }

        HideDialog();
    }

    public void RequestRemove(PCComponent component)
    {
        if (component == null)
        {
            return;
        }

        pendingComponent = component;
        if (messageText != null)
        {
            messageText.text = $"Вытащить деталь {GetComponentName(component)}?";
        }

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(true);
        }
        dialogActive = true;

        if (ComponentInfoPopup.Instance != null)
        {
            ComponentInfoPopup.Instance.Hide();
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(true);
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
        }
    }

    public void ConfirmRemove()
    {
        if (pendingComponent != null)
        {
            RemoveComponent(pendingComponent);
        }

        HideDialog();
    }

    public void CancelRemove()
    {
        HideDialog();
    }

    private void HideDialog()
    {
        pendingComponent = null;
        if (dialogRoot != null)
        {
            dialogRoot.SetActive(false);
        }

        if (dialogActive && firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(false);
        }

        if (dialogActive && firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        dialogActive = false;
    }

    private static void RemoveComponent(PCComponent component)
    {
        BuildSlot parentSlot = component.ParentSlot;
        if (parentSlot != null)
        {
            parentSlot.Remove();
        }
        else
        {
            PCAssemblyState assembly = Object.FindFirstObjectByType<PCAssemblyState>();
            if (assembly != null)
            {
                assembly.RegisterRemoved(component);
            }
            component.MarkInstalled(null);
        }

        Object.Destroy(component.gameObject);
    }

    private static string GetComponentName(PCComponent component)
    {
        if (component == null)
        {
            return "неизвестная деталь";
        }

        if (!string.IsNullOrWhiteSpace(component.DisplayName))
        {
            return component.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(component.Model))
        {
            return component.Model;
        }

        return component.ComponentType.ToString();
    }
}
