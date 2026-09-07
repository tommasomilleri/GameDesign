using UnityEngine;

public class IdleBreath : MonoBehaviour
{
    [Header("Impostazioni Respiro")]
    public float amount = 0.015f;
    public float speed = 1.2f;

    private float phaseOffset;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        // Assegna a ogni mucca un ritmo leggermente sfasato per non farle sembrare cloni
        phaseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float scaleY = 1f + Mathf.Sin(Time.time * speed + phaseOffset) * amount;
        transform.localScale = new Vector3(originalScale.x, originalScale.y * scaleY, originalScale.z);
    }
}