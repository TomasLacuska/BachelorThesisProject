#region UIManager Class
/*
 * =========================================================
 * UIManager
 *
 * Manages various UI elements for dynamic power, eco balance, credits,
 * tooltips, and popups. In this version, we have a unified tooltip system:
 * one set for building tooltips and one for milestone event tooltips.
 * Both tooltips use the same fade in/out logic and are clamped to stay
 * inside the screen bounds. The population routines are separate so you
 * can easily extend the data shown.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }
    #endregion

    #region UI Elements

    [Header("Dynamic Power UI Elements (Shown during placement)")]
    [Tooltip("Container for dynamic power stats during building placement.")]
    public GameObject powerStatsBuild;
    [Tooltip("Slider displaying current power output.")]
    public Slider powerSlider;
    [Tooltip("Text displaying final power output.")]
    public TextMeshProUGUI powerFinalPowerText;

    [Header("Dynamic Predicted Eco Panel")]
    [Tooltip("Panel displaying predicted eco cost.")]
    public GameObject predictedEcoPanel;
    [Tooltip("Text showing predicted eco cost.")]
    public TextMeshProUGUI predictedEcoText;

    [Header("Persistent Eco Balance UI Elements (Always visible)")]
    [Tooltip("Slider representing the eco balance.")]
    public Slider ecoBalanceSlider;
    [Tooltip("Text displaying the eco balance value.")]
    public TextMeshProUGUI ecoBalanceValueText;

    [Header("Credits UI")]
    [Tooltip("Text displaying the player's credits.")]
    public TextMeshProUGUI creditsText;
    [Tooltip("Text displaying the dynamic cost of building.")]
    public TextMeshProUGUI dynamicCostText;

    [Header("Special Building UI Settings")]
    [Tooltip("Prefabs for special power UI elements.")]
    public GameObject[] powerUIPrefabs;

    [Header("Building Tooltip UI")]
    [Tooltip("Panel for displaying building tooltips.")]
    public GameObject tooltipPanel;
    [Tooltip("Text showing building name in the tooltip.")]
    public TextMeshProUGUI tooltipName;
    [Tooltip("Text showing building class in the tooltip.")]
    public TextMeshProUGUI tooltipClass;
    [Tooltip("Text showing building description in the tooltip.")]
    public TextMeshProUGUI tooltipDesc;
    [Tooltip("Text showing power value in the tooltip.")]
    public TextMeshProUGUI powerValueText;
    [Tooltip("Text showing eco cost in the tooltip.")]
    public TextMeshProUGUI ecoValueText;
    [Tooltip("Text showing connection range in the tooltip.")]
    public TextMeshProUGUI rangeValueText;
    [Tooltip("Text showing building cost in the tooltip.")]
    public TextMeshProUGUI costValueText;

    [Header("Event Tooltip UI")]
    [Tooltip("Panel for displaying milestone event tooltips.")]
    public GameObject eventTooltipPanel;
    [Tooltip("Text showing the milestone event name.")]
    public TextMeshProUGUI eventTooltipName;
    [Tooltip("Text showing the affected building class for the event.")]
    public TextMeshProUGUI eventTooltipClass;
    [Tooltip("Text showing the milestone event description.")]
    public TextMeshProUGUI eventTooltipDesc;

    [Header("Tooltip Follow Settings")]
    [Tooltip("Offset for the tooltip position relative to the mouse.")]
    public Vector2 tooltipOffset = new Vector2(200f, 200f);

    [Header("UI Panels")]
    [Tooltip("Reference to the build panel controller.")]
    public BuildPanelController buildPanel;

    [Header("Popup Effects")]
    [Tooltip("Anchor transform for credits popups.")]
    public Transform creditsPopupAnchor;
    [Tooltip("Prefab for the credits popup.")]
    public GameObject creditsPopupPrefab;
    [Tooltip("Anchor transform for eco popups.")]
    public Transform ecoPopupAnchor;
    [Tooltip("Prefab for the eco popup.")]
    public GameObject ecoPopupPrefab;

    [Header("Win Panel")]
    [Tooltip("Panel shown when the player wins.")]
    public GameObject winPanel;

    [Header("Milestone Event Icons")]
    [Tooltip("Container for milestone event icons (should have a Horizontal Layout Group).")]
    public Transform eventIconContainer;
    [Tooltip("Prefab for a milestone event icon (should have an Image and EventIconHover component).")]
    public GameObject eventIconPrefab;

    [Header("Tooltip System Settings")]
    public Canvas mainCanvas;


    #endregion

    #region Private Variables

    // For building tooltip
    private bool isTooltipVisible = false;
    private Coroutine tooltipRoutine;
    private string currentHoveredID = null;
    private CanvasGroup tooltipCanvasGroup;
    private bool isHoveringBuilding = false;


    // For event tooltip
    private bool isEventTooltipVisible = false;
    private Coroutine eventTooltipRoutine;
    private CanvasGroup eventTooltipCanvasGroup;
    private bool isHoveringEvent = false;

    private int lastKnownEcoBalance = 1000;
    private Coroutine ecoBalanceEffectRoutine;
    private Coroutine ecoColorResetRoutine;
    private readonly Color ecoNormalColor = Color.white;
    private readonly Color ecoAlertColor = Color.red;
    private Vector3 ecoTextBasePosition;

    private Coroutine creditsAnimRoutine;
    private Coroutine creditsFlashRoutine;
    
    private static readonly NumberFormatInfo spaceNumberFormat = new NumberFormatInfo
    {
        NumberGroupSeparator = " ",
        NumberDecimalDigits = 0
    };

    #endregion

    #region Unity Event Functions

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }
    void Start()
    {
        if (ecoBalanceValueText != null)
            ecoTextBasePosition = ecoBalanceValueText.transform.localPosition;

        if (CreditsManager.Instance != null)
            UpdateCreditsUI(CreditsManager.Instance.GetCredits());
            
        // Setup building tooltip
        tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.alpha = 0f;
        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;
        tooltipPanel.SetActive(true);

        // Setup event tooltip
        eventTooltipCanvasGroup = eventTooltipPanel.GetComponent<CanvasGroup>();
        if (eventTooltipCanvasGroup == null)
            eventTooltipCanvasGroup = eventTooltipPanel.AddComponent<CanvasGroup>();
        eventTooltipCanvasGroup.alpha = 0f;
        eventTooltipCanvasGroup.interactable = false;
        eventTooltipCanvasGroup.blocksRaycasts = false;
        eventTooltipPanel.SetActive(true);

        UpdateEcoBalanceUI();
        HidePowerUI();
    }

    void Update()
    {
        // Hide both tooltips when any mouse button is pressed.
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            HideBuildingTooltip();
            HideEventTooltip();
        }

        // Update building tooltip position if visible.
        if (isTooltipVisible && tooltipPanel.activeSelf)
        {
            UpdateTooltipPosition(tooltipPanel);
        }




        // Update event tooltip position if visible.
        if (isEventTooltipVisible && eventTooltipPanel.activeSelf)
        {
            UpdateTooltipPosition(eventTooltipPanel);
        }

        // Toggle build panel with hotkey "B".
        if (Input.GetKeyDown(KeyCode.B))
            buildPanel?.TogglePanel();
    }

    #endregion

    #region Dynamic UI Updates

    public void UpdateUI(GameObject currentPreview, bool isHydroDamPlacement)
    {
        if (currentPreview == null)
        {
            HidePowerUI();
            return;
        }

        ConnectableObject co = currentPreview.GetComponent<ConnectableObject>();

        if (currentPreview != null)
        {
            if (powerStatsBuild != null && !powerStatsBuild.activeSelf)
                powerStatsBuild.SetActive(true);

            if (isHydroDamPlacement)
            {
                powerSlider?.gameObject.SetActive(false);
                int displayPower = co != null ? co.powerProduction : 0;
                powerFinalPowerText.text = $"{displayPower.ToString("N0", spaceNumberFormat)} <sprite=4>";
            }
            else
            {
                int previewPowerOutput = co != null ? co.powerProduction : 0;
                if (powerSlider != null)
                {
                    powerSlider.gameObject.SetActive(true);
                    powerSlider.maxValue = co != null ? co.GetEffectiveMaxProduction() : 10;
                    powerSlider.value = previewPowerOutput;
                }
                powerFinalPowerText.text = $"{previewPowerOutput.ToString("N0", spaceNumberFormat)}<sprite=4>";
            }

            if (co != null)
            {
                predictedEcoPanel?.SetActive(true);
                predictedEcoText.text = "~" + co.GetEcoCost().ToString("N0", spaceNumberFormat) + "<sprite=3>";
            }
            else
            {
                predictedEcoPanel?.SetActive(false);
                predictedEcoText.text = "";
            }
        }
        else
        {
            HidePowerUI();
        }

        if (EcoBalanceManager.Instance != null && !EcoBalanceManager.Instance.isPollutionInProgress)
            UpdateEcoBalanceUI();

        if (co != null && dynamicCostText != null)
            dynamicCostText.text = co.GetPlacementCost().ToString("N0", spaceNumberFormat) + " <sprite=0>";
    }

    public void UpdateEcoBalanceUI()
    {
        if (EcoBalanceManager.Instance == null) return;
        int newValue = Mathf.RoundToInt(EcoBalanceManager.Instance.ecoBalance);
        ecoBalanceValueText.text = $"{newValue} <sprite=3>";
        lastKnownEcoBalance = newValue;
    }

    public void TriggerEcoBalanceHitEffect(int from, int to)
    {
        if (ecoBalanceEffectRoutine != null)
            StopCoroutine(ecoBalanceEffectRoutine);
        ecoBalanceEffectRoutine = StartCoroutine(ShakeFlashAndAnimateEcoText(from, to));

        if (ecoColorResetRoutine != null)
            StopCoroutine(ecoColorResetRoutine);
        ecoColorResetRoutine = StartCoroutine(ForceColorResetAfter(0.4f, to));
    }

    private IEnumerator ForceColorResetAfter(float delay, int finalValue)
    {
        yield return new WaitForSeconds(delay);
        ecoBalanceValueText.color = ecoNormalColor;
        ecoBalanceValueText.text = $"{finalValue.ToString("N0", spaceNumberFormat)} <sprite=3>";
        ecoColorResetRoutine = null;
    }

    private IEnumerator ShakeFlashAndAnimateEcoText(int from, int to)
    {
        ecoBalanceValueText.transform.localPosition = ecoTextBasePosition;
        float shakeAmount = 5f;
        float duration = 0.3f;
        float time = 0f;
        Color originalColor = ecoBalanceValueText.color;
        ecoBalanceValueText.color = Color.red;
        float countTime = 0.25f;
        float countElapsed = 0f;
        while (countElapsed < countTime)
        {
            float t = countElapsed / countTime;
            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            ecoBalanceValueText.text = $"{value.ToString("N0", spaceNumberFormat)} <sprite=3>";
            countElapsed += Time.deltaTime;
            yield return null;
        }
        ecoBalanceValueText.text = $"{to.ToString("N0", spaceNumberFormat)} <sprite=3>";
        while (time < duration)
        {
            float offsetX = Random.Range(-shakeAmount, shakeAmount);
            float offsetY = Random.Range(-shakeAmount, shakeAmount);
            ecoBalanceValueText.transform.localPosition = ecoTextBasePosition + new Vector3(offsetX, offsetY, 0);
            time += Time.deltaTime;
            yield return null;
        }
        ecoBalanceValueText.transform.localPosition = ecoTextBasePosition;
        ecoBalanceEffectRoutine = null;
    }

    public void UpdateCreditsUI(int newValue)
    {
        if (creditsText == null)
            return;
        if (creditsAnimRoutine != null)
            StopCoroutine(creditsAnimRoutine);
        int oldValue = 0;
        if (int.TryParse(creditsText.text.Replace(" ", "").Split(' ')[0], out int parsed))
            oldValue = parsed;
        creditsAnimRoutine = StartCoroutine(AnimateCreditsChange(oldValue, newValue));
    }

    private IEnumerator AnimateCreditsChange(int from, int to)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            creditsText.text = $"{value.ToString("N0", spaceNumberFormat)} <sprite=0>";
            elapsed += Time.deltaTime;
            yield return null;
        }
        creditsText.text = $"{to.ToString("N0", spaceNumberFormat)} <sprite=0>";
        creditsAnimRoutine = null;
    }

    public void FlashCreditsRed(int currentAmount)
    {
        if (creditsFlashRoutine != null)
            StopCoroutine(creditsFlashRoutine);
        creditsFlashRoutine = StartCoroutine(FlashCreditsText(currentAmount));
    }

    private IEnumerator FlashCreditsText(int value)
    {
        Color originalColor = creditsText.color;
        Color flashColor = Color.red;
        float duration = 0.3f;
        float time = 0f;
        while (time < duration)
        {
            creditsText.color = flashColor;
            creditsText.text = $"{value.ToString("N0", spaceNumberFormat)} <sprite=0>";
            time += Time.deltaTime;
            yield return null;
        }
        creditsText.color = originalColor;
        creditsText.text = $"{value.ToString("N0", spaceNumberFormat)} <sprite=0>";
        creditsFlashRoutine = null;
    }

    public void UpdateFinalUI(int finalPower)
    {
        if (powerStatsBuild != null)
            powerStatsBuild.SetActive(false);
        ResetUI();
        UpdateEcoBalanceUI();
        buildPanel?.ShowIfStillOpen();
    }

    public void HidePowerUI()
    {
        powerStatsBuild?.SetActive(false);
        powerSlider?.gameObject.SetActive(false);
        if (powerFinalPowerText != null)
            powerFinalPowerText.text = "";
        predictedEcoPanel?.SetActive(false);
        if (predictedEcoText != null)
            predictedEcoText.text = "";
        Debug.Log("[UIManager] HidePowerUI() called.");
    }

    public void ResetUI()
    {
        powerStatsBuild?.SetActive(false);
        if (powerSlider != null)
        {
            powerSlider.gameObject.SetActive(false);
            powerSlider.maxValue = 10;
            powerSlider.value = 0;
        }
        if (powerFinalPowerText != null)
            powerFinalPowerText.text = "";
        predictedEcoPanel?.SetActive(false);
        if (predictedEcoText != null)
            predictedEcoText.text = "";
        if (dynamicCostText != null)
            dynamicCostText.text = "";
    }

    #endregion

    #region Tooltip Logic

    private void UpdateTooltipPosition(GameObject tooltipGO)
    {
        if (tooltipGO == null) return;

        RectTransform tooltipRect = tooltipGO.GetComponent<RectTransform>();
        RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
        if (tooltipRect == null || canvasRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        Vector2 screenMousePos = Input.mousePosition;
        Vector2 screenOffset = tooltipOffset;

        // Estimate final tooltip rect position in screen space
        Vector2 tooltipSize = tooltipRect.rect.size * mainCanvas.scaleFactor;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // If right edge would overflow, flip X offset
        if (screenMousePos.x + screenOffset.x + tooltipSize.x > screenWidth)
            screenOffset.x = -tooltipOffset.x - tooltipSize.x;

        // If top edge would overflow, flip Y offset
        if (screenMousePos.y + screenOffset.y + tooltipSize.y > screenHeight)
            screenOffset.y = -tooltipOffset.y - tooltipSize.y;

        Vector2 targetScreenPos = screenMousePos + screenOffset;

        // Convert screen to local anchored position
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, targetScreenPos, null, out localPoint);

        tooltipRect.anchoredPosition = localPoint;
    }





    // Unified fade in/out functions
    private IEnumerator FadeInTooltip(CanvasGroup cg)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1f;
    }

    // Shows building tooltip (for buildings, uses tooltipPanel)
    public void ShowBuildingTooltip(string id)
    {
        if (currentHoveredID == id)
            return;
        currentHoveredID = id;
        if (tooltipRoutine != null)
            StopCoroutine(tooltipRoutine);
        tooltipRoutine = StartCoroutine(ShowBuildingTooltipRoutine(id));
    }

    private IEnumerator ShowBuildingTooltipRoutine(string id)
    {
        // Immediately update tooltip position based on current mouse position.
        UpdateTooltipPosition(tooltipPanel);
        tooltipCanvasGroup.alpha = 0f;
        yield return new WaitForSeconds(0.5f);

        BuildingData data = BuildingInfoLibrary.Get(id);
        if (data != null)
        {
            tooltipName.text = data.displayName;
            tooltipClass.text = data.tagClass;
            tooltipDesc.text = data.description;
            powerValueText.text = data.maxPower.ToString("N0", spaceNumberFormat);
            ecoValueText.text = data.ecoCost.ToString("N0", spaceNumberFormat);
            rangeValueText.text = data.connectionRange.ToString("N0", spaceNumberFormat);
            costValueText.text = data.cost.ToString("N0", spaceNumberFormat);
            isTooltipVisible = true;
            yield return StartCoroutine(FadeInTooltip(tooltipCanvasGroup));
        }
    }

    public void HideBuildingTooltip()
    {
        if (tooltipRoutine != null)
            StopCoroutine(tooltipRoutine);
        tooltipCanvasGroup.alpha = 0f;
        isTooltipVisible = false;
        currentHoveredID = null;
    }

    // Shows milestone event tooltip (for events, uses eventTooltipPanel)
    public void ShowEventTooltip(string id)
    {
        if (currentHoveredID == id)
            return;
        currentHoveredID = id;
        if (eventTooltipRoutine != null)
            StopCoroutine(eventTooltipRoutine);
        eventTooltipRoutine = StartCoroutine(ShowEventTooltipRoutine(id));
    }

    private IEnumerator ShowEventTooltipRoutine(string id)
    {
        // Immediately update tooltip position based on current mouse position.
        UpdateTooltipPosition(tooltipPanel);
        tooltipCanvasGroup.alpha = 0f;
        yield return new WaitForSeconds(0.5f);

        MilestoneEventData evt = MilestoneEventLibrary.Get(id);
        if (evt != null)
        {
            eventTooltipName.text = evt.id.ToUpperInvariant();
            eventTooltipClass.text = evt.targetBuildingClass;
            eventTooltipDesc.text = evt.description;
            isEventTooltipVisible = true;
            yield return StartCoroutine(FadeInTooltip(eventTooltipCanvasGroup));
        }
    }

    public void HideEventTooltip()
    {
        if (eventTooltipRoutine != null)
            StopCoroutine(eventTooltipRoutine);
        eventTooltipCanvasGroup.alpha = 0f;
        isEventTooltipVisible = false;
        currentHoveredID = null;
    }

    #endregion

    #region Popup Effects

    public void ShowCreditGainPopup(int amount)
    {
        if (creditsPopupPrefab == null || creditsPopupAnchor == null)
            return;
        GameObject popup = Instantiate(creditsPopupPrefab, creditsPopupAnchor.position, Quaternion.identity, creditsPopupAnchor.parent);
        TextMeshProUGUI tmp = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"+{amount.ToString("N0", spaceNumberFormat)} <sprite=0>";
            tmp.color = Color.green;
        }
        StartCoroutine(FadeAndRiseAndDestroy(popup));
    }

    public void ShowCreditLossPopup(int amount)
    {
        if (creditsPopupPrefab == null || creditsPopupAnchor == null)
            return;
        GameObject popup = Instantiate(creditsPopupPrefab, creditsPopupAnchor.position, Quaternion.identity, creditsPopupAnchor.parent);
        TextMeshProUGUI tmp = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"-{amount.ToString("N0", spaceNumberFormat)} <sprite=0>";
            tmp.color = Color.red;
        }
        StartCoroutine(FadeAndRiseAndDestroy(popup));
    }

    public void ShowEcoLossPopup(int amount)
    {
        if (ecoPopupPrefab == null || ecoPopupAnchor == null)
            return;
        GameObject popup = Instantiate(ecoPopupPrefab, ecoPopupAnchor.position, Quaternion.identity, ecoPopupAnchor.parent);
        TextMeshProUGUI tmp = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"{amount.ToString("N0", spaceNumberFormat)}<sprite=3>";
            tmp.color = Color.red;
        }
        StartCoroutine(FadeAndRiseAndDestroy(popup));
    }

    private IEnumerator FadeAndRiseAndDestroy(GameObject obj)
    {
        float duration = 1f;
        float elapsed = 0f;
        if (obj == null)
            yield break;
        Vector3 startPos = obj.transform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 40f, 0);
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        while (elapsed < duration)
        {
            if (obj == null)
                yield break;
            float t = elapsed / duration;
            obj.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            cg.alpha = 1f - t;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (obj != null)
            Destroy(obj);
    }

    public void DisplayMilestoneEvent(MilestoneEventData eventData)
    {
        if (eventIconContainer == null || eventIconPrefab == null)
            return;

        // Clear any previous milestone icons
        foreach (Transform child in eventIconContainer)
            Destroy(child.gameObject);

        // Create new icon instance
        GameObject icon = Instantiate(eventIconPrefab, eventIconContainer);

        // Set hover tooltip logic on the icon
        EventIconHover hover = icon.GetComponent<EventIconHover>();
        if (hover != null)
            hover.milestoneEventID = eventData.id;

        // Set the icon image from the milestone event data
        Image iconImage = icon.GetComponent<Image>();
        if (iconImage != null && eventData.icon != null)
            iconImage.sprite = eventData.icon;
    }

    #endregion

    #region Win Screen

    public void ShowWinScreen()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            Debug.LogWarning("Win panel is not assigned in UIManager.");
        }
    }

    #endregion
}
