using System;
using System.Collections.Generic;

// MilestoneEvents provides a global event that is fired whenever a milestone event changes.
// Dynamic building components (and static ones forced to update) should notify when they have finished updating.
public static class MilestoneEvents
{
    public static event Action OnMilestoneChanged;
    public static event Action OnMilestoneRefreshReady;

    private static int pendingUpdates = 0;

    // Call this to trigger all dynamic (and static) building updates to recalc their production.
    // The parameter totalDynamicBuildings should be the total number of ConnectableObjects that require dynamic updating.
    public static void TriggerMilestoneChanged(int totalDynamicBuildings)
    {
        pendingUpdates = totalDynamicBuildings;
        OnMilestoneChanged?.Invoke();
    }


    // Each ConnectableObject (or its dynamic component) calls this once it updates its production.
    // When all have reported, OnMilestoneRefreshReady is fired.

    public static void NotifyDynamicBuildingUpdated()
    {
        pendingUpdates = Math.Max(0, pendingUpdates - 1);
        if (pendingUpdates == 0)
        {
            OnMilestoneRefreshReady?.Invoke(); // All dynamic buildings updated
        }
    }
}
