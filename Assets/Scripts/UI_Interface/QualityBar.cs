using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QualityBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Trascina qui l'oggetto 'Fill' dalla Hierarchy")]
    public Image moldFillImage;
    private int maxQuality = 100;
    private float displayedQuality = 100f;

    public void SetMaxQuality(int quality)
    {
        maxQuality = quality;
        displayedQuality = quality;
        UpdateMoldVisual(quality, true);
    }

    public void SetQuality(int quality)
    {
        /*
        if (!gameObject.activeInHierarchy)
        {
            if (moldFillImage != null) moldFillImage.fillAmount = 1f - (float)quality / maxQuality;   // <-- usa il TUO campo (slider/fillImage)
            return;
        }*/

        StopAllCoroutines();
        StartCoroutine(AnimateMoldVisual(quality));
    }

    private IEnumerator AnimateMoldVisual(int targetQuality)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        float startQuality = displayedQuality;
        Vector3 originalScale = transform.localScale;
        StartCoroutine(WobbleRoutine(originalScale));

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            displayedQuality = Mathf.Lerp(startQuality, targetQuality, t);

            UpdateMoldVisual(displayedQuality, false);
            yield return null;
        }

        displayedQuality = targetQuality;
        UpdateMoldVisual(displayedQuality, false);
    }

    private IEnumerator WobbleRoutine(Vector3 originalScale)
    {
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = 1f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.06f;
            transform.localScale = originalScale * scale;
            yield return null;
        }
        transform.localScale = originalScale;
    }
    private void UpdateMoldVisual(float currentQuality, bool instant)
    {
        if (moldFillImage != null && maxQuality > 0)
        {
            float healthPercent = currentQuality / maxQuality;
            float moldPercent = 1f - healthPercent;
            moldFillImage.fillAmount = moldPercent;
            if (healthPercent < 0.4f)
            {
                float pulse = 0.8f + Mathf.Sin(Time.unscaledTime * 5f) * 0.2f;
                moldFillImage.color = new Color(0.55f, 0.85f, 0.4f, pulse);
            }
            else if (healthPercent < 0.8f)
            {
                moldFillImage.color = new Color(0.7f, 0.9f, 0.55f, 1f);
            }
            else
            {
                moldFillImage.color = Color.white;
            }
        }
    }

    void Update()
    {
        if (moldFillImage != null && displayedQuality / maxQuality < 0.4f)
        {
            UpdateMoldVisual(displayedQuality, false);
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        transform.localScale = Vector3.one;
    }
}