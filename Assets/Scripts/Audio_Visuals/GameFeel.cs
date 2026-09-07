using UnityEngine;
using System.Collections;

public class GameFeel : MonoBehaviour
{
    public static GameFeel Instance;
    private bool isHitStopping = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Scuote il Canvas principale
    public void Shake(float amp = 12f, float dur = 0.15f)
    {
        StartCoroutine(ShakeRoutine(amp, dur));
    }

    // Congela il tempo per un istante (effetto impatto)
    public void HitStop(float dur = 0.08f)
    {
        // Se il gioco è già in pausa dal menu, non fare nulla
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused) return;
        if (!isHitStopping) StartCoroutine(HitStopRoutine(dur));
    }

    private IEnumerator ShakeRoutine(float amp, float dur)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        Vector3 originalPos = canvasRect.localPosition;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            // Diminuisce l'intensità col passare del tempo
            float currentAmp = Mathf.Lerp(amp, 0f, elapsed / dur);
            canvasRect.localPosition = originalPos + (Vector3)Random.insideUnitCircle * currentAmp;

            // Usa unscaledDeltaTime così trema anche durante l'HitStop!
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasRect.localPosition = originalPos;
    }

    private IEnumerator HitStopRoutine(float dur)
    {
        isHitStopping = true;
        Time.timeScale = 0f; // Ferma il tempo
        yield return new WaitForSecondsRealtime(dur);

        // Se il giocatore non ha aperto il menu di pausa nel frattempo, ripristina il tempo
        bool menuOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused;
        if (!menuOpen) Time.timeScale = 1f;
        isHitStopping = false;
    }
    void OnDisable()
    {
        bool menuOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused;
        if (isHitStopping && !menuOpen) Time.timeScale = 1f;
        isHitStopping = false;
    }
}