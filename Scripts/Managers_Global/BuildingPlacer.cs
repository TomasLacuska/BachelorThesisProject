#region BuildingPlacer Class
/*
 * =========================================================
 * BuildingPlacer
 *
 * Manages the logic for placing buildings in the game world.
 * Normal buildings use standard placement logic (ghost preview,
 * footprint indicator, etc.), while if the building has a HydroDam
 * component, placement is delegated to HydroDam.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuildingPlacer : MonoBehaviour
{
    #region Public Fields

    [Header("Layer Settings")]
    [Tooltip("Layer mask used for the terrain.")]
    public LayerMask terrainLayer;
    [Tooltip("Layer mask used to detect obstructions (blocks normal buildings).")]
    public LayerMask obstructionLayer;

    [Header("Raycast & Placement Settings")]
    [Tooltip("Height above the sample point from which we cast downward.")]
    public float raycastHeight = 100f;
    [Tooltip("Vertical offset for final building position above ground.")]
    public float finalPlacementYOffset = 0.0f;

    [Header("Rotation Settings")]
    [Tooltip("Key to lock rotation (for normal buildings).")]
    public KeyCode rotationKey = KeyCode.R;

    [Header("Ghost Visuals")]
    [Tooltip("Color for valid placement.")]
    public Color validColor = Color.green;
    [Tooltip("Color for invalid placement.")]
    public Color invalidColor = Color.red;
    [Tooltip("Vertical hover amplitude for the 'ghost' building preview.")]
    public float hoverAmplitude = 0.2f;
    [Tooltip("Speed of the up/down hover effect.")]
    public float hoverSpeed = 2f;
    [Tooltip("Base Y offset for the ghost preview above the ground.")]
    public float ghostBaseYOffset = 2.0f;
    [Tooltip("Duration of the 'slam' animation when finalizing placement.")]
    public float slamDuration = 0.2f;

    [Header("Tilt Settings")]
    [Tooltip("Multiplier for how much ghost movement affects tilt.")]
    public float tiltSensitivity = 10f;
    [Tooltip("Max tilt in degrees.")]
    public float maxTiltAngle = 30f;
    [Tooltip("Smoothing time for tilt changes.")]
    public float tiltSmoothTime = 0.1f;

    [Header("Final Placement & VFX")]
    [Tooltip("Dust VFX prefab to spawn when a building is placed.")]
    public GameObject dustVFXPrefab;
    [Tooltip("Vertical offset for the dust VFX (fine-tuning effect height).")]
    public float vfxYOffset = 1.0f;

    [Header("Decal Settings")]
    [Tooltip("Decal prefab (with DecalExpander attached) to show pollution spreading.")]
    public GameObject pollutionDecalPrefab;

    [Header("Footprint Indicator Settings (Normal Buildings)")]
    [Tooltip("Vertical offset applied to the footprint indicator above the terrain.")]
    public float footprintYOffset = 0.05f;
    [Tooltip("Line width for the footprint indicator.")]
    public float footprintLineWidth = 0.05f;
    [Tooltip("Raycast height for sampling terrain under the footprint indicator.")]
    public float footprintRaycastHeight = 50f;

    [Header("Debug Options")]
    [Tooltip("Show debug raycast lines for slope checking.")]
    public bool debugPlacementRaycasts = false;
    [Tooltip("Show debug rotation tracking gizmos (from locked position to cursor hit etc.).")]
    public bool debugRotationTracking = false;

    #endregion

    #region Private Fields

    private GameObject buildingPrefab;
    private GameObject currentPreview;
    private bool isPlacing = false;
    private Vector3 basePosition;
    private Vector3 lastMeshWorldPos;
    private float currentTiltX = 0f;
    private float currentTiltZ = 0f;
    private Quaternion baseRotation;

    private GameObject footprintIndicator;
    private LineRenderer footprintLR;
    private Dictionary<Renderer, Color[]> originalMaterialColors;
    private Transform meshParent;
    private Vector3[] initialLocalOffsets;
    private Bounds originalColliderBounds;

    // Flag for HydroDam placement mode
    private bool isHydroDamPlacement = false;

    // Rotation lock and snapping toggle for normal buildings
    private bool isRotationLock = false;
    private Vector3 lockedPosition;
    private bool isRotationSnapEnabled = false;

    #endregion

    #region Unity Methods

    void Update()
    {
        if (!isPlacing || currentPreview == null) return;

        // Toggle rotation snapping with TAB (only during build)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isRotationSnapEnabled = !isRotationSnapEnabled;
            Debug.Log("Rotation snapping " + (isRotationSnapEnabled ? "enabled" : "disabled"));
        }

        // Handle rotation lock input (only for normal buildings)
        if (!isHydroDamPlacement)
        {
            if (Input.GetKeyDown(rotationKey))
            {
                isRotationLock = true;
                lockedPosition = currentPreview.transform.position;
            }
            if (Input.GetKeyUp(rotationKey) || Input.GetMouseButtonDown(1))
            {
                isRotationLock = false;
            }
        }

        // HydroDam uses its own rotation logic
        if (isHydroDamPlacement)
        {
            HydroDam dam = currentPreview.GetComponent<HydroDam>();
            if (dam != null)
                dam.SnapToNearestSpot();
        }
        else
        {
            UpdatePreviewPosition();

            // Highlight connectable objects within range of the connection point
            ConnectableObject previewCO = currentPreview.GetComponent<ConnectableObject>();
            if (previewCO != null && meshParent != null)
            {
                Transform connectionPoint = GetConnectionPoint(meshParent);
                if (connectionPoint != null)
                {
                    SelectionHighlighter.Instance.HighlightInRange(connectionPoint.position, previewCO.GetConnectionRange(), currentPreview);
                }
            }

            UpdateFootprintIndicator();
        }

        UpdateGhostColor();
        HandleInput();

        // Cache UIManager instance locally for multiple uses
        var uiMgr = UIManager.Instance;
        if (uiMgr != null)
            uiMgr.UpdateUI(currentPreview, isHydroDamPlacement);
    }

    #endregion

    #region Public Methods

    public void StartPlacing(GameObject prefab)
    {
        if (isPlacing) return;

        buildingPrefab = prefab;
        isPlacing = true;
        currentPreview = Instantiate(buildingPrefab);

        SetupCurrentPreview(currentPreview);
        originalColliderBounds = GetCompoundColliderBounds(currentPreview);

        // Disable platform child if it exists
        Transform platformChild = currentPreview.transform.Find("Platform");
        if (platformChild != null)
            platformChild.gameObject.SetActive(false);

        // Check if this building is a HydroDam
        if (currentPreview.GetComponent<HydroDam>() != null)
        {
            isHydroDamPlacement = true;
            currentPreview.GetComponent<HydroDam>().BeginPlacement();
        }
        else
        {
            isHydroDamPlacement = false;
            CreateFootprintIndicator();
        }

        if (!isHydroDamPlacement && meshParent != null)
            lastMeshWorldPos = meshParent.position;

        // Enable range indicator if available
        RangeIndicator indicator = currentPreview.GetComponentInChildren<RangeIndicator>(true);
        if (indicator != null)
        {
            ConnectableObject co = currentPreview.GetComponent<ConnectableObject>();
            if (co != null)
                indicator.transform.localScale = Vector3.one * co.GetConnectionRange();
            indicator.EnableIndicator(true);
        }

        // Hide build panel
        var uiMgr = UIManager.Instance;
        if (uiMgr != null && uiMgr.buildPanel != null)
            uiMgr.buildPanel.HideTemporarily();
    }

    #endregion

    #region Setup Helpers

    // Sets up common preview properties (meshParent, baseRotation, original colors)
    private void SetupCurrentPreview(GameObject preview)
    {
        // Find the mesh child (tagged "Mesh")
        meshParent = null;
        foreach (Transform child in preview.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("Mesh"))
            {
                meshParent = child;
                break;
            }
        }
        if (meshParent == null)
            Debug.LogError("No child with tag 'Mesh' found in building prefab!");

        baseRotation = preview.transform.rotation;
        preview.transform.position = Vector3.zero;

        // Back up original material colors
        originalMaterialColors = new Dictionary<Renderer, Color[]>();
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Color[] colors = new Color[rend.materials.Length];
            for (int i = 0; i < rend.materials.Length; i++)
                colors[i] = rend.materials[i].color;
            originalMaterialColors[rend] = colors;
        }
    }

    // Restores the original material colors for the current preview
    private void RestoreOriginalMaterialColors()
    {
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (originalMaterialColors.ContainsKey(rend))
            {
                Color[] originalColors = originalMaterialColors[rend];
                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length && i < originalColors.Length; i++)
                    mats[i].color = originalColors[i];
            }
        }
    }

    // Returns the first child with the "ConnectionPoint" tag under the given parent.
    private Transform GetConnectionPoint(Transform parent)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("ConnectionPoint"))
                return child;
        }
        return null;
    }

    #endregion

    #region Footprint Indicator (Normal Buildings)

    private void CreateFootprintIndicator()
    {
        if (currentPreview == null) return;

        footprintIndicator = new GameObject("FootprintIndicator");
        footprintIndicator.transform.parent = transform;
        footprintIndicator.transform.localPosition = Vector3.zero;
        footprintIndicator.transform.localRotation = Quaternion.identity;

        footprintLR = footprintIndicator.AddComponent<LineRenderer>();
        footprintLR.loop = true;
        footprintLR.startWidth = footprintLineWidth;
        footprintLR.endWidth = footprintLineWidth;
        footprintLR.useWorldSpace = true;
        footprintLR.material = new Material(Shader.Find("Sprites/Default"));

        Bounds unionBounds = originalColliderBounds;
        Vector3 extents = unionBounds.extents;
        initialLocalOffsets = new Vector3[4];
        initialLocalOffsets[0] = new Vector3(-extents.x, 0, -extents.z);
        initialLocalOffsets[1] = new Vector3(extents.x, 0, -extents.z);
        initialLocalOffsets[2] = new Vector3(extents.x, 0, extents.z);
        initialLocalOffsets[3] = new Vector3(-extents.x, 0, extents.z);
    }

    private void UpdateFootprintIndicator()
    {
        if (currentPreview == null || footprintLR == null || initialLocalOffsets == null)
            return;

        Vector3[] worldCorners = new Vector3[5];
        Quaternion rotation = baseRotation;

        for (int i = 0; i < initialLocalOffsets.Length; i++)
        {
            Vector3 rotatedOffset = rotation * initialLocalOffsets[i];
            Vector3 worldPoint = basePosition + rotatedOffset;
            Vector3 rayOrigin = worldPoint + Vector3.up * footprintRaycastHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, footprintRaycastHeight * 2, terrainLayer))
                worldPoint.y = hit.point.y + footprintYOffset;
            else
                worldPoint.y = basePosition.y + footprintYOffset;

            worldCorners[i] = worldPoint;
        }
        worldCorners[4] = worldCorners[0];
        footprintLR.positionCount = 5;
        footprintLR.SetPositions(worldCorners);
    }

    #endregion

    #region Preview Position & Ghost

    private void UpdatePreviewPosition()
    {
        if (!isRotationLock)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayer))
            {
                basePosition = hit.point;
                float groundY = GetHighestGroundY() + finalPlacementYOffset;
                basePosition.y = groundY;
                currentPreview.transform.position = basePosition;

                // Hover effect
                float hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
                float totalYOffset = ghostBaseYOffset + hoverOffset;

                if (meshParent != null)
                {
                    meshParent.localPosition = new Vector3(0, totalYOffset, 0);
                    Vector3 meshWorldPos = currentPreview.transform.position + meshParent.localPosition;
                    Vector3 velocity = meshWorldPos - lastMeshWorldPos;
                    float desiredTiltX = Mathf.Clamp(-velocity.z * tiltSensitivity, -maxTiltAngle, maxTiltAngle);
                    float desiredTiltZ = Mathf.Clamp(velocity.x * tiltSensitivity, -maxTiltAngle, maxTiltAngle);
                    currentTiltX = Mathf.Lerp(currentTiltX, desiredTiltX, Time.deltaTime / tiltSmoothTime);
                    currentTiltZ = Mathf.Lerp(currentTiltZ, desiredTiltZ, Time.deltaTime / tiltSmoothTime);
                    meshParent.localRotation = Quaternion.Euler(currentTiltX, 0, currentTiltZ);
                    lastMeshWorldPos = meshWorldPos;
                }
            }
        }
        else
        {
            currentPreview.transform.position = lockedPosition;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit2, Mathf.Infinity, terrainLayer))
            {
                Vector3 targetPoint = hit2.point;
                Vector3 direction = targetPoint - lockedPosition;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    if (isRotationSnapEnabled)
                    {
                        float yAngle = lookRotation.eulerAngles.y;
                        float snappedY = Mathf.Round(yAngle / 45f) * 45f;
                        lookRotation = Quaternion.Euler(0, snappedY, 0);
                    }
                    currentPreview.transform.rotation = lookRotation;
                    baseRotation = lookRotation;
                    if (debugRotationTracking)
                    {
                        Debug.DrawLine(lockedPosition, targetPoint, Color.magenta, 0.1f);
                        Debug.DrawRay(lockedPosition, currentPreview.transform.right * 3f, Color.green, 0.1f);
                    }
                }
            }
        }
    }

    private void UpdateGhostColor()
    {
        Color targetColor = IsValidPlacement() ? validColor : invalidColor;
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
                mat.color = targetColor;
        }
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsValidPlacement())
                StartCoroutine(ConfirmPlacementRoutine());
            else
                Debug.Log("Invalid placement location!");
        }
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelPlacement();
    }

    #endregion

    #region Validation Helpers

    private bool IsValidPlacement()
    {
        if (isHydroDamPlacement)
        {
            HydroDam hd = currentPreview.GetComponent<HydroDam>();
            return hd != null && hd.IsSnappedToValidSpot();
        }

        Bounds unionBounds = originalColliderBounds;
        Vector3 extents = unionBounds.extents;
        Quaternion rot = Quaternion.Euler(0, baseRotation.eulerAngles.y, 0);
        int samplesPerEdge = 4;
        ConnectableObject co = currentPreview.GetComponent<ConnectableObject>();
        if (co != null)
            samplesPerEdge = co.placementSubdivision;

        float minX = -extents.x, maxX = extents.x, minZ = -extents.z, maxZ = extents.z;
        int steps = Mathf.Max(1, samplesPerEdge - 1);

        for (int ix = 0; ix < samplesPerEdge; ix++)
        {
            for (int iz = 0; iz < samplesPerEdge; iz++)
            {
                float tX = (samplesPerEdge == 1) ? 0.5f : ix / (float)steps;
                float tZ = (samplesPerEdge == 1) ? 0.5f : iz / (float)steps;
                float sampleX = Mathf.Lerp(minX, maxX, tX);
                float sampleZ = Mathf.Lerp(minZ, maxZ, tZ);
                Vector3 localOffset = new Vector3(sampleX, 0f, sampleZ);
                Vector3 worldOffset = rot * localOffset;
                Vector3 samplePos = basePosition + worldOffset;
                if (!CheckSlopeAndObstructions(samplePos))
                    return false;
            }
        }

        WindTurbine turbine = currentPreview.GetComponent<WindTurbine>();
        if (turbine != null && !turbine.IsPlacementValid())
            return false;

        return true;
    }

    private bool CheckSlopeAndObstructions(Vector3 samplePos)
    {
        int combinedMask = terrainLayer | obstructionLayer;
        Vector3 rayStart = samplePos + Vector3.up * raycastHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, combinedMask))
        {
            if (((1 << hit.collider.gameObject.layer) & obstructionLayer) != 0 &&
                !hit.collider.transform.IsChildOf(currentPreview.transform))
            {
                if (debugPlacementRaycasts)
                {
                    Debug.DrawLine(rayStart, hit.point, Color.red, 0.2f);
                    Debug.LogWarning($"[Placement] Sample at {samplePos} hit an obstruction.");
                }
                return false;
            }
            bool hitIsTerrain = ((1 << hit.collider.gameObject.layer) & terrainLayer) != 0;
            if (hitIsTerrain)
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                ConnectableObject co = currentPreview.GetComponent<ConnectableObject>();
                float maxAngle = (co != null) ? co.placementMaxSlopeAngle : 30f;
                if (debugPlacementRaycasts)
                {
                    Debug.DrawLine(rayStart, hit.point, Color.cyan, 0.2f);
                    Debug.Log($"[Placement] Sample at {samplePos} => slope = {angle:F2}");
                }
                return angle <= maxAngle;
            }
            if (!hit.collider.transform.IsChildOf(currentPreview.transform))
                return false;
        }
        else
        {
            if (debugPlacementRaycasts)
            {
                Debug.DrawRay(rayStart, Vector3.down * raycastHeight * 2, Color.red, 0.2f);
                Debug.LogWarning($"[Placement] No terrain/obstruction hit at {samplePos}");
            }
            return false;
        }
        return true;
    }

    #endregion

    #region Bounds & Ground Helpers

    private Bounds GetCompoundColliderBounds(GameObject building)
    {
        Collider[] colliders = building.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            return new Bounds(building.transform.position, Vector3.zero);
        Bounds unionBounds = colliders[0].bounds;
        foreach (Collider col in colliders)
            unionBounds.Encapsulate(col.bounds);
        return unionBounds;
    }

    private float GetHighestGroundY()
    {
        float highestY = float.MinValue;
        Bounds unionBounds = originalColliderBounds;
        Vector3 extents = unionBounds.extents;
        Quaternion rot = Quaternion.Euler(0, baseRotation.eulerAngles.y, 0);
        int samplesPerEdge = 4;
        ConnectableObject co = currentPreview.GetComponent<ConnectableObject>();
        if (co != null)
            samplesPerEdge = co.placementSubdivision;
        float minX = -extents.x, maxX = extents.x, minZ = -extents.z, maxZ = extents.z;
        int steps = Mathf.Max(1, samplesPerEdge - 1);
        for (int ix = 0; ix < samplesPerEdge; ix++)
        {
            for (int iz = 0; iz < samplesPerEdge; iz++)
            {
                float tX = (samplesPerEdge == 1) ? 0.5f : ix / (float)steps;
                float tZ = (samplesPerEdge == 1) ? 0.5f : iz / (float)steps;
                float sampleX = Mathf.Lerp(minX, maxX, tX);
                float sampleZ = Mathf.Lerp(minZ, maxZ, tZ);
                Vector3 localOffset = new Vector3(sampleX, 0f, sampleZ);
                Vector3 worldOffset = rot * localOffset;
                Vector3 samplePos = basePosition + worldOffset;
                Vector3 rayStart = samplePos + Vector3.up * raycastHeight;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, terrainLayer))
                {
                    if (hit.point.y > highestY)
                        highestY = hit.point.y;
                }
            }
        }
        if (highestY == float.MinValue)
            highestY = basePosition.y;
        return highestY;
    }

    #endregion

    #region Confirm & Cancel Placement

    private IEnumerator ConfirmPlacementRoutine()
    {
        SelectionHighlighter.Instance.ClearHighlights();

        // HydroDam building placement
        if (isHydroDamPlacement)
        {
            HydroDam hydroDam = currentPreview.GetComponent<HydroDam>();
            if (hydroDam != null)
            {
                if (!hydroDam.IsSnappedToValidSpot())
                {
                    Debug.Log("HydroDam is not snapped to a valid spot. Cannot build.");
                    yield break;
                }
                ConnectableObject co = currentPreview.GetComponent<ConnectableObject>();
                int cost = co != null ? co.GetPlacementCost() : 0;
                if (!CreditsManager.Instance.TrySpend(cost))
                {
                    Debug.Log("Cannot place building: insufficient credits.");
                    CancelPlacement();
                    yield break;
                }
                hydroDam.FinalizePlacement();
                isPlacing = false;
                currentPreview = null;
                var uiMgr = UIManager.Instance;
                if (uiMgr != null && uiMgr.buildPanel != null)
                    uiMgr.buildPanel.ShowIfStillOpen();
                yield break;
            }
        }
        else
        {
            // Normal building placement
            ConnectableObject co = currentPreview.GetComponent<ConnectableObject>();
            int cost = co != null ? co.GetPlacementCost() : 0;
            if (!CreditsManager.Instance.TrySpend(cost))
            {
                Debug.Log("Cannot place building: insufficient credits.");
                CancelPlacement();
                yield break;
            }
            isPlacing = false;
            Vector3 confirmedBasePos = basePosition;
            float finalY = GetHighestGroundY() + finalPlacementYOffset;
            Vector3 endPos = new Vector3(confirmedBasePos.x, finalY, confirmedBasePos.z);
            Vector3 startPos = currentPreview.transform.position;
            float initialMeshOffsetY = meshParent ? meshParent.localPosition.y : 0f;
            float elapsed = 0f;
            while (elapsed < slamDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slamDuration;
                currentPreview.transform.position = Vector3.Lerp(startPos, endPos, t);
                if (meshParent != null)
                {
                    float newY = Mathf.Lerp(initialMeshOffsetY, 0f, t);
                    meshParent.localPosition = new Vector3(meshParent.localPosition.x, newY, meshParent.localPosition.z);
                }
                yield return null;
            }
            currentPreview.transform.position = endPos;
            currentPreview.transform.rotation = baseRotation;
        }

        RestoreOriginalMaterialColors();

        if (meshParent != null)
        {
            meshParent.localPosition = Vector3.zero;
            meshParent.localRotation = Quaternion.identity;
        }

        RangeIndicator indicator = currentPreview.GetComponentInChildren<RangeIndicator>(true);
        if (indicator != null)
            indicator.DisableIndicator();

        float bottomY = GetCompoundColliderBounds(currentPreview).min.y;
        Vector3 vfxPos = new Vector3(basePosition.x, bottomY + vfxYOffset, basePosition.z);
        if (dustVFXPrefab != null)
            Instantiate(dustVFXPrefab, vfxPos, Quaternion.identity);
        if (footprintIndicator != null)
            Destroy(footprintIndicator);

        if (pollutionDecalPrefab != null && currentPreview != null)
        {
            ConnectableObject coDecal = currentPreview.GetComponent<ConnectableObject>();
            if (coDecal != null)
            {
                float pollution = coDecal.GetEcoCost();
                if (pollution > 0f)
                {
                    GameObject decal = Instantiate(pollutionDecalPrefab, currentPreview.transform);
                    DecalExpander expander = decal.GetComponent<DecalExpander>();
                    if (expander != null)
                        expander.SetPollutionValue(pollution);
                }
            }
        }

        isRotationLock = false;
        WindTurbine turbine = currentPreview.GetComponent<WindTurbine>();
        if (turbine != null)
            turbine.OnBuildingPlaced();
        BioBurner burner = currentPreview.GetComponent<BioBurner>();
        if (burner != null)
            burner.OnBuildingPlaced();
        SolarPanelStatic solar = currentPreview.GetComponent<SolarPanelStatic>();
        if (solar != null)
            solar.OnBuildingPlaced();


        if (currentPreview.GetComponent<ConnectableObject>() != null && UIManager.Instance != null)
            UIManager.Instance.UpdateFinalUI(currentPreview.GetComponent<ConnectableObject>().powerProduction);

        EcoBalanceManager.Instance.RemoveVegetationUnderBuilding(currentPreview);
        StartCoroutine(DelayedPollution(currentPreview, 0.8f));

        if (currentPreview.GetComponent<ConnectableObject>() != null)
        {
            Transform platformChild = currentPreview.transform.Find("Platform");
            if (platformChild != null)
                platformChild.gameObject.SetActive(currentPreview.GetComponent<ConnectableObject>().placePlatform);
        }

        bool copyMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Quaternion lastPlacedRotation = baseRotation;

        if (!copyMode)
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr != null)
            {
                uiMgr.ResetUI();
                if (uiMgr.buildPanel != null)
                    uiMgr.buildPanel.ShowIfStillOpen();
            }
            currentPreview = null;
        }
        else
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr != null && uiMgr.buildPanel != null)
                uiMgr.buildPanel.HideTemporarily();
            currentPreview = Instantiate(buildingPrefab);
            SetupCurrentPreview(currentPreview);
            originalColliderBounds = GetCompoundColliderBounds(currentPreview);
            if (currentPreview.GetComponent<HydroDam>() != null)
            {
                isHydroDamPlacement = true;
                currentPreview.GetComponent<HydroDam>().BeginPlacement();
            }
            else
            {
                isHydroDamPlacement = false;
                CreateFootprintIndicator();
            }
            baseRotation = lastPlacedRotation;
            currentPreview.transform.rotation = baseRotation;
            meshParent = null;
            foreach (Transform child in currentPreview.GetComponentsInChildren<Transform>())
            {
                if (child.CompareTag("Mesh"))
                {
                    meshParent = child;
                    break;
                }
            }
            if (meshParent != null)
                lastMeshWorldPos = currentPreview.transform.position + meshParent.localPosition;
            RangeIndicator indicatorNew = currentPreview.GetComponentInChildren<RangeIndicator>(true);
            ConnectableObject coNew = currentPreview.GetComponent<ConnectableObject>();
            if (indicatorNew != null && coNew != null)
            {
                indicatorNew.transform.localScale = Vector3.one * coNew.GetConnectionRange();
                indicatorNew.EnableIndicator(true);
            }
            isPlacing = true;
        }
    }

    private void CancelPlacement()
    {
        Debug.Log("[BuildingPlacer] CancelPlacement() called.");
        isPlacing = false;
        SelectionHighlighter.Instance.ClearHighlights();

        if (isHydroDamPlacement && currentPreview != null)
        {
            HydroDam hd = currentPreview.GetComponent<HydroDam>();
            if (hd != null)
                hd.CancelPlacement();
        }
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
        if (footprintIndicator != null)
            Destroy(footprintIndicator);
        if (UIManager.Instance != null)
            UIManager.Instance.ResetUI();
        if (UIManager.Instance != null && UIManager.Instance.buildPanel != null)
            UIManager.Instance.buildPanel.ShowIfStillOpen();
    }

    #endregion

    #region Coroutines

    private IEnumerator DelayedPollution(GameObject building, float delay)
    {
        yield return new WaitForSeconds(delay);
        ConnectableObject co = building.GetComponent<ConnectableObject>();
        if (co == null) yield break;
        float pollution = co.GetEcoCost();
        EcoBalanceManager.Instance.ApplyPollutionEffectGradually(building, pollution, 4f);
        float ecoBefore = EcoBalanceManager.Instance.ecoBalance;
        EcoBalanceManager.Instance.ecoBalance -= pollution;
        float ecoAfter = EcoBalanceManager.Instance.ecoBalance;
        var uiMgr = UIManager.Instance;
        if (uiMgr != null)
        {
            int oldValue = Mathf.RoundToInt(ecoBefore);
            int newValue = Mathf.RoundToInt(ecoAfter);
            if (oldValue != newValue)
            {
                uiMgr.TriggerEcoBalanceHitEffect(oldValue, newValue);
                uiMgr.ShowEcoLossPopup(newValue - oldValue);
            }
            uiMgr.UpdateEcoBalanceUI();
        }
    }

    #endregion
}
