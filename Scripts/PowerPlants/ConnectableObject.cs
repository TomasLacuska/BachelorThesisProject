#region ConnectableObject Class
/*
 * =========================================================
 * ConnectableObject
 *
 * Represents an object that can be connected to a power network.
 * Retrieves building data, manages cable connections, and updates network state.
 *
 * Milestone effects:
 *   - For static buildings (only having ConnectableObject):
 *         final power = (basePower + milestoneFlatOutputBonus) * milestoneOutputMultiplier
 *         effective max power = (maxProduction + milestoneFlatMaxBonus) * milestoneMaxMultiplier
 *
 *   - For dynamic buildings (with additional components like SolarPanelStatic,
 *     WindTurbine, BioBurner, HydroDam), they can retrieve the milestone multipliers
 *     via getters and adjust their own computed power accordingly.
 *
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections.Generic;

public class ConnectableObject : MonoBehaviour
{
    #region Public Fields

    [Header("Placement Settings")]
    // Number of subdivisions (samples per edge) for placement raycasts.
    public int placementSubdivision = 4;
    // Manually set the VFX scale (radius for the effect).
    public float vfxScale = 1f;
    // If true, building is placed at highest ground Y.
    public bool placePlatform = false;
    // Maximum allowed slope (degrees) for placement.
    public float placementMaxSlopeAngle = 30f;

    [Header("Database Reference")]
    // Unique building ID to fetch data from the BuildingInfoLibrary.
    public string buildingID;

    // Base point for cable connections.
    public Transform connectionPoint;
    // Runtime power produced (final computed value).
    public int powerProduction = 0;

    // Indicates whether this object is part of the connected power network.
    public bool isInNetwork = false;

    #endregion

    #region Private Fields

    // Values populated from BuildingData.
    private float baseProduction = 0f;
    private float maxProduction = 0f;
    private float connectionRange = 0f;
    private int placementCost = 0;
    private float ecoCost = 0f;
    private int maintenanceCost = 0;

    // New Milestone bonus variables.
    // These are applied only if a milestone event affecting this building's class is active.
    private float milestoneOutputMultiplier = 1f;   // Multiplier to final power output.
    private float milestoneMaxMultiplier = 1f;      // Multiplier to max power cap.
    private float milestoneFlatOutputBonus = 0f;    // Flat bonus added to base production.
    private float milestoneFlatMaxBonus = 0f;       // Flat bonus added to max production.

    // List of connections to other ConnectableObjects.
    private List<ConnectionData> connections = new List<ConnectionData>();

    #endregion

    #region Inner Classes

    [System.Serializable]
    public class ConnectionData
    {
        public ConnectableObject other;
        public ConnectionCable cable;
    }

    #endregion

    #region Unity Methods

    void Awake()
    {
        // Register this object with the global ConnectableManager.
        ConnectableManager.Instance?.Register(this);
        
        // Retrieve building data using buildingID from the BuildingInfoLibrary.
        if (!string.IsNullOrEmpty(buildingID))
        {
            BuildingData data = BuildingInfoLibrary.Get(buildingID);
            if (data != null)
            {
                baseProduction = data.basePower;
                maxProduction = data.maxPower;
                connectionRange = data.connectionRange;
                powerProduction = Mathf.RoundToInt(baseProduction);
                placementCost = data.cost;
                ecoCost = data.ecoCost;
                maintenanceCost = data.maintenanceCost;
            }
            else
            {
                Debug.LogWarning($"{name}: BuildingData not found for buildingID: {buildingID}");
            }
        }
        else
        {
            Debug.LogWarning($"{name}: buildingID not set.");
        }

        // If a milestone event is active, apply its effects to new buildings.
        if (MilestoneEventManager.Instance != null)
        {
            MilestoneEventData evt = MilestoneEventManager.Instance.GetActiveEvent();
            if (evt != null)
                ApplyMilestoneEffect(evt);
        }
    }

    void OnDestroy()
    {
        ConnectableManager.Instance?.Unregister(this);

        // Destroy all associated cables.
        connections.ForEach(c => { if (c.cable) Destroy(c.cable.gameObject); });

        if (isInNetwork)
            NetworkEvents.TriggerNetworkChanged(); // Only if meaningful
    }


    #endregion

    #region Getter Methods

    // Returns the base power from BuildingData.
    public float GetBaseProduction() => baseProduction;
    
    // Returns the effective max production considering milestone effects.
    public float GetEffectiveMaxProduction() =>
        (maxProduction + milestoneFlatMaxBonus) * milestoneMaxMultiplier;

    // Returns the connection range.
    public float GetConnectionRange() => connectionRange;
    // Returns the placement cost.
    public int GetPlacementCost() => placementCost;
    // Returns the ecological cost.
    public float GetEcoCost() => ecoCost;
    // Returns the maintenance cost.
    public int GetMaintenanceCost() => maintenanceCost;
    
    // Returns the milestone output multiplier for external use.
    public float GetMilestoneOutputMultiplier() => milestoneOutputMultiplier;
    // Returns the flat output bonus.
    public float GetMilestoneFlatOutputBonus() => milestoneFlatOutputBonus;

    #endregion

    #region Connection Management

    // Adds a connection to another object if not already connected.
    public void AddConnection(ConnectableObject other, ConnectionCable cable)
    {
        if (!IsAlreadyConnectedTo(other))
            connections.Add(new ConnectionData { other = other, cable = cable });
    }

    // Checks if already connected to the specified object.
    public bool IsAlreadyConnectedTo(ConnectableObject other) =>
        connections.Exists(c => c.other == other);

    // Removes the connection to the specified object.
    public void RemoveConnection(ConnectableObject other)
    {
        connections.RemoveAll(c => c.other == other);
        NetworkEvents.TriggerNetworkChanged();
    }

    // Returns an enumerable of connected objects.
    public IEnumerable<ConnectableObject> GetConnectedNodes()
    {
        foreach (var c in connections)
            if (c.other != null)
                yield return c.other;
    }

    #endregion

    #region Network State Management

    // Sets the network connection state and logs connectivity changes.
    public void SetInNetwork(bool state)
    {
        if (state && !isInNetwork)
        {
            isInNetwork = true;
            Debug.Log($"{name} is now connected to the power network.");
        }
        else if (!state && isInNetwork)
        {
            isInNetwork = false;
            Debug.Log($"{name} is disconnected from the power network.");
        }
    }

    #endregion

    #region Milestone Handlers

    // Applies the milestone event effects if this building's tag matches the event target.
    public void ApplyMilestoneEffect(MilestoneEventData evt)
    {
        BuildingData data = BuildingInfoLibrary.Get(buildingID);
        if (data != null && evt.Affects(data.tagClass))
        {
            milestoneOutputMultiplier = evt.outputMultiplier;
            milestoneMaxMultiplier = evt.maxMultiplier;
            milestoneFlatOutputBonus = evt.flatOutputBonus;
            milestoneFlatMaxBonus = evt.flatMaxBonus;
        }
        else
        {
            // Reset if the milestone event does not target this building.
            milestoneOutputMultiplier = 1f;
            milestoneMaxMultiplier = 1f;
            milestoneFlatOutputBonus = 0f;
            milestoneFlatMaxBonus = 0f;
        }
        UpdatePowerProduction();
    }

    // Clears any active milestone effects, returning multipliers and bonuses to default values.
    public void ClearMilestoneEffect()
    {
        milestoneOutputMultiplier = 1f;
        milestoneMaxMultiplier = 1f;
        milestoneFlatOutputBonus = 0f;
        milestoneFlatMaxBonus = 0f;
        UpdatePowerProduction();
    }

    // Updates the final power production based on base values and milestone modifications.
    private void UpdatePowerProduction()
    {
        // Calculation for static buildings:
        // final power = (baseProduction + flat bonus) * output multiplier.
        float computed = (baseProduction + milestoneFlatOutputBonus) * milestoneOutputMultiplier;
        int effectiveMax = Mathf.RoundToInt((maxProduction + milestoneFlatMaxBonus) * milestoneMaxMultiplier);
        powerProduction = Mathf.RoundToInt(Mathf.Clamp(computed, 0, effectiveMax));
        Debug.Log($"{name}: Power updated to {powerProduction} (Base: {baseProduction}, +FlatBonus: {milestoneFlatOutputBonus}, xMultiplier: {milestoneOutputMultiplier}; MaxCap: {maxProduction} +Flat: {milestoneFlatMaxBonus}, x: {milestoneMaxMultiplier} = {effectiveMax})");
    }

    // --------------------------------------------------------------------
    // UpdateWithEnvironmentMultiplier() Method
    //
    // This method allows external building components to update the final power
    // production based on an environmental multiplier (e.g., sunlight, wind height,
    // number of reserved farms). It uses the stored base power, adds any flat bonus,
    // multiplies by the milestone output multiplier, and then scales by the provided
    // environment multiplier. The result is clamped to the effective max production.
    // --------------------------------------------------------------------
    public void UpdateWithEnvironmentMultiplier(float multiplier)
    {
        float baseWithBonus = baseProduction + milestoneFlatOutputBonus;
        float computed = baseWithBonus * milestoneOutputMultiplier * multiplier;
        float max = (maxProduction + milestoneFlatMaxBonus) * milestoneMaxMultiplier;
        powerProduction = Mathf.RoundToInt(Mathf.Clamp(computed, 0, max));
    }
    // --------------------------------------------------------------------

    #endregion

    #region UI Methods

    // Shows or hides the connection range indicator.
    // Scales the indicator to match the connection range.
    public void ShowConnectionRangeIndicator(bool enable)
    {
        RangeIndicator[] indicators = GetComponentsInChildren<RangeIndicator>(true);
        foreach (RangeIndicator indicator in indicators)
        {
            if (indicator.gameObject.CompareTag("ConnectionRange"))
            {
                indicator.transform.localScale = Vector3.one * connectionRange;
                if (enable)
                    indicator.EnableIndicator(false);
                else
                    indicator.DisableIndicator();
            }
        }
    }

    #endregion

    #region Additional Public Method for ForceUpdate

    // Force recalculates the production for static buildings (ones without a dynamic component).
    public void ForceUpdate()
    {
        // For static buildings, simply recalc the production using UpdatePowerProduction().
        // (This method is already defined privately; you now expose it via ForceUpdate.)
        UpdatePowerProduction();
        MilestoneEvents.NotifyDynamicBuildingUpdated();
    }

    #endregion

}
