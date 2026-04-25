using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PCBuildEvaluationService : MonoBehaviour
{
    [Header("Power validation")]
    [Tooltip("Запас по мощности БП относительно рассчитанного потребления системы.")]
    [SerializeField, Range(1.0f, 1.5f)] private float psuHeadroomMultiplier = 1.2f;

    [Header("Scoring weights")]
    [SerializeField, Range(0f, 1f)] private float cpuWeight = 0.22f;
    [SerializeField, Range(0f, 1f)] private float gpuWeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float ramWeight = 0.15f;
    [SerializeField, Range(0f, 1f)] private float storageWeight = 0.10f;
    [SerializeField, Range(0f, 1f)] private float motherboardWeight = 0.08f;
    [SerializeField, Range(0f, 1f)] private float psuWeight = 0.10f;
    [SerializeField, Range(0f, 0.5f)] private float missingGpuPerformancePenalty = 0.25f;

    [Header("Optional references")]
    [SerializeField] private PCAssemblyState assemblyState;

    [Header("Evaluation UI - Confirm")]
    [SerializeField] private GameObject confirmDialogRoot;
    [SerializeField] private TMP_Text confirmDialogText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Evaluation UI - Result")]
    [SerializeField] private GameObject resultDialogRoot;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private Button resultCloseButton;

    public PCBuildEvaluationResult LastResult { get; private set; }

    private void Awake()
    {
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveListener(ConfirmEvaluateFromDialog);
            confirmYesButton.onClick.AddListener(ConfirmEvaluateFromDialog);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveListener(CancelEvaluateFromDialog);
            confirmNoButton.onClick.AddListener(CancelEvaluateFromDialog);
        }

        if (resultCloseButton != null)
        {
            resultCloseButton.onClick.RemoveListener(CloseResultDialog);
            resultCloseButton.onClick.AddListener(CloseResultDialog);
        }

        HideConfirmDialog();
        CloseResultDialog();
    }

    public PCBuildEvaluationResult EvaluateCurrentBuild()
    {
        if (assemblyState == null)
        {
            assemblyState = FindFirstObjectByType<PCAssemblyState>();
        }

        if (assemblyState == null)
        {
            LastResult = PCBuildEvaluationResult.Fail("Состояние сборки не найдено на сцене.");
            return LastResult;
        }

        List<PCComponent> parts = assemblyState.GetInstalledComponentsSnapshot();
        if (parts.Count == 0)
        {
            LastResult = PCBuildEvaluationResult.Fail("Сборка пустая: установите комплектующие.");
            return LastResult;
        }

        if (assemblyState.Cpu == null)
        {
            LastResult = PCBuildEvaluationResult.Fail("Процессор не установлен: система не сможет запуститься.");
            return LastResult;
        }

        if (assemblyState.InstalledRamCount <= 0)
        {
            LastResult = PCBuildEvaluationResult.Fail("Оперативная память не установлена: система не сможет запуститься.");
            return LastResult;
        }

        if (assemblyState.InstalledStorageCount <= 0)
        {
            LastResult = PCBuildEvaluationResult.Fail("Накопитель не установлен: системе некуда загружаться.");
            return LastResult;
        }

        if (assemblyState.InstalledCoolers == null || assemblyState.InstalledCoolers.Count <= 0)
        {
            LastResult = PCBuildEvaluationResult.Fail("Перегрев системы: на процессоре отсутствует кулер.");
            return LastResult;
        }

        int totalPowerDraw = 0;
        int gpuRequiredPsu = 0;
        float cpuScore = 0f;
        float gpuScore = 0f;
        float ramScore = 0f;
        float storageScore = 0f;
        float motherboardScore = 0f;
        float psuScore = 0f;

        int ramModules = 0;
        int storageDevices = 0;

        for (int i = 0; i < parts.Count; i++)
        {
            PCComponent c = parts[i];
            if (c == null)
            {
                continue;
            }

            totalPowerDraw += Mathf.Max(0, c.PowerWatts);
            float tierNorm = Mathf.Clamp01(c.ModelTier / 3f);

            switch (c.ComponentType)
            {
                case PCComponentType.CPU:
                    cpuScore = Mathf.Max(cpuScore, TierAndPowerScore(tierNorm, c.PowerWatts, 170f));
                    break;
                case PCComponentType.GPU:
                    gpuScore = Mathf.Max(gpuScore, TierAndPowerScore(tierNorm, c.GpuTdpW > 0 ? c.GpuTdpW : c.PowerWatts, 355f));
                    gpuRequiredPsu = Mathf.Max(gpuRequiredPsu, c.RequiredPSUPower);
                    break;
                case PCComponentType.RAM:
                    ramModules++;
                    ramScore += 0.5f * tierNorm + 0.5f;
                    break;
                case PCComponentType.Storage:
                    storageDevices++;
                    storageScore += 0.5f * tierNorm + 0.5f;
                    break;
                case PCComponentType.Motherboard:
                    motherboardScore = Mathf.Max(motherboardScore, tierNorm);
                    break;
                case PCComponentType.PSU:
                    psuScore = Mathf.Max(psuScore, tierNorm);
                    break;
            }
        }

        PCComponent psu = assemblyState.Psu;
        int psuWattage = psu != null ? Mathf.Max(0, psu.PsuPower) : 0;
        int recommendedPower = Mathf.CeilToInt(totalPowerDraw * Mathf.Max(1f, psuHeadroomMultiplier));
        recommendedPower = Mathf.Max(recommendedPower, gpuRequiredPsu);
        bool canBootByPower = psuWattage > 0 && psuWattage >= recommendedPower;

        if (!canBootByPower)
        {
            string reason = psuWattage <= 0
                ? "Блок питания не установлен."
                : $"Недостаточно мощности БП: требуется минимум {recommendedPower}W, установлен {psuWattage}W.";

            LastResult = new PCBuildEvaluationResult
            {
                CanBoot = false,
                Score100 = 0,
                Rating = "F",
                Summary = reason,
                TotalPowerDrawW = totalPowerDraw,
                RecommendedPsuW = recommendedPower,
                InstalledPsuW = psuWattage
            };
            return LastResult;
        }

        float normalizedRam = Mathf.Clamp01(ramScore / Mathf.Max(1, ramModules));
        float normalizedStorage = Mathf.Clamp01(storageScore / Mathf.Max(1, storageDevices));
        float balancePenalty = GetBalancePenalty(cpuScore, gpuScore, normalizedRam, normalizedStorage);
        float completenessBonus = GetCompletenessBonus(assemblyState);
        float psuHeadroomScore = Mathf.Clamp01((psuWattage - totalPowerDraw) / Mathf.Max(1f, totalPowerDraw));
        float normalizedPsu = Mathf.Clamp01(0.7f * psuScore + 0.3f * psuHeadroomScore);
        bool hasGpu = assemblyState.Gpu != null;

        float weighted =
            cpuScore * cpuWeight +
            gpuScore * gpuWeight +
            normalizedRam * ramWeight +
            normalizedStorage * storageWeight +
            motherboardScore * motherboardWeight +
            normalizedPsu * psuWeight;

        float finalNormalized = Mathf.Clamp01((weighted + completenessBonus) * (1f - balancePenalty));
        if (!hasGpu)
        {
            finalNormalized = Mathf.Clamp01(finalNormalized - missingGpuPerformancePenalty);
        }
        int score100 = Mathf.RoundToInt(finalNormalized * 100f);

        LastResult = new PCBuildEvaluationResult
        {
            CanBoot = true,
            Score100 = score100,
            Rating = ScoreToRating(score100),
            Summary = BuildSummary(score100, totalPowerDraw, psuWattage, recommendedPower, balancePenalty, hasGpu),
            TotalPowerDrawW = totalPowerDraw,
            RecommendedPsuW = recommendedPower,
            InstalledPsuW = psuWattage
        };
        return LastResult;
    }

    public void EvaluateAndLog()
    {
        // If UI is configured, start with confirmation dialog.
        if (confirmDialogRoot != null)
        {
            if (confirmDialogText != null)
            {
                confirmDialogText.text = "Вы точно хотите проверить сборку?";
            }

            confirmDialogRoot.SetActive(true);
            return;
        }

        // Fallback (without UI): direct evaluation to logs.
        EvaluateAndLogImmediate();
    }

    public void ConfirmEvaluateFromDialog()
    {
        HideConfirmDialog();
        EvaluateAndLogImmediate();
    }

    public void CancelEvaluateFromDialog()
    {
        HideConfirmDialog();
    }

    public void CloseResultDialog()
    {
        if (resultDialogRoot != null)
        {
            resultDialogRoot.SetActive(false);
        }
    }

    private void HideConfirmDialog()
    {
        if (confirmDialogRoot != null)
        {
            confirmDialogRoot.SetActive(false);
        }
    }

    private void EvaluateAndLogImmediate()
    {
        PCBuildEvaluationResult result = EvaluateCurrentBuild();
        if (result == null)
        {
            return;
        }

        if (!result.CanBoot)
        {
            Debug.LogWarning($"Build Evaluation: {result.Summary}");
            ShowResultDialog("Ошибка проверки сборки", result.Summary);
            return;
        }

        string logMessage =
            $"Build Evaluation: score {result.Score100}/100 ({result.Rating}). " +
            $"Power: {result.TotalPowerDrawW}W, PSU: {result.InstalledPsuW}W, recommended: {result.RecommendedPsuW}W. " +
            $"{result.Summary}";
        Debug.Log(logMessage);
        ShowResultDialog($"Итог сборки: {result.Score100}/100 ({result.Rating})", result.Summary);
    }

    private void ShowResultDialog(string title, string body)
    {
        if (resultDialogRoot == null)
        {
            return;
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = title;
        }

        if (resultBodyText != null)
        {
            resultBodyText.text = body;
        }

        resultDialogRoot.SetActive(true);
    }

    private static float TierAndPowerScore(float tierNorm, int powerWatts, float maxRefPower)
    {
        float powerNorm = Mathf.Clamp01(powerWatts / Mathf.Max(1f, maxRefPower));
        return Mathf.Clamp01(0.65f * tierNorm + 0.35f * powerNorm);
    }

    private static float GetBalancePenalty(float cpu, float gpu, float ram, float storage)
    {
        float pairMismatch = Mathf.Abs(cpu - gpu);
        float memoryMismatch = Mathf.Abs(Mathf.Max(cpu, gpu) - ram) * 0.6f;
        float storageMismatch = Mathf.Abs(Mathf.Max(cpu, gpu) - storage) * 0.3f;
        return Mathf.Clamp01(pairMismatch * 0.35f + memoryMismatch * 0.15f + storageMismatch * 0.10f);
    }

    private static float GetCompletenessBonus(PCAssemblyState assembly)
    {
        int have = 0;
        int need = 4; // CPU, PSU, RAM>=1, STORAGE>=1 (GPU теперь опционален)
        if (assembly.Cpu != null) have++;
        if (assembly.Psu != null) have++;
        if (assembly.InstalledRamCount > 0) have++;
        if (assembly.InstalledStorageCount > 0) have++;
        float bonus = Mathf.Clamp01(have / (float)need) * 0.08f;
        if (assembly.Gpu != null)
        {
            bonus += 0.02f;
        }
        return Mathf.Clamp01(bonus);
    }

    private static string ScoreToRating(int score)
    {
        if (score >= 90) return "S";
        if (score >= 80) return "A";
        if (score >= 65) return "B";
        if (score >= 50) return "C";
        if (score >= 35) return "D";
        return "F";
    }

    private static string BuildSummary(int score, int draw, int psu, int recommended, float balancePenalty, bool hasGpu)
    {
        string balance = balancePenalty < 0.12f ? "сбалансированная" :
                         balancePenalty < 0.25f ? "умеренно сбалансированная" :
                         "с заметным дисбалансом";
        string gpuNote = hasGpu ? string.Empty : " Видеокарта отсутствует: снижена общая производительность.";
        return $"Оценка {score}/100, сборка {balance}. Потребление {draw}W, установлен БП {psu}W, рекомендуется от {recommended}W.{gpuNote}";
    }
}

[System.Serializable]
public class PCBuildEvaluationResult
{
    public bool CanBoot;
    public int Score100;
    public string Rating;
    public string Summary;
    public int TotalPowerDrawW;
    public int RecommendedPsuW;
    public int InstalledPsuW;

    public static PCBuildEvaluationResult Fail(string summary)
    {
        return new PCBuildEvaluationResult
        {
            CanBoot = false,
            Score100 = 0,
            Rating = "F",
            Summary = summary,
            TotalPowerDrawW = 0,
            RecommendedPsuW = 0,
            InstalledPsuW = 0
        };
    }
}
