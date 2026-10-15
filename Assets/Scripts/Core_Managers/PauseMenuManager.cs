
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject pauseMenuContainer;
    public RectTransform menuPanel;          

    [Header("Settings Sub-Panel (volumi)")]
    public RectTransform settingsPanel;      
    public CanvasGroup menuGroup;            
    public CanvasGroup settingsGroup;     

    [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    public float hiddenYPos = 1200f;
    public float visibleYPos = 0f;
    [Tooltip("Durata della transizione menu <-> volumi")]
    public float swapDuration = 0.35f;
    [Tooltip("Spostamento orizzontale durante lo swap (effetto carosello)")]
    public float swapSlideX = 220f;

    [Header("System State")]
    [Tooltip("Se falso, premere ESC non fara' nulla.")]
    public bool canPause = false;

    public bool isPaused = false;
    private Coroutine slideCoroutine;
    private Coroutine swapCoroutine;
    private bool inSettings = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;   
        }
    }

    void Start()
    {
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
        if (menuPanel != null)
            menuPanel.anchoredPosition =
                new Vector2(menuPanel.anchoredPosition.x, hiddenYPos);
        // Il pannello volumi parte invisibile e non interattivo
        if (settingsGroup != null) SetGroup(settingsGroup, 0f, false);
        if (menuGroup != null) SetGroup(menuGroup, 1f, true);
    }

    void Update()
    {
        if (canPause && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                
                if (inSettings) CloseSettings();
                else ResumeGame();
            }
            else PauseGame();
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

        AudioListener.pause = true;

        if (ProceduralMusicManager.instance != null)
            ProceduralMusicManager.instance.PauseMusic();   

        if (GameManager.instance != null &&
            GameManager.instance.qualityBarContainer != null)
            GameManager.instance.qualityBarContainer.SetActive(false);

        inSettings = false;
        if (menuGroup != null) SetGroup(menuGroup, 1f, true);
        if (settingsGroup != null) SetGroup(settingsGroup, 0f, false);

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideMenu(visibleYPos, false));
    }

    public void ResumeGame()
    {
        isPaused = false;
        inSettings = false;
        Time.timeScale = 1f;

        AudioListener.pause = false;

        if (ProceduralMusicManager.instance != null)
            ProceduralMusicManager.instance.ResumeMusic();

        if (GameManager.instance != null &&
            GameManager.instance.qualityBarContainer != null)
            GameManager.instance.qualityBarContainer.SetActive(true);

        if (menuPanel == null)
        {
            if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
            return;
        }

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideMenu(hiddenYPos, true));
    }

    public void OpenSettings()
    {
        if (swapCoroutine != null) StopCoroutine(swapCoroutine);
        swapCoroutine = StartCoroutine(SwapPanels(menuGroup, settingsGroup, -1f));
        inSettings = true;
    }

    public void CloseSettings()
    {
        if (swapCoroutine != null) StopCoroutine(swapCoroutine);
        swapCoroutine = StartCoroutine(SwapPanels(settingsGroup, menuGroup, +1f));
        inSettings = false;
    }

    IEnumerator SwapPanels(CanvasGroup from, CanvasGroup to, float dir)
    {
        if (from == null || to == null) yield break;

        RectTransform fromRt = from.GetComponent<RectTransform>();
        RectTransform toRt = to.GetComponent<RectTransform>();
        Vector2 fromHome = fromRt.anchoredPosition;
        Vector2 toHome = toRt.anchoredPosition;

        to.gameObject.SetActive(true);
        to.alpha = 0f;
        to.interactable = false;
        to.blocksRaycasts = false;
        toRt.anchoredPosition = toHome + new Vector2(-dir * swapSlideX, 0f);
        toRt.localScale = Vector3.one * 0.94f;

        from.interactable = false;      
        from.blocksRaycasts = false;

        float t = 0f;
        while (t < swapDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / swapDuration);
            float e = 1f - Mathf.Pow(1f - k, 3f);

            from.alpha = 1f - e;
            fromRt.anchoredPosition = fromHome + new Vector2(dir * swapSlideX * e, 0f);

            to.alpha = e;
            toRt.anchoredPosition = Vector2.Lerp(
                toHome + new Vector2(-dir * swapSlideX, 0f), toHome, e);
            float scale = 0.94f + 0.06f * e + 0.015f * Mathf.Sin(e * Mathf.PI);
            toRt.localScale = Vector3.one * scale;

            yield return null;
        }

        from.alpha = 0f;
        fromRt.anchoredPosition = fromHome;      
        from.gameObject.SetActive(false);

        to.alpha = 1f;
        toRt.anchoredPosition = toHome;
        toRt.localScale = Vector3.one;
        to.interactable = true;
        to.blocksRaycasts = true;
    }

    void SetGroup(CanvasGroup g, float alpha, bool active)
    {
        g.alpha = alpha;
        g.interactable = active;
        g.blocksRaycasts = active;
        g.gameObject.SetActive(active || alpha > 0f);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;             
        if (GameManager.instance != null) GameManager.instance.ResetQuality();
        canPause = false;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator SlideMenu(float targetY, bool isSlidingOut)
    {
        if (menuPanel == null) yield break;

        float elapsedTime = 0f;
        Vector2 startPos = menuPanel.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        while (elapsedTime < slideDuration)
        {
            if (menuPanel == null) yield break;
            float t = elapsedTime / slideDuration;
            float smoothStep = t * t * (3f - 2f * t);
            menuPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothStep);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (menuPanel != null) menuPanel.anchoredPosition = endPos;
        if (isSlidingOut && pauseMenuContainer != null)
            pauseMenuContainer.SetActive(false);
    }

    public void ReplayCurrentLevel()
    {
        if (GameManager.instance == null) return;

        int lvlIndex = GameManager.instance.currentLevel;
        GameObject activeLevelGO = null;

        LevelSetup[] allLevels = FindObjectsByType<LevelSetup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            if (GameManager.instance != null)
                GameManager.instance.NotifyTransitionFinished();
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
