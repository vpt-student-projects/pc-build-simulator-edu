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

    public PCComponent Case
    {
        get
        {
            PruneMissingReferences();
            return caseComponent;
        }
    }
    public PCComponent Motherboard => motherboard;
    public PCComponent Psu => psu;
    public PCComponent Cpu => cpu;
    public PCComponent Gpu => gpu;
    public int InstalledRamCount
    {
        get
        {
            PruneMissingReferences();
            return installedRamCount;
        }
    }
    public int InstalledStorageCount
    {
        get
        {
            PruneMissingReferences();
            return installedStorageCount;
        }
    }
    public System.Collections.Generic.IReadOnlyList<PCComponent> InstalledRam
    {
        get
        {
            PruneMissingReferences();
            return installedRam;
        }
    }
    public System.Collections.Generic.IReadOnlyList<PCComponent> InstalledStorage
    {
        get
        {
            PruneMissingReferences();
            return installedStorage;
        }
    }
    public System.Collections.Generic.IReadOnlyList<PCComponent> InstalledCoolers
    {
        get
        {
            PruneMissingReferences();
            return installedCoolers;
        }
    }

    public void RegisterInstalled(PCComponent component)
    {
        PruneMissingReferences();
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
        PruneMissingReferences();
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
        PruneMissingReferences();
        return cpu != null &&
               installedRamCount > 0 &&
               psu != null &&
               gpu != null &&
               installedStorageCount > 0;
    }

    public bool CanInstallCase()
    {
        PruneMissingReferences();
        return caseComponent == null;
    }

    public System.Collections.Generic.List<PCComponent> GetInstalledComponentsSnapshot()
    {
        PruneMissingReferences();
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

    private void PruneMissingReferences()
    {
        installedRam.RemoveAll(item => item == null);
        installedStorage.RemoveAll(item => item == null);
        installedCoolers.RemoveAll(item => item == null);
        installedRamCount = installedRam.Count;
        installedStorageCount = installedStorage.Count;

        if (caseComponent == null)
        {
            caseComponent = null;
        }
        if (motherboard == null)
        {
            motherboard = null;
        }
        if (psu == null)
        {
            psu = null;
        }
        if (cpu == null)
        {
            cpu = null;
        }
        if (gpu == null)
        {
            gpu = null;
        }
    }
}
