using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Level4Manager : MonoBehaviour
{

    [Header("Co-op Sequence")]
    [SerializeField] private string[] correctSequence = { "drain", "press", "flip", "press", "flip" };
    private int currentStep = 0;

    [Header("Penalty Settings")]
    public int wrongActionPenalty = 20;

    [Header("Level Transitions")]
    public GameObject NextLevel;
    public GameObject CurrentLevel;

    [Header("Graphics to Animate")]
    public RectTransform pressGraphic;
    public RectTransform flipGraphic;
    public RectTransform drainGraphic;

    [Header("Visual Progress (I Timbri di Cera)")]
    [Tooltip("Inserisci qui le 5 immagini della UI (i quadratini da sostituire)")]
    public Image[] progressLights;

    [Tooltip("L'immagine della cera VUOTA (Es. WaxEmpty)")]
    public Sprite waxEmptySprite;

    // VECCHIO: public Sprite waxStampedSprite;
    // NUOVO: Un array per contenere tutti i tuoi timbri!
    [Tooltip("Trascina qui tutti i tuoi WaxFoot (1, 2, 3...) per pescarli a caso!")]
    public Sprite[] waxStampedSprites;

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip errorSound;

    private bool isAnimating = false;

    void Start()
    {
        UpdateLights(true);
    }

    public void ClickProcess(string process)
    {
        if (isAnimating) return;

        RectTransform clickedGraphic = null;
        if (process == "press") clickedGraphic = pressGraphic;
        else if (process == "flip") clickedGraphic = flipGraphic;
        else if (process == "drain") clickedGraphic = drainGraphic;

        // --- AZIONE CORRETTA ---
        if (process == correctSequence[currentStep])
        {
            Debug.Log("Correct: " + process);

            // 1. Avvia l'animazione fisica del timbro!
            if (currentStep < progressLights.Length)
            {
                StartCoroutine(StampWaxAnimation(progressLights[currentStep]));
            }

            currentStep++;

            // Suono di successo con pitch randomico[cite: 3]
            if (audioSource != null && successSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(successSound);
            }

            if (process == "press") StartCoroutine(SquishAnimation(clickedGraphic));
            else if (process == "flip") StartCoroutine(RotateAnimation(clickedGraphic));
            else if (process == "drain") StartCoroutine(VibrateAnimation(clickedGraphic));

            if (currentStep == correctSequence.Length)
            {
                StartCoroutine(CompleteLevelRoutine());
            }
        }
        // --- AZIONE SBAGLIATA ---
        else
        {
            Debug.Log("Wrong! RESETing sequence.");
            ResetSequence();

            if (audioSource != null && errorSound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(errorSound);
            }

            StartCoroutine(GlobalResetAnimation());

            // Penalità globale
            if (GameManager.instance != null)
            {
                GameManager.instance.DecreaseGlobalQuality(wrongActionPenalty);
            }
        }
    }

    public void ResetSequence()
    {
        currentStep = 0;
        UpdateLights(true); // Resetta visivamente tutte le cere istantaneamente
        Debug.Log("Sequence reset. Try again!");
    }

    void UpdateLights(bool resetToEmpty = false)
    {
        for (int i = 0; i < progressLights.Length; i++)
        {
            if (progressLights[i] != null && waxEmptySprite != null)
            {
                if (resetToEmpty)
                {
                    progressLights[i].sprite = waxEmptySprite;
                    progressLights[i].color = Color.white;
                    progressLights[i].rectTransform.localScale = Vector3.one;
                }
            }
        }
    }

    // ==========================================
    // ANIMAZIONE: IL TIMBRO RANDOM SULLA CERA
    // ==========================================
    IEnumerator StampWaxAnimation(Image waxImage)
    {
        if (waxImage == null) yield break;

        RectTransform rt = waxImage.rectTransform;
        Vector3 originalScale = Vector3.one;

        // 1. Il timbro scende (si schiaccia visivamente come per assorbire il colpo)
        float durationDown = 0.1f;
        float elapsed = 0f;
        while (elapsed < durationDown)
        {
            rt.localScale = Vector3.Lerp(originalScale, new Vector3(1.1f, 0.7f, 1f), elapsed / durationDown);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. MAGIA: Se hai inserito dei timbri nell'Array, ne pesca uno a caso!
        if (waxStampedSprites != null && waxStampedSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, waxStampedSprites.Length);
            waxImage.sprite = waxStampedSprites[randomIndex];
        }

        // 3. Rimbalzo elastico verso l'alto
        float durationUp = 0.15f;
        elapsed = 0f;
        while (elapsed < durationUp)
        {
            rt.localScale = Vector3.Lerp(new Vector3(1.1f, 0.7f, 1f), originalScale, elapsed / durationUp);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.localScale = originalScale;
    }

    // ==========================================
    // ANIMAZIONI STRUMENTI (Esistenti)
    // ==========================================

    IEnumerator SquishAnimation(RectTransform target)
    {
        if (target == null) yield break;
        isAnimating = true;
        Vector3 originalScale = target.localScale;

        target.localScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.7f, originalScale.z);
        yield return new WaitForSeconds(0.15f);

        target.localScale = originalScale;
        isAnimating = false;
    }

    IEnumerator RotateAnimation(RectTransform target)
    {
        if (target == null) yield break;
        isAnimating = true;

        float duration = 0.3f;
        float elapsed = 0f;
        Quaternion startRot = target.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, -180f);

        while (elapsed < duration)
        {
            target.localRotation = Quaternion.Lerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localRotation = endRot;
        isAnimating = false;
    }

    IEnumerator VibrateAnimation(RectTransform target)
    {
        if (target == null) yield break;
        isAnimating = true;

        Vector3 originalPos = target.localPosition;
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            target.localPosition = originalPos + new Vector3(Random.Range(-6f, 6f), Random.Range(-4f, 4f), 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
        isAnimating = false;
    }

    // ==========================================
    // ANIMAZIONE DI ERRORE GLOBALE
    // ==========================================

    IEnumerator GlobalResetAnimation()
    {
        isAnimating = true;

        Image pressImg = pressGraphic != null ? pressGraphic.GetComponent<Image>() : null;
        Image flipImg = flipGraphic != null ? flipGraphic.GetComponent<Image>() : null;
        Image drainImg = drainGraphic != null ? drainGraphic.GetComponent<Image>() : null;

        Color origPress = pressImg != null ? pressImg.color : Color.white;
        Color origFlip = flipImg != null ? flipImg.color : Color.white;
        Color origDrain = drainImg != null ? drainImg.color : Color.white;

        Color errorColor = new Color(1f, 0.3f, 0.3f);
        if (pressImg != null) pressImg.color = errorColor;
        if (flipImg != null) flipImg.color = errorColor;
        if (drainImg != null) drainImg.color = errorColor;

        Vector3 pPos = pressGraphic != null ? pressGraphic.localPosition : Vector3.zero;
        Vector3 fPos = flipGraphic != null ? flipGraphic.localPosition : Vector3.zero;
        Vector3 dPos = drainGraphic != null ? drainGraphic.localPosition : Vector3.zero;

        float shakeAmount = 20f;
        for (int i = 0; i < 4; i++)
        {
            float dir = (i % 2 == 0) ? 1f : -1f;
            Vector3 offset = new Vector3(shakeAmount * dir, 0, 0);

            if (pressGraphic != null) pressGraphic.localPosition = pPos + offset;
            if (flipGraphic != null) flipGraphic.localPosition = fPos + offset;
            if (drainGraphic != null) drainGraphic.localPosition = dPos + offset;

            yield return new WaitForSeconds(0.06f);
        }

        if (pressGraphic != null) pressGraphic.localPosition = pPos;
        if (flipGraphic != null) flipGraphic.localPosition = fPos;
        if (drainGraphic != null) drainGraphic.localPosition = dPos;

        if (pressImg != null) pressImg.color = origPress;
        if (flipImg != null) flipImg.color = origFlip;
        if (drainImg != null) drainImg.color = origDrain;

        isAnimating = false;
    }

    // ==========================================
    // FINE LIVELLO
    // ==========================================

    IEnumerator CompleteLevelRoutine()
    {
        isAnimating = true;
        Debug.Log("LEVEL 4 COMPLETE!");

        yield return new WaitForSeconds(0.5f);

        GoToNextLevel();
    }

    void GoToNextLevel()
    {
        if (NextLevel != null) NextLevel.SetActive(true);
        if (CurrentLevel != null) CurrentLevel.SetActive(false);
    }
    void OnDisable()
    {
        StopAllCoroutines();
    }
}