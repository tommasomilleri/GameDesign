using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("Physical Press Effect")]
    [Tooltip("Quanti pixel il bottone va in giù quando premuto (es. -8)")]
    public float pushDownOffset = -8f;
    [Tooltip("Quanto si restringe (1 = normale, 0.95 = leggermente più piccolo)")]
    public float pressScale = 0.95f;

    [Header("Hover Rotation Effect (Opzionale)")]
    [Tooltip("L'icona da far ruotare (es. l'ingranaggio). Lascia vuoto se non c'è nulla da ruotare.")]
    public Transform iconToRotate;
    [Tooltip("Di quanti gradi deve ruotare quando ci passi sopra col mouse")]
    public float hoverRotationAngle = 45f;
    [Tooltip("Velocità dell'animazione di rotazione")]
    public float rotationSpeed = 10f;

    [Header("VFX Settings")]
    public GameObject clickVFXPrefab;

    // Variabili di memoria
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private RectTransform rectTransform;
    private Quaternion originalIconRotation;
    private bool isHovering = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Salva le posizioni iniziali
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            originalScale = transform.localScale;
        }

        if (iconToRotate != null)
        {
            originalIconRotation = iconToRotate.localRotation;
        }
    }

    void Update()
    {
        // Gestisce la rotazione fluida se è stata assegnata un'icona
        if (iconToRotate != null)
        {
            Quaternion targetRotation = isHovering
                ? originalIconRotation * Quaternion.Euler(0, 0, hoverRotationAngle)
                : originalIconRotation;

            // Early-out: ruota solo se c'è una differenza visibile (risparmia performance!)
            if (Quaternion.Angle(iconToRotate.localRotation, targetRotation) > 0.1f)
            {
                iconToRotate.localRotation = Quaternion.Lerp(
                    iconToRotate.localRotation, targetRotation,
                    Time.unscaledDeltaTime * rotationSpeed);
            }
        }
    }

    // 1. Mouse entra
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        PlaySoundWithRandomPitch(hoverSound);
    }

    // 2. Mouse esce
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        // Se il giocatore preme ma sposta il mouse fuori dal bottone, resetta la posizione fisica per sicurezza
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
            transform.localScale = originalScale;
        }
    }

    // 3. Click premuto (scende fisicamente)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(originalPosition.x, originalPosition.y + pushDownOffset);
            transform.localScale = originalScale * pressScale;
        }
    }

    // 4. Click rilasciato (torna su)
    public void OnPointerUp(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
            transform.localScale = originalScale;
        }
    }

    // 5. Click completo registrato
    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySoundWithRandomPitch(clickSound);
        SpawnVFX();
    }

    private void PlaySoundWithRandomPitch(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip);
        }
    }

    private void SpawnVFX()
    {
        if (clickVFXPrefab != null)
        {
            GameObject spawnedVFX = Instantiate(clickVFXPrefab, transform.position, Quaternion.identity);
            spawnedVFX.transform.SetParent(transform.parent, true);
            Destroy(spawnedVFX, 2f);
        }
    }
}