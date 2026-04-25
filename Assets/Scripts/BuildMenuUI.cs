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

    [Header("Catalog (PostgreSQL ? JSON)")]
    [SerializeField] private ComponentCatalogService catalogService;
    [SerializeField] private PcPrefabCatalogMap prefabCatalogMap;
    [Tooltip("???? ? ???????? ???? ?????? ? ?????? ????? ???????? ù ???? ???????? ?? ??/JSON, ????? ?? ?????? ????.")]
    [SerializeField] private bool preferCatalogOverManualList = true;

    [Header("Legacy: ?????? ?????? (???? ??????? ????)")]
    [SerializeField] private List<BuildingPartData> allParts = new List<BuildingPartData>();

    [Header("Drag & Drop")]
    [SerializeField] private BuildModeDragController dragController;

    private Dictionary<Toggle, string> m_ToggleCategories = new Dictionary<Toggle, string>();

    private string m_CurrentCategory = "ALL";
    private string m_CurrentPowerFilter = "ALL";
    private string m_CurrentNameSort = "DEFAULT";

    private List<GameObject> spawnedCards = new List<GameObject>();

    private void Start()
    {
        if (cardPrefab == null || contentParent == null || tabsGroup == null)
        {
            Debug.LogError("BuildMenuUI: ?? ??? ?????? ????????? ? ??????????.");
            return;
        }

        if (catalogService == null)
        {
            catalogService = FindFirstObjectByType<ComponentCatalogService>();
        }

        SetupToggles();

        if (powerFilterDropdown != null)
        {
            powerFilterDropdown.ClearOptions();
            powerFilterDropdown.AddOptions(new List<string> { "???", "0-25%", "25-50%", "50-75%", "75-100%" });
            powerFilterDropdown.onValueChanged.AddListener(OnPowerFilterChanged);
        }

        if (nameFilterDropdown != null)
        {
            nameFilterDropdown.ClearOptions();
            nameFilterDropdown.AddOptions(new List<string> { "?? ?????????", "?? ???????? (?-?)", "?? ???????? (?-?)", "?? ???????? (????.)", "?? ???????? (????.)" });
            nameFilterDropdown.onValueChanged.AddListener(OnNameSortChanged);
        }

        RefreshCards();
    }

    private bool UseCatalog()
    {
        return preferCatalogOverManualList &&
               catalogService != null &&
               prefabCatalogMap != null &&
               catalogService.Items != null &&
               catalogService.Items.Count > 0;
    }

    private void SetupToggles()
    {
        m_ToggleCategories.Clear();

        foreach (Transform child in tabsGroup.transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                string category = GetCategoryFromToggleName(child.name);
                m_ToggleCategories[toggle] = category;

                toggle.onValueChanged.AddListener((isOn) => {
                    if (isOn)
                    {
                        OnCategoryChanged(toggle);
                    }
                });

                toggle.group = tabsGroup;
            }
        }

        foreach (var kvp in m_ToggleCategories)
        {
            if (kvp.Value == "ALL")
            {
                kvp.Key.isOn = true;
                break;
            }
        }
    }

    private string GetCategoryFromToggleName(string toggleName)
    {
        string n = toggleName.ToLowerInvariant().Trim();
        switch (n)
        {
            case "???":
            case "all":
                return "ALL";
            case "????":
            case "cpu":
                return "CPU";
            case "??":
            case "ram":
                return "RAM";
            case "?":
            case "psu":
                return "PSU";
            case "?????????":
            case "motherboard":
                return "MOTHERBOARD";
            case "??????":
            case "case":
                return "CASE";
            case "??????????":
            case "gpu":
                return "GPU";
            case "????????":
            case "storage":
                return "STORAGE";
            case "?????":
            case "cpu_cooler":
            case "cooler":
                return "CPU_COOLER";
            default:
                Debug.LogWarning($"BuildMenuUI: ??????????? ??? ??????? '{toggleName}', ???????????? ??? ??? ?????????.");
                return toggleName.ToUpperInvariant();
        }
    }

    private void OnCategoryChanged(Toggle activeToggle)
    {
        if (m_ToggleCategories.TryGetValue(activeToggle, out string category))
        {
            m_CurrentCategory = category;
            RefreshCards();
        }
    }

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

    private static float WattsToUiPercent(int watts)
    {
        return Mathf.Clamp(watts / 4f, 0f, 100f);
    }

    private bool PassesPowerFilter(BuildingPartData part)
    {
        if (m_CurrentPowerFilter == "ALL")
        {
            return true;
        }

        float power = part.PowerPercent;
        return PassesPowerPercentBucket(power);
    }

    private bool PassesPowerFilter(PcComponentData part)
    {
        if (m_CurrentPowerFilter == "ALL")
        {
            return true;
        }

        float power = WattsToUiPercent(part.PowerWatts);
        return PassesPowerPercentBucket(power);
    }

    private bool PassesPowerPercentBucket(float power)
    {
        switch (m_CurrentPowerFilter)
        {
            case "0-25": return power >= 0f && power <= 25f;
            case "25-50": return power > 25f && power <= 50f;
            case "50-75": return power > 50f && power <= 75f;
            case "75-100": return power > 75f && power <= 100f;
            default: return true;
        }
    }

    private List<BuildingPartData> SortParts(List<BuildingPartData> parts)
    {
        switch (m_CurrentNameSort)
        {
            case "NAME_ASC":
                parts.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase));
                break;
            case "NAME_DESC":
                parts.Sort((a, b) => string.Compare(b.DisplayName, a.DisplayName, System.StringComparison.OrdinalIgnoreCase));
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

    private List<PcComponentData> SortCatalogParts(List<PcComponentData> parts)
    {
        switch (m_CurrentNameSort)
        {
            case "NAME_ASC":
                parts.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
                break;
            case "NAME_DESC":
                parts.Sort((a, b) => string.Compare(b.Name, a.Name, System.StringComparison.OrdinalIgnoreCase));
                break;
            case "POWER_ASC":
                parts.Sort((a, b) => a.PowerWatts.CompareTo(b.PowerWatts));
                break;
            case "POWER_DESC":
                parts.Sort((a, b) => b.PowerWatts.CompareTo(a.PowerWatts));
                break;
        }
        return parts;
    }

    public void RefreshCards()
    {
        ClearCards();

        if (UseCatalog())
        {
            List<PcComponentData> filtered = new List<PcComponentData>();
            foreach (PcComponentData part in catalogService.Items)
            {
                if (part == null)
                {
                    continue;
                }

                bool categoryMatch = m_CurrentCategory == "ALL" ||
                    string.Equals(part.CategoryCode, m_CurrentCategory, System.StringComparison.OrdinalIgnoreCase);
                if (!categoryMatch || !PassesPowerFilter(part))
                {
                    continue;
                }

                filtered.Add(part);
            }

            filtered = SortCatalogParts(filtered);
            foreach (PcComponentData partData in filtered)
            {
                CreateCardFromCatalog(partData);
            }

            if (filtered.Count == 0)
            {
                Debug.LogWarning("BuildMenuUI: ??????? ????????, ?? ????? ???????? ??? ?????. ????????? ???? ????????? ? ???????.");
            }

            return;
        }

        List<BuildingPartData> filteredParts = new List<BuildingPartData>();
        foreach (BuildingPartData part in allParts)
        {
            if (part == null)
            {
                continue;
            }

            bool categoryMatch = m_CurrentCategory == "ALL" || part.Category == m_CurrentCategory;
            if (categoryMatch && PassesPowerFilter(part))
            {
                filteredParts.Add(part);
            }
        }

        filteredParts = SortParts(filteredParts);
        foreach (BuildingPartData partData in filteredParts)
        {
            CreateCardFromInspector(partData);
        }
    }

    private void CreateCardFromCatalog(PcComponentData data)
    {
        BuildingPart prefab = prefabCatalogMap.GetBuildingPrefab(data.CategoryCode);
        if (prefab == null)
        {
            Debug.LogWarning($"BuildMenuUI: ??? ??????? ? PcPrefabCatalogMap ??? ????????? '{data.CategoryCode}' (????????? id={data.DatabaseId}).");
            return;
        }

        GameObject card = Instantiate(cardPrefab, contentParent);
        GameObject holder = new GameObject("RuntimePC_" + data.DatabaseId);
        holder.transform.SetParent(card.transform, false);
        PCComponent runtimePc = holder.AddComponent<PCComponent>();
        runtimePc.ApplyFromData(data);

        UIComponentDragItem dragItem = card.GetComponent<UIComponentDragItem>();
        if (dragItem == null)
        {
            dragItem = card.AddComponent<UIComponentDragItem>();
        }
        dragItem.InitializeFromMenu(dragController, runtimePc, prefab);

        ApplyCardVisuals(card, data.Name, data.Description, WattsToUiPercent(data.PowerWatts), null);

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
            buttonDragItem.InitializeFromMenu(dragController, runtimePc, prefab);
        }

        spawnedCards.Add(card);
    }

    private void CreateCardFromInspector(BuildingPartData partData)
    {
        GameObject card = Instantiate(cardPrefab, contentParent);

        PCComponent componentData = partData.ComponentData;
        if (componentData == null && partData.PartPrefab != null)
        {
            componentData = partData.PartPrefab.GetComponent<PCComponent>();
        }

        if (componentData == null && partData.PartPrefab != null)
        {
            GameObject holder = new GameObject("LegacyPC");
            holder.transform.SetParent(card.transform, false);
            componentData = holder.AddComponent<PCComponent>();
            componentData.ApplyFromData(new PcComponentData
            {
                CategoryCode = partData.Category,
                Name = partData.DisplayName,
                Description = partData.Description,
                PowerWatts = Mathf.RoundToInt(partData.PowerPercent * 4f),
                ModelTier = 1,
                Price = 0
            });
        }

        UIComponentDragItem dragItem = card.GetComponent<UIComponentDragItem>();
        if (dragItem == null)
        {
            dragItem = card.AddComponent<UIComponentDragItem>();
        }
        dragItem.InitializeFromMenu(dragController, componentData, partData.PartPrefab);

        ApplyCardVisuals(card, partData.DisplayName, partData.Description, partData.PowerPercent, partData.Icon);

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

    private static void ApplyCardVisuals(GameObject card, string title, string description, float powerPercent, Sprite icon)
    {
        Transform nameTransform = card.transform.Find("Name");
        if (nameTransform != null)
        {
            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = title;
            }
        }

        Transform iconTransform = card.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
        }

        Transform descTransform = card.transform.Find("Description");
        if (descTransform != null)
        {
            TMP_Text descText = descTransform.GetComponent<TMP_Text>();
            if (descText != null)
            {
                descText.text = description;
            }
        }

        Transform powerTransform = card.transform.Find("PowerText");
        if (powerTransform != null)
        {
            TMP_Text powerText = powerTransform.GetComponent<TMP_Text>();
            if (powerText != null)
            {
                powerText.text = $"{Mathf.RoundToInt(powerPercent)}%";
            }
        }
    }

    private void ClearCards()
    {
        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        spawnedCards.Clear();
    }

    [System.Serializable]
    public class BuildingPartData
    {
        public string DisplayName;
        public string Category;
        public Sprite Icon;
        public string Description;
        public float PowerPercent;
        public BuildingPart PartPrefab;
        public PCComponent ComponentData;
    }
}
