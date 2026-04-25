using UnityEngine;

public static class PCCompatibilityService
{
    public static bool CanPlaceComponent(PCComponent component, BuildSlot slot)
    {
        if (component == null || slot == null)
        {
            return false;
        }

        PCAssemblyState assembly = Object.FindFirstObjectByType<PCAssemblyState>();
        if (assembly == null)
        {
            return true;
        }

        switch (component.ComponentType)
        {
            case PCComponentType.CPU:
                return ValidateCpu(component, assembly);
            case PCComponentType.RAM:
                return ValidateRam(component, assembly);
            case PCComponentType.GPU:
                return ValidateGpu(component, assembly);
            default:
                return true;
        }
    }

    private static bool ValidateCpu(PCComponent cpu, PCAssemblyState assembly)
    {
        if (assembly.Motherboard == null)
        {
            return false;
        }

        return SocketEquals(cpu.SocketCode, assembly.Motherboard.SocketCode);
    }

    private static bool ValidateRam(PCComponent ram, PCAssemblyState assembly)
    {
        if (assembly.Motherboard == null)
        {
            return false;
        }

        if (!RamEquals(ram.RamTypeCode, assembly.Motherboard.RamTypeCode))
        {
            return false;
        }

        return assembly.InstalledRamCount < assembly.Motherboard.RamSlotsCount;
    }

    private static bool ValidateGpu(PCComponent gpu, PCAssemblyState assembly)
    {
        if (assembly.Psu == null)
        {
            return false;
        }

        int need = gpu.RequiredPSUPower > 0 ? gpu.RequiredPSUPower : gpu.GpuTdpW;
        return need <= assembly.Psu.PsuPower;
    }

    private static bool SocketEquals(string a, string b)
    {
        return string.Equals(Normalize(a), Normalize(b), System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool RamEquals(string a, string b)
    {
        return string.Equals(Normalize(a), Normalize(b), System.StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();
    }
}
