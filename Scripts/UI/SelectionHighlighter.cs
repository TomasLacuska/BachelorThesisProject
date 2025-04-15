#region SelectionHighlighter Class
/*
 * =========================================================
 * SelectionHighlighter
 *
 * Provides functionality to highlight connectable objects in the scene
 * by enabling an Outline component on their "Mesh" child. Highlights are
 * applied based on proximity and can be cleared on demand.
 * =========================================================
 */
#endregion

using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlighter : MonoBehaviour
{
    public static SelectionHighlighter Instance { get; private set; }

    [Header("Highlight Settings")]
    [Tooltip("Default range within which objects are highlighted.")]
    public float DefaultRange = 50f;
    [Tooltip("Layer mask used to filter buildings.")]
    public LayerMask BuildingLayer;
    [Tooltip("Color of the outline applied to highlighted objects.")]
    public Color OutlineColor = Color.cyan;
    [Tooltip("Width of the outline effect.")]
    public float OutlineWidth = 10f;

    // List of active Outline components that have been enabled.
    private readonly List<Outline> activeOutlines = new();
    // Set of GameObjects that have had Outline enabled.
    private readonly HashSet<GameObject> outlinedObjects = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    // Highlights every building with a ConnectableObject in the given range.
    // center: center of the area; range: radius; exclude: optional GameObject to ignore.
    public void HighlightInRange(Vector3 center, float range, GameObject exclude = null)
    {
        ClearHighlights();

        List<ConnectableObject> allConnectables = ConnectableManager.Instance.GetAll();

        foreach (ConnectableObject connectable in allConnectables)
        {
            GameObject target = connectable.gameObject;
            if (exclude != null && target == exclude) continue;
            if (outlinedObjects.Contains(target)) continue;

            Transform meshRoot = FindMeshRoot(target.transform);
            if (meshRoot == null) continue;

            // Look for a child with tag "ConnectionPoint" within the mesh hierarchy.
            Transform connectionPoint = null;
            foreach (Transform child in meshRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("ConnectionPoint"))
                {
                    connectionPoint = child;
                    break;
                }
            }
            if (connectionPoint == null) continue;

            // Compute flat distance on the XZ plane.
            Vector2 centerXZ = new Vector2(center.x, center.z);
            Vector2 targetXZ = new Vector2(connectionPoint.position.x, connectionPoint.position.z);
            float flatDistance = Vector2.Distance(centerXZ, targetXZ);
            if (flatDistance > range) continue;

            // Apply outline to all renderers in the mesh hierarchy.
            Renderer[] renderers = meshRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                Outline outline = renderer.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = renderer.gameObject.AddComponent<Outline>();
                    outline.OutlineMode = Outline.Mode.OutlineAll;
                    outline.OutlineColor = OutlineColor;
                    outline.OutlineWidth = OutlineWidth;
                    outline.InitializeIfNeeded();
                }
                else
                {
                    outline.InitializeIfNeeded();
                }
                outline.enabled = true;
                if (!activeOutlines.Contains(outline))
                    activeOutlines.Add(outline);
                if (!outlinedObjects.Contains(target))
                    outlinedObjects.Add(target);
            }
        }
    }

    // Disables all outlines previously enabled by HighlightInRange.
    public void ClearHighlights()
    {
        foreach (var outline in activeOutlines)
        {
            if (outline != null)
                outline.enabled = false;
        }
        activeOutlines.Clear();
        outlinedObjects.Clear();
    }

    // Recursively finds a child with the tag "Mesh" in the given transform.
    // Returns the transform if found; otherwise, returns the base transform if it has a Renderer.
    private Transform FindMeshRoot(Transform baseTransform)
    {
        foreach (Transform child in baseTransform.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("Mesh"))
                return child;
        }
        if (baseTransform.GetComponent<Renderer>() != null)
            return baseTransform;
        return null;
    }
}
