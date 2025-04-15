#region BuildingData Class
/*
 * =========================================================
 * BuildingData
 *
 * Contains basic information for a building including ID, display name,
 * class tag, description, power values, connection range, costs, and maintenance.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class BuildingData
{
    #region Basic Building Information
    public string id;
    public string displayName;
    public string tagClass;
    public string description;
    public float basePower;
    public float maxPower;
    public float connectionRange;
    public int cost;
    public float ecoCost;
    public int maintenanceCost;
    #endregion

    #region Constructor
    // Constructs a BuildingData object with the provided values:
    // id: building identifier, 
    // name: display name, 
    // tag: classification tag,
    // desc: description, 
    // basePower: raw power, 
    // maxPower: maximum power,
    // range: connection range, 
    // cost: building cost, 
    // eco: ecological cost,
    // maintenance: maintenance cost.
    public BuildingData(string id, string name, string tag, string desc,
                        float basePower, float maxPower, float range, int cost, float eco, int maintenance)
    {
        this.id = id;
        this.displayName = name;
        this.tagClass = tag;
        this.description = desc;
        this.basePower = basePower;
        this.maxPower = maxPower;
        this.connectionRange = range;
        this.cost = cost;
        this.ecoCost = eco;
        this.maintenanceCost = maintenance;
    }
    #endregion
    
}
public class MilestoneEventData
{
    public string id;
    public string description;
    public string targetBuildingClass;
    public List<string> targetClasses = new();

    public float flatOutputBonus = 0f;
    public float flatMaxBonus = 0f;
    public float outputMultiplier = 1f;
    public float maxMultiplier = 1f;
    public Sprite icon;

    public MilestoneEventData(
        string id,
        string description,
        string targetClass,
        float outputMultiplier = 1f,
        float maxMultiplier = 1f,
        float flatOutputBonus = 0f,
        float flatMaxBonus = 0f
    )
    {
        this.id = id;
        this.description = description;
        this.outputMultiplier = outputMultiplier;
        this.maxMultiplier = maxMultiplier;
        this.targetBuildingClass = targetClass;
        this.flatOutputBonus = flatOutputBonus;
        this.flatMaxBonus = flatMaxBonus;

        // Split and trim the class tags
        if (!string.IsNullOrEmpty(targetClass))
        {
            var parts = targetClass.Split(',');
            foreach (var part in parts)
                targetClasses.Add(part.Trim());
        }
    }
    public bool Affects(string classTag) => targetClasses.Contains(classTag);

    public void SetIcon(Sprite sprite)
    {
        this.icon = sprite;
    }
}


