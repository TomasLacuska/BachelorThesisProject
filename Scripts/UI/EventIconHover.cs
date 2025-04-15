using UnityEngine;
using UnityEngine.EventSystems;

public class EventIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("ID of the milestone event this icon represents.")]
    public string milestoneEventID;

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.ShowEventTooltip(milestoneEventID);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.HideEventTooltip();
    }
}
