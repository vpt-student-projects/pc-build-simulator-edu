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

        return cpu.Socket == assembly.Motherboard.Socket;
    }

    private static bool ValidateRam(PCComponent ram, PCAssemblyState assembly)
    {
        if (assembly.Motherboard == null)
        {
            return false;
        }

        if (ram.Ram != assembly.Motherboard.Ram)
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

        return gpu.RequiredPSUPower <= assembly.Psu.PsuPower;
    }
}
