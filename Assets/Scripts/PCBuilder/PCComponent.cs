using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Универсальный компонент ПК: данные приходят из каталога (БД/JSON), визуал подставляется по model tier.
/// </summary>
public class PCComponent : MonoBehaviour
{
    [Header("Catalog identity")]
    [SerializeField] private int databaseId;
    [SerializeField] private string categoryCode = string.Empty;
    [SerializeField] private PCComponentType componentType = PCComponentType.None;
    [SerializeField] private int modelTier = 1;
    [SerializeField] private string displayName = string.Empty;
    [SerializeField] private string vendor = string.Empty;
    [SerializeField] private string model = string.Empty;
    [SerializeField] private string description = string.Empty;
    [SerializeField] private int priceMinorUnits;
    [SerializeField] private int powerWatts;
    [SerializeField] private string iconPath = string.Empty;

    [Header("Slots (optional — пусто = любой подходящий по типу)")]
    [SerializeField] private List<PCSlotType> compatibleSockets = new List<PCSlotType>();

    [Header("Install state")]
    [SerializeField] private bool isInstalled;
    [SerializeField] private BuildSlot parentSlot;

    [Header("Compatibility (строки как в PostgreSQL)")]
    [SerializeField] private string socketCode = string.Empty;
    [SerializeField] private string ramTypeCode = string.Empty;
    [SerializeField] private int ramSlotsCount;
    [SerializeField] private int requiredPSUPower;
    [SerializeField] private int psuPower;
    [SerializeField] private int gpuTdpW;

    public int DatabaseId => databaseId;
    public string CategoryCode => categoryCode;
    public PCComponentType ComponentType => componentType;
    public int ModelTier => modelTier;
    public string DisplayName => displayName;
    public string Vendor => vendor;
    public string Model => model;
    public string Description => description;
    public int PriceMinorUnits => priceMinorUnits;
    public int PowerWatts => powerWatts;
    public string IconPath => iconPath;
    public IReadOnlyList<PCSlotType> CompatibleSockets => compatibleSockets;
    public bool IsInstalled => isInstalled;
    public BuildSlot ParentSlot => parentSlot;

    public string SocketCode => socketCode;
    public string RamTypeCode => ramTypeCode;
    public int RamSlotsCount => ramSlotsCount;
    public int RequiredPSUPower => requiredPSUPower;
    public int PsuPower => psuPower;
    public int GpuTdpW => gpuTdpW;

    [System.Obsolete("Use SocketCode for DB-driven compatibility")]
    public SocketType Socket => ParseSocketType(socketCode);
    [System.Obsolete("Use RamTypeCode for DB-driven compatibility")]
    public RamType Ram => ParseRamType(ramTypeCode);

    public bool IsCompatibleWith(PCSlotType slotType)
    {
        if (compatibleSockets == null || compatibleSockets.Count == 0)
        {
            return true;
        }

        return compatibleSockets.Contains(slotType);
    }

    public void MarkInstalled(BuildSlot slot)
    {
        parentSlot = slot;
        isInstalled = slot != null;
    }

    public void ApplyFromData(PcComponentData data)
    {
        if (data == null)
        {
            return;
        }

        databaseId = data.DatabaseId;
        categoryCode = data.CategoryCode ?? string.Empty;
        componentType = PcComponentData.CategoryToComponentType(categoryCode);
        modelTier = Mathf.Clamp(data.ModelTier, 1, 3);
        displayName = data.Name ?? string.Empty;
        vendor = data.Vendor ?? string.Empty;
        model = data.Model ?? string.Empty;
        description = data.Description ?? string.Empty;
        priceMinorUnits = (int)(data.Price * 100m);
        powerWatts = data.PowerWatts;
        iconPath = data.IconPath ?? string.Empty;

        socketCode = data.Socket ?? string.Empty;
        ramTypeCode = data.RamType ?? string.Empty;
        ramSlotsCount = data.RamSlots;
        requiredPSUPower = data.RequiredPsuW > 0 ? data.RequiredPsuW : data.GpuTdpW;
        psuPower = data.PsuWattage;
        gpuTdpW = data.GpuTdpW;
    }

    public void CopyFrom(PCComponent template)
    {
        if (template == null)
        {
            return;
        }

        databaseId = template.databaseId;
        categoryCode = template.categoryCode;
        componentType = template.componentType;
        modelTier = template.modelTier;
        displayName = template.displayName;
        vendor = template.vendor;
        model = template.model;
        description = template.description;
        priceMinorUnits = template.priceMinorUnits;
        powerWatts = template.powerWatts;
        iconPath = template.iconPath;
        compatibleSockets = new List<PCSlotType>(template.compatibleSockets);
        socketCode = template.socketCode;
        ramTypeCode = template.ramTypeCode;
        ramSlotsCount = template.ramSlotsCount;
        requiredPSUPower = template.requiredPSUPower;
        psuPower = template.psuPower;
        gpuTdpW = template.gpuTdpW;
    }

    private static SocketType ParseSocketType(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return SocketType.None;
        }

        switch (code.Trim().ToUpperInvariant())
        {
            case "AM4": return SocketType.AM4;
            case "AM5": return SocketType.AM5;
            case "LGA1200": return SocketType.LGA1200;
            case "LGA1700": return SocketType.LGA1700;
            default: return SocketType.None;
        }
    }

    private static RamType ParseRamType(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return RamType.None;
        }

        switch (code.Trim().ToUpperInvariant())
        {
            case "DDR3": return RamType.DDR3;
            case "DDR4": return RamType.DDR4;
            case "DDR5": return RamType.DDR5;
            default: return RamType.None;
        }
    }

    public PcComponentData ToData()
    {
        return new PcComponentData
        {
            DatabaseId = databaseId,
            CategoryCode = categoryCode,
            Name = displayName,
            Vendor = vendor,
            Model = model,
            Description = description,
            Price = priceMinorUnits / 100m,
            PowerWatts = powerWatts,
            ModelTier = modelTier,
            IconPath = iconPath,
            Socket = socketCode,
            RamType = ramTypeCode,
            RamSlots = ramSlotsCount,
            RequiredPsuW = requiredPSUPower,
            PsuWattage = psuPower,
            GpuTdpW = gpuTdpW
        };
    }
}
