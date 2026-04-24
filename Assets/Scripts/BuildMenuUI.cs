using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EasyBuildSystem.Features.Runtime.Buildings.Placer;
using EasyBuildSystem.Features.Runtime.Buildings.Part;

public class BuildMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private ToggleGroup tabsGroup;

    [Header("Dropdown Filters")]
    [SerializeField] private TMP_Dropdown powerFilterDropdown;
    [SerializeField] private TMP_Dropdown nameFilterDropdown;

    [Header("Data")]
    [SerializeField] private List<BuildingPartData> allParts = new List<BuildingPartData>();
    [Header("Drag & Drop")]
    [SerializeField] private BuildModeDragController dragController;

    // ˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜ Toggle ˜ ˜˜˜˜˜˜˜˜˜
    private Dictionary<Toggle, string> m_ToggleCategories = new Dictionary<Toggle, string>();

    private string m_CurrentCategory = "ALL";
    private string m_CurrentPowerFilter = "ALL";
    private string m_CurrentNameSort = "DEFAULT";

    private List<GameObject> spawnedCards = new List<GameObject>();

    void Start()
    {
        // ˜˜˜˜˜˜˜˜
        if (cardPrefab == null || contentParent == null || tabsGroup == null)
        {
            Debug.LogError("˜˜ ˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜˜˜˜!");
            return;
        }

        // ˜˜˜˜˜˜˜˜˜ Toggle Group
        SetupToggles();

        // ˜˜˜˜˜˜˜˜˜ Dropdown ˜˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜
        if (powerFilterDropdown != null)
        {
            powerFilterDropdown.ClearOptions();
            powerFilterDropdown.AddOptions(new List<string> { "˜˜˜", "0-25%", "25-50%", "50-75%", "75-100%" });
            powerFilterDropdown.onValueChanged.AddListener(OnPowerFilterChanged);
        }

        // ˜˜˜˜˜˜˜˜˜ Dropdown ˜˜˜ ˜˜˜˜˜˜˜˜˜˜
        if (nameFilterDropdown != null)
        {
            nameFilterDropdown.ClearOptions();
            nameFilterDropdown.AddOptions(new List<string> { "˜˜ ˜˜˜˜˜˜˜˜˜", "˜˜ ˜˜˜˜˜˜˜˜ (˜-˜)", "˜˜ ˜˜˜˜˜˜˜˜ (˜-˜)", "˜˜ ˜˜˜˜˜˜˜˜ (˜˜˜˜.)", "˜˜ ˜˜˜˜˜˜˜˜ (˜˜˜˜.)" });
            nameFilterDropdown.onValueChanged.AddListener(OnNameSortChanged);
        }

        // ˜˜˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜
        RefreshCards();
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜ ˜˜˜˜ Toggle ˜ ˜˜˜˜˜˜
    /// </summary>
    private void SetupToggles()
    {
        m_ToggleCategories.Clear();

        // ˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜ ˜˜˜˜˜˜˜˜ Toggle
        foreach (Transform child in tabsGroup.transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                // ˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜
                toggle.onValueChanged.RemoveAllListeners();

                // ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜˜ ˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜
                string category = GetCategoryFromToggleName(child.name);
                m_ToggleCategories[toggle] = category;

                // ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜
                toggle.onValueChanged.AddListener((isOn) => {
                    if (isOn)
                    {
                        OnCategoryChanged(toggle);
                    }
                });

                // ˜˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜
                toggle.group = tabsGroup;
            }
        }

        // ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ Toggle (˜˜)
        foreach (var kvp in m_ToggleCategories)
        {
            if (kvp.Value == "ALL")
            {
                kvp.Key.isOn = true;
                break;
            }
        }
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜ Toggle
    /// </summary>
    private string GetCategoryFromToggleName(string toggleName)
    {
        // ˜˜˜˜˜˜˜ ˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜˜
        switch (toggleName.ToLower())
        {
            case "˜˜":
            case "all":
                return "ALL";
            case "˜˜˜":
            case "cpu":
                return "CPU";
            case "":
            case "ram":
                return "RAM";
            case " ":
            case "psu":
                return "PSU";
            case "˜˜˜˜˜˜˜˜˜":
            case "motherboard":
                return "MOTHERBOARD";
            case "˜˜˜˜˜˜":
            case "case":
                return "CASE";
            case "˜˜˜˜˜˜˜˜˜˜":
            case "gpu":
                return "GPU";
            default:
                Debug.LogWarning($"˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜ Toggle: {toggleName}. ˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜˜˜.");
                return toggleName.ToUpper();
        }
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ Toggle
    /// </summary>
    private void OnCategoryChanged(Toggle activeToggle)
    {
        if (m_ToggleCategories.TryGetValue(activeToggle, out string category))
        {
            m_CurrentCategory = category;
            RefreshCards();
        }
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜
    /// </summary>
    private void OnPowerFilterChanged(int index)
    {
        switch (index)
        {
            case 0: m_CurrentPowerFilter = "ALL"; break;
            case 1: m_CurrentPowerFilter = "0-25"; break;
            case 2: m_CurrentPowerFilter = "25-50"; break;
            case 3: m_CurrentPowerFilter = "50-75"; break;
            case 4: m_CurrentPowerFilter = "75-100"; break;
        }
        RefreshCards();
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜
    /// </summary>
    private void OnNameSortChanged(int index)
    {
        switch (index)
        {
            case 0: m_CurrentNameSort = "DEFAULT"; break;
            case 1: m_CurrentNameSort = "NAME_ASC"; break;
            case 2: m_CurrentNameSort = "NAME_DESC"; break;
            case 3: m_CurrentNameSort = "POWER_ASC"; break;
            case 4: m_CurrentNameSort = "POWER_DESC"; break;
        }
        RefreshCards();
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜, ˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜
    /// </summary>
    private bool PassesPowerFilter(BuildingPartData part)
    {
        if (m_CurrentPowerFilter == "ALL") return true;

        float power = part.PowerPercent;

        switch (m_CurrentPowerFilter)
        {
            case "0-25": return power >= 0 && power <= 25;
            case "25-50": return power > 25 && power <= 50;
            case "50-75": return power > 50 && power <= 75;
            case "75-100": return power > 75 && power <= 100;
            default: return true;
        }
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜
    /// </summary>
    private List<BuildingPartData> SortParts(List<BuildingPartData> parts)
    {
        switch (m_CurrentNameSort)
        {
            case "NAME_ASC":
                parts.Sort((a, b) => a.DisplayName.CompareTo(b.DisplayName));
                break;
            case "NAME_DESC":
                parts.Sort((a, b) => b.DisplayName.CompareTo(a.DisplayName));
                break;
            case "POWER_ASC":
                parts.Sort((a, b) => a.PowerPercent.CompareTo(b.PowerPercent));
                break;
            case "POWER_DESC":
                parts.Sort((a, b) => b.PowerPercent.CompareTo(a.PowerPercent));
                break;
        }
        return parts;
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜ ˜˜˜˜ ˜˜˜˜˜˜˜˜
    /// </summary>
    public void RefreshCards()
    {
        ClearCards();

        // ˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜˜
        List<BuildingPartData> filteredParts = new List<BuildingPartData>();

        foreach (var part in allParts)
        {
            if (part == null) continue;

            // ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜
            bool categoryMatch = m_CurrentCategory == "ALL" || part.Category == m_CurrentCategory;

            // ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜
            bool powerMatch = PassesPowerFilter(part);

            if (categoryMatch && powerMatch)
            {
                filteredParts.Add(part);
            }
        }

        // ˜˜˜˜˜˜˜˜˜
        filteredParts = SortParts(filteredParts);

        // ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜
        foreach (var partData in filteredParts)
        {
            CreateCard(partData);
        }
    }

    /// <summary>
    /// ˜˜˜˜˜˜ ˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜
    /// </summary>
    private void CreateCard(BuildingPartData partData)
    {
        GameObject card = Instantiate(cardPrefab, contentParent);

        PCComponent componentData = partData.ComponentData;
        if (componentData == null && partData.PartPrefab != null)
        {
            componentData = partData.PartPrefab.GetComponent<PCComponent>();
        }

        // Drag item receives data only from BuildMenuUI.
        UIComponentDragItem dragItem = card.GetComponent<UIComponentDragItem>();
        if (dragItem == null)
        {
            dragItem = card.AddComponent<UIComponentDragItem>();
        }
        dragItem.InitializeFromMenu(dragController, componentData, partData.PartPrefab);

        // ˜˜˜˜˜˜˜˜
        Transform nameTransform = card.transform.Find("Name");
        if (nameTransform != null)
        {
            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = partData.DisplayName;
        }

        // ˜˜˜˜˜˜
        Transform iconTransform = card.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null) iconImage.sprite = partData.Icon;
        }

        // ˜˜˜˜˜˜˜˜
        Transform descTransform = card.transform.Find("Description");
        if (descTransform != null)
        {
            TMP_Text descText = descTransform.GetComponent<TMP_Text>();
            if (descText != null) descText.text = partData.Description;
        }

        // ˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜
        Transform powerTransform = card.transform.Find("PowerText");
        if (powerTransform != null)
        {
            TMP_Text powerText = powerTransform.GetComponent<TMP_Text>();
            if (powerText != null) powerText.text = $"{partData.PowerPercent}%";
        }

        // ˜˜˜˜˜˜ "˜˜˜˜˜˜˜˜˜˜˜˜"
        Transform buttonTransform = card.transform.Find("UseButton");
        if (buttonTransform != null)
        {
            Button useBtn = buttonTransform.GetComponent<Button>();
            if (useBtn != null)
            {
                useBtn.onClick.RemoveAllListeners();
            }

            UIComponentDragItem buttonDragItem = buttonTransform.GetComponent<UIComponentDragItem>();
            if (buttonDragItem == null)
            {
                buttonDragItem = buttonTransform.gameObject.AddComponent<UIComponentDragItem>();
            }
            buttonDragItem.InitializeFromMenu(dragController, componentData, partData.PartPrefab);
        }

        spawnedCards.Add(card);
    }

    private void SelectPart(BuildingPart part)
    {
        if (part == null) return;

        BuildingPlacer placer = BuildingPlacer.Instance;
        if (placer == null) return;

        placer.SelectBuildingPart(part);
        placer.ChangeBuildMode(BuildingPlacer.BuildMode.PLACE);

        BuildMenuController menuController = GetComponent<BuildMenuController>();
        if (menuController != null)
        {
            menuController.CloseMenu();
        }
    }

    private void ClearCards()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        spawnedCards.Clear();
    }

    [System.Serializable]
    public class BuildingPartData
    {
        public string DisplayName;
        public string Category; // "CPU", "GPU", "RAM", "PSU", "MOTHERBOARD", "CASE"
        public Sprite Icon;
        public string Description;
        public float PowerPercent;
        public BuildingPart PartPrefab;
        public PCComponent ComponentData;
    }
}