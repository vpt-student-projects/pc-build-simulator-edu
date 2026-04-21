using UnityEngine;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;

public class BuildMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    private Animator menuAnimator;
    private bool isOpen = false;

    private Demo_FirstPersonCamera m_CameraController;

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
        }
        else
        {
            menuPanel.SetActive(false);

            // Разблокируем камеру и прячем курсор
            if (m_CameraController != null)
            {
                m_CameraController.LockCameraInput(false);
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