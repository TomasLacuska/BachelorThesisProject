#region HydroDamSpot Class
/*
 * =========================================================
 * HydroDamSpot
 *
 * Represents a hydro dam placement spot. Controls water rise animation,
 * disables buildable state, and deactivates itself after triggering water rise.
 * =========================================================
 */
#endregion

using UnityEngine;

public class HydroDamSpot : MonoBehaviour
{
    #region Public Fields

    [Tooltip("Reference to the water plane that should rise for this spot.")]
    public GameObject waterPlane;
    [Tooltip("Target Y height for the water after this dam is built.")]
    public float waterTargetY = 10f;
    [Tooltip("Duration for the water rise animation in seconds.")]
    public float waterRiseDuration = 5f;

    #endregion

    #region Private Fields

    // Flag to ensure the water rise is triggered only once.
    private bool hasBeenBuilt = false;

    #endregion

    #region Public Methods

    // Triggers the water rise animation and disables the buildable spot.
    public void TriggerWaterRise()
    {
        if (hasBeenBuilt) return;
        hasBeenBuilt = true;

        // Activate the water plane if it's not active.
        if (waterPlane != null)
        {
            if (!waterPlane.activeSelf)
                waterPlane.SetActive(true);

            // Detach the water plane so it remains active.
            waterPlane.transform.parent = null;
            WaterRiseController controller = waterPlane.GetComponent<WaterRiseController>();
            if (controller != null)
            {
                controller.StartRising(waterTargetY, waterRiseDuration);
            }
        }
        // Disable buildable functionality and deactivate this spot.
        DisableBuildable();
        gameObject.SetActive(false);
    }

    // Disables the spot's collider to prevent further building.
    public void DisableBuildable()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }

    #endregion
}
