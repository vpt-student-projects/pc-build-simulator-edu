using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ComponentInfoPopup : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private float visibleDurationSeconds = 7f;
    [SerializeField] private CanvasGroup popupGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image iconImage;

    public static ComponentInfoPopup Instance { get; private set; }
    private Coroutine hideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Hide();
    }

    public void Show(PCComponent component)
    {
        if (component == null)
        {
            return;
        }

        EnsureRefs();
        if (popupGroup == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(component.DisplayName)
                ? component.ComponentType.ToString()
                : component.DisplayName;
        }

        if (bodyText != null)
        {
            bodyText.text = BuildDescription(component);
        }

        if (iconImage != null)
        {
            Sprite icon = LoadIcon(component.IconPath);
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        popupGroup.alpha = 1f;
        popupGroup.interactable = false;
        popupGroup.blocksRaycasts = false;

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(AutoHideAfterDelay());
    }

    public void Hide()
    {
        EnsureRefs();
        if (popupGroup == null)
        {
            return;
        }

        popupGroup.alpha = 0f;
        popupGroup.interactable = false;
        popupGroup.blocksRaycasts = false;

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    private void EnsureRefs()
    {
        if (popupGroup == null)
        {
            popupGroup = GetComponent<CanvasGroup>();
        }
    }

    private static string BuildDescription(PCComponent c)
    {
        string text = string.IsNullOrWhiteSpace(c.Description) ? "Описание отсутствует." : c.Description.Trim();
        text += $"\n\nКатегория: {c.ComponentType}";

        if (!string.IsNullOrWhiteSpace(c.SocketCode))
        {
            text += $"\nСокет: {c.SocketCode}";
        }

        if (!string.IsNullOrWhiteSpace(c.RamTypeCode))
        {
            text += $"\nТип RAM: {c.RamTypeCode}";
        }

        if (c.RamSlotsCount > 0)
        {
            text += $"\nСлотов RAM: {c.RamSlotsCount}";
        }

        if (c.RequiredPSUPower > 0)
        {
            text += $"\nРекомендуемый PSU: {c.RequiredPSUPower}W";
        }
        else if (c.PsuPower > 0)
        {
            text += $"\nМощность PSU: {c.PsuPower}W";
        }

        if (c.GpuTdpW > 0)
        {
            text += $"\nTDP: {c.GpuTdpW}W";
        }

        return text;
    }

    private static Sprite LoadIcon(string iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        string normalized = iconPath.Trim().Replace('\\', '/');
        if (normalized.StartsWith("Assets/Resources/", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring("Assets/Resources/".Length);
        }
        else if (normalized.StartsWith("Resources/", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring("Resources/".Length);
        }

        if (normalized.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - 4);
        }
        else if (normalized.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - 4);
        }
        else if (normalized.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - 5);
        }

        Sprite sprite = Resources.Load<Sprite>(normalized);
        if (sprite != null)
        {
            return sprite;
        }

        if (!normalized.StartsWith("icons/", System.StringComparison.OrdinalIgnoreCase))
        {
            return Resources.Load<Sprite>("icons/" + normalized);
        }

        return null;
    }

    private IEnumerator AutoHideAfterDelay()
    {
        float delay = Mathf.Max(0.1f, visibleDurationSeconds);
        yield return new WaitForSeconds(delay);
        Hide();
    }
}
