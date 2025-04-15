using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class RuntimeTerrainDataClone : MonoBehaviour
{
    void Awake()
    {
        // Get the Terrain component attached to this GameObject.
        Terrain terrain = GetComponent<Terrain>();

        // Clone the TerrainData so that any modifications are applied only to the clone.
        TerrainData clonedData = Instantiate(terrain.terrainData);
        clonedData.name = terrain.terrainData.name + " (Clone)";
        terrain.terrainData = clonedData;

        Debug.Log("TerrainData cloned successfully. Runtime changes will not persist between sessions.");
    }
}
