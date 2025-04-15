#region CreditsManager Class
/*
 * =========================================================
 * CreditsManager
 *
 * Manages the player's credits and updates the UI accordingly.
 * Provides methods to spend and add credits.
 * =========================================================
 */
#endregion

using UnityEngine;

public class CreditsManager : MonoBehaviour
{
    #region Singleton

    // Singleton instance with a private setter.
    public static CreditsManager Instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance exists.
        if (Instance == null)
        {
            Instance = this;
            // Set the current credits to the starting value.
            currentCredits = startingCredits;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Public Fields

    [Header("Starting Credits")]
    [Tooltip("The initial amount of credits available.")]
    public int startingCredits = 10000;

    [Header("Current Balance")]
    [Tooltip("The player's current credit balance.")]
    public int currentCredits;

    #endregion

    #region Public Methods


    /// Attempts to spend the specified amount of credits.
    /// If sufficient credits are available, subtracts the amount, updates the UI, and returns true.
    /// Otherwise, flashes the UI and returns false.

    /// param name="amount" The amount of credits to spend.
    /// returns True if credits were spent, otherwise, false.
    public bool TrySpend(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            // Update the UI to show the credit loss.
            UIManager.Instance?.ShowCreditLossPopup(amount);
            UIManager.Instance?.UpdateCreditsUI(currentCredits);
            return true;
        }

        // Insufficient credits: flash the UI and log a warning.
        UIManager.Instance?.FlashCreditsRed(currentCredits);
        Debug.LogWarning($"Not enough credits. Needed: {amount}, Available: {currentCredits}");
        return false;
    }

    /// Adds the specified amount of credits to the player's balance and updates the UI.
    public void AddCredits(int amount)
    {
        currentCredits += amount;
        UIManager.Instance?.UpdateCreditsUI(currentCredits);
    }

    /// Retrieves the current credit balance.
    /// <returns>The player's current credits.</returns>
    public int GetCredits()
    {
        return currentCredits;
    }

    #endregion
}
