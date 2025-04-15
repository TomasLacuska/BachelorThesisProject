#region BuildTabController Class
/*
 * =========================================================
 * BuildTabController
 *
 * Manages tab switching in the build UI. It activates the selected tab's content,
 * fades in its child elements in a wave effect, and sets up button click events.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BuildTabController : MonoBehaviour
{
    #region Inner Classes

    [System.Serializable]
    public class Tab
    {
        [Tooltip("Button to select this tab.")]
        public Button tabButton;
        [Tooltip("Content GameObject for this tab.")]
        public GameObject tabContent;
    }

    #endregion

    #region Public Fields

    [Tooltip("Array of tabs to manage.")]
    public Tab[] tabs;
    [Tooltip("Delay between fading in each child UI element in the tab.")]
    public float childFadeDelay = 0.05f;
    [Tooltip("Duration for fading in each child UI element.")]
    public float childFadeDuration = 0.2f;

    #endregion

    #region Private Fields

    private int currentTab = 0;
    private Coroutine waveRoutine;

    #endregion

    #region Unity Methods

    void Start()
    {
        // Assign click events to each tab button.
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].tabButton.onClick.AddListener(() => SwitchTab(index));
        }
        // Activate the first tab.
        SwitchTab(0);
    }

    #endregion

    #region Public Methods

    // Switches to the specified tab index.
    public void SwitchTab(int index)
    {
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);

        // Activate content for the selected tab and deactivate others.
        for (int i = 0; i < tabs.Length; i++)
            tabs[i].tabContent.SetActive(i == index);

        currentTab = index;
        waveRoutine = StartCoroutine(FadeInTabChildren(tabs[index].tabContent.transform));
    }

    #endregion

    #region Private Methods

    // Fades in all direct child UI elements of the specified tab content.
    private IEnumerator FadeInTabChildren(Transform tabParent)
    {
        List<CanvasGroup> uiElements = new List<CanvasGroup>();

        foreach (Transform child in tabParent)
        {
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = child.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            uiElements.Add(cg);
        }

        foreach (var cg in uiElements)
        {
            StartCoroutine(FadeIn(cg));
            yield return new WaitForSeconds(childFadeDelay);
        }
    }

    // Fades in a single CanvasGroup.
    private IEnumerator FadeIn(CanvasGroup cg)
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
