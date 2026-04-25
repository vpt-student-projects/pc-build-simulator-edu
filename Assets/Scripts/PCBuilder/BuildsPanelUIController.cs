using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildsPanelUIController : MonoBehaviour
{
    [SerializeField] private BuildPersistenceService persistenceService;
    [SerializeField] private TMP_Dropdown buildsDropdown;
    [SerializeField] private TMP_InputField saveNameInput;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Text statusText;

    private readonly List<BuildListItem> items = new List<BuildListItem>();

    private void Awake()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(RefreshList);
        if (saveButton != null) saveButton.onClick.AddListener(SaveCurrent);
        if (loadButton != null) loadButton.onClick.AddListener(LoadSelected);
        if (deleteButton != null) deleteButton.onClick.AddListener(DeleteSelected);
    }

    private void OnEnable()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        if (persistenceService == null || buildsDropdown == null)
        {
            SetStatus("Не назначен persistenceService или dropdown.");
            return;
        }

        items.Clear();
        items.AddRange(persistenceService.GetBuilds());

        buildsDropdown.ClearOptions();
        var opts = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            opts.Add(items[i].ToString());
        }

        if (opts.Count == 0)
        {
            opts.Add("Сохранений нет");
        }

        buildsDropdown.AddOptions(opts);
        buildsDropdown.value = 0;
        SetStatus($"Загружено записей: {items.Count}");
    }

    public void SaveCurrent()
    {
        if (persistenceService == null)
        {
            return;
        }

        string name = saveNameInput != null ? saveNameInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Сборка " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        bool ok = persistenceService.SaveCurrentBuild(name);
        SetStatus(ok ? $"Сборка '{name}' сохранена." : "Не удалось сохранить сборку.");
        RefreshList();
    }

    public void LoadSelected()
    {
        if (persistenceService == null)
        {
            return;
        }

        BuildListItem selected = GetSelectedItem();
        if (selected == null)
        {
            SetStatus("Выберите сборку для загрузки.");
            return;
        }

        bool ok = persistenceService.LoadBuild(selected.BuildId);
        SetStatus(ok ? $"Сборка '{selected.Name}' загружена." : "Не удалось загрузить сборку.");
    }

    public void DeleteSelected()
    {
        if (persistenceService == null)
        {
            return;
        }

        BuildListItem selected = GetSelectedItem();
        if (selected == null)
        {
            SetStatus("Выберите сборку для удаления.");
            return;
        }

        bool ok = persistenceService.DeleteBuild(selected.BuildId);
        SetStatus(ok ? $"Сборка '{selected.Name}' удалена." : "Не удалось удалить сборку.");
        RefreshList();
    }

    private BuildListItem GetSelectedItem()
    {
        if (items.Count == 0 || buildsDropdown == null)
        {
            return null;
        }

        int idx = Mathf.Clamp(buildsDropdown.value, 0, items.Count - 1);
        return items[idx];
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
