using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager instance;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
                if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

        public void ChangeBackground(Sprite newSprite)
    {
        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }
}