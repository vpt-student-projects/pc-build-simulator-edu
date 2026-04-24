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

    public PCComponent Case => caseComponent;
    public PCComponent Motherboard => motherboard;
    public PCComponent Psu => psu;
    public PCComponent Cpu => cpu;
    public PCComponent Gpu => gpu;
    public int InstalledRamCount => installedRamCount;
    public int InstalledStorageCount => installedStorageCount;

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
                installedRamCount++;
                break;
            case PCComponentType.Storage:
                installedStorageCount++;
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
                installedRamCount = Mathf.Max(0, installedRamCount - 1);
                break;
            case PCComponentType.Storage:
                installedStorageCount = Mathf.Max(0, installedStorageCount - 1);
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
}
