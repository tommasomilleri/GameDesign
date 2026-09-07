using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PostFX : MonoBehaviour
{
    public static PostFX instance;

    [Header("Atmosphere Settings")]
    [Range(0f, 1f)] public float vignetteIntensity = 0.65f;
    [Range(0f, 0.1f)] public float noiseIntensity = 0.04f;
    public float noiseSpeed = 12f;

    private Image vignetteImage;
    private Image noiseImage;
    private Image tintImage;

    private Texture2D noiseTex;

    void Awake()
    {
        // Singleton pattern
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>();

        // 1. LIVELLO COLORE (Per colorare la stanza a seconda del livello)
        tintImage = CreateOverlayImage("PostFX_Tint", rootCanvas.transform);
        tintImage.color = new Color(0, 0, 0, 0);

        // 2. LIVELLO VIGNETTA (Angoli bui generati matematicamente)
        vignetteImage = CreateOverlayImage("PostFX_Vignette", rootCanvas.transform);
        Texture2D vigTex = GenerateVignette();
        vignetteImage.sprite = Sprite.Create(vigTex, new Rect(0, 0, 256, 256), Vector2.zero);
        vignetteImage.color = new Color(0, 0, 0, vignetteIntensity);

        // 3. LIVELLO GRANA/PULVISCOLO (Vecchia pellicola)
        noiseImage = CreateOverlayImage("PostFX_Noise", rootCanvas.transform);
        noiseTex = GenerateNoise();
        noiseImage.sprite = Sprite.Create(noiseTex, new Rect(0, 0, 128, 128), Vector2.zero);
        noiseImage.color = new Color(1, 1, 1, noiseIntensity);

        StartCoroutine(AnimateNoise());
        if (GameManager.instance != null && GameManager.instance.qualityBarContainer != null)
            GameManager.instance.qualityBarContainer.transform.SetAsLastSibling();
    }

    Image CreateOverlayImage(string objName, Transform parent)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling(); // Mette la grafica in primissimo piano

        Image img = go.AddComponent<Image>();
        img.raycastTarget = false; // CRITICO: Non blocca i click del giocatore!

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return img;
    }

    Texture2D GenerateVignette()
    {
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                Vector2 uv = new Vector2(x / 255f, y / 255f);
                float dist = Vector2.Distance(uv, new Vector2(0.5f, 0.5f));
                float alpha = Mathf.SmoothStep(0.4f, 0.8f, dist);
                tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    Texture2D GenerateNoise()
    {
        Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float val = Random.value;
                tex.SetPixel(x, y, new Color(val, val, val, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    IEnumerator AnimateNoise()
    {
        RectTransform rt = noiseImage.GetComponent<RectTransform>();
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f / noiseSpeed);
        while (true)
        {
            // Fa tremare la grana costantemente
            rt.anchoredPosition = new Vector2(Random.Range(-4f, 4f), Random.Range(-4f, 4f));
            rt.localScale = new Vector3(Random.value > 0.5f ? 1 : -1, Random.value > 0.5f ? 1 : -1, 1);
            yield return wait;
        }
    }

    // Questa funzione può essere chiamata da qualsiasi livello per cambiare il colore dell'aria!
    public void SetLevelTint(Color targetColor, float duration = 1.2f)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateNoise());
        StartCoroutine(LerpTint(targetColor, duration));
    }

    IEnumerator LerpTint(Color targetColor, float duration)
    {
        Color startColor = tintImage.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            tintImage.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }
        tintImage.color = targetColor;
    }
}