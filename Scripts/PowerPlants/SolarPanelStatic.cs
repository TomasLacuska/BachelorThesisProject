#region SolarPanelStatic Class
/*
 * =========================================================
 * SolarPanelStatic
 *
 * Manages static solar panel behavior by computing sunlight multipliers,
 * updating power production, and finalizing panel placement.
 * =========================================================
 */
#endregion

using UnityEngine;

[RequireComponent(typeof(ConnectableObject))]
public class SolarPanelStatic : MonoBehaviour
{
    #region Public Fields

    [Header("Solar Panel Settings")]
    [Tooltip("Pivot that tilts with the building.")]
    public Transform panelSurfacePivot;
    [Tooltip("Origin point for the raycast; usually placed above the mesh center.")]
    public Transform raycastOrigin;
    [Tooltip("Maximum distance for the raycast.")]
    public float maxRayDistance = 1000f;
    [Tooltip("Layer mask to detect obstructions that block sunlight.")]
    public LayerMask obstructionLayer;

    [Header("Sunlight Update Settings")]
    [Tooltip("Interval for updating sunlight multiplier in ghost preview mode.")]
    public float previewUpdateInterval = 0.1f;
    [Tooltip("Interval for updating sunlight multiplier after placement.")]
    public float builtUpdateInterval = 0.5f;
    [Tooltip("Indicates if the panel is in ghost preview mode.")]
    public bool isGhostPreview = true;
    [Tooltip("If enabled, draws debug rays for sunlight calculation.")]
    public bool debugRay = false;

    // Exposes the current sunlight multiplier as a read-only property.
    public float CurrentMultiplier => lastSunlightMultiplier;

    #endregion

    #region Private Fields

    private float updateTimer = 0f;
    private float lastSunlightMultiplier = 1f;
    private ConnectableObject connectableObject;

    #endregion

    #region Unity Methods

    void Awake()
    {
        connectableObject = GetComponent<ConnectableObject>();
        if (panelSurfacePivot == null)
            Debug.LogError("PanelSurfacePivot is not assigned.");
        if (raycastOrigin == null)
            raycastOrigin = transform;
    }

    void OnEnable()
    {
        MilestoneEvents.OnMilestoneChanged += ForceUpdate;
    }

    void OnDisable()
    {
        MilestoneEvents.OnMilestoneChanged -= ForceUpdate;
    }

    void Update()
    {
        float currentInterval = isGhostPreview ? previewUpdateInterval : builtUpdateInterval;
        updateTimer += Time.deltaTime;
        if (updateTimer >= currentInterval)
        {
            updateTimer = 0f;
            lastSunlightMultiplier = ComputeSunlightMultiplier();
            connectableObject.UpdateWithEnvironmentMultiplier(lastSunlightMultiplier);
        }
    }

    #endregion

    #region Public Methods

    public float ComputeSunlightMultiplier()
    {
        if (RenderSettings.sun == null || panelSurfacePivot == null)
            return 0f;
        Vector3 sunDirection = -RenderSettings.sun.transform.forward;
        Vector3 panelNormal = panelSurfacePivot.up;
        float dot = Mathf.Clamp01(Vector3.Dot(sunDirection, panelNormal));

        float angleFactor;
        if (dot >= 1f)
            angleFactor = 2f;
        else if (dot >= 0.5f)
            angleFactor = Mathf.Lerp(0.9f, 2f, (dot - 0.5f) * 2f);
        else
            angleFactor = Mathf.Lerp(0f, 0.9f, dot * 2f);

        Ray ray = new Ray(raycastOrigin.position, sunDirection);
        bool obstructed = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, obstructionLayer)
                          && !hit.collider.transform.IsChildOf(transform);

        if (debugRay)
        {
            if (obstructed)
                Debug.DrawRay(raycastOrigin.position, sunDirection * hit.distance, Color.red);
            else
                Debug.DrawRay(raycastOrigin.position, sunDirection * maxRayDistance, Color.green);
        }

        float obstructionFactor = obstructed ? 0f : 1f;
        return angleFactor * obstructionFactor;
    }

    public void FinalizePlacement()
    {
        isGhostPreview = false;
        updateTimer = 0f;
        lastSunlightMultiplier = ComputeSunlightMultiplier();
        connectableObject.UpdateWithEnvironmentMultiplier(lastSunlightMultiplier);
    }

    public float GetPreviewMultiplier()
    {
        return ComputeSunlightMultiplier();
    }

    // Newly added for consistency: When the building is confirmed, disable ghost preview.
    public void OnBuildingPlaced()
    {
        FinalizePlacement();
    }

    #endregion

    #region Event Handler

    private void ForceUpdate()
    {
        float multiplier = ComputeSunlightMultiplier();
        connectableObject.UpdateWithEnvironmentMultiplier(multiplier);
        MilestoneEvents.NotifyDynamicBuildingUpdated();
    }

    #endregion
}
