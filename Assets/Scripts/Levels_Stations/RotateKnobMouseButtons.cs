using UnityEngine;
using UnityEngine.UI;

// NOTA: Rimossi EventSystems e interfacce IPointer. Ora ascoltiamo il 3D!
public class RotateKnobMouseButtons : MonoBehaviour
{
    // [potState, zonaCorrente] -> zonaTarget. CONTRATTO COL MANUALE HTML.
    private static readonly int[,] TargetTable = {
        {2, 0, 1},   // pentola vuota
        {1, 2, 0},   // vapore
        {0, 1, 2}    // bolle
    };

    [Header("Visual Settings (Termometro Realistico)")]
    public Gradient gradient;
    public Image thermometerFill;

    [Header("I Tre Topolini (Zone Target)")]
    public GameObject blueMouse;
    public GameObject yellowMouse;
    public GameObject redMouse;
    [Header("UI & Level Management")]
    public GameObject level2InterfaceContainer; // Il contenitore stile Wax per Orologio e Termometro nel 3D
    public GameObject globalCanvasLevel2; // NUOVO: Il contenitore per la roba UI del livello 2 nel Canvas Generale
    public GameObject NextLevel;
    public GameObject CurrentLevel;
    public GameObject foam;
    public GameObject steam;
    public GameObject check;

    [Header("Orologio Analogico (Infallibile)")]
    public RectTransform timerHand;
    public float degreesPerTick = 30f;
    public float clockOffset = -140f;

    [Header("Fisica dell'Inerzia (Difficoltà)")]
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

    // --- VARIABILI INTERNE ---
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

    // =========================================================
    // NUOVO: GESTIONE ACCENSIONE/SPEGNIMENTO INTERFACCIA (Stile Livello 4)
    // =========================================================
    void OnEnable()
    {
        // Accende termometro e orologio OGNI VOLTA che questo livello viene attivato
        if (level2InterfaceContainer != null) level2InterfaceContainer.SetActive(true);
        if (globalCanvasLevel2 != null) globalCanvasLevel2.SetActive(true); // Accende la UI globale
    }

    void OnDisable()
    {
        // Spegne tutto quando questo livello si disattiva o viene completato
        if (level2InterfaceContainer != null) level2InterfaceContainer.SetActive(false);
        if (globalCanvasLevel2 != null) globalCanvasLevel2.SetActive(false); // Spegne la UI globale

        // Stoppiamo anche i suoni per massima sicurezza!
        if (audioSource != null) audioSource.Stop();
        if (clockAudioSource != null) clockAudioSource.Stop();
    }

    void Start()
    {
        if (thermometerFill != null) currentFillAmount = thermometerFill.fillAmount;
        RandomizePotState();
    }

    void Update()
    {
        if (levelCompleted) return;

        // =========================================================
        // 1. FISICA DELL'INERZIA (SOLO PER IL LIQUIDO)
        // =========================================================
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

        // =========================================================
        // 2. LOGICA DELLE ZONE
        // =========================================================
        if (currentFillAmount <= 0.33f) currentZone = 0;
        else if (currentFillAmount <= 0.66f) currentZone = 1;
        else currentZone = 2;

        UpdateMiceVisuals();

        // =========================================================
        // 3. TIMER DI COTTURA SPIETATO & AUDIO OROLOGIO
        // =========================================================
        if (hasStarted && check.activeSelf == false)
        {
            if (currentZone == targetZone)
            {
                currentErrorTime = 0f;
                currentCookingTime += Time.deltaTime;

                // GESTIONE AUDIO OROLOGIO CONTINUO
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

                // FERMA L'OROLOGIO: Il liquido è fuori zona, il timer si azzera!
                if (clockAudioSource != null && clockAudioSource.isPlaying)
                {
                    clockAudioSource.Stop();
                }

                if (currentErrorTime >= errorTolerance)
                {
                    currentErrorTime = 0f;
                    if (GameManager.instance != null)
                        GameManager.instance.DecreaseGlobalQuality(outOfZonePenalty);

                    RandomizePotState(); // Costringe il giocatore a riadattarsi!
                }
            }
        }
        else
        {
            // Silenzia l'orologio durante le pause o prima del primissimo click
            if (clockAudioSource != null && clockAudioSource.isPlaying) clockAudioSource.Stop();
        }

        // =========================================================
        // 4. OROLOGIO ANALOGICO
        // =========================================================
        if (timerHand != null)
        {
            float displayTime = (!hasStarted || check.activeSelf) ? 0f : currentCookingTime;
            float rotationAngle = clockOffset - (Mathf.Floor(displayTime) * degreesPerTick);
            timerHand.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
        }

        // =========================================================
        // 5. SPUNTA VERDE E RIPARTENZA
        // =========================================================
        if (check.activeSelf)
        {
            checkTimer += Time.deltaTime;
            if (checkTimer >= 1.5f)
            {
                check.SetActive(false);
                checkTimer = 0f;
                hasStarted = false; // Mette in pausa finché non clicchi di nuovo
                RandomizePotState();
            }
        }
    }

    void RoundWon()
    {
        currentWins++;
        currentCookingTime = 0f;
        fillVelocity = 0f; // Azzera l'inerzia

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

        // La famosa Matrice Infallibile!
        targetZone = TargetTable[potState, startingColumn];
    }

    // =========================================================
    // NUOVI COMANDI 3D: GUIDANO L'INERZIA E LA ROTAZIONE!
    // =========================================================
    public void ClickLeft() { HandleClick(-1); }
    public void ClickRight() { HandleClick(1); }

    private void HandleClick(int dir)
    {
        if (levelCompleted || check.activeSelf) return;
        if (!hasStarted) hasStarted = true;

        // Applica forza all'inerzia: dir = -1 (scende), +1 (sale)
        fillVelocity += clickForce * dir;

        // Ruota visivamente la manopola 3D sul suo asse Z
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

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (GameManager.instance != null)
        {
            GameManager.instance.TransitionToNextLevel(CurrentLevel, NextLevel);
        }
    }
}