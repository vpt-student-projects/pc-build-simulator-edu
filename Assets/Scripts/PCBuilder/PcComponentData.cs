using System;
using UnityEngine;

/// <summary>
/// Снимок данных комплектующего из каталога (БД / JSON). Не требует префаба с заполненным PCComponent.
/// </summary>
[Serializable]
public class PcComponentData
{
    public int DatabaseId;
    public string CategoryCode = string.Empty;
    public string Name = string.Empty;
    public string Vendor = string.Empty;
    public string Model = string.Empty;
    public string Description = string.Empty;
    public decimal Price;
    public int PowerWatts;
    public int ModelTier = 1;
    public string IconPath = string.Empty;

    public string Socket = string.Empty;
    public string RamType = string.Empty;
    public int RamSlots;
    public int MaxRamGb;
    public int RequiredPsuW;
    public int PsuWattage;
    public int GpuTdpW;
    public string StorageType = string.Empty;
    public int CapacityGb;
    public string CoolerSocketsCsv = string.Empty;

    public static PCComponentType CategoryToComponentType(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return PCComponentType.None;
        }

        switch (code.Trim().ToUpperInvariant())
        {
            case "CASE": return PCComponentType.Case;
            case "CPU": return PCComponentType.CPU;
            case "GPU": return PCComponentType.GPU;
            case "RAM": return PCComponentType.RAM;
            case "PSU": return PCComponentType.PSU;
            case "MOTHERBOARD": return PCComponentType.Motherboard;
            case "STORAGE": return PCComponentType.Storage;
            case "CPU_COOLER": return PCComponentType.CPUFan;
            default:
                Debug.LogWarning($"Unknown category code: {code}");
                return PCComponentType.None;
        }
    }
}
