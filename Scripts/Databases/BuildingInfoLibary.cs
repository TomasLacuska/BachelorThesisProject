#region BuildingInfoLibrary Class
/*
 * =========================================================
 * BuildingInfoLibrary
 *
 * Provides a mapping from building IDs to BuildingData objects.
 * Use Get(string id) to retrieve building information by ID.
 * =========================================================
 */
#endregion


using System.Collections.Generic;

public static class BuildingInfoLibrary
{
    // Dictionary mapping building IDs to BuildingData objects.
    public static Dictionary<string, BuildingData> buildingMap = new Dictionary<string, BuildingData>()
    {
        {
            // Coal Power Plant – Flat output building
            "coal_plant", new BuildingData(
                /*ID*/"coal_plant",
                /*Name*/"Coal Power Plant",
                /*Class*/"Fossil",
                /*Description*/"An efficient but polluting fossil fuel plant.",
                /*BasePower*/70f,
                /*MaxPower*/70f, 
                /*ConnectionRange*/25f,
                /*Cost*/4000,
                /*EcoCost*/40f,
                /*Maintenance*/25)
        },
        {
            // Wind Turbine – Dynamic multiplier building
            "wind_basic", new BuildingData(
                /*ID*/"wind_basic",
                /*Name*/"Wind Turbine",
                /*Class*/"Wind",
                /*Description*/"Lightweight HAWT for low-production areas.\n- Cannot be placed too close to other turbines.\n- Prefers low, flat terrain.",
                /*BasePower*/12.5f,
                /*MaxPower*/25f, 
                /*ConnectionRange*/25f,
                /*Cost*/2000,
                /*EcoCost*/0f,
                /*Maintenance*/10)
        },
        {
            // Offshore Wind Turbine – Dynamic multiplier building
            "wind_offshore", new BuildingData(
                /*ID*/"wind_offshore",
                /*Name*/"Offshore Wind Turbine",
                /*Class*/"Wind",
                /*Description*/"Offshore version of the normal HAWT, produces much more due to high winds next to the shore.\n- Cannot be placed too close to other turbines.\n- Can only be placed on the sea.",
                /*BasePower*/12.5f,
                /*MaxPower*/25f, 
                /*ConnectionRange*/25f,
                /*Cost*/2000,
                /*EcoCost*/0f,
                /*Maintenance*/10)
        },
        {
            // Static Solar Farm – Dynamic multiplier building
            "solar_static", new BuildingData(
                /*ID*/"solar_static",
                /*Name*/"Static Solar Farm",
                /*Class*/"Solar",
                /*Description*/"Static solar fields utilize the power of the SUN.\n- Must be turned towards the sun or no power is produced.",
                /*BasePower*/60f,
                /*MaxPower*/120f, 
                /*ConnectionRange*/25f,
                /*Cost*/12000,
                /*EcoCost*/0f,
                /*Maintenance*/15)
        },
        {
            // Hydro Dam – Flat output
            "hydro_dam", new BuildingData(
                /*ID*/"hydro_dam",
                /*Name*/"Dam Power Plant",
                /*Class*/"Hydro",
                /*Description*/"A big dam that provides steady power. \n- Can only be placed in high walled rivers.",
                /*BasePower*/300f,
                /*MaxPower*/300f,
                /*ConnectionRange*/25f,
                /*Cost*/30000,
                /*EcoCost*/0f,
                /*Maintenance*/150)
        },
        {
            // Small Power Pole – Infrastructure, no power
            "power_pole_small", new BuildingData(
                /*ID*/"power_pole_small",
                /*Name*/"Small Power Pole",
                /*Class*/"Infrastructure",
                /*Description*/"Small poles. Short range wiring.",
                /*BasePower*/0f,
                /*MaxPower*/0f,
                /*ConnectionRange*/50f,
                /*Cost*/500,
                /*EcoCost*/0f,
                /*Maintenance*/2)
        },
        {
            // Big Power Pole – Infrastructure, no power
            "power_pole_big", new BuildingData(
                /*ID*/"power_pole_big",
                /*Name*/"Big Power Pole",
                /*Class*/"Infrastructure",
                /*Description*/"Great for long distance wiring.",
                /*BasePower*/0f,
                /*MaxPower*/0f,
                /*ConnectionRange*/100f,
                /*Cost*/1000,
                /*EcoCost*/0f,
                /*Maintenance*/4)
        },
        {
            // Bio Burner Plant – Dynamic multiplier building
            "bio_plant", new BuildingData(
                /*ID*/"bio_plant",
                /*Name*/"Bio Burner Plant",
                /*Class*/"Bio",
                /*Description*/"A powerful renewable burner that uses waste from farms as fuel.\n- Has to be placed next to farms to function.\n- Reserves farms upon placement, no other B.B. can use them.",
                /*BasePower*/10f,
                /*MaxPower*/150f, 
                /*ConnectionRange*/25f,
                /*Cost*/15000,
                /*EcoCost*/20f,
                /*Maintenance*/35)
        }
    };

    public static BuildingData Get(string id)
    {
        if (buildingMap.TryGetValue(id, out BuildingData data))
            return data;
        return null;
    }
}
