#region ConnectionCable Class
/*
 * =========================================================
 * ConnectionCable
 *
 * Draws a curved cable between two connectable objects.
 * Handles preview visuals, collision detection, and deletion.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ConnectionCable : MonoBehaviour
{
    #region Public Settings

    [Header("Cable Curve Settings")]
    [Tooltip("Number of segments to define the cable curve.")]
    public int curveSegmentCount = 20;
    [Tooltip("Height applied at the curve's midpoint.")]
    public float curveHeight = 2f;
    [Tooltip("Width of the cable line.")]
    public float cableWidth = 0.1f;

    [Header("Materials")]
    [Tooltip("Default material for the cable.")]
    public Material cableMaterial;
    [Tooltip("Material used for preview when placement is valid.")]
    public Material previewValidMaterial;
    [Tooltip("Material used for preview when placement is invalid.")]
    public Material previewInvalidMaterial;
    [Tooltip("Material used when hovering in delete mode.")]
    public Material deleteHoverMaterial;

    [Header("Layer & Tag Filtering")]
    [Tooltip("Layer mask to detect obstacles along the cable.")]
    public LayerMask obstacleLayers;
    [Tooltip("Tag to identify valid connection points.")]
    public string connectionTag = "ConnectionPoint";

    [Header("Cable State")]
    [Tooltip("True if the cable is in preview mode.")]
    public bool isPreview = false;

    [Header("Collider Padding")]
    [Tooltip("Additional padding for the cable's collider bounds.")]
    public Vector3 colliderPadding = new Vector3(0.5f, 0.5f, 0.5f);

    #endregion

    #region Private Fields

    private LineRenderer lr;
    private BoxCollider boxCollider;
    private Rigidbody rb;
    private ConnectableObject buildingA, buildingB;
    private Vector3[] curvePoints;
    private HashSet<Collider> colliding = new HashSet<Collider>();
    private bool isHovered = false;

    #endregion

    #region Setup and Update

    void Awake()
    {
        SetupLineRenderer();
        SetupPhysics();
    }

    void Update()
    {
        // Update cable curve if not in preview mode and both endpoints are set.
        if (!isPreview && buildingA && buildingB)
            UpdateCable(buildingA.connectionPoint.position, buildingB.connectionPoint.position);
    }

    #endregion

    #region Setup Methods

    // Initializes and configures the LineRenderer.
    void SetupLineRenderer()
    {
        lr = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        lr.positionCount = curveSegmentCount;
        lr.widthMultiplier = cableWidth;
        lr.material = cableMaterial;

        curvePoints = new Vector3[curveSegmentCount];
    }

    // Sets up physics components (BoxCollider and Rigidbody).
    void SetupPhysics()
    {
        boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    #endregion

    #region Cable Update Logic

    // Initializes the cable connection between two buildings.
    // a: first connectable object; b: second connectable object.
    public void Initialize(ConnectableObject a, ConnectableObject b)
    {
        buildingA = a;
        buildingB = b;
        isPreview = false;

        lr.material = cableMaterial; // Reset to default material upon placement.
        UpdateCable(a.connectionPoint.position, b.connectionPoint.position);
    }

    // Updates the preview cable curve between given start and end points.
    // start: starting position; end: ending position.
    public void UpdatePreviewCurve(Vector3 start, Vector3 end)
    {
        UpdateCable(start, end);

        // Set preview material based on collision state.
        bool collides = IsColliding();
        if (previewValidMaterial != null && previewInvalidMaterial != null)
            lr.material = collides ? previewInvalidMaterial : previewValidMaterial;

        // Highlight valid targets.
        HighlightValidTargets();
    }

    // Highlights potential connection targets around buildingA.
    void HighlightValidTargets()
    {
        if (buildingA == null) return;
        if (SelectionHighlighter.Instance != null)
        {
            SelectionHighlighter.Instance.HighlightInRange(buildingA.transform.position, buildingA.GetConnectionRange());
        }
    }

    // Updates the cable curve using a quadratic Bézier curve.
    void UpdateCable(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) * 0.5f + Vector3.up * curveHeight;
        for (int i = 0; i < curveSegmentCount; i++)
        {
            float t = i / (float)(curveSegmentCount - 1);
            curvePoints[i] = QuadraticBezier(start, mid, end, t);
            lr.SetPosition(i, curvePoints[i]);
        }
        UpdateCollider(start, end);
    }

    // Calculates a point on a quadratic Bézier curve.
    Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    #endregion

    #region Collider Setup

    // Updates the BoxCollider to enclose the cable curve.
    void UpdateCollider(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) * 0.5f;
        transform.position = mid;
        transform.rotation = Quaternion.LookRotation((end - start).normalized);

        Vector3[] local = new Vector3[curveSegmentCount];
        for (int i = 0; i < curveSegmentCount; i++)
            local[i] = transform.InverseTransformPoint(curvePoints[i]);

        Bounds b = new Bounds(local[0], Vector3.zero);
        for (int i = 1; i < curveSegmentCount; i++)
            b.Encapsulate(local[i]);

        boxCollider.center = b.center;
        boxCollider.size = b.size + colliderPadding;
    }

    #endregion

    #region Collision Logic

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != gameObject)
            colliding.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        colliding.Remove(other);
    }

    // Checks whether the cable is colliding with any obstacles.
    public bool IsColliding()
    {
        foreach (var obj in colliding)
        {
            if (obj == null) continue;
            bool isObstacle = (obstacleLayers.value & (1 << obj.gameObject.layer)) > 0;
            bool isNotConnection = !obj.CompareTag(connectionTag);
            if (isObstacle && isNotConnection)
                return true;
        }
        return false;
    }

    #endregion

    #region Connection Management

    // Deletes the connection by removing mutual connections and triggering a network update.
    public void DeleteConnection()
    {
        buildingA?.RemoveConnection(buildingB);
        buildingB?.RemoveConnection(buildingA);

        Destroy(gameObject);
        NetworkEvents.TriggerNetworkChanged();
    }

    #endregion

    #region Delete Mode Visuals

    // Sets the hovered state for delete mode visuals.
    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;

        isHovered = hovered;
        if (!isPreview && hovered && deleteHoverMaterial != null)
            lr.material = deleteHoverMaterial;
        else
            lr.material = cableMaterial;
    }

    #endregion
}
