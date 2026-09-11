using UnityEngine;
using UnityEngine.UI;

public class RotateKnobMouseButtons : MonoBehaviour
{
    private static readonly int[,] TargetTable = {
        {2, 0, 1},
        {1, 2, 0},
        {0, 1, 2}
    };

    [Header("Visual Settings (Termometro Realistico)")]
    public Gradient gradient;
    public Image thermometerFill;

    [Header("I Tre Topolini (Zone Target)")]
    public GameObject blueMouse;
    public GameObject yellowMouse;
    public GameObject redMouse;

    [Header("UI & Level Management")]
    public GameObject level2InterfaceContainer;
    public GameObject globalCanvasLevel2;
    public GameObject NextLevel;
    public GameObject CurrentLevel;
    public GameObject foam;
    public GameObject steam;
    public GameObject check;

    [Header("Orologio Analogico")]
    public RectTransform timerHand;
    public float degreesPerTick = 30f;
    public float clockOffset = -140f;

    [Header("Fisica dell'Inerzia (Difficolta)")]
    [Range(0.01f, 1f)] public float clickForce = 0.15f;
    [Range(0.1f, 5f)] public float friction = 1.5f;

    [Header("Audio SFX (Pentola & Manopola)")]
    public AudioSource audioSource;
    public AudioClip knobClickSound;
    public AudioClip steamSound;
    public AudioClip bubblesSound;

    [Header("Audio SFX (Orologio)")]
    public AudioSource clockAudioSource;
    public AudioClip clockTickSound;

    [Header("Game Balance")]
    public float rotationStep = 15f;
    [Range(1f, 24f)] public float cookingTimeRequired = 12f;
    public int winsNeeded = 3;
    public float errorTolerance = 1f;
    public int outOfZonePenalty = 5;

    private float currentFillAmount = 0f;
    private float fillVelocity = 0f;

    private int currentZone = 0;
    private int targetZone = 0;
    private int potState = 0;
    private int currentWins = 0;

    private float currentCookingTime = 0f;
    private float currentErrorTime = 0f;
    private float checkTimer = 0f;

    private bool levelCompleted = false;
    private bool hasStarted = false;

    void Awake()
    {
        SetLevel2UI(false); // Parte rigorosamente spento
    }

    void OnEnable()
    {
        // Non forziamo l'accensione qui, lasciamo fare al LateUpdate!
    }

    void OnDisable()
    {
        SetLevel2UI(false);

        if (audioSource != null) audioSource.Stop();
        if (clockAudioSource != null) clockAudioSource.Stop();
    }

    void LateUpdate()
    {
        // 1. Spia l'interruttore del livello (GoST2) per sapere se siamo nel Livello 2
        bool levelActive = (CurrentLevel != null) ? CurrentLevel.activeInHierarchy : false;

        // 2. Mostra l'UI solo se siamo attivi e non abbiamo ancora vinto
        bool show = levelActive && !levelCompleted;

        // 3. Nascondi tutto durante le bolle di caricamento!
        if (GameManager.instance != null && GameManager.instance.IsChangingLevel)
            show = false;

        SetLevel2UI(show);
    }

    private void SetLevel2UI(bool show)
    {
        if (level2InterfaceContainer != null && level2InterfaceContainer.activeSelf != show)
            level2InterfaceContainer.SetActive(show);

        if (globalCanvasLevel2 != null
            && globalCanvasLevel2 != level2InterfaceContainer
            && globalCanvasLevel2.activeSelf != show)
        {
            globalCanvasLevel2.SetActive(show);
        }
    }

    void Start()
    {
        if (thermometerFill != null) currentFillAmount = thermometerFill.fillAmount;
        RandomizePotState();
    }

