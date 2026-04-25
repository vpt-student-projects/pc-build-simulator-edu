using UnityEngine;

public class PCAssemblyState : MonoBehaviour
{
    [SerializeField] private PCComponent caseComponent;
    [SerializeField] private PCComponent motherboard;
    [SerializeField] private PCComponent psu;
    [SerializeField] private PCComponent cpu;
    [SerializeField] private PCComponent gpu;
    [SerializeField] private int installedRamCount;
    [SerializeField] private int installedStorageCount;
    [SerializeField] private System.Collections.Generic.List<PCComponent> installedRam = new System.Collections.Generic.List<PCComponent>();
    [SerializeField] private System.Collections.Generic.List<PCComponent> installedStorage = new System.Collections.Generic.List<PCComponent>();
    [SerializeField] private System.Collections.Generic.List<PCComponent> installedCoolers = new System.Collections.Generic.List<PCComponent>();

    public PCComponent Case => caseComponent;
    public PCComponent Motherboard => motherboard;
    public PCComponent Psu => psu;
    public PCComponent Cpu => cpu;
    public PCComponent Gpu => gpu;
    public int InstalledRamCount => installedRamCount;
    public int InstalledStorageCount => installedStorageCount;
    public System.Collections.Generic.IReadOnlyList<PCComponent> InstalledRam => installedRam;
    public System.Collections.Generic.IReadOnlyList<PCComponent> InstalledStorage => installedStorage;
    public System.Collections.Generic.IReadOnlyList<PCComponent> InstalledCoolers => installedCoolers;

    public void RegisterInstalled(PCComponent component)
    {
        if (component == null)
        {
            return;
        }

        switch (component.ComponentType)
        {
            case PCComponentType.Case:
                caseComponent = component;
                break;
            case PCComponentType.Motherboard:
                motherboard = component;
                break;
            case PCComponentType.PSU:
                psu = component;
                break;
            case PCComponentType.CPU:
                cpu = component;
                break;
            case PCComponentType.GPU:
                gpu = component;
                break;
            case PCComponentType.RAM:
                if (!installedRam.Contains(component))
                {
                    installedRam.Add(component);
                }
                installedRamCount = installedRam.Count;
                break;
            case PCComponentType.Storage:
                if (!installedStorage.Contains(component))
                {
                    installedStorage.Add(component);
                }
                installedStorageCount = installedStorage.Count;
                break;
            case PCComponentType.CPUFan:
                if (!installedCoolers.Contains(component))
                {
                    installedCoolers.Add(component);
                }
                break;
        }
    }

    public void RegisterRemoved(PCComponent component)
    {
        if (component == null)
        {
            return;
        }

        switch (component.ComponentType)
        {
            case PCComponentType.Case:
                if (caseComponent == component) caseComponent = null;
                break;
            case PCComponentType.Motherboard:
                if (motherboard == component) motherboard = null;
                break;
            case PCComponentType.PSU:
                if (psu == component) psu = null;
                break;
            case PCComponentType.CPU:
                if (cpu == component) cpu = null;
                break;
            case PCComponentType.GPU:
                if (gpu == component) gpu = null;
                break;
            case PCComponentType.RAM:
                installedRam.Remove(component);
                installedRamCount = installedRam.Count;
                break;
            case PCComponentType.Storage:
                installedStorage.Remove(component);
                installedStorageCount = installedStorage.Count;
                break;
            case PCComponentType.CPUFan:
                installedCoolers.Remove(component);
                break;
        }
    }

    public bool IsAssemblyComplete()
    {
        return cpu != null &&
               installedRamCount > 0 &&
               psu != null &&
               gpu != null &&
               installedStorageCount > 0;
    }

    public System.Collections.Generic.List<PCComponent> GetInstalledComponentsSnapshot()
    {
        var list = new System.Collections.Generic.List<PCComponent>(16);
        if (caseComponent != null) list.Add(caseComponent);
        if (motherboard != null) list.Add(motherboard);
        if (psu != null) list.Add(psu);
        if (cpu != null) list.Add(cpu);
        if (gpu != null) list.Add(gpu);

        for (int i = 0; i < installedRam.Count; i++)
        {
            if (installedRam[i] != null) list.Add(installedRam[i]);
        }

        for (int i = 0; i < installedStorage.Count; i++)
        {
            if (installedStorage[i] != null) list.Add(installedStorage[i]);
        }

        for (int i = 0; i < installedCoolers.Count; i++)
        {
            if (installedCoolers[i] != null) list.Add(installedCoolers[i]);
        }

        return list;
    }
}
