using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class SimpleCellularTransition : MonoBehaviour
{
    public static SimpleCellularTransition Instance;

    [Header("Settings")]
    public float duration = 0.8f;
    public Color circleColor = Color.black;
    [Tooltip("Sprite di un cerchio pieno. Usa Knob (built-in) se non ne hai uno.")]
    public Sprite circleSprite;

    [Tooltip("Quante bolle nere devono apparire sullo schermo?")]
    public int numberOfCircles = 25;

    private Canvas canvas;
    private List<RectTransform> circles = new List<RectTransform>();
    private bool busy;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        BuildCanvas();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildCanvas()
    {
        GameObject go = new GameObject("SimpleCellularCanvas");
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        go.AddComponent<GraphicRaycaster>();

        // Crea le bolle sparse casualmente per lo schermo
        for (int i = 0; i < numberOfCircles; i++)
        {
            GameObject imgGO = new GameObject("Bubble_" + i);
            imgGO.transform.SetParent(go.transform, false);
            Image circle = imgGO.AddComponent<Image>();
            circle.color = circleColor;
            circle.sprite = circleSprite;
            circle.raycastTarget = true; // Blocca i click

            RectTransform rt = circle.rectTransform;

            // Posiziona il centro del cerchio in un punto a caso dello schermo
            float randX = UnityEngine.Random.Range(0f, 1f);
            float randY = UnityEngine.Random.Range(0f, 1f);

            rt.anchorMin = new Vector2(randX, randY);
            rt.anchorMax = new Vector2(randX, randY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.localScale = Vector3.zero; // Partono invisibili

            circles.Add(rt);
        }

        go.SetActive(false);
    }
    public void PlayOut(Action onDone)
    {
        if (!busy) StartCoroutine(OutRoutine(onDone));
    }

    public void PlayIn(Action onDone)
    {
        if (!busy) StartCoroutine(InRoutine(onDone));
    }

    IEnumerator OutRoutine(Action onDone)
    {
        busy = true;
        canvas.gameObject.SetActive(true);

        float maxDim = Mathf.Max(Screen.width, Screen.height);
        float targetScale = (maxDim / 100f) * 3.5f;

        // Le bolle crescono
        float t = 0f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            float s = Mathf.Lerp(0f, targetScale, k);
            foreach (var rt in circles) rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        foreach (var rt in circles) rt.localScale = new Vector3(targetScale, targetScale, 1f);

        // Invoca l'azione e SI FERMA (lasciando lo schermo coperto di bolle)
        if (onDone != null) onDone();
        busy = false;
    }

    IEnumerator InRoutine(Action onDone)
    {
        busy = true;
        float maxDim = Mathf.Max(Screen.width, Screen.height);
        float targetScale = (maxDim / 100f) * 3.5f;

        // Le bolle si rimpiccioliscono
        float t = 0f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            float s = Mathf.Lerp(targetScale, 0f, k);
            foreach (var rt in circles) rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        foreach (var rt in circles) rt.localScale = Vector3.zero;

        // Spegne il nero e sblocca
        canvas.gameObject.SetActive(false);
        if (onDone != null) onDone();
        busy = false;
    }
}