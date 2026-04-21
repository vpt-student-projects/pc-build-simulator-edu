using UnityEngine;// Если используете обычный Text
using TMPro;

using EasyBuildSystem.Features.Runtime.Buildings.Placer;
using EasyBuildSystem.Features.Runtime.Buildings.Part;

public class BuildingPartNameDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text m_Text; // Для TextMeshPro

    [Header("Settings")]
    [Tooltip("Текст, если ничего не выбрано или режим НЕ строительство")]
    [SerializeField] private string m_NoBuildingText = "Выберите объект";

    [Tooltip("Показывать название только в режиме PLACE? Если нет, то будет показывать и при удалении")]
    [SerializeField] private bool m_OnlyInPlaceMode = true;

    private BuildingPlacer m_Placer;

    private void Start()
    {
        // Находим Placer (он Singleton, можно найти через Instance)
        m_Placer = BuildingPlacer.Instance;

        if (m_Placer == null)
        {
            Debug.LogError("BuildingPlacer не найден на сцене!");
            enabled = false;
            return;
        }

        // Подписываемся на события смены объекта и смены режима
        m_Placer.OnChangedBuildingPartEvent.AddListener(UpdateDisplay);
        m_Placer.OnChangedBuildModeEvent.AddListener(OnModeChanged);

        // Первоначальное обновление текста
        UpdateDisplay(m_Placer.GetSelectedBuildingPart);
    }

    private void OnDestroy()
    {
        if (m_Placer != null)
        {
            m_Placer.OnChangedBuildingPartEvent.RemoveListener(UpdateDisplay);
            m_Placer.OnChangedBuildModeEvent.RemoveListener(OnModeChanged);
        }
    }

    private void OnModeChanged(BuildingPlacer.BuildMode mode)
    {
        // Если включена настройка "Только в режиме стройки", то скрываем текст в других режимах
        if (m_OnlyInPlaceMode && mode != BuildingPlacer.BuildMode.PLACE)
        {
            if (m_Text != null) m_Text.text = m_NoBuildingText;
        }
        else
        {
            // Иначе просто обновляем текст на основе текущего выбранного объекта
            UpdateDisplay(m_Placer.GetSelectedBuildingPart);
        }
    }

    private void UpdateDisplay(BuildingPart part)
    {
        if (m_Text == null)
        {
            Debug.LogWarning("UI Text не назначен в инспекторе!");
            return;
        }

        // Проверяем режим
        if (m_OnlyInPlaceMode && m_Placer.GetBuildMode != BuildingPlacer.BuildMode.PLACE)
        {
            m_Text.text = m_NoBuildingText;
            return;
        }

        // Если есть выбранный объект и он валиден
        if (part != null)
        {
            // Можно использовать имя префаба
            m_Text.text = part.name.Replace("(Clone)", ""); // Убираем (Clone) если появится

            // Или, если в BuildingPart есть поле Identifier, можно его:
            // if (!string.IsNullOrEmpty(part.GetGeneralSettings.Identifier))
            //     m_Text.text = part.GetGeneralSettings.Identifier;
            // else
            //     m_Text.text = part.name;
        }
        else
        {
            m_Text.text = m_NoBuildingText;
        }
    }
}