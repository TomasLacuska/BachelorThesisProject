#region RangeIndicator Class
/*
 * =========================================================
 * RangeIndicator
 *
 * Dynamically adjusts a mesh to serve as a visual range indicator,
 * aligning its vertices to the terrain using raycasts.
 * =========================================================
 */
#endregion

using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class RangeIndicator : MonoBehaviour
{
    #region Public Fields

    [Header("Raycast Settings")]
    [Tooltip("Layer mask for terrain raycasting.")]
    public LayerMask terrainLayer;
    [Tooltip("Height above vertices from which to start raycasts.")]
    public float raycastHeight = 50f;
    [Tooltip("Vertical offset added to hit points to avoid z-fighting.")]
    public float groundOffset = 0.05f;
    
    [Header("Indicator Mode")]
    [Tooltip("If true, the indicator updates dynamically.")]
    public bool dynamicIndicator = false;

    #endregion

    #region Private Fields

    private MeshFilter meshFilter;
    private Mesh indicatorMesh;
    private Vector3[] originalVertices;
    private bool indicatorActive = false;

    #endregion

    #region Unity Methods

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("RangeIndicator requires a MeshFilter component.");
            enabled = false;
            return;
        }
        indicatorMesh = meshFilter.mesh;
        originalVertices = indicatorMesh.vertices;
    }

    void OnEnable()
    {
        if (indicatorActive)
            UpdateIndicator();
    }

    void Update()
    {
        if (indicatorActive && dynamicIndicator)
            UpdateIndicator();
    }

    #endregion

    #region Public Methods

    // Enables the indicator with an option for dynamic updates.
    public void EnableIndicator(bool dynamicMode)
    {
        dynamicIndicator = dynamicMode;
        indicatorActive = true;
        gameObject.SetActive(true);
        UpdateIndicator();
    }

    // Disables the indicator and resets the mesh.
    public void DisableIndicator()
    {
        indicatorActive = false;
        ResetMesh();
        gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods

    // Resets the mesh vertices to their original positions.
    private void ResetMesh()
    {
        indicatorMesh.vertices = originalVertices;
        indicatorMesh.RecalculateNormals();
        indicatorMesh.RecalculateBounds();
    }

    // Updates the mesh vertices by raycasting to the terrain.
    void UpdateIndicator()
    {
        Vector3[] vertices = new Vector3[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(originalVertices[i]);
            Vector3 rayOrigin = worldPos + Vector3.up * raycastHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2, terrainLayer))
            {
                Vector3 adjustedWorldPos = hit.point + Vector3.up * groundOffset;
                vertices[i] = transform.InverseTransformPoint(adjustedWorldPos);
            }
            else
            {
                vertices[i] = originalVertices[i];
            }
        }
        indicatorMesh.vertices = vertices;
        indicatorMesh.RecalculateNormals();
        indicatorMesh.RecalculateBounds();
    }

    #endregion
}
