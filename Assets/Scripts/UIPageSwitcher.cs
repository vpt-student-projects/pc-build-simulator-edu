using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простое перелистывание UI-страниц (включение/выключение GameObject).
/// - Кнопки "вперед/назад"
/// - Запоминание выбранной страницы в рамках текущего запуска приложения
/// - Сброс состояния только после перезапуска приложения
/// </summary>
public class UIPageSwitcher : MonoBehaviour
{
    // runtime-only cache: живет, пока приложение запущено
    private static readonly Dictionary<string, int> RuntimePageByKey = new Dictionary<string, int>();

    [Header("Pages")]
    [SerializeField] private List<GameObject> pages = new List<GameObject>();
    [SerializeField] private bool autoFillFromChildren = false;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private bool clampAtEdges = true;

    [Header("State Key")]
    [Tooltip("Ключ для сохранения текущей страницы в runtime. Если пусто, используется имя объекта.")]
    [SerializeField] private string stateKey = string.Empty;

    [Header("Start Page")]
    [Tooltip("Страница, которая включится при первом открытии в текущем запуске (1-based).")]
    [SerializeField] private int initialPageOneBased = 1;

    private int currentIndex;
    private string EffectiveKey => string.IsNullOrWhiteSpace(stateKey) ? gameObject.name : stateKey.Trim();

    private void Awake()
    {
        if (autoFillFromChildren)
        {
            RebuildPagesFromChildren();
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
            nextButton.onClick.AddListener(NextPage);
        }

        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(PreviousPage);
            previousButton.onClick.AddListener(PreviousPage);
        }

        InitializeCurrentIndex();
        ApplyCurrentPage();
    }

    private void OnEnable()
    {
        // При повторном открытии страницы в том же запуске остаются на выбранной странице.
        if (pages.Count == 0 && autoFillFromChildren)
        {
            RebuildPagesFromChildren();
        }

        ApplyCurrentPage();
    }

    public void NextPage()
    {
        if (pages.Count == 0)
        {
            return;
        }

        if (clampAtEdges)
        {
            currentIndex = Mathf.Min(currentIndex + 1, pages.Count - 1);
        }
        else
        {
            currentIndex = (currentIndex + 1) % pages.Count;
        }

        SaveCurrentIndex();
        ApplyCurrentPage();
    }

    public void PreviousPage()
    {
        if (pages.Count == 0)
        {
            return;
        }

        if (clampAtEdges)
        {
            currentIndex = Mathf.Max(currentIndex - 1, 0);
        }
        else
        {
            currentIndex = (currentIndex - 1 + pages.Count) % pages.Count;
        }

        SaveCurrentIndex();
        ApplyCurrentPage();
    }

    public void GoToPageOneBased(int pageNumber)
    {
        if (pages.Count == 0)
        {
            return;
        }

        int target = Mathf.Clamp(pageNumber - 1, 0, pages.Count - 1);
        currentIndex = target;
        SaveCurrentIndex();
        ApplyCurrentPage();
    }

    public void AddPage(GameObject page)
    {
        if (page == null || pages.Contains(page))
        {
            return;
        }

        pages.Add(page);
        ApplyCurrentPage();
    }

    public void RemovePage(GameObject page)
    {
        if (page == null)
        {
            return;
        }

        if (!pages.Remove(page))
        {
            return;
        }

        if (currentIndex >= pages.Count)
        {
            currentIndex = Mathf.Max(0, pages.Count - 1);
        }

        SaveCurrentIndex();
        ApplyCurrentPage();
    }

    public void RebuildPagesFromChildren()
    {
        pages.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            pages.Add(transform.GetChild(i).gameObject);
        }
    }

    private void InitializeCurrentIndex()
    {
        if (pages.Count == 0)
        {
            currentIndex = 0;
            return;
        }

        if (RuntimePageByKey.TryGetValue(EffectiveKey, out int saved))
        {
            currentIndex = Mathf.Clamp(saved, 0, pages.Count - 1);
            return;
        }

        currentIndex = Mathf.Clamp(initialPageOneBased - 1, 0, pages.Count - 1);
        SaveCurrentIndex();
    }

    private void SaveCurrentIndex()
    {
        RuntimePageByKey[EffectiveKey] = currentIndex;
    }

    private void ApplyCurrentPage()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            GameObject page = pages[i];
            if (page != null)
            {
                page.SetActive(i == currentIndex);
            }
        }

        UpdateButtonsInteractable();
    }

    private void UpdateButtonsInteractable()
    {
        if (pages.Count == 0)
        {
            if (nextButton != null) nextButton.interactable = false;
            if (previousButton != null) previousButton.interactable = false;
            return;
        }

        if (!clampAtEdges)
        {
            if (nextButton != null) nextButton.interactable = pages.Count > 1;
            if (previousButton != null) previousButton.interactable = pages.Count > 1;
            return;
        }

        if (nextButton != null) nextButton.interactable = currentIndex < pages.Count - 1;
        if (previousButton != null) previousButton.interactable = currentIndex > 0;
    }
}
