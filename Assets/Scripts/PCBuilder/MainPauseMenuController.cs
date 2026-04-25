using System.Collections.Generic;
using System.Reflection;
using EasyBuildSystem.Examples.Bases.Scripts.FirstPerson;
using TMPro;
using UnityEngine;

public class MainPauseMenuController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private KeyCode openMenuKey = KeyCode.Escape;

    [Header("Pages")]
    [SerializeField] private GameObject mainPageRoot;
    [SerializeField] private GameObject buildsPageRoot;
    [SerializeField] private GameObject settingsPageRoot;

    [Header("Dependencies")]
    [SerializeField] private Demo_FirstPersonCamera firstPersonCamera;
    [SerializeField] private Demo_FirstPersonController firstPersonController;
    [SerializeField] private BuildMenuController buildMenuController;

    [Header("Settings UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown sensitivityDropdown;

    private readonly List<Resolution> resolutions = new List<Resolution>();
    private bool isOpen;

    private void Awake()
    {
        if (firstPersonCamera == null) firstPersonCamera = FindFirstObjectByType<Demo_FirstPersonCamera>();
        if (firstPersonController == null) firstPersonController = FindFirstObjectByType<Demo_FirstPersonController>();
        if (buildMenuController == null) buildMenuController = FindFirstObjectByType<BuildMenuController>();

        SetupResolutions();
        SetupSensitivity();
        CloseAllMenus();
    }

    private void Update()
    {
        if (Input.GetKeyDown(openMenuKey))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (isOpen) CloseAllMenus();
        else OpenMainMenu();
    }

    public void OpenMainMenu()
    {
        isOpen = true;
        if (mainMenuRoot != null) mainMenuRoot.SetActive(true);
        ShowPage(mainPageRoot);
        LockGameplay(true);
    }

    public void OnClickStart()
    {
        CloseAllMenus();
    }

    public void OnClickBuilds()
    {
        ShowPage(buildsPageRoot);
    }

    public void OnClickSettings()
    {
        ShowPage(settingsPageRoot);
    }

    public void OnClickBackToMain()
    {
        ShowPage(mainPageRoot);
    }

    public void OnClickExit()
    {
        Application.Quit();
    }

    public void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= resolutions.Count)
        {
            return;
        }

        Resolution r = resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
    }

    public void OnSensitivityChanged(int index)
    {
        float sensitivity = index switch
        {
            0 => 1.0f,
            1 => 1.5f,
            2 => 2.0f,
            3 => 2.5f,
            4 => 3.0f,
            _ => 2.0f
        };
        ApplyCameraSensitivity(sensitivity);
    }

    private void CloseAllMenus()
    {
        isOpen = false;
        if (mainMenuRoot != null) mainMenuRoot.SetActive(false);
        ShowPage(null);
        LockGameplay(false);
    }

    private void ShowPage(GameObject page)
    {
        if (mainPageRoot != null) mainPageRoot.SetActive(page == mainPageRoot);
        if (buildsPageRoot != null) buildsPageRoot.SetActive(page == buildsPageRoot);
        if (settingsPageRoot != null) settingsPageRoot.SetActive(page == settingsPageRoot);
    }

    private void LockGameplay(bool locked)
    {
        if (buildMenuController != null && locked)
        {
            buildMenuController.CloseMenu();
        }

        if (firstPersonCamera != null)
        {
            firstPersonCamera.LockCameraInput(locked);
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = !locked;
        }
    }

    private void SetupResolutions()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        resolutionDropdown.ClearOptions();
        resolutions.Clear();

        Resolution[] all = Screen.resolutions;
        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < all.Length; i++)
        {
            Resolution r = all[i];
            string label = $"{r.width}x{r.height}";
            if (options.Contains(label))
            {
                continue;
            }

            options.Add(label);
            resolutions.Add(r);
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
            {
                currentIndex = resolutions.Count - 1;
            }
        }

        if (options.Count == 0)
        {
            options.Add("Текущее");
            resolutions.Add(Screen.currentResolution);
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void SetupSensitivity()
    {
        if (sensitivityDropdown == null)
        {
            return;
        }

        sensitivityDropdown.ClearOptions();
        sensitivityDropdown.AddOptions(new List<string> { "1.0", "1.5", "2.0", "2.5", "3.0" });
        sensitivityDropdown.value = 2; // default 2.0
        sensitivityDropdown.onValueChanged.RemoveListener(OnSensitivityChanged);
        sensitivityDropdown.onValueChanged.AddListener(OnSensitivityChanged);
    }

    private void ApplyCameraSensitivity(float value)
    {
        if (firstPersonCamera == null)
        {
            return;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo xField = typeof(Demo_FirstPersonCamera).GetField("m_XSensitivity", flags);
        FieldInfo yField = typeof(Demo_FirstPersonCamera).GetField("m_YSensitivity", flags);
        xField?.SetValue(firstPersonCamera, value);
        yField?.SetValue(firstPersonCamera, value);
    }
}
