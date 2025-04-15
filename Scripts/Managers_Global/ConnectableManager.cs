#region ConnectableManager Class
/*
 * =========================================================
 * ConnectableManager
 *
 * Central manager that tracks all placed ConnectableObjects in the scene.
 * Used by systems like MilestoneEventManager, UIManager, and NetworkManager
 * to get access to live, instantiated power-producing objects.
 * =========================================================
 */
#endregion

using System.Collections.Generic;
using UnityEngine;

public class ConnectableManager : MonoBehaviour
{
    #region Singleton
    public static ConnectableManager Instance { get; private set; }
    #endregion

    #region Registered Connectables
    private readonly List<ConnectableObject> activeConnectables = new List<ConnectableObject>();
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    #endregion

    #region Registration Methods

    public void Register(ConnectableObject obj)
    {
        if (!activeConnectables.Contains(obj))
        {
            activeConnectables.Add(obj);
        }
    }

    public void Unregister(ConnectableObject obj)
    {
        if (activeConnectables.Contains(obj))
        {
            activeConnectables.Remove(obj);
        }
    }

    public List<ConnectableObject> GetAll() => activeConnectables;

    #endregion
}
