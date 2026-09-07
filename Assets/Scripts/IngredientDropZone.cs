using UnityEngine;
using UnityEngine.EventSystems;

public class ngredientDropZone : MonoBehaviour, IDropHandler
{
    [Tooltip("Drag the GameObject holding the Level3Manager here")]
    public Level3Manager level3Manager;

    // This function triggers automatically when you release the mouse OVER this object
    public void OnDrop(PointerEventData eventData)
    {
        // 1. Check if we actually dropped something
        if (eventData.pointerDrag != null)
        {
            // 2. Try to get the DraggableIngredient component from the dropped object
            DraggableIngredient ingredient = eventData.pointerDrag.GetComponent<DraggableIngredient>();

            // 3. If it IS an ingredient, send it to the Manager
            if (ingredient != null)
            {
                if (level3Manager != null)
                {
                    level3Manager.CheckIngredient(ingredient);
                }
                else
                {
                    Debug.LogWarning("Level3Manager is missing! Drag it into the PotDropZone Inspector.");
                }
            }
        }
    }
}