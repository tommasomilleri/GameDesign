using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager instance;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Singleton per renderlo rintracciabile da chiunque
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Funzione pubblica per cambiare l'immagine
    public void ChangeBackground(Sprite newSprite)
    {
        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }
}