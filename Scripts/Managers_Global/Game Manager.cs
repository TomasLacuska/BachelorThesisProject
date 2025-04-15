#region GameManager Class
/*
 * =========================================================
 * GameManager
 *
 * A singleton manager that persists between scenes.
 * Provides functionality for restarting or quitting the game.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Singleton

    // Singleton instance of the GameManager.
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }



    #endregion

    #region Public Methods

    // Restarts the current scene.
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Quits the game or logs a message in the editor.
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[GameManager] QuitGame() called – would exit application (Editor only)");
#else
        Application.Quit();
#endif
    }

    #endregion
}
