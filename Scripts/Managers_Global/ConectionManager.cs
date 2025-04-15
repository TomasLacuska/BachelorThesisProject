#region ConnectionManager Class
/*
 * =========================================================
 * ConnectionManager
 *
 * Manages cable connections between buildings.
 * Handles both building cables and deletion of cables,
 * provides UI feedback (button colors and cursor changes),
 * and updates cable previews.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.UI;
using System;

public class ConnectionManager : MonoBehaviour
{
    #region Singleton

    public static ConnectionManager Instance;

    private void Awake()
    {
        // Simple singleton pattern.
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    #endregion

    #region Public Fields

    [Header("Current Cable Building Mode")]
    [Tooltip("0 = none, 1 = cable build, 2 = cable delete")]
    public int currentMode = 0; // 0 = none, 1 = cable build, 2 = cable delete

    [Header("Cable")]
    public GameObject cablePrefab;
    public LayerMask buildingSelectionMask;

    [Header("Cable Snap Detection")]
    public LayerMask snapDetectionMask;

    [Header("Cable UI Buttons")]
    public Image cableBuildButtonImage;
    public Image cableDeleteButtonImage;

    [Header("Cable Button Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Cursor Textures")]
    public Texture2D cursorCable;
    public Texture2D cursorCutter;
    public Vector2 cursorHotspot = Vector2.zero;

    #endregion

    #region Private Fields

    private ConnectableObject selected;
    private ConnectionCable tempCable;
    private ConnectionCable lastHoveredCable = null;

    // Cache the SelectionHighlighter instance.
    private SelectionHighlighter highlighter => SelectionHighlighter.Instance;

    #endregion

    #region Unity Methods

    private void Update()
    {
        // Check for mode toggle input.
        if (Input.GetKeyDown(KeyCode.C))
            SetMode(1);
        else if (Input.GetKeyDown(KeyCode.X))
            SetMode(2);

        // Process behavior based on current mode.
        if (currentMode == 1)
            BuildMode();
        else if (currentMode == 2)
            DeleteMode();

        // Cancel current mode on ESC or right-click.
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            SetMode(0);
            highlighter.ClearHighlights();
        }
    }

    #endregion

    #region Mode Management

     
    // Sets the current mode. Toggling the same mode turns it off.
    
    public void SetMode(int mode)
    {
        currentMode = (currentMode == mode) ? 0 : mode;
        UpdateCableButtonVisuals();
        UpdateCursor();
    }

     
    // Updates the button visuals based on the current mode.
    
    private void UpdateCableButtonVisuals()
    {
        if (cableBuildButtonImage != null)
            cableBuildButtonImage.color = (currentMode == 1) ? selectedColor : normalColor;
        if (cableDeleteButtonImage != null)
            cableDeleteButtonImage.color = (currentMode == 2) ? selectedColor : normalColor;
    }

     
    // Updates the cursor texture based on the current mode.
    
    private void UpdateCursor()
    {
        if (currentMode == 1 && cursorCable != null)
            Cursor.SetCursor(cursorCable, cursorHotspot, CursorMode.Auto);
        else if (currentMode == 2 && cursorCutter != null)
            Cursor.SetCursor(cursorCutter, cursorHotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    #endregion

    #region Build Mode

    
    // Processes input and updates the cable preview while in build mode.
    private void BuildMode()
    {
        Camera cam = Camera.main;

        // Handle left-click: selection and cable creation.
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, buildingSelectionMask))
            {
                ConnectableObject building = hit.collider.GetComponentInParent<ConnectableObject>();
                if (building != null)
                {
                    if (selected == null)
                    {
                        // First selection: choose the building and start cable preview.
                        selected = building;
                        selected.ShowConnectionRangeIndicator(true);

                        GameObject previewObj = Instantiate(cablePrefab);
                        tempCable = previewObj.GetComponent<ConnectionCable>();
                        tempCable.isPreview = true;
                    }
                    else if (building != selected)
                    {
                        // Second selection: attempt to connect the two buildings.
                        float dist = Vector3.Distance(selected.connectionPoint.position, building.connectionPoint.position);
                        float range = selected.GetConnectionRange();
                        if (dist <= range)
                        {
                            if (!selected.IsAlreadyConnectedTo(building) && !tempCable.IsColliding())
                                CreateCable(selected, building);
                            else
                                Debug.LogWarning("Cannot connect: Path is blocked or already connected.");
                        }
                        else
                        {
                            Debug.LogWarning("Target is out of range.");
                        }
                        Deselect();
                    }
                }
            }
        }

        // On right-click: cancel current selection.
        if (Input.GetMouseButtonDown(1))
        {
            if (selected != null || tempCable != null)
                Deselect();
            else
                SetMode(0);
        }

        // Update cable preview curve if a building is selected.
        if (selected != null && tempCable != null && tempCable.isPreview)
        {
            Vector3 origin = selected.connectionPoint.position;
            Vector3 target = GetMouseWorldPos();

            // Check for snapping to a nearby building.
            Ray snapRay = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(snapRay, out RaycastHit snapHit, Mathf.Infinity, snapDetectionMask))
            {
                ConnectableObject snapBld = snapHit.collider.GetComponentInParent<ConnectableObject>();
                if (snapBld != null && snapBld != selected)
                    target = snapBld.connectionPoint.position;
            }

            float range = selected.GetConnectionRange();
            if (Vector3.Distance(origin, target) > range)
                target = origin + (target - origin).normalized * range;

            tempCable.UpdatePreviewCurve(origin, target);
            SelectionHighlighter.Instance.HighlightInRange(origin, range);
        }
    }

    // Instantiates a new cable between two buildings.
    private void CreateCable(ConnectableObject a, ConnectableObject b)
    {
        ConnectionCable cable = Instantiate(cablePrefab).GetComponent<ConnectionCable>();
        cable.isPreview = false;
        cable.Initialize(a, b);
        a.AddConnection(b, cable);
        b.AddConnection(a, cable);
        NetworkEvents.TriggerNetworkChanged();
    }

    #endregion

    #region Delete Mode

    // Processes input and cable deletion while in delete mode.
    private void DeleteMode()
    {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Perform raycast for cable hover detection.
        if (Physics.Raycast(ray, out RaycastHit hoverHit))
        {
            ConnectionCable hoverCable = hoverHit.collider.GetComponentInParent<ConnectionCable>();
            if (hoverCable != null && !hoverCable.isPreview)
            {
                if (lastHoveredCable != hoverCable)
                {
                    lastHoveredCable?.SetHovered(false);
                    hoverCable.SetHovered(true);
                    lastHoveredCable = hoverCable;
                }
            }
            else if (lastHoveredCable != null)
            {
                lastHoveredCable.SetHovered(false);
                lastHoveredCable = null;
            }
        }
        else if (lastHoveredCable != null)
        {
            lastHoveredCable.SetHovered(false);
            lastHoveredCable = null;
        }

        // On left-click: delete the hovered cable.
        if (Input.GetMouseButtonDown(0) && lastHoveredCable != null)
        {
            lastHoveredCable.DeleteConnection();
            lastHoveredCable = null;
        }

        // On right-click: exit delete mode.
        if (Input.GetMouseButtonDown(1))
        {
            SetMode(0);
            highlighter.ClearHighlights();
            lastHoveredCable?.SetHovered(false);
            lastHoveredCable = null;
        }
    }

    #endregion

    #region Utility Methods


    // Returns the mouse position in world space (on a horizontal plane).
    private Vector3 GetMouseWorldPos()
    {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float dist) ? ray.GetPoint(dist) : Vector3.zero;
    }


    // Clears the current building selection and cable preview.
    private void Deselect()
    {
        highlighter.ClearHighlights();
        if (selected != null)
        {
            selected.ShowConnectionRangeIndicator(false);
            selected = null;
        }
        if (tempCable != null)
        {
            Destroy(tempCable.gameObject);
            tempCable = null;
        }
    }

    #endregion
}