    void Update()
    {
        if (levelCompleted) return;

        if (thermometerFill != null)
        {
            fillVelocity = Mathf.Lerp(fillVelocity, 0f, friction * Time.deltaTime);
            currentFillAmount += fillVelocity * Time.deltaTime;

            if (currentFillAmount <= 0f || currentFillAmount >= 1f)
            {
                fillVelocity = 0f;
                currentFillAmount = Mathf.Clamp01(currentFillAmount);
            }

            thermometerFill.fillAmount = currentFillAmount;
            thermometerFill.color = gradient.Evaluate(currentFillAmount);
        }

        if (currentFillAmount <= 0.33f) currentZone = 0;
        else if (currentFillAmount <= 0.66f) currentZone = 1;
        else currentZone = 2;

        UpdateMiceVisuals();

        if (hasStarted && check.activeSelf == false)
        {
            if (currentZone == targetZone)
            {
                currentErrorTime = 0f;
                currentCookingTime += Time.deltaTime;

                if (clockAudioSource != null && clockTickSound != null)
                {
                    if (!clockAudioSource.isPlaying)
                    {
                        clockAudioSource.clip = clockTickSound;
                        clockAudioSource.loop = true;
                        clockAudioSource.Play();
                    }
                }

                if (currentCookingTime >= cookingTimeRequired)
                {
                    RoundWon();
                }
            }
            else
            {
                currentCookingTime = 0f;
                currentErrorTime += Time.deltaTime;

                if (clockAudioSource != null && clockAudioSource.isPlaying)
                {
                    clockAudioSource.Stop();
                }

                if (currentErrorTime >= errorTolerance)
                {
                    currentErrorTime = 0f;
                    if (GameManager.instance != null)
                        GameManager.instance.DecreaseGlobalQuality(outOfZonePenalty);

                    RandomizePotState();
                }
            }
        }
        else
        {
            if (clockAudioSource != null && clockAudioSource.isPlaying) clockAudioSource.Stop();
        }

        if (timerHand != null)
        {
            float displayTime = (!hasStarted || check.activeSelf) ? 0f : currentCookingTime;
            float rotationAngle = clockOffset - (Mathf.Floor(displayTime) * degreesPerTick);
            timerHand.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
        }

        if (check.activeSelf)
        {
            checkTimer += Time.deltaTime;
            if (checkTimer >= 1.5f)
            {
                check.SetActive(false);
                checkTimer = 0f;
                hasStarted = false;
                RandomizePotState();
            }
        }
    }

    void RoundWon()
    {
        currentWins++;
        currentCookingTime = 0f;
        fillVelocity = 0f;

        check.SetActive(true);
        checkTimer = 0f;

        if (audioSource != null) audioSource.Stop();
        if (clockAudioSource != null) clockAudioSource.Stop();

        if (currentWins >= winsNeeded) LevelComplete();
    }

    void RandomizePotState()
    {
        potState = Random.Range(0, 3);
        if (steam != null) steam.SetActive(potState == 1);
        if (foam != null) foam.SetActive(potState == 2);

        if (audioSource != null)
        {
            audioSource.Stop();
            if (potState == 1 && steamSound != null)
            {
                audioSource.clip = steamSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (potState == 2 && bubblesSound != null)
            {
                audioSource.clip = bubblesSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        int startingColumn = 0;
        if (currentFillAmount <= 0.33f) startingColumn = 0;
        else if (currentFillAmount <= 0.66f) startingColumn = 1;
        else startingColumn = 2;

        targetZone = TargetTable[potState, startingColumn];
    }

    public void ClickLeft() { HandleClick(-1); }
    public void ClickRight() { HandleClick(1); }

    private void HandleClick(int dir)
    {
        if (levelCompleted || check.activeSelf) return;
        if (!hasStarted) hasStarted = true;

        fillVelocity += clickForce * dir;

        transform.Rotate(0f, rotationStep * dir, 0f);

        if (audioSource != null && knobClickSound != null)
        {
            audioSource.PlayOneShot(knobClickSound);
        }
    }

    void UpdateMiceVisuals()
    {
        if (blueMouse != null) blueMouse.SetActive(currentZone == 0);
        if (yellowMouse != null) yellowMouse.SetActive(currentZone == 1);
        if (redMouse != null) redMouse.SetActive(currentZone == 2);
    }

    void LevelComplete()
    {
        levelCompleted = true;
        if (check != null) check.SetActive(true);
        if (audioSource != null) audioSource.Stop();
        if (clockAudioSource != null) clockAudioSource.Stop();

        SetLevel2UI(false);

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (GameManager.instance != null)
        {
            GameManager.instance.TransitionToNextLevel(CurrentLevel, NextLevel);
        }
    }
}