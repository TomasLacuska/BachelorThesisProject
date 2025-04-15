#region MilestoneController Class
/*
 * =========================================================
 * MilestoneController
 *
 * Manages milestone upgrades and related UI updates. This controller
 * updates power production displays, animates UI elements (slider, text,
 * upgrade panel), and handles milestone upgrade logic, including awarding
 * credits and adjusting power requirements.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MilestoneController : MonoBehaviour
{
    #region UI References & Settings

    [Header("UI References")]
    public Slider powerSlider;
    public TextMeshProUGUI powerText;
    public GameObject upgradePanel; // Contains the upgrade button
    public TextMeshProUGUI milestoneText;

    [Header("Power Requirements")]
    // This value is the threshold the network must reach to enable the upgrade.
    public int requiredPower = 100;

    [Header("Animation Settings")]
    public float sliderSmoothTime = 0.25f;
    public float textUpdateTime = 0.3f;
    public float upgradeSlideDuration = 0.4f;
    public Vector2 upgradeHiddenPos; // Position when hidden
    public Vector2 upgradeShownPos;  // Position when visible

    #endregion

    #region Private Variables

    private int currentPower = 0;
    // Milestone level starts at 1. On each upgrade, the reward is 10000 * milestoneLevel.
    private int milestoneLevel = 1;
    private Coroutine textAnimRoutine;
    private Coroutine sliderAnimRoutine;
    private Coroutine upgradeAnimRoutine;
    
    private RectTransform upgradeRect;

    #endregion

    #region Unity Event Functions

    void Start()
    {
        // Cache the RectTransform for the upgrade panel and set its initial position.
        upgradeRect = upgradePanel.GetComponent<RectTransform>();
        upgradeRect.anchoredPosition = upgradeHiddenPos;
        upgradePanel.SetActive(true);

        // Subscribe to network change events so that the UI updates when connectivity changes.
        NetworkEvents.OnNetworkChanged += RefreshPower;
        RefreshPower(); // Initial update
    }

    void OnDestroy()
    {
        NetworkEvents.OnNetworkChanged -= RefreshPower;
        MilestoneEvents.OnMilestoneRefreshReady -= OnAllMilestoneEffectsApplied; // Prevent ghost calls
    }


    #endregion

    #region UI Updates

    // Recalculates total power production, updates slider and text, and checks upgrade visibility.
    public void RefreshPower()
    {
        int newPower = PowerNetworkManager.Instance.TotalPowerProduction;
        UpdateTextSmooth(newPower);
        UpdateSliderSmooth(newPower);
        CheckUpgradeVisibility(newPower);
    }


    // Animates the power text smoothly from the previous value to the target.
    void UpdateTextSmooth(int target)
    {
        if (this != null && textAnimRoutine != null)
            StopCoroutine(textAnimRoutine);
        textAnimRoutine = StartCoroutine(AnimateText(currentPower, target));
        currentPower = target;
    }

    IEnumerator AnimateText(int from, int to)
    {
        float time = 0f;
        while (time < textUpdateTime)
        {
            if (this == null || !gameObject.activeInHierarchy)
                yield break; // Bail out

            float t = time / textUpdateTime;
            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            powerText.text = $"{value} / {requiredPower}\n<sprite=4><sprite=4><sprite=4>";
            time += Time.deltaTime;
            yield return null;
        }

        if (this != null)
            powerText.text = $"{to} / {requiredPower}\n<sprite=4><sprite=4><sprite=4>";
    }


    // Updates the slider value smoothly based on the current power.
    void UpdateSliderSmooth(int newPower)
    {
        if (this != null && sliderAnimRoutine != null)
            StopCoroutine(sliderAnimRoutine);
        float targetValue = Mathf.Clamp01((float)newPower / requiredPower);
        sliderAnimRoutine = StartCoroutine(AnimateSlider(targetValue));
    }


    IEnumerator AnimateSlider(float target)
    {
        float current = powerSlider.value;
        float time = 0f;
        while (time < sliderSmoothTime)
        {
            float t = time / sliderSmoothTime;
            powerSlider.value = Mathf.Lerp(current, target, t);
            time += Time.deltaTime;
            yield return null;
        }
        powerSlider.value = target;
    }

    // Checks whether the upgrade panel should be visible based on total power.
    void CheckUpgradeVisibility(int total)
    {
        bool shouldShow = total >= requiredPower;
        Vector2 target = shouldShow ? upgradeShownPos : upgradeHiddenPos;

        if (upgradeAnimRoutine != null)
            StopCoroutine(upgradeAnimRoutine);
        upgradeAnimRoutine = StartCoroutine(SlideUpgradePanel(target));
    }

    IEnumerator SlideUpgradePanel(Vector2 target)
    {
        if (upgradeRect == null)
            yield break;

        Vector2 start = upgradeRect.anchoredPosition;
        float time = 0f;

        CanvasGroup cg = upgradePanel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = upgradePanel.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;
        float endAlpha = (target == upgradeShownPos) ? 1f : 0f;

        while (time < upgradeSlideDuration)
        {
            if (upgradeRect == null)
                yield break;

            float t = time / upgradeSlideDuration;
            upgradeRect.anchoredPosition = Vector2.Lerp(start, target, t);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            time += Time.deltaTime;
            yield return null;
        }

        if (upgradeRect != null)
            upgradeRect.anchoredPosition = target;
        cg.alpha = endAlpha;
    }

    #endregion

    #region Milestone Upgrade Integration

    // Called when the upgrade button is pressed. Increases milestone level, awards credits,
    // and adjusts the required power.
    public void OnUpgradeButtonPressed()
    {
        milestoneLevel++;

        if (milestoneLevel == 12)
        {
            UIManager.Instance.ShowWinScreen();
            return;
        }

        // Clear previous event + apply new one
        MilestoneEventManager.Instance.ClearMilestoneEvent();
        MilestoneEventData selectedEvent = MilestoneEventLibrary.GetRandomEvent();
        MilestoneEventManager.Instance.ApplyMilestoneEvent(selectedEvent);
        UIManager.Instance.DisplayMilestoneEvent(selectedEvent);

        // Handle connectable updates
        var allConnectables = ConnectableManager.Instance.GetAll();
        int pendingCount = 0;
        foreach (var obj in allConnectables)
        {
            if (obj.gameObject.GetComponent<WindTurbine>() != null ||
                obj.gameObject.GetComponent<SolarPanelStatic>() != null ||
                obj.gameObject.GetComponent<BioBurner>() != null)
            {
                pendingCount++;
            }
            else
            {
                obj.ForceUpdate(); // Static buildings update immediately
            }
        }

        // Subscribe to the actual logic to be triggered after all buildings are ready
        MilestoneEvents.OnMilestoneRefreshReady += OnAllMilestoneEffectsApplied;
        MilestoneEvents.TriggerMilestoneChanged(pendingCount);

    }
    //Called after recieving all building are ready
    private void OnAllMilestoneEffectsApplied()
    {
        // Unsubscribe so it doesn't trigger next time
        MilestoneEvents.OnMilestoneRefreshReady -= OnAllMilestoneEffectsApplied;

        int totalProduction = PowerNetworkManager.Instance.TotalPowerProduction;

        // Base reward is 10000 multiplied by the current milestone level.
        float baseReward = 10000 * milestoneLevel;

        // Bonus multiplier for surplus power.
        float bonusMultiplier = (totalProduction >= requiredPower * 1.2f) ? 1.1f : 1f;

        // EcoBalance modifier (clamped to avoid negative rewards).
        float ecoMultiplier = Mathf.Clamp01(EcoBalanceManager.Instance.ecoBalance / EcoBalanceManager.Instance.originEco);

        // Final reward calculation.
        int finalReward = Mathf.RoundToInt(baseReward * bonusMultiplier * ecoMultiplier);

        // Add credits and update UI
        CreditsManager.Instance.AddCredits(finalReward);
        milestoneText.text = $"MILESTONE {milestoneLevel}";
        UIManager.Instance.ShowCreditGainPopup(finalReward);


        float t = Mathf.InverseLerp(1, 12, milestoneLevel);
        float scaleFactor = Mathf.Lerp(1.1f, 2.5f, t); // Grows over time
        requiredPower = Mathf.RoundToInt(100 * milestoneLevel * scaleFactor);


        NetworkEvents.TriggerNetworkChanged();

        // Refresh UI with correct power now
        RefreshPower();

        Debug.Log($"Milestone upgraded to level {milestoneLevel}. Reward: {finalReward} (EcoBalance: {ecoMultiplier * 100}%). New required power: {requiredPower}");
    }



    #endregion
}
