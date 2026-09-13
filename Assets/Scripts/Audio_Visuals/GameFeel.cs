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

        public void Shake(float amp = 12f, float dur = 0.15f)
    {
        StartCoroutine(ShakeRoutine(amp, dur));
    }

        public void HitStop(float dur = 0.08f)
    {
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
                        float currentAmp = Mathf.Lerp(amp, 0f, elapsed / dur);
            canvasRect.localPosition = originalPos + (Vector3)Random.insideUnitCircle * currentAmp;

                        elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasRect.localPosition = originalPos;
    }

    private IEnumerator HitStopRoutine(float dur)
    {
        isHitStopping = true;
        Time.timeScale = 0f;         yield return new WaitForSecondsRealtime(dur);

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