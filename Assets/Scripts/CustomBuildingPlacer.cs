using EasyBuildSystem.Features.Runtime.Buildings.Placer;
using EasyBuildSystem.Features.Runtime.Buildings.Part;
using UnityEngine;

public class CustomBuildingPlacer : BuildingPlacer
{
    private bool m_IsInputLocked = false;

    /// <summary>
    /// Блокирует или разблокирует возможность ставить/удалять объекты
    /// </summary>
    public void LockBuildingActions(bool lockActions)
    {
        m_IsInputLocked = lockActions;

        // Если заблокировали во время превью — отменяем превью
        if (lockActions && HasPreview())
        {
            CancelPreview();
        }
    }

    public override bool PlacingBuildingPart()
    {
        if (m_IsInputLocked)
            return false;

        return base.PlacingBuildingPart();
    }

    public override bool DestroyBuildingPart()
    {
        if (m_IsInputLocked)
            return false;

        return base.DestroyBuildingPart();
    }

    public override bool EditingBuildingPart()
    {
        if (m_IsInputLocked)
            return false;

        return base.EditingBuildingPart();
    }
}