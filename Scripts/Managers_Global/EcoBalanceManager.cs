#region EcoBalanceManager Class
/*
 * =========================================================
 * EcoBalanceManager
 *
 * Manages the ecological balance by tracking eco points and 
 * applying environmental effects such as vegetation removal 
 * under buildings and gradual pollution on trees.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EcoBalanceManager : MonoBehaviour
{
    #region Singleton

    public static EcoBalanceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    #endregion

    #region Public Fields

    [Header("Terrain Reference")]
    [Tooltip("Reference to the Terrain.")]
    public Terrain terrain;

    [Header("Eco Balance")]
    [Tooltip("Current ecological balance.")]
    public float ecoBalance = 1000f;
    [Tooltip("Original ecological balance.")]
    public float originEco = 1000f;

    [Header("Tree Prototype Indices")]
    [Tooltip("Index of the 'live' tree in the Terrain's tree prototypes array.")]
    public int liveTreePrototypeIndex = 0;
    [Tooltip("Index of the 'dead' tree in the Terrain's tree prototypes array.")]
    public int deadTreePrototypeIndex = 1;

    [Header("Pollution State")]
    [Tooltip("Indicates if a pollution effect is currently in progress.")]
    public bool isPollutionInProgress = false;

    #endregion

    #region Public Methods

    
    // Removes vegetation (trees and detail objects) under the specified building.
    // Deducts eco balance points per removed tree and updates the UI.
    
    // "building" The building GameObject.
    public void RemoveVegetationUnderBuilding(GameObject building)
    {
        Vector2[] footprint = ComputeBuildingFootprint(building);
        if (footprint == null)
        {
            Debug.LogWarning("Could not compute building footprint for vegetation removal!");
            return;
        }

        TerrainData tData = terrain.terrainData;
        List<TreeInstance> originalTrees = new List<TreeInstance>(tData.treeInstances);
        List<TreeInstance> keptTrees = new List<TreeInstance>();
        int treesRemoved = 0;

        // Remove trees that are within the building's footprint.
        for (int i = 0; i < originalTrees.Count; i++)
        {
            TreeInstance tree = originalTrees[i];
            Vector3 treeWorldPos = GetTreeWorldPos(tree, tData);
            Vector2 treePos2D = new Vector2(treeWorldPos.x, treeWorldPos.z);
            if (IsPointInPolygon(treePos2D, footprint))
                treesRemoved++;
            else
                keptTrees.Add(tree);
        }
        tData.treeInstances = keptTrees.ToArray();
        terrain.Flush();

        // Remove detail objects within the footprint.
        int detailLayerCount = tData.detailPrototypes.Length;
        for (int layer = 0; layer < detailLayerCount; layer++)
        {
            int[,] detailLayerData = tData.GetDetailLayer(0, 0, tData.detailWidth, tData.detailHeight, layer);
            for (int y = 0; y < tData.detailHeight; y++)
            {
                for (int x = 0; x < tData.detailWidth; x++)
                {
                    float posX = x / (float)tData.detailWidth * tData.size.x + terrain.transform.position.x;
                    float posZ = y / (float)tData.detailHeight * tData.size.z + terrain.transform.position.z;
                    Vector2 detailPos2D = new Vector2(posX, posZ);
                    if (IsPointInPolygon(detailPos2D, footprint))
                        detailLayerData[y, x] = 0;
                }
            }
            tData.SetDetailLayer(0, 0, layer, detailLayerData);
        }

        // Deduct eco balance points and update UI.
        ecoBalance -= treesRemoved;
        UIManager.Instance?.ShowEcoLossPopup(treesRemoved);
        UIManager.Instance?.UpdateEcoBalanceUI();
        Debug.Log($"Removed {treesRemoved} trees under building. EcoBalance now {ecoBalance}");
    }

    
    // Gradually applies a pollution effect to live trees within a given radius around a building.
    
    // "building" The building GameObject.
    // "treePollutionValue" Pollution radius and intensity value.
    // "duration" Duration over which the effect is applied in seconds.
    public void ApplyPollutionEffectGradually(GameObject building, float treePollutionValue, float duration = 4f)
    {
        if (terrain == null) return;
        isPollutionInProgress = true;

        TerrainData tData = terrain.terrainData;
        TreeInstance[] trees = tData.treeInstances;
        List<(int index, float distance)> affectedTrees = new List<(int, float)>();

        // Identify live trees within the pollution radius.
        for (int i = 0; i < trees.Length; i++)
        {
            if (trees[i].prototypeIndex == liveTreePrototypeIndex)
            {
                Vector3 worldPos = GetTreeWorldPos(trees[i], tData);
                float dist = Vector3.Distance(building.transform.position, worldPos);
                if (dist <= treePollutionValue)
                    affectedTrees.Add((i, dist));
            }
        }

        affectedTrees.Sort((a, b) => a.distance.CompareTo(b.distance));
        StartCoroutine(GraduallyPolluteTreesCoroutine(building, treePollutionValue, duration, affectedTrees, trees));
    }

    #endregion

    #region Private Methods

    
    /// Computes a building's 2D footprint based on its colliders.
    
    /// "building" The building GameObject.
    /// returns An array of four Vector2 points representing the footprint, or null if not computable.
    private Vector2[] ComputeBuildingFootprint(GameObject building)
    {
        Bounds unionBounds = GetCompoundColliderBounds(building);
        if (unionBounds.size == Vector3.zero)
            return null;

        Vector3 extents = unionBounds.extents;
        Vector3[] localOffsets = new Vector3[4]
        {
            new Vector3(-extents.x, 0, -extents.z),
            new Vector3(extents.x, 0, -extents.z),
            new Vector3(extents.x, 0, extents.z),
            new Vector3(-extents.x, 0, extents.z)
        };

        Vector3 basePos = building.transform.position;
        Quaternion rot = building.transform.rotation;
        Vector2[] footprint = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 worldCorner = basePos + (rot * localOffsets[i]);
            footprint[i] = new Vector2(worldCorner.x, worldCorner.z);
        }
        return footprint;
    }

    
    // Checks whether a 2D point lies within a polygon.
    // "point" The 2D point.
    // "polygon" An array of polygon vertices.
    // returns True if the point is inside the polygon; otherwise, false.
    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int crossingNumber = 0;
        int count = polygon.Length;
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                crossingNumber++;
            }
        }
        return crossingNumber % 2 == 1;
    }

    
    /// Computes the combined bounds of all colliders attached to the building.
    
    /// "building">The building GameObject.
    /// returns A Bounds object representing the union of all child colliders.
    private Bounds GetCompoundColliderBounds(GameObject building)
    {
        Collider[] colliders = building.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            return new Bounds(building.transform.position, Vector3.zero);

        Bounds unionBounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
            unionBounds.Encapsulate(colliders[i].bounds);
        return unionBounds;
    }

    
    /// Converts a tree instance's normalized position to its world space position.
    
    /// "tree">The tree instance.
    /// "tData">The TerrainData.
    /// returns The world position of the tree.
    private Vector3 GetTreeWorldPos(TreeInstance tree, TerrainData tData)
    {
        Vector3 scalePos = Vector3.Scale(tree.position, tData.size);
        return terrain.transform.position + scalePos;
    }

    #endregion

    #region Coroutines

    
    /// Gradually applies a pollution effect to live trees over the specified duration.
    /// Changes tree prototypes to represent dead trees based on their distance from the building.
    
    private IEnumerator GraduallyPolluteTreesCoroutine(GameObject building, float treePollutionValue, float duration, List<(int index, float distance)> affectedTrees, TreeInstance[] trees)
    {
        float inner = treePollutionValue * 0.33f;
        float mid = treePollutionValue * 0.66f;
        int treesKilled = 0;
        int totalAffected = affectedTrees.Count;
        float interval = duration / Mathf.Max(1, totalAffected);

        for (int i = 0; i < totalAffected; i++)
        {
            yield return new WaitForSeconds(interval);
            int idx = affectedTrees[i].index;
            float dist = affectedTrees[i].distance;
            bool killTree;
            if (dist <= inner)
                killTree = true;
            else if (dist <= mid)
                killTree = Random.value < 0.5f;
            else
                killTree = Random.value < 0.2f;

            if (killTree)
            {
                trees[idx].prototypeIndex = deadTreePrototypeIndex;
                treesKilled++;
            }
            terrain.terrainData.treeInstances = trees;
            terrain.Flush();
        }

        isPollutionInProgress = false;
        UIManager.Instance?.UpdateEcoBalanceUI();
    }

    #endregion
}
