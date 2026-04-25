using System;
using System.Collections.Generic;
using EasyBuildSystem.Features.Runtime.Buildings.Part;
using UnityEngine;

/// <summary>
/// Карта: код категории из БД → префаб BuildingPart для сборки + до 3 визуальных моделей (model_tier 1..3).
/// </summary>
[CreateAssetMenu(fileName = "PcPrefabCatalogMap", menuName = "PC Builder/Prefab Catalog Map")]
public class PcPrefabCatalogMap : ScriptableObject
{
    [Serializable]
    public class CategoryEntry
    {
        [Tooltip("Код из component_categories.code, например CPU, MOTHERBOARD")]
        public string categoryCode = "CPU";
        [Tooltip("Префаб с BuildingPart + корнем для установки в слот")]
        public BuildingPart buildingPrefab;
        [Tooltip("Визуал tier 1 — подставляется под дочерний Model")]
        public GameObject visualModelTier1;
        public GameObject visualModelTier2;
        public GameObject visualModelTier3;
        [Tooltip("Имя дочернего Transform под корнём префаба, куда вставляются меши (по умолчанию Model)")]
        public string modelChildName = "Model";
    }

    [SerializeField] private List<CategoryEntry> entries = new List<CategoryEntry>();

    public BuildingPart GetBuildingPrefab(string categoryCode)
    {
        CategoryEntry e = FindEntry(categoryCode);
        return e != null ? e.buildingPrefab : null;
    }

    public GameObject GetVisualModelForTier(string categoryCode, int modelTier)
    {
        CategoryEntry e = FindEntry(categoryCode);
        if (e == null)
        {
            return null;
        }

        switch (Mathf.Clamp(modelTier, 1, 3))
        {
            case 1: return e.visualModelTier1;
            case 2: return e.visualModelTier2;
            case 3: return e.visualModelTier3;
            default: return e.visualModelTier1;
        }
    }

    public string GetModelChildName(string categoryCode)
    {
        CategoryEntry e = FindEntry(categoryCode);
        return string.IsNullOrEmpty(e?.modelChildName) ? "Model" : e.modelChildName;
    }

    private CategoryEntry FindEntry(string categoryCode)
    {
        if (string.IsNullOrEmpty(categoryCode))
        {
            return null;
        }

        string key = categoryCode.Trim().ToUpperInvariant();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null &&
                string.Equals(entries[i].categoryCode?.Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                return entries[i];
            }
        }

        return null;
    }
}
