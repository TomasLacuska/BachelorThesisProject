#region CameraController Class
/*
 * =========================================================
 * CameraController
 *
 * Handles panning, rotation, and zooming of the camera pivot.
 * It uses keyboard inputs for rotation and panning, and the scroll wheel for zoom.
 * =========================================================
 */
#endregion

using UnityEngine;

public class CameraController : MonoBehaviour
{
    #region Public Fields

    [Header("References")]
    [Tooltip("Camera that is a child of this pivot.")]
    public Transform cameraTransform;

    [Header("Panning")]
    [Tooltip("Speed for panning (WASD/Arrow Keys).")]
    public float panSpeed = 20f;

    [Header("Rotation")]
    [Tooltip("Rotation step (in degrees) per key press (Q/E).")]
    public float rotationStep = 90f;
    [Tooltip("Time for smooth rotation interpolation.")]
    public float rotationSmoothTime = 0.2f;

    [Header("Zoom")]
    [Tooltip("Speed multiplier for zooming.")]
    public float zoomSpeed = 5f;
    [Tooltip("Time for smooth zoom interpolation.")]
    public float zoomSmoothTime = 0.2f;
    [Tooltip("Minimum distance from the pivot when zooming in.")]
    public float minZoomDistance = 3f;

    [Header("Terrain Following")]
    [Tooltip("Offset above the terrain height.")]
    public float heightAboveTerrain = 2f;
    [Tooltip("Layers considered as terrain.")]
    public LayerMask terrainLayer;
    [Tooltip("Enable this to visualize terrain raycast.")]
    public bool debugTerrainRay = false;


    #endregion

    #region Private Fields

    // Rotation variables.
    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;

    // Zoom variables.
    private Vector3 initialCameraLocalPos;
    private Vector3 zoomDirection;
    private float restDistance;
    private float currentDistance;
    private float targetDistance;
    private float zoomVelocity;

    // Panning variables.
    private Vector3 basePivotPosition;
    private float currentHeight;
    private float heightVelocity;


    #endregion

    #region Unity Methods

    void Start()
    {
        // Store the camera's initial local position.
        initialCameraLocalPos = cameraTransform.localPosition;
        // Compute the rest distance (magnitude of the initial local position).
        restDistance = initialCameraLocalPos.magnitude;
        // Compute the normalized zoom direction.
        zoomDirection = initialCameraLocalPos.normalized;
        
        // Start at the rest distance.
        currentDistance = restDistance;
        targetDistance = restDistance;

        // Record the pivot's starting position.
        basePivotPosition = transform.position;
        currentHeight = transform.position.y;

        // Initialize rotation based on the pivot's current Y rotation.
        targetYaw = transform.eulerAngles.y;
        currentYaw = targetYaw;
    }

    void Update()
    {
        // ----- ROTATION -----
        // Adjust target yaw using Q and E keys.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            targetYaw -= rotationStep;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            targetYaw += rotationStep;
        }
        // Smoothly interpolate the current yaw toward the target.
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        // ----- PANNING -----
        // Retrieve horizontal and vertical input for panning.
        float panH = Input.GetAxis("Horizontal");
        float panV = Input.GetAxis("Vertical");

        // Use pivot's forward and right vectors (projected onto the horizontal plane).
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        // Calculate movement and update the pivot's base position.
        Vector3 panMovement = (right * panH + forward * panV) * panSpeed * Time.deltaTime;
        basePivotPosition += panMovement;

        // Raycast downward to find terrain height
        Ray ray = new Ray(basePivotPosition + Vector3.up * 200f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 250f, terrainLayer))
        {
            float desiredHeight = hit.point.y + heightAboveTerrain;

            // Smoothly interpolate current height to desired terrain height
            currentHeight = Mathf.SmoothDamp(currentHeight, desiredHeight, ref heightVelocity, 0.2f);

            transform.position = new Vector3(basePivotPosition.x, currentHeight, basePivotPosition.z);

            if (debugTerrainRay)
                Debug.DrawRay(ray.origin, ray.direction * 200f, Color.green);
        }
        else
        {
            // No terrain hit — keep current height
            transform.position = new Vector3(basePivotPosition.x, currentHeight, basePivotPosition.z);
        }




        // ----- ZOOM -----
        // Get scroll wheel input.
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            // Scrolling forward zooms in (reduces distance).
            targetDistance -= scrollInput * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, restDistance);
        }
        // Smoothly interpolate the current distance toward the target.
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
        // Update the camera's local position along the zoom direction.
        cameraTransform.localPosition = zoomDirection * currentDistance;
    }

    #endregion
}
