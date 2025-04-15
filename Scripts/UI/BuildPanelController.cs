#region BuildPanelController Class
/*
 * =========================================================
 * BuildPanelController
 *
 * Manages the build panel UI by handling slide animations,
 * fading in child UI elements in a wave effect, and toggling panel visibility.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BuildPanelController : MonoBehaviour
{
    #region Public Fields

    [Header("Core Elements")]
    [Tooltip("RectTransform for the build panel.")]
    public RectTransform panelTransform;
    [Tooltip("CanvasGroup for controlling panel visibility and interaction.")]
    public CanvasGroup canvasGroup;
    [Tooltip("Transform for the triangle arrow indicator.")]
    public Transform triangleTransform;

    [Header("Wave Animation Settings")]
    [Tooltip("Duration for the panel slide animation.")]
    public float panelSlideDuration = 0.4f;
    [Tooltip("Delay between fading in each child UI element.")]
    public float childFadeDelay = 0.05f;
    [Tooltip("Duration for fading in each child UI element.")]
    public float childFadeDuration = 0.2f;

    [Header("Positions")]
    [Tooltip("Anchored position when the panel is hidden.")]
    public Vector2 hiddenPosition = new Vector2(-350, 0);
    [Tooltip("Anchored position when the panel is visible.")]
    public Vector2 visiblePosition = new Vector2(0, 0);

    #endregion

    #region Private Fields

    private bool isOpen = false;
    private Coroutine currentAnim;
    private List<CanvasGroup> childUIGroups = new List<CanvasGroup>();

    #endregion

    #region Unity Methods

    void Start()
    {
        // Cache all direct children of panelTransform that have a CanvasGroup.
        foreach (Transform child in panelTransform)
        {
            var cg = child.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                childUIGroups.Add(cg);
            }
        }

        // Initialize panel as closed.
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelTransform.anchoredPosition = hiddenPosition;
    }

    #endregion

    #region Public Methods

    // Toggles the build panel open/closed.
    public void TogglePanel()
    {
        if (currentAnim != null)
            StopCoroutine(currentAnim);
        isOpen = !isOpen;
        currentAnim = StartCoroutine(AnimatePanel(isOpen));
    }

    // Hides the panel temporarily if open.
    public void HideTemporarily()
    {
        if (isOpen && currentAnim == null)
            StartCoroutine(AnimatePanel(false));
    }

    // Shows the panel if it is open.
    public void ShowIfStillOpen()
    {
        if (isOpen && currentAnim == null)
            StartCoroutine(AnimatePanel(true));
    }

    #endregion

    #region Private Methods

    // Animates the panel sliding and fading in/out.
    private IEnumerator AnimatePanel(bool opening)
    {
        float elapsed = 0f;
        Vector2 start = panelTransform.anchoredPosition;
        Vector2 end = opening ? visiblePosition : hiddenPosition;
        float startAlpha = canvasGroup.alpha;
        float endAlpha = opening ? 1f : 0f;

        // Rotate the triangle arrow.
        float targetRotationZ = opening ? 180f : 0f;
        Quaternion startRot = triangleTransform.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetRotationZ);

        while (elapsed < panelSlideDuration)
        {
            float t = elapsed / panelSlideDuration;
            panelTransform.anchoredPosition = Vector2.Lerp(start, end, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            triangleTransform.rotation = Quaternion.Lerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        panelTransform.anchoredPosition = end;
        canvasGroup.alpha = endAlpha;
        triangleTransform.rotation = endRot;

        if (opening)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(WaveFadeIn());
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            foreach (var cg in childUIGroups)
            {
                cg.alpha = 0;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        currentAnim = null;
    }

    // Fades in child UI elements in a wave effect.
    private IEnumerator WaveFadeIn()
    {
        foreach (var cg in childUIGroups)
        {
            StartCoroutine(FadeInUI(cg));
            yield return new WaitForSeconds(childFadeDelay);
        }
    }

    // Fades in a single CanvasGroup.
    private IEnumerator FadeInUI(CanvasGroup cg)
    {
        float elapsed = 0f;
        while (elapsed < childFadeDuration)
        {
            float t = elapsed / childFadeDuration;
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    #endregion
}
