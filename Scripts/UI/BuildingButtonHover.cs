#region BuildingButtonHover Class
/*
 * =========================================================
 * BuildingButtonHover
 *
 * Handles pointer hover events for building UI buttons.
 * When the pointer enters the button, it displays the building tooltip;
 * when the pointer exits, it hides the tooltip.
 * =========================================================
 */
#endregion

using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Public Fields

    [Tooltip("Unique building ID for which the tooltip should be shown.")]
    public string buildingID;

    #endregion

    #region Interface Methods

    // Called when the pointer enters the UI element.
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.ShowBuildingTooltip(buildingID);
    }

    // Called when the pointer exits the UI element.
    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.HideBuildingTooltip();
    }

    #endregion
}
