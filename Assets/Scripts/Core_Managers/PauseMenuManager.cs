using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    // Singleton per accedervi facilmente da ovunque
    public static PauseMenuManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject pauseMenuContainer;
    public RectTransform menuPanel;

    [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    public float hiddenYPos = 1200f;
    public float visibleYPos = 0f;

    [Header("System State")]
    [Tooltip("Se falso, premere ESC non farà nulla.")]
    public bool canPause = false;

    public bool isPaused = false;
    private Coroutine slideCoroutine;

    void Awake()
    {
        // Crea il Singleton in modo sicuro
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }

    void Start()
    {
        if (pauseMenuContainer != null)
        {
            pauseMenuContainer.SetActive(false);
        }

        if (menuPanel != null)
        {
            menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, hiddenYPos);
        }
    }

    void Update()
    {
        // ORA LEGGE ESC SOLO SE CANPAUSE È TRUE!
        if (canPause && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (pauseMenuContainer == null || menuPanel == null)
        {
            Debug.LogWarning("[PauseMenu] Riferimenti UI mancanti: pausa annullata.");
            return;
        }

        isPaused = true;
        pauseMenuContainer.SetActive(true);
        Time.timeScale = 0f;

        // Mette in pausa la musica di sottofondo
        if (ProceduralMusicManager.instance != null)
        {
            ProceduralMusicManager.instance.PauseMusic();
        }

        if (GameManager.instance != null && GameManager.instance.qualityBarContainer != null)
        {
            GameManager.instance.qualityBarContainer.SetActive(false); // Scompare in pausa
        }

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideMenu(visibleYPos, false));
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // Fa ripartire la musica dal punto esatto in cui si era fermata
        if (ProceduralMusicManager.instance != null)
        {
            ProceduralMusicManager.instance.ResumeMusic();
        }

        if (GameManager.instance != null && GameManager.instance.qualityBarContainer != null)
        {
            GameManager.instance.qualityBarContainer.SetActive(true); // <-- CORRETTO: Ricompare!
        }

        if (menuPanel == null)
        {
            if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
            return;
        }

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideMenu(hiddenYPos, true));
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        // 1. Riporta il tempo alla normalità, altrimenti la nuova scena si caricherebbe in pausa!
        if (GameManager.instance != null) GameManager.instance.ResetQuality();
        canPause = false;
        isPaused = false;

        // 2. Ricarica la scena attuale da zero usando il suo Index
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator SlideMenu(float targetY, bool isSlidingOut)
    {
        if (menuPanel == null) yield break; // LUCCHETTO 1: Previene l'errore se manca la UI

        float elapsedTime = 0f;
        Vector2 startPos = menuPanel.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        while (elapsedTime < slideDuration)
        {
            if (menuPanel == null) yield break; // LUCCHETTO 2: Previene l'errore se la UI sparisce durante l'animazione

            float t = elapsedTime / slideDuration;
            float smoothStep = t * t * (3f - 2f * t);

            menuPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothStep);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (menuPanel != null) menuPanel.anchoredPosition = endPos;

        if (isSlidingOut && pauseMenuContainer != null)
        {
            pauseMenuContainer.SetActive(false);
        }
    }

    public void ReplayCurrentLevel()
    {
        if (GameManager.instance == null) return;

        int lvlIndex = GameManager.instance.currentLevel;
        GameObject activeLevelGO = null;

        LevelSetup[] allLevels = FindObjectsByType<LevelSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LevelSetup lvl in allLevels)
        {
            if (lvl.levelIndex == lvlIndex && lvl.gameObject.activeInHierarchy)
            {
                activeLevelGO = lvl.gameObject;
                break;
            }
        }

        if (activeLevelGO != null)
        {
            activeLevelGO.SetActive(false);
            activeLevelGO.SetActive(true);
            ResumeGame();
        }
        else
        {
            Debug.LogWarning("Impossibile trovare il livello attivo per il Replay!");
            ResumeGame();
        }
    }
}