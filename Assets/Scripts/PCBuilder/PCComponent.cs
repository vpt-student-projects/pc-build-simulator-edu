using System.Collections.Generic;
using UnityEngine;

public class PCComponent : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private PCComponentType componentType = PCComponentType.None;
    [SerializeField] private List<PCSlotType> compatibleSockets = new List<PCSlotType>();

    [Header("Install State")]
    [SerializeField] private bool isInstalled;
    [SerializeField] private BuildSlot parentSlot;

    [Header("Compatibility (Basic DB fields)")]
    [SerializeField] private SocketType socketType = SocketType.None;
    [SerializeField] private RamType ramType = RamType.None;
    [SerializeField] private int ramSlotsCount;
    [SerializeField] private int requiredPSUPower;
    [SerializeField] private int psuPower;

    public PCComponentType ComponentType => componentType;
    public IReadOnlyList<PCSlotType> CompatibleSockets => compatibleSockets;
    public bool IsInstalled => isInstalled;
    public BuildSlot ParentSlot => parentSlot;
    public SocketType Socket => socketType;
    public RamType Ram => ramType;
    public int RamSlotsCount => ramSlotsCount;
    public int RequiredPSUPower => requiredPSUPower;
    public int PsuPower => psuPower;

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

    public void CopyFrom(PCComponent template)
    {
        if (template == null)
        {
            return;
        }

        componentType = template.componentType;
        compatibleSockets = new List<PCSlotType>(template.compatibleSockets);
        socketType = template.socketType;
        ramType = template.ramType;
        ramSlotsCount = template.ramSlotsCount;
        requiredPSUPower = template.requiredPSUPower;
        psuPower = template.psuPower;
    }
}
