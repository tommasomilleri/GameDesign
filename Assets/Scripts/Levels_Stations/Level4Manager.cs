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

    [Header("3D Models to Animate (Usa i Transform!)")]
    public Transform pressModel;
    public Transform flipModel;
    public Transform drainModel;
    [Header("Visual Progress (I Timbri di Cera UI)")]
    public GameObject waxContainer; 
    public Image[] progressLights;
    public Sprite waxEmptySprite;
    public Sprite[] waxStampedSprites;

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip errorSound;

    private bool isAnimating = false;
    private bool levelCompleted = false;
    void OnEnable()
    {
        levelCompleted = false;   
        
        if (waxContainer != null) waxContainer.SetActive(true);

        UpdateLights(true);
    }
    void LateUpdate()
    {
        bool levelActive = (CurrentLevel != null) ? CurrentLevel.activeInHierarchy : false;
        bool show = levelActive && !levelCompleted;

        if (GameManager.instance != null && GameManager.instance.IsChangingLevel)
            show = false;

        if (waxContainer != null && waxContainer.activeSelf != show)
        {
            waxContainer.SetActive(show);
        }
    }
    public void ClickProcess(string process)
    {
        
        if (CurrentLevel != null && !CurrentLevel.activeInHierarchy)
        {
            CurrentLevel.SetActive(true);
        }
        if (levelCompleted) return;
        if (isAnimating) return;

        Transform clickedModel = null;
        if (process == "press") clickedModel = pressModel;
        else if (process == "flip") clickedModel = flipModel;
        else if (process == "drain") clickedModel = drainModel;

        // --- AZIONE CORRETTA ---
        if (process == correctSequence[currentStep])
        {
            Debug.Log("Correct: " + process);
            
            if (currentStep < progressLights.Length)
            {
                StartCoroutine(StampWaxAnimation(progressLights[currentStep]));
            }

            currentStep++;

            
            if (audioSource != null && successSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(successSound);
            }
            if (process == "press") StartCoroutine(SquishAnimation(clickedModel));
            else if (process == "flip") StartCoroutine(RotateAnimation(clickedModel));
            else if (process == "drain") StartCoroutine(VibrateAnimation(clickedModel));

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

            if (GameManager.instance != null)
            {
                GameManager.instance.DecreaseGlobalQuality(wrongActionPenalty);
            }
        }
    }

    public void ResetSequence()
    {
        currentStep = 0;
        UpdateLights(true);
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
    IEnumerator StampWaxAnimation(Image waxImage)
    {
        if (waxImage == null) yield break;

        RectTransform rt = waxImage.rectTransform;
        Vector3 originalScale = Vector3.one;

        float durationDown = 0.1f;
        float elapsed = 0f;
        while (elapsed < durationDown)
        {
            rt.localScale = Vector3.Lerp(originalScale, new Vector3(1.1f, 0.7f, 1f), elapsed / durationDown);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (waxStampedSprites != null && waxStampedSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, waxStampedSprites.Length);
            waxImage.sprite = waxStampedSprites[randomIndex];
        }

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
    IEnumerator SquishAnimation(Transform target)
    {
        if (target == null) yield break;
        isAnimating = true;
        Vector3 originalScale = target.localScale;

        target.localScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.7f, originalScale.z);
        yield return new WaitForSeconds(0.15f);

        target.localScale = originalScale;
        isAnimating = false;
    }

    IEnumerator RotateAnimation(Transform target)
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

    IEnumerator VibrateAnimation(Transform target)
    {
        if (target == null) yield break;
        isAnimating = true;

        Vector3 originalPos = target.localPosition;
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            
            target.localPosition = originalPos + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
        isAnimating = false;
    }

    IEnumerator GlobalResetAnimation()
    {
        isAnimating = true;
        
        MeshRenderer pressRend = pressModel != null ? pressModel.GetComponentInChildren<MeshRenderer>() : null;
        MeshRenderer flipRend = flipModel != null ? flipModel.GetComponentInChildren<MeshRenderer>() : null;
        MeshRenderer drainRend = drainModel != null ? drainModel.GetComponentInChildren<MeshRenderer>() : null;
        
        Color origPress = pressRend != null ? pressRend.material.GetColor("_BaseColor") : Color.white;
        Color origFlip = flipRend != null ? flipRend.material.GetColor("_BaseColor") : Color.white;
        Color origDrain = drainRend != null ? drainRend.material.GetColor("_BaseColor") : Color.white;

        
        Color errorColor = new Color(1f, 0.3f, 0.3f);
        if (pressRend != null) pressRend.material.SetColor("_BaseColor", errorColor);
        if (flipRend != null) flipRend.material.SetColor("_BaseColor", errorColor);
        if (drainRend != null) drainRend.material.SetColor("_BaseColor", errorColor);

        
        Vector3 pPos = pressModel != null ? pressModel.localPosition : Vector3.zero;
        Vector3 fPos = flipModel != null ? flipModel.localPosition : Vector3.zero;
        Vector3 dPos = drainModel != null ? drainModel.localPosition : Vector3.zero;

        
        float shakeAmount = 0.2f;
        for (int i = 0; i < 4; i++)
        {
            float dir = (i % 2 == 0) ? 1f : -1f;
            Vector3 offset = new Vector3(shakeAmount * dir, 0, 0);

            if (pressModel != null) pressModel.localPosition = pPos + offset;
            if (flipModel != null) flipModel.localPosition = fPos + offset;
            if (drainModel != null) drainModel.localPosition = dPos + offset;

            yield return new WaitForSeconds(0.06f);
        }

        
        if (pressModel != null) pressModel.localPosition = pPos;
        if (flipModel != null) flipModel.localPosition = fPos;
        if (drainModel != null) drainModel.localPosition = dPos;

        if (pressRend != null) pressRend.material.SetColor("_BaseColor", origPress);
        if (flipRend != null) flipRend.material.SetColor("_BaseColor", origFlip);
        if (drainRend != null) drainRend.material.SetColor("_BaseColor", origDrain);

        isAnimating = false;
    }

    IEnumerator CompleteLevelRoutine()
    {
        isAnimating = true;
        levelCompleted = true;
        Debug.Log("LEVEL 4 COMPLETE!");

        
        yield return new WaitForSecondsRealtime(0.5f);

        GoToNextLevel();
    }
    void GoToNextLevel()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        
        if (GameManager.instance != null)
        {
            GameManager.instance.TransitionToNextLevel(CurrentLevel, NextLevel);
        }
        else
        {
            if (NextLevel != null) NextLevel.SetActive(true);
            if (CurrentLevel != null) CurrentLevel.SetActive(false);
        }
    }
    void OnDisable()
    {
        if (waxContainer != null) waxContainer.SetActive(false);
        StopAllCoroutines();
    }
}