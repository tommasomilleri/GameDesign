using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableIngredient : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    public string ingredientName;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Store the starting position to snap back if dropped in the wrong place
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Disable raycasts so the mouse can "see" the pot (drop zone) underneath the ingredient
        canvasGroup.blocksRaycasts = false;

        // Moves this object to the bottom of the Hierarchy list.
        // This ensures the ingredient renders on top of the pot and all other UI elements!
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the ingredient, adjusting for the Canvas scale factor to prevent mouse drift
        rectTransform.anchoredPosition +=
            eventData.delta / GetComponentInParent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Re-enable raycasts so the item can be clicked/dragged again if needed
        canvasGroup.blocksRaycasts = true;

        // Return to original position unless the LevelManager deactivated it (meaning it was a correct match)
        if (gameObject.activeSelf)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}