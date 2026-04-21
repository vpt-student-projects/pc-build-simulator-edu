using UnityEngine;
using UnityEngine.UI;
using EasyBuildSystem.Features.Runtime.Buildings.Placer;
using EasyBuildSystem.Features.Runtime.Buildings.Part;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;

public class ComponentSelectButton : MonoBehaviour
{
    private BuildingPart m_ComponentPrefab;

    private bool m_IsPressed = false;
    private bool m_IsDragging = false;
    private bool m_WasPlaced = false;

    private CustomBuildingPlacer m_Placer;
    private Demo_FirstPersonCamera m_CameraController;
    private BuildMenuController m_MenuController;
    private GameObject m_MenuPanel;

    private void Start()
    {
        m_Placer = FindObjectOfType<CustomBuildingPlacer>();
        m_CameraController = FindObjectOfType<Demo_FirstPersonCamera>();
        m_MenuController = FindObjectOfType<BuildMenuController>();

        if (m_MenuController != null)
        {
            var field = typeof(BuildMenuController).GetField("menuPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                m_MenuPanel = field.GetValue(m_MenuController) as GameObject;
            }
        }
    }

    /// <summary>
    /// Устанавливает префаб компонента (вызывается из BuildMenuUI)
    /// </summary>
    public void SetComponentPrefab(BuildingPart prefab)
    {
        m_ComponentPrefab = prefab;
    }

    private void Update()
    {
        if (m_IsPressed)
        {
            if (Input.GetMouseButton(0))
            {
                if (!m_IsDragging && m_ComponentPrefab != null)
                {
                    StartDragging();
                }
            }
            else
            {
                if (m_IsDragging)
                {
                    StopDragging();
                }
                m_IsPressed = false;
            }
        }
    }

    public void OnButtonPressed()
    {
        m_IsPressed = true;
        Debug.Log($"Кнопка нажата, префаб: {(m_ComponentPrefab != null ? m_ComponentPrefab.name : "NULL")}");
    }

    private void StartDragging()
    {
        m_IsDragging = true;
        m_WasPlaced = false;

        Debug.Log($"Начинаем перетаскивание: {m_ComponentPrefab.name}");

        if (m_MenuPanel != null)
            m_MenuPanel.SetActive(false);

        if (m_Placer != null)
        {
            m_Placer.SelectBuildingPart(m_ComponentPrefab);
            m_Placer.ChangeBuildMode(BuildingPlacer.BuildMode.PLACE);
        }

        if (m_CameraController != null)
            m_CameraController.LockCameraInput(true);
    }

    private void StopDragging()
    {
        m_IsDragging = false;

        if (m_Placer != null)
        {
            if (m_Placer.CanPlacing)
            {
                m_Placer.PlacingBuildingPart();
                m_WasPlaced = true;
                Debug.Log("Объект размещён!");
            }
            else
            {
                m_Placer.CancelPreview();
                m_Placer.ChangeBuildMode(BuildingPlacer.BuildMode.NONE);
                Debug.Log("Нельзя разместить — отмена");
            }
        }

        if (m_CameraController != null)
            m_CameraController.LockCameraInput(false);

        if (!m_WasPlaced && m_MenuPanel != null)
            m_MenuPanel.SetActive(true);
    }
}