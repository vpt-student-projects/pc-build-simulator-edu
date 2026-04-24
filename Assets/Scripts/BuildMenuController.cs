using UnityEngine;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;

public class BuildMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    private Animator menuAnimator;
    private bool isOpen = false;

    private Demo_FirstPersonCamera m_CameraController;
    private Demo_FirstPersonController m_FirstPersonController;

    private void Start()
    {
        menuAnimator = menuPanel.GetComponent<Animator>();
        menuPanel.SetActive(false);

        // Находим контроллер камеры
        m_CameraController = FindObjectOfType<Demo_FirstPersonCamera>();
        if (m_CameraController == null)
        {
            Debug.LogWarning("Demo_FirstPersonCamera не найден на сцене!");
        }

        m_FirstPersonController = FindObjectOfType<Demo_FirstPersonController>();
        if (m_FirstPersonController == null)
        {
            Debug.LogWarning("Demo_FirstPersonController не найден на сцене!");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            menuPanel.SetActive(true);
            menuAnimator.SetTrigger("Toggle");

            // Блокируем камеру и освобождаем курсор
            if (m_CameraController != null)
            {
                m_CameraController.LockCameraInput(true);
            }

            if (m_FirstPersonController != null)
            {
                m_FirstPersonController.enabled = false;
            }
        }
        else
        {
            menuPanel.SetActive(false);

            // Разблокируем камеру и прячем курсор
            if (m_CameraController != null)
            {
                m_CameraController.LockCameraInput(false);
            }

            if (m_FirstPersonController != null)
            {
                m_FirstPersonController.enabled = true;
            }
        }
    }
    /// <summary>
    /// Скрывает меню, но НЕ возвращает блокировку камеры и курсора
    /// </summary>
    public void HideMenuWithoutLock()
    {
        isOpen = false;
        menuPanel.SetActive(false);
        // НЕ вызываем LockCameraInput(false) — камера остаётся заблокированной
        if (m_FirstPersonController != null)
        {
            m_FirstPersonController.enabled = false;
        }
    }

    /// <summary>
    /// Показывает меню (используется если размещение не удалось)
    /// </summary>
    public void ShowMenu()
    {
        isOpen = true;
        menuPanel.SetActive(true);
        menuAnimator.SetTrigger("Toggle");

        if (m_CameraController != null)
        {
            m_CameraController.LockCameraInput(true);
        }

        if (m_FirstPersonController != null)
        {
            m_FirstPersonController.enabled = false;
        }
    }
    // Метод для закрытия меню по кнопке (если нужно)
    public void CloseMenu()
    {
        if (isOpen)
        {
            ToggleMenu();
        }
    }
}