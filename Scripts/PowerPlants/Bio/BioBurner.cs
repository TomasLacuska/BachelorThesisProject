#region BioBurner Class
/*
 * =========================================================
 * BioBurner
 *
 * A dynamic power-producing building that uses nearby unreserved farms
 * to compute an environmental multiplier for energy output.
 *
 * This script manages farm detection, ghost preview updates, and
 * final reservation logic upon placement.
 *
 * Requires: ConnectableObject component on the same GameObject.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ConnectableObject))]
public class BioBurner : MonoBehaviour
{
    #region Inspector Fields

    [Header("BioBurner Settings")]
    [Tooltip("Detection radius to search for nearby farm objects.")]
    public float detectionRadius = 15f;

    [Tooltip("Layer mask for farm objects.")]
    public LayerMask farmLayer;

    [Tooltip("Multiplier added per reserved farm (starting from 0).")]
    public float farmMultiplierIncrement = 1f;

    [Header("Preview Settings")]
    [Tooltip("Is this building in ghost (preview) mode?")]
    public bool isGhostPreview = true;

    #endregion

    #region Private Fields

    private ConnectableObject connectableObject;
    private RangeIndicator farmRangeIndicator;
    private int previewFarmCount = 0;
    public List<Farm> reservedFarms = new();

    #endregion

    #region Computed Properties

    public float CurrentMultiplier =>
        previewFarmCount > 0 ? (1f + previewFarmCount * farmMultiplierIncrement) : 0f;

    #endregion

    #region Unity Methods

    void Awake()
    {
        connectableObject = GetComponent<ConnectableObject>();
        if (connectableObject == null)
            Debug.LogError("BioBurner requires a ConnectableObject component.");

        foreach (RangeIndicator indicator in GetComponentsInChildren<RangeIndicator>(true))
        {
            if (indicator.gameObject.CompareTag("Farm"))
            {
                farmRangeIndicator = indicator;
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
        UpdatePreviewIndicator();
        UpdatePreviewMultiplier();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    void OnDestroy()
    {
        foreach (Farm farm in reservedFarms)
        {
            if (farm != null)
                farm.Release();
        }
    }

    #endregion

    #region Preview and Placement Logic

    private void UpdatePreviewIndicator()
    {
        if (farmRangeIndicator == null) return;

        if (isGhostPreview)
        {
            if (!farmRangeIndicator.gameObject.activeSelf)
                farmRangeIndicator.EnableIndicator(true);

            farmRangeIndicator.dynamicIndicator = true;
            farmRangeIndicator.transform.localScale = Vector3.one * detectionRadius;
        }
        else if (farmRangeIndicator.gameObject.activeSelf)
        {
            farmRangeIndicator.DisableIndicator();
        }
    }

    private void UpdatePreviewMultiplier()
    {
        if (!isGhostPreview) return;

        previewFarmCount = ComputeAvailableFarmCount();
        connectableObject?.UpdateWithEnvironmentMultiplier(CurrentMultiplier);
    }

    private int ComputeAvailableFarmCount()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, farmLayer);
        int count = 0;

        foreach (Collider col in hits)
        {
            Farm farm = col.GetComponent<Farm>();
            if (farm != null && !farm.IsReserved())
                count++;
        }
        return count;
    }

    public void OnBuildingPlaced()
    {
        FinalizePlacement();
    }

    public void FinalizePlacement()
    {
        isGhostPreview = false;

        if (farmRangeIndicator != null && farmRangeIndicator.gameObject.activeSelf)
            farmRangeIndicator.DisableIndicator();

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, farmLayer);
        foreach (Collider col in hits)
        {
            Farm farm = col.GetComponent<Farm>();
            if (farm != null && !farm.IsReserved())
            {
                farm.Reserve(this);
                reservedFarms.Add(farm);
            }
        }

        float multiplier = reservedFarms.Count > 0
            ? (1f + reservedFarms.Count * farmMultiplierIncrement)
            : 0f;

        connectableObject?.UpdateWithEnvironmentMultiplier(multiplier);
    }

    #endregion

    #region Event Handler

    private void ForceUpdate()
    {
        connectableObject?.UpdateWithEnvironmentMultiplier(CurrentMultiplier);
        MilestoneEvents.NotifyDynamicBuildingUpdated();
    }

    #endregion
}
