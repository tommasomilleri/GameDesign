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

    [Tooltip("Drag the QualityBar UI object here just ONCE!")]
    public QualityBar globalQualityBar;
    public GameObject qualityBarContainer;

    [Header("Story Endings Settings")]
    public GameObject endingPanel;

    // (La variabile levelTransitionEffect non serve più, ma la lascio per non rompere l'Inspector)
    [Header("Easy Transition")]
    public PixeLadder.EasyTransition.TransitionEffect levelTransitionEffect;

    [Header("Transition Settings (NOVITA')")]
    public UnityEvent onPlayTransition;
    public float transitionDelay = 1.5f;

    [Header("Loading Screen Universale")]
    public FakeLoadingScreen globalLoadingScreen;

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
            Debug.Log("Quality hit zero! Instant Bad Ending!");
            TriggerEnding();

            if (globalQualityBar != null)
            {
                globalQualityBar.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("Global Quality is now: " + currentQuality);
            if (globalQualityBar != null)
            {
                globalQualityBar.SetQuality(currentQuality);
            }
        }
    }

    public void TriggerEnding()
    {
        Debug.Log("Triggering Ending Panel. Final Quality: " + currentQuality);

        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
        }

        if (qualityBarContainer != null)
        {
            qualityBarContainer.SetActive(false);
        }
    }

    public void ResetQuality()
    {
        currentQuality = maxQuality;
        runStartTime = Time.time;
        errorsPerLevel = new int[6];
        if (globalQualityBar != null)
        {
            globalQualityBar.SetMaxQuality(maxQuality);
            globalQualityBar.SetQuality(currentQuality);
            globalQualityBar.gameObject.SetActive(false);
        }
        if (endingPanel != null) endingPanel.SetActive(false);
    }

    // --- LA MAGIA DELLE TRANSIZIONI ---
    public void TransitionBetweenPanels(GameObject currentPanel, GameObject nextPanel)
    {
        if (onPlayTransition != null && onPlayTransition.GetPersistentEventCount() > 0)
        {
            onPlayTransition.Invoke();
            StartCoroutine(SwapAfterDelay(currentPanel, nextPanel, transitionDelay));
        }
        else
        {
            if (nextPanel != null) nextPanel.SetActive(true); // <-- CORRETTO QUI! (Era nextLevel)
            if (currentPanel != null) currentPanel.SetActive(false);
        }
    }

    // --- IL NUOVO PONTE UNIVERSALE (AGGIORNATO AL CELLULAR SENZA SHADER) ---
    public void TransitionToNextLevel(GameObject currentLevel, GameObject targetLevel)
    {
        // Chiama il NOSTRO script infallibile invece di quello dell'asset
        if (SimpleCellularTransition.Instance != null && globalLoadingScreen != null)
        {
            globalLoadingScreen.nextLevelPanel = targetLevel;

            // FASE 1: Le bolle crescono per coprire lo schermo e ci restano!
            SimpleCellularTransition.Instance.PlayOut(() =>
            {
                globalLoadingScreen.gameObject.SetActive(true);
                if (currentLevel != null) currentLevel.SetActive(false);
            });
        }
        else
        {
            // Fallback: vecchio sistema a pannelli
            if (globalLoadingScreen != null)
            {
                globalLoadingScreen.nextLevelPanel = targetLevel;
                TransitionBetweenPanels(currentLevel, globalLoadingScreen.gameObject);
            }
            else
            {
                TransitionBetweenPanels(currentLevel, targetLevel);
            }
        }
    }

    private IEnumerator SwapAfterDelay(GameObject current, GameObject next, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (next != null) next.SetActive(true);
        if (current != null) current.SetActive(false);
    }
}