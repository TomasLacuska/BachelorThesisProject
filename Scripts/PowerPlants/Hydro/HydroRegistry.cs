#region HydroRegistry Class
/*
 * =========================================================
 * HydroRegistry
 *
 * Maintains a registry of all hydro spots in the scene.
 * Populates the list of hydro spot transforms based on the "Hydro" tag.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections.Generic;

public class HydroRegistry : MonoBehaviour
{
    #region Singleton

    public static HydroRegistry Instance { get; private set; }

    private void Awake()
    {
        // Implement singleton pattern: destroy duplicates.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally persist this manager across scenes:
        // DontDestroyOnLoad(gameObject);

        // Populate HydroSpots from GameObjects tagged "Hydro".
        HydroSpots.Clear();
        GameObject[] all = GameObject.FindGameObjectsWithTag("Hydro");
        foreach (var obj in all)
        {
            HydroSpots.Add(obj.transform);
        }
    }

    #endregion

    #region Public Properties

    // Public list of hydro spot transforms.
    public List<Transform> HydroSpots { get; private set; } = new List<Transform>();

    #endregion
}
