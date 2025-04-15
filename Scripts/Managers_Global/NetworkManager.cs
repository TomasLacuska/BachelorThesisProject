#region PowerNetworkManager Class
/*
 * =========================================================
 * PowerNetworkManager
 *
 * Manages the connectivity of the power network by recalculating
 * which nodes are connected starting from the goal. Uses a breadth-first
 * search (BFS) to update each node's connectivity and sums the total power
 * production.
 * =========================================================
 */
#endregion

using UnityEngine;
using System.Collections.Generic;

public class PowerNetworkManager : MonoBehaviour
{
    // Singleton
    public static PowerNetworkManager Instance { get; private set; }


    [Tooltip("The minimum total power required for the network to be considered powered.")]
    public int requiredPower = 100;
    
    [Tooltip("Reference to the Goal building (the consumer).")]
    public ConnectableObject goal;
    
    public int TotalPowerProduction { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Subscribes to the network change event and recalculates network connectivity.
    void OnEnable()
    {
        NetworkEvents.OnNetworkChanged += RecalculateNetwork;
        RecalculateNetwork();
    }
    
    // Unsubscribes from the network change event.
    void OnDisable()
    {
        NetworkEvents.OnNetworkChanged -= RecalculateNetwork;
    }
    
    // Performs a breadth-first search (BFS) starting from the goal to determine connectivity.
    // Updates the connectivity state of each node and logs power production status.
    public void RecalculateNetwork()
    {
        // "allNodes" All ConnectableObject nodes in the scene.
        var allNodes = FindObjectsByType<ConnectableObject>(FindObjectsSortMode.None);
        
        if (goal == null)
            return;
        
        // Initialize BFS with the goal node.
        Queue<ConnectableObject> queue = new Queue<ConnectableObject>();
        HashSet<ConnectableObject> visited = new HashSet<ConnectableObject>();
        
        queue.Enqueue(goal);
        visited.Add(goal);
        
        // Process each node and enqueue its unvisited neighbors.
        while (queue.Count > 0)
        {
            ConnectableObject node = queue.Dequeue();
            foreach (var neighbor in node.GetConnectedNodes())
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        // Update connectivity status for each node.
        foreach (var node in allNodes)
        {
            node.SetInNetwork(visited.Contains(node));
        }
        
        // Sum the total power production of connected nodes.
        TotalPowerProduction = 0;
        foreach (var node in visited)
        {
            TotalPowerProduction += node.powerProduction;
        }
        
        if (TotalPowerProduction < requiredPower)
            Debug.LogWarning($"Insufficient power: {TotalPowerProduction} available, {requiredPower} required.");
        else
            Debug.Log($"Network is sufficiently powered: {TotalPowerProduction} available.");
    }
}
