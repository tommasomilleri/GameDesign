using UnityEngine;

/// <summary>
/// Navigazione WASD del punto di vista dentro un'area limitata.
/// Muove il Follow target della CinemachineCamera attiva (MAI la
/// camera direttamente). Accelerazione/decelerazione smussate,
/// clamp morbido sui bordi della Bounds.
/// Uno per stazione, sul rispettivo NavTarget.
/// </summary>
public class DioramaNavigator : MonoBehaviour
{
    [Header("Area navigabile")]
    [Tooltip("BoxCollider (trigger) che delimita l'area. Obbligatorio.")]
    public BoxCollider navBounds;

    [Header("Movimento")]
    public float moveSpeed = 2.5f;
    [Tooltip("Secondi per raggiungere la velocita' piena")]
    public float accelTime = 0.18f;
    [Tooltip("Il movimento e' relativo alla camera (W = avanti visivo)")]
    public bool cameraRelative = true;
    [Tooltip("Blocca l'altezza? (consigliato per i diorami)")]
    public bool lockY = true;

    [Header("Gating")]
    [Tooltip("Indice stazione nel CameraDirector: attivo solo se e' la corrente")]
    public int stationIndex = -1;

    Vector3 velocity;        // velocita' corrente (per lo smoothing)
    Vector3 velRef;          // ref per SmoothDamp
    Vector3 home;            // posizione iniziale (per il reset)
    float fixedY;

    void Start()
    {
        home = transform.position;
        fixedY = home.y;
        if (navBounds == null)
            Debug.LogError("[DioramaNavigator] navBounds mancante su " + name, this);
    }

    void OnEnable()
    {
        // Ogni volta che la stazione (ri)parte: punto di vista di default
        transform.position = home;
        velocity = Vector3.zero;
    }

    void Update()
    {
        // ===== CATENA DI GUARDIE (stesso stile dell'Interactor) =====
        if (navBounds == null) return;
        if (PauseMenuManager.Instance != null &&
            PauseMenuManager.Instance.isPaused) return;
        if (GameManager.instance == null ||
            !GameManager.instance.gameplayActive) return;
        if (GameManager.instance.IsChangingLevel) return;
        // Solo se la regia inquadra QUESTA stazione
        if (stationIndex >= 0 && CameraDirector.Instance != null &&
            CameraDirector.Instance.Current != stationIndex) return;

        // ===== INPUT (Input Manager classico, come da corso U1) =====
        float h = Input.GetAxisRaw("Horizontal");   // A/D
        float v = Input.GetAxisRaw("Vertical");     // W/S
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();   // niente boost diagonale

        // ===== DIREZIONE RELATIVA ALLA CAMERA (pattern U4 FocalPoint) =====
        Vector3 wish;
        if (cameraRelative && Camera.main != null)
        {
            Vector3 fwd = Camera.main.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Camera.main.transform.right; right.y = 0f; right.Normalize();
            wish = fwd * input.z + right * input.x;
        }
        else wish = input;

        // ===== SMOOTHING: accelerazione e frenata morbide =====
        Vector3 targetVel = wish * moveSpeed;
        velocity = Vector3.SmoothDamp(velocity, targetVel, ref velRef, accelTime);

        Vector3 next = transform.position + velocity * Time.deltaTime;

        // ===== CLAMP ALL'AREA: dentro la Bounds, sempre =====
        Bounds b = navBounds.bounds;
        next.x = Mathf.Clamp(next.x, b.min.x, b.max.x);
        next.z = Mathf.Clamp(next.z, b.min.z, b.max.z);
        next.y = lockY ? fixedY : Mathf.Clamp(next.y, b.min.y, b.max.y);

        // FRENATA SUI BORDI: se il clamp ha "mangiato" il movimento,
        // azzera la velocita' su quell'asse (niente spinta contro il muro)
        if (Mathf.Approximately(next.x, b.min.x) ||
            Mathf.Approximately(next.x, b.max.x)) velocity.x = 0f;
        if (Mathf.Approximately(next.z, b.min.z) ||
            Mathf.Approximately(next.z, b.max.z)) velocity.z = 0f;

        transform.position = next;
    }

    /// <summary>Riporta il punto di vista al default (collegabile a un bottone).</summary>
    public void ResetView()
    {
        transform.position = home;
        velocity = Vector3.zero;
    }
}