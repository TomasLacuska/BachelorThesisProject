#region WindTurbine Class
/*
 * =========================================================
 * WindTurbine
 *
 * Controls wind turbine behavior including power production based on
 * height sampling and environmental factors. Also manages ghost preview mode,
 * proximity conflicts, and head bone tilt animation upon placement.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ConnectableObject))]
public class WindTurbine : MonoBehaviour
{
    #region Public Fields

    [Header("Wind Turbine Settings")]
    [Tooltip("Diameter for sampling wind height.")]
    public float samplingDiameter = 10f;
    [Tooltip("Number of grid subdivisions for height sampling.")]
    public int gridSubdivisions = 5;
    
    [Header("Height Classification Thresholds")]
    [Tooltip("Minimum height threshold for low wind classification.")]
    public float lowHeightThreshold = 25f;
    [Tooltip("Maximum height threshold for high wind classification.")]
    public float highHeightThreshold = 60f;
    
    [Header("Raycast Settings")]
    [Tooltip("Starting height for the raycast used in sampling.")]
    public float heightSamplingRaycast = 100f;
    [Tooltip("Layer mask used for height sampling raycasts.")]
    public LayerMask samplingLayer;

    [Header("Debug Settings")]
    [Tooltip("Enable to draw debug rays for sampling.")]
    public bool debugRaycast = false;
    [Tooltip("Multiplier used for debugging wind effects.")]
    public float debugMultiplier = 1f;

    [Header("Ghost Mode")]
    [Tooltip("True if the turbine is in ghost preview mode.")]
    public bool isGhostPreview = true;
    
    [Header("Head Bone Tilt Control")]
    [Tooltip("Reference to the head bone for tilt animation.")]
    public Transform headBone;
    [Tooltip("If true, tilts head bone in world space; otherwise local space.")]
    public bool tiltInWorldSpace = false;
    [Tooltip("Target Euler angles for head tilt animation.")]
    public Vector3 headTargetEulerAngles = new Vector3(0f, 45f, 0f);
    [Tooltip("Duration of the head tilt animation in seconds.")]
    public float tiltDuration = 1f;

    #endregion

    #region Private Fields

    private bool proximityConflict = false;
    private ConnectableObject connectableObject;
    private Animator turbineAnimator;
    private RangeIndicator windRangeIndicator;

    #endregion

    #region Unity Methods

    void Awake()
    {
        connectableObject = GetComponent<ConnectableObject>();
        if (connectableObject == null)
            Debug.LogError("WindTurbine requires a ConnectableObject component.");

        turbineAnimator = GetComponentInChildren<Animator>();
        if (turbineAnimator != null)
            turbineAnimator.enabled = false;
        else
            Debug.LogWarning("No Animator found on WindTurbine.");

        if (headBone == null)
        {
            headBone = transform.Find("Head");
            if (headBone == null)
                Debug.LogWarning("Head bone not assigned or found.");
        }

        RangeIndicator[] indicators = GetComponentsInChildren<RangeIndicator>(true);
        foreach (RangeIndicator indicator in indicators)
        {
            if (indicator.gameObject.CompareTag("Wind"))
            {
                windRangeIndicator = indicator;
                break;
            }
        }
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
        if (windRangeIndicator != null)
        {
            if (isGhostPreview)
            {
                if (!windRangeIndicator.gameObject.activeSelf)
                    windRangeIndicator.EnableIndicator(true);
                else
                    windRangeIndicator.dynamicIndicator = true;
                windRangeIndicator.transform.localScale = Vector3.one * (samplingDiameter / 2);
            }
            else
            {
                if (windRangeIndicator.gameObject.activeSelf)
                    windRangeIndicator.DisableIndicator();
            }
        }

        if (isGhostPreview)
        {
            debugMultiplier = ComputeHeightMultiplier();
            connectableObject.UpdateWithEnvironmentMultiplier(debugMultiplier);
        }
    }

    #endregion

    #region Public Methods

    public float ComputeHeightMultiplier()
    {
        int lowCount = 0, midCount = 0, highCount = 0, validSamples = 0;
        proximityConflict = false;
        
        float radius = samplingDiameter * 0.5f;
        int samples = Mathf.Max(1, gridSubdivisions);
        
        for (int i = 0; i < samples; i++)
        {
            for (int j = 0; j < samples; j++)
            {
                float percentX = (samples == 1) ? 0.5f : i / (float)(samples - 1);
                float percentZ = (samples == 1) ? 0.5f : j / (float)(samples - 1);
                float offsetX = Mathf.Lerp(-radius, radius, percentX);
                float offsetZ = Mathf.Lerp(-radius, radius, percentZ);
                Vector2 offset = new Vector2(offsetX, offsetZ);
                if (offset.magnitude > radius)
                    continue;
                Vector3 samplePosition = new Vector3(
                    transform.position.x + offsetX,
                    transform.position.y,
                    transform.position.z + offsetZ
                );
                Vector3 rayOrigin = samplePosition + Vector3.up * heightSamplingRaycast;
                Ray ray = new Ray(rayOrigin, Vector3.down);

                if (Physics.Raycast(ray, out RaycastHit hit, heightSamplingRaycast * 2f, samplingLayer))
                {
                    // Instead of simply checking for a WindTurbine component,
                    // we check if the hit object's ConnectableObject has a building tag of "Wind"
                    ConnectableObject otherCO = hit.collider.GetComponentInParent<ConnectableObject>();
                    if (otherCO != null)
                    {
                        BuildingData data = BuildingInfoLibrary.Get(otherCO.buildingID);
                        if (data != null && data.tagClass == "Wind" && otherCO != connectableObject)
                        {
                            proximityConflict = true;
                            if (debugRaycast)
                                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.blue, 0.1f);
                            continue; // Skip this sample as it's invalid
                        }
                    }

                    // If no conflicting wind building detected, count the sample.
                    float y = hit.point.y;
                    if (y < lowHeightThreshold)
                        lowCount++;
                    else if (y > highHeightThreshold)
                        highCount++;
                    else
                        midCount++;
                    validSamples++;
                    if (debugRaycast && isGhostPreview)
                        Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow, 0.1f);
                }
            }
        }

        float multiplier = validSamples > 0
            ? (2f * lowCount + 1f * midCount) / validSamples
            : 1f;
        return multiplier;
    }

    public bool IsPlacementValid()
    {
        return !proximityConflict;
    }

    public void OnBuildingPlaced()
    {
        FinalizePlacement();
    }

    public void FinalizePlacement()
    {
        isGhostPreview = false;
        debugRaycast = false;
        debugMultiplier = ComputeHeightMultiplier();
        connectableObject.UpdateWithEnvironmentMultiplier(debugMultiplier);

        if (turbineAnimator != null)
            turbineAnimator.enabled = true;

        if (headBone != null)
            StartCoroutine(TiltHeadBone());
    }

    #endregion

    #region Coroutines

    private IEnumerator TiltHeadBone()
    {
        Quaternion startRot = tiltInWorldSpace ? headBone.rotation : headBone.localRotation;
        Quaternion endRot = Quaternion.Euler(headTargetEulerAngles);
        float elapsed = 0f;
        while (elapsed < tiltDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tiltDuration);
            if (tiltInWorldSpace)
                headBone.rotation = Quaternion.Slerp(startRot, endRot, t);
            else
                headBone.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        if (tiltInWorldSpace)
            headBone.rotation = endRot;
        else
            headBone.localRotation = endRot;
    }

    #endregion

    #region Event Handler

    private void ForceUpdate()
    {
        float multiplier = ComputeHeightMultiplier();
        connectableObject.UpdateWithEnvironmentMultiplier(multiplier);
        MilestoneEvents.NotifyDynamicBuildingUpdated();
    }

    #endregion
}
