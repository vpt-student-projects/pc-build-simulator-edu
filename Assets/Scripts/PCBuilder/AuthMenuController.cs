using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;

public class AuthMenuController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject authRoot;
    [SerializeField] private GameObject loginPageRoot;
    [SerializeField] private GameObject registerPageRoot;

    [Header("Dependencies")]
    [SerializeField] private BuildPersistenceService persistenceService;
    [SerializeField] private MainPauseMenuController pauseMenuController;
    [SerializeField] private Demo_FirstPersonCamera firstPersonCamera;
    [SerializeField] private Demo_FirstPersonController firstPersonController;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToRegisterButton;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button goToLoginButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        if (persistenceService == null) persistenceService = FindFirstObjectByType<BuildPersistenceService>();
        if (pauseMenuController == null) pauseMenuController = FindFirstObjectByType<MainPauseMenuController>();
        if (firstPersonCamera == null) firstPersonCamera = FindFirstObjectByType<Demo_FirstPersonCamera>();
        if (firstPersonController == null) firstPersonController = FindFirstObjectByType<Demo_FirstPersonController>();

        if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
        if (goToRegisterButton != null) goToRegisterButton.onClick.AddListener(OpenRegisterPage);
        if (goToLoginButton != null) goToLoginButton.onClick.AddListener(OpenLoginPage);

        LockGameplayForAuth(true);
        if (pauseMenuController != null)
        {
            pauseMenuController.SetAuthenticated(false);
        }

        if (authRoot != null) authRoot.SetActive(true);
        OpenLoginPage();
        SetStatus("Войдите в аккаунт или зарегистрируйтесь.");
    }

    private void Update()
    {
        // Some external scripts can re-lock cursor on focus/click.
        // While auth UI is active, force UI cursor mode every frame.
        if (authRoot != null && authRoot.activeInHierarchy)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }

    public void OpenLoginPage()
    {
        if (loginPageRoot != null) loginPageRoot.SetActive(true);
        if (registerPageRoot != null) registerPageRoot.SetActive(false);
    }

    public void OpenRegisterPage()
    {
        if (loginPageRoot != null) loginPageRoot.SetActive(false);
        if (registerPageRoot != null) registerPageRoot.SetActive(true);
    }

    public void OnLoginClicked()
    {
        if (persistenceService == null)
        {
            SetStatus("PersistenceService не назначен.");
            return;
        }

        string username = loginUsernameInput != null ? loginUsernameInput.text : string.Empty;
        string password = loginPasswordInput != null ? loginPasswordInput.text : string.Empty;
        bool ok = persistenceService.LoginUser(username, password, out string error);
        if (!ok)
        {
            SetStatus(error);
            return;
        }

        CompleteAuth($"Успешный вход: {username}");
    }

    public void OnRegisterClicked()
    {
        if (persistenceService == null)
        {
            SetStatus("PersistenceService не назначен.");
            return;
        }

        string username = registerUsernameInput != null ? registerUsernameInput.text : string.Empty;
        string password = registerPasswordInput != null ? registerPasswordInput.text : string.Empty;
        string confirm = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : string.Empty;
        if (!string.Equals(password, confirm))
        {
            SetStatus("Пароли не совпадают.");
            return;
        }

        bool ok = persistenceService.RegisterUser(username, password, out string error);
        if (!ok)
        {
            SetStatus(error);
            return;
        }

        CompleteAuth($"Регистрация успешна: {username}");
    }

    private void CompleteAuth(string message)
    {
        if (persistenceService != null && persistenceService.CurrentUserId > 0)
        {
            // If multiple persistence services exist in scene, sync user id across all.
            BuildPersistenceService[] all = FindObjectsByType<BuildPersistenceService>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    all[i].SetCurrentUserId(persistenceService.CurrentUserId);
                }
            }
        }

        SetStatus(message);
        if (authRoot != null)
        {
            authRoot.SetActive(false);
        }

        LockGameplayForAuth(false);
        if (pauseMenuController != null)
        {
            pauseMenuController.SetAuthenticated(true);
        }
    }

    private void LockGameplayForAuth(bool locked)
    {
        if (firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(locked);
        }
        else
        {
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = !locked;
        }

        if (!locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        else
        {
            Debug.Log(message);
        }
    }
}
