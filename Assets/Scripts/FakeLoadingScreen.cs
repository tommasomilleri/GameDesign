using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PixeLadder.EasyTransition;

public class FakeLoadingScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider loadingBar;
    public TextMeshProUGUI tipText;
    public TextMeshProUGUI promptText;
    public RectTransform loadingIcon;

    [Header("Loading Settings")]
    [Range(1f, 10f)] public float loadingDuration = 4f;

    [Header("Cheese Tips")]
    [TextArea(2, 4)]
    public string[] cheeseTips;

    [Header("Level Transition")]
    [Tooltip("Trascina qui il livello che deve aprirsi DOPO questo caricamento")]
    public GameObject nextLevelPanel;

    // --- VARIABILI INTERNE ---
    private int currentTipIndex = 0;
    private bool isLoaded = false;
    private bool isTransitioning = false;

    void OnEnable()
    {
        isLoaded = false;
        isTransitioning = false;

        if (loadingBar != null) loadingBar.value = 0f;
        if (promptText != null) promptText.text = "Click or press Space for the next tip...";
        ShuffleAndShowTip();

        StopAllCoroutines();
        StartCoroutine(FakeLoadRoutine());
    }

    void Update()
    {
        if (isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (!isLoaded) ShowNextTip();
            else TriggerExitTransition();
        }
    }

    IEnumerator FakeLoadRoutine()
    {
        float elapsed = 0f;
        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            if (loadingBar != null) loadingBar.value = Mathf.Clamp01(elapsed / loadingDuration);
            yield return null;
        }

        if (loadingBar != null) loadingBar.value = 1f;
        isLoaded = true;
        if (promptText != null) promptText.text = "Loading complete! Press to continue.";
    }

    void ShowNextTip()
    {
        if (cheeseTips == null || cheeseTips.Length == 0) return;
        currentTipIndex = (currentTipIndex + 1) % cheeseTips.Length;
        if (tipText != null) tipText.text = cheeseTips[currentTipIndex];
    }

    void ShuffleAndShowTip()
    {
        if (cheeseTips != null && cheeseTips.Length > 0)
        {
            currentTipIndex = UnityEngine.Random.Range(0, cheeseTips.Length);
            if (tipText != null) tipText.text = cheeseTips[currentTipIndex];
        }
    }

    void TriggerExitTransition()
    {
        isLoaded = false;
        isTransitioning = true; // Chiude l'input

        if (SimpleCellularTransition.Instance != null)
        {
            // FASE 2: Scambia le scene al buio...
            if (nextLevelPanel != null) nextLevelPanel.SetActive(true);
            this.gameObject.SetActive(false);

            // ...e rimpicciolisce le bolle svelando il nuovo livello!
            SimpleCellularTransition.Instance.PlayIn(null);
        }
        else if (GameManager.instance != null)
        {
            GameManager.instance.TransitionBetweenPanels(this.gameObject, nextLevelPanel);
        }
        else
        {
            if (nextLevelPanel != null) nextLevelPanel.SetActive(true);
            this.gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}