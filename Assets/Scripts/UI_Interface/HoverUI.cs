using UnityEngine;
using UnityEngine.EventSystems;

public class HoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Impostazioni Cursore")]
    public Texture2D customeCursor;

    [Tooltip("Il punto dell'immagine che clicca. Metti metà della risoluzione per centrarlo (es. 16, 16 per un'immagine 32x32)")]
    public Vector2 hotSpot = Vector2.zero;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Ora usa l'hotSpot personalizzabile invece del rigido Vector2.zero
        Cursor.SetCursor(customeCursor, hotSpot, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}