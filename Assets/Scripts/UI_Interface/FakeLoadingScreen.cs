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

    [Header("Auto Continue")]
    [Tooltip("Se il giocatore non clicca, passa comunque dopo N secondi. 0 = disattivato")]
    public float autoContinueAfter = 8f;

    [Header("Cheese Tips")]
    [TextArea(2, 4)]
    public string[] cheeseTips;

    [Header("Level Transition")]
    [Tooltip("Assegnato a runtime dal GameManager. Lascialo vuoto.")]
    public GameObject nextLevelPanel;

    private int currentTipIndex = 0;
    private bool isLoaded = false;
    private bool isTransitioning = false;
    private float armTime;

    public void BeginLoading(GameObject nextPanel)
    {
        if (nextPanel != null) nextLevelPanel = nextPanel;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else
        {
            ResetAndRun();
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("[FakeLoadingScreen] Il padre di " + gameObject.name +
                           " e' spento: la schermata non sara' mai visibile.", gameObject);
            ForceGoToNextLevel();
        }
    }

    void OnEnable()
    {
        ResetAndRun();
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    private void LockLoadingBar()
    {
        if (loadingBar == null) return;

        loadingBar.interactable = false;
        loadingBar.transition = Selectable.Transition.None;
        loadingBar.navigation = new Navigation { mode = Navigation.Mode.None };

        Graphic[] graphics = loadingBar.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        if (loadingBar.handleRect != null)
            loadingBar.handleRect.gameObject.SetActive(false);
    }

    private void ResetAndRun()
    {
        isLoaded = false;
        isTransitioning = false;
        armTime = Time.unscaledTime + 0.35f;

        LockLoadingBar();

        if (loadingBar != null) loadingBar.value = 0f;
        if (promptText != null) promptText.text = "Click or press Space for the next tip...";
        ShuffleAndShowTip();

        StopAllCoroutines();
        StartCoroutine(FakeLoadRoutine());
    }

    void Update()
    {
        if (isTransitioning) return;
        if (Time.unscaledTime < armTime) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0))
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
            elapsed += Time.unscaledDeltaTime;
            if (loadingBar != null) loadingBar.value = Mathf.Clamp01(elapsed / loadingDuration);
            yield return null;
        }

        if (loadingBar != null) loadingBar.value = 1f;
        isLoaded = true;
        if (promptText != null) promptText.text = "Loading complete! Press to continue.";

        if (autoContinueAfter > 0f)
        {
            float waited = 0f;
            while (waited < autoContinueAfter)
            {
                if (isTransitioning) yield break;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!isTransitioning)
            {
                Debug.LogWarning("[FakeLoadingScreen] Nessun input ricevuto: avanzo automaticamente.");
                TriggerExitTransition();
            }
        }
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
        if (isTransitioning) return;

        isTransitioning = true;
        isLoaded = false;
        StopAllCoroutines();

        if (nextLevelPanel == null)
        {
            Debug.LogError("[FakeLoadingScreen] nextLevelPanel NULL: impossibile avanzare!", this);
            if (SimpleCellularTransition.Instance != null)
                SimpleCellularTransition.Instance.PlayIn(null);
            if (GameManager.instance != null)
                GameManager.instance.NotifyTransitionFinished();
            isTransitioning = false;
            return;
        }

        GameObject target = nextLevelPanel;

        gameObject.SetActive(false);

        if (GameManager.instance != null)
        {
            // Accensione rimandata di 1 frame: il click non viene riletto dal pannello
            GameManager.instance.ActivatePanelNextFrame(target);
        }
        else
        {
            target.SetActive(true);
            if (SimpleCellularTransition.Instance != null)
                SimpleCellularTransition.Instance.PlayIn(null);
        }
    }

    private void ForceGoToNextLevel()
    {
        if (nextLevelPanel != null) nextLevelPanel.SetActive(true);
        gameObject.SetActive(false);
        if (GameManager.instance != null) GameManager.instance.NotifyTransitionFinished();
    }
}