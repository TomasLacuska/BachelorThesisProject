#region DecalExpander Class
/*
 * =========================================================
 * DecalExpander
 *
 * Expands a decal based on a pollution value by scaling it
 * over a specified duration.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections;

public class DecalExpander : MonoBehaviour
{
    #region Public Fields

    [Tooltip("Pollution value; target scale is 2x this value on X and Y.")]
    public float pollutionValue = 20f;
    [Tooltip("Time in seconds for the decal to expand to full size.")]
    public float expansionDuration = 4f;

    #endregion

    #region Private Fields

    private Vector3 targetScale;
    private bool initialized = false;

    #endregion

    #region Public Methods

    // Sets the pollution value externally and computes the target scale.
    // pollutionValue: the pollution value to use.
    public void SetPollutionValue(float value)
    {
        pollutionValue = value;
        targetScale = new Vector3(pollutionValue * 2f, pollutionValue * 2f, 1f);
        initialized = true;
    }

    #endregion

    #region Unity Methods

    void Start()
    {
        // If not explicitly initialized, use the preset pollutionValue.
        if (!initialized)
        {
            targetScale = new Vector3(pollutionValue * 2f, pollutionValue * 2f, 1f);
        }
        // Start with zero scale on X and Y.
        transform.localScale = new Vector3(0f, 0f, 1f);
        StartCoroutine(ExpandDecal());
    }

    #endregion

    #region Coroutines

    // Smoothly expands the decal to the target scale over the given duration.
    private IEnumerator ExpandDecal()
    {
        float elapsed = 0f;
        Vector3 startScale = new Vector3(0f, 0f, 1f);
        while (elapsed < expansionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expansionDuration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        // Ensure final scale is exactly target scale.
        transform.localScale = targetScale;
    }

    #endregion
}
