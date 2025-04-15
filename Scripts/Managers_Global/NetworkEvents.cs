#region NetworkEvents Class
/*
 * =========================================================
 * NetworkEvents
 *
 * Provides a simple event system for network changes.
 * =========================================================
 */
#endregion

using System;

public static class NetworkEvents
{
    // "OnNetworkChanged" Event triggered when the network state changes.
    public static event Action OnNetworkChanged;
    
    // Invokes the OnNetworkChanged event if there are subscribers.
    public static void TriggerNetworkChanged()
    {
        OnNetworkChanged?.Invoke();
    }
}
