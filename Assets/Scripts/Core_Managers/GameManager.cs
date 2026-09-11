using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Global Cheese Quality")]
    public int maxQuality = 100;
    public int currentQuality;

    [Header("Statistiche Partita")]
    public int[] errorsPerLevel = new int[6];
    public int currentLevel = 1;
    public float runStartTime;

    [Header("3D Stations")]
    public int unlockedStation = 0;

    [Tooltip("Drag the QualityBar UI object here just ONCE!")]
    public QualityBar globalQualityBar;
    public GameObject qualityBarContainer;

    [Header("Story Endings Settings")]
    public GameObject endingPanel;

    [Header("Easy Transition")]
    public PixeLadder.EasyTransition.TransitionEffect levelTransitionEffect;

    [Header("Transition Settings")]
    public UnityEvent onPlayTransition;
    public float transitionDelay = 1.5f;

    [Header("Loading Screen Universale")]
    public FakeLoadingScreen globalLoadingScreen;

    [Header("Developer Cheat (Skip Level)")]
    [Tooltip("Trascina qui tutti i GoST in ordine (GoST1, GoST2, GoST3...)")]
    public GameObject[] debugGoSTSequence;

    private bool isChangingLevel = false;
    public bool IsChangingLevel { get { return isChangingLevel; } }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        currentQuality = maxQuality;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        if (globalQualityBar != null)
        {
            globalQualityBar.SetMaxQuality(maxQuality);
            globalQualityBar.SetQuality(currentQuality);
            globalQualityBar.gameObject.SetActive(false);
        }
    }

    public void DecreaseGlobalQuality(int damage)
    {
        currentQuality -= damage;
        if (currentLevel >= 1 && currentLevel <= 5)
        {
            errorsPerLevel[currentLevel]++;
        }

        if (GameFeel.Instance != null)
        {
            GameFeel.Instance.Shake();
            GameFeel.Instance.HitStop();
        }

        if (currentQuality <= 0)
        {
            currentQuality = 0;
            TriggerEnding();

            if (globalQualityBar != null)
                globalQualityBar.gameObject.SetActive(false);
        }
        else
        {
            if (globalQualityBar != null)
                globalQualityBar.SetQuality(currentQuality);
        }
    }

    public void TriggerEnding()
    {
        if (endingPanel != null) endingPanel.SetActive(true);
        if (qualityBarContainer != null) qualityBarContainer.SetActive(false);
    }

    public void ResetQuality()
    {
        currentQuality = maxQuality;
        runStartTime = Time.time;
        errorsPerLevel = new int[6];
        currentLevel = 1;
        unlockedStation = 0;
        isChangingLevel = false;

        if (globalQualityBar != null)
        {
            globalQualityBar.SetMaxQuality(maxQuality);
            globalQualityBar.SetQuality(currentQuality);
            globalQualityBar.gameObject.SetActive(false);
        }
        if (endingPanel != null) endingPanel.SetActive(false);
    }

    public void NotifyTransitionFinished()
    {
        isChangingLevel = false;
    }

    public void TransitionBetweenPanels(GameObject currentPanel, GameObject nextPanel)
    {
        if (onPlayTransition != null && onPlayTransition.GetPersistentEventCount() > 0)
        {
            onPlayTransition.Invoke();
            StartCoroutine(SwapAfterDelay(currentPanel, nextPanel, transitionDelay));
        }
        else
        {
            if (nextPanel != null) nextPanel.SetActive(true);
            if (currentPanel != null) currentPanel.SetActive(false);
        }
    }

    public void TransitionToNextLevel(GameObject currentLevel, GameObject targetLevel)
    {
        if (isChangingLevel)
        {
            Debug.LogWarning("[GameManager] TransitionToNextLevel ignorata: gia' in corso.");
            return;
        }
        if (targetLevel == null)
        {
            Debug.LogError("[GameManager] targetLevel NULL! Assegna NextLevel nell'Inspector.");
            return;
        }
        if (targetLevel == currentLevel)
        {
            Debug.LogError("[GameManager] NextLevel == CurrentLevel! Controlla l'Inspector.");
            return;
        }

        isChangingLevel = true;
        unlockedStation++;

        StartLoadingSequence(currentLevel, targetLevel);
    }

    public void TransitionIntoFirstLevel(GameObject currentPanel, GameObject firstLevel)
    {
        if (isChangingLevel)
        {
            Debug.LogWarning("[GameManager] TransitionIntoFirstLevel ignorata: gia' in corso.");
            return;
        }
        if (firstLevel == null)
        {
            Debug.LogError("[GameManager] firstLevel NULL!");
            return;
        }

        isChangingLevel = true;
        unlockedStation = 0;

        StartLoadingSequence(currentPanel, firstLevel);
    }

    private void StartLoadingSequence(GameObject currentPanel, GameObject targetPanel)
    {
        if (SimpleCellularTransition.Instance != null && globalLoadingScreen != null)
        {
            SimpleCellularTransition.Instance.PlayOut(() =>
            {
                if (currentPanel != null) currentPanel.SetActive(false);
                globalLoadingScreen.BeginLoading(targetPanel);
            });
            return;
        }

        if (globalLoadingScreen != null)
        {
            if (currentPanel != null) currentPanel.SetActive(false);
            globalLoadingScreen.BeginLoading(targetPanel);
            return;
        }

        Debug.LogWarning("[GameManager] LoadingScreen mancante: passaggio diretto al livello.");
        targetPanel.SetActive(true);
        if (currentPanel != null) currentPanel.SetActive(false);
        isChangingLevel = false;
    }

    // Accende il pannello al frame SUCCESSIVO: evita che il click che ha chiuso
    // la loading screen venga riletto dall'Update del pannello appena acceso.
    public void ActivatePanelNextFrame(GameObject panel)
    {
        StartCoroutine(ActivatePanelRoutine(panel));
    }

    private IEnumerator ActivatePanelRoutine(GameObject panel)
    {
        yield return null;

        if (panel != null)
        {
            panel.SetActive(true);

            if (!panel.activeInHierarchy)
                Debug.LogError("[GameManager] " + panel.name + ": il padre e' spento!", panel);
        }

        if (SimpleCellularTransition.Instance != null)
            SimpleCellularTransition.Instance.PlayIn(null);

        // Sblocco GARANTITO: non dipende dalla callback di PlayIn, che non viene
        // invocata se la transizione precedente era ancora in corso ('busy').
        yield return new WaitForSecondsRealtime(1.2f);
        isChangingLevel = false;
    }

    private IEnumerator SwapAfterDelay(GameObject current, GameObject next, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (next != null) next.SetActive(true);
        if (current != null) current.SetActive(false);
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F12) && !isChangingLevel)
        {
            DevSkipLevel();
        }
#endif
    }

    void DevSkipLevel()
    {
        if (debugGoSTSequence == null || debugGoSTSequence.Length == 0) return;

        for (int i = 0; i < debugGoSTSequence.Length - 1; i++)
        {
            if (debugGoSTSequence[i] != null && debugGoSTSequence[i].activeInHierarchy)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                TransitionToNextLevel(debugGoSTSequence[i], debugGoSTSequence[i + 1]);
                return;
            }
        }

        Debug.LogWarning("Nessun GoST da saltare o sei gia' all'ultimo livello!");
    }
}