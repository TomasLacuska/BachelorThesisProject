#region MilestoneEventManager Class
/*
 * =========================================================
 * MilestoneEventManager
 *
 * Singleton manager for handling milestone-based building effects.
 * Applies or clears milestone modifiers across all ConnectableObjects.
 *
 * Provides:
 * - Global access to the active milestone event
 * - Apply/Clear operations for milestone logic
 * =========================================================
 */
#endregion

using UnityEngine;

public class MilestoneEventManager : MonoBehaviour
{
    #region Singleton

    public static MilestoneEventManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    #endregion

    #region Private Fields

    private MilestoneEventData activeEvent;

    #endregion

    #region Public Methods


    // Applies a new milestone event and updates all connectable objects.
    public void ApplyMilestoneEvent(MilestoneEventData newEvent)
    {
        activeEvent = newEvent;

        foreach (var co in ConnectableManager.Instance.GetAll())
        {
            if (co == null) continue;
            co.ApplyMilestoneEffect(activeEvent);
        }

        Debug.Log($"Applied milestone event: {activeEvent.id}");
        NetworkEvents.TriggerNetworkChanged();
    }


    // Clears the current milestone event and resets all affected buildings.
    public void ClearMilestoneEvent()
    {
        foreach (var co in ConnectableManager.Instance.GetAll())
        {
            co.ClearMilestoneEffect();
        }

        activeEvent = null;
        NetworkEvents.TriggerNetworkChanged();
    }


    // Returns the currently active milestone event.
    public MilestoneEventData GetActiveEvent() => activeEvent;

    #endregion
}
