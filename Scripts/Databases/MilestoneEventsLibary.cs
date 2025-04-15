#region MilestoneEventLibrary Class
/*
 * =========================================================
 * MilestoneEventLibrary
 *
 * Provides a structured mapping of milestone event IDs to their corresponding
 * MilestoneEventData definitions. Used to apply global milestone effects
 * to buildings based on their class (e.g., "Wind").
 *
 * Use Get(string id) to retrieve a specific event.
 * Use GetAll() to get all available events.
 * Use GetRandomEvent() to retrieve a random one.
 * =========================================================
 */
#endregion

using System.Collections.Generic;
using UnityEngine;

public static class MilestoneEventLibrary
{
    #region Event Data Map

    // Dictionary mapping milestone event IDs to their definitions.
    public static Dictionary<string, MilestoneEventData> eventMap = new Dictionary<string, MilestoneEventData>()
    {
        {
            "windy",
            new MilestoneEventData(
                "windy",
                "Strong winds! Wind output +20%.",
                "Wind",
                outputMultiplier: 1.2f,
                maxMultiplier: 1.2f)
        },
        {
            "solar_flare",
            new MilestoneEventData(
                "solar_flare",
                "Clear skies! Solar output +30%.",
                "Solar",
                outputMultiplier: 1.3f,
                maxMultiplier: 1.3f)
        },
        {
            "drought",
            new MilestoneEventData(
                "drought",
                "Drought season. Hydro and Bio power -30%.",
                "Hydro,Bio",
                outputMultiplier: 0.7f,
                maxMultiplier: 0.7f)
        },
        {
            "overcast",
            new MilestoneEventData(
                "overcast",
                "The clouds block the sunlight. Solar power -40%.",
                "Solar",
                outputMultiplier: 0.6f,
                maxMultiplier: 0.6f)
        }
    };

    #endregion

    #region Public Methods
    static MilestoneEventLibrary()
    {
        foreach (var evt in eventMap.Values)
        {
            // Try to load icon with the same name as the event ID
            Sprite loadedIcon = Resources.Load<Sprite>($"MilestoneIcons/{evt.id}");
            if (loadedIcon != null)
                evt.SetIcon(loadedIcon);
            else
                Debug.LogWarning($"[MilestoneEventLibrary] No icon found for event '{evt.id}' in Resources/MilestoneIcons/");
        }
    }
    // Retrieves a specific milestone event by ID
    public static MilestoneEventData Get(string id)
    {
        if (eventMap.TryGetValue(id, out MilestoneEventData data))
            return data;
        return null;
    }

    // Returns all milestone event definitions
    public static List<MilestoneEventData> GetAll()
    {
        return new List<MilestoneEventData>(eventMap.Values);
    }

    // Returns a random milestone event (can be used for testing or game progression)
    public static MilestoneEventData GetRandomEvent()
    {
        List<MilestoneEventData> all = GetAll();
        if (all.Count == 0) return null;
        return all[Random.Range(0, all.Count)];
    }

    #endregion
}
