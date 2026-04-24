using UnityEngine;
using UnityEngine.EventSystems;
using EasyBuildSystem.Features.Runtime.Buildings.Placer;
using EasyBuildSystem.Features.Runtime.Buildings.Part;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;

public class DragButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private BuildingPart m_ComponentPrefab; // Префаб компонента

    private bool m_IsDragging = false;
    private bool m_WasPlaced = false;

    private CustomBuildingPlacer m_Placer;
    private Demo_FirstPersonCamera m_CameraController;
    private BuildMenuController m_MenuController;

    private void Start()
    {
        if (FindObjectOfType<BuildModeDragController>() != null)
        {
            enabled = false;
            return;
        }

        m_Placer = FindObjectOfType<CustomBuildingPlacer>();
        m_CameraController = FindObjectOfType<Demo_FirstPersonCamera>();
        m_MenuController = FindObjectOfType<BuildMenuController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (m_ComponentPrefab == null) return;

        m_IsDragging = true;
        m_WasPlaced = false;

        // Закрываем меню (но оставляем курсор свободным)
        if (m_MenuController != null)
        {
            m_MenuController.HideMenuWithoutLock(); // Новый метод, см. ниже
        }

        // Включаем режим строительства
        m_Placer.SelectBuildingPart(m_ComponentPrefab);
        m_Placer.ChangeBuildMode(BuildingPlacer.BuildMode.PLACE);

        // Блокируем камеру
        if (m_CameraController != null)
        {
            m_CameraController.LockCameraInput(true);
        }

        // Отключаем автоматическое размещение по клику
        // Будем размещать только при отпускании
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!m_IsDragging) return;

        m_IsDragging = false;

        // Пытаемся разместить объект
        if (m_Placer != null && m_Placer.CanPlacing)
        {
            m_Placer.PlacingBuildingPart();
            m_WasPlaced = true;
        }
        else
        {
            // Если нельзя разместить — отменяем превью
            m_Placer.CancelPreview();
            m_Placer.ChangeBuildMode(BuildingPlacer.BuildMode.NONE);
        }

        // Возвращаем управление камерой
        if (m_CameraController != null)
        {
            m_CameraController.LockCameraInput(false);
        }

        // Если не разместили — показываем меню снова
        if (!m_WasPlaced && m_MenuController != null)
        {
            m_MenuController.ShowMenu();
        }
    }
}