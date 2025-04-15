#region CableUIButton Class
/*
 * =========================================================
 * CableUIButton
 *
 * Updates the button's appearance based on the current cable mode.
 * Changes the button's color to indicate whether it is selected.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.UI;

public class CableUIButton : MonoBehaviour
{
    #region Public Fields

    [Tooltip("Mode ID for this button (1 = Build, 2 = Delete).")]
    public int modeID;
    [Tooltip("Image component of the button.")]
    public Image buttonImage;
    [Tooltip("Default color when not selected.")]
    public Color normalColor = Color.white;
    [Tooltip("Color when selected.")]
    public Color selectedColor = Color.yellow;

    #endregion

    #region Unity Methods

    void Update()
    {
        if (ConnectionManager.Instance == null || buttonImage == null)
            return;

        bool selected = ConnectionManager.Instance.currentMode == modeID;
        buttonImage.color = selected ? selectedColor : normalColor;
    }

    #endregion
}
