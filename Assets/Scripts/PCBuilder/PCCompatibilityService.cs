using UnityEngine;

public static class PCCompatibilityService
{
    public static bool CanPlaceComponent(PCComponent component, BuildSlot slot)
    {
        return TryGetCompatibilityError(component, slot, out _);
    }

    public static bool TryGetCompatibilityError(PCComponent component, BuildSlot slot, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (component == null || slot == null)
        {
            errorMessage = "Не удалось определить компонент или слот для установки.";
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
                return ValidateCpu(component, assembly, out errorMessage);
            case PCComponentType.RAM:
                return ValidateRam(component, assembly, out errorMessage);
            case PCComponentType.GPU:
                return ValidateGpu(component, assembly, out errorMessage);
            case PCComponentType.CPUFan:
                return ValidateCpuCooler(component, assembly, out errorMessage);
            default:
                return true;
        }
    }

    private static bool ValidateCpu(PCComponent cpu, PCAssemblyState assembly, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (assembly.Motherboard == null)
        {
            errorMessage = $"Сначала установите материнскую плату, а затем процессор {Display(cpu)}.";
            return false;
        }

        if (!SocketEquals(cpu.SocketCode, assembly.Motherboard.SocketCode))
        {
            errorMessage =
                $"На материнской плате {Display(assembly.Motherboard)} стоит сокет {ValueOrUnknown(assembly.Motherboard.SocketCode)}, " +
                $"а вы пытаетесь вставить {Display(cpu)} с сокетом {ValueOrUnknown(cpu.SocketCode)}.";
            return false;
        }

        return true;
    }

    private static bool ValidateRam(PCComponent ram, PCAssemblyState assembly, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (assembly.Motherboard == null)
        {
            errorMessage = $"Сначала установите материнскую плату, а затем оперативную память {Display(ram)}.";
            return false;
        }

        if (!RamEquals(ram.RamTypeCode, assembly.Motherboard.RamTypeCode))
        {
            errorMessage =
                $"Материнская плата {Display(assembly.Motherboard)} поддерживает память {ValueOrUnknown(assembly.Motherboard.RamTypeCode)}, " +
                $"а модуль {Display(ram)} имеет тип {ValueOrUnknown(ram.RamTypeCode)}.";
            return false;
        }

        if (assembly.InstalledRamCount >= assembly.Motherboard.RamSlotsCount)
        {
            errorMessage =
                $"На плате {Display(assembly.Motherboard)} закончились слоты ОЗУ: " +
                $"{assembly.InstalledRamCount}/{Mathf.Max(0, assembly.Motherboard.RamSlotsCount)} занято.";
            return false;
        }

        return true;
    }

    private static bool ValidateGpu(PCComponent gpu, PCAssemblyState assembly, out string errorMessage)
    {
        // По задаче: не блокируем установку GPU из-за нехватки питания.
        errorMessage = string.Empty;
        return true;
    }

    private static bool ValidateCpuCooler(PCComponent cooler, PCAssemblyState assembly, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (assembly.Cpu == null && assembly.Motherboard == null)
        {
            errorMessage = $"Сначала установите материнскую плату или процессор, затем кулер {Display(cooler)}.";
            return false;
        }

        string targetSocket = assembly.Cpu != null ? assembly.Cpu.SocketCode : assembly.Motherboard.SocketCode;
        if (string.IsNullOrWhiteSpace(targetSocket) || string.IsNullOrWhiteSpace(cooler.SocketCode))
        {
            return true;
        }

        string[] supported = cooler.SocketCode.Split(',');
        for (int i = 0; i < supported.Length; i++)
        {
            if (SocketEquals(supported[i], targetSocket))
            {
                return true;
            }
        }

        errorMessage =
            $"Кулер {Display(cooler)} поддерживает сокеты {cooler.SocketCode}, " +
            $"а в сборке используется сокет {ValueOrUnknown(targetSocket)}.";
        return false;
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

    private static string Display(PCComponent component)
    {
        if (component == null)
        {
            return "неизвестный компонент";
        }

        if (!string.IsNullOrWhiteSpace(component.DisplayName))
        {
            return component.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(component.Model))
        {
            return component.Model;
        }

        return component.ComponentType.ToString();
    }

    private static string ValueOrUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "не указан" : value.Trim();
    }
}
