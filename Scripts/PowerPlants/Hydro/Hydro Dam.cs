#region HydroDam Class
/*
 * =========================================================
 * HydroDam
 *
 * Manages the placement and behavior of hydro dam buildings.
 * It snaps the dam to valid hydro spots, creates footprint indicators,
 * finalizes placement by triggering water rise, and restores appearance.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ConnectableObject))]
public class HydroDam : MonoBehaviour
{
    #region Public Fields

    [Header("Terrain & Spot Settings")]
    [Tooltip("Layer mask for terrain (used to raycast).")]
    public LayerMask terrainLayer;
    [Tooltip("Distance threshold for snapping to a HydroDam spot.")]
    public float snapThreshold = 2f;

    [Header("Indicator Settings (HydroDam Footprint)")]
    [Tooltip("Raycast height used for footprint alignment.")]
    public float footprintRaycastHeight = 50f;
    [Tooltip("Vertical offset for the footprint indicator.")]
    public float footprintYOffset = 0.05f;
    [Tooltip("Thick line width for the hydro spot indicator.")]
    public float indicatorLineWidth = 0.2f;
    [Tooltip("Color for the hydro spot indicator.")]
    public Color indicatorColor = Color.yellow;
    [Tooltip("Optional: Custom material for the indicator. Uses default if not assigned.")]
    public Material indicatorMaterial;

    #endregion

    #region Private Fields

    // Internal structure to store hydro spot indicator data.
    private class HydroSpotIndicatorData
    {
        public Transform spot;
        public LineRenderer lineRenderer;
        public Vector3[] localCorners;
    }
    // List of footprint indicators.
    private List<HydroSpotIndicatorData> hydroSpotIndicators = new List<HydroSpotIndicatorData>();
    // Flag to track if the dam is in placement mode.
    private bool isBeingPlaced = false;

    #endregion

    #region Unity Methods

    void Update()
    {
        if (isBeingPlaced)
        {
            // Snap to the nearest valid hydro spot.
            SnapToNearestSpot();
            // Update footprint indicators to align with terrain.
            UpdateHydroSpotIndicators();
        }
    }

    #endregion

    #region Public Methods

    // Begin placement mode and create footprint indicators.
    public void BeginPlacement()
    {
        isBeingPlaced = true;
        CreateHydroSpotIndicators();
    }

    // Snap the dam to the nearest hydro spot if within snapThreshold; otherwise, follow the mouse.
    public void SnapToNearestSpot()
    {
        List<Transform> spots = HydroRegistry.Instance.HydroSpots;
        if (spots == null || spots.Count == 0)
            return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayer))
            return;
        Transform closestSpot = null;
        float minDist = Mathf.Infinity;
        foreach (Transform spot in spots)
        {
            if (!spot.gameObject.activeInHierarchy)
                continue;
            float dist = Vector3.Distance(hit.point, spot.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestSpot = spot;
            }
        }
        if (closestSpot != null && minDist < snapThreshold)
            transform.position = closestSpot.position;
        else
            transform.position = hit.point;
    }

    // Check if the dam is snapped to any valid hydro spot.
    public bool IsSnappedToValidSpot()
    {
        List<Transform> spots = HydroRegistry.Instance.HydroSpots;
        if (spots == null || spots.Count == 0)
            return false;
        foreach (Transform spot in spots)
        {
            if (!spot.gameObject.activeInHierarchy)
                continue;
            if (Vector3.Distance(transform.position, spot.position) < snapThreshold)
                return true;
        }
        return false;
    }

    // Finalize placement: if snapped to a valid spot, trigger water rise, restore appearance, and clear indicators.
    public void FinalizePlacement()
    {
        if (!IsSnappedToValidSpot())
        {
            Debug.LogWarning("[HydroDam] Cannot finalize placement: not snapped to a valid spot.");
            return;
        }

        isBeingPlaced = false;

        // Snap to the closest valid spot.
        List<Transform> spots = HydroRegistry.Instance.HydroSpots;
        Transform chosenSpot = null;
        float minDist = Mathf.Infinity;
        foreach (Transform spot in spots)
        {
            if (!spot.gameObject.activeInHierarchy)
                continue;
            float dist = Vector3.Distance(transform.position, spot.position);
            if (dist < minDist && dist < snapThreshold)
            {
                minDist = dist;
                chosenSpot = spot;
            }
        }

        if (chosenSpot != null)
        {
            transform.position = chosenSpot.position;
            HydroDamSpot spotScript = chosenSpot.GetComponent<HydroDamSpot>();
            if (spotScript != null)
            {
                spotScript.TriggerWaterRise();
                spotScript.DisableBuildable();
            }
        }
        else
        {
            Debug.LogWarning("[HydroDam] No valid spot found within snap range.");
            return;
        }

        // Restore building materials to default.
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                    mat.color = Color.white;
            }
        }

        // Reset mesh position and rotation.
        Transform mesh = transform.Find("Mesh");
        if (mesh != null)
        {
            mesh.localPosition = Vector3.zero;
            mesh.localRotation = Quaternion.identity;
        }

        // Destroy all footprint indicators.
        foreach (var data in hydroSpotIndicators)
        {
            if (data.lineRenderer != null)
                Destroy(data.lineRenderer.gameObject);
        }
        hydroSpotIndicators.Clear();

        // Disable any range indicator if present.
        RangeIndicator indicator = GetComponentInChildren<RangeIndicator>(true);
        if (indicator != null)
            indicator.DisableIndicator();
    }

    // Cancel placement mode and remove footprint indicators.
    public void CancelPlacement()
    {
        isBeingPlaced = false;
        foreach (var data in hydroSpotIndicators)
        {
            if (data.lineRenderer != null)
                Destroy(data.lineRenderer.gameObject);
        }
        hydroSpotIndicators.Clear();
    }

    #endregion

    #region Private Methods

    // Create footprint indicators for each valid hydro spot.
    private void CreateHydroSpotIndicators()
    {
        // Clear existing indicators.
        foreach (var d in hydroSpotIndicators)
        {
            if (d.lineRenderer != null)
                Destroy(d.lineRenderer.gameObject);
        }
        hydroSpotIndicators.Clear();

        List<Transform> spots = HydroRegistry.Instance.HydroSpots;
        foreach (Transform spot in spots)
        {
            if (!spot.gameObject.activeInHierarchy)
                continue;
            HydroSpotIndicatorData data = new HydroSpotIndicatorData();
            data.spot = spot;

            // Create indicator GameObject and add LineRenderer.
            GameObject indicatorObj = new GameObject("HydroSpotIndicator");
            LineRenderer lr = indicatorObj.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.startWidth = indicatorLineWidth;
            lr.endWidth = indicatorLineWidth;
            lr.useWorldSpace = true;
            if (indicatorMaterial != null)
                lr.material = indicatorMaterial;
            else
            {
                Material defaultMat = new Material(Shader.Find("Unlit/Color"));
                defaultMat.color = indicatorColor;
                defaultMat.EnableKeyword("_EMISSION");
                defaultMat.SetColor("_EmissionColor", indicatorColor);
                lr.material = defaultMat;
            }
            data.lineRenderer = lr;

            // Set up local corners based on the spot's collider.
            Collider col = spot.GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Vector3 center = box.center;
                Vector3 extents = box.size * 0.5f;
                data.localCorners = new Vector3[]
                {
                    center + new Vector3(-extents.x, 0, -extents.z),
                    center + new Vector3(extents.x, 0, -extents.z),
                    center + new Vector3(extents.x, 0, extents.z),
                    center + new Vector3(-extents.x, 0, extents.z)
                };
            }
            else
            {
                data.localCorners = new Vector3[]
                {
                    new Vector3(-1, 0, -1),
                    new Vector3(1, 0, -1),
                    new Vector3(1, 0, 1),
                    new Vector3(-1, 0, 1)
                };
            }
            hydroSpotIndicators.Add(data);
        }
    }

    // Update each footprint indicator so that it aligns with the terrain.
    private void UpdateHydroSpotIndicators()
    {
        foreach (var data in hydroSpotIndicators)
        {
            if (data.lineRenderer == null || data.localCorners == null)
                continue;
            Vector3[] worldCorners = new Vector3[data.localCorners.Length + 1];
            for (int i = 0; i < data.localCorners.Length; i++)
            {
                Vector3 local = data.localCorners[i];
                Vector3 worldPoint = data.spot.TransformPoint(local);
                Vector3 rayOrigin = worldPoint + Vector3.up * footprintRaycastHeight;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, footprintRaycastHeight * 2, terrainLayer))
                    worldPoint.y = hit.point.y + footprintYOffset;
                worldCorners[i] = worldPoint;
            }
            worldCorners[worldCorners.Length - 1] = worldCorners[0];
            data.lineRenderer.positionCount = worldCorners.Length;
            data.lineRenderer.SetPositions(worldCorners);
        }
    }

    #endregion
}
