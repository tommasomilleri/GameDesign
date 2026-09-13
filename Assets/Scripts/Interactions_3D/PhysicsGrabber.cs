using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("Impostazioni Presa")]
    public float grabSpeed = 15f;
    public float maxThrowSpeed = 3f;
    [Tooltip("VELOCITA' MASSIMA di traino: il tetto che impedisce il tunneling!")]
    public float maxPullSpeed = 12f;

    [Header("Profondita' (rotella mouse)")]
    [Tooltip("Velocita' della rotella. Scala della scena: oggetti a 150m → ~40")]
    public float scrollSpeed = 2f;
    public float minDepth = 0.8f;
    public float maxDepth = 6f;
    [Tooltip("Velocita' con cui la profondita' rientra nel range se afferri un oggetto fuori range (m/s). NIENTE piu' strattone al grab!")]
    public float depthAdjustSpeed = 3f;

    [Header("Stabilita'")]
    public float deadZone = 0.05f;

    [Header("Debug")]
    public bool debugLogs = true;

    public bool IsHolding { get; private set; }
    // Aggiungi questa variabile pubblica
    public Rigidbody HeldRigidbody => held;
    public void ConfigureDepth(float newMinDepth, float newMaxDepth, float newScrollSpeed)
    {
        minDepth = newMinDepth;
        maxDepth = newMaxDepth;
        scrollSpeed = newScrollSpeed;
        // NON riclampiamo currentDepth di colpo: ci pensa il
        // riavvicinamento morbido in FixedUpdate.
    }

    Camera cam;
    Rigidbody held;
    float currentDepth;
    Vector3 grabOffset;
    CollisionDetectionMode heldOriginalMode;   // per ripristinarla al rilascio

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            Debug.LogError("[Grabber] Nessuna Camera su '" + name +
                           "'. Mettilo sulla Main Camera!", this);
    }

    void Update()
    {
        if (cam == null) return;

        bool paused = PauseMenuManager.Instance != null &&
                      PauseMenuManager.Instance.isPaused;
        bool noGameplay = GameManager.instance == null ||
                          !GameManager.instance.gameplayActive;
        bool changing = GameManager.instance != null &&
                        GameManager.instance.IsChangingLevel;
        bool blocked = paused || noGameplay || changing;

        if (blocked)
        {
            if (debugLogs && Input.GetMouseButtonDown(0))
                Debug.Log("[Grabber] BLOCCATO: paused=" + paused +
                          " gameplayActive=" + !noGameplay +
                          " isChangingLevel=" + changing);
            if (held != null) Release();
            return;
        }

        // ---- 1. AFFERRA ----
        if (Input.GetMouseButtonDown(0) && held == null)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.rigidbody != null && hit.rigidbody.CompareTag("Grabbable"))
                {
                    held = hit.rigidbody;
                    held.useGravity = false;
                    held.linearDamping = 10f;

                    // ANTI-TUNNEL: fisica continua mentre e' in mano
                    heldOriginalMode = held.collisionDetectionMode;
                    held.collisionDetectionMode =
                        CollisionDetectionMode.ContinuousDynamic;

                    // FIX STRATTONE: si parte dalla distanza VERA
                    // dell'oggetto, SENZA clamp. Il rientro nel range
                    // avviene morbido in FixedUpdate.
                    currentDepth = Vector3.Distance(
                        cam.transform.position, held.position);
                    currentDepth = Vector3.Distance(cam.transform.position, held.position);

                    // AGGIUNGI QUESTA RIGA: Calcola la distanza tra il centro dell'oggetto e il punto esatto che hai cliccato
                    grabOffset = held.position - r.GetPoint(currentDepth);
                    IsHolding = true;
                }
                else if (debugLogs)
                {
                    if (hit.rigidbody == null)
                        Debug.Log("[Grabber] colpito '" + hit.collider.name +
                                  "' a distanza " + hit.distance.ToString("F1") +
                                  " ma NON ha Rigidbody");
                    else
                        Debug.Log("[Grabber] colpito '" + hit.rigidbody.name +
                                  "' a distanza " + hit.distance.ToString("F1") +
                                  " ma il tag e' '" + hit.rigidbody.tag + "'");
                }
            }
            else if (debugLogs)
            {
                Debug.Log("[Grabber] il raycast non ha colpito NULLA");
            }
        }

        // ---- 2. ROTELLA ----
        if (held != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                currentDepth = Mathf.Clamp(currentDepth + scroll * scrollSpeed,
                                           minDepth, maxDepth);
        }

        // ---- 3. RILASCIA ----
        if (Input.GetMouseButtonUp(0) && held != null) Release();
    }

    void Release()
    {
        if (held != null)
        {
            held.useGravity = true;
            held.linearDamping = 0f;
            held.collisionDetectionMode = heldOriginalMode;   // ripristina
            held.linearVelocity = Vector3.ClampMagnitude(
                held.linearVelocity * 0.2f, maxThrowSpeed);
            held.angularVelocity = Vector3.zero;
        }
        held = null;
        IsHolding = false;
    }
    /*
    void FixedUpdate()
    {
        if (held == null) return;

        if (!held.gameObject.activeInHierarchy)
        {
            held = null;
            IsHolding = false;
            return;
        }

        // Rientro MORBIDO nel range: se hai afferrato un oggetto piu'
        // lontano di maxDepth (o piu' vicino di minDepth), la
        // profondita' scivola verso il range invece di scattare.
        float clamped = Mathf.Clamp(currentDepth, minDepth, maxDepth);
        currentDepth = Mathf.MoveTowards(currentDepth, clamped,
                                         depthAdjustSpeed * Time.fixedDeltaTime);

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 target = r.GetPoint(currentDepth);

        Vector3 dir = target - held.position;

        // ANTI-TUNNEL parte 2: velocita' di traino LIMITATA.
        // dir * grabSpeed puo' esplodere se il target e' lontano:
        // il tetto maxPullSpeed impedisce di saltare i collider.
        held.linearVelocity = dir.magnitude < deadZone
            ? Vector3.zero
            : Vector3.ClampMagnitude(dir * grabSpeed, maxPullSpeed);
    }*/
    void FixedUpdate()
    {
        if (held == null) return;

        if (!held.gameObject.activeInHierarchy)
        {
            held = null;
            IsHolding = false;
            return;
        }

        // 1. Rientro MORBIDO ma ISTANTANEO: usiamo Lerp per coprire
        // le grandi distanze in una frazione di secondo.
        float clamped = Mathf.Clamp(currentDepth, minDepth, maxDepth);
        currentDepth = Mathf.Lerp(currentDepth, clamped, 15f * Time.fixedDeltaTime);

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 target = r.GetPoint(currentDepth) + grabOffset;
        Vector3 dir = target - held.position;

        // 2. ANTI-TUNNEL DINAMICO: Se la fiala è lontanissima (sullo scaffale),
        // ignoriamo il tetto di 12f per farla arrivare subito alla camera.
        // Se è vicina (distanza < 5), riattiviamo il tuo maxPullSpeed per precisione.
        float dynamicMaxSpeed = dir.magnitude > 5f ? (dir.magnitude * 50f) : maxPullSpeed;

        held.linearVelocity = dir.magnitude < deadZone
            ? Vector3.zero
            : Vector3.ClampMagnitude(dir * grabSpeed, dynamicMaxSpeed);
    }
}